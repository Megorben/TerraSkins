"""Generates item icons for every effect, the wardrobe UI textures and the mod icon.

Effect icons are composed from the same FX textures and palette colors the effects use in game.
The effect list and palettes are read from the C# sources so they can never drift apart.
Run from the repository root:  python3 tools/gen_icons.py
"""
import math
import os
import re

import numpy as np
from PIL import Image, ImageDraw

import fxlib as fx
import preview_effects as pe

ROOT = os.path.join(os.path.dirname(__file__), "..")
ICONS = os.path.join(ROOT, "Assets", "Textures", "Items", "Effects")
UI = os.path.join(ROOT, "Assets", "Textures", "UI")


# --------------------------------------------------------------------------------------- read the C# sources

def read_palettes():
    src = open(os.path.join(ROOT, "Common", "Effects", "FxPalette.cs")).read()
    pal = {}
    pattern = re.compile(r'public static readonly FxPalette (\w+) = new\("\w+", ' + r", ".join([r"new Color\((\d+), (\d+), (\d+)\)"] * 4) + r"(, Rainbow: true)?\);")
    for m in pattern.finditer(src):
        nums = [int(v) for v in m.groups()[1:13]]
        pal[m.group(1)] = {
            "colors": [tuple(nums[i:i + 3]) for i in range(0, 12, 3)],
            "rainbow": m.group(14) is not None,
        }
    return pal


def read_catalog():
    src = open(os.path.join(ROOT, "Common", "Effects", "Library", "SkinEffectCatalog.cs")).read()
    entries = []
    for m in re.finditer(r'new (\w+)\("(\w+)", Palettes\.(\w+)(.*?)\),\s*$', src, re.M):
        entries.append({"cls": m.group(1), "name": m.group(2), "palette": m.group(3), "args": m.group(4)})
    return entries


PALETTES = read_palettes()


def colors(name, t=0.0):
    p = PALETTES[name]
    c = list(p["colors"])
    if p["rainbow"]:
        import colorsys

        def hsl(h, l):
            r, g, b = colorsys.hls_to_rgb(h % 1, l, 1)
            return (r * 255, g * 255, b * 255)
        c[1], c[2], c[3] = hsl(t, 0.62), hsl(t + 0.18, 0.5), hsl(t + 0.5, 0.7)
    return c


# --------------------------------------------------------------------------------------- icon drawing helpers

S = 64  # icons are drawn at 2x and downsampled


def new_frame(time=2.0):
    img = fx.canvas(S, S)
    return img, pe.Frame(img, (S / 2, S / 2), time, size=(20, 42))


def finish(img, name):
    # Additive glow carries light but no alpha; on an icon that light must become coverage.
    img = img.copy()
    img[..., 3] = np.maximum(img[..., 3], np.clip(img[..., :3].max(axis=2) * 1.1, 0, 1))
    small = fx.downsample(img, 2)
    # Brighten weak glows a little so icons read well in inventory slots.
    small[..., :4] = np.clip(small[..., :4] * 1.15, 0, 1)
    small = fx.outline(small, (18, 10, 32), threshold=0.5)
    fx.save(small, os.path.join(ICONS, name + ".png"))


def C(p, i, k=1.0):
    return pe.clr(p[i], k)


def tex(name):
    return pe.tex(name)


# --------------------------------------------------------------------------------------- per effect icons

def icon_black_hole(f, p, args):
    jets = "jets: true" in args
    if jets:
        for side in (-1, 1):
            f.glow("Ray", (S / 2, S / 2), C(p, 3, 0.7), 0 if side < 0 else math.pi, (10 / 64, 30 / 256), origin=(32, 256))
    pe.black_hole(f, np.array([S / 2, S / 2]), 27, p, clumps=40, infall=8)


def icon_galaxy(f, p, args):
    c = (S / 2, S / 2)
    f.bloom(c, C(p, 2, 0.5), 30)
    f.glow("Galaxy", c, C(p, 1, 1.0), 0.4, 62 / 256)
    f.glow("Galaxy", c, C(p, 3, 0.5), 2.2, 46 / 256)
    f.bloom(c, C(p, 0, 0.9), 7)


def icon_soul_flame(f, p, args):
    f.bloom((S / 2, 44), C(p, 1, 0.45), 20)
    for x, k, sc in ((-11, 0.7, 0.55), (11, 0.7, 0.55), (0, 1.0, 0.85)):
        f.glow("Flame", (S / 2 + x, 34), C(p, 1, k), 0, (sc * 0.8, sc))
        f.glow("Flame", (S / 2 + x, 38), C(p, 0, k * 0.8), 0, (sc * 0.45, sc * 0.6))


def icon_rays(f, p, args):
    c = (S / 2, S / 2)
    f.bloom(c, C(p, 1, 0.4), 28)
    for i in range(12):
        f.glow("Ray", c, C(p, 1 if i % 2 == 0 else 3, 0.8), i * math.tau / 12, (8 / 64, 30 / 256), origin=(32, 256))
    f.bloom(c, C(p, 0, 1.0), 9)
    f.sparkle(c, C(p, 0), 26)


DIPPER = [(-1, -0.35), (-0.62, -0.5), (-0.25, -0.38), (0.08, -0.25), (0.2, 0.18), (0.75, 0.32), (0.82, -0.12)]
DIPPER_LINKS = [(0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 6), (6, 3)]
ORION = [(-0.55, -0.95), (0.5, -0.85), (-0.18, -0.05), (0, 0), (0.18, 0.05), (-0.62, 0.9), (0.6, 0.82), (0, -1.05)]
ORION_LINKS = [(0, 2), (1, 4), (2, 3), (3, 4), (2, 5), (4, 6), (0, 7), (7, 1)]


