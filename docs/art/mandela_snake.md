# Mandela Snake / 曼德拉蛇

Hand-painted catchable fish/monster pack (MVP-adjacent). Follows the STYLE_GUIDE v2 hand-painted lock (异质 / 压抑 / 侵蚀): rock, rot-moss, bone-ash, eldritch color on the eyes only. Does not replace the legacy pixel sheet `Assets/Art/Fish/fish_silhouettes_mvp.png`. No gameplay wiring.

| File | Purpose |
|------|---------|
| `Assets/Art/Fish/mandela_snake/mandela_snake_side.png` | Side silhouette / in-water sprite reference (1280×720) |
| `Assets/Art/Fish/mandela_snake/mandela_snake_portrait.png` | Colored catch portrait for UI (1024×1024) |
| `Assets/Art/Fish/mandela_snake/mandela_snake_bite.png` | Bite / reel action frame (1280×720) |
| `Assets/Art/Fish/mandela_snake/README.md` | Palette, import, and naming |

Frames sit on void `#0B0E14`; mask to transparent in engine if needed. Import as soft paint (Bilinear), not the Point filter used by the pixel fish sheet.
