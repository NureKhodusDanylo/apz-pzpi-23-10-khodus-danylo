from machine import Pin
from time import sleep_us

class TM1637(object):
    # Словарь со всеми буквами и цифрами
    _SEGMENTS = {
        '0': 0x3F, '1': 0x06, '2': 0x5B, '3': 0x4F, '4': 0x66, '5': 0x6D, '6': 0x7D, '7': 0x07,
        '8': 0x7F, '9': 0x6F, 'A': 0x77, 'B': 0x7C, 'C': 0x39, 'D': 0x5E, 'E': 0x79, 'F': 0x71,
        'G': 0x3D, 'H': 0x76, 'I': 0x06, 'J': 0x1E, 'K': 0x76, 'L': 0x38, 'M': 0x55, 'N': 0x54,
        'O': 0x3F, 'P': 0x73, 'Q': 0x67, 'R': 0x50, 'S': 0x6D, 'T': 0x78, 'U': 0x3E, 'V': 0x1C,
        'W': 0x2A, 'X': 0x76, 'Y': 0x66, 'Z': 0x5B, ' ': 0x00, '-': 0x40, '_': 0x08,
    }

    def __init__(self, clk, dio, brightness=7):
        self.clk = clk
        self.dio = dio
        if not 0 <= brightness <= 7:
            raise ValueError("Brightness must be 0-7")
        self._brightness = brightness
        self.clk.init(Pin.OUT, value=0)
        self.dio.init(Pin.OUT, value=0)
        self.clk.value(1)
        self.dio.value(1)
        sleep_us(10)
        self._write_data_cmd()
        self._write_dsp_ctrl()

    def _start(self):
        self.dio.value(0)
        sleep_us(10)
        self.clk.value(0)
        sleep_us(10)

    def _stop(self):
        self.dio.value(0)
        sleep_us(10)
        self.clk.value(1)
        sleep_us(10)
        self.dio.value(1)

    def _write_data_cmd(self):
        self._start()
        self._write_byte(0x40)
        self._stop()

    def _write_dsp_ctrl(self):
        self._start()
        self._write_byte(0x88 | self._brightness)
        self._stop()

    def _write_byte(self, b):
        for i in range(8):
            self.dio.value((b >> i) & 1)
            sleep_us(10)
            self.clk.value(1)
            sleep_us(10)
            self.clk.value(0)
            sleep_us(10)
        self.clk.value(0)
        sleep_us(10)
        self.clk.value(1)
        sleep_us(10)
        self.clk.value(0)
        sleep_us(10)

    def brightness(self, val=None):
        if val is None:
            return self._brightness
        if not 0 <= val <= 7:
            raise ValueError("Brightness must be 0-7")
        self._brightness = val
        self._write_data_cmd()
        self._write_dsp_ctrl()

    def write(self, segments, pos=0):
        if not 0 <= pos <= 3:
            raise ValueError("Pos must be 0-3")
        self._write_data_cmd()
        self._start()
        self._write_byte(0xC0 | pos)
        for seg in segments:
            self._write_byte(seg)
        self._stop()
        self._write_dsp_ctrl()

    def encode_char(self, char):
        # Преобразуем символ в верхний регистр и ищем в словаре
        # Если символа нет, возвращаем пустоту (0x00)
        c = char.upper()
        return self._SEGMENTS.get(c, 0x00)

    def hex(self, val):
        string = '{:04x}'.format(val & 0xffff)
        self.show(string)

    def number(self, num):
        num = max(-999, min(num, 9999))
        string = '{0: >4d}'.format(num)
        self.show(string)

    def show(self, string, colon=False):
        segments = bytearray(4)
        for i in range(4):
            # Берем символ или пробел, если строка короче 4
            char = string[i] if i < len(string) else ' '
            segments[i] = self.encode_char(char)
        
        if colon:
            segments[1] |= 0x80
            
        self.write(segments)