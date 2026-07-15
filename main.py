import os
import cv2
import numpy as np
import base64
import json
import time
import requests
import urllib3
import uuid
import mediapipe as mp
from datetime import datetime
from dotenv import load_dotenv

# Suppress self-signed SSL certificate warnings for local development
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

from camera_service import CameraService

# Load environment variables
load_dotenv()

SECRET_KEY = os.getenv("SECRET_KEY", "change-me-in-production-key-1234")
API_URL = os.getenv("FACE_RECO_SERVICE_URL", "http://localhost:8000")
BACKEND_URL = os.getenv("BACKEND_URL", "https://localhost:7177")
CAMERA_SOURCE = os.getenv("CAMERA_SOURCE", "0")
CAMERA_KEY = os.getenv("CAMERA_KEY", "1234")
IS_EMPLOYEE_CAMERA = os.getenv("IS_EMPLOYEE_CAMERA", "False").lower() in ("true", "1", "yes")

# ==============================================================================
# Liveness Detection Constants & Configuration (Merged from anti_spoof.py)
# ==============================================================================
MAX_FACES = 1
MIN_DETECTION_CONFIDENCE = 0.5
FACE_MAX_HEIGHT = 200
DEPTH_THRESHOLD = 0.025
BLINK_THRESHOLD = 0.20
CONSECUTIVE_FRAMES = 2
HEAD_POSE_THRESHOLD = 0.15
LIVE_CONSECUTIVE_REQUIRED = 2
LIVENESS_COOLDOWN = 10

LEFT_EYE_INDICES = [33, 160, 158, 133, 153, 144]
RIGHT_EYE_INDICES = [362, 385, 387, 263, 373, 380]
KEY_LANDMARKS = [1, 10, 152, 234, 454]

# MediaPipe Face Mesh Setup
mp_face_mesh = mp.solutions.face_mesh
face_mesh = mp_face_mesh.FaceMesh(
    static_image_mode=False,
    max_num_faces=MAX_FACES,
    refine_landmarks=True,
    min_detection_confidence=MIN_DETECTION_CONFIDENCE
)

# ==============================================================================
# Liveness Detection Classes & Functions (Merged from anti_spoof.py)
# ==============================================================================
class LivenessDetector:
    def __init__(self):
        self.blink_counter = 0
        self.blink_detected = False
        self.prev_ear = 0.0
        self.face_present = False
        self.prev_yaw = 0.0
        self.prev_pitch = 0.0
        self.movement_detected = False
        self.frame_count = 0
        self.live_counter = 0
        
        self.liveness_confirmed = False
        self.liveness_confirm_time = 0
        self.last_challenge_issued = None

    def reset(self):
        self.blink_counter = 0
        self.blink_detected = False
        self.movement_detected = False
        self.prev_yaw = 0.0
        self.prev_pitch = 0.0
        self.live_counter = 0

    def check_blink(self, ear):
        if ear < BLINK_THRESHOLD:
            self.blink_counter += 1
        else:
            if self.blink_counter >= CONSECUTIVE_FRAMES:
                self.blink_detected = True
            self.blink_counter = 0
        return self.blink_detected

    def check_movement(self, yaw, pitch):
        if self.prev_yaw != 0.0 or self.prev_pitch != 0.0:
            yaw_diff = abs(yaw - self.prev_yaw)
            pitch_diff = abs(pitch - self.prev_pitch)
            if yaw_diff > HEAD_POSE_THRESHOLD or pitch_diff > HEAD_POSE_THRESHOLD:
                self.movement_detected = True
        self.prev_yaw = yaw
        self.prev_pitch = pitch
        return self.movement_detected

    def confirm_liveness(self):
        self.liveness_confirmed = True
        self.liveness_confirm_time = time.time()
        self.blink_detected = False
        self.movement_detected = False
        self.live_counter = 0

    def is_in_cooldown(self):
        if not self.liveness_confirmed:
            return False
        if time.time() - self.liveness_confirm_time < LIVENESS_COOLDOWN:
            return True
        else:
            self.liveness_confirmed = False
            return False

# Global mapping of camera_id -> LivenessDetector
detectors = {}

def get_detector(camera_id="default"):
    global detectors
    if camera_id not in detectors:
        detectors[camera_id] = LivenessDetector()
    return detectors[camera_id]

def reset_detector(camera_id="default"):
    get_detector(camera_id).reset()

