# Locked Art Style Guide — Echoes of Fate / 命运残响 (M0 + official 8-ally idles + walk & attack + enemy placeholder walk & attack + M0 hit flash & death puff)

**Style lock: Q 版暗黑 (chibi dark-fantasy).** This is the source of truth for Echoes of Fate production.  
**Supersedes** the withdrawn realistic / painterly / low-poly-realistic M0 draft on PR #10. Those assets are **void** — do not patch or ship them.  
Prior Nia Island / Cthulhu sheets are also legacy and are not for this mode.

**Engine:** Unity 6 LTS + URP 2D Renderer. Pure 2D / 2.5D isometric. **No** 3D models, UVs, 3D scenes, or 3D pipelines.  
**Camera:** Orthogonal isometric.  
**Mood:** dark fantasy with a cute, graphic, toy-like read. Palette stays grave-lamp gold, cold blue-black, rust red, wet stone, parchment, ash — but forms are rounded, simplified, and readable, never photoreal.

The PRD visual reference board is **color and material law only**. It is not a license to render realistic stone, rust, or cloaks.

## What “Q 版暗黑” means

| Rule | Value |
|------|--------|
| Proportions | Big head, short body, short limbs, rounded readable masses |
| Head : body | About **1 : 1.5** to **1 : 2** (head is large; body is compact) |
| Surface | Flat / graphic paint, soft cel or simple planes, toy-like |
| Forbidden finish | Realistic oil painting, photo materials, sculpted relief, low-poly realism, heavy pore/fabric/rock photography |

At **64 px** unit height, a character must be recognizable in one second from silhouette alone.

## Specs (defaults)

| Item | Value |
|------|--------|
| Unit sprite height | 64 px (body + head); canvas 96×96 or 128×128 for cloak / weapon overflow |
| Dice face | 64×64 px (authored at 128×128) |
| Ground cell | Isometric diamond; arena ground shows a clear **3×3 vs 3×3** grid |
| Pixels Per Unit | 64 |
| Filter | Bilinear (graphic paint), not photoreal detail |

## Locked palette

| Hex | Role |
|-----|------|
| `#0B1220` | Void / far night / UI base |
| `#152038` | Cold blue-black, far fog |
| `#1E2C4A` | Wet-slate mid (stylized, not photo stone) |
| `#3A4A66` | Wet-stone highlight |
| `#8A9BB0` | Cold metal / low fog |
| `#C9A227` | Grave-lamp gold — lamps, ally ring, UI accent |
| `#E8C547` | Gold highlight — **spot only** |
| `#5C1C22` | Rust-blood shadow |
| `#8B2E32` | Rust red — blood, enemy ring, danger |
| `#B84A3E` | Rust highlight |
| `#4A7A9A` | Fate-crack mid |
| `#7EB3C9` | Fate-crack light — **spot only** |
| `#C9B89A` | Parchment mid |
| `#E8DCC4` | Parchment highlight |
| `#6B6560` | Ash mid |
| `#9A938A` | Ash highlight |
| `#2A2420` | Crevice / cloak core |

**Discipline:** Large areas = cold blue-black, stylized wet stone, rust, ash. Gold and fate-crack cyan are accents. Parchment is for UI / dice.

## Character silhouette (kept)

Large **cloak**, **pointed hood**, **lantern**, and/or **polearm**. Cute and rounded, but still dark-fantasy — not pastoral, not animal-mascot cute.

## Ally vs enemy

- Ally: grave-lamp **gold** ground ring and a thin gold edge; cold blue-black cloak; gold lantern.
- Enemy: **rust-red** ground ring and rust edge; ash / rust cloak; **no** grave-lamp gold.
- **Do not** ship melee and enemy as a palette-swap of the same pose. At 64 px they must still differ by silhouette (hood shape, weapon shape, stance, or cloak cut).
- E1 may place two enemies (`enemy_c1`, `enemy_c2`) from the same enemy sprite.

## Dice (UI)

Faces, left to right in sheets: **sword, shield, star, feather, coin, skull**.

- Flat, iconic, graphic. Parchment tile, gold rim, clear symbol.
- **Star (`face_star`):** a clear **magic / occult star** (e.g. a simple 4- or 8-point spark or rune-star). **Do not** use a hexagram / Star of David / Seal of Solomon.
- **Coin (`face_coin`):** must read as a **gold coin** in one glance (round coin, rim, maybe a simple stamp). **Do not** use a compass rose or medallion-badge.
- **Locked:** a padlock or clasp drawn **fully inside** that face’s own square. Never in the gutter between tiles (slicing must not drop the lock).
- **Cracked:** graphic rust-red cracks on the face; symbol stays readable.

