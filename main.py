import os
import face_recognition
import cv2
import numpy as np
from datetime import datetime
import anti_spoof

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


def run_live_recognition():
    KNOWN_FACES_DIR = "known_faces"
    known_face_encodings, known_face_names = load_known_faces(KNOWN_FACES_DIR)
    
    if not known_face_encodings:
        print("No student faces loaded. Exiting...")
        return

    already_marked_present = set()

    print("------------------------------------")
    print("Attendance System Started")
    print("Liveness Detection Enabled")
    print("Press 'q' to quit")
    print("------------------------------------")
    
    # Try multiple camera indices
    camera_index = 0
    video_capture = None
    for idx in range(3):
        cap = cv2.VideoCapture(idx, cv2.CAP_DSHOW)
        if cap.isOpened():
            video_capture = cap
            camera_index = idx
            print(f"Camera opened successfully at index {idx}")
            break
        else:
            cap.release()
    
    if video_capture is None:
        print("Could not open any camera. Please check your camera connection.")
        return

    process_this_frame = True
    unknown_reported = False
    spoof_reported = False

    while True:
        ret, frame = video_capture.read()
        if not ret:
            print("Failed to grab frame from webcam. Trying to reconnect...")
            video_capture.release()
            video_capture = cv2.VideoCapture(camera_index, cv2.CAP_DSHOW)
            if not video_capture.isOpened():
                print("Reconnection failed. Exiting.")
                break
            continue

        if process_this_frame:
            small_frame = cv2.resize(frame, (0, 0), fx=0.25, fy=0.25)
            rgb_small_frame = cv2.cvtColor(small_frame, cv2.COLOR_BGR2RGB)
            
            face_locations = face_recognition.face_locations(rgb_small_frame)
            
            if len(face_locations) == 0:
                unknown_reported = False
                spoof_reported = False
                # Reset detector when no face
                anti_spoof.detector.reset()
                # Note: liveness_confirmed is NOT reset here, cooldown continues
            else:
                top, right, bottom, left = face_locations[0]
                face_roi = rgb_small_frame[top:bottom, left:right]
                
                is_live, depth_score, proc_time, annotated, ear, move = anti_spoof.is_live_face(
                    face_roi,
                    draw_mesh=False
                )
                
                if not is_live:
                    if not spoof_reported:
                        print("[WARNING] Spoof detected! Face recognition skipped.")
                        spoof_reported = True
                else:
                    spoof_reported = False
                    
                    # Encode and recognize
                    face_encodings = face_recognition.face_encodings(
                        rgb_small_frame,
                        [(top, right, bottom, left)]
                    )
                    
                    if len(face_encodings) > 0:
                        face_encoding = face_encodings[0]
                        
                        matches = face_recognition.compare_faces(
                            known_face_encodings,
                            face_encoding,
                            tolerance=0.55
                        )
                        
                        name = "Unknown"
                        face_distances = face_recognition.face_distance(
                            known_face_encodings,
                            face_encoding
                        )
                        
                        if len(face_distances) > 0:
                            best_match_index = np.argmin(face_distances)
                            if matches[best_match_index]:
                                name = known_face_names[best_match_index]
                                if name not in already_marked_present:
                                    already_marked_present.add(name)
                                    current_time = datetime.now().strftime("%H:%M:%S")
                                    print(f"{name} is present! (Detected at {current_time})")
                        
                        if name == "Unknown":
                            if not unknown_reported:
                                print("Unknown person detected.")
                                unknown_reported = True
                        else:
                            unknown_reported = False

        process_this_frame = not process_this_frame
        
        cv2.imshow('Face Recognition Attendance System (Test Mode)', frame)

        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    video_capture.release()
    cv2.destroyAllWindows()


if __name__ == "__main__":
    run_live_recognition()