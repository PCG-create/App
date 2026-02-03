from __future__ import annotations

from dataclasses import dataclass
from typing import List
import os

from sentence_transformers import CrossEncoder
from transformers import pipeline


@dataclass
class GenerationConfig:
    enable_llm: bool = False
    llm_model: str = "distilgpt2"
    cross_encoder_model: str = "cross-encoder/ms-marco-MiniLM-L-6-v2"


class GeneratorEngine:
    def __init__(self) -> None:
        self.config = GenerationConfig(enable_llm=os.getenv("ENABLE_LLM", "0") == "1")
        self.cross_encoder = CrossEncoder(self.config.cross_encoder_model)
        self.text_generator = None
        if self.config.enable_llm:
            self.text_generator = pipeline("text-generation", model=self.config.llm_model)

    def generate(self, context: str, stage: str, retrieved: List[str], sentiment: float) -> List[str]:
        base = list(retrieved)
        base.extend(self._template_candidates(stage))
        if self.text_generator:
            prompt = self._prompt(context, stage)
            llm_out = self.text_generator(prompt, max_new_tokens=40, num_return_sequences=2)
            for item in llm_out:
                text = item["generated_text"].replace(prompt, "").strip()
                if text:
                    base.append(text)
        filtered = self._filter_for_sentiment(base, sentiment)
        return self._rank(context, filtered)

    def _template_candidates(self, stage: str) -> List[str]:
        if stage == "problem":
            return [
                "Can you walk me through a recent example?",
                "How often does that happen in a typical week?",
            ]
        if stage == "awareness":
            return [
                "What impact does that have on your goals?",
                "How does that affect your customers or team?",
            ]
        if stage == "solution":
            return [
                "What would success look like in three months?",
                "Which outcomes matter most to you?",
            ]
        if stage == "closing":
            return [
                "What would be the right next step for you?",
                "Who needs to be part of that decision?",
            ]
        return [
            "What is most important for you to solve first?",
            "Can you share a bit more about that?",
        ]

    def _prompt(self, context: str, stage: str) -> str:
        safe_context = context[-300:]
        return (
            "You are a sales coach. Provide one open ended question "
            "grounded in the conversation. Stage: "
            f"{stage}. Context: {safe_context}\nQuestion:"
        )

    def _filter_for_sentiment(self, candidates: List[str], sentiment: float) -> List[str]:
        if sentiment >= -0.2:
            return candidates
        blocked_terms = {"next step", "decision", "approve", "timeline", "commit"}
        filtered = []
        for line in candidates:
            lower = line.lower()
            if any(term in lower for term in blocked_terms):
                continue
            filtered.append(line)
        return filtered

    def _rank(self, context: str, candidates: List[str]) -> List[str]:
        unique = list(dict.fromkeys([c.strip() for c in candidates if c.strip()]))
        if not unique:
            return []
        pairs = [(context, c) for c in unique]
        scores = self.cross_encoder.predict(pairs)
        ranked = sorted(zip(scores, unique), key=lambda x: x[0], reverse=True)
        return [line for _, line in ranked]