Naming: `ui_dice_{sword|shield|star|feather|coin|skull}_{normal|locked|cracked}.png`

## TestArena HUD (M0 graybox)

A small component kit that polishes the *existing* TestArena graybox rail and unit plates. Full spec, states, loc keys, and 9-slice notes: [`docs/ui/m0-testarena-hud.md`](../ui/m0-testarena-hud.md). Slices: `Assets/_Project/Art/UI/Hud/`. Manifest: `Assets/_Project/Art/UI/Hud/manifest.json`. Previews: `docs/ui/previews/`.

- Fate energy bar (0–100) with a **tick at 50**. Can-roll / 可掷骰 is energy ≥ 50 **and** this battle has not rolled yet — it is not “full 100”. Skull and bone endcaps overlay the 240×16 track.
- Mid-combat dice result: three 48×48 carved slots with bone-corner brackets + 双生 / 同调 badge (skull chrome, runtime text). Reuse existing `ui_dice_*_normal.png`. M0 has no lock, reroll, or crack in this flow.
- Unit nameplates 88×38, carved frames (gold ally, rust enemy, ash dead), chipped corners. Enemy/dead stamp a rust or ash skull and a rust blood spatter. HP fill is rust `#8B2E32`. Shield fill is grave-lamp gold `#C9A227`. **Default Unity green HP is forbidden.**
- Buttons: primary 208×36 parchment (出征), secondary 100×32 cold fill (掷骰 / 确认), carved dark shell + gold band, skull and bone in the 9-slice *endcaps only*. Labels are runtime loc keys, never baked into the 9-slices.

Chrome mood is heavy and carved (skulls, bones, rust blood, chips and cracks) but stays in the locked Q 版暗黑 palette. Do not copy another game’s layout or switch to pastoral green / warm purple-brown / photoreal bevels. Fate-crack cyan `#7EB3C9` remains **spot only**, never a UI fill. Unique ornaments must never sit in a stretchable 9-slice center.

## TestArena — Rift Battlefield (not Grave-Lamp Village)

- `env_riftarena_ground`: stylized **Q-version** isometric brick / tile grid, **3×3 ally vs 3×3 enemy**, visible mid-line. Graphic rust stains, fate-crack cyan, ash. No photoreal rubble.
- `env_riftarena_bg_mid`: a **backdrop layer** (distant stylized ridges, a rift in the dark, hanging ash). Mid is still a distant backdrop, not a second battlefield.
- `env_riftarena_bg_far`: far void / sky / distant graphic cracks. **Not** another ground texture.
- `env_riftarena_fg` (optional): near-edge stylized stones / ash along the bottom only. Foreground still must not hide footholds.

### TestArena dressing props (additive)

This is an **additive dressing layer** for the TestArena rift battlefield. It sits on the locked ground plate so the **3×3 vs 3×3** mid-line and the footholds stay easy to read. It does not void or replace the locked M0 environment pack.

The original M0 plates remain the locked environment pack. Their PNGs, paths, and importer settings are unchanged, and they still have no `.meta` files:

- `Assets/_Project/Art/Environment/TestArena/env_riftarena_ground.png`
- `Assets/_Project/Art/Environment/TestArena/env_riftarena_bg_far.png`
- `Assets/_Project/Art/Environment/TestArena/env_riftarena_bg_mid.png`
- `Assets/_Project/Art/Environment/TestArena/env_riftarena_fg.png`

Sort order: dressing sits on **Ground**, under **UnitsRing** / **Units**, so characters stay readable on top.

```
BGFar < BGMid < Ground (locked plate, then dressing) < UnitsRing < Units < FG < UI
```

Preview: `docs/art/previews/lockpass_riftarena_dressing.jpg`.

Registered **1280×720** overlays (same canvas as the locked plates). Pivot **center (0.5, 0.5)**:

- `env_riftarena_prop_midline.png` — midline rust, crack, and ash ticks
- `env_riftarena_prop_footholds.png` — 16 foothold marks (the two cells that fall in the rift itself are unmarked)
- `env_riftarena_prop_edge_rubble.png` — edge rubble, ash, and chips
- `env_riftarena_prop_dressing.png` — optional convenience composite of the three overlays above

