import cv2
import mediapipe as mp
import numpy as np
import time

# =============================================
# Configuration
# =============================================
MAX_FACES = 1
MIN_DETECTION_CONFIDENCE = 0.5
FACE_MAX_HEIGHT = 200
DEPTH_THRESHOLD = 0.025         # Adjusted after testing (was 0.03)
BLINK_THRESHOLD = 0.20          # Lowered for better detection (was 0.25)
CONSECUTIVE_FRAMES = 2
HEAD_POSE_THRESHOLD = 0.15      # Increased to avoid false movement (was 0.08)
LIVE_CONSECUTIVE_REQUIRED = 2   # Reduced to 2 for faster confirmation
LIVENESS_COOLDOWN = 10          # Seconds to keep live status without re-challenge

# =============================================
# MediaPipe Face Mesh
# =============================================
mp_face_mesh = mp.solutions.face_mesh
face_mesh = mp_face_mesh.FaceMesh(
    static_image_mode=False,
    max_num_faces=MAX_FACES,
    refine_landmarks=True,
    min_detection_confidence=MIN_DETECTION_CONFIDENCE
)

LEFT_EYE_INDICES = [33, 160, 158, 133, 153, 144]
RIGHT_EYE_INDICES = [362, 385, 387, 263, 373, 380]
KEY_LANDMARKS = [1, 10, 152, 234, 454]


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
        
        # NEW: Cooldown mechanism
        self.liveness_confirmed = False
        self.liveness_confirm_time = 0
        self.last_challenge_issued = None  # Store last challenge type for debugging

    def reset(self):
        self.blink_counter = 0
        self.blink_detected = False
        self.movement_detected = False
        self.prev_yaw = 0.0
        self.prev_pitch = 0.0
        self.live_counter = 0
        # Do NOT reset liveness_confirmed here, we want it to persist until cooldown expires

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
        """Mark liveness as confirmed and set cooldown timer."""
        self.liveness_confirmed = True
        self.liveness_confirm_time = time.time()
        # Reset blink and movement flags to avoid immediate re-trigger
        self.blink_detected = False
        self.movement_detected = False
        self.live_counter = 0

    def is_in_cooldown(self):
        """Check if still within cooldown period after liveness confirmation."""
        if not self.liveness_confirmed:
            return False
        if time.time() - self.liveness_confirm_time < LIVENESS_COOLDOWN:
            return True
        else:
            # Cooldown expired, reset flag
            self.liveness_confirmed = False
            return False


# Global dict of detector instances indexed by camera_id/session_id
detectors = {}

def get_detector(camera_id="default"):
    global detectors
    if camera_id not in detectors:
        detectors[camera_id] = LivenessDetector()
    return detectors[camera_id]

def reset_detector(camera_id="default"):
    get_detector(camera_id).reset()

# Kept for backward compatibility
detector = get_detector("default")


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
    ear = (v1 + v2) / (2.0 * h)
    return ear


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


def is_live_face(face_roi_rgb, draw_mesh=False, camera_id="default"):
    """
    Enhanced liveness detection with cooldown.
    Returns: (is_live, depth_score, proc_time, annotated_image, ear, movement_detected)
    """
    start_time = time.time()
    annotated_image = None
    ear = 0.0
    depth_score = 0.0
    movement_detected = False

    if face_roi_rgb is None or face_roi_rgb.size == 0:
        return False, 0.0, 0.0, None, 0.0, False

    detector_inst = get_detector(camera_id)

    # --- Cooldown check (quick return if already confirmed) ---
    if detector_inst.is_in_cooldown():
        return True, 0.0, 0.0, None, 0.0, False

    # --- Process frame for liveness ---
    h, w = face_roi_rgb.shape[:2]
    if h > FACE_MAX_HEIGHT:
        scale = FACE_MAX_HEIGHT / h
        new_w = int(w * scale)
        face_roi_rgb = cv2.resize(face_roi_rgb, (new_w, FACE_MAX_HEIGHT))

    results = face_mesh.process(face_roi_rgb)
    if not results.multi_face_landmarks:
        detector_inst.reset()
        return False, 0.0, time.time() - start_time, None, 0.0, False

    landmarks = results.multi_face_landmarks[0]

    # 1. Depth score
    depth_score = compute_depth_score(landmarks)

    # 2. Blink detection
    left_ear = compute_ear(landmarks, LEFT_EYE_INDICES)
    right_ear = compute_ear(landmarks, RIGHT_EYE_INDICES)
    ear = (left_ear + right_ear) / 2.0
    detector_inst.check_blink(ear)

    # 3. Head movement
    yaw, pitch, roll = compute_head_pose(landmarks, face_roi_rgb.shape)
    detector_inst.check_movement(yaw, pitch)
    movement_detected = detector_inst.movement_detected

    # 4. Decision: require depth + (blink or movement)
    challenge_passed = (depth_score > DEPTH_THRESHOLD and
                        (detector_inst.blink_detected or movement_detected))

    # Debug print
    print(
        f"Depth: {depth_score:.4f} | "
        f"Blink: {detector_inst.blink_detected} | "
        f"Move: {movement_detected} | "
        f"ChallengePass: {challenge_passed}"
    )

    if challenge_passed:
        detector_inst.live_counter += 1
        if detector_inst.live_counter >= LIVE_CONSECUTIVE_REQUIRED:
            # Confirm liveness and start cooldown
            detector_inst.confirm_liveness()
            print(f"[LIVENESS - {camera_id}] Confirmed! Face is live (cooldown started).")
            return True, depth_score, time.time() - start_time, annotated_image, ear, movement_detected
    else:
        detector_inst.live_counter = 0
        # If we had a false start, reset the flags to avoid re-triggering
        # but keep tracking for next frames

    # If we reach here, not live yet
    return False, depth_score, time.time() - start_time, annotated_image, ear, movement_detected