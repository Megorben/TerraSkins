"""Generates the shared effect textures in Assets/Textures/FX.

Most textures are white with an alpha mask so the mod can tint them with any palette color.
Run from the repository root:  python3 tools/gen_fx.py
"""
import math
import os

import numpy as np
from PIL import ImageDraw

import fxlib as fx

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Textures", "FX")


def glow(size=128):
    x, y = fx.grid(size, size)
    r = np.hypot(x, y) / (size / 2)
    a = np.exp(-4.2 * r * r) * np.clip(1 - r ** 4, 0, 1)
    return fx.from_alpha(a)


def glow_sharp(size=64):
    x, y = fx.grid(size, size)
    r = np.hypot(x, y) / (size / 2)
    a = np.exp(-16 * r * r) + 0.4 * np.exp(-4.5 * r * r)
    a = a / a.max() * np.clip(1 - r ** 4, 0, 1)
    return fx.from_alpha(a)


def disc(size=64):
    x, y = fx.grid(size, size)
    d = np.hypot(x, y)
    return fx.from_alpha(np.clip((size / 2 - 1.5 - d) / 1.2, 0, 1))


def ring(size=256, radius_k=0.44, width=7.0, halo=22.0):
    x, y = fx.grid(size, size)
    d = np.hypot(x, y)
    r = size * radius_k
    a = np.exp(-((d - r) / width) ** 2) + 0.28 * np.exp(-((d - r) / halo) ** 2)
    a *= np.clip((size / 2 - d) / 6, 0, 1)
    return fx.from_alpha(a / a.max())


def ring_thin(size=256):
    x, y = fx.grid(size, size)
    d = np.hypot(x, y)
    r = size * 0.47
    a = np.exp(-((d - r) / 1.7) ** 2) + 0.22 * np.exp(-((d - r) / 6.5) ** 2)
    a *= np.clip((size / 2 - d) / 3, 0, 1)
    return fx.from_alpha(a / a.max())


def star(size=64):
    x, y = fx.grid(size, size)
    x, y = x / (size / 2), y / (size / 2)
    r = np.hypot(x, y)

    def arm(u, v, length=1.0, width=0.055):
        k = np.clip(1 - np.abs(u) / length, 0, 1)
        return k ** 2.2 * np.exp(-(v / (width * (0.35 + k))) ** 2)

    main = np.maximum(arm(x, y), arm(y, x))
    du, dv = (x + y) / math.sqrt(2), (x - y) / math.sqrt(2)
    diag = 0.45 * np.maximum(arm(du, dv, 0.45, 0.04), arm(dv, du, 0.45, 0.04))
    core = np.exp(-(r / 0.13) ** 2) + 0.35 * np.exp(-(r / 0.32) ** 2)
    a = np.clip(main + diag + core, 0, 1)
    return fx.from_alpha(a)


def streak(w=128, h=16):
    x, y = fx.grid(w, h)
    x, y = x / (w / 2), y / (h / 2)
    a = np.clip(1 - np.abs(x), 0, 1) ** 1.4 * np.exp(-(y / 0.42) ** 2)
    a += 0.5 * np.clip(1 - np.abs(x) * 1.6, 0, 1) ** 2 * np.exp(-(y / 0.18) ** 2)
    return fx.from_alpha(np.clip(a / a.max(), 0, 1))


def smoke(frame=64, frames=4):
    img = fx.canvas(frame * frames, frame)
    x, y = fx.grid(frame, frame)
    r = np.hypot(x, y) / (frame / 2)
    for i in range(frames):
        n = fx.value_noise(frame, frame, 3, seed=11 + i, octaves=5)
        n2 = fx.value_noise(frame, frame, 2, seed=91 + i, octaves=3)
        base = np.clip(1 - r * (0.75 + 0.4 * (n2 - 0.5)), 0, 1)
        a = fx.smoothstep(0.05, 0.6, base * (0.6 + 0.9 * n))
        a *= np.clip(1 - r ** 6, 0, 1)
        fx.over(img, fx.from_alpha(a * 0.9), i * frame, 0)
    return img