Loose decals and foothold marks. Pivot **center (0.5, 0.5)**:

- `env_riftarena_prop_rust_01.png` … `env_riftarena_prop_rust_05.png`
- `env_riftarena_prop_crack_01.png` … `env_riftarena_prop_crack_05.png`
- `env_riftarena_prop_ashpuff_01.png` … `env_riftarena_prop_ashpuff_04.png`
- `env_riftarena_prop_foothold_west.png`
- `env_riftarena_prop_foothold_east.png`

Standing props. Pivot **bottom center (0.5, 0.1)**:

- `env_riftarena_prop_rubble_01.png` … `env_riftarena_prop_rubble_03.png`
- `env_riftarena_prop_ash_01.png`, `env_riftarena_prop_ash_02.png`
- `env_riftarena_prop_chip_01.png`, `env_riftarena_prop_chip_02.png`
- `env_riftarena_prop_midline_stones.png`

Folder: `Assets/_Project/Art/Environment/TestArena/`. Import matches character sprites: Texture Type **Sprite (2D and UI)**, Sprite Mode **Single**, Pixels Per Unit **64**, Filter Mode **Bilinear**, `alphaIsTransparency` on, mipmaps off, compression uncompressed. This pack does not wire TestArena scenes or code.

## Layers and pivots

```
BGFar < BGMid < Ground < UnitsRing < Units < FG < UI
```

| Asset | Pivot |
|-------|--------|
| Characters | Feet, center of the ground ring (about 0.5, 0.12–0.18) |
| Dice faces | Center (0.5, 0.5) |
| Ground | Geometric center of the isometric field |
| BG Far / Mid | Center |
| FG | Center or bottom |
| Dressing overlays, foothold marks, rust / crack / ashpuff decals | Center (0.5, 0.5) |
| Standing dressing props (rubble, ash, chip, midline stones) | Bottom center (0.5, 0.1) |

Y-sort units within `Units` by foot pivot.

## Naming

```
ui_dice_{sword|shield|star|feather|coin|skull}_{normal|locked|cracked}.png
env_riftarena_{bg_far|bg_mid|ground|fg}.png
env_riftarena_prop_*.png
chr_ally_placeholder_melee_{anim}.png
chr_ally_placeholder_caster_{anim}.png
chr_enemy_placeholder_{anim}.png
chr_ally_{ashguard|oathblade|lampsister|riftmage|misthunter|boneapostle|chainjudge|bloodacolyte}_idle_01.png
chr_ally_{id}_walk_01.png … chr_ally_{id}_walk_04.png
chr_ally_{id}_attack_01.png … chr_ally_{id}_attack_04.png
chr_enemy_placeholder_walk_01.png … chr_enemy_placeholder_walk_04.png
chr_enemy_placeholder_attack_01.png … chr_enemy_placeholder_attack_04.png
fx_hit_flash_{ally|enemy}_01.png … fx_hit_flash_{ally|enemy}_04.png
fx_death_puff_{ally|enemy}_01.png … fx_death_puff_{ally|enemy}_04.png
ui_btn_{primary|secondary}_{normal|hover|pressed|disabled}.png
ui_bar_track.png
ui_bar_track_ready.png
ui_bar_fill_{hp|shield|fate}.png
ui_nameplate_{ally|enemy|dead}.png
ui_dice_slot_empty.png
ui_badge_combo.png
ui_ornament_skull{,_rust,_ash}.png
ui_ornament_bone.png
ui_ornament_bone_corner.png
ui_ornament_blood.png
ui_bar_endcap_{left,right}{_ready,}.png
```

Dice files stay `ui_dice_{sword|shield|star|feather|coin|skull}_{normal|locked|cracked}.png`. This kit does not rename them and does not ship locked/cracked usage in the M0 mid-combat flow.

M0 ships placeholder `idle` only (melee / caster / enemy). The official 8 playable allies now have production idle sprites (see below). Official walk and attack frames are in scope and shipped for all eight playable allies (`ashguard`, `oathblade`, `lampsister`, `riftmage`, `misthunter`, `boneapostle`, `chainjudge`, `bloodacolyte`). Placeholders remain temporary stand-ins until those official idles are approved and merged. The M0 dice and TestArena environment pack is unchanged and is not voided. TestArena plays these frames for all eight: movement loops the walk, a basic-attack swing plays the attack one-shot, and idle covers reserve, setup, and the wait between swings. Walk and attack frames for the single M0 enemy placeholder (`unit_placeholder_enemy`, 锈蚀步卒 / Rustsoldier) are in scope, shipped, and wired into TestArena (see below). Ally and caster placeholders remain idle-only. Playback is 8 frames per second for every ally (125 ms per frame; walk cycle 0.5 s; attack one-shot 0.5 s), which finishes inside the existing melee 1.2 s and ranged 1.5 s intervals.

