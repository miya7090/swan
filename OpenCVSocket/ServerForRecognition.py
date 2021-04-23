import time
import socket
import cv2
import EmotionRecognition as ER

# SETTINGS ---
# TODO add option to turn off face visualization
# ------------

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

f_cascade, e_cascade, emotion_net = ER.loadClassifiers()

while capture.isOpened():
    ret, frame = capture.read()

    faceImg = ER.extractFaceImg(frame, f_cascade, e_cascade)

    if faceImg is not None:
        currentEmotion = ER.getEmotionOfFace(faceImg, emotion_net)

        if (time.time() - lastPing > pingInterval):
            lastPing = time.time()
            sock.sendto(currentEmotion.encode(), (UDP_IP, UDP_PORT))
            print("sending [" + currentEmotion + "] to socket")

    # close if 'q' is pressed
    if cv2.waitKey(1) == ord('q'):
        break

capture.release()
cv2.destroyAllWindows()