def icon_constellation(f, p, args):
    stars, links = (ORION, ORION_LINKS) if "Orion" in args else (DIPPER, DIPPER_LINKS)
    f.glow("Nebula", (S / 2, S / 2), C(p, 2, 0.35), 0, 70 / 128)
    pts = [np.array([S / 2 + x * 25, S / 2 + y * 25]) for x, y in stars]
    for a, b in links:
        f.line(pts[a], pts[b], C(p, 1, 0.7), 2.4)
    for q in pts:
        f.bloom(q, C(p, 1, 0.5), 5)
        f.sparkle(q, C(p, 0), 14)


def icon_void(f, p, args):
    dark = tuple(v * 0.25 for v in p[2])
    for i, (x, y, s) in enumerate(((-10, 4, 0.7), (10, 2, 0.75), (0, -8, 0.8), (0, 10, 0.7))):
        f.drawn("Smoke", (S / 2 + x, S / 2 + y), dark + (0.9,), i, s, src=(64 * (i % 4), 0, 64, 64))
    f.bloom((S / 2, S / 2), C(p, 1, 0.6), 18)
    f.glow("Eye", (S / 2, S / 2), C(p, 3), 0, 2.4)
    f.glow("Eye", (S / 2, S / 2), C(p, 0, 0.5), 0, (2.4, 1.6))


def icon_petals(f, p, args):
    kind = int(args.strip(", ")) if args.strip(", ").isdigit() else 0
    for i in range(7):
        ang = i * math.tau / 7
        r = 18 + (i % 2) * 6
        pos = (S / 2 + math.cos(ang) * r, S / 2 + math.sin(ang) * r * 0.8)
        col = p[1] if i % 3 else p[3]
        f.drawn("Petals", pos, col + (1,), ang + 1, 1.5, src=(kind * 16, 0, 16, 16))
    f.drawn("Petals", (S / 2, S / 2), p[1] + (1,), 0.4, 2.2, src=(kind * 16, 0, 16, 16))


def icon_blizzard(f, p, args):
    f.bloom((S / 2, S / 2), C(p, 1, 0.5), 22)
    f.glow("Snowflake", (S / 2, S / 2), C(p, 0), 0.2, 1.5)
    for x, y in ((-20, -18), (20, 16), (18, -20), (-18, 18)):
        f.glow("Snowflake", (S / 2 + x, S / 2 + y), C(p, 1), 0.5, 0.45)


def bolt(f, p, a, b, seed, width=1.0):
    pts = [np.array(a, np.float32)]
    a, b = np.array(a, np.float32), np.array(b, np.float32)
    n = np.array([-(b - a)[1], (b - a)[0]]) / np.linalg.norm(b - a)
    for i in range(1, 6):
        k = i / 6
        pts.append(a + (b - a) * k + n * (pe.rnd(seed, i) - 0.5) * 18)
    pts.append(b)
    for q0, q1 in zip(pts, pts[1:]):
        f.line(q0, q1, C(p, 1, 0.6), 7 * width)
        f.line(q0, q1, C(p, 0), 2.5 * width)


def icon_storm(f, p, args):
    f.bloom((S / 2, S / 2), C(p, 2, 0.5), 24)
    bolt(f, p, (S / 2 - 6, 6), (S / 2 + 4, S - 6), 3)
    bolt(f, p, (S / 2, S / 2), (S - 8, S / 2 + 10), 7, 0.7)


def icon_bubbles(f, p, args):
    f.bloom((S / 2, S / 2 + 8), C(p, 1, 0.4), 24)
    for x, y, s in ((-10, 10, 1.0), (12, -4, 0.75), (-6, -16, 0.55), (14, 18, 0.45)):
        f.drawn("Bubble", (S / 2 + x, S / 2 + y), p[0] + (0.95,), 0, s)
        f.glow("Bubble", (S / 2 + x, S / 2 + y), C(p, 1, 0.5), 0, s * 1.1)


def icon_hearts(f, p, args):
    f.bloom((S / 2, S / 2), C(p, 1, 0.5), 22)
    f.glow("Heart", (S / 2 - 3, S / 2 + 3), C(p, 1), -0.15, 1.35)
    f.glow("Heart", (S / 2 - 3, S / 2 + 3), C(p, 0, 0.45), -0.15, 0.8)
    f.glow("Heart", (S / 2 + 16, S / 2 - 16), C(p, 3), 0.25, 0.6)
    f.sparkle((S / 2 - 18, S / 2 - 16), C(p, 0), 16)


def icon_melody(f, p, args):
    cols = colors("Prism", 0.0)
    f.glow("Notes", (S / 2 + 2, S / 2 + 2), cols[1], 0.1, 1.6, src=(32, 0, 32, 32))
    c2 = colors("Prism", 0.45)[1]
    f.glow("Notes", (S / 2 - 16, S / 2 - 12), c2, -0.2, 0.9, src=(0, 0, 32, 32))
    f.sparkle((S / 2 + 18, S / 2 - 18), (255, 255, 255), 14)


def icon_nebula(f, p, args):
    for i, (x, y) in enumerate(((-8, -6), (8, 4), (0, 10), (6, -10))):
        col = p[3] if i % 2 else p[1]
        f.glow("Nebula", (S / 2 + x, S / 2 + y), pe.clr(col, 0.7), i, 44 / 128)
    for x, y in ((-14, 12), (16, -14), (4, -2)):
        f.sparkle((S / 2 + x, S / 2 + y), C(p, 0), 12)


