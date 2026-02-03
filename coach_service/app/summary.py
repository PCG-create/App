from __future__ import annotations

from typing import List
from .schemas import LiveMetrics
from .perception import PerceptionEngine


def build_summary(perception: PerceptionEngine, metrics: LiveMetrics) -> str:
    lines = []
    lines.append("Call summary")
    lines.append(f"Stage: {metrics.methodology_stage}")
    lines.append(f"Talk to listen: {metrics.talk_listen_ratio:.2f}")
    lines.append(f"Sentiment: {metrics.sentiment:.2f}")
    lines.append("Recent transcript:")
    for message in perception.recent_messages():
        lines.append(f"- {message.speaker}: {message.text}")
    return "\n".join(lines)
