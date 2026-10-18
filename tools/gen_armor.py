"""Generates the TerraSkins vanity sets: head, body (1.4 composite sheet) and legs/robe sheets plus glow layers and
item icons. Body and leg silhouettes and shading come from tools/templates (tModLoader ExampleMod sprites, MIT),
recolored with each set's materials and decorated per set. Helmets, hats and crowns are drawn from scratch.

Run from anywhere:  python3 tools/gen_armor.py
"""
import os
import re

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(HERE, "..")
OUT = os.path.join(ROOT, "Assets", "Textures", "Items", "Sets")
TPL = os.path.join(HERE, "templates")

FRAME_W, FRAME_H = 40, 56
BOB_FRAMES = {7, 8, 9, 14, 15, 16}  # frames where head and waist sit 2px higher

# ------------------------------------------------------------------------------------------------ materials


def ramp(*hexes):
    return [tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) for h in hexes]


class SetStyle:
    def __init__(self, name, primary, trim, glow, softness=0.0, pattern=None, emblem="diamond", pauldrons=False, gloves=None, boots=None):
        self.name = name
        self.primary = primary   # 5 tones, outline..highlight
        self.trim = trim         # 5 tones
        self.glow = glow         # bright emissive color(s)
        self.softness = softness  # 0: keep template plate detail, 1: smooth cloth
        self.pattern = pattern
        self.emblem = emblem
        self.pauldrons = pauldrons
        self.gloves = gloves or trim
        self.boots = boots or trim


STYLES = {
    "VoidSovereign": SetStyle("VoidSovereign",
                              ramp("0c0614", "1d1030", "2f1d4a", "48306b", "6a4d94"),
                              ramp("1a0f26", "3a2357", "6b3fa0", "9a6ad0", "d0b0ff"),
                              [(200, 120, 255), (255, 130, 230)], pattern="stars", emblem="eye", pauldrons=True),
    "CelestialSeraph": SetStyle("CelestialSeraph",
                                ramp("4a4338", "a8a090", "d8d2c4", "f0ece2", "ffffff"),
                                ramp("5a3a08", "a8701a", "e0aa3c", "ffd36e", "fff2b8"),
                                [(255, 230, 150)], softness=0.3, emblem="sun", pauldrons=True),
    "AstralVoyager": SetStyle("AstralVoyager",
                              ramp("060a1e", "101a40", "1c2c66", "2c4490", "4a66c0"),
                              ramp("2a2008", "6a5214", "b08a2a", "e0c060", "fff0b0"),
                              [(170, 200, 255), (255, 240, 180)], softness=0.85, pattern="stars", emblem="star"),
    "InfernalWarlord": SetStyle("InfernalWarlord",
                                ramp("120808", "2a1414", "432020", "5e2e2a", "7c4038"),
                                ramp("3a2208", "7a4a10", "c0782a", "e8a84a", "ffd890"),
                                [(255, 150, 60), (255, 220, 120)], pattern="cracks", emblem="horns", pauldrons=True),
    "FrostMonarch": SetStyle("FrostMonarch",
                             ramp("1c3048", "3a6488", "6aa0c8", "a8d4f0", "eaf8ff"),
                             ramp("203058", "4058a0", "7090e0", "b0c8ff", "ffffff"),
                             [(170, 230, 255)], softness=0.2, pattern="frost", emblem="snow", pauldrons=True),
    "SakuraRonin": SetStyle("SakuraRonin",
                            ramp("3c2834", "8a5a70", "d890aa", "f4bccc", "fff0f4"),
                            ramp("201820", "3a2c38", "5a4458", "7a6278", "9a8298"),
                            [(255, 180, 210)], softness=0.9, pattern="petals", emblem="flower",
                            gloves=ramp("3a2a22", "6a4e3c", "a07a5a", "c8a07c", "e8caa8")),
    "NeonPhantom": SetStyle("NeonPhantom",
                            ramp("06080c", "10141c", "1c2230", "2a3244", "3c465c"),
                            ramp("0c1418", "183038", "285058", "387078", "5090a0"),
                            [(90, 240, 255), (255, 80, 220)], softness=0.1, pattern="circuit", emblem="chevron"),
    "VerdantDruid": SetStyle("VerdantDruid",
                             ramp("14200c", "2a4016", "406224", "5c8434", "84b050"),
                             ramp("2a1a0c", "4e3218", "7a5028", "a07040", "c89a60"),
                             [(150, 255, 120)], softness=0.85, pattern="vines", emblem="leaf"),
    "AbyssalTide": SetStyle("AbyssalTide",
                            ramp("041818", "0a3434", "125250", "1e7470", "349c94"),
                            ramp("1a1430", "302a5a", "4c4690", "7470c0", "a8a8f0"),
                            [(80, 240, 220)], softness=0.2, pattern="scales", emblem="pearl", pauldrons=True),
    "PrismaticParagon": SetStyle("PrismaticParagon",
                                 ramp("4c4a5c", "9896ac", "cfcde0", "ecebf6", "ffffff"),
                                 ramp("38304c", "6a5a94", "a090d0", "d0c4f4", "ffffff"),
                                 [(255, 120, 120), (255, 220, 110), (120, 255, 160), (110, 190, 255), (220, 130, 255)],
                                 softness=0.3, pattern="prism", emblem="prism", pauldrons=True),
}


