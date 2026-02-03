import asyncio
import json
import time
import websockets


SAMPLE_DIALOG = [
    ("rep", "Thanks for taking the time today. How are things going?"),
    ("prospect", "Busy, we have a lot of manual steps in our process."),
    ("rep", "What does your current workflow look like?"),
    ("prospect", "We use spreadsheets and it slows us down."),
    ("rep", "How is that impacting your team right now?"),
    ("prospect", "It causes delays and people get frustrated."),
    ("rep", "What happens if this stays the same for the next quarter?"),
]


async def main() -> None:
    uri = "ws://127.0.0.1:8000/ws/ingest"
    async with websockets.connect(uri) as ws:
        for speaker, text in SAMPLE_DIALOG:
            payload = {
                "speaker": speaker,
                "text": text,
                "timestamp_ms": int(time.time() * 1000),
            }
            await ws.send(json.dumps(payload))
            await ws.recv()
            await asyncio.sleep(0.8)


if __name__ == "__main__":
    asyncio.run(main())
