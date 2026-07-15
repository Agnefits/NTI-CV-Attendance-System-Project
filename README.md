# AI-Powered Smart Attendance System

An enterprise-ready **Smart Attendance System** utilizing **Face Detection**, **dlib-based Face Recognition**, and **MediaPipe Liveness Challenge-Response Detection** to automate attendance tracking. The system integrates a high-performance **ASP.NET Core 9 MVC Web Application** backend with a stateless **Python FastAPI Face Recognition Microservice** and an edge-camera client.

🚀 **Live Deployment**: [https://cv-attendance-system.runasp.net](https://cv-attendance-system.runasp.net)

---

## NTI Graduation Project

**National Telecommunication Institute (NTI)**  
**Training Program:** Summer Training Computer Vision  
**Group Code:** S26-B1-Computer V.-G1-E

### Instructors
- Eng. Maryem Ehab
- Eng. Mohamed Khaled
- Eng. Mohamed Samer
- Eng. Omar Gira

### Team Members
- Abdallah Salah Abdallah
- Alaa Ali Khalaf
- Khaled Ashraf Ali
- Malak Mohamed Ali
- Mostafa Mahdy Mohamed

---

## Core System Architecture

```
NTI-CV-Attendance-System-Project/
│
├── Website/
│   └── Attendance System/             # ASP.NET Core 9.0 MVC Web App
│       ├── Controllers/               # Controllers with role-based auth policies
│       ├── ViewModels/                # DTOs & View Models (Log, Student, Dashboard...)
│       ├── Views/                     # Razor views (conditional layout per role)
│       ├── Data/                      # DbContext & seeder scripts
│       ├── Models/                    # EF TPT entities (BaseUser -> Student, Employee)
│       └── Program.cs                 # Main entrypoint & Dependency Injections
│
├── server.py                          # FastAPI stateless recognition service (API-only mode)
├── main.py                            # Edge-camera client with interactive liveness detection
├── anti_spoof.py                      # MediaPipe FaceMesh Liveness detector (blink, move, depth check)
├── camera_service.py                  # Multi-threaded OpenCV webcam frame grabber
├── add_student.py                     # Command-line tool for registering student embeddings
├── requirements.txt                   # Python dependencies (face_recognition, mediapipe, opencv)
└── README.md                          # Project documentation
```

---

## Technology Stack

### 1. Web Application & Database
- **Framework**: ASP.NET Core 9.0 (MVC Pattern)
- **Database Engine**: Microsoft SQL Server
- **ORM**: Entity Framework Core 9.0 (Table-Per-Type TPT Inheritance)
- **Security**: Custom Role-based authorization filters (`[AuthorizedRoles]`) and cookie authentication.

### 2. AI & Face Recognition Engine
- **Framework**: Python 3.12 + FastAPI
- **Libraries**:
  - `face_recognition` (powered by `dlib` HOG & Deep Metric models)
  - `opencv-python` (image and video stream capture)
  - `mediapipe` (liveness analysis & landmark triangulation)
  - `numpy` (cosine distance computations)

---

## Detailed AI & Computer Vision Pipeline

The core intelligence of the system is divided into two modules: face recognition and challenge-based liveness verification.

### 1. Face Recognition Pipeline
1. **Face Detection & Localization**: Frames are read via OpenCV. We use **dlib's HOG + Linear SVM detector** to find face boundaries. For client devices, we apply a `0.25` scaling factor to decrease frame sizes and optimize CPU/GPU execution speeds.
2. **Facial Landmark Estimation**: A dlib **68-point shape predictor** calculates landmark coordinates on eyes, nose, mouth, and eyebrows. The face is horizontally aligned based on the centers of both eyes to neutralize head tilt before verification.
3. **Deep Feature Embedding Generation**: The aligned face image is fed into dlib's **ResNet-34 face recognition model** (pre-trained on 3+ million faces). The model extracts a highly discriminative **128-dimensional vector (embedding)** representing facial structure.
4. **Distance Matching & Verification**: Face embeddings are compared against known user vectors downloaded at client launch using **Cosine Similarity & Euclidean distance**. If the distance is below the threshold (typically `0.60`, which represents a confidence score of `60.0%` or higher), the identity is successfully matched.

### 2. Multi-Challenge Interactive Liveness Detection (Anti-Spoofing)
To block presentation attacks (e.g., holding a printed photo, mobile video, or 3D mask in front of the lens), the local client camera uses **MediaPipe FaceMesh (468 3D landmarks)** to enforce interactive challenges:
- **Eye Aspect Ratio (EAR) Blink Detection**: Computes eyelid distance ratios at landmark points `[33, 160, 158, 133, 153, 144]` (left eye) and `[362, 385, 387, 263, 373, 380]` (right eye). The system requires a natural blink (EAR dipping below `0.20` and returning to normal) to pass.
- **Head Yaw and Pitch Move Challenge**: Tracks 3D rotation angles of the head by calculating nose tip position relative to jaw points. The client displays on-screen prompt challenges (e.g. *Turn Head Left*, *Turn Head Right*, *Look Up*, *Look Down*) and flags a pass only when the target rotation angles are completed.
- **Dynamic Bounding Box Scaling (Depth Check)**: Measures face bounding box surface scaling relative to iris distance points `[468, 473]`. If the distance and area remain entirely flat during face movements, the frame is flagged as a 2D spoof attempt.

---

## Key Features & Implementations

### 1. Real-Time Recognition & Syncing
- **Face Embeddings**: Extracts a 128-dimensional vector from images.
- **Sync Routine**: Edge-camera client pulls known face embeddings dynamically from the ASP.NET database at launch.
- **Dynamic Config**: The ASP.NET client reads model versions (`AIModelVersion`), host URLs (`AIServiceBaseUrl`), and signature keys (`AIModelSecretKey`) dynamically from database settings at runtime, instead of hardcoded `appsettings.json`.

### 2. Multi-Challenge Liveness Detection
To prevent spoofing via photos or video screens, the camera client implements an interactive challenge-response system using **MediaPipe FaceMesh**:
- **Blink Challenge**: Measures Eye Aspect Ratio (EAR) variations.
- **Move Challenge**: Analyzes head orientation changes.
- **Depth Challenge**: Evaluates face bounding box scaling.

### 3. Role-Based Navigation & Dashboards
The user dashboard and sidebar navigation items dynamically adjust based on user claims:
- **Admin**: Has full administrative controls, views global stats, daily trends chart, camera logs, settings, and full system audit logs.
- **Teacher**: Restricted to schedule calendars and student attendance records for classes they teach. Dashboard displays classes taught, student attendance rates, and today's stats.
- **Student**: Resticted to viewing their own schedule (based on Level & Class) and their own attendance rate, class counts, and logs.
- **Employee**: Restricted to checking in/out and viewing their own check-in/out attendance metrics.

### 4. Settings Formatting & Validation
- **Input Formatting**: Formats inputs with strict UI components (checkbox/dropdown select for booleans, time-picker for times, number-pickers for integers/decimals).
- **Time Validation**: Ensures start times (e.g. `EmployeeCheckInStart`) are consistently scheduled before end times (e.g. `EmployeeCheckInEnd`) before saving.

---

## Getting Started

### 1. Setup Python API Microservice
1. Install Python 3.12.
2. Install dependencies:
   ```bash
   pip install -r requirements.txt
   ```
3. Run the API microservice:
   ```bash
   python server.py
   ```
   *Note: `server.py` runs purely in API-only stateless mode, removing local camera hardware/display threads.*

### 2. Run the Edge-Camera Client
Ensure `.env` contains your `FACE_RECO_SERVICE_URL` and `BACKEND_URL`.
```bash
python main.py
```
This runs the local edge-camera stream with interactive liveness detection checks.

### 3. Setup Web Application
1. Configure your database connection string in `Website\Attendance System\appsettings.json`.
2. Apply Entity Framework migrations:
   ```bash
   dotnet ef database update
   ```
3. Run the project:
   ```bash
   dotnet run
   ```

---

## Database Design (TPT Inheritance)
```
             [BaseUser] (Shared attributes: Id, Name, Email, Password, Role)
              /      \
      [Student]      [Employee]
                     (Inherits BaseUser)
```
- **Student**: Linked to `Level` and `Class`.
- **Employee**: Represents teachers and administrative staff. Linked to `Branch`.
- **Lesson**: Maps to a `Teacher` (EmployeeId), `Level`, and `Class`.
- **StudentAttendance & EmployeeAttendance**: Records daily check-in logs, status (Present, Late, Absent), and the AI camera source key.
- **Settings**: Dynamic configuration database store.
- **Logs**: Table auditing operations (Create, Update, Delete) throughout the system.
