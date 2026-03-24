import cv2
import mediapipe as mp
from mediapipe.tasks import python
from mediapipe.tasks.python import vision
import numpy as np
from collections import deque
import time
from udp_server import UDPServer


class HandGestureDetector:
    def __init__(self, model_path='hand_landmarker.task', udp_server=None):
        base_options = python.BaseOptions(model_asset_path=model_path)
        options = vision.HandLandmarkerOptions(
            base_options=base_options,
            num_hands=1,
            min_hand_detection_confidence=0.5,
            min_hand_presence_confidence=0.5
        )
        self.detector = vision.HandLandmarker.create_from_options(options)

        self.udp_server = udp_server

        self.HAND_CONNECTIONS = [
            (0, 1), (1, 2), (2, 3), (3, 4),  # большой палец
            (0, 5), (5, 6), (6, 7), (7, 8),  # указательный
            (0, 9), (9, 10), (10, 11), (11, 12),  # средний
            (0, 13), (13, 14), (14, 15), (15, 16),  # безымянный
            (0, 17), (17, 18), (18, 19), (19, 20),  # мизинец
            (5, 9), (9, 13), (13, 17)  # соединения ладони
        ]

        self.prev_time = time.time()
        self.fps_counter = deque(maxlen=30)

    @staticmethod
    def calculate_index_finger_vector(hand_landmarks, frame_shape):
        points = []
        for landmark in hand_landmarks:
            x = int(landmark.x * frame_shape[1])
            y = int(landmark.y * frame_shape[0])
            points.append((x, y))

        if len(points) < 21:
            return None

        # указательный палец
        index_mcp = points[5]  # основание
        index_tip = points[8]  # кончик

        vector = np.array([index_tip[0] - index_mcp[0], -(index_tip[1] - index_mcp[1])])

        norm = np.linalg.norm(vector)
        if norm > 0:
            normalized_vector = vector / norm
            return normalized_vector, index_mcp, index_tip
        return None

    @staticmethod
    def is_fist_gesture(hand_landmarks, frame_shape):
        points = []
        for landmark in hand_landmarks:
            x = int(landmark.x * frame_shape[1])
            y = int(landmark.y * frame_shape[0])
            points.append((x, y))

        if len(points) < 21:
            return False

        finger_tips = [4, 8, 12, 16, 20]  # большой, указательный, средний, безымянный, мизинец
        finger_pips = [3, 6, 10, 14, 18]  # средние суставы
        finger_mcps = [2, 5, 9, 13, 17]  # основания

        # Центр ладони (приблизительно между основаниями пальцев)
        palm_center_x = np.mean([points[i][0] for i in [0, 5, 9, 13, 17]])
        palm_center_y = np.mean([points[i][1] for i in [0, 5, 9, 13, 17]])
        palm_center = np.array([palm_center_x, palm_center_y])

        # Радиус ладони (среднее расстояние от центра до оснований)
        palm_radius = np.mean([np.linalg.norm(np.array(points[i]) - palm_center)
                               for i in [0, 5, 9, 13, 17]])

        bent_fingers = 0

        for i, (tip_idx, pip_idx, mcp_idx) in enumerate(zip(finger_tips, finger_pips, finger_mcps)):
            tip_point = np.array(points[tip_idx])
            pip_point = np.array(points[pip_idx])
            mcp_point = np.array(points[mcp_idx])

            # Расстояние от кончика до центра ладони
            tip_to_palm_dist = np.linalg.norm(tip_point - palm_center)

            if i == 0:  # большой палец
                # Проверяем, находится ли кончик большого пальца близко к ладони
                if tip_to_palm_dist < palm_radius * 1.3:
                    bent_fingers += 1
            else:
                # Для остальных пальцев: проверяем, согнут ли палец
                # Вычисляем угол между векторами от основания до среднего сустава и от среднего до кончика
                vec1 = pip_point - mcp_point
                vec2 = tip_point - pip_point

                # Нормализуем векторы
                norm1 = np.linalg.norm(vec1)
                norm2 = np.linalg.norm(vec2)

                if norm1 > 0 and norm2 > 0:
                    vec1_norm = vec1 / norm1
                    vec2_norm = vec2 / norm2

                    # Косинус угла между векторами
                    cos_angle = np.dot(vec1_norm, vec2_norm)

                    # Если угол большой (косинус маленький) - палец согнут
                    if cos_angle < 0.5:  # угол больше 60 градусов
                        bent_fingers += 1
                    # Дополнительная проверка: кончик пальца близко к ладони
                    elif tip_to_palm_dist < palm_radius * 1.2:
                        bent_fingers += 1

        # Также проверяем общую компактность руки
        all_points = np.array(points)
        hand_spread = np.max(np.linalg.norm(all_points - palm_center, axis=1))

        # Если рука компактная и большинство пальцев согнуты - это кулак
        is_fist = (bent_fingers >= 4 and hand_spread < palm_radius * 2.5) or bent_fingers >= 5

        return is_fist

    def process_frame(self, frame):
        annotated_frame = frame.copy()

        current_time = time.time()
        fps = 1.0 / (current_time - self.prev_time) if self.prev_time else 0
        self.prev_time = current_time
        self.fps_counter.append(fps)
        avg_fps = sum(self.fps_counter) / len(self.fps_counter)

        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb_frame)

        # распознавание
        detection_result = self.detector.detect(mp_image)

        hand_detected = False
        if detection_result.hand_landmarks:
            for hand_idx, hand_landmarks in enumerate(detection_result.hand_landmarks):
                points = []
                for landmark in hand_landmarks:
                    x = int(landmark.x * frame.shape[1])
                    y = int(landmark.y * frame.shape[0])
                    points.append((x, y))

                for connection in self.HAND_CONNECTIONS:
                    start_idx, end_idx = connection
                    if start_idx < len(points) and end_idx < len(points):
                        cv2.line(annotated_frame, points[start_idx], points[end_idx],
                                 (100, 100, 100), 1)

                for i, (x, y) in enumerate(points):
                    cv2.circle(annotated_frame, (x, y), 4, (0, 255, 0), -1)

                hand_detected = True
                current_is_fist = self.is_fist_gesture(hand_landmarks, frame.shape)

                if current_is_fist:
                    cv2.putText(annotated_frame, "FIST",
                                (10, 60 + hand_idx * 40),
                                cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 255), 2)

                    # красный круг
                    center_x = int(np.mean([p[0] for p in points]))
                    center_y = int(np.mean([p[1] for p in points]))
                    cv2.circle(annotated_frame, (center_x, center_y), 50, (0, 0, 255), 3)

                    if hand_idx == 0 and self.udp_server:
                        self.udp_server.update_fist_detected(True)
                else:
                    # Обычная рука
                    vector_data = self.calculate_index_finger_vector(hand_landmarks, frame.shape)
                    if vector_data:
                        norm_vector, start_point, end_point = vector_data

                        cv2.arrowedLine(annotated_frame, start_point, end_point,
                                        (0, 0, 255), 4, tipLength=0.2)

                        for i in [5, 6, 7, 8]:
                            if i < len(points):
                                cv2.circle(annotated_frame, points[i], 6, (0, 0, 255), -1)

                        hand_text = f"Hand {hand_idx}"
                        cv2.putText(annotated_frame, hand_text,
                                    (10, 60 + hand_idx * 40),
                                    cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 0), 2)
                        vec_text = f"Vector: ({norm_vector[0]:.2f}, {norm_vector[1]:.2f})"
                        cv2.putText(annotated_frame, vec_text,
                                    (10, 60 + hand_idx * 40 + 25),
                                    cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 2)

                        if hand_idx == 0 and self.udp_server:
                            self.udp_server.update_vector(norm_vector)
                            self.udp_server.update_fist_detected(False)

        if not hand_detected:
            cv2.putText(annotated_frame, "NO HAND DETECTED",
                        (10, 100), cv2.FONT_HERSHEY_SIMPLEX,
                        0.7, (0, 0, 255), 2)
            if self.udp_server:
                self.udp_server.update_fist_detected(False)

        cv2.putText(annotated_frame, f"FPS: {avg_fps:.1f}", (10, 30),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 0), 2)

        return annotated_frame


def main():
    udp_server = UDPServer()
    udp_server.start()
    detector = HandGestureDetector(
        model_path='hand_landmarker.task',
        udp_server=udp_server
    )
    cap = cv2.VideoCapture(0)
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, 200)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 150)
    print(f"UDP сервер отправляет данные на {udp_server.ip}:{udp_server.port}")

    try:
        while cap.isOpened():
            success, frame = cap.read()
            if not success:
                break

            frame = cv2.flip(frame, 1)
            annotated_frame = detector.process_frame(frame)
            cv2.imshow('Hand Gesture Detection + UDP Server', annotated_frame)

            if cv2.waitKey(1) & 0xFF == 27:
                break
    finally:
        detector.detector.close()
        cap.release()
        cv2.destroyAllWindows()
        udp_server.stop()


if __name__ == "__main__":
    main()
