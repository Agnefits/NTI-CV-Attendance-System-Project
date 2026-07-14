

import os
import time
import threading
from datetime import datetime

import cv2
import numpy as np
import face_recognition
import schedule

# ----------------------------
# CONFIG
# ----------------------------
START_TIME = "19:44"
STOP_TIME = "19:45"
KNOWN_FACES_DIR = "known_faces"
SHOW_WINDOW = True  # خليها False لو الفيديو هيتعرض جوّه الـ GUI بدل نافذة منفصلة

# ----------------------------
# Internal state
# ----------------------------
_model_thread = None
_stop_event = threading.Event()



# ============================================================
# 1) FACE RECOGNITION
# ============================================================
def load_known_faces(known_faces_dir):
    known_face_encodings = []
    known_face_names = []

    print("Loading and encoding known faces...")

    if not os.path.exists(known_faces_dir):
        print(f"Error: Directory '{known_faces_dir}' not found. Creating it now...")
        os.makedirs(known_faces_dir)
        print("Please place student photos in 'known_faces' and run again.")
        return [], []

    for filename in os.listdir(known_faces_dir):
        if filename.lower().endswith(('.png', '.jpg', '.jpeg', '.webp')):
            image_path = os.path.join(known_faces_dir, filename)
            name = os.path.splitext(filename)[0]

            try:
                image = face_recognition.load_image_file(image_path)
                encodings = face_recognition.face_encodings(image)

                if len(encodings) > 0:
                    known_face_encodings.append(encodings[0])
                    known_face_names.append(name)
                    print(f"Successfully loaded student: {name}")
                else:
                    print(f"Skipping {filename}: No face detected.")
            except Exception as e:
                print(f"Could not process {filename}: {e}")

    return known_face_encodings, known_face_names


def run_live_recognition(stop_event=None, show_window=True, known_faces_dir="known_faces"):
    """
    stop_event: threading.Event() يوقف اللووب من بره (من الـ scheduler مثلاً).
    show_window: False لو مش عايز نافذة OpenCV منفصلة تفتح.
    """
    known_face_encodings, known_face_names = load_known_faces(known_faces_dir)

    if not known_face_encodings:
        print("No student faces loaded. Exiting...")
        return

    already_marked_present = set()

    print("Starting webcam...")
    video_capture = cv2.VideoCapture(0)

    process_this_frame = True

    while True:
        if stop_event is not None and stop_event.is_set():
            print("Stop signal received. Ending recognition loop.")
            break

        ret, frame = video_capture.read()
        if not ret:
            print("Failed to grab frame from webcam.")
            break

        if process_this_frame:
            small_frame = cv2.resize(frame, (0, 0), fx=0.25, fy=0.25)
            rgb_small_frame = cv2.cvtColor(small_frame, cv2.COLOR_BGR2RGB)

            face_locations = face_recognition.face_locations(rgb_small_frame)
            face_encodings = face_recognition.face_encodings(rgb_small_frame, face_locations)

            face_names = []
            for face_encoding in face_encodings:
                matches = face_recognition.compare_faces(known_face_encodings, face_encoding, tolerance=0.55)
                name = "Unknown"

                face_distances = face_recognition.face_distance(known_face_encodings, face_encoding)
                if len(face_distances) > 0:
                    best_match_index = np.argmin(face_distances)
                    if matches[best_match_index]:
                        name = known_face_names[best_match_index]

                        if name not in already_marked_present:
                            already_marked_present.add(name)
                            current_time = datetime.now().strftime("%H:%M:%S")
                            print(f"{name} is present! (Detected at {current_time})")

                face_names.append(name)

        process_this_frame = not process_this_frame

        if show_window:
            cv2.imshow('Face Recognition Attendance System (Test Mode)', frame)
            if cv2.waitKey(1) & 0xFF == ord('q'):
                break

    video_capture.release()
    if show_window:
        cv2.destroyAllWindows()


# ============================================================
# 2) SCHEDULER (نقطة 4: بداية ونهاية التشغيل التلقائي)
# ============================================================
def run_attendance_model():
    print(f"[{datetime.now()}] Attendance model STARTED.")
    run_live_recognition(
        stop_event=_stop_event,
        show_window=SHOW_WINDOW,
        known_faces_dir=KNOWN_FACES_DIR,
    )
    print(f"[{datetime.now()}] Attendance model loop exited.")


def start_attendance_model():
    global _model_thread
    if _model_thread is not None and _model_thread.is_alive():
        print("Model already running.")
        return
    _stop_event.clear()
    _model_thread = threading.Thread(target=run_attendance_model, daemon=True)
    _model_thread.start()


def stop_attendance_model():
    global _model_thread
    if _model_thread is None or not _model_thread.is_alive():
        print("Model is not running.")
        return
    _stop_event.set()
    _model_thread.join(timeout=5)
    print(f"[{datetime.now()}] Attendance model STOPPED.")


def run_scheduler(start_time=START_TIME, stop_time=STOP_TIME):
    schedule.every().day.at(start_time).do(start_attendance_model)
    schedule.every().day.at(stop_time).do(stop_attendance_model)

    print(f"Scheduler running. Model will start at {start_time} and stop at {stop_time} daily.")
    while True:
        schedule.run_pending()
        time.sleep(5)


def start_scheduler_background(start_time=START_TIME, stop_time=STOP_TIME):
    """
    نقطة الاستدعاء الوحيدة المطلوبة في main.py.
    بتشغّل الجدولة في thread منفصل عشان متعملش block للـ GUI.
    """
    scheduler_thread = threading.Thread(
        target=run_scheduler,
        args=(start_time, stop_time),
        daemon=True,
    )
    scheduler_thread.start()
    return scheduler_thread


if __name__ == "__main__":
    run_scheduler()