IDs: `face_sword` → `ui_dice_sword_*`; `unit_placeholder_melee` / `caster` / `enemy` → the matching `chr_*` files; official allies use the stable IDs in the table below.

## Forbidden

- Shipping or iterating the withdrawn realistic M0 draft
- 3D meshes, perspective cameras, photo-rock / photo-fabric
- Pixel-art nearest look for this pack
- Cozy pastoral greens, neon UI, Grave-Lamp Village as TestArena
- Shipping official 8-ally **idle** sprites is now in scope (see roster below). Official **walk** and **attack** frames are now in scope and shipped for all eight playable allies (see below). Enemy placeholder **walk** and **attack** are now in scope and shipped (see below). The M0 **hit flash** and **death puff** pack is in scope and shipped (see below). Still out of scope: 6 enemies, Boss, Boss VFX, character hit-react loops, character death/down animation frames, skill loops, projectile atlases, Grave-Lamp Village, shop or large-map UI, audio, 80 icons
- Restyling or shipping 墓灯村, shop UI, world-map UI, or a full official HUD as part of this M0 TestArena kit
- Default engine-green HP bars (use rust `#8B2E32`)
- Baking player-visible strings (出征 / 掷骰 / 确认 / 命运能量 / 生命 / 护盾 / 双生 / 同调) into reusable 9-slices
- Using fate-crack cyan `#7EB3C9` as a UI panel or bar fill
- Neon UI, cozy pastoral greens, or a new large TestArena chrome that replaces the current rail / 3×3 board

The M0 dice pack and TestArena environment pack remain unchanged and are not voided.

See `docs/art/USAGE.md`.

## Official 8-ally idle roster

One orthogonal isometric idle per playable ally, style-locked to this Q 版暗黑 guide. Canvas **128×128** RGBA, PPU **64**, filter **bilinear**, pivot at the feet / gold ring (about **0.5, 0.12–0.18**). Allies face right. Every ally has a grave-lamp gold ground ring `#C9A227`. Cloak, pointed hood, lantern, and polearm are kit pieces used or omitted per role — do not palette-swap the same pose. Silhouettes must stay distinct at 64 px.

| ID | Name | Role read | Signature kit |
|----|------|-----------|----------------|
| `ashguard` | 灰烬卫士 | Front tank, guard / counter | Visor-hood, ash-gray plate, large remnant-ember shield, short mace, knee-length cloak |
| `oathblade` | 烬誓剑士 | Front-mid burst, warrior / bleed | Shallower hood, long oath-sword in a ready slash, tattered mid cloak, rust-red sash, no shield |
| `lampsister` | 墓灯修女 | Back healer, holy / prayer | Softer tall hood, large held grave-lamp, prayer beads, long modest cloak, parchment lining, no weapon |
| `riftmage` | 裂隙术士 | Back AoE, arcane / curse | Tallest sharp hood, long staff with cyan fate-sigil crystal, cyan cloak cracks, optional tiny belt lantern |
| `misthunter` | 雾行猎手 | Back mobile, ranger / combo | Hood down, tall longbow, quiver, short fog-gray cape, light leather |
| `boneapostle` | 骨匣使徒 | Mid-back summoner, deathrattle | Bone casket on the back, bone staff, ashen parchment robes, skull motifs (no separate familiar) |
| `chainjudge` | 锁链判官 | Front-mid control, guard / curse | Hooked chain, judicial cloak with rust lining, scale charm, planted stance |
| `bloodacolyte` | 血契侍从 | Mid-back risk, sacrifice / bleed | Smallish hood, ritual knife and blood chalice (not a lantern), rust-stained thinner cloak, pact marks |

Path: `Assets/_Project/Art/Characters/chr_ally_{id}_idle_01.png`

**Color pass:** Official idles use stronger per-role accent colors — large linings, sashes, cloak panels, and weapon glow, not hairline trim — so the eight read as chromatically distinct at 64 px and not as a single navy-blue mass. Accents stay inside the locked palette (grave-lamp gold, rust red, parchment, fate-crack cyan, ash / bone ivory, blood rust). Cloak base colors may differ by role. Do not add colors outside the palette (no pink, lime, or pure-white armor).

