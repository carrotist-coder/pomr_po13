import cv2
import mediapipe as mp
from mediapipe.tasks import python
from mediapipe.tasks.python import vision
import numpy as np
from collections import deque
import time

base_options = python.BaseOptions(model_asset_path='hand_landmarker.task')
options = vision.HandLandmarkerOptions(
    base_options=base_options,
    num_hands=2,
    min_hand_detection_confidence=0.5,
    min_hand_presence_confidence=0.5
)

detector = vision.HandLandmarker.create_from_options(options)

HAND_CONNECTIONS = [
    # Большой палец
    (0, 1), (1, 2), (2, 3), (3, 4),
    # Указательный палец
    (0, 5), (5, 6), (6, 7), (7, 8),
    # Средний палец
    (0, 9), (9, 10), (10, 11), (11, 12),
    # Безымянный палец
    (0, 13), (13, 14), (14, 15), (15, 16),
    # Мизинец
    (0, 17), (17, 18), (18, 19), (19, 20),
    # Дополнительные соединения ладони
    (5, 9), (9, 13), (13, 17)
]

cap = cv2.VideoCapture(0)

# Для измерения скорости
prev_hand_positions = {}  # Словарь для хранения предыдущих позиций рук
prev_time = time.time()
fps_counter = deque(maxlen=30)
speed_history = deque(maxlen=10)

while cap.isOpened():
    success, frame = cap.read()
    if not success:
        break

    frame = cv2.flip(frame, 1)

    annotated_frame = frame.copy()

    current_time = time.time()
    fps = 1.0 / (current_time - prev_time) if prev_time else 0
    prev_time = current_time
    fps_counter.append(fps)
    avg_fps = sum(fps_counter) / len(fps_counter)

    rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb_frame)

    # распознавание
    detection_result = detector.detect(mp_image)

    # Отрисовка точек и линий
    current_hand_positions = {}

    if detection_result.hand_landmarks:
        for hand_idx, hand_landmarks in enumerate(detection_result.hand_landmarks):
            points = []
            for landmark in hand_landmarks:
                x = int(landmark.x * frame.shape[1])
                y = int(landmark.y * frame.shape[0])
                points.append((x, y))

            for connection in HAND_CONNECTIONS:
                start_idx, end_idx = connection
                if start_idx < len(points) and end_idx < len(points):
                    cv2.line(annotated_frame, points[start_idx], points[end_idx],
                             (255, 255, 0), 2)

            for i, (x, y) in enumerate(points):
                # точки
                cv2.circle(annotated_frame, (x, y), 6, (0, 255, 0), -1)
                cv2.circle(annotated_frame, (x, y), 8, (255, 255, 255), 2)

                # подпись с номером
                cv2.putText(annotated_frame, str(i), (x + 10, y - 10),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 0, 0), 2)
                cv2.putText(annotated_frame, str(i), (x + 12, y - 12),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 1)

            if points:
                current_hand_positions[hand_idx] = points[0]  # центр ладони (точка 0) для измерения скорости

                # скорость движения руки
                if hand_idx in prev_hand_positions:
                    prev_x, prev_y = prev_hand_positions[hand_idx]
                    curr_x, curr_y = current_hand_positions[hand_idx]

                    # Евклидово расстояние между кадрами
                    distance = np.sqrt((curr_x - prev_x) ** 2 + (curr_y - prev_y) ** 2)

                    # Скорость в пикселях в секунду
                    speed = distance * avg_fps
                    speed_history.append(speed)
                    avg_speed = sum(speed_history) / len(speed_history)

                    speed_text = f"Hand {hand_idx + 1} speed: {speed:.1f} px/s"
                    cv2.putText(annotated_frame, speed_text,
                                (10, 60 + hand_idx * 25),
                                cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 0, 255), 2)

                    # Визуализация скорости цветом линий (чем быстрее, тем краснее)
                    if speed > 50:
                        for connection in HAND_CONNECTIONS:
                            start_idx, end_idx = connection
                            if start_idx < len(points) and end_idx < len(points):
                                color_intensity = min(255, int(speed * 2))
                                cv2.line(annotated_frame, points[start_idx], points[end_idx],
                                         (0, 255 - color_intensity, 255), 3)

    prev_hand_positions = current_hand_positions

    cv2.putText(annotated_frame, f"FPS: {avg_fps:.1f}", (10, 30),
                cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 0), 2)

    if speed_history:
        avg_speed_all = sum(speed_history) / len(speed_history)
        cv2.putText(annotated_frame, f"Avg speed: {avg_speed_all:.1f} px/s",
                    (10, 110) if detection_result.hand_landmarks else (10, 60),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 255), 2)

    cv2.imshow('MediaPipe Hands - Speed Visualization', annotated_frame)

    if cv2.waitKey(1) & 0xFF == 27:
        break

detector.close()
cap.release()
cv2.destroyAllWindows()