def compute_ear(landmarks, eye_indices):
    points = []
    for idx in eye_indices:
        point = landmarks.landmark[idx]
        points.append([point.x, point.y])
    points = np.array(points)

    v1 = np.linalg.norm(points[1] - points[5])
    v2 = np.linalg.norm(points[2] - points[4])
    h = np.linalg.norm(points[0] - points[3])

    if h == 0:
        return 0.0
    return (v1 + v2) / (2.0 * h)

def compute_head_pose(landmarks, image_shape):
    nose_tip = np.array([landmarks.landmark[1].x, landmarks.landmark[1].y, landmarks.landmark[1].z])
    chin = np.array([landmarks.landmark[152].x, landmarks.landmark[152].y, landmarks.landmark[152].z])
    left_eye = np.array([landmarks.landmark[33].x, landmarks.landmark[33].y, landmarks.landmark[33].z])
    right_eye = np.array([landmarks.landmark[263].x, landmarks.landmark[263].y, landmarks.landmark[263].z])

    yaw = np.arctan2(nose_tip[0] - chin[0], nose_tip[2] - chin[2] + 0.001)
    pitch = np.arctan2(nose_tip[1] - chin[1], nose_tip[2] - chin[2] + 0.001)
    roll = np.arctan2(left_eye[1] - right_eye[1], left_eye[0] - right_eye[0] + 0.001)

    return yaw, pitch, roll

def compute_depth_score(landmarks):
    z_values = [landmarks.landmark[i].z for i in KEY_LANDMARKS if i < len(landmarks.landmark)]
    if len(z_values) < 2:
        return 0.0
    return np.std(z_values)

def is_live_face(face_roi_rgb, camera_id="default"):
    """Performs liveness check on face crop. Returns (is_live, depth_score, ear, movement_detected)."""
    if face_roi_rgb is None or face_roi_rgb.size == 0:
        return False, 0.0, 0.0, False

    detector_inst = get_detector(camera_id)

    if detector_inst.is_in_cooldown():
        return True, 0.0, 0.0, False

    h, w = face_roi_rgb.shape[:2]
    if h > FACE_MAX_HEIGHT:
        scale = FACE_MAX_HEIGHT / h
        new_w = int(w * scale)
        face_roi_rgb = cv2.resize(face_roi_rgb, (new_w, FACE_MAX_HEIGHT))

    results = face_mesh.process(face_roi_rgb)
    if not results.multi_face_landmarks:
        detector_inst.reset()
        return False, 0.0, 0.0, False

    landmarks = results.multi_face_landmarks[0]

    # 1. Depth std dev
    depth_score = compute_depth_score(landmarks)

    # 2. Blink check
    left_ear = compute_ear(landmarks, LEFT_EYE_INDICES)
    right_ear = compute_ear(landmarks, RIGHT_EYE_INDICES)
    ear = (left_ear + right_ear) / 2.0
    detector_inst.check_blink(ear)

    # 3. Head movement check
    yaw, pitch, roll = compute_head_pose(landmarks, face_roi_rgb.shape)
    detector_inst.check_movement(yaw, pitch)
    movement_detected = detector_inst.movement_detected

    # 4. Challenge logic
    challenge_passed = (depth_score > DEPTH_THRESHOLD and
                        (detector_inst.blink_detected or movement_detected))

    print(
        f"Liveness Check [{camera_id}] — Depth: {depth_score:.4f} | "
        f"Blink: {detector_inst.blink_detected} | "
        f"Move: {movement_detected} | "
        f"Pass: {challenge_passed}"
    )

    if challenge_passed:
        detector_inst.live_counter += 1
        if detector_inst.live_counter >= LIVE_CONSECUTIVE_REQUIRED:
            detector_inst.confirm_liveness()
            print(f"[LIVENESS - {camera_id}] Confirmed! Face is live.")
            return True, depth_score, ear, movement_detected
    else:
        detector_inst.live_counter = 0

    return False, depth_score, ear, movement_detected


# ==============================================================================
# Client / Backend Integration logic
# ==============================================================================
user_id_to_name = {}

def frame_to_base64(frame):
    """Converts an OpenCV image frame to a base64 string."""
    _, buffer = cv2.imencode('.jpg', frame)
    return base64.b64encode(buffer).decode('utf-8')

