from __future__ import annotations

import asyncio
import json
import os
import time
from typing import List

from fastapi import FastAPI, WebSocket, WebSocketDisconnect
from fastapi.middleware.cors import CORSMiddleware
from fastapi import HTTPException

from .config import settings
from .schemas import TranscriptMessage
from .state import SharedState, create_state, update_metrics, update_vision
from .vision import VisionEngine
from .summary import build_summary
from vosk import Model, KaldiRecognizer


app = FastAPI(title="Coach Service", version="0.1.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


class ConnectionManager:
    def __init__(self) -> None:
        self.ui_clients: List[WebSocket] = []

    async def connect_ui(self, websocket: WebSocket) -> None:
        await websocket.accept()
        self.ui_clients.append(websocket)

    def disconnect_ui(self, websocket: WebSocket) -> None:
        if websocket in self.ui_clients:
            self.ui_clients.remove(websocket)

    async def broadcast_metrics(self, payload: dict) -> None:
        if not self.ui_clients:
            return
        data = json.dumps(payload)
        for ws in list(self.ui_clients):
            try:
                await ws.send_text(data)
            except WebSocketDisconnect:
                self.disconnect_ui(ws)


state: SharedState = create_state()
manager = ConnectionManager()
vision_engine = VisionEngine()

VOSK_MODEL_PATH = os.getenv("VOSK_MODEL_PATH", "")
vosk_model = Model(VOSK_MODEL_PATH) if VOSK_MODEL_PATH else None


async def metrics_pusher() -> None:
    while True:
        await manager.broadcast_metrics(state.last_metrics.model_dump())
        await asyncio.sleep(settings.ui_push_interval_sec)


@app.on_event("startup")
async def startup_event() -> None:
    asyncio.create_task(metrics_pusher())


@app.websocket("/ws/ui")
async def ws_ui(websocket: WebSocket) -> None:
    await manager.connect_ui(websocket)
    try:
        while True:
            message = await websocket.receive_text()
            if message == "ping":
                await websocket.send_text("pong")
    except WebSocketDisconnect:
        manager.disconnect_ui(websocket)


@app.websocket("/ws/ingest")
async def ws_ingest(websocket: WebSocket) -> None:
    await websocket.accept()
    try:
        while True:
            payload = await websocket.receive_text()
            data = json.loads(payload)
            msg = TranscriptMessage(**data)
            update_metrics(state, msg)
            await websocket.send_text(
                json.dumps({"status": "ok", "received_ms": int(time.time() * 1000)})
            )
    except WebSocketDisconnect:
        return


@app.websocket("/ws/audio")
async def ws_audio(websocket: WebSocket) -> None:
    await websocket.accept()
    if vosk_model is None:
        await websocket.send_text("error:Vosk model not configured")
        await websocket.close()
        return
    recognizer = KaldiRecognizer(vosk_model, 16000)
    try:
        while True:
            data = await websocket.receive_bytes()
            if recognizer.AcceptWaveform(data):
                result = json.loads(recognizer.Result())
                text = result.get("text", "").strip()
                if text:
                    msg = TranscriptMessage(
                        speaker="rep",
                        text=text,
                        timestamp_ms=int(time.time() * 1000),
                    )
                    update_metrics(state, msg)
            else:
                partial = json.loads(recognizer.PartialResult()).get("partial", "").strip()
                if partial:
                    msg = TranscriptMessage(
                        speaker="rep",
                        text=partial,
                        timestamp_ms=int(time.time() * 1000),
                    )
                    update_metrics(state, msg)
            await websocket.send_text("ok")
    except WebSocketDisconnect:
        return


@app.websocket("/ws/vision")
async def ws_vision(websocket: WebSocket) -> None:
    await websocket.accept()
    try:
        while True:
            frame = await websocket.receive_bytes()
            result = vision_engine.analyze(frame)
            update_vision(state, result)
            await websocket.send_text("ok")
    except WebSocketDisconnect:
        return


@app.post("/outcome")
async def post_outcome(payload: dict) -> dict:
    outcome = payload.get("outcome")
    if outcome not in {"meeting_booked", "follow_up", "lost"}:
        raise HTTPException(status_code=400, detail="Invalid outcome")
    state.bandit.apply_outcome(state.last_suggestions, outcome)
    return {"status": "ok"}


@app.get("/summary")
async def get_summary() -> dict:
    summary = build_summary(state.perception, state.last_metrics)
    return {"summary": summary}
