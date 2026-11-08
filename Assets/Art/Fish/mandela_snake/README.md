# Mandela Snake / 曼德拉蛇

Catchable fish/monster art for Abyss Hook (MVP-adjacent). Soft hand-painted Cthulhu style per `docs/art/STYLE_GUIDE.md` v2 (heterogeneity / oppression / erosion). **Not** pixel.

## Files
| File | Use |
|------|-----|
| `mandela_snake_side.png` | Side silhouette / in-water sprite reference |
| `mandela_snake_portrait.png` | Colored catch portrait (UI) |
| `mandela_snake_bite.png` | Bite / reel action frame |

## Import notes
- Prefer PNG with alpha; generated frames sit on void `#0B0E14` — mask to transparent in engine if needed.
- Palette: rock / rot-moss / bone-ash; eldritch `#6EC8B8` / `#9B7AE8` spot-only (eyes).
- Scale target: readable near ~128px character height; slice/crop to final sprite bounds in Unity.
- No gameplay code in this pack.
- Legacy pixel sheet `Assets/Art/Fish/fish_silhouettes_mvp.png` stays as-is.

## Naming
`mandela_snake_<side|portrait|bite>.png` under `Assets/Art/Fish/mandela_snake/`.
