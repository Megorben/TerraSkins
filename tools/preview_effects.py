"""Renders approximate previews of a few effects, ported from the C# code, to tune sizes and colors.

This mimics FxFrame: positions are in pixels, Glow() is additive, Draw() is alpha blended.
Run: python3 tools/preview_effects.py <out.png>
"""
import math
import os
import sys

import numpy as np

import fxlib as fx

FXDIR = os.path.join(os.path.dirname(__file__), "..", "Assets", "Textures", "FX")
_cache = {}


def tex(name):
    if name not in _cache:
        from PIL import Image
        _cache[name] = fx.from_pil(Image.open(os.path.join(FXDIR, name + ".png")))
    return _cache[name]


def h(a, b=0, c=0):
    v = (a * 0x9E3779B1 ^ b * 0x85EBCA77 ^ c * 0xC2B2AE3D) & 0xFFFFFFFF
    v ^= v >> 15
    v = (v * 0x2C1B3C6D) & 0xFFFFFFFF
    v ^= v >> 12
    v = (v * 0x297A2D39) & 0xFFFFFFFF
    v ^= v >> 15
    return v


def rnd(a, b=0, c=0):
    return (h(a, b, c) & 0xFFFFFF) / 16777216


def cycle(time, index, count, lifetime):
    t = time / lifetime + index / count
    return t - math.floor(t), int(math.floor(t))


def lerp(a, b, t):
    return a + (b - a) * t


def clr(c, k):
    return tuple(min(255, max(0, v * k)) for v in c)


class Frame:
    def __init__(self, img, center, time, direction=1, grav=1, size=(20, 42)):
        self.img, self.center, self.time = img, np.array(center, np.float32), time
        self.dir, self.grav = direction, grav
        self.size = size

    def local(self, x, y):
        return self.center + np.array([x, y * self.grav])

    @property
    def head(self):
        return self.center + np.array([0, -self.size[1] / 2 * self.grav])

    @property
    def feet(self):
        return self.center + np.array([0, self.size[1] / 2 * self.grav])

    def _draw(self, sprite, pos, color, rotation, scale, origin=None, additive=False, src=None, flip=False):
        s = sprite
        if src is not None:
            x, y, w, hh = src
            s = s[y:y + hh, x:x + w]
        # Origin handling: we position the sprite center, so shift by (center - origin) rotated and scaled.
        sh, sw = s.shape[:2]
        if origin is None:
            origin = (sw / 2, sh / 2)
        sx, sy = (scale, scale) if np.isscalar(scale) else scale
        if flip:
            origin = (sw - origin[0], origin[1])
        off = np.array([(sw / 2 - origin[0]) * sx, (sh / 2 - origin[1]) * sy])
        c, si = math.cos(rotation), math.sin(rotation)
        off = np.array([off[0] * c - off[1] * si, off[0] * si + off[1] * c])
        if abs(sx) * sw < 0.5 or abs(sy) * sh < 0.5:
            return
        fx.draw(self.img, s, pos + off, color[:3], 1.0 if len(color) < 4 else color[3], (abs(sx), abs(sy)), rotation, additive, flip_x=flip or sx < 0)

    def glow(self, name, pos, color, rotation=0.0, scale=1.0, origin=None, src=None, flip=False):
        self._draw(tex(name), pos, color, rotation, scale, origin, True, src, flip)

    def drawn(self, name, pos, color, rotation=0.0, scale=1.0, origin=None, src=None, flip=False):
        self._draw(tex(name), pos, color, rotation, scale, origin, False, src, flip)

    def bloom(self, pos, color, radius):
        self.glow("Glow", pos, color, 0, radius * 2 / 128)

    def sparkle(self, pos, color, size, rotation=0.0):
        self.glow("Star", pos, color, rotation, size / 64)
        self.glow("Star", pos, clr((255, 255, 255), 0.8), rotation, size * 0.45 / 64)

    def line(self, a, b, color, thickness):
        d = np.array(b) - np.array(a)
        length = float(np.hypot(*d))
        if length < 0.5:
            return
        ang = math.atan2(d[1], d[0])
        self.glow("Pixel", np.array(a), color, ang, (length / 4, thickness / 4), origin=(0, 2))


