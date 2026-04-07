import sys
from PyQt6.QtWidgets import (QApplication, QMainWindow, QTextEdit,
                             QVBoxLayout, QWidget, QGraphicsOpacityEffect)
from PyQt6.QtCore import Qt, QPropertyAnimation, QEasingCurve
from PyQt6.QtGui import QFont, QTextCharFormat

STYLES = """
    QWidget {
        background-color: #2c2c2c;
        color: #dcdcdc;
        border-radius: 8px;
    }
    QTextEdit {
        background-color: #1e1e1e;
        border: 1px solid #3f3f3f;
        padding: 20px;
        font-family: 'Segoe UI', sans-serif;
        line-height: 1.5;
    }
"""


class TextEditor(QMainWindow):
    def __init__(self):
        super().__init__()
        self.init_ui()
        self.fade_in()

    def init_ui(self):
        self.setWindowFlags(Qt.WindowType.FramelessWindowHint)
        self.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground)
        self.resize(800, 600)

        self.central_widget = QWidget()
        self.setCentralWidget(self.central_widget)
        self.layout = QVBoxLayout(self.central_widget)
        self.layout.setContentsMargins(15, 15, 15, 15)

        self.editor = QTextEdit()
        self.editor.setAcceptRichText(True)
        self.editor.setPlaceholderText("Начните вводить заметку...")
        self.layout.addWidget(self.editor)

        self.opacity_effect = QGraphicsOpacityEffect(self)
        self.setGraphicsEffect(self.opacity_effect)
        self.setStyleSheet(STYLES)

    def set_heading(self, level):
        cursor = self.editor.textCursor()
        cursor.blockFormat()
        sizes = {1: 24, 2: 18, 0: 12}

        format = QTextCharFormat()
        format.setFontPointSize(sizes.get(level, 12))
        format.setFontWeight(QFont.Weight.Bold if level > 0 else QFont.Weight.Normal)

        cursor.select(cursor.SelectionType.BlockUnderCursor)
        cursor.mergeCharFormat(format)

    def toggle_bold(self):
        cursor = self.editor.textCursor()
        format = QTextCharFormat()
        format.setFontWeight(
            QFont.Weight.Normal if cursor.charFormat().fontWeight() == QFont.Weight.Bold else QFont.Weight.Bold)
        cursor.mergeCharFormat(format)

    def fade_in(self):
        self.anim = QPropertyAnimation(self.opacity_effect, b"opacity")
        self.anim.setDuration(800)
        self.anim.setStartValue(0)
        self.anim.setEndValue(1)
        self.anim.setEasingCurve(QEasingCurve.Type.InOutQuad)
        self.anim.start()

    def close_smoothly(self):
        self.anim = QPropertyAnimation(self.opacity_effect, b"opacity")
        self.anim.setDuration(500)
        self.anim.setStartValue(1)
        self.anim.setEndValue(0)
        self.anim.finished.connect(self.close)
        self.anim.start()

    def keyPressEvent(self, event):
        if event.modifiers() == Qt.KeyboardModifier.ControlModifier:
            if event.key() == Qt.Key.Key_1:
                self.set_heading(1)
            elif event.key() == Qt.Key.Key_2:
                self.set_heading(2)
            elif event.key() == Qt.Key.Key_0:
                self.set_heading(0)
            elif event.key() == Qt.Key.Key_B:
                self.toggle_bold()
            elif event.key() == Qt.Key.Key_Q:
                self.close_smoothly()
        if event.key() == Qt.Key.Key_Escape:
            self.close_smoothly()
        super().keyPressEvent(event)


if __name__ == "__main__":
    app = QApplication(sys.argv)
    editor = TextEditor()
    editor.show()
    sys.exit(app.exec())
