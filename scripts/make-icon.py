#!/usr/bin/env python3
"""Generates the LessSleep Thunderstore icon (256x256 PNG) without external deps."""

import struct
import zlib

SIZE = 256
BG = (27, 42, 65)
MOON = (245, 200, 92)
TEXT = (232, 238, 245)


def make_canvas():
    return [[BG for _ in range(SIZE)] for _ in range(SIZE)]


def fill_circle(px, cx, cy, r, color):
    for y in range(max(0, cy - r), min(SIZE, cy + r + 1)):
        for x in range(max(0, cx - r), min(SIZE, cx + r + 1)):
            if (x - cx) ** 2 + (y - cy) ** 2 <= r * r:
                px[y][x] = color


def draw_z(px, x, y, w, h, t, color):
    for i in range(w):
        for j in range(t):
            px[y + j][x + i] = color
            px[y + h - 1 - j][x + i] = color
    for j in range(h):
        xpos = x + int(round((w - 1) * (1 - j / max(1, h - 1))))
        for k in range(t):
            xx = min(SIZE - 1, max(0, xpos + k))
            px[y + j][xx] = color


def write_png(path, px):
    raw = b"".join(b"\x00" + bytes(v for p in row for v in p) for row in px)

    def chunk(tag, data):
        body = tag + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body))

    ihdr = struct.pack(">IIBBBBB", SIZE, SIZE, 8, 2, 0, 0, 0)
    with open(path, "wb") as fh:
        fh.write(b"\x89PNG\r\n\x1a\n")
        fh.write(chunk(b"IHDR", ihdr))
        fh.write(chunk(b"IDAT", zlib.compress(raw, 9)))
        fh.write(chunk(b"IEND", b""))


def main():
    px = make_canvas()
    fill_circle(px, 158, 92, 62, MOON)
    fill_circle(px, 182, 74, 54, BG)
    draw_z(px, 42, 132, 62, 44, 7, TEXT)
    draw_z(px, 96, 176, 46, 32, 6, MOON)
    draw_z(px, 40, 196, 34, 24, 5, TEXT)
    write_png("icon.png", px)
    print("icon.png written (256x256)")


if __name__ == "__main__":
    main()
