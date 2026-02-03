from __future__ import annotations

from dataclasses import dataclass
from typing import List
import time

from .generation import GeneratorEngine
from .learning import BanditState
from .perception import PerceptionEngine
from .retrieval import RetrievalEngine
from .schemas import LiveMetrics, TranscriptMessage
from .config import settings
from .vision import VisionResult


@dataclass
class SharedState:
    perception: PerceptionEngine
    retrieval: RetrievalEngine
    generator: GeneratorEngine
    bandit: BanditState
    last_suggestions: List[str]
    vision_engagement: float
    last_metrics: LiveMetrics


def create_state() -> SharedState:
    perception = PerceptionEngine(max_history=settings.max_history)
    retrieval = RetrievalEngine()
    generator = GeneratorEngine()
    bandit = BanditState()
    now_ms = int(time.time() * 1000)
    metrics = LiveMetrics(
        talk_listen_ratio=0.0,
        questions_per_minute=0.0,
        sentiment=0.0,
        engagement=0.6,
        methodology_stage="connect",
        say_next=["Welcome. Start with an open question."],
        last_update_ms=now_ms,
    )
    return SharedState(
        perception=perception,
        retrieval=retrieval,
        generator=generator,
        bandit=bandit,
        last_suggestions=[],
        vision_engagement=0.5,
        last_metrics=metrics,
    )


def update_metrics(state: SharedState, message: TranscriptMessage) -> LiveMetrics:
    state.perception.ingest(message)
    stage = state.perception.stage()
    context = " ".join(state.perception.recent_context())
    candidates = state.retrieval.query(context, stage, settings.top_k)
    retrieved_lines = [c.line for c in candidates]
    sentiment = state.perception.sentiment()
    generated = state.generator.generate(context, stage, retrieved_lines, sentiment)
    ranked = state.bandit.rank(generated)
    say_next = ranked[:3]
    state.bandit.register_lines(say_next)
    state.last_suggestions = say_next
    engagement = (state.perception.engagement() * 0.7) + (state.vision_engagement * 0.3)
    metrics = LiveMetrics(
        talk_listen_ratio=state.perception.talk_listen_ratio(),
        questions_per_minute=state.perception.questions_per_minute(),
        sentiment=sentiment,
        engagement=engagement,
        methodology_stage=stage,
        say_next=say_next,
        last_update_ms=int(time.time() * 1000),
    )
    state.last_metrics = metrics
    return metrics


def update_vision(state: SharedState, vision: VisionResult) -> LiveMetrics:
    state.vision_engagement = vision.gaze_score if vision.face_present else 0.2
    metrics = state.last_metrics.model_copy(deep=True)
    metrics.engagement = (state.perception.engagement() * 0.7) + (state.vision_engagement * 0.3)
    metrics.last_update_ms = int(time.time() * 1000)
    state.last_metrics = metrics
    return metrics
