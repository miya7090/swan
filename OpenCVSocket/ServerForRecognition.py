import time
import socket
import cv2
import EmotionRecognition as ER

# SETTINGS ---
FACE_VISUALIZE = True # whether to display feed
ANNOTATE_FACE = True # whether to display feed with circles over faces
                      # (only displays if FACE_VISUALIZE also true)
PING_INTERVAL = 2 # seconds between sending updates for current expression
# ------------

UDP_IP = "127.0.0.1"
UDP_PORT = 5065

lastPing = time.time()
lastFaceCoords = None

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
print("socket created")

try:
    default = 0 # cam source
    capture = cv2.VideoCapture(default)
    print("camera found")
except:
    print("error finding camera")

f_cascade, e_cascade, emotion_net = ER.loadClassifiers()
print("loaded classifiers")

while capture.isOpened():
    ret, frame = capture.read()

    if (time.time() - lastPing > PING_INTERVAL): # estimate current emotion
        lastPing = time.time()
        faceImg, lastFaceCoords = ER.extractFaceImg(frame, f_cascade, e_cascade)

        if faceImg is None:
            print("no face detected")
        else:
            currentEmotion = ER.getEmotionOfFace(faceImg, emotion_net)
            sock.sendto(currentEmotion.encode(), (UDP_IP, UDP_PORT))
            print("sending [" + currentEmotion + "] from socket")
    
    if ANNOTATE_FACE:
        frame = ER.annotateFace(frame, lastFaceCoords)

    if FACE_VISUALIZE:
        cv2.imshow("Face detected", frame)
    
    # close if 'q' is pressed
    if cv2.waitKey(1) == ord('q'):
        break

capture.release()
cv2.destroyAllWindows()