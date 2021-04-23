import numpy as np
import cv2
import os

# Using FER model for emotion recognition:
# https://bleedai.com/facial-expression-recognition-emotion-recognition-with-opencv/
# https://arxiv.org/pdf/1608.01041.pdf and https://github.com/ebarsoum/FERPlus


face_filename = "data\haarcascade_frontalface_default.xml"
eyes_filename = "data\haarcascade_eye_tree_eyeglasses.xml"
model = 'emotion-ferplus-8.onnx'
emotions = ['Neutral', 'Happy', 'Surprise', 'Sad', 'Anger', 'Disgust', 'Fear', 'Contempt']

def loadClassifiers():
    # Load classifiers for face tracking
    cv2_base_dir = os.path.dirname(os.path.abspath(cv2.__file__))
    f_casc_name = os.path.join(cv2_base_dir, face_filename)
    e_casc_name = os.path.join(cv2_base_dir, eyes_filename)
    f_cascade = cv2.CascadeClassifier()
    e_cascade = cv2.CascadeClassifier()

    if (not f_cascade.load(cv2.samples.findFile(f_casc_name))) or (not e_cascade.load(cv2.samples.findFile(e_casc_name))):
        print("error loading cascade classifier")

    # Load classifier for emotion classification
    emotion_net = cv2.dnn.readNetFromONNX(model)

    return f_cascade, e_cascade, emotion_net

def extractFaceImg(frame, face_cascade, eyes_cascade, pad=5):
    '''Returns original image of face in frame, or None if no face.
    Parameters: image frame, two classifiers,
    pad (the padding around face to include when extracting face)'''

    # sample code from https://docs.opencv.org/4.5.2/db/d28/tutorial_cascade_classifier.html
    frame_gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    frame_gray = cv2.equalizeHist(frame_gray)
    
    # detect first face
    faces = face_cascade.detectMultiScale(frame_gray)
    if len(faces) > 0:
        x,y,w,h = faces[0]
        faceROI = frame_gray[y-pad:y+h+pad,x-pad:x+w+pad]
        return faceROI, (x,y,w,h)
    else:
        return None, None # no face detected

def annotateFace(frame, coords, circleColor=(255,0,255)):
    '''Adds circles onto face+eyes in frame at coords'''
    if coords is not None:
        x,y,w,h = coords
        center = (x + w//2, y + h//2)
        frame = cv2.ellipse(frame, center, (w//2, h//2), 0, 0, 360, circleColor, 4)
    return frame

def getEmotionOfFace(img, emotion_net):
    '''Returns a string denoting most likely emotion in face image'''

    # resize and reshape
    resized_face = cv2.resize(img, (64, 64))
    processed_face = resized_face.reshape(1,1,64,64)
    # feed into network
    emotion_net.setInput(processed_face)
    predicted = emotion_net.forward()

    # compute softmax values for each sets of scores
    expanded = np.exp(predicted - np.max(predicted))
    probabilities = np.squeeze(expanded/expanded.sum())
    
    return emotions[probabilities.argmax()]