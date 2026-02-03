from __future__ import annotations

from dataclasses import dataclass
from typing import List, Tuple

import faiss
import numpy as np
from sentence_transformers import SentenceTransformer

from .config import settings


@dataclass
class RetrievalItem:
    line: str
    stage: str
    tags: List[str]


class RetrievalEngine:
    def __init__(self) -> None:
        self.model = SentenceTransformer(settings.model_name)
        self.items = self._seed_items()
        self.index, self.embeddings = self._build_index(self.items)

    def _seed_items(self) -> List[RetrievalItem]:
        return [
            RetrievalItem(
                line="What does your current process look like today?",
                stage="situation",
                tags=["open", "nepq"],
            ),
            RetrievalItem(
                line="How is that impacting your team right now?",
                stage="problem",
                tags=["impact", "nepq"],
            ),
            RetrievalItem(
                line="What happens if this stays the same for the next quarter?",
                stage="awareness",
                tags=["consequence", "nepq"],
            ),
            RetrievalItem(
                line="What would an ideal outcome look like for you?",
                stage="solution",
                tags=["vision", "nepq"],
            ),
            RetrievalItem(
                line="Who else should be involved in evaluating next steps?",
                stage="closing",
                tags=["decision", "nepq"],
            ),
            RetrievalItem(
                line="Can you share more about that?",
                stage="connect",
                tags=["probe", "nepq"],
            ),
        ]

    def _build_index(self, items: List[RetrievalItem]) -> Tuple[faiss.IndexFlatIP, np.ndarray]:
        texts = [i.line for i in items]
        embeddings = self.model.encode(texts, normalize_embeddings=True)
        dim = embeddings.shape[1]
        index = faiss.IndexFlatIP(dim)
        index.add(embeddings.astype(np.float32))
        return index, embeddings

    def query(self, context: str, stage: str, top_k: int) -> List[RetrievalItem]:
        if not context:
            return self.items[:top_k]
        query_vec = self.model.encode([context], normalize_embeddings=True)
        scores, indices = self.index.search(query_vec.astype(np.float32), top_k)
        ranked = []
        for idx in indices[0]:
            if idx < 0:
                continue
            item = self.items[int(idx)]
            ranked.append(item)
        stage_matched = [i for i in ranked if i.stage == stage]
        if stage_matched:
            return stage_matched[:top_k]
        return ranked[:top_k]
