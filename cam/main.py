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

        # Для сглаживания детекции кулака (избегаем мерцания)
        self.fist_history = deque(maxlen=5)
        self.vector_history = deque(maxlen=3)

        # для плавного перехода
        self.fist_transition_counter = 0
        self.FIST_TRANSITION_FRAMES = 8  # кадров для выхода из кулака

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
    def is_index_finger(hand_landmarks, frame_shape, straight_threshold=0.88):
        points = []
        for landmark in hand_landmarks:
            x = int(landmark.x * frame_shape[1])
            y = int(landmark.y * frame_shape[0])
            points.append((x, y))

        if len(points) < 21:
            return False

        # Точки указательного пальца
        index_mcp = np.array(points[5])  # основание
        index_pip = np.array(points[6])  # средний сустав
        index_dip = np.array(points[7])  # верхний сустав
        index_tip = np.array(points[8])  # кончик

        vec_proximal = index_pip - index_mcp
        vec_middle = index_dip - index_pip
        vec_distal = index_tip - index_dip

        norm_prox = np.linalg.norm(vec_proximal)
        norm_mid = np.linalg.norm(vec_middle)
        norm_dist = np.linalg.norm(vec_distal)

        if norm_prox == 0 or norm_mid == 0 or norm_dist == 0:
            return False

        vec_prox_norm = vec_proximal / norm_prox
        vec_mid_norm = vec_middle / norm_mid
        vec_dist_norm = vec_distal / norm_dist

        angle_prox_mid = np.dot(vec_prox_norm, vec_mid_norm)
        angle_mid_dist = np.dot(vec_mid_norm, vec_dist_norm)

        is_straight = angle_prox_mid > straight_threshold and angle_mid_dist > straight_threshold

        palm_center = np.array([points[0][0], points[0][1]])
        tip_distance = np.linalg.norm(index_tip - palm_center)
        mcp_distance = np.linalg.norm(index_mcp - palm_center)

        is_extended = is_straight and tip_distance > mcp_distance

        return is_extended

    @staticmethod
    def is_fist_gesture(hand_landmarks, frame_shape):
        points = []
        for landmark in hand_landmarks:
            x = int(landmark.x * frame_shape[1])
            y = int(landmark.y * frame_shape[0])
            points.append((x, y))

        if len(points) < 21:
            return False

        index_extended = HandGestureDetector.is_index_finger(hand_landmarks, frame_shape)
        if index_extended:
            return False

        finger_tips = [4, 8, 12, 16, 20]  # большой, указательный, средний, безымянный, мизинец
        finger_pips = [3, 6, 10, 14, 18]  # средние суставы
        finger_mcps = [2, 5, 9, 13, 17]  # основания

        # Центр ладони (приблизительно между основаниями пальцев)
        palm_center = np.array([np.mean([points[i][0] for i in [0, 5, 9, 13, 17]]),
                                np.mean([points[i][1] for i in [0, 5, 9, 13, 17]])])

        # Радиус ладони (среднее расстояние от центра до оснований)
        palm_radius = np.mean([np.linalg.norm(np.array(points[i]) - palm_center)
                               for i in [0, 5, 9, 13, 17]])

        bent_fingers = 0

        # Проверяем большой палец отдельно
        thumb_tip = np.array(points[4])
        thumb_to_palm_dist = np.linalg.norm(thumb_tip - palm_center)
        thumb_bent = thumb_to_palm_dist < palm_radius * 1.2

        if thumb_bent:
            bent_fingers += 1

        # Проверяем остальные пальцы (начиная с указательного, который уже согнут)
        for i in range(1, 5):
            tip_idx = finger_tips[i]
            pip_idx = finger_pips[i]
            mcp_idx = finger_mcps[i]

            tip_point = np.array(points[tip_idx])
            pip_point = np.array(points[pip_idx])
            mcp_point = np.array(points[mcp_idx])

            # Расстояние от кончика до центра ладони
            tip_to_palm_dist = np.linalg.norm(tip_point - palm_center)

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
                cos_angle = np.dot(vec1_norm, vec2_norm)

                if cos_angle < 0.7 or tip_to_palm_dist < palm_radius * 1.3:
                    bent_fingers += 1
            else:
                if tip_to_palm_dist < palm_radius * 1.3:
                    bent_fingers += 1

        is_fist = bent_fingers >= 4
        all_points = np.array(points)
        hand_spread = np.max(np.linalg.norm(all_points - palm_center, axis=1))

        if hand_spread > palm_radius * 2.8:
            is_fist = False

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

                # Сглаживание детекции
                self.fist_history.append(current_is_fist)
                smoothed_is_fist = sum(self.fist_history) > len(self.fist_history) // 2

                if smoothed_is_fist:
                    cv2.putText(annotated_frame, "FIST",
                                (10, 60 + hand_idx * 40),
                                cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 0, 255), 2)

                    # красный круг
                    center_x = int(np.mean([p[0] for p in points]))
                    center_y = int(np.mean([p[1] for p in points]))
                    cv2.circle(annotated_frame, (center_x, center_y), 50, (0, 0, 255), 3)

                    if hand_idx == 0 and self.udp_server:
                        self.udp_server.update_fist_detected(True)
                        #  self.udp_server.update_vector(None)
                    self.fist_transition_counter = self.FIST_TRANSITION_FRAMES
                else:
                    # прямой ли указательный палец
                    index_is_straight = self.is_index_finger(hand_landmarks, frame.shape, straight_threshold=0.88)

                    # после выхода из кулака некоторое время не отправляем вектор
                    if self.fist_transition_counter > 0:
                        self.fist_transition_counter -= 1
                        cv2.putText(annotated_frame, "EXITING FIST...",
                                    (10, 60 + hand_idx * 40 + 60),
                                    cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 165, 255), 1)
                        index_is_straight = False

                    if index_is_straight:
                        vector_data = self.calculate_index_finger_vector(hand_landmarks, frame.shape)
                        if vector_data:
                            norm_vector, start_point, end_point = vector_data

                            # Сглаживание вектора
                            self.vector_history.append(norm_vector)
                            if len(self.vector_history) > 0:
                                avg_vector = np.mean(self.vector_history, axis=0)
                                avg_vector = avg_vector / np.linalg.norm(avg_vector)
                            else:
                                avg_vector = norm_vector

                            cv2.arrowedLine(annotated_frame, start_point, end_point,
                                            (0, 0, 255), 4, tipLength=0.2)

                            for i in [5, 6, 7, 8]:
                                if i < len(points):
                                    cv2.circle(annotated_frame, points[i], 6, (0, 0, 255), -1)

                            hand_text = "INDEX FINGER"
                            cv2.putText(annotated_frame, hand_text,
                                        (10, 60 + hand_idx * 40),
                                        cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 255), 2)

                            vec_text = f"Vector: ({avg_vector[0]:.2f}, {avg_vector[1]:.2f})"
                            cv2.putText(annotated_frame, vec_text,
                                        (10, 60 + hand_idx * 40 + 30),
                                        cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 1)

                            if hand_idx == 0 and self.udp_server:
                                self.udp_server.update_vector(avg_vector)
                                self.udp_server.update_fist_detected(False)
                    else:
                        cv2.putText(annotated_frame, "INDEX FINGER BENT - NO VECTOR",
                                    (10, 60 + hand_idx * 40),
                                    cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 0, 255), 2)
                        if hand_idx == 0 and self.udp_server:
                            self.udp_server.update_fist_detected(False)
                            # self.udp_server.update_vector(None)

        if not hand_detected:
            cv2.putText(annotated_frame, "NO HAND DETECTED",
                        (10, 100), cv2.FONT_HERSHEY_SIMPLEX,
                        0.7, (0, 0, 255), 2)
            if self.udp_server:
                self.udp_server.update_fist_detected(False)
                self.fist_history.clear()
                self.vector_history.clear()
                self.fist_transition_counter = 0

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
