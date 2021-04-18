import time
import math
import socket
import numpy as np
import cv2
import EmotionRecognition as ER
import os

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

# Load classifiers
face_filename = "data\haarcascade_frontalface_default.xml"
eyes_filename = "data\haarcascade_eye_tree_eyeglasses.xml"
cv2_base_dir = os.path.dirname(os.path.abspath(cv2.__file__))
f_casc_name = os.path.join(cv2_base_dir, face_filename)
e_casc_name = os.path.join(cv2_base_dir, eyes_filename)
f_cascade = cv2.CascadeClassifier()
e_cascade = cv2.CascadeClassifier()

if (not f_cascade.load(cv2.samples.findFile(f_casc_name))) or (not e_cascade.load(cv2.samples.findFile(e_casc_name))):
    print("error loading cascade classifier")

while capture.isOpened():
    ret, frame = capture.read()

    ER.annotateFrame(frame, f_cascade, e_cascade)

    faceImg = ER.extractFaceImg(frame)
    if faceImg != None:
        currentEmotion = ER.getEmotionOfFace(faceImg)

        if (time.time() - lastPing > pingInterval):
            lastPing = time.time()
            sock.sendto(currentEmotion.encode(), (UDP_IP, UDP_PORT))
            print("sending [" + currentEmotion + "] to socket")

    # close if 'q' is pressed
    if cv2.waitKey(1) == ord('q'):
        break

capture.release()
cv2.destroyAllWindows()