# --------------------------------------------------------------------------------------- palettes (from FxPalette.cs)

PAL = {
    "Inferno": [(255, 250, 230), (255, 160, 50), (200, 50, 10), (255, 220, 120)],
    "Violet": [(250, 235, 255), (190, 90, 255), (90, 20, 170), (255, 120, 220)],
    "Holy": [(255, 255, 245), (255, 235, 170), (240, 180, 60), (200, 230, 255)],
    "Starlight": [(255, 255, 255), (170, 200, 255), (80, 90, 220), (255, 230, 160)],
    "Azure": [(235, 250, 255), (80, 190, 255), (20, 70, 200), (140, 255, 240)],
}


def ramp(p, k):
    k = min(1, max(0, k))
    a, b = (p[0], p[1]) if k < 0.5 else (p[1], p[2])
    t = k * 2 if k < 0.5 else (k - 0.5) * 2
    return tuple(lerp(a[i], b[i], t) for i in range(3))


# --------------------------------------------------------------------------------------- black hole (BlackHoleRenderer.cs)

def disk_pos(angle, r, tilt, rot):
    depth = math.sin(angle)
    x, y = math.cos(angle) * r, depth * r * tilt
    c, s = math.cos(rot), math.sin(rot)
    return np.array([x * c - y * s, x * s + y * c]), depth


def black_hole(f, center, radius, p, clumps=56, infall=14, tilt=0.32, rot=-0.14):
    t = f.time
    core = radius * 0.37
    f.bloom(center, clr(p[2], 0.32), radius * 1.7)
    f.bloom(center, clr(p[1], 0.18), radius * 1.05)

    def disk_half(front):
        size = 256
        half = 128
        src = (0, half, 256, half) if front else (0, 0, 256, half)
        origin = (128, 0) if front else (128, half)
        sx = radius * 2 / size
        pulse = 0.9 + 0.1 * math.sin(t * 2.1)
        f.glow("Disk", center, clr(p[2], 0.65), rot, (sx * 1.15, sx * tilt * 1.15), origin, src)
        f.glow("Disk", center, clr(p[1], pulse), rot, (sx, sx * tilt), origin, src)
        f.glow("Disk", center, clr(p[0], 0.6), rot, (sx * 0.72, sx * tilt * 0.72), origin, src)

    def clumps_pass(front):
        for i in range(clumps):
            u = rnd(i, 11)
            r = radius * lerp(0.36, 0.98, math.sqrt(u))
            omega = 1.1 * (radius / r) ** 1.5
            ang = rnd(i, 12) * math.tau + t * omega
            pos, depth = disk_pos(ang, r, tilt, rot)
            if (depth >= 0) != front:
                continue
            heat = (r / radius - 0.36) / 0.62
            beaming = 1 + 0.55 * math.cos(ang)
            color = clr(ramp(p, heat * 0.9 + 0.05), 0.55 * beaming)
            tangent = np.array([-math.sin(ang), math.cos(ang) * tilt])
            c, s = math.cos(rot), math.sin(rot)
            tangent = np.array([tangent[0] * c - tangent[1] * s, tangent[0] * s + tangent[1] * c])
            length = (0.06 + 0.05 * omega) * r / 128 * 3
            size = lerp(2.2, 4.5, rnd(i, 13)) / 16
            f.glow("Streak", center + pos, color, math.atan2(tangent[1], tangent[0]), (length, size * 2.2))

    disk_half(False)
    clumps_pass(False)
    ring_scale = core * 1.28 / (256 * 0.44)
    flick = 0.85 + 0.15 * math.sin(t * 5.3)
    f.glow("Ring", center, clr(p[1], 0.85 * flick), rot, (ring_scale * 1.04, ring_scale))
    f.glow("RingThin", center, clr(p[0], 1.0), 0, core * 1.08 / (256 * 0.47))
    f.drawn("Glow", center, (0, 0, 0, 0.85), 0, core * 2.9 / 128)
    f.drawn("Disc", center, (0, 0, 0, 1), 0, core * 2 / 64)
    disk_half(True)
    clumps_pass(True)
    for i in range(infall):
        age, gen = cycle(t, i, infall, 2.4)
        start_angle = rnd(gen, i, 21) * math.tau
        alpha = math.sin(age * math.pi) * 0.9
        col = tuple(lerp(p[1][k], p[0][k], age) for k in range(3))
        for k in range(4):
            a = max(0.0, age - k * 0.025)
            r = radius * lerp(1.5, 0.38, a * a)
            pos, _ = disk_pos(start_angle + a * 4.2, r, tilt, rot)
            f.glow("GlowSharp", center + pos, clr(col, alpha * (1 - k * 0.22)), 0, (3.2 - k * 0.6) / 64 * 2)


