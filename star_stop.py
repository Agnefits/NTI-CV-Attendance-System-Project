import os
import time
import threading
import requests
import urllib3
import schedule
from datetime import datetime
from dotenv import load_dotenv

# Suppress self-signed SSL certificate warnings for local development
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# Load configuration
load_dotenv()

SECRET_KEY = os.getenv("SECRET_KEY", "change-me-in-production-key-1234")
BACKEND_URL = os.getenv("BACKEND_URL", "https://localhost:7177")
IS_EMPLOYEE_CAMERA = os.getenv("IS_EMPLOYEE_CAMERA", "False").lower() in ("true", "1", "yes")

# Internal state
_model_thread = None
_stop_event = threading.Event()

def fetch_employee_windows():
    """Fetches employee check-in/out windows from the ASP.NET Core backend settings."""
    headers = {"X-AI-Secret-Key": SECRET_KEY}
    url = f"{BACKEND_URL}/api/attendance/employee-windows"
    
    print("---------------------------------------------")
    print(f"Fetching employee attendance windows from: {url}")
    print("---------------------------------------------")
    
    try:
        resp = requests.get(url, headers=headers, timeout=10, verify=False)
        if resp.status_code == 200:
            data = resp.json()
            print(f"Successfully retrieved windows: {data}")
            return data
        else:
            print(f"Backend API returned status code {resp.status_code}. Using defaults.")
    except Exception as e:
        print(f"Could not connect to backend to fetch windows: {e}. Using defaults.")
        
    return {
        "checkInStart": "07:30",
        "checkInEnd": "09:30",
        "checkOutStart": "15:00",
        "checkOutEnd": "19:00"
    }

def is_time_between(start_str, end_str):
    """Checks if current time is within [start_str, end_str] (formatted as HH:MM)."""
    now = datetime.now().time()
    try:
        start = datetime.strptime(start_str, "%H:%M").time()
        end = datetime.strptime(end_str, "%H:%M").time()
        if start <= end:
            return start <= now <= end
        else:  # Handles window wrapping past midnight
            return now >= start or now <= end
    except Exception as e:
        print(f"Error parsing window times ({start_str} - {end_str}): {e}")
        return False

def run_attendance_model():
    """Starts the official recognition engine in the current thread."""
    import main
    print(f"[{datetime.now()}] Scheduled Attendance Model STARTED.")
    try:
        main.run_live_recognition(stop_event=_stop_event)
    except Exception as e:
        print(f"Error running recognition model: {e}")
    print(f"[{datetime.now()}] Scheduled Attendance Model STOPPED.")

def start_attendance_model():
    """Spawns the attendance recognition loop thread if not already running."""
    global _model_thread
    if _model_thread is not None and _model_thread.is_alive():
        print(f"[{datetime.now()}] Attendance model is already running. Skipping start command.")
        return
    _stop_event.clear()
    _model_thread = threading.Thread(target=run_attendance_model, daemon=True)
    _model_thread.start()

def stop_attendance_model():
    """Signals the attendance recognition loop to stop and joins the thread."""
    global _model_thread
    if _model_thread is None or not _model_thread.is_alive():
        print(f"[{datetime.now()}] Attendance model is not running. Skipping stop command.")
        return
    print(f"[{datetime.now()}] Stopping Attendance model...")
    _stop_event.set()
    _model_thread.join(timeout=5)
    _model_thread = None

def run_employee_scheduler():
    """
    Main entry point for employee camera scheduler:
    1. Fetches windows from backend.
    2. Runs checking immediately if launched within a window.
    3. Configures daily cron schedules to start/stop automatically.
    """
    windows = fetch_employee_windows()
    
    ci_start = windows.get("checkInStart", "07:30")
    ci_end = windows.get("checkInEnd", "09:30")
    co_start = windows.get("checkOutStart", "15:00")
    co_end = windows.get("checkOutEnd", "19:00")
    
    # 1. Startup check: If script is launched during check-in or check-out times, start immediately.
    if is_time_between(ci_start, ci_end) or is_time_between(co_start, co_end):
        print(f"[{datetime.now()}] Launch time is within active attendance hours. Starting model.")
        start_attendance_model()
    else:
        print(f"[{datetime.now()}] Launch time is outside active attendance hours. Waiting for scheduled window.")

    # 2. Schedule daily check-in start and end times
    schedule.every().day.at(ci_start).do(start_attendance_model)
    schedule.every().day.at(ci_end).do(stop_attendance_model)
    
    # 3. Schedule daily check-out start and end times
    schedule.every().day.at(co_start).do(start_attendance_model)
    schedule.every().day.at(co_end).do(stop_attendance_model)

    print("----------------------------------------------------------------------")
    print(f"Scheduler active: Check-in ({ci_start} - {ci_end}) | Check-out ({co_start} - {co_end})")
    print("Keep this window open to process scans automatically.")
    print("----------------------------------------------------------------------")
    
    try:
        while True:
            schedule.run_pending()
            time.sleep(1)
    except KeyboardInterrupt:
        print("\nStopping scheduler and release resources...")
        stop_attendance_model()

if __name__ == "__main__":
    run_employee_scheduler()