def halo_ring(f, c, p, rx, ry, k=1.0):
    f.glow("Ring", c, C(p, 1, 0.95 * k), 0, (rx / (256 * 0.44), ry / (256 * 0.44)))
    f.glow("RingThin", c, C(p, 0, k), 0, (rx * 0.86 / (256 * 0.47), ry * 0.86 / (256 * 0.47)))


def icon_angel_halo(f, p, args):
    c = (S / 2, S / 2 + 2)
    f.bloom(c, C(p, 1, 0.45), 26)
    halo_ring(f, c, p, 26, 9)
    f.sparkle((S / 2 + 20, S / 2 - 4), C(p, 0), 16)


def icon_fallen_halo(f, p, args):
    c = (S / 2, S / 2 + 6)
    f.bloom(c, C(p, 2, 0.6), 24)
    for i in range(7):
        ang = math.pi + i * math.pi / 6
        pos = (c[0] + math.cos(ang) * 24, c[1] + math.sin(ang) * 8)
        f.glow("Flame", (pos[0], pos[1] - 4), C(p, 1, 0.9), 0, (0.3, 0.38), origin=(16, 51))
    f.drawn("Ring", c, (0, 0, 0, 0.95), 0, (27 / 112.6, 10 / 112.6))
    f.glow("RingThin", c, C(p, 1, 0.8), 0, (23 / 120, 8 / 120))


def icon_singularity(f, p, args):
    pe.black_hole(f, np.array([S / 2, S / 2 + 2]), 24, p, clumps=30, infall=6, tilt=0.36)


def icon_star_crown(f, p, args):
    c = (S / 2, S / 2 + 4)
    f.glow("RingThin", c, C(p, 1, 0.6), 0, (26 / 120, 9 / 120))
    for i in range(7):
        ang = i * math.tau / 7 + 0.3
        depth = math.sin(ang)
        f.sparkle((c[0] + math.cos(ang) * 24, c[1] + depth * 8), C(p, 0, 0.7 + 0.3 * depth), 16 + 6 * depth)


def icon_rune_halo(f, p, args):
    c = (S / 2, S / 2 + 4)
    f.bloom(c, C(p, 1, 0.4), 22)
    f.glow("RingThin", c, C(p, 1, 0.8), 0, (27 / 120, 10 / 120))
    for i in range(8):
        ang = i * math.tau / 8
        depth = math.sin(ang)
        facing = max(0.3, abs(depth))
        f.glow("Runes", (c[0] + math.cos(ang) * 27, c[1] + depth * 10), C(p, 0, 0.6 + 0.4 * depth), 0, (0.9 * facing, 0.9), src=(16 * (i % 8), 0, 16, 16))


def icon_planet(f, p, args):
    c = (S / 2, S / 2)
    ring_scale = (30 / 112.6, 10 / 112.6)
    f.glow("Ring", c, C(p, 3, 0.9), -0.32, ring_scale, origin=(128, 128), src=(0, 0, 256, 128))
    f.drawn("Planet", c, p[1] + (1,), 0.3, 1.2)
    f.glow("Ring", c, C(p, 3, 1.0), -0.32, ring_scale, origin=(128, 0), src=(0, 128, 256, 128))
    f.drawn("Orb", (S / 2 + 22, S / 2 - 16), p[0] + (1,), 0, 0.35)


def icon_flame_crown(f, p, args):
    c = (S / 2, S / 2 + 8)
    f.bloom(c, C(p, 1, 0.45), 22)
    for i in range(9):
        ang = math.pi + i * math.pi / 8
        depth = math.sin(ang + math.pi)
        pos = (c[0] + math.cos(ang) * 22, c[1] + math.sin(ang) * 6 - 4)
        f.glow("Flame", pos, C(p, 1, 0.9), 0, (0.32, 0.42), origin=(16, 51))
        f.glow("Flame", pos, C(p, 0, 0.6), 0, (0.18, 0.26), origin=(16, 51))


def orbit_path(f, p, rx=26, ry=9, tilt=-0.25):
    c = np.array([S / 2, S / 2])
    for k in range(28):
        ang = k * math.tau / 28
        pos, _ = pe.disk_pos(ang, rx, ry / rx, tilt)
        f.bloom(c + pos, C(p, 1, 0.22), 2.5)


def icon_star_orbit(f, p, args):
    orbit_path(f, p)
    c = np.array([S / 2, S / 2])
    for i, ang in enumerate((0.5, 2.6, 4.6)):
        pos, depth = pe.disk_pos(ang, 26, 9 / 26, -0.25)
        col = colors("Prism", i / 3)[1] if "Prism" in args or PALETTES[current_palette]["rainbow"] else p[1]
        f.bloom(c + pos, pe.clr(col, 0.6), 9)
        f.sparkle(c + pos, col, 22 + 5 * depth)


def icon_crystal_orbit(f, p, args):
    orbit_path(f, p)
    c = np.array([S / 2, S / 2])
    for ang in (0.6, 2.7, 4.7):
        pos, depth = pe.disk_pos(ang, 24, 9 / 24, -0.25)
        f.bloom(c + pos, C(p, 1, 0.4), 8)
        f.drawn("Crystal", c + pos, tuple(pe.lerp(p[1][k], p[0][k], 0.25) for k in range(3)) + (1,), 0.2, 0.75 + 0.15 * depth)


def icon_moons(f, p, args):
    orbit_path(f, p, 27, 10)
    f.drawn("Planet", (S / 2 - 10, S / 2 + 4), tuple(pe.lerp(p[0][k], p[1][k], 0.2) for k in range(3)) + (1,), 0.2, 0.8)
    f.drawn("Planet", (S / 2 + 16, S / 2 - 10), p[3] + (1,), 0.5, 0.5)


