from pydantic import BaseModel, Field
from typing import List, Literal


class TranscriptMessage(BaseModel):
    speaker: Literal["rep", "prospect"]
    text: str = Field(min_length=1)
    timestamp_ms: int


class LiveMetrics(BaseModel):
    talk_listen_ratio: float
    questions_per_minute: float
    sentiment: float
    engagement: float
    methodology_stage: str
    say_next: List[str]
    last_update_ms: int
