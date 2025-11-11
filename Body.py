import mediapipe as mp
import cv2

mp_drawing = mp.solutions.drawing_utils
mp_holistic = mp.solutions.holistic
mp_pose = mp.solutions.pose

DETECTION_AREA_NORM = (0.6, 0.7, 1.0, 1.0)

max_tested = 5
for i in range(max_tested):
    cap = cv2.VideoCapture(i)
    if cap.isOpened():
        print(f"Cámara encontrada en índice {i}")
        cap.release()
    else:
        print(f"No hay cámara en índice {i}")

cap = cv2.VideoCapture(0)

if not cap.isOpened():
    print("Error: No se pudo abrir la cámara")
    exit()

def is_in_detection_area(x, y, area_norm):
    x_min, y_min, x_max, y_max = area_norm
    return x_min <= x <= x_max and y_min <= y <= y_max

with mp_holistic.Holistic(min_detection_confidence=0.5, min_tracking_confidence=0.5) as holistic:
    while cap.isOpened():
        ret, frame = cap.read()
        if not ret:
            print("No se pudo leer frame")
            break

        h, w, _ = frame.shape

        image = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        image.flags.writeable = False 
        results = holistic.process(image)
        image.flags.writeable = True 
        image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)

        x_min_px = int(DETECTION_AREA_NORM[0] * w)
        y_min_px = int(DETECTION_AREA_NORM[1] * h)
        x_max_px = int(DETECTION_AREA_NORM[2] * w)
        y_max_px = int(DETECTION_AREA_NORM[3] * h)
        
        cv2.rectangle(image, 
                      (x_min_px, y_min_px), 
                      (x_max_px, y_max_px), 
                      (0, 255, 0), 
                      2) 

        foot_touched = False
        if results.pose_landmarks:
            landmarks = results.pose_landmarks.landmark
            
            left_ankle = landmarks[mp_pose.PoseLandmark.LEFT_ANKLE.value]
            right_ankle = landmarks[mp_pose.PoseLandmark.RIGHT_ANKLE.value]

            if left_ankle.visibility > 0.5:
                if is_in_detection_area(left_ankle.x, left_ankle.y, DETECTION_AREA_NORM):
                    print("Left foot touched the delimited area!")
                    foot_touched = True

            if right_ankle.visibility > 0.5 and not foot_touched: 
                if is_in_detection_area(right_ankle.x, right_ankle.y, DETECTION_AREA_NORM):
                    print("Right foot touched the delimited area!")
                    foot_touched = True


        mp_drawing.draw_landmarks(image, results.pose_landmarks, mp_holistic.POSE_CONNECTIONS,
                                  mp_drawing.DrawingSpec(color=(245, 117, 66), thickness=2, circle_radius=4),
                                  mp_drawing.DrawingSpec(color=(245, 66, 230), thickness=2, circle_radius=2))


        cv2.imshow('Full Body Detection with Area Check', image)

        if cv2.waitKey(10) & 0xFF == ord('q'):
            break

cap.release()
cv2.destroyAllWindows()