def load_known_faces_from_backend(backend_url, secret_key):
    """Fetches face embeddings from the backend and registers local name maps."""
    global user_id_to_name
    cache_path = "backend_embeddings_cache.json"
    known_embeddings = []
    
    headers = {"X-AI-Secret-Key": secret_key}
    
    print("---------------------------------------------")
    print(f"Syncing face embeddings from backend: {backend_url}")
    print("---------------------------------------------")
    
    fetched_successfully = False
    records = []
    
    try:
        response = requests.get(
            f"{backend_url}/api/attendance/active-embeddings",
            headers=headers,
            timeout=10,
            verify=False
        )
        if response.status_code == 200:
            records = response.json()
            with open(cache_path, "w") as f:
                json.dump(records, f, indent=4)
            fetched_successfully = True
            print(f"Successfully synced {len(records)} face embedding(s).")
        else:
            print(f"Backend API returned status code: {response.status_code}")
    except Exception as e:
        print(f"Could not connect to backend to fetch embeddings: {e}")
        
    if not fetched_successfully:
        print("Falling back to local offline embeddings cache...")
        if os.path.exists(cache_path):
            try:
                with open(cache_path, "r") as f:
                    records = json.load(f)
                print(f"Loaded {len(records)} face embedding(s) from offline cache.")
            except Exception as e:
                print(f"Error reading offline cache: {e}")
                records = []
        else:
            print("No offline cache found. Face recognition matches will fail.")
            records = []
            
    for r in records:
        uid = r.get("user_id")
        name = r.get("user_name", "Unknown User")
        emb_json = r.get("embedding_json")
        
        if uid and emb_json:
            user_id_to_name[uid] = name
            known_embeddings.append({
                "user_id": uid,
                "embedding_json": emb_json
            })
            
    print("---------------------------------------------")
    return known_embeddings