**Density pass:** Official idles should be accessory-dense and color-complex: layered materials (cloth, metal, bone, rust, gold trim, lamp glow, ash, blood, cyan cracks) and several chunky, toy-like ornaments per unit (clasps, seals, vials, charms, pouches, scabbards, hanging tags). Prefer a few large readable accessories over many tiny specks. Kits must still read at 64 px and keep distinct silhouettes; do not hide the gold ally ring under props. The M0 dice and TestArena environment pack is unchanged and is not voided.

**Gear quota pass:** Official idles must show **5 to 7 fairly obvious gear pieces** on each unit — chunky, countable objects (weapon, shield, lantern, book, scabbard, quiver, vials, seals, charms, pouches, pauldrons, and similar) that you can point at in the 128 px sprite and still notice at 64 px. Do not count tiny trim, stitching, or a 2 px clasp. Do not count the cloak as a whole unless it is a clearly separate layered garment, and do not count the gold ground ring. Prefer large toyshop ornaments over speck noise; if packing would muddy the 64 px read, enlarge pieces rather than adding more tiny ones. The M0 dice and TestArena environment pack is unchanged and is not voided.

## Official walk + attack (all eight playable allies)

Official walk and attack frames are in scope and shipped for all eight playable allies (`ashguard`, `oathblade`, `lampsister`, `riftmage`, `misthunter`, `boneapostle`, `chainjudge`, `bloodacolyte`) under `Assets/_Project/Art/Characters/`. All units use this same frame count.

| Clip | Frames | Playback |
|------|--------|----------|
| Walk | 4 | Loop while moving: `chr_ally_{id}_walk_01.png` … `chr_ally_{id}_walk_04.png` |
| Attack | 4 | One-shot on a basic-attack swing: prep, hit, recover, return toward idle — `chr_ally_{id}_attack_01.png` … `chr_ally_{id}_attack_04.png` |
| Idle | 1 | Reserve, setup, standing, and the wait between swings |

**TestArena playback** (all eight, one constant set in `AllyPoseClock`): 8 frames per second. Movement (seeking or repositioning) loops the walk. The instant a basic attack swings, the attack plays once and does not loop. When that one-shot ends, idle plays while the unit waits out the rest of its attack interval. Reserve, setup, and any unit that has not marched stay on idle. `unit_placeholder_enemy` is wired into TestArena on the same `AllyPoseClock` (see below). The 0.5 s attack clip finishes inside the existing intervals (melee 1.2 s, ranged 1.5 s). Those combat numbers are unchanged.

Character hit-react loops, character death/down animation frames, skill loops, and projectile atlases stay out of scope. The M0 hit-flash and death-puff pack is a separate shipped set (see below).

Import matches idle: **128×128** RGBA, PPU **64**, filter **Bilinear**, Texture Type **Sprite (2D and UI)**, pivot about **(0.50, 0.125)** at the feet / gold ring, facing **right**, grave-lamp gold ring `#C9A227`. Q 版暗黑 style lock, palette, and the 5–7 gear quota still apply. TestArena plays these frames with the rules above. The M0 dice and TestArena environment pack is unchanged and is not voided.

Review contact sheets: `docs/art/previews/lockpass_ashguard_riftmage_walk_attack.jpg` and `docs/art/previews/lockpass_remaining6_walk_attack.jpg`.

## Enemy placeholder walk + attack (锈蚀步卒 / Rustsoldier)

Walk and attack frames are in scope and shipped for the single M0 enemy (`unit_placeholder_enemy`, display name 锈蚀步卒 / Rustsoldier) under `Assets/_Project/Art/Characters/`. `unit_placeholder_enemy` is wired into TestArena. Battlefield slots `enemy_c1` / `enemy_c2` share this one catalog entry and play independently. Frame counts match the official allies. Playback reuses `AllyPoseClock` unchanged: the clock is not renamed, and its constants stay 8 frames per second, 4 frames, a 0.5 s walk loop, a 0.5 s attack one-shot, and a 0.08 s move hold.

| Clip | Frames | Playback |
|------|--------|----------|
| Walk | 4 | Loop while seeking or otherwise moving: `chr_enemy_placeholder_walk_01.png` … `chr_enemy_placeholder_walk_04.png` |
| Attack | 4 | One-shot on a basic-attack swing (`kind == "basic"`), from frame 0: prep, hit, recover, return toward idle — `chr_enemy_placeholder_attack_01.png` … `chr_enemy_placeholder_attack_04.png`. The clip does not loop. Skill casts do not start it. |
| Idle | 1 | Setup, standing, and the wait between swings. The production idle file is unchanged. |

