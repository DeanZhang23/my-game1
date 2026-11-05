# Art usage — legacy pixel MVP + locked hand-painted terrain

Two packs live side by side. Gameplay and the GDD are unchanged; these notes only say where the art sits and how to import it.

## Legacy pixel MVP (placeholders)

**Status:** Legacy placeholders pending reskin. Sheets under `Assets/Art/Characters`, `Assets/Art/UI`, `Assets/Art/Fish`, and `Assets/Art/Backgrounds` (plus `Assets/Art/Style/palette_abyss_hook.png`) stay until a hand-painted pass replaces them. They are **not** the locked style. Production direction is `docs/art/STYLE_GUIDE.md` (v2).

**Placeholder status:** Grok Imagine concept sheets. Slice into final sprites as needed; mark any temporary assets `PLACEHOLDER` in importer until Verifier signs off.

### Paths

| File | Purpose |
|------|---------|
| `docs/art/STYLE_GUIDE.md` | Locked hand-painted v2 guide (supersedes the pixel MVP sheet style) |
| `docs/art/USAGE.md` | This file |
| `Assets/Art/Style/palette_abyss_hook.png` | Pixel-MVP palette board (legacy) |
| `Assets/Art/Characters/sheet_nia_idle_walk.png` | Nia idle (4) + walk R/L concept sheet — **slice to 64×64 cells** |
| `Assets/Art/Backgrounds/bg_pier_night.png` | Pier / fog / lighthouse backdrop for `Pier` scene |
| `Assets/Art/UI/ui_fishing_controls.png` | Cast + Hook button states + Bite cue — **slice to separate sprites** |
| `Assets/Art/Fish/fish_silhouettes_mvp.png` | Gutter Minnow, Lantern Smelt, Whisper Eel silhouettes (E2) |

### Unity import (legacy pixel only)

- Characters / UI / fish: **Filter Mode = Point**, Compression = None, Sprite Mode = Multiple (slice).
- Backdrop: Sprite / Texture; Bilinear OK if soft fog preferred.
- Nia pivot: Bottom (feet). UI/fish: Center.
- Flip walk-left in code if only walk-right cells are used.

### Engineer wiring (existing hooks; no new gameplay)

- Ready → show `ui_btn_cast`; BiteWindow → swap label/sprite to `ui_btn_hook` + flash `ui_cue_bite`.
- Backdrop behind pier collider strip; Nia clamped on pier.

### Known follow-ups

- True transparent PNGs after manual / tool slice (source sheets may still show checker).
- Separate `char_nia_idle_XX` / `char_nia_walk_XX` frames once Engineer confirms cell size.
- Reskin Characters, UI, Fish, and Backgrounds to the hand-painted lock.

## Terrain (locked hand-painted)

Soft hand-painted modular terrain. Mood: 异质 / 压抑 / 侵蚀. Palette and rules: `docs/art/STYLE_GUIDE.md`. Pack notes: `Assets/Art/Terrain/README.md`.

| File | Purpose |
|------|---------|
| `Assets/Art/Terrain/sheet_terrain_base_v2.png` | Preferred modular sheet — rocks, rot-moss overhang, waterfall, pond, ruined shrine |
| `Assets/Art/Terrain/tiles_walkable_set.png` | Walkable caps, fills, and floating-island strip |
| `Assets/Art/Terrain/sheet_terrain_base_v1.png` | Earlier pass (archive). Prefer v2 |
| `Assets/Art/Terrain/README.md` | Pack index |

### Import

- Concept sheets (1280×720). Slice to **64×64** (or native module bounds) before final import.
- **Filter Mode = Bilinear** (soft paint). Compression = None until sliced. Sprite Mode = Multiple.
- Keep walkable tops and collision outlines clear. Do not cover footholds with eldritch glow or debris.
- Structure follows the terrain-base north star. Mood and palette follow the locked guide (sick rot moss and cold rock, not cozy green).
