Prior Nia / Cthulhu sheets are legacy and are not for this mode.

# Locked Art Style Guide — Echoes of Fate / 命运残响 (M0)

**Source of truth** for new production on this project. Supersedes prior Abyss Hook / Nia Island and Cthulhu terrain guides for Echoes of Fate work.  
**Genre:** 2D / 2.5D isometric tactics (Test Arena + later route).  
**Engine:** Unity 6 LTS + URP 2D Renderer. **No** 3D models, UVs, 3D scenes, or 3D pipelines.  
**Camera:** Orthogonal isometric.  
**Mood:** grave-lamp gold against cold blue-black; rust, wet stone, parchment, ash.

PRD visual reference board (Grave-Lamp Village / Rift Battlefield / character silhouette) is **color, material, and silhouette law only** — not a production asset. Do not ship the board graphics.

## Specs (defaults)

| Item | Value |
|------|--------|
| Unit sprite height | 64 px (body); canvas 96×96 or 128×128 to leave cloak / weapon overflow |
| Dice face | 64×64 px |
| Ground cell (isometric diamond) | 128×64 px logical; authored at 2× (256×128) then imported |
| Pixels Per Unit | 64 |
| Platform | Desktop landscape |
| Filter | Bilinear (painted), not Point/Nearest unless a future pixel pack is locked |

## Locked palette

| Hex | Role |
|-----|------|
| `#0B1220` | Void / far night / UI base |
| `#152038` | Cold blue-black stone, far fog |
| `#1E2C4A` | Wet slate mid |
| `#3A4A66` | Wet stone highlight |
| `#8A9BB0` | Cold metal edge / low fog |
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
| `#2A2420` | Crevice AO / cloak core |

**Discipline:** Large areas = cold blue-black, wet stone, rust, ash. Gold and fate-crack cyan are point lights and readable accents, never full-screen washes. Parchment is for UI / dice / documents.

## Three laws (from the reference board)

1. **Grave-lamp gold** is the only warm comfort; it is a small lamp in a cold field, never a sunny key light.
2. **Rift battlefield** (TestArena): rust-red blood, cyan fate cracks, ash. Do not use Grave-Lamp Village (cold blue slabs + low fog + single gold lamp) for M0 TestArena.
3. **Character silhouette:** large cloak, pointed hood, lantern and/or sharp polearm. Readable in one second at 64 px. High contrast against the ground.

## Line, light, silhouette

- Form from value masses and a thin faction edge, not thick cartoon outlines.
- Ally edge / ground ring: `#C9A227`. Enemy edge / ground ring: `#8B2E32`. Rings sit under the feet (sorting: Ground + 1 or a Units-Ring layer below the body).
- Soft top-left isometric key; highlights are cold-wet or lamp-gold. No warm sunlight.
- Cloak is the largest readable shape; face/hood second; weapon and lantern as 4 px+ color blocks.

## Layers (Unity Sorting Layer suggestion)

```
BGFar < BGMid < Ground < UnitsRing < Units < FG < UI
```

Y-sort units within `Units` by foot pivot.

## Pivots

| Asset | Pivot |
|-------|--------|
| Characters | Feet, center of the ground ring (0.5, 0.12–0.18) |
| Dice faces | Center (0.5, 0.5) |
| Ground tiles / arena ground | Geometric center of the isometric diamond |
| BG Far / Mid | Center; stretch or slice as a backdrop |
| FG | Center or bottom, overlapping the near edge of the grid |

## Naming

```
ui_dice_{sword|shield|star|feather|coin|skull}_{normal|locked|cracked}.png
env_riftarena_{bg_far|bg_mid|ground|fg}.png
chr_ally_placeholder_melee_{anim}.png
chr_ally_placeholder_caster_{anim}.png
chr_enemy_placeholder_{anim}.png
```

M0 ships `idle` only unless a later ticket adds attack/cast/hit/death.

IDs map to GDD: `face_sword` → `ui_dice_sword_*`; `unit_placeholder_melee` → `chr_ally_placeholder_melee_*`; `unit_placeholder_caster` → `chr_ally_placeholder_caster_*`; `unit_placeholder_enemy` → `chr_enemy_placeholder_*`.

## Ally vs enemy

- **Do not** rely on color-swap alone if silhouettes match.
- Ally: gold ring, thin gold edge, cloak in cold blue-black with gold lamp trim, readable lantern or polished polearm.
- Enemy: rust-red ring, thin rust edge, more ash and jagged cloak, no grave-lamp gold. Same body language (cloak + weapon) is allowed; ring + edge + absence of gold must read in a crowded 3×3 vs 3×3.
- E1 may place two enemies (`enemy_c1`, `enemy_c2`) using the same enemy sprite.

## Forbidden (M0 and later unless a new lock)

- 3D meshes, baked lighting, perspective cameras
- Pixel-art nearest look for this pack
- Cozy pastoral greens, pure white, neon UI
- Grave-Lamp Village as TestArena
- 8 official roster units, Boss, route / shop / rest UI, full VFX sets, 80 icons (out of M0 scope)

## M0 pack contents

See `docs/art/USAGE.md`.
