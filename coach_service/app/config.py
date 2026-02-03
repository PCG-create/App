from dataclasses import dataclass


@dataclass
class Settings:
    ui_push_interval_sec: float = 1.0
    max_history: int = 50
    model_name: str = "sentence-transformers/all-MiniLM-L6-v2"
    top_k: int = 5


settings = Settings()