def icon_wisps(f, p, args):
    for i, ang0 in enumerate((0.3, 3.4)):
        for k in range(10):
            ang = ang0 - k * 0.12
            pos = (S / 2 + math.cos(ang) * 22, S / 2 + math.sin(ang) * 14)
            f.glow("GlowSharp", pos, C(p, 1, 0.8 * (1 - k / 10)), 0, (12 - k) / 64 * 2)
        pos = (S / 2 + math.cos(ang0) * 22, S / 2 + math.sin(ang0) * 14)
        f.bloom(pos, C(p, 1, 0.6), 9)
        f.bloom(pos, C(p, 0), 4)


def icon_blades(f, p, args):
    for i in range(4):
        ang = i * math.tau / 4 + 0.4
        pos = (S / 2 + math.cos(ang) * 18, S / 2 + math.sin(ang) * 18)
        f.glow("Blade", pos, C(p, 1, 0.7), ang + math.pi / 2, 1.0)
        f.drawn("Blade", pos, tuple(pe.lerp(p[0][k], p[1][k], 0.35) for k in range(3)) + (0.95,), ang + math.pi / 2, 0.85)


def icon_atom(f, p, args):
    c = np.array([S / 2, S / 2])
    for i in range(3):
        rot = i * math.pi / 3 + 0.3
        for k in range(30):
            pos, _ = pe.disk_pos(k * math.tau / 30, 27, 8 / 27, rot)
            f.bloom(c + pos, C(p, 1, 0.75), 3)
        pos, _ = pe.disk_pos(1.2 + i * 2, 27, 8 / 27, rot)
        f.sparkle(c + pos, C(p, 0), 16)
    f.bloom(c, C(p, 1, 0.8), 8)


def icon_butterfly(f, p, args):
    f.bloom((S / 2, S / 2), C(p, 3, 0.4), 22)
    f.drawn("Butterfly", (S / 2, S / 2), p[1] + (1,), 0.15, 3.0, src=(0, 0, 16, 16))
    f.glow("Butterfly", (S / 2, S / 2), C(p, 0, 0.25), 0.15, 3.0, src=(0, 0, 16, 16))


def comet_positions(n=8):
    for i in range(n):
        k = i / (n - 1)
        yield k, (14 + k * 38, 50 - k * 38)


def icon_stardust(f, p, args):
    for k, pos in comet_positions():
        col = p[3] if int(k * 7) % 2 else p[1]
        f.sparkle(pos, pe.clr(col, 0.4 + 0.6 * k), 8 + 16 * k, k)
    f.bloom((52, 12), C(p, 0, 0.8), 8)


def icon_ribbon(f, p, args):
    prev = None
    for i in range(30):
        k = i / 29
        pos = np.array([8 + k * 48, S / 2 + math.sin(k * 5) * 12])
        if prev is not None:
            col = colors(current_palette, k * 0.9)[1]
            f.line(prev, pos, pe.clr(col, 0.9), 9 * (0.4 + 0.6 * k))
            f.line(prev, pos, pe.clr((255, 255, 255), 0.5), 3 * (0.4 + 0.6 * k))
        prev = pos


def icon_flame_trail(f, p, args):
    # A fireball streaking up and to the right, embers trailing behind.
    for k, pos in comet_positions(6):
        f.glow("GlowSharp", pos, pe.clr(pe.ramp(p, 1 - k), 0.35 + 0.5 * k), 0, (4 + 10 * k) / 64 * 2)
    head = (50, 14)
    f.glow("Flame", (head[0] - 6, head[1] + 6), C(p, 1), -2.35, (0.9, 1.0), origin=(16, 51))
    f.glow("Flame", (head[0] - 6, head[1] + 6), C(p, 0, 0.8), -2.35, (0.45, 0.6), origin=(16, 51))
    f.bloom(head, C(p, 0), 7)


def icon_void_trail(f, p, args):
    dark = tuple(v * 0.3 for v in p[2])
    for k, pos in comet_positions(6):
        f.drawn("Smoke", pos, dark + (0.85,), k * 3, 0.35 + 0.35 * k, src=(64 * int(k * 5 % 4), 0, 64, 64))
        f.bloom(pos, C(p, 3, 0.4 * k), 5)
    f.sparkle((50, 14), C(p, 3), 16)


def icon_petal_trail(f, p, args):
    kind = int(args.strip(", ")) if args.strip(", ").isdigit() else 0
    for k, pos in comet_positions(6):
        f.drawn("Petals", (pos[0], pos[1] + (6 if int(k * 5) % 2 else -4)), (p[1] if int(k * 5) % 2 else p[3]) + (0.5 + 0.5 * k,), k * 4, 1.2 + 0.6 * k, src=(kind * 16, 0, 16, 16))


def icon_lightning(f, p, args):
    f.bloom((S / 2, S / 2), C(p, 2, 0.5), 22)
    bolt(f, p, (10, 50), (54, 14), 11)


def icon_frost_trail(f, p, args):
    for k, pos in comet_positions(6):
        if int(k * 5) % 2:
            f.drawn("Crystal", pos, p[1] + (0.6 + 0.4 * k,), k * 2, 0.5 + 0.3 * k)
        else:
            f.glow("Snowflake", pos, C(p, 0, 0.5 + 0.5 * k), k, 0.3 + 0.35 * k)


def icon_bubble_trail(f, p, args):
    for k, pos in comet_positions(5):
        f.drawn("Bubble", pos, p[0] + (0.9,), 0, 0.3 + 0.45 * k)
        f.glow("Bubble", pos, C(p, 1, 0.4), 0, (0.3 + 0.45 * k) * 1.1)


