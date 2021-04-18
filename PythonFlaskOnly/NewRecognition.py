import time
import math
import socket
import numpy as np
import cv2

UDP_IP = "127.0.0.1"
UDP_PORT = 5065

lastPing = time.time()
pingInterval = 2
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

try:
    default = 0 # cam source
    capture = cv2.VideoCapture(default)
except:
    print("error finding camera")

while capture.isOpened():
    ret, frame = capture.read()
    cv2.imshow("Test img", frame)

    if (time.time() - lastPing > pingInterval):
        lastPing = time.time()
        sock.sendto( ("abcdefg~").encode(), (UDP_IP, UDP_PORT) )
        print("sending to socket")

    # close if 'q' is pressed
    if cv2.waitKey(1) == ord('q'):
        break

capture.release()
cv2.destroyAllWindows()