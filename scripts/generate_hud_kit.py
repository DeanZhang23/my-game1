#!/usr/bin/env python3
"""Generate the M0 TestArena HUD component kit (slices, manifest, previews).

Style: Q 版暗黑, flat graphic UI. All hex values and sizes are exact.
Player-visible strings are never baked into reusable 9-slices.
"""

from __future__ import annotations

import json
import math
import os
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path("/workspace/ui-kit-m0")
HUD = ROOT / "Assets/_Project/Art/UI/Hud"
PREV = ROOT / "docs/ui/previews"
DOCS_UI = ROOT / "docs/ui"
DOCS_ART = ROOT / "docs/art"
DICE_DIR = ROOT / "_scratch/dice"
REFS = ROOT / "refs"

# --- Locked palette ---
VOID = (0x0B, 0x12, 0x20, 255)
COLD1 = (0x15, 0x20, 0x38, 255)
COLD2 = (0x1E, 0x2C, 0x4A, 255)
COLD3 = (0x3A, 0x4A, 0x66, 255)
GOLD = (0xC9, 0xA2, 0x27, 255)
GOLD_HI = (0xE8, 0xC5, 0x47, 255)
RUST = (0x8B, 0x2E, 0x32, 255)
RUST_SH = (0x5C, 0x1C, 0x22, 255)
RUST_HI = (0xB8, 0x4A, 0x3E, 255)
PARCH = (0xC9, 0xB8, 0x9A, 255)
PARCH_HI = (0xE8, 0xDC, 0xC4, 255)
ASH = (0x6B, 0x65, 0x60, 255)
ASH_HI = (0x9A, 0x93, 0x8A, 255)
CREVICE = (0x2A, 0x24, 0x20, 255)
CYAN = (0x7E, 0xB3, 0xC9, 255)  # spot only — never a UI fill
GRAYBOX_BG = (0x12, 0x14, 0x1A, 255)  # current TestArena screenshot void

# Pressed fills (slightly darker, still in-family)
PARCH_PRESSED = (0x9A, 0x8A, 0x68, 255)
COLD1_PRESSED = (0x10, 0x18, 0x2A, 255)

# --- Sizes (display px at 1080p-ish graybox) ---
BTN_SEC = (100, 32)
BTN_PRI = (208, 36)
BAR_TRACK = (240, 16)
BAR_FILL_SRC = (32, 12)
BAR_FILL_SHIELD_SRC = (32, 6)
BAR_TRACK_SRC = (32, 16)
BAR_TRACK_VITAL_SRC = (32, 8)
BAR_TICK = (3, 16)
NAMEPLATE = (88, 38)
SLOT = (48, 48)
BADGE = (52, 22)
GLOW = (180, 68)

# 9-slice borders. Unique ornaments live in corners / left-right endcaps, never in the stretchable center.
SLICE_BTN = 8  # legacy alias
SLICE_BTN_PRI = (18, 10, 14, 10)  # l,t,r,b — left holds the skull, right the bone
SLICE_BTN_SEC = (14, 10, 12, 10)
SLICE_TRACK = 6
SLICE_FILL = 2
SLICE_PLATE = 10
SLICE_SLOT = 12  # bone-corner brackets live in the four corners
SLICE_BADGE = (16, 6, 8, 6)  # left holds a 12px skull

# Nameplate inner rects (content, inside 88×38 chrome).
# Bumped from 88×36 so 9px numbers can sit BESIDE the bars, not inside a 5px shield track.
NP_NAME = (4, 3, 84, 16)          # x0,y0,x1,y1
NP_HP = (4, 18, 58, 26)           # 54×8 track
NP_HP_NUM = (60, 18, 84, 26)
NP_SHIELD = (4, 28, 58, 35)       # 54×7 track
NP_SH_NUM = (60, 28, 84, 35)

# Right-rail layout on the 1616×909 graybox screenshots
RAIL_RIGHT = 1605
ENERGY_X = RAIL_RIGHT - BAR_TRACK[0]  # 1365
ENERGY_LABEL_Y = 142
ENERGY_BAR_Y = 160
DICE_Y = 184
SLOT_GAP = 6
BADGE_GAP = 8
BTN_GAP = 8
BTN_STACK_X = ENERGY_X + (BAR_TRACK[0] - BTN_PRI[0]) // 2  # 1381
BTN_SEC_Y = 244
BTN_PRI_Y = 284  # 244+32+8

# Previews used Noto Sans CJK SC locally. FONT_*, ROOT, refs/, and _scratch/ below are box-only paths, not secrets.
FONT_REG = "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc"
FONT_BOLD = "/usr/share/fonts/opentype/noto/NotoSansCJK-Bold.ttc"
FONT_INDEX = 2  # Noto Sans CJK SC


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    path = FONT_BOLD if bold else FONT_REG
    return ImageFont.truetype(path, size, index=FONT_INDEX)


def rgba(c, a=None):
    if a is None:
        return c if len(c) == 4 else (*c[:3], 255)
    return (*c[:3], a)


def mix(a, b, t: float):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3)) + (255,)


