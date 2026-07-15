import cv2
import threading
import time
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("CameraService")

class CameraService:
    """
    A service that wraps OpenCV VideoCapture, supporting:
    - Webcams (via integer index, e.g. 0, 1)
    - IP Cameras (via RTSP/RTMP URL string)
    - Camera streams via URL (via HTTP/HTTPS stream string)
    It implements a threaded reader to prevent buffer lag, which is critical for network streams.
    """
    def __init__(self, source, width=None, height=None, threaded=None):
        self.raw_source = source
        self.source = source
        self.width = width
        self.height = height
        
        self.cap = None
        self.is_webcam = False
        
        self.frame = None
        self.ret = False
        self.running = False
        self.thread = None
        self.lock = threading.Lock()
        
        self._parse_source()
        
        # Auto-configure threading if not explicitly provided:
        # Disabled for local webcams (ensures single-thread stability), enabled for streams (avoids buffer lag).
        if threaded is None:
            self.threaded = not self.is_webcam
        else:
            self.threaded = threaded

    def _parse_source(self):
        """Standardizes the source parameter to float/int or string."""
        if isinstance(self.source, str):
            # If the string contains only digits, it is a webcam index
            if self.source.isdigit():
                self.source = int(self.source)
                self.is_webcam = True
            elif self.source.startswith(("rtsp://", "rtmp://", "http://", "https://")):
                self.is_webcam = False
            else:
                # Try parsing as an integer, otherwise assume it's a file path or device name string
                try:
                    self.source = int(self.source)
                    self.is_webcam = True
                except ValueError:
                    self.is_webcam = False
        elif isinstance(self.source, int):
            self.is_webcam = True
        else:
            self.is_webcam = False

    def open(self):
        """Opens the camera source and starts the background acquisition thread if threaded."""
        logger.info(f"Opening camera source: {self.source} (is_webcam={self.is_webcam})")
        
        if self.is_webcam:
            # CAP_DSHOW is faster and prevents startup delay on Windows webcams
            self.cap = cv2.VideoCapture(self.source, cv2.CAP_DSHOW)
        else:
            # Network stream or URL cam
            self.cap = cv2.VideoCapture(self.source)
            
        if not self.cap.isOpened() and self.is_webcam:
            logger.warning("Failed to open webcam with CAP_DSHOW, trying default backend...")
            self.cap = cv2.VideoCapture(self.source)
            
        if self.cap.isOpened():
            if self.width:
                self.cap.set(cv2.CAP_PROP_FRAME_WIDTH, self.width)
            if self.height:
                self.cap.set(cv2.CAP_PROP_FRAME_HEIGHT, self.height)
                
            logger.info("Camera source opened successfully.")
            
            if self.threaded:
                self.running = True
                self.ret = False
                self.frame = None
                self.thread = threading.Thread(target=self._update_loop, daemon=True)
                self.thread.start()
                
            return True
        else:
            logger.error(f"Failed to open camera source: {self.source}")
            return False

    def _update_loop(self):
        """Background thread that continuously grabs the latest frame from the capture device."""
        while self.running:
            if self.cap is not None and self.cap.isOpened():
                ret, frame = self.cap.read()
                with self.lock:
                    self.ret = ret
                    if ret:
                        self.frame = frame
            time.sleep(0.005)  # Yield CPU execution

    def read(self):
        """Returns the latest grabbed frame. Returns (ret, frame)."""
        if self.threaded:
            with self.lock:
                return self.ret, self.frame
        else:
            if self.cap is not None and self.cap.isOpened():
                return self.cap.read()
            return False, None

    def reconnect(self, max_attempts=5, delay=2):
        """Attempts to reconnect to the camera source with retries."""
        logger.info("Reconnection requested.")
        self.release()
        for attempt in range(1, max_attempts + 1):
            logger.info(f"Reconnection attempt {attempt}/{max_attempts}...")
            if self.open():
                logger.info("Reconnected successfully.")
                return True
            time.sleep(delay)
        logger.error("Could not reconnect after maximum attempts.")
        return False

    def release(self):
        """Releases the camera resources and stops the thread."""
        self.running = False
        if self.thread is not None:
            self.thread.join(timeout=1.0)
            self.thread = None
        if self.cap is not None:
            self.cap.release()
            self.cap = None
        logger.info("Camera resources released.")
        
    def is_opened(self):
        """Returns true if the capture is initialized and running."""
        if self.threaded:
            return self.running and self.cap is not None and self.cap.isOpened()
        return self.cap is not None and self.cap.isOpened()
