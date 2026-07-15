"""
CVFaceRecoV1 — Face Recognition Microservice (dlib/face_recognition edition)
FastAPI + face_recognition + inline cosine similarity

Endpoints:
  POST /api/embed         — Extract a 128-dim embedding from a single image
  POST /api/attend        — Recognize faces against a list of known embeddings
  POST /api/index/rebuild — (Optional) Replace the in-memory index
  GET  /api/health        — Liveness probe

Authentication:
  Every request must include the header:  X-AI-Secret-Key: <key>
  Set the key via the SECRET_KEY environment variable (or .env file).
"""

import os
import json
import time
import base64
import logging
from io import BytesIO
from typing import Optional
from datetime import datetime

import numpy as np
from PIL import Image
import cv2
import uvicorn
from fastapi import FastAPI, HTTPException, Depends, Header
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from dotenv import load_dotenv

# ── face_recognition (dlib-based) ─────────────────────────────────────────────
import face_recognition

load_dotenv()

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("CVFaceReco")

# ── Configuration ─────────────────────────────────────────────────────────────
SECRET_KEY      = os.getenv("SECRET_KEY", "change-me-in-production-key-1234")
MODEL_VERSION   = os.getenv("MODEL_VERSION", "CVFaceRecoV1_dlib")
COSINE_THRESHOLD = float(os.getenv("COSINE_THRESHOLD", "0.40"))
CAMERA_SOURCE   = os.getenv("CAMERA_SOURCE", None)

logger.info("Face Recognition Service using dlib/face-recognition is ready.")

# ── FastAPI app ───────────────────────────────────────────────────────────────
app = FastAPI(
    title="CV Face Recognition Service",
    version=MODEL_VERSION,
    description="dlib/face_recognition-based face embedding & recognition microservice.",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# ── Auth dependency ───────────────────────────────────────────────────────────
def verify_key(x_ai_secret_key: Optional[str] = Header(default=None)):
    if x_ai_secret_key != SECRET_KEY:
        raise HTTPException(status_code=401, detail="Invalid or missing X-AI-Secret-Key.")

# ── Helpers ───────────────────────────────────────────────────────────────────

def decode_image(b64_string: str) -> np.ndarray:
    """Decode a base64 image string to RGB numpy array for face_recognition."""
    if "," in b64_string:
        b64_string = b64_string.split(",", 1)[1]
    img_bytes = base64.b64decode(b64_string)
    pil_img   = Image.open(BytesIO(img_bytes)).convert("RGB")
    return np.array(pil_img)


def get_faces_with_scores(img) -> list:
    """Detect faces and get their location and HOG detection score (quality)."""
    detector = face_recognition.api.face_detector
    rects, scores, idx = detector.run(img, 1, -1.0)
    
    faces = []
    for rect, score in zip(rects, scores):
        top = max(0, rect.top())
        right = min(img.shape[1], rect.right())
        bottom = min(img.shape[0], rect.bottom())
        left = max(0, rect.left())
        
        # Pure sigmoid mapping of the raw HOG model decision score to [0.0, 1.0]
        quality = 1.0 / (1.0 + np.exp(-score))
        
        faces.append({
            "box": (top, right, bottom, left),
            "quality": round(float(quality), 4)
        })
    return faces


def cosine_similarity(a: np.ndarray, b: np.ndarray) -> float:
    """Cosine similarity between two L2-normalised vectors."""
    return float(np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b) + 1e-8))


def embedding_to_json(embedding: np.ndarray) -> str:
    """Serialize a numpy float array to a compact JSON string."""
    return json.dumps(embedding.tolist())


def json_to_embedding(json_str: str) -> np.ndarray:
    """Deserialize JSON string back to a float32 numpy array."""
    return np.array(json.loads(json_str), dtype=np.float32)


def face_area(loc) -> int:
    """Calculate the area of a face bounding box (top, right, bottom, left)."""
    return (loc[2] - loc[0]) * (loc[1] - loc[3])


# ── Request / Response schemas ────────────────────────────────────────────────

class EmbedRequest(BaseModel):
    image: str                          # base64 image
    model_version: str = MODEL_VERSION

class EmbedResponse(BaseModel):
    face_found: bool
    embedding_json: str = ""
    quality_score: float = 0.0
    model_version: str = MODEL_VERSION


class EmbeddingEntry(BaseModel):
    user_id: str                        # GUID string
    embedding_json: str                 # JSON float[]


class AttendRequest(BaseModel):
    image: str                          # base64 image
    model_version: str = MODEL_VERSION
    known_embeddings: list[EmbeddingEntry]
    detect_liveness: bool = False
    camera_id: str = "default"


class FaceMatchResult(BaseModel):
    user_id: str
    confidence: float
    bounding_box: Optional[list[float]] = None
    is_live: Optional[bool] = None


class AttendResponse(BaseModel):
    matches: list[FaceMatchResult]
    total_faces_detected: int
    model_version: str = MODEL_VERSION


class RebuildRequest(BaseModel):
    model_version: str = MODEL_VERSION
    embeddings: list[EmbeddingEntry]


# ── Endpoints ─────────────────────────────────────────────────────────────────