def ghost_sprite():
    w, h = 20, 42
    a = np.zeros((h, w), np.float32)
    a[2:16, 4:16] = 1
    a[16:30, 3:17] = 1
    a[30:42, 4:9] = 1
    a[30:42, 11:16] = 1
    return fx.from_alpha(a)


def icon_afterimage(f, p, args):
    g = ghost_sprite()
    for i, (x, k) in enumerate(((-16, 0.3), (-7, 0.55), (4, 1.0))):
        col = colors(current_palette, i * 0.3)[1]
        s = fx.tint(fx.transform(g, 1.1), col, k * 0.85, additive=i < 2)
        fx.over(f.img, s, int(S / 2 + x - 11), int(S / 2 - 23))


def wing_pair(f, p, name, root, scale, base_alpha=0.85, additive_only=False):
    anchor = (S / 2, S / 2 + 4)
    for side in (-1, 1):
        flip = side < 0
        f.glow(name, anchor, C(p, 1, 0.4), -0.15 * side, scale * 1.08, origin=root, flip=flip)
        if not additive_only:
            base = tuple(pe.lerp(p[0][k], p[1][k], 0.3) for k in range(3))
            f.drawn(name, anchor, base + (base_alpha,), -0.15 * side, scale, origin=root, flip=flip)
        f.glow(name, anchor, C(p, 0, 0.3 if not additive_only else 0.8), -0.15 * side, scale, origin=root, flip=flip)


def icon_seraph(f, p, args):
    wing_pair(f, p, "WingFeathered", (10, 70), 0.28)


def icon_tendrils(f, p, args):
    dark = tuple(v * 0.3 for v in p[2])
    anchor = np.array([S / 2, S / 2 + 6])
    for side in (-1, 1):
        for j in range(4):
            ang = -0.2 + j * 0.45
            pos = anchor.copy()
            for k in range(8):
                ang += 0.08 + math.sin(k * 0.6 + j) * 0.12
                pos = pos + np.array([math.cos(ang) * side, -math.sin(ang)]) * 3.6
                r = pe.lerp(4, 1.2, k / 7)
                f.drawn("Disc", pos, dark + (1,), 0, r * 2 / 64)
                f.glow("GlowSharp", pos, C(p, 1, 0.4), 0, r * 3 / 64)
            f.sparkle(pos, C(p, 3), 10)


def icon_crystal_wings(f, p, args):
    wing_pair(f, p, "WingCrystal", (10, 64), 0.24, 0.7)


def icon_phoenix(f, p, args):
    wing_pair(f, p, "WingFeathered", (10, 70), 0.28, additive_only=True)
    for x, y in ((-18, -8), (16, -12), (-8, -18), (8, -4)):
        f.glow("Flame", (S / 2 + x, S / 2 + y), C(p, 1, 0.8), 0, (0.22, 0.28), origin=(16, 51))


def icon_constellation_wings(f, p, args):
    anchor = np.array([S / 2, S / 2 + 6])
    for side in (-1, 1):
        tips = []
        for ang, length in ((0.25, 26), (0.7, 30), (1.15, 22)):
            prev = anchor
            for k in range(1, 4):
                pos = anchor + np.array([math.cos(ang) * side, -math.sin(ang)]) * length * k / 3
                f.line(prev, pos, C(p, 1, 0.6), 1.8)
                f.sparkle(pos, C(p, 0), 8 + 3 * k)
                prev = pos
            tips.append(prev)
        for a, b in zip(tips, tips[1:]):
            f.line(a, b, C(p, 3, 0.6), 1.5)


def icon_magic_circle(f, p, args):
    c = (S / 2, S / 2 + 6)
    f.glow("Glow", c, C(p, 1, 0.45), 0, (60 / 128, 22 / 128))
    f.glow("RuneCircle", c, C(p, 1, 1.0), 0, (60 / (256 * 0.94), 24 / (256 * 0.94)))
    for i in range(5):
        f.sparkle((S / 2 - 18 + i * 9, S / 2 - 6 - (i % 2) * 8), C(p, 3, 0.8), 9)


def icon_footsteps(f, p, args):
    if "Frost" in args:
        for k, rot in ((-1, -0.45), (0, 0), (1, 0.45)):
            f.drawn("Crystal", (S / 2 + k * 6, S / 2 + 4), p[1] + (1,), rot, 0.9 if k == 0 else 0.65, origin=(8, 32))
        f.glow("Glow", (S / 2, S / 2 + 20), C(p, 1, 0.5), 0, (50 / 128, 14 / 128))
        f.sparkle((S / 2 + 12, S / 2 - 12), C(p, 0), 16)
        return
    c = np.array([S / 2, S / 2])
    for k in range(5):
        ang = k * math.tau / 5
        off = np.array([math.cos(ang), math.sin(ang)]) * 9
        f.drawn("Petals", c + off, p[1] + (1,), ang + math.pi / 2, 1.6, src=(0, 0, 16, 16))
    f.drawn("Disc", c, p[3] + (1,), 0, 10 / 64)
    f.sparkle(c + np.array([14, -16]), C(p, 0), 14)


def icon_shadow_pool(f, p, args):
    c = (S / 2, S / 2 + 10)
    f.drawn("Glow", c, (0, 0, 0, 0.95), 0, (62 / 128, 24 / 128))
    f.glow("Ring", c, C(p, 1, 0.7), 0, (24 / 112.6, 8 / 112.6))
    dark = tuple(v * 0.4 for v in p[2])
    for i, x in enumerate((-10, 0, 10)):
        f.drawn("Smoke", (S / 2 + x, S / 2 - 6 - (i % 2) * 6), dark + (0.8,), i, 0.35, src=(64 * i, 0, 64, 64))
    f.sparkle((S / 2 + 14, S / 2 - 14), C(p, 3), 12)


