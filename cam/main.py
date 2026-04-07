import cv2
import mediapipe as mp
from mediapipe.tasks import python
from mediapipe.tasks.python import vision
import numpy as np
from collections import deque
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

        # Для сглаживания детекции кулака (избегаем мерцания)
        self.fist_history = deque(maxlen=5)
        self.vector_history = deque(maxlen=3)
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

        return is_straight and tip_distance > mcp_distance

    @staticmethod
    def is_fist_gesture(hand_landmarks, frame_shape):
        points = []
        for landmark in hand_landmarks:
            x = int(landmark.x * frame_shape[1])
            y = int(landmark.y * frame_shape[0])
            points.append((x, y))

        if len(points) < 21:
            return False

        if HandGestureDetector.is_index_finger(hand_landmarks, frame_shape):
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
        if thumb_to_palm_dist < palm_radius * 1.2:
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
        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb_frame)

        # распознавание
        detection_result = self.detector.detect(mp_image)

        if not detection_result.hand_landmarks:
            if self.udp_server:
                self.udp_server.update_fist_detected(False)
                self.fist_history.clear()
                self.vector_history.clear()
                self.fist_transition_counter = 0
            return

        for hand_landmarks in detection_result.hand_landmarks:
            current_is_fist = self.is_fist_gesture(hand_landmarks, frame.shape)

            self.fist_history.append(current_is_fist)
            smoothed_is_fist = sum(self.fist_history) > len(self.fist_history) // 2

            if smoothed_is_fist:
                if self.udp_server:
                    self.udp_server.update_fist_detected(True)
                self.fist_transition_counter = self.FIST_TRANSITION_FRAMES
            else:
                index_is_straight = self.is_index_finger(hand_landmarks, frame.shape, straight_threshold=0.88)

                if self.fist_transition_counter > 0:
                    self.fist_transition_counter -= 1
                    index_is_straight = False

                if index_is_straight:
                    vector_data = self.calculate_index_finger_vector(hand_landmarks, frame.shape)
                    if vector_data:
                        norm_vector, _, _ = vector_data

                        self.vector_history.append(norm_vector)
                        if len(self.vector_history) > 0:
                            avg_vector = np.mean(self.vector_history, axis=0)
                            avg_vector = avg_vector / np.linalg.norm(avg_vector)
                        else:
                            avg_vector = norm_vector

                        if self.udp_server:
                            self.udp_server.update_vector(avg_vector)
                            self.udp_server.update_fist_detected(False)
                else:
                    if self.udp_server:
                        self.udp_server.update_fist_detected(False)


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
            detector.process_frame(frame)
            cv2.waitKey(1)

    except KeyboardInterrupt:
        print("\nОстановка по запросу пользователя")
    finally:
        detector.detector.close()
        cap.release()
        udp_server.stop()


if __name__ == "__main__":
    main()
