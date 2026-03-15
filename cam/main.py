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

                # вектор указательного пальца
                vector_data = self.calculate_index_finger_vector(hand_landmarks, frame.shape)
                if vector_data:
                    hand_detected = True
                    norm_vector, start_point, end_point = vector_data

                    cv2.arrowedLine(annotated_frame, start_point, end_point,
                                    (0, 0, 255), 4, tipLength=0.2)

                    for i in [5, 6, 7, 8]:
                        if i < len(points):
                            cv2.circle(annotated_frame, points[i], 6, (0, 0, 255), -1)

                    y_offset = 60 + hand_idx * 40
                    hand_text = f"Hand {hand_idx}"
                    cv2.putText(annotated_frame, hand_text,
                                (10, y_offset),
                                cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 0), 2)
                    vec_text = f"Vector: ({norm_vector[0]:.2f}, {norm_vector[1]:.2f})"
                    cv2.putText(annotated_frame, vec_text,
                                (10, y_offset + 25),
                                cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 2)

                    if hand_idx == 0 and self.udp_server:
                        self.udp_server.update_vector(norm_vector)

        if not hand_detected:
            cv2.putText(annotated_frame, "NO HAND DETECTED",
                        (10, 100), cv2.FONT_HERSHEY_SIMPLEX,
                        0.7, (0, 0, 255), 2)

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
    print(f"UDP сервер отправляет данные на {udp_server.ip}:{udp_server.port}")

    try:
        while cap.isOpened():
            success, frame = cap.read()
            if not success:
                break

            frame = cv2.flip(frame, 1)
            annotated_frame = detector.process_frame(frame)
            cv2.imshow('Index Finger Vector Detection + UDP Server', annotated_frame)

            if cv2.waitKey(1) & 0xFF == 27:
                break
    finally:
        detector.detector.close()
        cap.release()
        cv2.destroyAllWindows()
        udp_server.stop()


if __name__ == "__main__":
    main()
