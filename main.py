import os
import face_recognition
import cv2
import numpy as np
from datetime import datetime

def load_known_faces(known_faces_dir):
    known_face_encodings = []
    known_face_names = []

    print("Loading and encoding known faces...")
    
    # Create the folder if it doesn't exist yet
    if not os.path.exists(known_faces_dir):
        print(f"Error: Directory '{known_faces_dir}' not found. Creating it now...")
        os.makedirs(known_faces_dir)
        print("Please place student photos in 'known_faces' and run again.")
        return [], []

    # Loop through all files inside the known faces directory
    for filename in os.listdir(known_faces_dir):
        # Only process files with valid image extensions
        if filename.lower().endswith(('.png', '.jpg', '.jpeg', '.webp')):
            image_path = os.path.join(known_faces_dir, filename)
            
            # Extract the filename without its extension to use as the student's name
            name = os.path.splitext(filename)[0]
            
            try:
                # Load the image and extract its 128-dimension face encoding signature
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

    # Use an in-memory set to track who is marked present (automatically blocks duplicates)
    already_marked_present = set()

    # Initialize webcam (0 = primary camera)
    print("Starting webcam... Press 'q' to quit.")
    video_capture = cv2.VideoCapture(0)
    
    # added this to check a frame and skip the nest one (cuts CPU load in half)
    process_this_frame = True

    while True:
        # Grab a single frame from the camera feed
        ret, frame = video_capture.read()
        if not ret:
            print("Failed to grab frame from webcam.")
            break

        # Process this frame, skip the next
        if process_this_frame:
            # Resize frame to 1/4 size to speed up face recognition processing
            small_frame = cv2.resize(frame, (0, 0), fx=0.25, fy=0.25)
            
            # Convert colors from BGR (OpenCV standard) to RGB (face_recognition standard)
            rgb_small_frame = cv2.cvtColor(small_frame, cv2.COLOR_BGR2RGB)
            
            # Locate all faces and calculate their unique encodings in this frame
            face_locations = face_recognition.face_locations(rgb_small_frame)
            face_encodings = face_recognition.face_encodings(rgb_small_frame, face_locations)

            face_names = []
            for face_encoding in face_encodings:
                # Check if the live face matches any stored reference signatures
                matches = face_recognition.compare_faces(known_face_encodings, face_encoding, tolerance=0.55)
                name = "Unknown"

                # Calculate similarity distances (lowest distance = best match)
                face_distances = face_recognition.face_distance(known_face_encodings, face_encoding)
                if len(face_distances) > 0:
                    best_match_index = np.argmin(face_distances)
                    if matches[best_match_index]:
                        name = known_face_names[best_match_index]
                        
                        # If a student is matched and has NOT been printed during this run
                        if name not in already_marked_present:
                            already_marked_present.add(name) 
                            current_time = datetime.now().strftime("%H:%M:%S")
                            print(f"{name} is present! (Detected at {current_time})")

                face_names.append(name)

        # Flip the toggle so the next frame skips CPU-heavy calculations
        process_this_frame = not process_this_frame
        
        cv2.imshow('Face Recognition Attendance System (Test Mode)', frame)

        # Break loop if 'q' is pressed on keyboard
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    
    video_capture.release()
    cv2.destroyAllWindows()

if __name__ == "__main__":
    run_live_recognition()