# Echoes of Fate — art USAGE (M0 + official 8-ally idles + walk & attack + enemy placeholder walk & attack + M0 hit flash & death puff)

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
| Pivot | Characters at feet / ring; dice center; ground center. Dressing overlays, foothold marks, and rust / crack / ashpuff decals: center (0.5, 0.5). Standing dressing props (rubble, ash, chip, midline stones): bottom center (0.5, 0.1) |

Sorting layers: `BGFar` < `BGMid` < `Ground` < `UnitsRing` < `Units` < `FG` < `UI`. Y-sort units by foot pivot. Keep 2D Lights local (gold lamp or rust/cyan rift), never a full-screen wash.

If a PNG still has void `#0B1220` instead of alpha, mask it before placing in a scene. Production slices in this pack are authored with transparency.

## Paths

```
docs/art/STYLE_GUIDE.md
docs/art/USAGE.md
Assets/_Project/Art/UI/Dice/ui_dice_{face}_{state}.png
Assets/_Project/Art/UI/Dice/sheets/ui_dice_faces_{normal|locked|cracked}.png
Assets/_Project/Art/Environment/TestArena/env_riftarena_{bg_far|bg_mid|ground|fg}.png
Assets/_Project/Art/Environment/TestArena/env_riftarena_prop_*.png
docs/art/previews/lockpass_riftarena_dressing.jpg
Assets/_Project/Art/Characters/chr_ally_placeholder_melee_idle.png
Assets/_Project/Art/Characters/chr_ally_placeholder_caster_idle.png
Assets/_Project/Art/Characters/chr_enemy_placeholder_idle.png
Assets/_Project/Art/Characters/chr_ally_{ashguard|oathblade|lampsister|riftmage|misthunter|boneapostle|chainjudge|bloodacolyte}_idle_01.png
Assets/_Project/Art/Characters/chr_ally_{id}_walk_01.png … chr_ally_{id}_walk_04.png
Assets/_Project/Art/Characters/chr_ally_{id}_attack_01.png … chr_ally_{id}_attack_04.png
Assets/_Project/Art/Characters/chr_enemy_placeholder_walk_01.png … chr_enemy_placeholder_walk_04.png
Assets/_Project/Art/Characters/chr_enemy_placeholder_attack_01.png … chr_enemy_placeholder_attack_04.png
Assets/_Project/Art/VFX/fx_hit_flash_{ally|enemy}_01.png … fx_hit_flash_{ally|enemy}_04.png
Assets/_Project/Art/VFX/fx_death_puff_{ally|enemy}_01.png … fx_death_puff_{ally|enemy}_04.png
```

Walk and attack paths are shipped for all eight playable allies (`ashguard`, `oathblade`, `lampsister`, `riftmage`, `misthunter`, `boneapostle`, `chainjudge`, `bloodacolyte`). Walk and attack paths for the single M0 enemy placeholder (`unit_placeholder_enemy`, 锈蚀步卒 / Rustsoldier) are also shipped. `unit_placeholder_enemy` is wired into TestArena. M0 hit-flash and death-puff paths are shipped (see below).

## Dice

- `normal` — flat parchment, gold rim, clear icon
- `locked` — padlock **inside** the face square
- `cracked` — graphic rust cracks (`status_crack`)

Star is a magic spark-star, not a hexagram. Coin is a gold coin, not a compass.

## TestArena

`env_riftarena_ground` is the only playable field (3×3 vs 3×3). Mid is still a distant backdrop, not a second battlefield. Far is sky/void. Foreground is a bottom overlay only and still must not hide footholds.

The original M0 plates `env_riftarena_{ground,bg_far,bg_mid,fg}.png` remain the locked environment pack. They are not voided or replaced. Their PNGs, paths, GUIDs, pivots, and importer settings stay as they are, and they still have no `.meta` files.

## TestArena dressing props (additive)

This is an additive dressing layer for the TestArena rift battlefield. It helps read the 3×3 vs 3×3 mid-line and the footholds. It does not replace the locked ground plate. This pack does not wire TestArena scenes or code.

Sort order: dressing sits on **Ground**, under **UnitsRing** / **Units**, so characters stay readable on top.

```
BGFar < BGMid < Ground (locked plate, then dressing) < UnitsRing < Units < FG < UI
```

Preview: `docs/art/previews/lockpass_riftarena_dressing.jpg`.

Folder: `Assets/_Project/Art/Environment/TestArena/`.

Registered 1280×720 overlays (same canvas as the locked plates). Pivot center (0.5, 0.5):

- `env_riftarena_prop_midline.png` — midline rust, crack, and ash ticks
- `env_riftarena_prop_footholds.png` — 16 foothold marks (the two cells that fall in the rift itself are unmarked)
- `env_riftarena_prop_edge_rubble.png` — edge rubble, ash, and chips
- `env_riftarena_prop_dressing.png` — optional convenience composite of the three overlays above

