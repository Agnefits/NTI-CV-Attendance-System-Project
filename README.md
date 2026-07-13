# Smart Attendance System

An AI-powered **Smart Attendance System** that uses **Face Detection**, **Face Recognition**, and **Liveness Detection** to automate attendance recording for students and employees. The system combines a **.NET Full Stack MVC** web application with a **Python Face Recognition API** served using **FastAPI**.

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

# Technologies

### Backend
- .NET Full Stack MVC
- SQL Server
- Entity Framework

### AI Model
- Python
- Face Recognition
- OpenCV
- FastAPI

### Frontend
- HTML
- CSS
- JavaScript
- Bootstrap

---

# Project Architecture

```
SmartAttendanceSystem/
│
├── SmartAttendance.Web/               # .NET MVC Application
│   ├── Controllers/
│   ├── Models/
│   ├── Views/
│   ├── Services/
│   ├── wwwroot/
│   └── Program.cs
│
├── SmartAttendance.API/               # Python FastAPI
│   ├── app.py
│   ├── routes/
│   ├── services/
│   ├── recognition/
│   ├── models/
│   ├── dataset/
│   ├── embeddings/
│   ├── trained_model/
│   └── requirements.txt
│
├── Database/
│   ├── SmartAttendance.sql
│   └── Seed.sql
│
├── Documentation/
│   ├── Report.pdf
│   └── Diagrams/
│
└── README.md
```

---

# System Workflow

1. User registers in the system.
2. Face images are collected.
3. The AI model extracts facial embeddings.
4. The model recognizes faces in real time.
5. Attendance is automatically recorded.
6. Attendance reports are generated.

---

# Main Features

### Face Detection
Detects human faces from a live camera in real time.

### Face Recognition
Recognizes registered users by comparing facial features with stored facial embeddings.

### Automatic Attendance
Attendance is recorded automatically after successful recognition.

### Real-Time Recognition
Supports continuous recognition using a live camera stream.

### Attendance Logging
Stores attendance records including:

- User Name
- Date
- Time
- Attendance Status

### Registered User Management
Allows administrators to:

- Add Users
- Update Users
- Delete Users

### Unknown Face Detection
Detects faces that are not registered and labels them as **Unknown**.

### Attendance Reports
Generate attendance reports with filtering and export capabilities.

---

# Additional Features

- Automatic system operation during working hours.
- Liveness Detection to distinguish a real person from a printed photo or screen image.
- Supports both:
  - Live Camera
  - Image Upload
- Option to enable or disable camera/image mode using Check/Uncheck.
- Can be used for:
  - Student Attendance
  - Employee Check-In / Check-Out
- Supports recognition while wearing:
  - Face Masks
  - Glasses

---

# AI Development Pipeline

## 1. Data Collection & Preprocessing

- Collect face images
- Face alignment
- Image resizing
- Data cleaning
- Data augmentation
- Train / Validation / Test split

---

## 2. Model Architecture Selection & Hyper Parameters

- Face Recognition model selection
- Hyperparameter tuning
- Optimizer selection
- Learning Rate tuning
- Batch Size selection
- Epoch configuration

---

## 3. Model Training

- Train the face recognition model
- Generate facial embeddings
- Save trained weights
- Monitor training performance

---

## 4. Model Evaluation

Evaluate using:

- Accuracy
- Precision
- Recall
- F1 Score
- Confusion Matrix

Test under different conditions:

- Different lighting
- Different angles
- Masked faces
- Glasses
- Unknown users

---

## 5. Integration (FastAPI)

The trained Python model is exposed through FastAPI.

Workflow:

MVC Application
→ Send Image
→ FastAPI
→ Face Detection
→ Face Recognition
→ Return User Information
→ Save Attendance in SQL Server

---

# Suggested Database Tables

```
Branches
---------
BranchId
BranchName
Description

Roles
------
RoleId
RoleName
Description

Users
------
UserId
FullName
Email
Phone
ImagePath
EmbeddingPath
RoleId
BranchId
IsActive
CreatedAt

Attendance
-----------
AttendanceId
UserId
AttendanceDate
CheckInTime
CheckOutTime
Status

FaceEmbeddings
---------------
EmbeddingId
UserId
EmbeddingFile
CreatedAt

UnknownFaces
-------------
UnknownId
ImagePath
DetectedAt
CameraName

SystemSettings
---------------
SettingId
SettingName
SettingValue

Logs
-----
LogId
UserId
Action
CreatedAt
```

---

# Future Improvements

- Multi-camera support
- Mobile application
- Cloud deployment
- Email notifications
- QR code backup attendance
- Face recognition optimization
- Analytics dashboard

---

# Project Objectives

- Eliminate manual attendance.
- Increase attendance accuracy.
- Prevent attendance fraud.
- Improve security using AI.
- Provide fast and reliable attendance management.
- Support educational institutions and business organizations.

---

# License

This project is developed for educational purposes.
