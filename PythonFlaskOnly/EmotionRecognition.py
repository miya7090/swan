import math
import numpy as np
import cv2

def annotateFrame(frame, face_cascade, eyes_cascade):
    # sample code from https://docs.opencv.org/4.5.2/db/d28/tutorial_cascade_classifier.html

    frame_gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    frame_gray = cv2.equalizeHist(frame_gray)
    #-- Detect faces
    faces = face_cascade.detectMultiScale(frame_gray)
    for (x,y,w,h) in faces:
        center = (x + w//2, y + h//2)
        frame = cv2.ellipse(frame, center, (w//2, h//2), 0, 0, 360, (255, 0, 255), 4)
        faceROI = frame_gray[y:y+h,x:x+w]
        #-- In each face, detect eyes
        eyes = eyes_cascade.detectMultiScale(faceROI)
        for (x2,y2,w2,h2) in eyes:
            eye_center = (x + x2 + w2//2, y + y2 + h2//2)
            radius = int(round((w2 + h2)*0.25))
            frame = cv2.circle(frame, eye_center, radius, (255, 0, 0 ), 4)
    cv2.imshow("Face detected", frame)

def extractFaceImg(frame):
    # Extracts image of face in frame (returns None if no face)
    # TODO/sorta combine with above
    pass

def getEmotionOfFace(img):
    # Returns a string denoting most likely emotion in face image
    # TODO
    pass