def rounded_layer(size, radius, fill, outline=None, outline_w=1, scale=4):
    """Antialiased rounded rect layer."""
    w, h = size
    im = Image.new("RGBA", (w * scale, h * scale), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    # Inset by half the outline so the rim stays inside the canvas
    if outline and outline_w > 0:
        inset = 0
        d.rounded_rectangle(
            [inset, inset, w * scale - 1 - inset, h * scale - 1 - inset],
            radius=max(0, radius * scale),
            fill=fill,
            outline=outline,
            width=outline_w * scale,
        )
    else:
        d.rounded_rectangle(
            [0, 0, w * scale - 1, h * scale - 1],
            radius=max(0, radius * scale),
            fill=fill,
        )
    return im.resize((w, h), Image.Resampling.LANCZOS)


def composite(base: Image.Image, layer: Image.Image, xy=(0, 0)):
    tmp = Image.new("RGBA", base.size, (0, 0, 0, 0))
    tmp.paste(layer, xy)
    return Image.alpha_composite(base, tmp)


def draw_button(size, fill, rim, rim_w, radius=8, inner_hi=None):
    w, h = size
    layer = rounded_layer(size, radius, fill, rim, rim_w, scale=4)
    if inner_hi:
        # 1px inner highlight along the top, clipped to the rounded body
        inset = rim_w + 1
        hi = rounded_layer(
            (w - inset * 2, 3),
            max(1, radius - inset),
            rgba(inner_hi, 70),
            None,
            0,
            scale=4,
        )
        # Keep only the top pixel-ish band by pasting near the top interior
        layer = composite(layer, hi, (inset, inset))
    return layer


def draw_track(size, fill, rim, rim_w=1, radius=3):
    return rounded_layer(size, radius, fill, rim, rim_w, scale=4)


def draw_fill(size, fill, highlight, radius=2):
    """Horizontal bar fill with a 1px graphic top highlight."""
    w, h = size
    base = rounded_layer(size, radius, fill, None, 0, scale=4)
    if h >= 4 and highlight:
        hi = rounded_layer((w, max(2, h // 3)), radius, rgba(highlight, 160), None, 0, scale=4)
        # Crop to top portion by pasting at y=1
        overlay = Image.new("RGBA", size, (0, 0, 0, 0))
        overlay = composite(overlay, hi, (0, 1))
        # Mask highlight to the fill body
        mask = base.split()[-1]
        overlay.putalpha(ImageChops_and(overlay.split()[-1], mask) if False else overlay.split()[-1])
        # Simpler: paste highlight then re-mask
        tmp = composite(base, hi, (0, 1))
        tmp.putalpha(base.split()[-1])
        return tmp
    return base


def text_size(font_obj, text):
    b = font_obj.getbbox(text)
    return b[2] - b[0], b[3] - b[1]


def fit_ellipsis(text, font_obj, max_w):
    if font_obj.getlength(text) <= max_w:
        return text
    ell = "…"
    for i in range(len(text), 0, -1):
        t = text[:i].rstrip() + ell
        if font_obj.getlength(t) <= max_w:
            return t
    return ell


def draw_text(draw, xy, text, font_obj, fill, anchor="lt"):
    draw.text(xy, text, font=font_obj, fill=fill, anchor=anchor)


def load_dice(name: str, display=48) -> Image.Image:
    im = Image.open(DICE_DIR / f"ui_dice_{name}_normal.png").convert("RGBA")
    return im.resize((display, display), Image.Resampling.LANCZOS)


# ---------------------------------------------------------------------------
# Ornaments and carved chrome (weight revision)
# Unique skulls/bones are overlay slices or baked only into 9-slice endcaps/corners.
# Wear (chips, hairline cracks) is restricted to corner tiles so edges still stretch.
# ---------------------------------------------------------------------------

def _px_grid(d, cells, palette, scale, ox=0, oy=0):
    for y, row in enumerate(cells):
        for x, ch in enumerate(row):
            col = palette.get(ch)
            if col:
                d.rectangle(
                    [ox + x * scale, oy + y * scale, ox + (x + 1) * scale - 1, oy + (y + 1) * scale - 1],
                    fill=col,
                )


def make_skull(variant: str = "bone", size: int = 16) -> Image.Image:
    """Pixel-clean skull. variant: bone | rust | ash. size 16 or 12."""
    if variant == "rust":
        pal = {"x": PARCH, "h": PARCH_HI, "s": ASH, "o": VOID, "v": CREVICE, "r": RUST, "d": RUST_SH}
    elif variant == "ash":
        pal = {"x": ASH_HI, "h": ASH, "s": mix(ASH, VOID, 0.35), "o": VOID, "v": CREVICE, "r": RUST_SH, "d": RUST_SH}
    else:
        pal = {"x": PARCH, "h": PARCH_HI, "s": ASH, "o": VOID, "v": CREVICE, "r": RUST, "d": RUST_SH}
    if size == 16:
        grid = [
            "................",
            ".....hhhhhh.....",
            "...hhxxxxxxhh...",
            "..hhxxxxxxxxhh..",
            ".hhxxooxxooxxhh.",
            ".hxxooo  oooxxh.",
            ".hxxooo  oooxxh.",
            ".hhxxooxxooxxhh.",
            "..hhxxxvvxxxhh..",
            "..hhxxxxxxxxhh..",
            "...hxxssssxxh...",
            "...hxoxxxoxxh...",
            "...hxxoxoxxhh...",
            "....hxxxxxxh....",
            ".....hhxxhh.....",
            "................",
        ]
    else:
        grid = [
            "............",
            "...xxxxxx...",
            "..xxxxxxxx..",
            ".xxooxxooxx.",
            ".xooo  ooox.",
            ".xxooxxooxx.",
            "..xxxvvxxx..",
            "..xxxxxxxx..",
            "...xssssx...",
            "...xoxoxx...",
            "....xxxx....",
            "............",
        ]
    scale = 4
    h, w = len(grid), len(grid[0])
    im = Image.new("RGBA", (w * scale, h * scale), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    _px_grid(d, grid, pal, scale)
    if variant == "rust":
        # One drip from the right socket — not a red wash over the face.
        if size == 16:
            d.ellipse([10 * scale, 7 * scale, 13 * scale, 11 * scale], fill=RUST)
            d.polygon(
                [(11 * scale, 10 * scale), (13 * scale, 10 * scale), (12 * scale, 14 * scale)],
                fill=RUST,
            )
        else:
            d.ellipse([7 * scale, 5 * scale, 10 * scale, 8 * scale], fill=RUST)
            d.polygon(
                [(8 * scale, 7 * scale), (10 * scale, 7 * scale), (9 * scale, 10 * scale)],
                fill=RUST,
            )
    if variant == "ash":
        # Hairline crack across the cranium
        d.line([(3 * scale, 4 * scale), (size * scale - 3 * scale, 6 * scale)], fill=VOID, width=max(1, scale // 2))
    return im.resize((w, h), Image.Resampling.LANCZOS)


def make_bone(w: int = 16, h: int = 8, variant: str = "bone") -> Image.Image:
    body, hi = (PARCH, PARCH_HI) if variant != "ash" else (ASH_HI, ASH)
    scale = 4
    im = Image.new("RGBA", (w * scale, h * scale), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse([0, 0, 6 * scale - 1, (h // 2 + 1) * scale], fill=body)
    d.ellipse([0, (h // 2 - 1) * scale, 6 * scale - 1, h * scale - 1], fill=body)
    d.ellipse([(w - 6) * scale, 0, w * scale - 1, (h // 2 + 1) * scale], fill=body)
    d.ellipse([(w - 6) * scale, (h // 2 - 1) * scale, w * scale - 1, h * scale - 1], fill=body)
    d.rounded_rectangle([3 * scale, 2 * scale, (w - 3) * scale, (h - 2) * scale - 1], scale, fill=hi)
    d.ellipse([1 * scale, 1 * scale, 3 * scale, 3 * scale], fill=hi)
    d.ellipse([(w - 4) * scale, 1 * scale, (w - 2) * scale, 3 * scale], fill=hi)
    return im.resize((w, h), Image.Resampling.LANCZOS)


def make_bone_corner(n: int = 12, variant: str = "bone") -> Image.Image:
    """Top-left L-shaped bone bracket. Rotate for the other three corners."""
    body, hi = (PARCH, PARCH_HI) if variant != "ash" else (ASH_HI, ASH)
    scale = 4
    im = Image.new("RGBA", (n * scale, n * scale), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse([0, 0, 6 * scale - 1, 6 * scale - 1], fill=body)
    d.rounded_rectangle([3 * scale, 1 * scale + scale // 2, (n - 1) * scale, 5 * scale], scale, fill=body)
    d.ellipse([(n - 6) * scale, 0, n * scale - 1, 6 * scale - 1], fill=body)
    d.rounded_rectangle([1 * scale + scale // 2, 3 * scale, 5 * scale, (n - 1) * scale], scale, fill=body)
    d.ellipse([0, (n - 6) * scale, 6 * scale - 1, n * scale - 1], fill=body)
    d.ellipse([1 * scale, 1 * scale, 3 * scale, 3 * scale], fill=hi)
    return im.resize((n, n), Image.Resampling.LANCZOS)


def make_blood(w: int = 16, h: int = 16) -> Image.Image:
    scale = 4
    im = Image.new("RGBA", (w * scale, h * scale), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    drops = [
        (2, 3, 6, 7, RUST),
        (6, 1, 10, 6, RUST),
        (8, 6, 15, 14, RUST_SH),
        (3, 8, 8, 15, RUST),
        (11, 2, 14, 6, RUST_HI),
        (1, 10, 4, 15, RUST_SH),
        (12, 10, 15, 15, RUST),
    ]
    for x0, y0, x1, y1, c in drops:
        d.ellipse([x0 * scale, y0 * scale, x1 * scale, y1 * scale], fill=c)
    d.ellipse([9 * scale, 12 * scale, 12 * scale, 16 * scale], fill=RUST)
    d.polygon([(10 * scale, 14 * scale), (12 * scale, 14 * scale), (11 * scale, h * scale - 1)], fill=RUST)
    return im.resize((w, h), Image.Resampling.LANCZOS)


def draw_carved(size, fill, rim, rim_w=2, shell=COLD2, radius=6, wear=True, scale=4):
    """Heavy carved frame: dark shell + gold (or ash/rust) band + inset well + corner wear."""
    w, h = size
    W, H = w * scale, h * scale
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, 0, W - 1, H - 1], radius * scale, fill=shell)
    inset = 1 * scale
    d.rounded_rectangle(
        [inset, inset, W - 1 - inset, H - 1 - inset],
        max(1, (radius - 1) * scale),
        fill=rim,
    )
    inset2 = inset + rim_w * scale
    d.rounded_rectangle(
        [inset2, inset2, W - 1 - inset2, H - 1 - inset2],
        max(1, (radius - 1 - rim_w) * scale),
        fill=fill,
    )
    if h >= 12:
        d.rounded_rectangle(
            [inset2 + scale, inset2 + scale, W - 1 - inset2 - scale, inset2 + 2 * scale],
            scale,
            fill=rgba(mix(fill, (255, 255, 255, 255), 0.12), 90),
        )
    if wear:
        _corner_wear(d, W, H, scale, shell)
    return im.resize((w, h), Image.Resampling.LANCZOS)


def _corner_wear(d, W, H, s, shell):
    """Chips and hairline cracks, 10px corners only — 9-slice safe."""
    d.polygon([(2 * s, 1 * s), (7 * s, 1 * s), (3 * s, 4 * s)], fill=shell)
    d.line([(4 * s, 3 * s), (8 * s, 8 * s)], fill=CREVICE, width=max(1, s // 2))
    d.polygon([(W - 1 - 2 * s, 2 * s), (W - 1 - 1 * s, 7 * s), (W - 1 - 6 * s, 3 * s)], fill=shell)
    d.line([(W - 1 - 4 * s, 4 * s), (W - 1 - 8 * s, 8 * s)], fill=CREVICE, width=max(1, s // 2))
    d.polygon([(W - 1 - 1 * s, H - 1 - 2 * s), (W - 1 - 8 * s, H - 1 - 1 * s), (W - 1 - 3 * s, H - 1 - 6 * s)], fill=shell)
    d.line([(W - 1 - 5 * s, H - 1 - 4 * s), (W - 1 - 10 * s, H - 1 - 9 * s)], fill=CREVICE, width=max(1, s // 2))
    d.line([(3 * s, H - 1 - 3 * s), (8 * s, H - 1 - 8 * s)], fill=CREVICE, width=max(1, s // 2))


def stamp_bone_corners(panel: Image.Image, variant: str = "bone", n: int = 12) -> Image.Image:
    bc = make_bone_corner(n, variant)
    w, h = panel.size
    panel = composite(panel, bc, (0, 0))
    panel = composite(panel, bc.transpose(Image.Transpose.ROTATE_270), (w - n, 0))
    panel = composite(panel, bc.transpose(Image.Transpose.ROTATE_90), (0, h - n))
    panel = composite(panel, bc.transpose(Image.Transpose.ROTATE_180), (w - n, h - n))
    return panel


def make_endcap(side: str, ready: bool) -> Image.Image:
    """Left skull cap (16×16) or right bone cap (14×16). Overlay, not 9-sliced."""
    rim = GOLD if ready else mix(COLD3, GOLD, 0.45)
    if side == "left":
        cap = draw_carved((16, 16), COLD1, rim, 1, COLD2, 4, wear=False)
        cap = composite(cap, make_skull("bone", 12), (2, 2))
        return cap
    cap = draw_carved((14, 16), COLD1, rim, 1, COLD2, 4, wear=False)
    cap = composite(cap, make_bone(12, 6, "bone"), (1, 5))
    return cap



# ---------------------------------------------------------------------------
# Slice generation
# ---------------------------------------------------------------------------

def save(im: Image.Image, path: Path):
    path.parent.mkdir(parents=True, exist_ok=True)
    im.save(path, "PNG")
    print(f"  wrote {path.relative_to(ROOT)}  {im.size[0]}×{im.size[1]} {im.mode}")


def make_slices():
    HUD.mkdir(parents=True, exist_ok=True)

    # Buttons — carved chrome, skull/bone baked into unique 9-slice endcaps only.
    primary = {
        "normal": dict(fill=PARCH, rim=GOLD, rim_w=2, shell=COLD2, wear=True, orn="bone"),
        "hover": dict(fill=PARCH, rim=GOLD_HI, rim_w=2, shell=COLD2, wear=True, orn="bone"),
        "pressed": dict(fill=PARCH_PRESSED, rim=GOLD, rim_w=2, shell=COLD2, wear=True, orn="bone"),
        "disabled": dict(fill=COLD2, rim=ASH, rim_w=2, shell=mix(VOID, ASH, 0.45), wear=True, orn="ash"),
    }
    secondary = {
        "normal": dict(fill=COLD1, rim=GOLD, rim_w=2, shell=COLD2, wear=True, orn="bone"),
        "hover": dict(fill=COLD1, rim=GOLD_HI, rim_w=2, shell=COLD2, wear=True, orn="bone"),
        "pressed": dict(fill=COLD1_PRESSED, rim=GOLD, rim_w=2, shell=COLD2, wear=True, orn="bone"),
        "disabled": dict(fill=COLD2, rim=ASH, rim_w=2, shell=mix(VOID, ASH, 0.45), wear=True, orn="ash"),
    }
    for state, sp in primary.items():
        im = draw_carved(BTN_PRI, sp["fill"], sp["rim"], sp["rim_w"], sp["shell"], 7, sp["wear"])
        skull = make_skull(sp["orn"], 16)
        bone = make_bone(16, 8, sp["orn"])
        im = composite(im, skull, (2, (BTN_PRI[1] - 16) // 2))
        im = composite(im, bone, (BTN_PRI[0] - 18, (BTN_PRI[1] - 8) // 2))
        save(im, HUD / f"ui_btn_primary_{state}.png")
    for state, sp in secondary.items():
        im = draw_carved(BTN_SEC, sp["fill"], sp["rim"], sp["rim_w"], sp["shell"], 6, sp["wear"])
        skull = make_skull(sp["orn"], 12)
        bone = make_bone(12, 6, sp["orn"])
        im = composite(im, skull, (2, (BTN_SEC[1] - 12) // 2))
        im = composite(im, bone, (BTN_SEC[0] - 15, (BTN_SEC[1] - 6) // 2))
        save(im, HUD / f"ui_btn_secondary_{state}.png")

    # Tracks — thicker carved rim; unique endcaps are separate overlays.
    save(draw_carved(BAR_TRACK_SRC, COLD1, COLD3, 2, COLD2, 3, True), HUD / "ui_bar_track.png")
    save(draw_carved(BAR_TRACK_SRC, COLD1, GOLD, 2, COLD2, 3, True), HUD / "ui_bar_track_ready.png")
    save(draw_track(BAR_TRACK_VITAL_SRC, CREVICE, RUST_SH, 1, 2), HUD / "ui_bar_track_vital.png")
    save(make_endcap("left", False), HUD / "ui_bar_endcap_left.png")
    save(make_endcap("right", False), HUD / "ui_bar_endcap_right.png")
    save(make_endcap("left", True), HUD / "ui_bar_endcap_left_ready.png")
    save(make_endcap("right", True), HUD / "ui_bar_endcap_right_ready.png")

    # Tick marks — slightly heavier carved sliver
    tick = Image.new("RGBA", (3, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(tick)
    d.rectangle([1, 2, 1, 13], fill=COLD3)
    d.rectangle([0, 3, 2, 12], fill=COLD3)
    save(tick, HUD / "ui_bar_tick.png")
    tick_r = Image.new("RGBA", (3, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(tick_r)
    d.rectangle([1, 1, 1, 14], fill=GOLD)
    d.rectangle([0, 2, 2, 13], fill=GOLD_HI)
    save(tick_r, HUD / "ui_bar_tick_ready.png")

    # Fills (unchanged palette)
    save(draw_fill(BAR_FILL_SRC, RUST, RUST_HI, 2), HUD / "ui_bar_fill_hp.png")
    save(draw_fill(BAR_FILL_SHIELD_SRC, GOLD, GOLD_HI, 2), HUD / "ui_bar_fill_shield.png")
    save(draw_fill(BAR_FILL_SRC, GOLD, GOLD_HI, 2), HUD / "ui_bar_fill_fate.png")

    # Nameplates — carved frames + corner wear. Skulls/blood are overlay slices, not 9-slice centers.
    for name, rim, fill, shell, track_rim in [
        ("ally", GOLD, COLD1, COLD2, RUST_SH),
        ("enemy", RUST, mix(VOID, RUST_SH, 0.35), mix(VOID, RUST_SH, 0.55), RUST_SH),
        ("dead", ASH, mix(VOID, ASH, 0.35), mix(VOID, ASH, 0.5), ASH),
    ]:
        im = draw_nameplate_chrome(rim, fill, shell, track_rim)
        save(im, HUD / f"ui_nameplate_{name}.png")

    # Empty dice slot — carved well + bone-corner brackets (unique 12px corners)
    slot = draw_carved(SLOT, COLD1, GOLD, 2, COLD2, 6, True)
    well = rounded_layer((36, 36), 4, VOID, COLD2, 1, scale=4)
    slot = composite(slot, well, (6, 6))
    slot = stamp_bone_corners(slot, "bone", 12)
    save(slot, HUD / "ui_dice_slot_empty.png")

    # Combo badge — skull baked in the left 16px 9-slice region, no text
    badge = draw_carved(BADGE, COLD1, GOLD, 2, COLD2, 5, True)
    badge = composite(badge, make_skull("bone", 12), (2, (BADGE[1] - 12) // 2))
    save(badge, HUD / "ui_badge_combo.png")

    # Pending-confirm glow
    save(make_pending_glow(GLOW), HUD / "ui_dice_pending_glow.png")

    # Overlay ornaments (runtime / compose stamps)
    save(make_skull("bone", 16), HUD / "ui_ornament_skull.png")
    save(make_skull("rust", 16), HUD / "ui_ornament_skull_rust.png")
    save(make_skull("ash", 16), HUD / "ui_ornament_skull_ash.png")
    save(make_bone(16, 8, "bone"), HUD / "ui_ornament_bone.png")
    save(make_bone_corner(12, "bone"), HUD / "ui_ornament_bone_corner.png")
    save(make_blood(16, 16), HUD / "ui_ornament_blood.png")


def draw_nameplate_chrome(rim, fill, shell, track_rim) -> Image.Image:
    w, h = NAMEPLATE
    plate = draw_carved((w, h), fill, rim, 2, shell, 6, True)
    hp_w = NP_HP[2] - NP_HP[0]
    hp_h = NP_HP[3] - NP_HP[1]
    sh_w = NP_SHIELD[2] - NP_SHIELD[0]
    sh_h = NP_SHIELD[3] - NP_SHIELD[1]
    hp_track = draw_track((hp_w, hp_h), CREVICE, track_rim, 1, 2)
    sh_track = draw_track((sh_w, sh_h), CREVICE, track_rim, 1, 2)
    plate = composite(plate, hp_track, (NP_HP[0], NP_HP[1]))
    plate = composite(plate, sh_track, (NP_SHIELD[0], NP_SHIELD[1]))
    return plate


def make_pending_glow(size):
    w, h = size
    # Soft gold aura, not neon. Draw a fat rounded rect then blur.
    scale = 2
    im = Image.new("RGBA", (w * scale, h * scale), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    pad = 10 * scale
    d.rounded_rectangle(
        [pad, pad, w * scale - 1 - pad, h * scale - 1 - pad],
        radius=12 * scale,
        fill=rgba(GOLD, 110),
        outline=rgba(GOLD_HI, 90),
        width=3 * scale,
    )
    im = im.filter(ImageFilter.GaussianBlur(radius=6 * scale))
    im = im.resize((w, h), Image.Resampling.LANCZOS)
    return im


def write_manifest():
    items = [
        {
            "file": "ui_btn_primary_{normal,hover,pressed,disabled}.png",
            "size": list(BTN_PRI),
            "nine_slice": {"left": 18, "right": 14, "top": 10, "bottom": 10},
            "pivot": [0.5, 0.5],
            "notes": "Carved dark shell + 2px gold rim, chipped corners. 16px skull baked in the left endcap, bone in the right. NO baked label. Runtime text: loc ui.testarena.btn_march (出征), 14px #0B1220 (disabled #6B6560).",
        },
        {
            "file": "ui_btn_secondary_{normal,hover,pressed,disabled}.png",
            "size": list(BTN_SEC),
            "nine_slice": {"left": 14, "right": 12, "top": 10, "bottom": 10},
            "pivot": [0.5, 0.5],
            "notes": "Carved shell + 2px gold rim, chipped corners. 12px skull in the left endcap, bone knuckle on the right. NO baked label. Runtime text: ui.testarena.btn_roll (掷骰) / ui.testarena.btn_confirm (确认), 14px #E8DCC4 (disabled #6B6560).",
        },
        {
            "file": "ui_bar_track.png",
            "size": list(BAR_TRACK_SRC),
            "nine_slice": {"left": 6, "right": 6, "top": 6, "bottom": 6},
            "pivot": [0.5, 0.5],
            "display": {"fate": list(BAR_TRACK)},
            "notes": "Carved fate track, cold rim. Display at 240×16. Overlay ui_bar_endcap_left/right.png on the ends (do not 9-slice the skull). Tick stays at 50%.",
        },
        {
            "file": "ui_bar_track_ready.png",
            "size": list(BAR_TRACK_SRC),
            "nine_slice": {"left": 6, "right": 6, "top": 6, "bottom": 6},
            "pivot": [0.5, 0.5],
            "display": {"fate": list(BAR_TRACK)},
            "notes": "Ready track with gold carved rim. Use ui_bar_endcap_left_ready.png / ui_bar_endcap_right_ready.png.",
        },
        {
            "file": "ui_bar_track_vital.png",
            "size": list(BAR_TRACK_VITAL_SRC),
            "nine_slice": {"left": 4, "right": 4, "top": 3, "bottom": 3},
            "pivot": [0.5, 0.5],
            "notes": "Optional standalone #2A2420 underlay for HP/shield if not using the tracks baked into the nameplate chrome. Nameplates already include empty tracks.",
        },
        {
            "file": "ui_bar_tick.png",
            "size": list(BAR_TICK),
            "nine_slice": None,
            "pivot": [0.5, 0.5],
            "notes": "Place at 50% of the fate track (center x = track_x + 120 on a 240px bar). Use ui_bar_tick_ready.png when in 可掷骰 state.",
        },
        {
            "file": "ui_bar_tick_ready.png",
            "size": list(BAR_TICK),
            "nine_slice": None,
            "pivot": [0.5, 0.5],
            "notes": "Emphasized 50 tick, #E8C547, ready / 可掷骰 only.",
        },
        {
            "file": "ui_bar_fill_fate.png",
            "size": list(BAR_FILL_SRC),
            "nine_slice": {"left": 2, "right": 2, "top": 2, "bottom": 2},
            "pivot": [0.0, 0.5],
            "image_type": "Filled (Horizontal, Origin Left) or Sliced + width",
            "notes": "Gold #C9A227. In ready state, add a 2–3px #E8C547 leading edge on the fill's right end (runtime, not baked).",
        },
        {
            "file": "ui_bar_fill_hp.png",
            "size": list(BAR_FILL_SRC),
            "nine_slice": {"left": 2, "right": 2, "top": 2, "bottom": 2},
            "pivot": [0.0, 0.5],
            "image_type": "Filled (Horizontal, Origin Left)",
            "notes": "Rust #8B2E32. Default Unity green is forbidden. Overlay on nameplate HP track rect (4,17,84,24) with 1px inset if needed.",
        },
        {
            "file": "ui_bar_fill_shield.png",
            "size": list(BAR_FILL_SHIELD_SRC),
            "nine_slice": {"left": 2, "right": 2, "top": 2, "bottom": 2},
            "pivot": [0.0, 0.5],
            "image_type": "Filled (Horizontal, Origin Left)",
            "notes": "Grave-lamp gold #C9A227. Overlay on nameplate shield track rect (4,26,84,31).",
        },
        {
            "file": "ui_nameplate_ally.png",
            "size": list(NAMEPLATE),
            "nine_slice": {"left": 10, "right": 10, "top": 10, "bottom": 10},
            "pivot": [0.5, 0.0],
            "content_rects": {
                "name": {"x0": 4, "y0": 3, "x1": 84, "y1": 16},
                "hp": {"x0": 4, "y0": 18, "x1": 58, "y1": 26},
                "hp_num": {"x0": 60, "y0": 18, "x1": 84, "y1": 26},
                "shield": {"x0": 4, "y0": 28, "x1": 58, "y1": 35},
                "shield_num": {"x0": 60, "y0": 28, "x1": 84, "y1": 35},
            },
            "notes": "88×38 carved gold frame, chipped corners. Stamp ui_ornament_bone_corner.png on the frame (not in the stretchable center). Runtime name 11px #E8DCC4. Pivot top-center.",
        },
        {
            "file": "ui_nameplate_enemy.png",
            "size": list(NAMEPLATE),
            "nine_slice": {"left": 10, "right": 10, "top": 10, "bottom": 10},
            "pivot": [0.5, 0.0],
            "content_rects": {
                "name": {"x0": 4, "y0": 3, "x1": 84, "y1": 16},
                "hp": {"x0": 4, "y0": 18, "x1": 58, "y1": 26},
                "hp_num": {"x0": 60, "y0": 18, "x1": 84, "y1": 26},
                "shield": {"x0": 4, "y0": 28, "x1": 58, "y1": 35},
                "shield_num": {"x0": 60, "y0": 28, "x1": 84, "y1": 35},
            },
            "notes": "Carved rust rim, chipped corners. Stamp ui_ornament_skull_rust.png at (1,1) and ui_ornament_blood.png (12px) at (74,1), in the frame, not over numbers. No 生命/护盾 words.",
        },
        {
            "file": "ui_nameplate_dead.png",
            "size": list(NAMEPLATE),
            "nine_slice": {"left": 10, "right": 10, "top": 10, "bottom": 10},
            "pivot": [0.5, 0.0],
            "notes": "Carved ash rim, cracks, empty tracks. Stamp ui_ornament_skull_ash.png at (1,1) and ui_ornament_blood.png (12px) at (74,1). Runtime name #6B6560, CanvasGroup 50% fade.",
        },
        {
            "file": "ui_dice_slot_empty.png",
            "size": list(SLOT),
            "nine_slice": {"left": 12, "right": 12, "top": 12, "bottom": 12},
            "pivot": [0.5, 0.5],
            "notes": "Carved 48×48 well, 2px gold rim, bone-corner brackets baked in the four 12px corners (safe to 9-slice). Rolled faces: scale ui_dice_*_normal.png into the well. M0 has no lock/crack in this flow.",
        },
        {
            "file": "ui_badge_combo.png",
            "size": list(BADGE),
            "nine_slice": {"left": 16, "right": 8, "top": 6, "bottom": 6},
            "pivot": [0.5, 0.5],
            "notes": "Carved chrome, 12px skull baked in the left 16px endcap. Runtime text 11px #E8DCC4 in the field to the right, via combo.twin / combo.attunement. Hide when there is no combo.",
        },
        {
            "file": "ui_dice_pending_glow.png",
            "size": list(GLOW),
            "nine_slice": None,
            "pivot": [0.5, 0.5],
            "notes": "Soft gold aura, place behind the three 48×48 slots during the 6s pending-confirm window. Not neon.",
        },
        {
            "file": "ui_ornament_skull.png",
            "size": [16, 16],
            "nine_slice": None,
            "pivot": [0.5, 0.5],
            "notes": "Bone/parchment skull overlay. Also baked into primary-button left endcaps. Never place in a stretchable 9-slice center.",
        },
        {
            "file": "ui_ornament_skull_rust.png",
            "size": [16, 16],
            "nine_slice": None,
            "pivot": [0.5, 0.5],
            "notes": "Same skull with a rust drip from the right socket. Stamp on enemy nameplates at (1,1) (or 12px).",
        },
        {
            "file": "ui_ornament_skull_ash.png",
            "size": [16, 16],
            "nine_slice": None,
            "pivot": [0.5, 0.5],
            "notes": "Ash skull with a cranium crack. Dead plates and disabled endcaps.",
        },
        {
            "file": "ui_ornament_bone.png",
            "size": [16, 8],
            "nine_slice": None,
            "pivot": [0.5, 0.5],
            "notes": "Horizontal bone knuckle. Baked into button right endcaps.",
        },
        {
            "file": "ui_ornament_bone_corner.png",
            "size": [12, 12],
            "nine_slice": None,
            "pivot": [0.0, 0.0],
            "notes": "Top-left L-shaped bone bracket. Rotate 90/180/270 for the other corners. Baked into dice-slot corners; stamp on ally nameplate corners.",
        },
        {
            "file": "ui_ornament_blood.png",
            "size": [16, 16],
            "nine_slice": None,
            "pivot": [0.5, 0.5],
            "notes": "Rust #8B2E32 spatters. Stamp on enemy/dead nameplates (e.g. 70,20) and sparingly on a spent fate track. Do not 9-slice.",
        },
        {
            "file": "ui_bar_endcap_left.png / ui_bar_endcap_left_ready.png",
            "size": [16, 16],
            "nine_slice": None,
            "pivot": [0.0, 0.5],
            "notes": "Skull set into a carved cap. Overlay at the left end of the 240×16 fate track. Ready variant uses a gold bezel.",
        },
        {
            "file": "ui_bar_endcap_right.png / ui_bar_endcap_right_ready.png",
            "size": [14, 16],
            "nine_slice": None,
            "pivot": [1.0, 0.5],
            "notes": "Bone knuckle cap. Overlay at the right end of the fate track.",
        },
    ]
    meta = {
        "kit": "m0-testarena-hud",
        "style": "Q 版暗黑",
        "canvas_reference": "1616×909 TestArena graybox (1080p-ish)",
        "import": {
            "texture_type": "Sprite (2D and UI)",
            "mesh_type": "Full Rect",
            "filter": "Bilinear",
            "compression": "None or High Quality",
            "generate_mipmaps": False,
            "pixels_per_unit": 64,
            "alpha": "PNG RGBA, no baked #0B1220 void background on slices",
        },
        "typography_px": {
            "button_label": 14,
            "energy_label": 12,
            "energy_value": 11,
            "nameplate_name": 11,
            "nameplate_numbers": 9,
            "combo_badge": 11,
        },
        "items": items,
    }
    path = HUD / "manifest.json"
    path.write_text(json.dumps(meta, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"  wrote {path.relative_to(ROOT)}")


# ---------------------------------------------------------------------------
# Compositing helpers for previews / mocks
# ---------------------------------------------------------------------------

def load_slice(name: str) -> Image.Image:
    return Image.open(HUD / name).convert("RGBA")


def nine_slice(src: Image.Image, size, border) -> Image.Image:
    """Simple 9-slice scale to size. border is int or (l,t,r,b)."""
    if isinstance(border, int):
        l = t = r = b = border
    else:
        l, t, r, b = border
    sw, sh = src.size
    dw, dh = size
    if dw == sw and dh == sh:
        return src.copy()
    out = Image.new("RGBA", (dw, dh), (0, 0, 0, 0))

    def put(src_box, dst_box):
        sx0, sy0, sx1, sy1 = src_box
        dx0, dy0, dx1, dy1 = dst_box
        tw, th = dx1 - dx0, dy1 - dy0
        if tw <= 0 or th <= 0 or sx1 <= sx0 or sy1 <= sy0:
            return
        patch = src.crop((sx0, sy0, sx1, sy1)).resize((tw, th), Image.Resampling.LANCZOS)
        out.paste(patch, (dx0, dy0), patch)

    # corners
    put((0, 0, l, t), (0, 0, l, t))
    put((sw - r, 0, sw, t), (dw - r, 0, dw, t))
    put((0, sh - b, l, sh), (0, dh - b, l, dh))
    put((sw - r, sh - b, sw, sh), (dw - r, dh - b, dw, dh))
    # edges
    put((l, 0, sw - r, t), (l, 0, dw - r, t))
    put((l, sh - b, sw - r, sh), (l, dh - b, dw - r, dh))
    put((0, t, l, sh - b), (0, t, l, dh - b))
    put((sw - r, t, sw, sh - b), (dw - r, t, dw, dh - b))
    # center
    put((l, t, sw - r, sh - b), (l, t, dw - r, dh - b))
    return out


def filled_bar(fill_src: Image.Image, dest_size, fraction: float, leading_edge=None):
    """Horizontal left-to-right fill. fraction in 0..1."""
    dw, dh = dest_size
    frac = max(0.0, min(1.0, fraction))
    fw = int(round(dw * frac))
    if fw <= 0:
        return Image.new("RGBA", (dw, dh), (0, 0, 0, 0))
    bar = nine_slice(fill_src, (max(fw, 4), dh), SLICE_FILL)
    if fw < 4:
        bar = bar.crop((0, 0, fw, dh))
    elif fw != bar.size[0] or dh != bar.size[1]:
        bar = bar.resize((fw, dh), Image.Resampling.LANCZOS)
    out = Image.new("RGBA", (dw, dh), (0, 0, 0, 0))
    out.paste(bar, (0, 0), bar)
    if leading_edge and fw >= 3:
        # 3px highlight cap on the leading edge
        cap = Image.new("RGBA", (3, dh), (0, 0, 0, 0))
        d = ImageDraw.Draw(cap)
        d.rectangle([0, 1, 2, dh - 2], fill=rgba(leading_edge, 220))
        out.paste(cap, (max(0, fw - 3), 0), cap)
    return out


def compose_fate_bar(current: int, maximum: int, state: str) -> Image.Image:
    """state: empty | filling | ready | spent | full_ready
    Returns a 240×16 composed bar (track + fill + tick + endcaps).
    Endcaps overlay the ends and do not change the 240 display width or the 50 tick.
    """
    w, h = BAR_TRACK
    ready = state in ("ready", "full_ready")
    track = nine_slice(
        load_slice("ui_bar_track_ready.png" if ready else "ui_bar_track.png"),
        (w, h),
        SLICE_TRACK,
    )
    frac = 0 if maximum <= 0 else current / maximum
    edge = GOLD_HI if ready else None
    # Thicker carved frame: 1px shell + 2px rim → 3px inset
    inner_w, inner_h = w - 6, h - 6
    fill = filled_bar(load_slice("ui_bar_fill_fate.png"), (inner_w, inner_h), frac, edge)
    track = composite(track, fill, (3, 3))
    tick = load_slice("ui_bar_tick_ready.png" if ready else "ui_bar_tick.png")
    tx = w // 2 - tick.size[0] // 2
    ty = (h - tick.size[1]) // 2
    track = composite(track, tick, (tx, ty))
    suffix = "_ready" if ready else ""
    track = composite(track, load_slice(f"ui_bar_endcap_left{suffix}.png"), (0, 0))
    right = load_slice(f"ui_bar_endcap_right{suffix}.png")
    track = composite(track, right, (w - right.size[0], 0))
    if state == "spent":
        # Spare rust wear on an already-rolled track, not a wash.
        blood = load_slice("ui_ornament_blood.png").resize((8, 8), Image.Resampling.LANCZOS)
        track = composite(track, blood, (20, h - 7))
    return track


def compose_nameplate(
    faction: str,
    name: str,
    hp: int,
    hp_max: int,
    shield: int,
    shield_max: int,
    dead: bool = False,
    faded: bool = False,
) -> Image.Image:
    src = load_slice("ui_nameplate_dead.png" if dead else f"ui_nameplate_{faction}.png")
    plate = src.copy()
    if dead:
        plate = composite(plate, load_slice("ui_ornament_skull_ash.png").resize((12, 12), Image.Resampling.LANCZOS), (1, 1))
        # Blood sits in the top-right frame, above the number column (y < 16).
        plate = composite(plate, load_slice("ui_ornament_blood.png").resize((12, 12), Image.Resampling.LANCZOS), (74, 1))
    elif faction == "enemy":
        plate = composite(plate, load_slice("ui_ornament_skull_rust.png").resize((12, 12), Image.Resampling.LANCZOS), (1, 1))
        plate = composite(plate, load_slice("ui_ornament_blood.png").resize((12, 12), Image.Resampling.LANCZOS), (74, 1))
    else:
        # Ally: bone bracket on the top-left frame only — bottom-right would cover shield numbers.
        plate = composite(plate, load_slice("ui_ornament_bone_corner.png"), (0, 0))

    def fill_rect(rect, fill_file, cur, mx):
        x0, y0, x1, y1 = rect
        iw, ih = (x1 - x0 - 2, y1 - y0 - 2)
        frac = 0 if mx <= 0 or dead else max(0.0, min(1.0, cur / mx))
        bar = filled_bar(load_slice(fill_file), (max(1, iw), max(1, ih)), frac, None)
        return bar, (x0 + 1, y0 + 1)

    hp_bar, hp_xy = fill_rect(NP_HP, "ui_bar_fill_hp.png", hp, hp_max)
    plate = composite(plate, hp_bar, hp_xy)
    sh_bar, sh_xy = fill_rect(NP_SHIELD, "ui_bar_fill_shield.png", shield, shield_max)
    plate = composite(plate, sh_bar, sh_xy)

    draw = ImageDraw.Draw(plate)
    name_font = font(11)
    num_font = font(9)
    name_color = ASH if dead else PARCH_HI
    name_rect_w = NP_NAME[2] - NP_NAME[0]
    label = fit_ellipsis(name, name_font, name_rect_w)
    draw_text(
        draw,
        ((NP_NAME[0] + NP_NAME[2]) // 2, (NP_NAME[1] + NP_NAME[3]) // 2),
        label,
        name_font,
        name_color,
        anchor="mm",
    )
    # Numbers live in a reserved column beside the bars (not overlaid on a 5–8px track).
    hp_text = f"{hp}/{hp_max}"
    sh_text = f"{shield}"
    num_col = ASH_HI if dead else PARCH_HI
    hx = NP_HP_NUM[2] - 1
    hy = (NP_HP_NUM[1] + NP_HP_NUM[3]) // 2
    sx = NP_SH_NUM[2] - 1
    sy = (NP_SH_NUM[1] + NP_SH_NUM[3]) // 2
    for (x, y, t) in ((hx, hy, hp_text), (sx, sy, sh_text)):
        draw_text(draw, (x, y), t, num_font, num_col, anchor="rm")

    if faded:
        # Runtime dead state: CanvasGroup / color alpha 50%. Not baked into the slice.
        a = plate.split()[-1].point(lambda px: int(px * 0.5))
        plate.putalpha(a)
    return plate


def compose_button(kind: str, state: str, label: str) -> Image.Image:
    src = load_slice(f"ui_btn_{kind}_{state}.png")
    im = src.copy()
    draw = ImageDraw.Draw(im)
    f = font(14, bold=True)
    if state == "disabled":
        col = ASH
    elif kind == "primary":
        col = VOID
    else:
        col = PARCH_HI
    cx, cy = im.size[0] // 2, im.size[1] // 2
    draw_text(draw, (cx, cy - 1), label, f, col, anchor="mm")
    return im


def compose_badge(text: str) -> Image.Image:
    im = load_slice("ui_badge_combo.png").copy()
    draw = ImageDraw.Draw(im)
    # Text sits in the field to the right of the baked 12px skull (left 16px is the 9-slice endcap).
    cx = 16 + (im.size[0] - 16) // 2
    draw_text(draw, (cx, im.size[1] // 2 - 1), text, font(11, bold=True), PARCH_HI, anchor="mm")
    return im


def compose_dice_row(faces: list[str] | None, combo: str | None, pending: bool) -> Image.Image:
    """faces: three names or None for empty. combo: 双生 / 同调 / None."""
    slot_w, slot_h = SLOT
    trio_w = slot_w * 3 + SLOT_GAP * 2
    badge_w = BADGE[0] if combo else 0
    total_w = trio_w + ((BADGE_GAP + badge_w) if combo else 0)
    total_h = max(slot_h, BADGE[1]) + (8 if pending else 0)
    # Extra padding if glow
    pad = 10 if pending else 0
    canvas = Image.new("RGBA", (total_w + pad * 2 + (GLOW[0] - trio_w if pending else 0), max(total_h, GLOW[1] if pending else total_h) + pad * 2), (0, 0, 0, 0))
    # Simpler fixed canvas: glow size or tight
    cw = max(total_w + pad * 2, GLOW[0] if pending else total_w)
    ch = max(slot_h + pad * 2, GLOW[1] if pending else slot_h)
    canvas = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
    # center the trio
    trio_x = (cw - (trio_w + ((BADGE_GAP + badge_w) if combo else 0))) // 2
    trio_y = (ch - slot_h) // 2
    if pending:
        glow = load_slice("ui_dice_pending_glow.png")
        gx = (cw - glow.size[0]) // 2
        gy = (ch - glow.size[1]) // 2
        canvas = composite(canvas, glow, (gx, gy))
    empty = load_slice("ui_dice_slot_empty.png")
    for i in range(3):
        x = trio_x + i * (slot_w + SLOT_GAP)
        canvas = composite(canvas, empty, (x, trio_y))
        if faces and i < len(faces) and faces[i]:
            face = load_dice(faces[i], 42)
            fx = x + (slot_w - 42) // 2
            fy = trio_y + (slot_h - 42) // 2
            canvas = composite(canvas, face, (fx, fy))
    if combo:
        badge = compose_badge(combo)
        bx = trio_x + trio_w + BADGE_GAP
        by = trio_y + (slot_h - badge.size[1]) // 2
        canvas = composite(canvas, badge, (bx, by))
    return canvas, (trio_x, trio_y, trio_w, slot_h)


def compose_energy_block(current, maximum, state, show_can_roll_label=False) -> Image.Image:
    """Label + value + bar. Optional 可掷骰 sits on the label row, not under the bar."""
    im = Image.new("RGBA", (BAR_TRACK[0], 16 + 4 + BAR_TRACK[1]), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    label_f = font(12)
    value_f = font(11)
    d_col = PARCH_HI
    draw_text(d, (0, 8), "命运能量", label_f, d_col, anchor="lm")
    val = f"{current}/{maximum}"
    draw_text(d, (BAR_TRACK[0], 8), val, value_f, d_col, anchor="rm")
    if show_can_roll_label:
        vw = value_f.getlength(val)
        draw_text(d, (BAR_TRACK[0] - vw - 8, 8), "可掷骰", font(11, bold=True), GOLD_HI, anchor="rm")
    bar = compose_fate_bar(current, maximum, state)
    im = composite(im, bar, (0, 18))
    return im


# ---------------------------------------------------------------------------
# kit_sheet.png
# ---------------------------------------------------------------------------

def caption(draw, xy, text, size=12, fill=ASH_HI, anchor="lt"):
    draw_text(draw, xy, text, font(size), fill, anchor=anchor)


def make_kit_sheet():
    W, H = 1400, 1680
    sheet = Image.new("RGBA", (W, H), VOID)
    d = ImageDraw.Draw(sheet)
    # Title
    draw_text(d, (40, 28), "M0 TestArena HUD  —  Component Kit  (carved chrome)", font(22, bold=True), PARCH_HI, "lt")
    draw_text(d, (40, 58), "Echoes of Fate / 命运残响   ·   Q 版暗黑   ·   skulls, bones, blood, wear   ·   no baked player copy", font(13), COLD3, "lt")

    y = 96
    # Palette
    draw_text(d, (40, y), "Locked palette (UI)", font(14, bold=True), GOLD, "lt")
    y += 24
    swatches = [
        ("#0B1220 Void", VOID),
        ("#152038 Cold mid", COLD1),
        ("#1E2C4A Cold mid", COLD2),
        ("#3A4A66 Cold hi", COLD3),
        ("#C9B89A Parchment", PARCH),
        ("#E8DCC4 Parchment hi", PARCH_HI),
        ("#C9A227 Gold", GOLD),
        ("#E8C547 Gold spot", GOLD_HI),
        ("#8B2E32 Rust HP", RUST),
        ("#5C1C22 Rust sh", RUST_SH),
        ("#B84A3E Rust hi", RUST_HI),
        ("#6B6560 Ash", ASH),
        ("#2A2420 Crevice", CREVICE),
        ("#7EB3C9 Cyan spot only", CYAN),
    ]
    x = 40
    for label, col in swatches:
        sw = Image.new("RGBA", (36, 24), (0, 0, 0, 0))
        sd = ImageDraw.Draw(sw)
        sd.rounded_rectangle([0, 0, 35, 23], 3, fill=col, outline=COLD3)
        sheet = composite(sheet, sw, (x, y))
        d = ImageDraw.Draw(sheet)
        draw_text(d, (x + 42, y + 12), label, font(10), PARCH_HI, "lm")
        x += 240
        if x > 1280:
            x = 40
            y += 34
    y += 44

    # Ornaments (1x + 4x zoom — must read at UI scale)
    d = ImageDraw.Draw(sheet)
    draw_text(d, (40, y), "Ornaments  (overlay slices; never in a stretchable 9-slice center)", font(14, bold=True), GOLD, "lt")
    y += 22
    orn_items = [
        ("ui_ornament_skull.png", "Skull 16"),
        ("ui_ornament_skull_rust.png", "Skull rust"),
        ("ui_ornament_skull_ash.png", "Skull ash"),
        ("ui_ornament_bone.png", "Bone"),
        ("ui_ornament_bone_corner.png", "Bone corner"),
        ("ui_ornament_blood.png", "Blood"),
        ("ui_bar_endcap_left.png", "Bar cap L"),
        ("ui_bar_endcap_left_ready.png", "Bar cap L ready"),
        ("ui_bar_endcap_right.png", "Bar cap R"),
    ]
    x = 40
    for fname, lab in orn_items:
        im = load_slice(fname)
        zoom = im.resize((im.size[0] * 4, im.size[1] * 4), Image.Resampling.NEAREST)
        sheet = composite(sheet, im, (x, y))
        sheet = composite(sheet, zoom, (x, y + 20))
        d = ImageDraw.Draw(sheet)
        draw_text(d, (x, y + 20 + zoom.size[1] + 2), lab, font(10), ASH_HI, "lt")
        x += max(zoom.size[0], 64) + 16
    y += 110

    # Buttons
    d = ImageDraw.Draw(sheet)
    draw_text(d, (40, y), "Buttons  (carved frame; skull/bone in endcaps; labels are runtime stand-ins)", font(14, bold=True), GOLD, "lt")
    y += 28
    states = ["normal", "hover", "pressed", "disabled"]
    # secondary row
    draw_text(d, (40, y + 16), "Secondary  100×32", font(12), ASH_HI, "lm")
    x = 220
    for st in states:
        btn = compose_button("secondary", st, "掷骰")
        sheet = composite(sheet, btn, (x, y))
        d = ImageDraw.Draw(sheet)
        draw_text(d, (x + 50, y + 38), st.capitalize(), font(11), ASH_HI, "mt")
        x += 130
    # confirm as second example under hover? just one row is enough; show 确认 once
    btn = compose_button("secondary", "normal", "确认")
    sheet = composite(sheet, btn, (x + 20, y))
    d = ImageDraw.Draw(sheet)
    draw_text(d, (x + 70, y + 38), "Confirm label", font(11), ASH_HI, "mt")
    y += 70

    draw_text(d, (40, y + 18), "Primary  208×36", font(12), ASH_HI, "lm")
    x = 220
    for st in states:
        btn = compose_button("primary", st, "出征")
        sheet = composite(sheet, btn, (x, y))
        d = ImageDraw.Draw(sheet)
        draw_text(d, (x + 104, y + 42), st.capitalize(), font(11), ASH_HI, "mt")
        x += 230
    y += 80

    # 9-slice guide
    d = ImageDraw.Draw(sheet)
    draw_text(d, (40, y), "9-slice guides  (unique ornaments stay inside the marked endcaps / corners)", font(14, bold=True), GOLD, "lt")
    y += 24
    guides = (
        ("secondary", "Secondary  L14 T10 R12 B10", (14, 10, 12, 10)),
        ("primary", "Primary  L18 T10 R14 B10", (18, 10, 14, 10)),
    )
    gx = 40
    for kind, label, (bl, bt, br, bb) in guides:
        src = load_slice(f"ui_btn_{kind}_normal.png")
        zoom = src.resize((src.size[0] * 2, src.size[1] * 2), Image.Resampling.NEAREST)
        guide = zoom.copy()
        gd = ImageDraw.Draw(guide)
        col = (0x7E, 0xB3, 0xC9, 200)
        for i in range(0, guide.size[0], 6):
            gd.rectangle([i, bt * 2, i + 3, bt * 2 + 1], fill=col)
            gd.rectangle([i, guide.size[1] - bb * 2 - 1, i + 3, guide.size[1] - bb * 2], fill=col)
        for i in range(0, guide.size[1], 6):
            gd.rectangle([bl * 2, i, bl * 2 + 1, i + 3], fill=col)
            gd.rectangle([guide.size[0] - br * 2 - 1, i, guide.size[0] - br * 2, i + 3], fill=col)
        sheet = composite(sheet, guide, (gx, y))
        d = ImageDraw.Draw(sheet)
        draw_text(d, (gx, y + guide.size[1] + 6), label, font(11), ASH_HI, "lt")
        gx += guide.size[0] + 40
    y += 110

    # Fate energy
    d = ImageDraw.Draw(sheet)
    draw_text(d, (40, y), "Fate energy bar  240×16   tick at 50   loc  ui.testarena.fate_energy", font(14, bold=True), GOLD, "lt")
    y += 26
    energy_states = [
        ("Empty (0)", 0, 100, "empty", False),
        ("Filling (32/100)", 32, 100, "filling", False),
        ("Ready / 可掷骰 (62/100)", 62, 100, "ready", True),
        ("Full ready (100/100)", 100, 100, "full_ready", True),
        ("Spent, already rolled (12/100)", 12, 100, "spent", False),
    ]
    for i, (label, cur, mx, st, can) in enumerate(energy_states):
        block = compose_energy_block(cur, mx, st, show_can_roll_label=can)
        col = i % 2
        row = i // 2
        x = 40 + col * 640
        yy = y + row * 78
        sheet = composite(sheet, block, (x, yy))
        d = ImageDraw.Draw(sheet)
        draw_text(d, (x, yy + block.size[1] + 2), label, font(11), ASH_HI, "lt")
    y += 78 * 3 + 8

    # Dice result
    d = ImageDraw.Draw(sheet)
    draw_text(d, (40, y), "Dice result area  3× 48×48 slots + combo badge   (faces reused from ui_dice_*_normal)", font(14, bold=True), GOLD, "lt")
    y += 26
    variants = [
        (None, None, False, "Empty (setup / before roll)"),
        (None, None, False, "Waiting, can-roll (slots still empty)"),
        (["sword", "sword", "star"], "双生", False, "Rolled, Twin  combo.twin"),
        (["sword", "sword", "sword"], "同调", True, "Pending confirm, Attunement  combo.attunement  + glow"),
    ]
    x = 40
    yy = y
    for faces, combo, pending, label in variants:
        row, _ = compose_dice_row(faces, combo, pending)
        if x + row.size[0] > W - 40:
            x = 40
            yy += 110
        sheet = composite(sheet, row, (x, yy))
        d = ImageDraw.Draw(sheet)
        draw_text(d, (x, yy + row.size[1] + 4), label, font(11), ASH_HI, "lt")
        x += max(row.size[0], 280) + 40
    y = yy + 120

    # Nameplates
    d = ImageDraw.Draw(sheet)
    draw_text(d, (40, y), "Nameplates  88×38   rust HP  ·  gold shield  ·  numbers beside bars  ·  no 生命/护盾 words", font(14, bold=True), GOLD, "lt")
    y += 24
    plates = [
        compose_nameplate("ally", "灰烬卫士", 80, 80, 24, 40, dead=False),
        compose_nameplate("ally", "裂隙术士", 80, 80, 0, 40, dead=False),
        compose_nameplate("enemy", "锈蚀步卒", 68, 80, 0, 40, dead=False),
        compose_nameplate("ally", "锁链判官", 54, 80, 12, 40, dead=False),
        compose_nameplate("ally", "Bloodacolyte", 72, 80, 8, 40, dead=False),
        compose_nameplate("ally", "Lampsister", 60, 80, 0, 40, dead=False),
        compose_nameplate("dead", "灰烬卫士", 0, 80, 0, 40, dead=True, faded=False),
        compose_nameplate("enemy", "锈蚀步卒", 0, 80, 0, 40, dead=True, faded=False),
    ]
    labels = [
        "Ally + shield",
        "Ally, 0 shield",
        "Enemy",
        "Chinese 锁链判官",
        "Bloodacolyte (fits)",
        "Lampsister (fits)",
        "Dead (ash, then 50% fade at runtime)",
        "Enemy dead",
    ]
    x = 40
    for plate, lab in zip(plates, labels):
        # Show dead both raw and faded? The last two already use dead chrome.
        # For the first dead, also show a faded copy beside? Keep simple.
        sheet = composite(sheet, plate, (x, y))
        d = ImageDraw.Draw(sheet)
        draw_text(d, (x + 44, y + 42), lab, font(10), ASH_HI, "mt")
        x += 160
        if x > 1280:
            x = 40
            y += 70
    y += 80

    # Typography
    d = ImageDraw.Draw(sheet)
    draw_text(d, (40, y), "Typography  (px, Noto Sans CJK SC in these previews)", font(14, bold=True), GOLD, "lt")
    y += 24
    specs = [
        (14, True, "Button label 14  出征  掷骰  确认"),
        (12, False, "Energy label 12  命运能量"),
        (11, False, "Energy value / combo / name 11  62/100  同调  灰烬卫士"),
        (9, False, "Nameplate numbers 9  80/80  24"),
    ]
    for size, bold, sample in specs:
        draw_text(d, (40, y), sample, font(size, bold=bold), PARCH_HI, "lt")
        draw_text(d, (720, y), f"{size}px", font(11), ASH, "lt")
        y += size + 10

    y += 16
    d = ImageDraw.Draw(sheet)
    draw_text(
        d,
        (40, y),
        "Out of scope: 墓灯村 · shop · world map · full official HUD · 语言 / 种子 / roster / 3×3 chrome / combat log.  M0 has no lock, reroll, or crack in this flow.",
        font(12),
        ASH,
        "lt",
    )

    PREV.mkdir(parents=True, exist_ok=True)
    # Crop leftover void at the bottom (canvas is opaque #0B1220, so use color bbox).
    px = sheet.load()
    last = 0
    void = VOID[:3]
    for y in range(sheet.size[1] - 1, -1, -1):
        if any(px[x, y][:3] != void for x in range(0, sheet.size[0], 3)):
            last = y
            break
    sheet = sheet.crop((0, 0, sheet.size[0], min(sheet.size[1], last + 40)))
    bg = Image.new("RGB", sheet.size, VOID[:3])
    bg.paste(sheet, mask=sheet.split()[-1] if sheet.mode == "RGBA" else None)
    out = PREV / "kit_sheet.png"
    bg.save(out, "PNG")
    print(f"  wrote {out.relative_to(ROOT)}  {bg.size}")


# ---------------------------------------------------------------------------
# Screenshot mocks
# ---------------------------------------------------------------------------

def cover_rect(im: Image.Image, box, color=GRAYBOX_BG, radius=0):
    """Paint a solid rounded/rect cover over old chrome (RGB or RGBA image)."""
    layer = Image.new("RGBA", im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    x0, y0, x1, y1 = box
    if radius:
        d.rounded_rectangle([x0, y0, x1, y1], radius, fill=color)
    else:
        d.rectangle([x0, y0, x1, y1], fill=color)
    if im.mode != "RGBA":
        im = im.convert("RGBA")
    return Image.alpha_composite(im, layer)


def paste_rgb(base: Image.Image, layer: Image.Image, xy):
    if base.mode != "RGBA":
        base = base.convert("RGBA")
    return composite(base, layer.convert("RGBA"), xy)


def paint_energy_and_buttons(shot: Image.Image, energy, dice_faces, combo, pending, btn_states, show_march=True):
    """Overlay the right-rail kit onto a screenshot.

    energy: (current, max, state, show_can_roll)
    btn_states: dict roll, confirm, march -> state names
    show_march: False during AutoCombat / Resolve (current graybox hides 出征).
    """
    im = shot.convert("RGBA")
    # Cover old energy text, -/-/-, old buttons (and 出征 if present)
    # Seed field / language stay.
    covers = [
        # energy + placeholder line (keep well below the seed field y=98-132)
        (1355, 138, 1610, 200),
        # old 掷骰 / 确认
        (1278, 208, 1610, 256),
        # old 出征 (setup only; harmless if empty)
        (1368, 258, 1610, 312),
        # the gap we now occupy down to the new 出征
        (1355, 200, 1610, 328),
    ]
    for box in covers:
        im = cover_rect(im, box, GRAYBOX_BG, radius=0)

    cur, mx, state, can = energy
    block = compose_energy_block(cur, mx, state, show_can_roll_label=can)
    # compose_energy_block: label at y=8 center, bar at y=18.
    im = paste_rgb(im, block, (ENERGY_X, ENERGY_BAR_Y - 18))

    row, _ = compose_dice_row(dice_faces, combo, pending)
    # Align slots with the energy bar's left edge. compose_dice_row may have padding when glowing.
    # We want the first slot at ENERGY_X.
    # Find the first non-transparent column as a cheap origin, or use known padding.
    # Recompute: without glow, trio starts at 0; with glow, centered.
    # Place so the visual trio starts at ENERGY_X.
    # I'll rebuild placement explicitly here for mocks (tighter control).
    im = place_dice_on_rail(im, dice_faces, combo, pending)

    # Buttons
    roll = compose_button("secondary", btn_states["roll"], "掷骰")
    confirm = compose_button("secondary", btn_states["confirm"], "确认")
    im = paste_rgb(im, roll, (BTN_STACK_X, BTN_SEC_Y))
    im = paste_rgb(im, confirm, (BTN_STACK_X + BTN_SEC[0] + BTN_GAP, BTN_SEC_Y))
    if show_march:
        march = compose_button("primary", btn_states["march"], "出征")
        im = paste_rgb(im, march, (BTN_STACK_X, BTN_PRI_Y))
    return im


def place_dice_on_rail(im, faces, combo, pending):
    slot_w, slot_h = SLOT
    trio_w = slot_w * 3 + SLOT_GAP * 2
    x0 = ENERGY_X
    y0 = DICE_Y
    if pending:
        glow = load_slice("ui_dice_pending_glow.png")
        gx = x0 + trio_w // 2 - glow.size[0] // 2
        gy = y0 + slot_h // 2 - glow.size[1] // 2
        im = paste_rgb(im, glow, (gx, gy))
    empty = load_slice("ui_dice_slot_empty.png")
    for i in range(3):
        x = x0 + i * (slot_w + SLOT_GAP)
        im = paste_rgb(im, empty, (x, y0))
        if faces and i < len(faces) and faces[i]:
            face = load_dice(faces[i], 42)
            im = paste_rgb(im, face, (x + (slot_w - 42) // 2, y0 + (slot_h - 42) // 2))
    if combo:
        badge = compose_badge(combo)
        im = paste_rgb(im, badge, (x0 + trio_w + BADGE_GAP, y0 + (slot_h - badge.size[1]) // 2))
    return im


def cover_old_nameplate_regions(im, regions_and_colors):
    for box, color in regions_and_colors:
        im = cover_rect(im, box, color, radius=3)
    return im


def load_char(filename: str, size: int, flip: bool = False) -> Image.Image:
    """Official production idles already on this box; used only to rebuild messy combat portraits."""
    path = Path("/workspace/my-game1/Assets/_Project/Art/Characters") / filename
    im = Image.open(path).convert("RGBA")
    if flip:
        im = im.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    return im.resize((size, size), Image.Resampling.LANCZOS)


def restore_combat_midfield(m: Image.Image, setup: Image.Image, combat: Image.Image) -> Image.Image:
    """Replace the overflowing 60×26-designed cards with clean cells + aisle, then official portraits."""
    if m.mode != "RGBA":
        m = m.convert("RGBA")
    setup = setup.convert("RGBA")
    combat = combat.convert("RGBA")

    # Empty ally_b2 (same cell is empty in the setup shot)
    m.paste(setup.crop((294, 375, 385, 454)), (294, 375))
    # ally_c2: empty fill from b2 + the ally_c2 slot pill from setup
    m.paste(setup.crop((294, 375, 385, 432)), (390, 375))
    m.paste(setup.crop((390, 432, 481, 454)), (390, 432))
    # 6px gutters between mid-row ally cells
    gap = setup.crop((288, 375, 294, 454))
    m.paste(gap, (288, 375))
    m.paste(gap, (385, 375))
    # Clean aisle (copy from unused bottom-row aisle, which has the divider and no units)
    aisle = combat.crop((481, 471, 650, 550))
    m.paste(aisle, (481, 375))
    # Cover the leftover stacked enemy card that spilled into the row gap
    aisle_cap = combat.crop((481, 471, 650, 516))
    m.paste(aisle_cap, (481, 330))

    # Official portraits, combat display ~40 (body ~32 + ring)
    rift = load_char("chr_ally_riftmage_idle_01.png", 40)
    ash = load_char("chr_ally_ashguard_idle_01.png", 40)
    rust = load_char("chr_enemy_placeholder_idle.png", 40, flip=True)

    # 裂隙术士 in ally_b2, feet just above the nameplate
    # 灰烬卫士 in the aisle, left of the divider (~x=565)
    # 锈蚀步卒 in the aisle, right of the divider
    portraits = [
        (rift, 340, 415),  # center_x, feet_y
        (ash, 520, 415),
        (rust, 610, 415),
    ]
    for spr, cx, feet_y in portraits:
        x = int(cx - spr.size[0] / 2)
        y = int(feet_y - spr.size[1])
        m = paste_rgb(m, spr, (x, y))
    return m, portraits


def make_mocks():
    setup = Image.open(REFS / "testarena-setup.png")
    combat = Image.open(REFS / "testarena-combat.png")

    # --- mock_setup: rail only. Brief says keep the rest of the layout;
    # nameplates are shown on combat units and on mock_nameplates.png. ---
    m = paint_energy_and_buttons(
        setup,
        energy=(0, 100, "empty", False),
        dice_faces=None,
        combo=None,
        pending=False,
        btn_states={"roll": "disabled", "confirm": "disabled", "march": "normal"},
    )
    out = PREV / "mock_setup.png"
    m.convert("RGB").save(out, "PNG")
    print(f"  wrote {out.relative_to(ROOT)}  {m.size}")

    def combat_with_new_plates(base_rgb, energy, faces, combo, pending, btn_states):
        m = paint_energy_and_buttons(base_rgb, energy, faces, combo, pending, btn_states, show_march=False)
        m, _portraits = restore_combat_midfield(m, setup, combat)
        p_rift = compose_nameplate("ally", "裂隙术士", 80, 80, 0, 40)
        p_ash = compose_nameplate("ally", "灰烬卫士", 60, 80, 0, 40)
        p_rust = compose_nameplate("enemy", "锈蚀步卒", 68, 80, 0, 40)
        # Hang each plate just under the token feet, centered on the sprite.
        # ash @520 and rust @610 are 90px apart; 88px plates then sit 2px apart, just
        # either side of the mid-line, without overlapping.
        py = 417
        placements = [
            (p_rift, 340 - p_rift.size[0] // 2, py),
            (p_ash, 520 - p_ash.size[0] // 2, py),
            (p_rust, 610 - p_rust.size[0] // 2, py),
        ]
        for plate, x, y in placements:
            m = paste_rgb(m, plate, (int(x), int(y)))
        return m

    ready = combat_with_new_plates(
        combat,
        energy=(62, 100, "ready", True),
        faces=None,
        combo=None,
        pending=False,
        btn_states={"roll": "normal", "confirm": "disabled", "march": "disabled"},
    )
    out = PREV / "mock_combat_ready.png"
    ready.convert("RGB").save(out, "PNG")
    print(f"  wrote {out.relative_to(ROOT)}  {ready.size}")

    pending = combat_with_new_plates(
        combat,
        energy=(12, 100, "spent", False),
        faces=["sword", "sword", "sword"],
        combo="同调",
        pending=True,
        btn_states={"roll": "disabled", "confirm": "normal", "march": "disabled"},
    )
    out = PREV / "mock_combat_pending.png"
    pending.convert("RGB").save(out, "PNG")
    print(f"  wrote {out.relative_to(ROOT)}  {pending.size}")


def make_nameplate_comparison():
    W, H = 1280, 720
    im = Image.new("RGBA", (W, H), VOID)
    d = ImageDraw.Draw(im)
    draw_text(d, (48, 32), "Nameplates  —  old 60×26 wrap vs new 88×38", font(22, bold=True), PARCH_HI, "lt")
    draw_text(
        d,
        (48, 64),
        "Old plates wrap English names and the 生命 / 护盾 labels. New plates: one-line name (ellipsis if needed), rust HP, gold shield, numbers only.",
        font(13),
        COLD3,
        "lt",
    )

    # --- Old 60×26 recreation ---
    draw_text(d, (48, 110), "Before  (60×26, default green — forbidden)", font(14, bold=True), RUST_HI, "lt")

    def old_plate(name, hp_line, wrap_second, bar_frac, w=60, h=26):
        # Recreate the wrap problem on a Unity-like muted green card
        card = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        body = rounded_layer((w, h), 2, (0x2E, 0x3D, 0x35, 255), (0x47, 0x60, 0x56, 255), 1, 4)
        card = composite(card, body, (0, 0))
        dd = ImageDraw.Draw(card)
        f9 = font(9)
        f8 = font(8)
        draw_text(dd, (3, 1), name if font(9).getlength(name) <= w - 6 else name, f9, (0xE8, 0xE8, 0xE8, 255), "lt")
        # Force wrap like the current UI: first line name, then 生命/护盾 wrapping
        draw_text(dd, (3, 11), hp_line, f8, (0xC8, 0xC8, 0xC8, 255), "lt")
        if wrap_second:
            draw_text(dd, (3, 19), wrap_second, f8, (0xC8, 0xC8, 0xC8, 255), "lt")
        # green bar along the bottom, overflowing the 26px box to show the problem
        # (the real UI overflows the designed 60×26)
        return card

    def old_plate_overflow(name, hp_cur, shield, faction="ally"):
        """Designed 60×26, but content overflows the way the graybox does."""
        # Draw on a larger card to show the overflow, with a 60×26 frame marked.
        canvas = Image.new("RGBA", (92, 44), (0, 0, 0, 0))
        fill = (0x2E, 0x3D, 0x35, 255) if faction == "ally" else (0x47, 0x2A, 0x2A, 255)
        rim = (0x47, 0x60, 0x56, 255) if faction == "ally" else (0x6A, 0x42, 0x42, 255)
        # The overflowing actual box ~90×40
        body = rounded_layer((90, 40), 2, fill, rim, 1, 4)
        canvas = composite(canvas, body, (0, 0))
        dd = ImageDraw.Draw(canvas)
        f11 = font(11)
        f9 = font(9)
        draw_text(dd, (4, 2), name, f11, (0xEE, 0xEE, 0xEE, 255), "lt")
        draw_text(dd, (4, 15), "生命 " + str(hp_cur) + " 护盾", f9, (0xD0, 0xD0, 0xD0, 255), "lt")
        draw_text(dd, (4, 25), str(0 if isinstance(shield, int) else shield), f9, (0xD0, 0xD0, 0xD0, 255), "lt")
        # default green bar
        bar_w = 80
        dd.rectangle([4, 34, 4 + bar_w, 38], fill=(0x1A, 0x1A, 0x1A, 255))
        fw = int(bar_w * min(1, hp_cur / 80 if hp_cur else 0) or 0)
        if hp_cur > 0:
            # if we don't know, fill by hp/80 for the recreation
            frac = 1.0 if hp_cur >= 80 else hp_cur / 80
            dd.rectangle([4, 34, 4 + int(bar_w * frac), 38], fill=(0x73, 0xBF, 0x6A, 255))
        # 60×26 design frame in rust-ish outline to show the intended box
        dd.rectangle([0, 0, 59, 25], outline=(0xE8, 0xC5, 0x47, 200))
        return canvas

    olds = [
        (old_plate_overflow("裂隙术士", 80, 0), "Chinese + 生命/护盾 wrap"),
        (old_plate_overflow("Bloodacolyte", 72, 0), "English name + wrap"),
        (old_plate_overflow("Lampsister", 60, 0), "Lampsister wrap"),
        (old_plate_overflow("锈蚀步卒", 68, 0, "enemy"), "Enemy, same wrap"),
    ]
    x = 48
    y = 140
    for plate, lab in olds:
        im = composite(im, plate, (x, y))
        d = ImageDraw.Draw(im)
        draw_text(d, (x, y + 48), lab, font(11), ASH_HI, "lt")
        x += 200
    d = ImageDraw.Draw(im)
    draw_text(d, (48, y + 72), "Gold outline = the designed 60×26 box the current text overflows.", font(11), ASH, "lt")

    # --- New plates ---
    draw_text(d, (48, 280), "After  88×38  (this kit)", font(14, bold=True), GOLD, "lt")
    news = [
        (compose_nameplate("ally", "灰烬卫士", 80, 80, 24, 40), "Ally with shield"),
        (compose_nameplate("enemy", "锈蚀步卒", 68, 80, 0, 40), "Enemy"),
        (compose_nameplate("dead", "裂隙术士", 0, 80, 0, 40, dead=True, faded=False), "Dead, ash chrome"),
        (compose_nameplate("ally", "Bloodacolyte", 72, 80, 8, 40), "Bloodacolyte, one line"),
        (compose_nameplate("ally", "Lampsister", 60, 80, 0, 40), "Lampsister, one line"),
        (compose_nameplate("ally", "锁链判官", 54, 80, 12, 40), "锁链判官, one line"),
    ]
    x = 48
    y = 312
    for plate, lab in news:
        # For the dead one, it's already 50% faded; draw a faint token behind it so the fade reads
        im = composite(im, plate, (x, y))
        d = ImageDraw.Draw(im)
        draw_text(d, (x + 44, y + 44), lab, font(11), ASH_HI, "mt")
        x += 200
        if x > 1200:
            x = 48
            y += 90

    y = 470
    d = ImageDraw.Draw(im)
    draw_text(d, (48, y), "On a unit token  (setup ~44px, combat portrait ~32px)", font(14, bold=True), GOLD, "lt")
    y += 28
    # Fake tokens + plates
    def token_demo(cx, cy, plate, token_size, ring):
        tok = rounded_layer((token_size + 8, token_size + 8), (token_size + 8) // 2, COLD2, ring, 2, 4)
        nonlocal im
        im = composite(im, tok, (cx - tok.size[0] // 2, cy - tok.size[1] // 2))
        im = composite(im, plate, (cx - plate.size[0] // 2, cy + token_size // 2 + 2))

    token_demo(140, y + 24, compose_nameplate("ally", "灰烬卫士", 80, 80, 24, 40), 44, GOLD)
    token_demo(360, y + 30, compose_nameplate("ally", "裂隙术士", 80, 80, 0, 40), 32, GOLD)
    token_demo(580, y + 30, compose_nameplate("enemy", "锈蚀步卒", 68, 80, 0, 40), 32, RUST)
    token_demo(800, y + 30, compose_nameplate("dead", "Lampsister", 0, 80, 0, 40, dead=True, faded=True), 32, ASH)
    d = ImageDraw.Draw(im)
    draw_text(d, (140, y + 90), "Setup token 44 + plate", font(11), ASH_HI, "mt")
    draw_text(d, (360, y + 90), "Combat 32 + ally", font(11), ASH_HI, "mt")
    draw_text(d, (580, y + 90), "Combat 32 + enemy", font(11), ASH_HI, "mt")
    draw_text(d, (800, y + 90), "Dead", font(11), ASH_HI, "mt")

    draw_text(
        d,
        (48, 680),
        "Ellipsis rule: name rect is 80×13 inside the plate. Bloodacolyte (68px at 11pt) and Lampsister (57px) fit without clipping; longer strings truncate with …",
        font(12),
        ASH_HI,
        "lt",
    )

    bg = Image.new("RGB", im.size, VOID[:3])
    bg.paste(im, mask=im.split()[-1])
    out = PREV / "mock_nameplates.png"
    bg.save(out, "PNG")
    print(f"  wrote {out.relative_to(ROOT)}  {bg.size}")


def main():
    print("== slices ==")
    make_slices()
    print("== manifest ==")
    write_manifest()
    print("== kit sheet ==")
    make_kit_sheet()
    print("== mocks ==")
    make_mocks()
    print("== nameplate comparison ==")
    make_nameplate_comparison()
    print("done")


if __name__ == "__main__":
    main()
