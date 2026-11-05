# Abyss Hook MVP Art — Usage Notes

**Placeholder status:** Grok Imagine concept sheets. Slice into final Unity sprites as needed; mark any temporary assets `PLACEHOLDER` in importer until Verifier signs off.

## Paths (this PR)

| File | Purpose |
|------|---------|
| `docs/art/STYLE_GUIDE.md` | Palette, 64×64 targets, silhouette, naming, Nearest filter |
| `docs/art/USAGE.md` | This file |
| `Assets/Art/Style/palette_abyss_hook.png` | Locked palette board reference |
| `Assets/Art/Characters/sheet_nia_idle_walk.png` | Nia idle (4) + walk R/L concept sheet — **slice to 64×64 cells** |
| `Assets/Art/Backgrounds/bg_pier_night.png` | Pier / fog / lighthouse backdrop for `Pier` scene |
| `Assets/Art/UI/ui_fishing_controls.png` | Cast + Hook button states + Bite cue — **slice to separate sprites** |
| `Assets/Art/Fish/fish_silhouettes_mvp.png` | Gutter Minnow, Lantern Smelt, Whisper Eel silhouettes (E2) |

## Unity import

- Characters / UI / fish: **Filter Mode = Point**, Compression = None, Sprite Mode = Multiple (slice).
- Backdrop: Sprite / Texture; Bilinear OK if soft fog preferred.
- Nia pivot: Bottom (feet). UI/fish: Center.
- Flip walk-left in code if only walk-right cells are used.

## Engineer wiring (no new gameplay)

- Ready → show `ui_btn_cast`; BiteWindow → swap label/sprite to `ui_btn_hook` + flash `ui_cue_bite`.
- Backdrop behind pier collider strip; Nia clamped on pier.

## Known follow-ups

- True transparent PNGs after manual / tool slice (source sheets may still show checker).
- Separate `char_nia_idle_XX` / `char_nia_walk_XX` frames once Engineer confirms cell size.