Loose decals and foothold marks. Pivot center (0.5, 0.5):

- `env_riftarena_prop_rust_01.png` … `env_riftarena_prop_rust_05.png`
- `env_riftarena_prop_crack_01.png` … `env_riftarena_prop_crack_05.png`
- `env_riftarena_prop_ashpuff_01.png` … `env_riftarena_prop_ashpuff_04.png`
- `env_riftarena_prop_foothold_west.png`
- `env_riftarena_prop_foothold_east.png`

Standing props. Pivot bottom center (0.5, 0.1):

- `env_riftarena_prop_rubble_01.png` … `env_riftarena_prop_rubble_03.png`
- `env_riftarena_prop_ash_01.png`, `env_riftarena_prop_ash_02.png`
- `env_riftarena_prop_chip_01.png`, `env_riftarena_prop_chip_02.png`
- `env_riftarena_prop_midline_stones.png`

Naming: `env_riftarena_prop_*.png`.

| Setting | Value |
|---------|--------|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Single |
| Pixels Per Unit | 64 |
| Filter Mode | Bilinear |
| alphaIsTransparency | On |
| Mipmaps | Off |
| Compression | None / uncompressed (same as character sprite art) |
| Pivot | Overlays, foothold marks, rust / crack / ashpuff: (0.5, 0.5). Rubble, ash, chip, midline stones: (0.5, 0.1) |

## Placeholders

Ally melee and caster placeholders remain idle-only. The enemy placeholder (`unit_placeholder_enemy`, 锈蚀步卒 / Rustsoldier) walk and attack frames are wired into TestArena (see below). All Q-version (big head, short body). Melee = cloak + polearm. Caster = pointed hood + lantern or staff. Enemy uses a **different silhouette** from melee, plus rust ring / rust outline and no gold, and faces left.

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

Import as Sprite (2D and UI). Y-sort by the foot / ring pivot. These files are idle stills. TestArena shows this still for reserve, setup, standing, and the wait between swings. Walk and attack for all eight playable allies are a separate shipped set (below). Character hit-react loops, character death/down animation frames, skill loops, and projectile atlases are still out of scope. The M0 hit-flash and death-puff pack is a separate shipped set (below).

Official idles use stronger per-role accent colors (linings, sashes, cloak panels, weapon glow) so the roster is not a single navy mass, and they are accessory-dense with layered materials and chunky ornaments. Each official idle should show 5–7 fairly obvious gear pieces, readable at 64 px (not tiny trim; the gold ground ring does not count). Palette and import settings are otherwise unchanged. The M0 dice / TestArena pack is not voided.

## Official walk + attack (all eight playable allies)

In scope and shipped for all eight playable allies (`ashguard`, `oathblade`, `lampsister`, `riftmage`, `misthunter`, `boneapostle`, `chainjudge`, `bloodacolyte`). TestArena plays them. Movement loops the walk. A basic-attack swing plays the attack one-shot, then idle while that unit waits for the next swing. Reserve, setup, and units that have not marched stay on idle. `unit_placeholder_enemy` is wired into TestArena on the same `AllyPoseClock` (see below).

Playback is one constant set for every ally (`AllyPoseClock`): 8 frames per second, 125 ms per frame. The walk cycle is 0.5 s. The attack one-shot is 0.5 s and finishes inside the existing melee 1.2 s and ranged 1.5 s intervals. Those combat numbers are unchanged.

| Clip | Count | Naming |
|------|-------|--------|
| Walk | 4-frame loop while moving | `chr_ally_{id}_walk_01.png` … `chr_ally_{id}_walk_04.png` |
| Attack | 4-frame one-shot on a basic-attack swing | prep, hit, recover, return toward idle — `chr_ally_{id}_attack_01.png` … `chr_ally_{id}_attack_04.png` |
| Idle | still | reserve, setup, standing, and the wait between swings |

All units use this same frame count.

| Setting | Value |
|---------|--------|
| Canvas | 128×128 RGBA |
| Texture Type | Sprite (2D and UI) |
| Pixels Per Unit | 64 |
| Filter Mode | Bilinear |
| Pivot | Feet / gold ring, about (0.50, 0.125) |
| Facing | Right |
| Ground ring | Grave-lamp gold `#C9A227` |

Folder: `Assets/_Project/Art/Characters/`. Review contact sheets: `docs/art/previews/lockpass_ashguard_riftmage_walk_attack.jpg` and `docs/art/previews/lockpass_remaining6_walk_attack.jpg`. Character hit-react loops, character death/down animation frames, skill loops, and projectile atlases stay out of scope. The M0 hit-flash and death-puff pack is a separate shipped set (below). The M0 dice and TestArena environment pack is unchanged and is not voided.

## Enemy placeholder walk + attack (锈蚀步卒 / Rustsoldier)