def icon_sunbeam(f, p, args):
    f.glow("Glow", (S / 2, S - 12), C(p, 1, 0.6), 0, (54 / 128, 16 / 128))
    for i in range(3):
        f.glow("Ray", (S / 2, S - 12), C(p, 0 if i == 2 else 1, 0.6), 0, ((20 - i * 5) / 64, 54 / 256), origin=(32, 256))
    for x, y in ((-8, 20), (6, 34), (0, 46)):
        f.sparkle((S / 2 + x, S - y), C(p, 0), 10)


DRAWERS = {
    "BlackHoleAura": icon_black_hole,
    "GalaxyAura": icon_galaxy,
    "SoulFlameAura": icon_soul_flame,
    "DivineRaysAura": icon_rays,
    "ConstellationAura": icon_constellation,
    "VoidAura": icon_void,
    "PetalStormAura": icon_petals,
    "BlizzardAura": icon_blizzard,
    "StormAura": icon_storm,
    "BubbleAura": icon_bubbles,
    "HeartsAura": icon_hearts,
    "MelodyAura": icon_melody,
    "NebulaAura": icon_nebula,
    "AngelHalo": icon_angel_halo,
    "FallenHalo": icon_fallen_halo,
    "SingularityHalo": icon_singularity,
    "StarCrownHalo": icon_star_crown,
    "RuneHalo": icon_rune_halo,
    "PlanetHalo": icon_planet,
    "FlameCrownHalo": icon_flame_crown,
    "StarOrbit": icon_star_orbit,
    "CrystalOrbit": icon_crystal_orbit,
    "MoonOrbit": icon_moons,
    "WispOrbit": icon_wisps,
    "BladeOrbit": icon_blades,
    "AtomOrbit": icon_atom,
    "ButterflyOrbit": icon_butterfly,
    "StardustTrail": icon_stardust,
    "RibbonTrail": icon_ribbon,
    "FlameTrail": icon_flame_trail,
    "VoidTrail": icon_void_trail,
    "PetalTrail": icon_petal_trail,
    "LightningTrail": icon_lightning,
    "FrostTrail": icon_frost_trail,
    "BubbleTrail": icon_bubble_trail,
    "AfterimageTrail": icon_afterimage,
    "SeraphWings": icon_seraph,
    "VoidTendrilWings": icon_tendrils,
    "CrystalWings": icon_crystal_wings,
    "PhoenixWings": icon_phoenix,
    "ConstellationWings": icon_constellation_wings,
    "MagicCircleGround": icon_magic_circle,
    "FootstepGround": icon_footsteps,
    "ShadowPoolGround": icon_shadow_pool,
    "SunbeamGround": icon_sunbeam,
}

current_palette = None


def build_effect_icons():
    global current_palette
    os.makedirs(ICONS, exist_ok=True)
    entries = read_catalog()
    for e in entries:
        current_palette = e["palette"]
        p = colors(e["palette"])
        img, f = new_frame()
        DRAWERS[e["cls"]](f, p, e["args"])
        finish(img, e["name"])
    return entries


# --------------------------------------------------------------------------------------- UI textures

LAV = (225, 215, 255)
GOLD = (255, 214, 120)


def vector_icon(size, painter, color=LAV, factor=4):
    a = fx.supersampled_mask(size, size, painter, factor)
    return fx.from_alpha(a, tuple(c / 255 for c in color))


