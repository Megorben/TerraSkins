"""Small image toolkit used by the TerraSkins texture generators.

Images are float32 numpy arrays of shape (H, W, 4) holding *premultiplied* RGBA in [0, 1],
which makes compositing behave exactly like Terraria's SpriteBatch:
  - alpha blending:    dst = src + dst * (1 - src.a)
  - additive blending: same formula with src.a = 0 (what the mod does with "A = 0" colors)
"""
import math

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

# ---------------------------------------------------------------------------------------------- basics


def canvas(w, h):
    return np.zeros((h, w, 4), np.float32)


def grid(w, h, cx=None, cy=None):
    """Pixel center coordinates relative to (cx, cy)."""
    cx = (w - 1) / 2 if cx is None else cx
    cy = (h - 1) / 2 if cy is None else cy
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    return x - cx, y - cy


def from_alpha(alpha, rgb=(1, 1, 1)):
    """White (or rgb) image with the given straight alpha map."""
    a = np.clip(alpha, 0, 1).astype(np.float32)
    img = np.zeros(a.shape + (4,), np.float32)
    for i in range(3):
        img[..., i] = rgb[i] * a
    img[..., 3] = a
    return img


def from_gray_alpha(gray, alpha):
    """Image with per pixel brightness (straight) and alpha."""
    a = np.clip(alpha, 0, 1).astype(np.float32)
    g = np.clip(gray, 0, 1).astype(np.float32)
    img = np.zeros(a.shape + (4,), np.float32)
    for i in range(3):
        img[..., i] = g * a
    img[..., 3] = a
    return img


def to_pil(img):
    """Premultiplied float image -> straight alpha 8 bit PIL image."""
    a = img[..., 3:4]
    rgb = np.where(a > 1e-6, img[..., :3] / np.maximum(a, 1e-6), 0)
    out = np.concatenate([np.clip(rgb, 0, 1), np.clip(a, 0, 1)], axis=2)
    return Image.fromarray((out * 255 + 0.5).astype(np.uint8), "RGBA")


def from_pil(im):
    arr = np.asarray(im.convert("RGBA")).astype(np.float32) / 255
    arr[..., :3] *= arr[..., 3:4]
    return arr


def save(img, path):
    to_pil(img).save(path)


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def tint(img, color, alpha=1.0, additive=False):
    """Multiplies a (white) sprite by a color like SpriteBatch does. color is 0..255 RGB."""
    c = np.array([color[0] / 255, color[1] / 255, color[2] / 255, 0 if additive else 1], np.float32) * alpha
    out = img.copy()
    out[..., 0] *= c[0]
    out[..., 1] *= c[1]
    out[..., 2] *= c[2]
    out[..., 3] *= c[3]
    return out


def over(dst, src, x=0, y=0):
    """Composites src (premultiplied) onto dst at integer offset, in place."""
    h, w = src.shape[:2]
    H, W = dst.shape[:2]
    x0, y0 = max(0, x), max(0, y)
    x1, y1 = min(W, x + w), min(H, y + h)
    if x0 >= x1 or y0 >= y1:
        return dst
    s = src[y0 - y:y1 - y, x0 - x:x1 - x]
    d = dst[y0:y1, x0:x1]
    d[...] = s + d * (1 - s[..., 3:4])
    return dst


def transform(sprite, scale=1.0, rotation=0.0, flip_x=False):
    """Scales (float or (sx, sy)) and rotates (radians, clockwise on screen) a sprite."""
    pil = to_pil(sprite)
    if flip_x:
        pil = pil.transpose(Image.FLIP_LEFT_RIGHT)
    sx, sy = (scale, scale) if np.isscalar(scale) else scale
    w = max(1, int(round(pil.width * abs(sx))))
    h = max(1, int(round(pil.height * abs(sy))))
    pil = pil.resize((w, h), Image.LANCZOS if min(abs(sx), abs(sy)) < 1 else Image.BICUBIC)
    if rotation:
        pil = pil.rotate(-math.degrees(rotation), resample=Image.BICUBIC, expand=True)
    return from_pil(pil)


def draw(dst, sprite, center, color=(255, 255, 255), alpha=1.0, scale=1.0, rotation=0.0, additive=False, flip_x=False):
    """SpriteBatch.Draw lookalike: tinted, scaled and rotated around the sprite center."""
    s = transform(sprite, scale, rotation, flip_x)
    s = tint(s, color, alpha, additive)
    h, w = s.shape[:2]
    return over(dst, s, int(round(center[0] - w / 2)), int(round(center[1] - h / 2)))


def downsample(img, factor):
    h, w = img.shape[:2]
    return img.reshape(h // factor, factor, w // factor, factor, 4).mean(axis=(1, 3))


# ---------------------------------------------------------------------------------------------- noise


def value_noise(w, h, cells, seed=0, octaves=4, persistence=0.5):
    """Tileable-ish fractal value noise in [0, 1]."""
    rng = np.random.default_rng(seed)
    total = np.zeros((h, w), np.float32)
    amp, norm = 1.0, 0.0
    for o in range(octaves):
        c = cells * (2 ** o)
        lattice = rng.random((c + 1, c + 1)).astype(np.float32)
        y = np.linspace(0, c, h, endpoint=False)
        x = np.linspace(0, c, w, endpoint=False)
        yi, xi = np.floor(y).astype(int), np.floor(x).astype(int)
        yf, xf = y - yi, x - xi
        yf = yf * yf * (3 - 2 * yf)
        xf = xf * xf * (3 - 2 * xf)
        a = lattice[yi][:, xi]
        b = lattice[yi][:, xi + 1]
        c_ = lattice[yi + 1][:, xi]
        d = lattice[yi + 1][:, xi + 1]
        top = a + (b - a) * xf[None, :]
        bottom = c_ + (d - c_) * xf[None, :]
        total += (top + (bottom - top) * yf[:, None]) * amp
        norm += amp
        amp *= persistence
    return total / norm


# ---------------------------------------------------------------------------------------------- shapes


def supersampled_mask(w, h, painter, factor=4):
    """Rasterizes PIL drawing commands at a higher resolution and returns an anti-aliased alpha mask."""
    big = Image.new("L", (w * factor, h * factor), 0)
    d = ImageDraw.Draw(big)
    painter(d, factor)
    small = big.resize((w, h), Image.LANCZOS)
    return np.asarray(small).astype(np.float32) / 255


def blur(img, radius):
    pil = to_pil(img).filter(ImageFilter.GaussianBlur(radius))
    return from_pil(pil)


def outline(img, color=(20, 10, 35), threshold=0.35):
    """Adds a 1px dark outline around opaque pixels (pixel-art style item icons)."""
    a = img[..., 3] > threshold
    padded = np.pad(a, 1)
    grown = a | padded[2:, 1:-1] | padded[:-2, 1:-1] | padded[1:-1, 2:] | padded[1:-1, :-2]
    ring = grown & ~a
    out = img.copy()
    c = np.array([color[0] / 255, color[1] / 255, color[2] / 255, 1], np.float32)
    out[ring] = c + out[ring] * 0
    return out