In scope and shipped for the single M0 enemy (`unit_placeholder_enemy`, 锈蚀步卒 / Rustsoldier). `unit_placeholder_enemy` is wired into TestArena. Battlefield slots `enemy_c1` / `enemy_c2` share this one catalog entry and play independently. Frame counts match the official allies. Playback reuses `AllyPoseClock` unchanged (8 frames per second, 4 frames, walk loop 0.5 s, attack one-shot 0.5 s, move hold 0.08 s).

Setup, or not moving and not attacking, or waiting between swings: idle. Seeking or otherwise moving: loop the walk. The instant a basic-attack swing fires (`kind == "basic"`), the attack one-shot starts at frame 0 and does not loop. Skill casts do not start the attack clip. When the clip ends, idle plays if the unit is still waiting, and walk plays if the unit is still moving. The attack plays to the end even if the unit is also moving. A new basic swing restarts the one-shot from frame 0.

| Clip | Count | Naming |
|------|-------|--------|
| Walk | 4-frame loop while seeking or otherwise moving | `chr_enemy_placeholder_walk_01.png` … `chr_enemy_placeholder_walk_04.png` |
| Attack | 4-frame one-shot on a basic-attack swing: prep, hit, recover, return toward idle | `chr_enemy_placeholder_attack_01.png` … `chr_enemy_placeholder_attack_04.png` |
| Idle | still | setup, standing, and the wait between swings; the production idle file is unchanged |

| Setting | Value |
|---------|--------|
| Canvas | 128×128 RGBA |
| Texture Type | Sprite (2D and UI) |
| Pixels Per Unit | 64 |
| Filter Mode | Bilinear |
| Pivot | (0.50, 0.15) — matches the existing enemy idle, not the allies' 0.125 |
| Facing | Left (toward the allies) |
| Ground ring | Rust red `#8B2E32` (rust outer edge, dark-blue inner). No grave-lamp gold `#C9A227`. |

The existing `chr_enemy_placeholder_idle.png` remains 732×732, pivot (0.50, 0.15), PPU 64, guid `e1d00009e1d00009e1d00009e1d00009`. This wiring does not replace or rescale it or its `.meta`. TestArena portraits are fixed-size UI images (combat 32, setup/token 44) with `preserveAspect`. Walk and attack sprites are swapped inside those boxes. `SetNativeSize` is not used, and the 128 frames are not placed at world size from PPU 64. The 128 frames pad the figure to the same fraction of the canvas as the 732 idle (feet near 93% of canvas height, figure height about 86% of the canvas), so the on-screen character stays roughly the same size when the pose changes. A sharpness difference between the 732 idle and the 128 frames is acceptable and is left alone.

Folder: `Assets/_Project/Art/Characters/`. Review contact sheet: `docs/art/previews/lockpass_enemy_placeholder_walk_attack.jpg`. Character hit-react loops, character death/down animation frames, skill loops, projectile atlases, Boss VFX, and additional enemy types stay out of scope. The M0 hit-flash and death-puff pack is a separate shipped set (below). Do not delete the official-ally playback notes above. The M0 dice and TestArena environment pack is unchanged and is not voided.

## M0 hit flash and death puff

In scope and shipped under `Assets/_Project/Art/VFX/`. One set, two colorways (`_ally` and `_enemy`), shared by every unit of that side. TestArena scenes, prefabs, and code leave these sprites unplayed. Combat numbers stay as they are. Character art stays as it is. The M0 dice and TestArena environment pack is unchanged and is not voided.

| Clip | Count | Naming | Pivot |
|------|-------|--------|-------|
| Hit flash | 4-frame one-shot, no loop | `fx_hit_flash_{ally\|enemy}_01.png` … `_04.png` | (0.50, 0.50) on the unit body |
| Death puff | 4-frame one-shot, no loop | `fx_death_puff_{ally\|enemy}_01.png` … `_04.png` | (0.50, 0.18) at the feet / ground ring |

Playback: recommended 10 fps, 100 ms per frame, 0.4 s total. Do not loop.

| Setting | Value |
|---------|--------|
| Canvas | 128×128 RGBA |
| Texture Type | Sprite (2D and UI) |
| Pixels Per Unit | 64 |
| Filter Mode | Bilinear |
| Compression | None |
| Mipmaps | Off |
| Color space | sRGB, alphaIsTransparency |
| Pivot | Custom (alignment 9); see the table above |

`_ally` is grave-lamp gold `#C9A227` with highlight `#E8C547`. `_enemy` is rust red `#8B2E32` with highlight `#B84A3E` and shadow `#5C1C22`. Death puffs are ash `#6B6560` / `#9A938A` with those accents.

Review contact sheet: `docs/art/previews/lockpass_m0_hit_death_vfx.jpg`.

Still out of scope: skill loops, projectile atlases, Boss VFX, character hit-react loops, character death/down animation frames, Grave-Lamp Village, additional enemy types.