def slot_icons():
    s = 24

    def hanger(d, f):
        k = s * f / 24
        d.arc([10 * k, 3 * k, 14 * k, 7 * k], 180, 90, fill=255, width=int(1.6 * k))
        d.line([(12 * k, 7 * k), (12 * k, 9 * k)], fill=255, width=int(1.6 * k))
        d.line([(12 * k, 9 * k), (3 * k, 17 * k), (21 * k, 17 * k), (12 * k, 9 * k)], fill=255, width=int(1.8 * k), joint="curve")

    def helmet(d, f):
        k = s * f / 24
        d.chord([4 * k, 3 * k, 20 * k, 22 * k], 180, 360, fill=255)
        d.rectangle([4 * k, 12 * k, 20 * k, 19 * k], fill=255)
        d.rectangle([9 * k, 12 * k, 19 * k, 14 * k], fill=0)
        d.rectangle([11 * k, 14 * k, 13 * k, 19 * k], fill=0)

    def chest(d, f):
        k = s * f / 24
        d.polygon([(3 * k, 5 * k), (9 * k, 3 * k), (12 * k, 6 * k), (15 * k, 3 * k), (21 * k, 5 * k), (19 * k, 11 * k), (18 * k, 21 * k), (6 * k, 21 * k), (5 * k, 11 * k)], fill=255)
        d.line([(12 * k, 8 * k), (12 * k, 19 * k)], fill=0, width=int(1.2 * k))

    def legs(d, f):
        k = s * f / 24
        d.rectangle([6 * k, 3 * k, 18 * k, 7 * k], fill=255)
        d.polygon([(6 * k, 7 * k), (11 * k, 7 * k), (11 * k, 19 * k), (12 * k, 21 * k), (5 * k, 21 * k), (6 * k, 19 * k)], fill=255)
        d.polygon([(13 * k, 7 * k), (18 * k, 7 * k), (18 * k, 19 * k), (19 * k, 21 * k), (12 * k, 21 * k), (13 * k, 19 * k)], fill=255)

    def wings(d, f):
        k = s * f / 24
        for side in (-1, 1):
            cx = 12 * k
            pts = [(cx, 12 * k), (cx + side * 10 * k, 3 * k), (cx + side * 11 * k, 9 * k), (cx + side * 9 * k, 14 * k), (cx + side * 6 * k, 19 * k)]
            d.polygon(pts, fill=255)
        d.ellipse([10 * k, 9 * k, 14 * k, 16 * k], fill=255)

    def cape(d, f):
        k = s * f / 24
        d.polygon([(7 * k, 3 * k), (17 * k, 3 * k), (21 * k, 21 * k), (16 * k, 19 * k), (12 * k, 22 * k), (8 * k, 19 * k), (3 * k, 21 * k)], fill=255)
        d.ellipse([9 * k, 2 * k, 15 * k, 6 * k], fill=0)

    def mask(d, f):
        k = s * f / 24
        d.ellipse([3 * k, 6 * k, 21 * k, 19 * k], fill=255)
        d.ellipse([6 * k, 10 * k, 10.5 * k, 13.5 * k], fill=0)
        d.ellipse([13.5 * k, 10 * k, 18 * k, 13.5 * k], fill=0)

    def neck(d, f):
        k = s * f / 24
        d.arc([4 * k, 1 * k, 20 * k, 15 * k], 20, 160, fill=255, width=int(2 * k))
        d.polygon([(12 * k, 13 * k), (16 * k, 17 * k), (12 * k, 22 * k), (8 * k, 17 * k)], fill=255)

    def glove(d, f):
        k = s * f / 24
        d.rounded_rectangle([7 * k, 9 * k, 18 * k, 21 * k], radius=3 * k, fill=255)
        for i, x in enumerate((7, 10, 13, 16)):
            d.rounded_rectangle([x * k, (3 + (i % 3)) * k, (x + 2.6) * k, 12 * k], radius=1.2 * k, fill=255)
        d.rounded_rectangle([3 * k, 11 * k, 8 * k, 15 * k], radius=1.5 * k, fill=255)

    def belt(d, f):
        k = s * f / 24
        d.rectangle([2 * k, 9 * k, 22 * k, 15 * k], fill=255)
        d.rectangle([9 * k, 7 * k, 15 * k, 17 * k], outline=255, width=int(1.8 * k), fill=0)

    def boot(d, f):
        k = s * f / 24
        d.polygon([(7 * k, 3 * k), (14 * k, 3 * k), (14 * k, 14 * k), (21 * k, 17 * k), (21 * k, 21 * k), (5 * k, 21 * k), (6 * k, 14 * k)], fill=255)

    def shield(d, f):
        k = s * f / 24
        d.polygon([(12 * k, 2 * k), (20 * k, 5 * k), (19 * k, 14 * k), (12 * k, 22 * k), (5 * k, 14 * k), (4 * k, 5 * k)], fill=255)
        d.polygon([(12 * k, 6 * k), (16 * k, 8 * k), (15.5 * k, 13 * k), (12 * k, 17 * k)], fill=0)

    def balloon(d, f):
        k = s * f / 24
        d.ellipse([5 * k, 2 * k, 19 * k, 17 * k], fill=255)
        d.polygon([(11 * k, 16.5 * k), (13 * k, 16.5 * k), (12 * k, 18.5 * k)], fill=255)
        d.line([(12 * k, 18 * k), (11 * k, 22 * k)], fill=255, width=int(1.2 * k))

    def aura(d, f):
        k = s * f / 24
        for i, r in enumerate((10, 7, 4)):
            d.arc([(12 - r) * k, (12 - r) * k, (12 + r) * k, (12 + r) * k], 30 + i * 60, 300 + i * 60, fill=255, width=int(1.8 * k))
        d.ellipse([10.5 * k, 10.5 * k, 13.5 * k, 13.5 * k], fill=255)

    def halo(d, f):
        k = s * f / 24
        d.ellipse([3 * k, 4 * k, 21 * k, 11 * k], outline=255, width=int(2.2 * k))
        d.ellipse([8 * k, 13 * k, 16 * k, 22 * k], fill=255)

    def orbit(d, f):
        k = s * f / 24
        d.ellipse([2 * k, 7 * k, 22 * k, 17 * k], outline=255, width=int(1.5 * k))
        d.ellipse([8.5 * k, 8.5 * k, 15.5 * k, 15.5 * k], fill=255)
        d.ellipse([17 * k, 12 * k, 22 * k, 17 * k], fill=255)

    def trail(d, f):
        k = s * f / 24
        d.ellipse([14 * k, 3 * k, 21 * k, 10 * k], fill=255)
        for i in range(4):
            r = 2.6 - i * 0.5
            cx, cy = (13 - i * 3) * k, (11 + i * 3) * k
            d.ellipse([cx - r * k, cy - r * k, cx + r * k, cy + r * k], fill=255)

    def ground(d, f):
        k = s * f / 24
        d.ellipse([2 * k, 13 * k, 22 * k, 21 * k], outline=255, width=int(1.6 * k))
        d.ellipse([7 * k, 15 * k, 17 * k, 19 * k], outline=255, width=int(1.2 * k))
        for x in (7, 12, 17):
            d.line([(x * k, 12 * k), (x * k, 6 * k)], fill=255, width=int(1.2 * k))

    painters = [hanger, helmet, chest, legs, wings, cape, mask, neck, glove, belt, boot, shield, balloon, aura, halo, orbit, trail, ground]
    img = fx.canvas(s * len(painters), s)
    for i, painter in enumerate(painters):
        color = GOLD if i == 0 else (LAV if i < 13 else (200, 170, 255))
        fx.over(img, vector_icon(s, painter, color), i * s, 0)
    return img


