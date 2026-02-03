from __future__ import annotations

from collections import deque
from dataclasses import dataclass
from typing import Deque, List
import time

from .schemas import TranscriptMessage


POSITIVE_WORDS = {
    "great",
    "good",
    "love",
    "like",
    "yes",
    "sure",
    "absolutely",
    "interested",
    "excited",
    "helpful",
}

NEGATIVE_WORDS = {
    "no",
    "not",
    "never",
    "bad",
    "hate",
    "expensive",
    "concern",
    "problem",
    "issue",
    "busy",
}

STAGE_KEYWORDS = {
    "connect": {"thanks", "appreciate", "time"},
    "situation": {"currently", "today", "process", "workflow", "stack"},
    "problem": {"pain", "issue", "challenge", "problem", "frustrated"},
    "awareness": {"impact", "risk", "consequence"},
    "solution": {"need", "want", "looking", "evaluate"},
    "closing": {"next step", "timeline", "decision", "approve"},
}


@dataclass
class PerceptionState:
    history: Deque[TranscriptMessage]
    rep_word_count: int = 0
    prospect_word_count: int = 0
    rep_questions: int = 0
    start_time_ms: int = 0


class PerceptionEngine:
    def __init__(self, max_history: int) -> None:
        self.state = PerceptionState(history=deque(maxlen=max_history))

    def ingest(self, message: TranscriptMessage) -> None:
        if self.state.start_time_ms == 0:
            self.state.start_time_ms = message.timestamp_ms
        self.state.history.append(message)

        word_count = len(message.text.split())
        if message.speaker == "rep":
            self.state.rep_word_count += word_count
            if "?" in message.text:
                self.state.rep_questions += message.text.count("?")
        else:
            self.state.prospect_word_count += word_count

    def _sentiment_score(self, text: str) -> float:
        tokens = [t.strip(".,!?;:").lower() for t in text.split()]
        pos = sum(1 for t in tokens if t in POSITIVE_WORDS)
        neg = sum(1 for t in tokens if t in NEGATIVE_WORDS)
        if pos == 0 and neg == 0:
            return 0.0
        return (pos - neg) / max(pos + neg, 1)

    def sentiment(self) -> float:
        if not self.state.history:
            return 0.0
        scores = [self._sentiment_score(m.text) for m in self.state.history]
        return sum(scores) / max(len(scores), 1)

    def talk_listen_ratio(self) -> float:
        rep = self.state.rep_word_count
        prospect = self.state.prospect_word_count
        if prospect == 0:
            return float(rep)
        return rep / prospect

    def questions_per_minute(self) -> float:
        if self.state.start_time_ms == 0:
            return 0.0
        elapsed_min = (time.time() * 1000 - self.state.start_time_ms) / 60000
        if elapsed_min <= 0:
            return 0.0
        return self.state.rep_questions / elapsed_min

    def engagement(self) -> float:
        sentiment = self.sentiment()
        ratio = self.talk_listen_ratio()
        score = 0.6
        if sentiment < -0.2:
            score -= 0.2
        if ratio > 2.0:
            score -= 0.1
        if ratio < 0.8:
            score += 0.05
        return max(0.0, min(1.0, score))

    def stage(self) -> str:
        text = " ".join(m.text.lower() for m in list(self.state.history)[-6:])
        for stage, keywords in STAGE_KEYWORDS.items():
            for kw in keywords:
                if kw in text:
                    return stage
        return "connect"

    def recent_context(self) -> List[str]:
        return [m.text for m in list(self.state.history)[-6:]]

    def recent_messages(self, limit: int = 12) -> List[TranscriptMessage]:
        return list(self.state.history)[-limit:]
