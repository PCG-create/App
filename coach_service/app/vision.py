from __future__ import annotations

from dataclasses import dataclass
from typing import Tuple
import cv2
import numpy as np
import mediapipe as mp


@dataclass
class VisionResult:
    face_present: bool
    gaze_score: float


class VisionEngine:
    def __init__(self) -> None:
        self.detector = mp.solutions.face_detection.FaceDetection(model_selection=0, min_detection_confidence=0.5)

    def analyze(self, jpeg_bytes: bytes) -> VisionResult:
        image = self._decode(jpeg_bytes)
        if image is None:
            return VisionResult(face_present=False, gaze_score=0.0)
        height, width, _ = image.shape
        rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
        result = self.detector.process(rgb)
        if not result.detections:
            return VisionResult(face_present=False, gaze_score=0.0)
        detection = result.detections[0]
        bbox = detection.location_data.relative_bounding_box
        center_x = bbox.xmin + bbox.width / 2.0
        center_y = bbox.ymin + bbox.height / 2.0
        gaze_score = self._gaze_proxy(center_x, center_y)
        return VisionResult(face_present=True, gaze_score=gaze_score)

    def _decode(self, jpeg_bytes: bytes) -> np.ndarray | None:
        data = np.frombuffer(jpeg_bytes, dtype=np.uint8)
        image = cv2.imdecode(data, cv2.IMREAD_COLOR)
        return image

    def _gaze_proxy(self, cx: float, cy: float) -> float:
        dx = abs(cx - 0.5)
        dy = abs(cy - 0.5)
        dist = (dx * dx + dy * dy) ** 0.5
        score = max(0.0, 1.0 - dist * 2.0)
        return min(1.0, score)
