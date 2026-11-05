# Abyss Hook / Nia Island — Art Style Guide (MVP Slice 1)

**Tone:** Lovecraftian pier at night — fog, black rocks, purple moonlight hints, old lighthouse.  
**Protagonist:** Black cat **Nia**, golden vertical pupils, fishing on the pier.  
**Engine:** Unity 2D · import filter **Nearest** for all pixel sprites.

## Palette (lock)

| Role | Hex | Use |
|------|-----|-----|
| Void / night water | `#0B0A12` | Abyss, deep water |
| Ink outline | `#1A1520` | Contours |
| Wet black rock | `#1C1E28` | Rocks, pier posts |
| Pier plank | `#3A3228` | Wood boards |
| Fog mid | `#4A4A5C` | Fog banks |
| Moon purple | `#6B4A8A` | Moonlight wash, distant lighthouse glow |
| Deep teal | `#1A3A3A` | Water midtones |
| Phosphor / bite cue | `#6BFF9A` | Bite flash, rare magic |
| Gold pupil | `#E8C84A` | Nia eyes only (signature) |
| Bone / UI text | `#C8B89A` | Labels, parchment UI |
| Blood rust | `#8B3A3A` | Fail / danger accents |
| Cat fur | `#121214`–`#2A2A32` | Nia body (near-black with cool edge light) |

No pure white. Gold pupils are the only warm “friendly” accent on Nia.

## Resolution targets

| Asset | Size | Notes |
|-------|------|-------|
| Nia character cell | **64×64** | Feet bottom-center; hard 1px ink outline |
| Nia walk sheet | 64×N frames | Horizontal strip, 1px padding between frames |
| Pier backdrop | 1920×1080 or 960×540 | Soft fog OK on BG only; pixel props optional |
| Cast/Hook button | 256×96 (or 128×48) | Big, readable; two label states |
| Bite cue | 64×64 or overlay flash | Phosphor pull / flash — unmistakable |
| Fish silhouettes | 64×32 or 48×24 | Side view, transparent BG |

## Silhouette notes

- **Nia:** Rounded cat silhouette, upright or slight crouch while fishing; ears + tail readable; **golden slit pupils** must read at 50% zoom.
- **Pier:** Horizontal boards + vertical posts; black rocks below; lighthouse far left/right as small silhouette.
- **Fog:** Soft layered planes behind pier; keep playable area contrast high (Nia + button never lost in fog).
- **Fish:** Distinct silhouettes — Minnow small/round, Smelt mid with faint lantern belly, Whisper Eel long/sinuous.

## Naming

```
char_nia_idle_00.png … 
char_nia_walk_00.png …
sheet_nia_idle.png
sheet_nia_walk.png
bg_pier_night.png
ui_btn_cast.png
ui_btn_hook.png
ui_cue_bite.png
fish_gutter_sil.png
fish_lantern_sil.png
fish_whisper_sil.png
```

## Out of style

- Soft painted / hand-painted fantasy (retired)
- Bright daytime cartoony colors
- Photoreal water
- Bilinear-blurred sprites in source files

## Engineer handoff

- Characters/UI/fish: **Filter Mode = Point (no filter)**, compression none/lossless.
- Backdrop may use Bilinear if soft fog layers.
- Pivot: feet for Nia; center for UI/fish.
- Place under `Assets/Art/` with this guide at `docs/art/STYLE_GUIDE.md`.