def read_sets():
    src = open(os.path.join(ROOT, "Content", "Items", "Sets", "SkinSet.cs")).read()
    sets = []
    for m in re.finditer(r'new\("(\w+)", HeadStyle\.(\w+), (true|false),', src):
        sets.append((m.group(1), m.group(2), m.group(3) == "true"))
    return sets


# ------------------------------------------------------------------------------------------------ helpers

def load(name):
    return np.array(Image.open(os.path.join(TPL, name)).convert("RGBA")).astype(np.int32)


def tone_map(tpl):
    """Normalized brightness (0 outline .. 1 highlight) of the template, ranked by luminance."""
    lum = tpl[..., 0] * 0.3 + tpl[..., 1] * 0.59 + tpl[..., 2] * 0.11
    mask = tpl[..., 3] > 0
    levels = np.unique(np.round(lum[mask]))
    t = np.zeros(lum.shape, np.float32)
    if len(levels) > 1:
        idx = np.searchsorted(levels, np.round(lum))
        t = idx / (len(levels) - 1)
    return np.where(mask, t, 0).astype(np.float32), mask


def boundary(mask):
    p = np.pad(mask, 1)
    inner = p[2:, 1:-1] & p[:-2, 1:-1] & p[1:-1, 2:] & p[1:-1, :-2]
    return mask & ~inner


def smooth_tone(mask):
    """Geometry based shading for cloth: light from the top left."""
    p = np.pad(mask, 2).astype(np.float32)
    up = p[1:-3, 2:-2] + p[0:-4, 2:-2]
    left = p[2:-2, 1:-3] + p[2:-2, 0:-4]
    down = p[3:-1, 2:-2] + p[4:, 2:-2]
    right = p[2:-2, 3:-1] + p[2:-2, 4:]
    t = 0.55 + 0.12 * ((2 - up) + (2 - left)) - 0.12 * ((2 - down) + (2 - right))
    return np.clip(t, 0.15, 1).astype(np.float32)


def shade(tone, mask, mat, ramps, edge_dark=True):
    """Turns a tone map + material index map into RGBA using each material's 5-tone ramp."""
    out = np.zeros(tone.shape + (4,), np.uint8)
    edge = boundary(mask) if edge_dark else np.zeros_like(mask)
    idx = np.clip(np.round(1 + tone * 3.2), 1, 4).astype(int)
    idx[edge] = 0
    for m, r in enumerate(ramps):
        sel = mask & (mat == m)
        if not sel.any():
            continue
        pal = np.array(r, np.uint8)
        out[sel, :3] = pal[idx[sel]]
        out[sel, 3] = 255
    return out