def disk(size=256):
    """Face-on accretion disk: bright inner edge, swirling streaks, fading outwards."""
    x, y = fx.grid(size, size)
    r = np.hypot(x, y) / (size / 2)
    th = np.arctan2(y, x)
    inner, outer = 0.33, 0.98
    mask = fx.smoothstep(inner - 0.02, inner + 0.05, r) * (1 - fx.smoothstep(outer - 0.25, outer, r))
    falloff = np.exp(-(r - inner) * 2.4)
    spiral = th * 3 + np.log(np.maximum(r, 1e-3)) * 7
    streaks = 0.62 + 0.22 * np.sin(spiral * 4) + 0.16 * np.sin(spiral * 9 + 1.3)
    noise = fx.value_noise(size, size, 6, seed=5, octaves=4)
    a = mask * falloff * streaks * (0.7 + 0.6 * noise)
    a += 0.9 * np.exp(-((r - inner - 0.03) / 0.025) ** 2) * mask
    return fx.from_alpha(np.clip(a / np.percentile(a, 99.7), 0, 1))


def galaxy(size=256, seed=3):
    x, y = fx.grid(size, size)
    r = np.hypot(x, y) / (size / 2)
    th = np.arctan2(y, x)
    a = np.zeros_like(r)
    for k in range(2):
        arm_angle = th - (k * math.pi + np.log(np.maximum(r, 0.02)) * 2.3)
        d = np.angle(np.exp(1j * arm_angle))
        width = 0.35 + 0.25 * r
        a += np.exp(-(d / width) ** 2) * np.exp(-r * 2.0) * fx.smoothstep(0.02, 0.15, r)
    noise = fx.value_noise(size, size, 8, seed=seed, octaves=4)
    a = a * (0.55 + 0.7 * noise)
    a += 1.4 * np.exp(-(r / 0.09) ** 2) + 0.35 * np.exp(-(r / 0.3) ** 2)
    rng = np.random.default_rng(seed)
    for _ in range(140):
        rr = rng.random() ** 0.7 * 0.95
        arm = rng.integers(0, 2) * math.pi + math.log(max(rr, 0.03)) * 2.3 + rng.normal(0, 0.35)
        px = size / 2 + math.cos(arm) * rr * size / 2
        py = size / 2 + math.sin(arm) * rr * size / 2
        a += 0.9 * rng.random() * np.exp(-((x + size / 2 - px) ** 2 + (y + size / 2 - py) ** 2) / 1.6)
    a *= np.clip(1 - r ** 3, 0, 1)
    return fx.from_alpha(np.clip(a / np.percentile(a, 99.5), 0, 1))


def rune_glyph_strokes():
    """Line strokes of 8 made-up runes in a unit square (0..1)."""
    return [
        [((0.5, 0.05), (0.5, 0.95)), ((0.5, 0.3), (0.15, 0.1)), ((0.5, 0.3), (0.85, 0.1)), ((0.5, 0.7), (0.85, 0.9))],
        [((0.2, 0.05), (0.2, 0.95)), ((0.2, 0.1), (0.8, 0.35)), ((0.8, 0.35), (0.2, 0.6)), ((0.2, 0.6), (0.8, 0.95))],
        [((0.5, 0.05), (0.1, 0.95)), ((0.5, 0.05), (0.9, 0.95)), ((0.25, 0.6), (0.75, 0.6))],
        [((0.15, 0.2), (0.85, 0.2)), ((0.5, 0.2), (0.5, 0.95)), ((0.2, 0.6), (0.8, 0.6))],
        [((0.2, 0.05), (0.2, 0.95)), ((0.8, 0.05), (0.8, 0.95)), ((0.2, 0.5), (0.8, 0.15)), ((0.2, 0.85), (0.8, 0.5))],
        [((0.5, 0.05), (0.85, 0.5)), ((0.85, 0.5), (0.5, 0.95)), ((0.5, 0.95), (0.15, 0.5)), ((0.15, 0.5), (0.5, 0.05)), ((0.5, 0.3), (0.5, 0.7))],
        [((0.15, 0.05), (0.85, 0.95)), ((0.85, 0.05), (0.15, 0.95)), ((0.5, 0.05), (0.5, 0.3))],
        [((0.5, 0.95), (0.5, 0.3)), ((0.5, 0.3), (0.15, 0.05)), ((0.5, 0.3), (0.85, 0.05)), ((0.25, 0.65), (0.75, 0.65))],
    ]