# --------------------------------------------------------------------------------------- wings (WingEffects.cs)

def seraph_wings(f, p):
    anchor = f.local(-3 * f.dir, -7)
    flap = math.sin(f.time * 2.2) * 0.14 + 0.12
    root = (10, 70)
    for side, near in ((-f.dir, True), (f.dir, False)):
        scale = 0.46 if near else 0.4
        alpha = 1 if near else 0.75
        rot = -flap * side
        flip = side < 0
        f.glow("WingFeathered", anchor, clr(p[1], 0.35 * alpha), rot, scale * 1.07, root, flip=flip)
        base = tuple(lerp(p[0][k], p[1][k], 0.3) for k in range(3))
        f.drawn("WingFeathered", anchor, base + (0.82 * alpha,), rot, scale, root, flip=flip)
        f.glow("WingFeathered", anchor, clr(p[0], 0.25 * alpha), rot, scale, root, flip=flip)


def angel_halo(f, p):
    center = f.head + np.array([0, -9 + math.sin(f.time * 2.1) * 1.3])
    pulse = 0.85 + 0.15 * math.sin(f.time * 3)
    f.bloom(center, clr(p[1], 0.35 * pulse), 18)
    f.glow("Ring", center, clr(p[1], 0.9 * pulse), 0, (11 / (256 * 0.44), 3.6 / (256 * 0.44)))
    f.glow("RingThin", center, clr(p[0], pulse), 0, (9.5 / (256 * 0.47), 3 / (256 * 0.47)))


def galaxy(f, p):
    center = f.local(0, -4)
    spin = f.time * 0.22
    f.bloom(center, clr(p[2], 0.35), 62)
    s = 104 / 256
    f.glow("Galaxy", center, clr(p[2], 0.55), spin * 0.8 + 0.5, s * 1.12)
    f.glow("Galaxy", center, clr(p[1], 0.9), spin, s)
    f.glow("Galaxy", center, clr(p[3], 0.45), spin + 1.9, s * 0.72)
    f.bloom(center, clr(p[0], 0.75), 13)
    f.bloom(center, clr(p[1], 0.5), 24)


def player_stub(img, center):
    """A neutral silhouette standing in for the player."""
    x0, y0 = int(center[0] - 10), int(center[1] - 21)
    body = np.zeros((42, 20, 4), np.float32)
    body[2:16, 4:16] = (0.80, 0.62, 0.50, 1)     # head
    body[16:30, 3:17] = (0.25, 0.30, 0.55, 1)    # torso
    body[30:42, 4:16] = (0.20, 0.20, 0.25, 1)    # legs
    fx.over(img, body, x0, y0)


def main(out):
    W, H = 240, 170
    panels = []
    for name, fn, pal in (("black hole", lambda f: black_hole(f, f.local(0, -6), 44, PAL["Inferno"]), "Inferno"),
                          ("violet", lambda f: black_hole(f, f.local(0, -6), 44, PAL["Violet"]), "Violet"),
                          ("galaxy", lambda f: galaxy(f, PAL["Starlight"]), "Starlight"),
                          ("seraph", lambda f: (seraph_wings(f, PAL["Holy"]), angel_halo(f, PAL["Holy"])), "Holy")):
        img = fx.canvas(W, H)
        img[...] = (0.03, 0.025, 0.06, 1)
        center = (W / 2, H / 2 + 12)
        f = Frame(img, center, 3.7)
        fn(f)
        player_stub(img, center)
        if name == "black hole" or name == "violet":
            pass
        panels.append(img)
    sheet = np.concatenate(panels, axis=1)
    from PIL import Image
    pil = fx.to_pil(sheet).resize((sheet.shape[1] * 2, sheet.shape[0] * 2), Image.NEAREST)
    pil.save(out)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "preview.png")