**TestArena playback** for this enemy is the ally rule set: setup, or not moving and not attacking, or waiting between swings, stays on idle. Seeking or otherwise moving loops the walk. The instant a basic swing fires, the attack one-shot starts at frame 0. When that clip ends, idle plays if the unit is still waiting out its attack interval, and walk plays if the unit is still moving. The attack plays to the end even if the unit is also moving. A new basic swing restarts the one-shot from frame 0.

Import: **128×128** RGBA, PPU **64**, filter **Bilinear**, Texture Type **Sprite (2D and UI)**, pivot **(0.50, 0.15)** to match the existing enemy idle (not the allies' 0.125). Faces **left** (toward the allies). Rust-red ground ring `#8B2E32` (rust outer edge, dark-blue inner). **No** grave-lamp gold `#C9A227`. Q 版暗黑 style lock. Silhouette matches the existing placeholder idle (fox-like pointed hood, rust-edged gray-black tattered cloak, rust scythe); not a palette-swap of ashguard or any ally.

The existing `chr_enemy_placeholder_idle.png` remains **732×732** RGBA, pivot (0.50, 0.15), PPU 64, guid `e1d00009e1d00009e1d00009e1d00009`. This wiring does not replace, rescale, or retouch that idle or its `.meta`. TestArena portraits are fixed-size UI images (combat 32, setup/token 44) with `preserveAspect`. Walk and attack sprites are swapped inside those boxes. `SetNativeSize` is not used, and the 128 frames are not placed at world size from PPU 64. The 128 frames pad the figure to the same fraction of the canvas as the 732 idle (feet near 93% of canvas height, figure height about 86% of the canvas), so the on-screen character stays roughly the same size when the pose changes. A sharpness difference between the 732 idle and the 128 frames is acceptable and is left alone.

Character hit-react loops, character death/down animation frames, skill loops, projectile atlases, Boss VFX, and additional enemy types stay out of scope. The M0 hit-flash and death-puff pack is a separate shipped set (see below).

Review contact sheet: `docs/art/previews/lockpass_enemy_placeholder_walk_attack.jpg`.

## M0 hit flash and death puff

The M0 hit-flash and death-puff pack is in scope and shipped. It is one graphic set in two colorways (`_ally` and `_enemy`), the same shapes for every unit of that side. These are standalone one-shots played on the unit (hit flash) or at the feet / ground ring (death puff). They are separate from character hit-react loops and from character death/down animation frames. TestArena scenes, prefabs, and code leave these sprites unplayed, and combat numbers stay as they are. The M0 dice and TestArena environment pack is unchanged and is not voided.

Path: `Assets/_Project/Art/VFX/`

```
fx_hit_flash_{ally|enemy}_01.png … fx_hit_flash_{ally|enemy}_04.png
fx_death_puff_{ally|enemy}_01.png … fx_death_puff_{ally|enemy}_04.png
```

| Item | Value |
|------|--------|
| Canvas | 128×128 RGBA |
| Texture Type | Sprite (2D and UI) |
| Pixels Per Unit | 64 |
| Filter Mode | Bilinear |
| Compression | None |
| Mipmaps | Off |
| Color space | sRGB, alphaIsTransparency |
| Pivot | Custom (alignment 9) |

| Clip | Frames | Pivot | Playback |
|------|--------|-------|----------|
| Hit flash | 4 | (0.50, 0.50), on the unit body | One-shot, no loop. Recommended 10 fps (100 ms per frame), 0.4 s |
| Death puff | 4 | (0.50, 0.18), at the feet / ground ring | One-shot, no loop. Recommended 10 fps (100 ms per frame), 0.4 s |

**Colorways.** `_ally` is grave-lamp gold `#C9A227` with highlight `#E8C547`. `_enemy` is rust red `#8B2E32` with highlight `#B84A3E` and shadow `#5C1C22`. Death puffs are ash `#6B6560` / `#9A938A` with those accents.

Review contact sheet: `docs/art/previews/lockpass_m0_hit_death_vfx.jpg`.

Still out of scope: skill loops, projectile atlases, Boss VFX, character hit-react loops, character death/down animation frames, Grave-Lamp Village, additional enemy types.