def runes(size=16):
    img = fx.canvas(size * 8, size)
    for i, strokes in enumerate(rune_glyph_strokes()):
        def paint(d, f, strokes=strokes):
            for (x0, y0), (x1, y1) in strokes:
                d.line([(x0 * (size - 3) * f + 1.5 * f, y0 * (size - 3) * f + 1.5 * f), (x1 * (size - 3) * f + 1.5 * f, y1 * (size - 3) * f + 1.5 * f)], fill=255, width=int(1.6 * f))
        a = fx.supersampled_mask(size, size, paint)
        fx.over(img, fx.from_alpha(a), i * size, 0)
    return img


def rune_circle(size=256):
    def paint(d, f):
        c = size * f / 2

        def circle(radius, width):
            d.ellipse([c - radius * f, c - radius * f, c + radius * f, c + radius * f], outline=255, width=max(1, int(width * f)))

        circle(size * 0.47, 2.2)
        circle(size * 0.42, 1.2)
        circle(size * 0.27, 1.6)
        circle(size * 0.1, 1.2)
        for tri in range(2):
            pts = []
            for k in range(3):
                ang = -math.pi / 2 + tri * math.pi + k * 2 * math.pi / 3
                pts.append((c + math.cos(ang) * size * 0.27 * f, c + math.sin(ang) * size * 0.27 * f))
            d.line(pts + [pts[0]], fill=255, width=int(1.4 * f))
        glyphs = rune_glyph_strokes()
        for k in range(16):
            ang = k * 2 * math.pi / 16
            gx = c + math.cos(ang) * size * 0.345 * f
            gy = c + math.sin(ang) * size * 0.345 * f
            g = glyphs[k % len(glyphs)]
            s = size * 0.06 * f
            for (x0, y0), (x1, y1) in g:
                p0 = (x0 - 0.5) * s, (y0 - 0.5) * s
                p1 = (x1 - 0.5) * s, (y1 - 0.5) * s
                rot = ang + math.pi / 2

                def rr(p):
                    return gx + p[0] * math.cos(rot) - p[1] * math.sin(rot), gy + p[0] * math.sin(rot) + p[1] * math.cos(rot)

                d.line([rr(p0), rr(p1)], fill=255, width=max(1, int(1.1 * f)))
        for k in range(48):
            ang = k * 2 * math.pi / 48
            r0, r1 = size * 0.42, size * 0.47
            d.line([(c + math.cos(ang) * r0 * f, c + math.sin(ang) * r0 * f), (c + math.cos(ang) * (r0 + (r1 - r0) * (0.6 if k % 2 else 1)) * f, c + math.sin(ang) * (r0 + (r1 - r0) * (0.6 if k % 2 else 1)) * f)], fill=255, width=max(1, int(0.9 * f)))

    a = fx.supersampled_mask(size, size, paint, factor=3)
    img = fx.from_alpha(a)
    # Soft glow under the lines.
    soft = fx.blur(img, 2.5)
    return np.clip(img + soft * 0.6, 0, 1)


def ray(w=64, h=256):
    x, y = fx.grid(w, h, cx=(w - 1) / 2, cy=0)
    u = x / (w / 2)
    v = y / (h - 1)  # 0 at the far tip, 1 at the origin (bottom)
    width = 0.12 + 0.75 * (1 - v)
    a = np.exp(-(u / width) ** 2) * v ** 1.3 * fx.smoothstep(0.0, 0.08, v)
    a *= fx.smoothstep(1.0, 0.94, v) * 0 + 1
    return fx.from_alpha(np.clip(a, 0, 1))


def orb(size=32):
    x, y = fx.grid(size, size)
    r = np.hypot(x, y) / (size / 2 - 1)
    a = np.clip((1 - r) * size / 3, 0, 1)
    nz = np.sqrt(np.clip(1 - r * r, 0, 1))
    lx, ly, lz = -0.5, -0.6, 0.62
    light = np.clip((x / (size / 2)) * lx + (y / (size / 2)) * ly + nz * lz, 0, 1)
    gray = 0.35 + 0.65 * light + 0.35 * np.exp(-((x + size * 0.14) ** 2 + (y + size * 0.16) ** 2) / 6)
    return fx.from_gray_alpha(np.clip(gray, 0, 1), a)


def planet(size=32):
    x, y = fx.grid(size, size)
    r = np.hypot(x, y) / (size / 2 - 1)
    a = np.clip((1 - r) * size / 3, 0, 1)
    nz = np.sqrt(np.clip(1 - r * r, 0, 1))
    light = np.clip((x / (size / 2)) * -0.55 + (y / (size / 2)) * -0.35 + nz * 0.75, 0, 1)
    bands = 0.82 + 0.18 * np.sin(y / (size / 2) * 9 + 0.6 * np.sin(x / 4))
    gray = (0.25 + 0.75 * light) * bands
    return fx.from_gray_alpha(np.clip(gray, 0, 1), a)


