# Echoes of Fate — M0 art USAGE

Art-only pack for TestArena. Do not treat prior Abyss Hook / Cthulhu sheets as production for this mode.

## Import (Unity 6 / URP 2D)

| Setting | Value |
|---------|--------|
| Texture Type | Sprite (2D and UI) |
| Pixels Per Unit | 64 |
| Filter Mode | Bilinear |
| Compression | None or High quality for UI; default for env |
| Mesh Type | Tight or Full Rect (dice = Full Rect) |
| Pivot | See STYLE_GUIDE (characters at feet / ring; dice center; ground center) |

Sorting layers: `BGFar` < `BGMid` < `Ground` < `UnitsRing` < `Units` < `FG` < `UI`. Y-sort units by foot pivot. 2D Lights may tint gold (ally lamp) or rust/cyan (rift) — keep them local, not full-screen.

Generated frames may sit on void `#0B1220` or a chroma key; if a PNG is not pre-multiplied transparent, mask the void before shipping to a scene.

## Paths

```
docs/art/STYLE_GUIDE.md
docs/art/USAGE.md
Assets/_Project/Art/UI/Dice/ui_dice_{face}_{state}.png
Assets/_Project/Art/UI/Dice/sheets/ui_dice_faces_{normal|locked|cracked}.png
Assets/_Project/Art/Environment/TestArena/env_riftarena_{bg_far|bg_mid|ground|fg}.png
Assets/_Project/Art/Characters/chr_ally_placeholder_melee_idle.png
Assets/_Project/Art/Characters/chr_ally_placeholder_caster_idle.png
Assets/_Project/Art/Characters/chr_enemy_placeholder_idle.png
```

The `sheets/` contact sheets are the uncut source. Import the eighteen sliced `ui_dice_{face}_{state}.png` files, not the sheets.

## Dice states

- `normal` — parchment face, gold rim, clear symbol
- `locked` — iron/wax seal, cooler and dimmer, still readable
- `cracked` — rust-red fissures through the face (maps to `status_crack` presentation)

Faces: sword, shield, star, feather, coin, skull.

## TestArena (Rift Battlefield)

`env_riftarena_ground` is an isometric field marked for **3×3 ally vs 3×3 enemy** with a mid-line. Place units on cell centers. BG Far / Mid are backdrops; FG is optional near-edge ash / cracked stone (must not hide footholds).

## Placeholders

Idle only in M0. Melee = front-line cloak + polearm. Caster = pointed hood + lantern or staff. Enemy shares one look for `enemy_c1` / `enemy_c2`, distinguished from allies by rust ring and rust outline (no grave-lamp gold).