def put_glow(glow, y, x, color, alpha=255):
    if 0 <= y < glow.shape[0] and 0 <= x < glow.shape[1]:
        glow[y, x, :3] = color
        glow[y, x, 3] = alpha


def glow_color(style, i):
    return style.glow[i % len(style.glow)]


# ------------------------------------------------------------------------------------------------ body sheet

TORSO_CELLS = [(0, 0), (1, 0), (0, 2), (1, 2)]
SHOULDER_CELLS = [(0, 1), (1, 1), (0, 3), (1, 3)]
# Direction from shoulder to hand for each arm cell (dx, dy).
HAND_DIR = {}
for r in (0, 2):
    HAND_DIR.update({(2, r): (0, 1), (3, r): (0, -1), (4, r): (1, -1), (5, r): (1, 0), (6, r): (1, 1), (7, r): (0, 1), (8, r): (0, 1)})
for r in (1, 3):
    HAND_DIR.update({(2, r): (0, 1), (3, r): (1, 0), (4, r): (1, 0), (5, r): (1, 0), (6, r): (1, 0), (7, r): (0, 1), (8, r): (0, 1)})

PRIMARY, TRIM, GLOVE = 0, 1, 2


def body_sheet(style):
    tpl = load("Body.png")
    tone_all, mask_all = tone_map(tpl)
    H, W = mask_all.shape
    base = np.zeros((H, W, 4), np.uint8)
    glow = np.zeros((H, W, 4), np.uint8)
    rng = np.random.default_rng(sum(map(ord, style.name)))

    for cy in range(4):
        for cx in range(9):
            ys, xs = slice(cy * FRAME_H, (cy + 1) * FRAME_H), slice(cx * FRAME_W, (cx + 1) * FRAME_W)
            mask = mask_all[ys, xs].copy()
            if not mask.any():
                continue
            if style.pauldrons and (cx, cy) in SHOULDER_CELLS:
                # Bulkier shoulder plates: grow the piece by one pixel up and out.
                p = np.pad(mask, 1)
                mask = mask | p[2:, 1:-1] | p[1:-1, 2:] | p[1:-1, :-2]
            tone = tone_all[ys, xs].copy()
            tone = np.where(mask_all[ys, xs], tone, smooth_tone(mask))
            tone = tone * (1 - style.softness) + smooth_tone(mask) * style.softness
            mat = np.full(mask.shape, PRIMARY)
            g = np.zeros((FRAME_H, FRAME_W, 4), np.uint8)
            yy, xx = np.nonzero(mask)
            y0, y1, x0, x1 = yy.min(), yy.max(), xx.min(), xx.max()

            if (cx, cy) in TORSO_CELLS:
                decorate_torso(style, mask, mat, tone, g, (y0, y1, x0, x1), rng)
            elif (cx, cy) in SHOULDER_CELLS:
                mat[mask] = TRIM
                if style.pauldrons:
                    tone = np.clip(tone + 0.1, 0, 1)
            else:
                decorate_arm(style, mask, mat, tone, g, HAND_DIR.get((cx, cy), (0, 1)), (y0, y1, x0, x1))

            cell = shade(tone, mask, mat, [style.primary, style.trim, style.gloves])
            base[ys, xs] = cell
            glow[ys, xs] = g
    return base, glow