def crystal(w=16, h=32):
    # Pixel art hexagonal shard: light left facet, darker right facet, bright ridge.
    from PIL import Image
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    top, bottom = (w // 2, 0), (w // 2, h - 1)
    left_top, left_bottom = (1, h * 0.28), (2, h * 0.78)
    right_top, right_bottom = (w - 2, h * 0.28), (w - 3, h * 0.78)
    d.polygon([top, left_top, left_bottom, bottom, right_bottom, right_top], fill=(185, 185, 185, 255))
    d.polygon([top, left_top, left_bottom, bottom, (w // 2, h * 0.6)], fill=(245, 245, 245, 255))
    d.polygon([top, (w // 2, h * 0.6), bottom, right_bottom, right_top], fill=(150, 150, 150, 255))
    d.line([top, (w // 2, h * 0.6)], fill=(255, 255, 255, 255))
    return fx.from_pil(im)


def blade(w=16, h=48):
    from PIL import Image
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cx = w // 2
    d.polygon([(cx, 0), (cx + 3, 6), (cx + 3, 33), (cx - 3, 33), (cx - 3, 6)], fill=(200, 200, 200, 255))
    d.polygon([(cx, 0), (cx, 33), (cx - 3, 33), (cx - 3, 6)], fill=(245, 245, 245, 255))
    d.line([(cx, 3), (cx, 32)], fill=(255, 255, 255, 255))
    d.rectangle([cx - 7, 34, cx + 7, 36], fill=(170, 170, 170, 255))
    d.rectangle([cx - 1, 37, cx + 1, 44], fill=(130, 130, 130, 255))
    d.ellipse([cx - 2, 44, cx + 2, 47], fill=(220, 220, 220, 255))
    return fx.from_pil(im)


def flame(w=32, h=64):
    x, y = fx.grid(w, h, cx=(w - 1) / 2, cy=0)
    u = x / (w / 2)
    v = y / (h - 1)  # 0 tip, 1 bottom
    profile = np.clip(np.sin(np.clip((v - 0.02) / 0.9, 0, 1) ** 0.75 * math.pi * 0.5), 0, 1) * (1 - fx.smoothstep(0.86, 0.98, v))
    width = 0.08 + 0.82 * profile
    a = np.exp(-(u / np.maximum(width, 1e-3)) ** 2 * 2.2) * fx.smoothstep(0.0, 0.12, v)
    core = np.exp(-(u / np.maximum(width * 0.45, 1e-3)) ** 2 * 2) * fx.smoothstep(0.35, 0.8, v)
    a = np.clip(a * 0.85 + core * 0.5, 0, 1)
    return fx.from_alpha(a)


def bubble(size=32):
    x, y = fx.grid(size, size)
    d = np.hypot(x, y)
    r = size / 2 - 2
    edge = np.exp(-((d - r) / 1.2) ** 2)
    fill = 0.12 * (d < r)
    highlight = np.exp(-((x + r * 0.42) ** 2 + (y + r * 0.42) ** 2) / 5) * 0.95
    return fx.from_alpha(np.clip(edge + fill + highlight, 0, 1))


def petals(size=16):
    img = fx.canvas(size * 3, size)
    from PIL import Image

    # Sakura petal: rounded, notched tip.
    def sakura(d, f):
        s = size * f
        d.ellipse([s * 0.18, s * 0.12, s * 0.82, s * 0.95], fill=255)
        d.polygon([(s * 0.5, s * 0.25), (s * 0.4, s * 0.05), (s * 0.6, s * 0.05)], fill=0)

    def leaf(d, f):
        s = size * f
        d.polygon([(s * 0.5, s * 0.03), (s * 0.85, s * 0.45), (s * 0.5, s * 0.97), (s * 0.15, s * 0.45)], fill=255)
        d.ellipse([s * 0.16, s * 0.2, s * 0.84, s * 0.8], fill=255)

    def maple(d, f):
        s = size * f
        cx, cy = s * 0.5, s * 0.55
        pts = []
        for k in range(10):
            ang = -math.pi / 2 + k * math.pi / 5
            rad = (0.48 if k % 2 == 0 else 0.2) * s
            if k in (4, 6):
                rad *= 0.8
            pts.append((cx + math.cos(ang) * rad, cy + math.sin(ang) * rad))
        d.polygon(pts, fill=255)

    for i, painter in enumerate((sakura, leaf, maple)):
        a = fx.supersampled_mask(size, size, painter)
        x, y = fx.grid(size, size)
        shade = 0.82 + 0.18 * (-(x + y) / size)
        vein = np.exp(-(x / 0.6) ** 2) * 0.15 if i > 0 else 0
        gray = np.clip(shade - vein + 0.1, 0, 1)
        fx.over(img, fx.from_gray_alpha(gray, a), i * size, 0)
    return img


def snowflake(size=32):
    def paint(d, f):
        c = size * f / 2
        for k in range(6):
            ang = k * math.pi / 3
            ex, ey = c + math.cos(ang) * size * 0.46 * f, c + math.sin(ang) * size * 0.46 * f
            d.line([(c, c), (ex, ey)], fill=255, width=int(1.6 * f))
            for t, l in ((0.45, 0.18), (0.7, 0.13)):
                bx, by = c + math.cos(ang) * size * 0.46 * t * f, c + math.sin(ang) * size * 0.46 * t * f
                for side in (-1, 1):
                    a2 = ang + side * 0.8
                    d.line([(bx, by), (bx + math.cos(a2) * size * l * f, by + math.sin(a2) * size * l * f)], fill=255, width=int(1.3 * f))
        d.ellipse([c - 2 * f, c - 2 * f, c + 2 * f, c + 2 * f], fill=255)

    img = fx.from_alpha(fx.supersampled_mask(size, size, paint))
    return np.clip(img + fx.blur(img, 1.5) * 0.5, 0, 1)


def heart(size=32):
    x, y = fx.grid(size, size)
    u, v = x / (size * 0.3), -(y - size * 0.02) / (size * 0.3)
    f = (u * u + v * v - 1) ** 3 - u * u * v ** 3
    a = np.clip(-f * 25, 0, 1)
    img = fx.from_alpha(a)
    hl = np.exp(-((x + size * 0.12) ** 2 + (y + size * 0.1) ** 2) / 6) * a
    img[..., :3] = np.clip(img[..., :3] * 0.88 + hl[..., None] * 0.12, 0, 1)
    return img


def notes(size=32):
    img = fx.canvas(size * 2, size)

    def eighth(d, f):
        s = size * f
        d.ellipse([s * 0.18, s * 0.66, s * 0.5, s * 0.9], fill=255)
        d.line([(s * 0.48, s * 0.78), (s * 0.48, s * 0.12)], fill=255, width=int(2.2 * f))
        d.line([(s * 0.48, s * 0.12), (s * 0.78, s * 0.38)], fill=255, width=int(2.6 * f))

    def beamed(d, f):
        s = size * f
        d.ellipse([s * 0.08, s * 0.7, s * 0.36, s * 0.92], fill=255)
        d.ellipse([s * 0.56, s * 0.62, s * 0.84, s * 0.84], fill=255)
        d.line([(s * 0.34, s * 0.8), (s * 0.34, s * 0.18)], fill=255, width=int(2.2 * f))
        d.line([(s * 0.82, s * 0.72), (s * 0.82, s * 0.1)], fill=255, width=int(2.2 * f))
        d.line([(s * 0.34, s * 0.18), (s * 0.82, s * 0.1)], fill=255, width=int(3.5 * f))

    for i, painter in enumerate((eighth, beamed)):
        fx.over(img, fx.from_alpha(fx.supersampled_mask(size, size, painter)), i * size, 0)
    return img


def feather(w=32, h=16):
    x, y = fx.grid(w, h, cx=1, cy=(h - 1) / 2)
    u = x / (w - 2)
    v = y / (h / 2)
    prof = np.clip(u * 2.5, 0, 1) ** 0.7 * np.sqrt(np.clip(1 - ((u - 0.55) / 0.46) ** 2, 0, 1)) * 0.85
    a = np.clip((prof - np.abs(v - 0.08 * np.sin(u * 3))) * 9, 0, 1)
    shaft = np.exp(-(v / 0.1) ** 2) * (u < 0.95)
    barbs = 0.85 + 0.15 * np.cos(u * 26 + np.abs(v) * 8)
    return fx.from_gray_alpha(np.clip(barbs - shaft * 0.1, 0, 1), np.clip(a + shaft * 0.5 * (u > 0), 0, 1))


def wing_feathered(size=128, root=(10, 70)):
    """A spread angel wing growing up and to the right from root: an arched arm bone carrying two rows of coverts,
    secondaries hanging below it and long primaries fanning out from the wrist. Coordinates are in texture pixels."""
    big = 3
    img = fx.canvas(size * big, size * big)
    p0 = np.array(root, np.float32)
    p1 = np.array([30, 40], np.float32)
    p2 = np.array([64, 30], np.float32)

    def bone(t):
        return (1 - t) ** 2 * p0 + 2 * (1 - t) * t * p1 + t * t * p2

    def feather_sprite(length, half_width, shade):
        length, half_width = length * big, half_width * big
        w, h = int(length), int(half_width * 2 + 6)
        x, y = fx.grid(w, h, cx=0, cy=(h - 1) / 2)
        u = x / length
        v = y / half_width
        # Narrow quill, wide vane, rounded tip.
        prof = np.clip(u * 3.0, 0, 1) ** 0.5 * np.sqrt(np.clip(1 - ((u - 0.52) / 0.5) ** 2, 0, 1))
        inside = (prof - np.abs(v)) * half_width
        a = np.clip(inside * 0.5, 0, 1)
        edge = np.clip(1 - inside * 0.18, 0, 1) * a
        gray = shade * (0.92 + 0.08 * np.cos(u * 34 + np.abs(v) * 4)) * (1 - 0.4 * edge)
        shaft = np.exp(-(v / 0.07) ** 2) * (u < 0.92)
        return fx.from_gray_alpha(np.clip(gray * (1 - 0.18 * shaft), 0, 1), a)

    def place(spr, base, angle_deg):
        """Puts the feather's quill at base (texture pixels), pointing at angle (degrees, counter-clockwise from +X)."""
        ang = math.radians(angle_deg)
        rot = fx.transform(spr, 1.0, -ang)
        w = spr.shape[1]
        cx = base[0] * big + math.cos(ang) * w / 2
        cy = base[1] * big - math.sin(ang) * w / 2
        rh, rw = rot.shape[:2]
        fx.over(img, rot, int(cx - rw / 2), int(cy - rh / 2))

    for k in range(11):
        t = k / 10
        place(feather_sprite(50 - 10 * abs(t - 0.3), 4.6, 0.8 + 0.1 * t), bone(0.86 + 0.14 * (1 - t)), 30 - 85 * t)
    for k in range(12):
        t = k / 11
        place(feather_sprite(36 + 14 * t, 4.6, 0.86), bone(0.1 + 0.75 * t), -82 + 30 * t)
    for k in range(13):
        t = k / 12
        place(feather_sprite(18 + 5 * t, 3.8, 0.94), bone(0.04 + 0.9 * t), -60 + 34 * t)
    for k in range(13):
        t = k / 12
        place(feather_sprite(10, 3.0, 1.0), bone(0.02 + 0.9 * t), -36 + 22 * t)
    return fx.downsample(img, big)


def wing_crystal(size=128, root=(10, 64)):
    big = 2
    S = size * big
    rx, ry = root[0] * big, root[1] * big

    def paint(d, f):
        upper = [(rx, ry), (S * 0.25, S * 0.18), (S * 0.62, S * 0.04), (S * 0.95, S * 0.1), (S * 0.86, S * 0.32), (S * 0.55, S * 0.48), (rx + 8, ry - 4)]
        lower = [(rx, ry + 4), (S * 0.45, S * 0.58), (S * 0.72, S * 0.75), (S * 0.6, S * 0.92), (S * 0.32, S * 0.86), (rx + 4, ry + 10)]
        d.polygon(upper, fill=150)
        d.polygon(lower, fill=150)
        # Facets.
        for p in ((S * 0.62, S * 0.04), (S * 0.95, S * 0.1), (S * 0.86, S * 0.32), (S * 0.55, S * 0.48), (S * 0.25, S * 0.18)):
            d.line([(rx, ry), p], fill=255, width=2)
        for p in ((S * 0.72, S * 0.75), (S * 0.6, S * 0.92), (S * 0.32, S * 0.86)):
            d.line([(rx, ry + 4), p], fill=255, width=2)
        d.line(upper + [upper[0]], fill=255, width=3)
        d.line(lower + [lower[0]], fill=255, width=3)

    from PIL import Image
    im = Image.new("L", (S, S), 0)
    paint(ImageDraw.Draw(im), 1)
    a = np.asarray(im.resize((size, size), Image.LANCZOS)).astype(np.float32) / 255
    alpha = np.clip(a * 1.3, 0, 1) * 0.8 + 0.2 * (a > 0.05)
    gray = np.clip(0.6 + a * 0.5, 0, 1)
    img = fx.from_gray_alpha(gray, np.clip(alpha, 0, 1))
    return np.clip(img + fx.blur(img, 2) * 0.3, 0, 1)


def butterfly(size=16):
    img = fx.canvas(size * 2, size)

    def open_wings(d, f):
        s = size * f
        d.ellipse([s * 0.05, s * 0.08, s * 0.48, s * 0.55], fill=255)
        d.ellipse([s * 0.52, s * 0.08, s * 0.95, s * 0.55], fill=255)
        d.ellipse([s * 0.15, s * 0.45, s * 0.47, s * 0.88], fill=230)
        d.ellipse([s * 0.53, s * 0.45, s * 0.85, s * 0.88], fill=230)
        d.rectangle([s * 0.46, s * 0.2, s * 0.54, s * 0.85], fill=120)

    def closed_wings(d, f):
        s = size * f
        d.ellipse([s * 0.3, s * 0.05, s * 0.5, s * 0.6], fill=255)
        d.ellipse([s * 0.5, s * 0.05, s * 0.7, s * 0.6], fill=255)
        d.ellipse([s * 0.34, s * 0.45, s * 0.66, s * 0.85], fill=230)
        d.rectangle([s * 0.46, s * 0.2, s * 0.54, s * 0.85], fill=120)

    for i, painter in enumerate((open_wings, closed_wings)):
        from PIL import Image
        big = Image.new("L", (size * 4, size * 4), 0)
        painter(ImageDraw.Draw(big), 4)
        g = np.asarray(big.resize((size, size), Image.LANCZOS)).astype(np.float32) / 255
        a = np.clip(g * 1.6, 0, 1)
        fx.over(img, fx.from_gray_alpha(np.clip(g / np.maximum(a, 1e-3), 0, 1), a), i * size, 0)
    return img


def eye(w=16, h=8):
    x, y = fx.grid(w, h)
    u, v = x / (w / 2), y / (h / 2)
    almond = np.clip((1 - u * u) - np.abs(v) * 1.2, 0, 1)
    a = np.clip(almond * 4, 0, 1)
    pupil = np.exp(-(u / 0.12) ** 2) * (np.abs(v) < 0.9)
    gray = np.clip(1 - pupil * 0.85, 0, 1)
    return fx.from_gray_alpha(gray, a)


def nebula(size=128):
    x, y = fx.grid(size, size)
    r = np.hypot(x, y) / (size / 2)
    n = fx.value_noise(size, size, 3, seed=21, octaves=6)
    a = fx.smoothstep(0.3, 0.85, n) * np.clip(1 - r, 0, 1) ** 1.5 * 1.3
    return fx.from_alpha(np.clip(a, 0, 1))


def pixel():
    return fx.from_alpha(np.ones((4, 4), np.float32))


TEXTURES = {
    "Glow": glow,
    "GlowSharp": glow_sharp,
    "Disc": disc,
    "Ring": ring,
    "RingThin": ring_thin,
    "Star": star,
    "Streak": streak,
    "Smoke": smoke,
    "Disk": disk,
    "Galaxy": galaxy,
    "RuneCircle": rune_circle,
    "Runes": runes,
    "Ray": ray,
    "Orb": orb,
    "Crystal": crystal,
    "Blade": blade,
    "Flame": flame,
    "Bubble": bubble,
    "Petals": petals,
    "Snowflake": snowflake,
    "Heart": heart,
    "Notes": notes,
    "Feather": feather,
    "WingFeathered": wing_feathered,
    "WingCrystal": wing_crystal,
    "Butterfly": butterfly,
    "Eye": eye,
    "Nebula": nebula,
    "Planet": planet,
    "Pixel": pixel,
}


def build():
    os.makedirs(OUT, exist_ok=True)
    out = {}
    for name, fn in TEXTURES.items():
        img = fn()
        fx.save(img, os.path.join(OUT, name + ".png"))
        out[name] = img
    return out


if __name__ == "__main__":
    build()
    print(f"wrote {len(TEXTURES)} textures to {os.path.normpath(OUT)}")
