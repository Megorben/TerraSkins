"""Composes each generated set onto a standing figure (frame 0) for a quick visual check."""
import os
import sys

import numpy as np
from PIL import Image

D = os.path.join(os.path.dirname(__file__), "..", "Assets", "Textures", "Items", "Sets")


def cell(img, cx, cy):
    return img.crop((cx * 40, cy * 56, cx * 40 + 40, cy * 56 + 56))


def figure(name, frame=0, glow=True):
    out = Image.new("RGBA", (40, 56), (0, 0, 0, 0))
    sfx = "_Glow"
    body = Image.open(f"{D}/{name}Body_Body.png").convert("RGBA")
    bodyg = Image.open(f"{D}/{name}Body_Body{sfx}.png").convert("RGBA")
    legsp = f"{D}/{name}Legs_Legs.png" if os.path.exists(f"{D}/{name}Legs_Legs.png") else f"{D}/{name}Body_Legs.png"
    legs = Image.open(legsp).convert("RGBA")
    legsg = Image.open(legsp.replace(".png", "_Glow.png")).convert("RGBA")
    head = Image.open(f"{D}/{name}Head_Head.png").convert("RGBA")
    headg = Image.open(f"{D}/{name}Head_Head_Glow.png").convert("RGBA")
    bob = -2 if frame in (7, 8, 9, 14, 15, 16) else 0
    layers = [cell(body, 2, 2), cell(body, 1, 1), legs.crop((0, frame * 56, 40, frame * 56 + 56)), cell(body, 0, 0),
              head.crop((0, frame * 56, 40, frame * 56 + 56)), cell(body, 0, 1), cell(body, 2, 0)]
    glows = [cell(bodyg, 2, 2), cell(bodyg, 1, 1), legsg.crop((0, frame * 56, 40, frame * 56 + 56)), cell(bodyg, 0, 0),
             headg.crop((0, frame * 56, 40, frame * 56 + 56)), cell(bodyg, 0, 1), cell(bodyg, 2, 0)]
    for l, g in zip(layers, glows):
        out.alpha_composite(l)
        if glow:
            out.alpha_composite(g)
    return out


names = [n[:-len("Head.png")] for n in sorted(os.listdir(D)) if n.endswith("Head.png") and "_" not in n]
sheet = Image.new("RGBA", (len(names) * 44, 60), (40, 36, 60, 255))
for i, n in enumerate(names):
    sheet.alpha_composite(figure(n), (i * 44 + 2, 2))
sheet = sheet.resize((sheet.width * 4, sheet.height * 4), Image.NEAREST)
sheet.save(sys.argv[1] if len(sys.argv) > 1 else "armor_preview.png")
print(names)