def decorate_torso(style, mask, mat, tone, g, box, rng):
    y0, y1, x0, x1 = box
    cxm = (x0 + x1) // 2 + 1
    # Collar and belt bands.
    mat[(np.arange(FRAME_H)[:, None] <= y0 + 1) & mask] = TRIM
    belt = (np.arange(FRAME_H)[:, None] >= y1 - 2) & (np.arange(FRAME_H)[:, None] <= y1 - 1) & mask
    mat[belt] = TRIM
    # Center trim line down the chest.
    col = mask[:, cxm] & (np.arange(FRAME_H) > y0 + 1) & (np.arange(FRAME_H) < y1 - 2)
    if style.softness > 0.5:
        mat[col, cxm] = TRIM
    # Emblem in the middle of the chest.
    ey = (y0 + y1) // 2
    emblem(style, mat, tone, g, ey, cxm)
    pattern(style, mask, mat, tone, g, box, rng, belt)
    # Belt buckle glow.
    put_glow(g, y1 - 2, cxm, glow_color(style, 0), 200)


def emblem(style, mat, tone, g, y, x):
    c0 = glow_color(style, 0)
    c1 = glow_color(style, 1)
    shape = {
        "diamond": [(0, 0), (-1, 0), (1, 0), (0, -1), (0, 1)],
        "eye": [(0, -1), (0, 0), (0, 1), (-1, 0), (1, 0), (0, -2), (0, 2)],
        "sun": [(0, 0), (-1, 0), (1, 0), (0, -1), (0, 1), (-1, -1), (1, 1), (-1, 1), (1, -1)],
        "star": [(0, 0), (-1, 0), (1, 0), (0, -1), (0, 1), (-2, 0)],
        "horns": [(0, 0), (-1, -1), (-1, 1), (-2, -2), (-2, 2)],
        "snow": [(0, 0), (-1, 0), (1, 0), (0, -1), (0, 1), (-2, 0), (2, 0)],
        "flower": [(0, 0), (-1, 0), (1, 0), (0, -1), (0, 1)],
        "chevron": [(0, 0), (-1, -1), (-1, 1), (1, 0)],
        "leaf": [(0, 0), (-1, 0), (1, 0), (0, 1), (-2, 0)],
        "pearl": [(0, 0), (-1, 0), (0, 1), (-1, 1)],
        "prism": [(0, 0), (-1, 0), (1, 0), (0, -1), (0, 1), (-2, 0), (2, 0)],
    }[style.emblem]
    for i, (dy, dx) in enumerate(shape):
        yy, xx = y + dy, x + dx
        if 0 <= yy < FRAME_H and 0 <= xx < FRAME_W and mat[yy, xx] >= 0:
            mat[yy, xx] = TRIM
            tone[yy, xx] = 0.95
            color = glow_color(style, i) if style.pattern == "prism" else (c1 if (dy, dx) == (0, 0) else c0)
            put_glow(g, yy, xx, color, 255 if (dy, dx) == (0, 0) else 190)


