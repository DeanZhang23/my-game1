# Echoes of Fate — art USAGE (M0 + official 8-ally idles)

**Style:** Q 版暗黑 (chibi dark-fantasy). See `STYLE_GUIDE.md`.  
The earlier realistic / painterly M0 draft is **void**. Do not import it into TestArena. Prior Abyss Hook / Cthulhu sheets are also not for this mode.

## Import (Unity 6 / URP 2D)

| Setting | Value |
|---------|--------|
| Texture Type | Sprite (2D and UI) |
| Pixels Per Unit | 64 |
| Filter Mode | Bilinear |
| Compression | None or High quality for UI |
| Mesh Type | Dice = Full Rect; characters Tight or Full Rect |
| Pivot | Characters at feet / ring; dice center; ground center |

Sorting layers: `BGFar` < `BGMid` < `Ground` < `UnitsRing` < `Units` < `FG` < `UI`. Y-sort units by foot pivot. Keep 2D Lights local (gold lamp or rust/cyan rift), never a full-screen wash.

If a PNG still has void `#0B1220` instead of alpha, mask it before placing in a scene. Production slices in this pack are authored with transparency.

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
Assets/_Project/Art/Characters/chr_ally_{ashguard|oathblade|lampsister|riftmage|misthunter|boneapostle|chainjudge|bloodacolyte}_idle_01.png
```

## Dice

- `normal` — flat parchment, gold rim, clear icon
- `locked` — padlock **inside** the face square
- `cracked` — graphic rust cracks (`status_crack`)

Star is a magic spark-star, not a hexagram. Coin is a gold coin, not a compass.

## TestArena

`env_riftarena_ground` is the only playable field (3×3 vs 3×3). Mid is a backdrop. Far is sky/void. FG is a bottom overlay only.

## Placeholders

Idle only. All Q-version (big head, short body). Melee = cloak + polearm. Caster = pointed hood + lantern or staff. Enemy uses a **different silhouette** from melee, plus rust ring / rust outline and no gold.

`chr_ally_placeholder_*` and `chr_enemy_placeholder_idle` remain **temporary stand-ins** until the official 8-ally idles below are approved and merged. Do not delete the M0 dice or TestArena environment pack.

## Official 8-ally idles

Files: `Assets/_Project/Art/Characters/chr_ally_{id}_idle_01.png`

IDs: `ashguard` (灰烬卫士), `oathblade` (烬誓剑士), `lampsister` (墓灯修女), `riftmage` (裂隙术士), `misthunter` (雾行猎手), `boneapostle` (骨匣使徒), `chainjudge` (锁链判官), `bloodacolyte` (血契侍从).

| Setting | Value |
|---------|--------|
| Canvas | 128×128 RGBA |
| Pixels Per Unit | 64 |
| Filter Mode | Bilinear |
| Pivot | Feet / gold ring, about (0.5, 0.12–0.18) — measured on this pack: approximately (0.50, 0.12–0.13) |
| Facing | Right (toward the enemy side) |
| Ground ring | Grave-lamp gold `#C9A227` |

Import as Sprite (2D and UI). Y-sort by the foot / ring pivot. These are idle stills only — no animation sets in this pack.