def cell_icons():
    s = 32

    def keep_own(d, f):
        k = s * f / 32
        d.ellipse([12 * k, 5 * k, 20 * k, 13 * k], fill=255)
        d.rounded_rectangle([10 * k, 14 * k, 22 * k, 27 * k], radius=3 * k, fill=255)
        d.arc([3 * k, 3 * k, 29 * k, 29 * k], 200, 330, fill=255, width=int(2 * k))
        d.polygon([(27 * k, 7 * k), (30 * k, 14 * k), (23 * k, 13 * k)], fill=255)

    def hide(d, f):
        k = s * f / 32
        d.ellipse([4 * k, 10 * k, 28 * k, 22 * k], outline=255, width=int(2 * k))
        d.ellipse([12 * k, 12 * k, 20 * k, 20 * k], fill=255)
        d.line([(6 * k, 26 * k), (26 * k, 6 * k)], fill=255, width=int(2.6 * k))

    def no_dye(d, f):
        k = s * f / 32
        d.polygon([(16 * k, 4 * k), (24 * k, 17 * k), (8 * k, 17 * k)], fill=255)
        d.ellipse([8 * k, 11 * k, 24 * k, 27 * k], fill=255)
        d.line([(6 * k, 27 * k), (26 * k, 6 * k)], fill=0, width=int(4.5 * k))
        d.line([(6 * k, 27 * k), (26 * k, 6 * k)], fill=255, width=int(2 * k))

    def save(d, f):
        k = s * f / 32
        d.ellipse([4 * k, 4 * k, 28 * k, 28 * k], outline=255, width=int(2.2 * k))
        d.line([(16 * k, 9 * k), (16 * k, 23 * k)], fill=255, width=int(2.8 * k))
        d.line([(9 * k, 16 * k), (23 * k, 16 * k)], fill=255, width=int(2.8 * k))

    img = fx.canvas(s * 4, s)
    for i, (painter, color) in enumerate(((keep_own, LAV), (hide, (255, 170, 190)), (no_dye, LAV), (save, GOLD))):
        fx.over(img, vector_icon(s, painter, color), i * s, 0)
    return img


def wardrobe_toggle():
    s = 14
    # A tiny golden hanger with a sparkle.

    def hanger(d, f):
        k = s * f / 14
        d.arc([5.5 * k, 1 * k, 8.5 * k, 4 * k], 180, 90, fill=255, width=int(1.2 * k))
        d.line([(7 * k, 4 * k), (7 * k, 5.5 * k)], fill=255, width=int(1.2 * k))
        d.line([(7 * k, 5.5 * k), (1 * k, 11 * k), (13 * k, 11 * k), (7 * k, 5.5 * k)], fill=255, width=int(1.4 * k), joint="curve")

    img = vector_icon(s, hanger, GOLD)
    return img


def preview_background(w=196, h=400):
    x, y = fx.grid(w, h, cx=w / 2, cy=0)
    v = y / h
    img = fx.canvas(w, h)
    top = np.array([0.06, 0.04, 0.13])
    bottom = np.array([0.16, 0.09, 0.24])
    for i in range(3):
        img[..., i] = top[i] + (bottom[i] - top[i]) * v
    img[..., 3] = 1
    rng = np.random.default_rng(4)
    for _ in range(70):
        sx, sy = rng.random() * w, rng.random() * h * 0.62
        b = rng.random() ** 2
        d2 = (x + w / 2 - sx) ** 2 + (y - sy) ** 2
        star = np.exp(-d2 / 0.6) * b
        img[..., :3] += star[..., None] * np.array([0.9, 0.9, 1.0])
    # Floor glow under the feet (at 70% height).
    fy = h * 0.7
    floor = np.exp(-((x / (w * 0.42)) ** 2 + ((y - fy) / 10) ** 2) * 2.5)
    img[..., :3] += floor[..., None] * np.array([0.35, 0.25, 0.55])
    vignette = np.clip(1 - ((x / (w * 0.7)) ** 2 + ((v - 0.5) / 0.8) ** 2) * 0.5, 0.4, 1)
    img[..., :3] *= vignette[..., None]
    img[..., :3] = np.clip(img[..., :3], 0, 1)
    return img


def mod_icon(size=80):
    img = fx.canvas(size, size)
    x, y = fx.grid(size, size)
    r = np.hypot(x, y) / (size / 2)
    img[..., 0] = 0.07 + 0.08 * (1 - r)
    img[..., 1] = 0.04 + 0.04 * (1 - r)
    img[..., 2] = 0.14 + 0.12 * (1 - r)
    img[..., 3] = 1
    rng = np.random.default_rng(9)
    for _ in range(40):
        sx, sy, b = rng.random() * size, rng.random() * size, rng.random() ** 2
        img[..., :3] += np.exp(-((x + size / 2 - sx) ** 2 + (y + size / 2 - sy) ** 2) / 0.5)[..., None] * b
    f = pe.Frame(img, (size / 2, size / 2), 1.3)
    pe.black_hole(f, np.array([size / 2, size / 2 + 2]), 33, colors("Inferno"), clumps=70, infall=12)
    f.sparkle((size * 0.8, size * 0.2), (255, 240, 220), 22)
    img[..., :3] = np.clip(img[..., :3], 0, 1)
    img[..., 3] = 1
    return img


def build_ui():
    os.makedirs(UI, exist_ok=True)
    fx.save(slot_icons(), os.path.join(UI, "SlotIcons.png"))
    fx.save(cell_icons(), os.path.join(UI, "CellIcons.png"))
    fx.save(wardrobe_toggle(), os.path.join(UI, "WardrobeToggle.png"))
    fx.save(preview_background(), os.path.join(UI, "PreviewBackground.png"))
    fx.save(mod_icon(), os.path.join(ROOT, "icon.png"))


if __name__ == "__main__":
    build_ui()
    entries = build_effect_icons()
    print(f"wrote UI textures and {len(entries)} effect icons")