def pattern(style, mask, mat, tone, g, box, rng, belt):
    y0, y1, x0, x1 = box
    p = style.pattern
    inside = mask & ~boundary(mask) & (mat == PRIMARY)
    ys, xs = np.nonzero(inside)
    if p == "stars":
        for _ in range(6):
            i = rng.integers(len(ys))
            put_glow(g, ys[i], xs[i], glow_color(style, rng.integers(9)), int(rng.integers(140, 255)))
    elif p == "cracks":
        for start in ((y0 + 3, x0 + 2), (y0 + 4, x1 - 3)):
            y, x = start
            for _ in range(7):
                if 0 <= y < FRAME_H and 0 <= x < FRAME_W and inside[y, x]:
                    tone[y, x] = 0.0
                    put_glow(g, y, x, glow_color(style, 0), 230)
                y += 1
                x += int(rng.integers(-1, 2))
    elif p == "circuit":
        y = (y0 + y1) // 2 + 2
        for x in range(x0 + 1, x1):
            if inside[y, x]:
                put_glow(g, y, x, glow_color(style, 0), 210)
        for yy in range(y0 + 2, y):
            for x in (x0 + 2, x1 - 2):
                if inside[yy, x]:
                    put_glow(g, yy, x, glow_color(style, 1), 200)
    elif p == "frost":
        for y, x in zip(ys, xs):
            if (x + y) % 5 == 0 and y < (y0 + y1) // 2:
                tone[y, x] = 1.0
                put_glow(g, y, x, glow_color(style, 0), 120)
    elif p == "petals":
        for _ in range(4):
            i = rng.integers(len(ys))
            for dy, dx in ((0, 0), (0, 1), (1, 0)):
                yy, xx = ys[i] + dy, xs[i] + dx
                if 0 <= yy < FRAME_H and 0 <= xx < FRAME_W and inside[yy, xx]:
                    tone[yy, xx] = 1.0
    elif p == "vines":
        x = x0 + 2
        for y in range(y0 + 2, y1 - 2):
            x = x + (1 if (y // 2) % 2 else -1) * 0
            xx = x + (y % 4 == 0)
            if inside[y, xx]:
                mat[y, xx] = TRIM
                if y % 3 == 0:
                    put_glow(g, y, xx, glow_color(style, 0), 170)
    elif p == "scales":
        for y, x in zip(ys, xs):
            if (y % 3 == 0 and (x + y // 3) % 2 == 0):
                tone[y, x] = max(0.0, tone[y, x] - 0.35)
    elif p == "prism":
        for y, x in zip(ys, xs):
            if (x - y) % 6 == 0:
                put_glow(g, y, x, glow_color(style, (x + y) // 6), 110)


def decorate_arm(style, mask, mat, tone, g, hand, box):
    ys, xs = np.nonzero(mask)
    proj = ys * hand[1] + xs * hand[0]
    lo, hi = proj.min(), proj.max()
    span = max(1, hi - lo)
    k = (proj - lo) / span
    gloves = k > 0.72
    cuff = (k > 0.6) & ~gloves
    mat[ys[gloves], xs[gloves]] = GLOVE
    mat[ys[cuff], xs[cuff]] = TRIM
    if span > 6:
        for y, x in zip(ys[cuff], xs[cuff]):
            put_glow(g, y, x, glow_color(style, 0), 150)
            break


# ------------------------------------------------------------------------------------------------ legs

def legs_sheet(style, robe):
    tpl = load("RobeLegs.png" if robe else "Legs.png")
    tone_all, mask_all = tone_map(tpl)
    H, W = mask_all.shape
    base = np.zeros((H, W, 4), np.uint8)
    glow = np.zeros((H, W, 4), np.uint8)
    frames = H // FRAME_H
    for f in range(frames):
        ys = slice(f * FRAME_H, (f + 1) * FRAME_H)
        mask = mask_all[ys]
        if not mask.any():
            continue
        tone = tone_all[ys] * (1 - style.softness) + smooth_tone(mask) * style.softness
        mat = np.full(mask.shape, PRIMARY)
        g = np.zeros((FRAME_H, FRAME_W, 4), np.uint8)
        yy, xx = np.nonzero(mask)
        y0, y1 = yy.min(), yy.max()
        rows = np.arange(FRAME_H)[:, None]
        if robe:
            mat[(rows >= y1 - 1) & mask] = TRIM
            mat[(rows <= y0) & mask] = TRIM
            hem = (rows == y1 - 2) & mask & ~boundary(mask)
            for y, x in zip(*np.nonzero(hem)):
                if x % 3 == 0:
                    put_glow(g, y, x, glow_color(style, x // 3), 200)
            # A soft vertical trim down the front.
            cx = int(np.median(xx)) + 1
            for y in range(y0 + 1, y1 - 1):
                if mask[y, cx] and not boundary(mask)[y, cx]:
                    mat[y, cx] = TRIM
        else:
            mat[(rows <= y0) & mask] = TRIM            # waist
            mat[(rows >= y1 - 3) & mask] = 2           # boots
            knee = (rows == y0 + 5) & mask & ~boundary(mask)
            mat[knee] = TRIM
            for y, x in zip(*np.nonzero(knee)):
                if x % 4 == 1:
                    put_glow(g, y, x, glow_color(style, x), 190)
        cell = shade(tone, mask, mat, [style.primary, style.trim, style.boots])
        base[ys] = cell
        glow[ys] = g
    return base, glow


# ------------------------------------------------------------------------------------------------ heads

class Canvas:
    """A single 40x56 head frame painted with materials, then shaded automatically."""

    def __init__(self):
        self.mat = np.full((FRAME_H, FRAME_W), -1)
        self.tone = np.zeros((FRAME_H, FRAME_W), np.float32)
        self.glow = np.zeros((FRAME_H, FRAME_W, 4), np.uint8)

    def poly(self, pts, mat, tone=0.6):
        img = Image.new("L", (FRAME_W, FRAME_H), 0)
        from PIL import ImageDraw
        ImageDraw.Draw(img).polygon(pts, fill=255)
        m = np.array(img) > 0
        self.mat[m] = mat
        self.tone[m] = tone

    def rect(self, x0, y0, x1, y1, mat, tone=0.6):
        self.mat[y0:y1 + 1, x0:x1 + 1] = mat
        self.tone[y0:y1 + 1, x0:x1 + 1] = tone

    def ellipse(self, x0, y0, x1, y1, mat, tone=0.6):
        img = Image.new("L", (FRAME_W, FRAME_H), 0)
        from PIL import ImageDraw
        ImageDraw.Draw(img).ellipse([x0, y0, x1, y1], fill=255)
        m = np.array(img) > 0
        self.mat[m] = mat
        self.tone[m] = tone

    def px(self, x, y, mat, tone=0.6):
        self.mat[y, x] = mat
        self.tone[y, x] = tone

    def glow_px(self, x, y, color, alpha=255):
        put_glow(self.glow, y, x, color, alpha)
        if self.mat[y, x] < 0:
            self.mat[y, x] = 1
        self.tone[y, x] = 1.0

    def render(self, ramps):
        mask = self.mat >= 0
        tone = np.clip(self.tone + (smooth_tone(mask) - 0.55) * 0.9, 0, 1)
        return shade(tone, mask, np.maximum(self.mat, 0), ramps)


def head_void(c, s):
    # Full horned helmet with a glowing visor slit.
    c.poly([(12, 13), (16, 9), (25, 9), (29, 13), (29, 28), (12, 28)], 0, 0.55)
    c.poly([(14, 10), (11, 4), (10, 0), (15, 7)], 1, 0.6)      # back horn
    c.poly([(24, 9), (27, 3), (30, 0), (28, 7)], 1, 0.7)      # front horn
    c.rect(12, 23, 29, 24, 1, 0.5)
    c.rect(19, 17, 29, 18, 0, 0.0)
    for x in range(20, 30):
        c.glow_px(x, 17, s.glow[0], 230)
    c.glow_px(26, 17, s.glow[1])
    c.glow_px(27, 17, s.glow[1])


def head_seraph(c, s):
    # Golden winged circlet resting on the hair.
    c.rect(13, 12, 28, 13, 1, 0.7)
    c.poly([(13, 12), (8, 6), (7, 9), (11, 13)], 1, 0.8)
    c.poly([(13, 11), (9, 3), (8, 6)], 1, 0.9)
    c.poly([(28, 12), (32, 7), (33, 10), (29, 13)], 1, 0.8)
    c.poly([(19, 11), (21, 7), (23, 11)], 1, 0.9)
    c.glow_px(21, 10, s.glow[0])
    c.glow_px(21, 9, s.glow[0], 200)


def head_astral(c, s):
    # Deep starry hood, open at the face.
    c.poly([(8, 28), (9, 16), (13, 10), (20, 8), (26, 10), (30, 15), (30, 18), (26, 14), (21, 13), (19, 16), (19, 28)], 0, 0.6)
    c.poly([(26, 10), (31, 6), (32, 10), (30, 15)], 0, 0.5)
    c.rect(19, 13, 29, 14, 1, 0.75)
    for x, y in ((12, 15), (15, 20), (11, 23), (14, 26), (17, 11)):
        c.glow_px(x, y, s.glow[(x + y) % len(s.glow)], 220)


def head_infernal(c, s):
    # Iron helmet with a jagged crown and burning eyes.
    c.poly([(12, 14), (15, 10), (26, 10), (29, 14), (29, 28), (12, 28)], 0, 0.55)
    for x in (13, 17, 21, 25):
        c.poly([(x, 10), (x + 1, 5), (x + 3, 10)], 1, 0.75)
    c.rect(12, 24, 29, 25, 1, 0.5)
    c.rect(21, 17, 28, 19, 0, 0.0)
    for x in (23, 24, 27, 28):
        c.glow_px(x, 18, s.glow[0])
    c.glow_px(18, 22, s.glow[0], 160)
    c.glow_px(16, 15, s.glow[1], 160)


def head_frost(c, s):
    # Ice crown with crystal spikes.
    c.rect(13, 11, 28, 13, 1, 0.65)
    for x, h in ((13, 5), (16, 8), (20, 10), (24, 8), (27, 5)):
        c.poly([(x, 11), (x + 1, 11 - h), (x + 3, 11)], 0, 0.85)
    c.glow_px(21, 12, s.glow[0])
    c.glow_px(21, 3, s.glow[0], 200)


def head_sakura(c, s):
    # Wide straw kasa hat.
    c.poly([(4, 15), (14, 9), (20, 6), (27, 9), (36, 15), (34, 16), (6, 16)], 0, 0.6)
    c.rect(13, 13, 27, 14, 1, 0.5)
    for x in (9, 14, 20, 26, 31):
        c.px(x, 13, 0, 0.3)
    c.glow_px(28, 12, s.glow[0], 220)


def head_neon(c, s):
    # Sleek visor helmet with neon lines.
    c.ellipse(11, 9, 30, 29, 0, 0.55)
    c.rect(12, 26, 28, 29, 0, 0.45)
    c.poly([(18, 16), (30, 15), (30, 21), (18, 21)], 1, 0.3)
    for x in range(19, 30):
        c.glow_px(x, 16, s.glow[0], 200)
    for y in range(11, 27):
        c.glow_px(14, y, s.glow[1], 150 if y % 2 else 220)
    c.glow_px(26, 19, s.glow[0])


def head_druid(c, s):
    # Antler crown woven with leaves.
    c.rect(13, 12, 28, 13, 1, 0.55)
    c.poly([(14, 12), (11, 5), (9, 2), (12, 6), (13, 3), (15, 8)], 1, 0.75)
    c.poly([(27, 12), (30, 5), (32, 2), (30, 7), (33, 6), (28, 11)], 1, 0.75)
    for x, y in ((17, 11), (20, 11), (23, 11)):
        c.ellipse(x - 1, y - 2, x + 1, y, 0, 0.7)
    c.glow_px(20, 10, s.glow[0])


def head_abyss(c, s):
    # Dome helmet with a fin and a glass visor.
    c.ellipse(11, 9, 30, 29, 0, 0.55)
    c.poly([(14, 9), (17, 2), (22, 8)], 1, 0.7)
    c.ellipse(19, 13, 30, 23, 1, 0.25)
    c.glow_px(27, 15, s.glow[0])
    c.glow_px(28, 16, s.glow[0], 200)
    c.glow_px(13, 22, s.glow[0], 170)


def head_prism(c, s):
    # Faceted crystal helm.
    c.poly([(12, 28), (12, 14), (16, 9), (25, 9), (29, 13), (29, 28)], 0, 0.6)
    c.poly([(16, 9), (19, 1), (21, 9)], 1, 0.9)
    c.poly([(21, 9), (24, 4), (25, 9)], 1, 0.85)
    c.rect(20, 17, 29, 18, 1, 0.2)
    for i, x in enumerate(range(20, 30)):
        c.glow_px(x, 17, s.glow[i % len(s.glow)], 230)
    c.glow_px(19, 3, s.glow[0], 200)


HEADS = {
    "VoidSovereign": head_void, "CelestialSeraph": head_seraph, "AstralVoyager": head_astral,
    "InfernalWarlord": head_infernal, "FrostMonarch": head_frost, "SakuraRonin": head_sakura,
    "NeonPhantom": head_neon, "VerdantDruid": head_druid, "AbyssalTide": head_abyss, "PrismaticParagon": head_prism,
}


def head_sheet(style):
    c = Canvas()
    HEADS[style.name](c, style)
    frame = c.render([style.primary, style.trim])
    gframe = c.glow
    base = np.zeros((FRAME_H * 20, FRAME_W, 4), np.uint8)
    glow = np.zeros_like(base)
    for f in range(20):
        dy = -2 if f in BOB_FRAMES else 0
        y = f * FRAME_H
        src0 = max(0, -dy)
        base[y:y + FRAME_H - src0] = frame[src0:]
        glow[y:y + FRAME_H - src0] = gframe[src0:]
    return base, glow, frame, gframe


# ------------------------------------------------------------------------------------------------ icons

def crop_icon(rgba, pad=1):
    a = rgba[..., 3] > 0
    if not a.any():
        return rgba[:2, :2]
    ys, xs = np.nonzero(a)
    return rgba[max(0, ys.min() - pad):ys.max() + 1 + pad, max(0, xs.min() - pad):xs.max() + 1 + pad]


def merge(base, glow):
    out = base.copy()
    g = glow[..., 3] > 0
    out[g] = glow[g]
    return out


def body_icon(base, glow):
    full = merge(base, glow)
    cell = lambda cx, cy: full[cy * FRAME_H:(cy + 1) * FRAME_H, cx * FRAME_W:(cx + 1) * FRAME_W]
    layers = [cell(2, 2), cell(1, 1), cell(0, 0), cell(0, 1), cell(2, 0)]  # back arm, back shoulder, torso, front shoulder, front arm
    out = np.zeros_like(layers[0])
    for layer in layers:
        m = layer[..., 3] > 0
        out[m] = layer[m]
    return crop_icon(out)


def save(arr, path):
    Image.fromarray(arr.astype(np.uint8), "RGBA").save(path)


def build():
    os.makedirs(OUT, exist_ok=True)
    sets = read_sets()
    for name, head_style, robe in sets:
        style = STYLES[name]
        h, hg, hframe, hgframe = head_sheet(style)
        save(h, os.path.join(OUT, f"{name}Head_Head.png"))
        save(hg, os.path.join(OUT, f"{name}Head_Head_Glow.png"))
        save(crop_icon(merge(hframe, hgframe)), os.path.join(OUT, f"{name}Head.png"))

        b, bg = body_sheet(style)
        save(b, os.path.join(OUT, f"{name}Body_Body.png"))
        save(bg, os.path.join(OUT, f"{name}Body_Body_Glow.png"))
        save(body_icon(b, bg), os.path.join(OUT, f"{name}Body.png"))

        l, lg = legs_sheet(style, robe)
        if robe:
            save(l, os.path.join(OUT, f"{name}Body_Legs.png"))
            save(lg, os.path.join(OUT, f"{name}Body_Legs_Glow.png"))
        else:
            save(l, os.path.join(OUT, f"{name}Legs_Legs.png"))
            save(lg, os.path.join(OUT, f"{name}Legs_Legs_Glow.png"))
            save(crop_icon(merge(l[:FRAME_H], lg[:FRAME_H])), os.path.join(OUT, f"{name}Legs.png"))
    print(f"wrote {len(sets)} sets to {os.path.normpath(OUT)}")


if __name__ == "__main__":
    build()
