import socket
import threading
import time
import queue


class UDPServer:
    def __init__(self, ip="127.0.0.1", port=5005, broadcast_interval=0.5):
        self.server_thread = None
        self.ip = ip
        self.port = port
        self.broadcast_interval = broadcast_interval
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)

        self.data_queue = queue.Queue()
        self.running = True
        self.fist_detected = False

        # Для отладки
        self.sent_count = 0

    def send_data(self, data):
        try:
            message = data.encode('utf-8')
            self.socket.sendto(message, (self.ip, self.port))
            self.sent_count += 1
            print(f"Отправлено ({self.sent_count}): {message}")
        except Exception as e:
            print(f"Ошибка отправки: {e}")

    def broadcast_loop(self):
        print(f"UDP сервер запущен на {self.ip}:{self.port}")
        print(f"Интервал отправки: {self.broadcast_interval * 1000:.0f} мс")

        while self.running:
            try:
                data = self.data_queue.get(timeout=1.0)
                self.send_data(data)
                time.sleep(self.broadcast_interval)
            except queue.Empty:
                continue
            except Exception as e:
                print(f"Ошибка в цикле отправки: {e}")
                time.sleep(0.1)

    def start(self):
        self.server_thread = threading.Thread(target=self.broadcast_loop)
        self.server_thread.daemon = True
        self.server_thread.start()
        print("UDP сервер запущен в фоновом режиме")

    def update_vector(self, vector_data):
        if vector_data is not None:
            while not self.data_queue.empty():
                try:
                    self.data_queue.get_nowait()
                except queue.Empty:
                    break
            x = float(vector_data[0])
            y = float(vector_data[1])
            message = f"{x:.4f} {y:.4f}"
            self.data_queue.put(message)

    def update_fist_detected(self, is_fist):
        if is_fist != self.fist_detected:
            self.fist_detected = is_fist
            if is_fist:
                while not self.data_queue.empty():
                    try:
                        self.data_queue.get_nowait()
                    except queue.Empty:
                        break
                self.data_queue.put("ok")

    def stop(self):
        self.running = False
        if hasattr(self, 'server_thread'):
            self.server_thread.join(timeout=2.0)
        self.socket.close()
        print("UDP сервер остановлен")