def run_live_recognition(stop_event=None):
    global user_id_to_name
    
    # Load known face embeddings from backend
    known_embeddings = load_known_faces_from_backend(BACKEND_URL, SECRET_KEY)
    
    if not known_embeddings:
        print("No face embeddings could be loaded. Attendance system starting in scan-only mode.")

    # Initialize Camera Service (threading auto-configured based on source type)
    print(f"Initializing Camera Service with source: {CAMERA_SOURCE}...")
    camera = CameraService(CAMERA_SOURCE)
    if not camera.open():
        if str(CAMERA_SOURCE).isdigit():
            fallback_found = False
            for idx in range(3):
                if idx == int(CAMERA_SOURCE):
                    continue
                camera = CameraService(idx)
                if camera.open():
                    print(f"Fallback: Opened webcam at index {idx}")
                    fallback_found = True
                    break
            if not fallback_found:
                print("Could not open any webcam index. Exiting.")
                return
        else:
            print(f"Could not open camera stream source {CAMERA_SOURCE}. Exiting.")
            return

    already_marked_present = set()
    unknown_reported = False
    spoof_reported = False
    headers = {"X-AI-Secret-Key": SECRET_KEY}

    print("------------------------------------")
    print("Smart Attendance Client Started")
    print(f"Liveness Detection Enabled | Camera: {CAMERA_SOURCE}")
    if CAMERA_KEY:
        print(f"Assigned Camera Key: {CAMERA_KEY} ({'Employee' if IS_EMPLOYEE_CAMERA else 'Student'} scan mode)")
    else:
        print("WARNING: CAMERA_KEY not set in .env. Attendance will not be recorded on the backend database.")
    print("Press 'q' in the window or Ctrl+C in terminal to stop")
    print("------------------------------------")

    process_this_frame = True
    last_matches = []

    try:
        while True:
            if stop_event is not None and stop_event.is_set():
                print("Stop signal received. Ending recognition loop.")
                break

            ret, frame = camera.read()
            if not ret or frame is None:
                print("Warning: Failed to grab frame. Reconnecting...")
                if not camera.reconnect():
                    print("Reconnection failed. Exiting.")
                    break
                continue

            # Resize for faster MediaPipe liveness check
            small_frame = cv2.resize(frame, (0, 0), fx=0.25, fy=0.25)
            rgb_small_frame = cv2.cvtColor(small_frame, cv2.COLOR_BGR2RGB)

            # Liveness check on small frame
            is_live, depth_score, ear, move = is_live_face(
                rgb_small_frame,
                camera_id="local_cam"
            )

            # Create display frame for visual output
            display_frame = frame.copy()
            h_orig, w_orig = display_frame.shape[:2]

            # Draw status box overlay
            status_color = (0, 255, 0) if is_live else (0, 165, 255)
            status_text = f"Source: {CAMERA_SOURCE} | Liveness: {'LIVE' if is_live else 'CHALLENGING/SPOOF'}"
            cv2.putText(display_frame, status_text, (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.7, status_color, 2)
            cv2.putText(display_frame, f"EAR: {ear:.2f} | Depth Z std-dev: {depth_score:.4f}", (10, 60), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 1)

            # Check if a face is present
            if depth_score > 0:
                if not is_live:
                    if not spoof_reported:
                        print("[WARNING] Spoof detected or challenge failed! Face recognition skipped.")
                        spoof_reported = True
                    
                    # Draw Spoof warning on screen
                    cv2.putText(display_frame, "SPOOF DETECTED / CHALLENGE FAILED", (w_orig // 6, h_orig - 30), 
                                cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 0, 255), 2)
                    last_matches = []
                else:
                    spoof_reported = False
                    
                    # Run face recognition on interval frames to save CPU / bandwidth
                    if process_this_frame:
                        # 1. Post frame to backend webhook for official recording
                        if CAMERA_KEY:
                            endpoint = "employee-scan" if IS_EMPLOYEE_CAMERA else "scan"
                            try:
                                payload = {
                                    "imageBase64": frame_to_base64(frame),
                                    "cameraKey": CAMERA_KEY
                                }
                                resp = requests.post(
                                    f"{BACKEND_URL}/api/attendance/{endpoint}",
                                    json=payload,
                                    headers=headers,
                                    timeout=10,
                                    verify=False
                               )
                                if resp.status_code == 200:
                                    res = resp.json()
                                    rec_cnt = res.get("recorded", 0)
                                    if rec_cnt > 0:
                                        print(f"[BACKEND] Success: {res.get('message', 'Recorded')}")
                                else:
                                    print(f"[BACKEND] Webhook failed: HTTP {resp.status_code} - Response: {resp.text}")
                            except Exception as we:
                                print(f"[BACKEND] Webhook connection error: {we}")

                        # 2. Local console recognition
                        if known_embeddings:
                            try:
                                b64_frame = frame_to_base64(frame)
                                response = requests.post(
                                    f"{API_URL}/api/attend",
                                    json={
                                        "image": b64_frame,
                                        "model_version": "CVFaceRecoV1",
                                        "known_embeddings": known_embeddings
                                    },
                                    headers=headers,
                                    timeout=8
                                )
                                
                                if response.status_code == 200:
                                    result = response.json()
                                    matches = result.get("matches", [])
                                    last_matches = matches
                                    
                                    for match in matches:
                                        uid = match.get("user_id")
                                        conf = match.get("confidence")
                                        
                                        if uid != "Unknown":
                                            name = user_id_to_name.get(uid, uid)
                                            if uid not in already_marked_present:
                                                already_marked_present.add(uid)
                                                print(f"[MATCH] {name} identified (confidence: {conf:.2%})")
                                                
                                    if not matches and result.get("total_faces_detected", 0) > 0:
                                        if not unknown_reported:
                                            print("[RECOGNITION] Unknown person detected.")
                                            unknown_reported = True
                                    else:
                                        unknown_reported = False
                                else:
                                    print(f"[RECOGNITION] Service error: HTTP {response.status_code}")
                            except Exception as e:
                                print(f"[RECOGNITION] Connection error: {e}")

                    # Draw bounding boxes and labels for active matches
                    for match in last_matches:
                        uid = match.get("user_id")
                        conf = match.get("confidence")
                        bbox = match.get("bounding_box")
                        if bbox and len(bbox) == 4:
                            x1, y1, x2, y2 = int(bbox[0]), int(bbox[1]), int(bbox[2]), int(bbox[3])
                            name = user_id_to_name.get(uid, uid)
                            cv2.rectangle(display_frame, (x1, y1), (x2, y2), (0, 255, 0), 2)
                            label = f"{name} ({conf:.2%})"
                            cv2.putText(display_frame, label, (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 0), 2)

            else:
                unknown_reported = False
                spoof_reported = False
                last_matches = []
                reset_detector("local_cam")

            process_this_frame = not process_this_frame

            cv2.imshow('Smart Attendance System (Client Preview)', display_frame)

            if cv2.waitKey(1) & 0xFF == ord('q'):
                break
            
    except KeyboardInterrupt:
        print("\nStopping Smart Attendance Client...")
    finally:
        camera.release()
        cv2.destroyAllWindows()

if __name__ == "__main__":
    if IS_EMPLOYEE_CAMERA:
        import star_stop
        print("Employee camera mode detected. Starting scheduler in star_stop.py...")
        star_stop.run_employee_scheduler()
    else:
        run_live_recognition()