from __future__ import annotations

from dataclasses import dataclass, field
from typing import Dict, List


@dataclass
class BanditArm:
    line: str
    shown: int = 0
    wins: int = 0

    def score(self) -> float:
        if self.shown == 0:
            return 0.5
        return self.wins / self.shown


@dataclass
class BanditState:
    arms: Dict[str, BanditArm] = field(default_factory=dict)

    def register_lines(self, lines: List[str]) -> None:
        for line in lines:
            if line not in self.arms:
                self.arms[line] = BanditArm(line=line)
            self.arms[line].shown += 1

    def apply_outcome(self, lines: List[str], outcome: str) -> None:
        reward = 1 if outcome in {"meeting_booked", "follow_up"} else 0
        for line in lines:
            if line not in self.arms:
                self.arms[line] = BanditArm(line=line)
            if reward == 1:
                self.arms[line].wins += 1

    def rank(self, lines: List[str]) -> List[str]:
        scored = []
        for line in lines:
            arm = self.arms.get(line)
            score = arm.score() if arm else 0.5
            scored.append((score, line))
        scored.sort(reverse=True)
        return [line for _, line in scored]