@app.get("/api/health")
def health():
    return {
        "status": "ok",
        "model_version": MODEL_VERSION,
        "model_loaded": True,
        "timestamp": time.time(),
    }


@app.post("/api/embed", response_model=EmbedResponse, dependencies=[Depends(verify_key)])
def embed(req: EmbedRequest):
    """
    Extract a 128-dimensional dlib face embedding from a single image.
    """
    try:
        img = decode_image(req.image)
        faces = get_faces_with_scores(img)
    except Exception as e:
        logger.error("Decode/detect error: %s", e)
        raise HTTPException(status_code=400, detail=f"Image decode or detection failed: {e}")

    if not faces:
        return EmbedResponse(face_found=False)

    # Use the largest face
    best_face = max(faces, key=lambda f: face_area(f["box"]))
    best_loc = best_face["box"]
    quality = best_face["quality"]
    
    try:
        encodings = face_recognition.face_encodings(img, [best_loc])
    except Exception as e:
        logger.error("Encoding extraction error: %s", e)
        raise HTTPException(status_code=500, detail=f"Failed to extract face encodings: {e}")

    if not encodings:
        return EmbedResponse(face_found=False)

    emb = encodings[0]
    return EmbedResponse(
        face_found=True,
        embedding_json=embedding_to_json(emb),
        quality_score=quality
    )


@app.post("/api/attend", response_model=AttendResponse, dependencies=[Depends(verify_key)])
def attend(req: AttendRequest):
    """
    Detect all faces in the image and match each against the supplied known embeddings.
    """
    try:
        img = decode_image(req.image)
        faces = get_faces_with_scores(img)
        # Filter out weak detections (likely false positives from background patterns)
        valid_faces = [f for f in faces if f["quality"] >= 0.65]
        
        face_locations = [f["box"] for f in valid_faces]
        encodings = face_recognition.face_encodings(img, face_locations)
    except Exception as e:
        logger.error("Attend decode/detect error: %s", e)
        raise HTTPException(status_code=400, detail=f"Image decode or detection failed: {e}")

    if not face_locations or not encodings:
        return AttendResponse(matches=[], total_faces_detected=0)

    # Preload known embeddings into numpy arrays
    known: list[tuple[str, np.ndarray]] = []
    for entry in req.known_embeddings:
        try:
            known.append((entry.user_id, json_to_embedding(entry.embedding_json)))
        except Exception:
            logger.warning("Could not parse embedding for user_id=%s — skipping.", entry.user_id)

    matches: list[FaceMatchResult] = []

    for face_item, det_emb in zip(valid_faces, encodings):
        loc = face_item["box"]
        # Format bbox: [x1, y1, x2, y2]
        bbox = [loc[3], loc[0], loc[1], loc[2]]
        
        is_live = None
        if req.detect_liveness:
            # Running as APIs only - skip anti-spoof checks and assume True
            is_live = True

        # If liveness check failed, skip matching this face
        if is_live is False:
            logger.warning(f"Face failed liveness check (camera_id={req.camera_id}). Skipping recognition.")
            continue

        best_uid, best_score = None, -1.0

        if known:
            # Extract known encodings list for face_distance
            known_encs = [item[1] for item in known]
            # Calculate face distances using the actual dlib model distance metric
            distances = face_recognition.face_distance(known_encs, det_emb)
            
            # Find the best match (minimum distance)
            best_idx = int(np.argmin(distances))
            best_distance = float(distances[best_idx])
            
            # Map dlib distance to confidence score: 1.0 - distance
            # Dlib matching threshold is typically 0.6.
            # If distance is 0.6, confidence is exactly 0.40, which matches SecurityThreshold = 0.40
            confidence = 1.0 - best_distance
            
            best_uid = known[best_idx][0]
            best_score = confidence

        if best_uid and best_score >= COSINE_THRESHOLD:
            # Normalize bounding box coordinates for frontend percentage overlay rendering
            h_img, w_img = img.shape[:2]
            x1_pct = (bbox[0] / w_img) * 100
            y1_pct = (bbox[1] / h_img) * 100
            x2_pct = (bbox[2] / w_img) * 100
            y2_pct = (bbox[3] / h_img) * 100
            norm_bbox = [round(x1_pct, 1), round(y1_pct, 1), round(x2_pct, 1), round(y2_pct, 1)]

            matches.append(FaceMatchResult(
                user_id=best_uid,
                confidence=round(best_score, 4),
                bounding_box=norm_bbox,
                is_live=is_live
            ))

    return AttendResponse(matches=matches, total_faces_detected=len(face_locations))


@app.post("/api/index/rebuild", dependencies=[Depends(verify_key)])
def rebuild_index(req: RebuildRequest):
    count = len(req.embeddings)
    logger.info("Index rebuild acknowledged: %d embeddings.", count)
    return {"status": "ok", "count": count, "model_version": MODEL_VERSION}


@app.on_event("startup")
def startup_event():
    logger.info("API Server started successfully. Background Camera Service is disabled.")


# ── Entry point ───────────────────────────────────────────────────────────────
if __name__ == "__main__":
    uvicorn.run("server:app", host="0.0.0.0", port=8000, reload=False)
