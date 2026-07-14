import os
import cv2

KNOWN_FACES_DIR = "known_faces"

def add_new_student():

    if not os.path.exists(KNOWN_FACES_DIR):
        os.makedirs(KNOWN_FACES_DIR)

    
    student_name = input("Enter student name: ").strip()

    if student_name == "":
        print("Student name cannot be empty!")
        return

   
    cap = cv2.VideoCapture(0)
    

    print("Press 's' to save the photo")
    print("Press 'q' to cancel")

    while True:

        ret, frame = cap.read()

        if not ret:
            print("Failed to access camera.")
            break

        cv2.imshow("Add New Student", frame)

        key = cv2.waitKey(1)

       
        if key == ord('s'):

            image_path = os.path.join(
                KNOWN_FACES_DIR,
                f"{student_name}.jpg"
            )

            cv2.imwrite(image_path, frame)

            print(f"{student_name} added successfully!")

            break

      
        elif key == ord('q'):
            print("Cancelled.")
            break

    cap.release()
    cv2.destroyAllWindows()


if __name__ == "__main__":
    add_new_student()