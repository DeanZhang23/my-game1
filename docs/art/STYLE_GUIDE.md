# Locked Art Style Guide — Cthulhu Soft Hand-Painted (v2)

**Legacy placeholders:** Pixel MVP sheets in `Assets/Art/Characters`, `Assets/Art/UI`, `Assets/Art/Fish`, and `Assets/Art/Backgrounds` stay in the repo as legacy placeholders pending reskin. They are not the locked production style.

**Source of truth:** Game Art Director lock (Dean). Supersedes prior pixel Abyss Hook MVP sheet style for new terrain/character production.  
**Genre:** 2D side-view platform / modular terrain  
**Medium:** Soft hand-painted digital illustration (blended brush). **Not** pixel art. **Not** hard-edge vector.  
**Mood triad:** 异质 · 压抑 · 侵蚀 (heterogeneity · oppression · erosion)

## Specs (defaults)
| Item | Value |
|------|--------|
| Tile | 64×64 px |
| Character height | ~128 px |
| Platform | Desktop landscape |
| Engine | TBD (engine-agnostic) |

## North-star reference (structure only)
`reference-terrain-base` locks **construction & precision**: rounded river-stone modules, moss overhangs, waterfall/pond joins, vegetation + small shrine scale/bite.  
**Do not** copy its warm cozy green emotion or palette.

## Three pillars
1. **Heterogeneity** — forms/colors slightly “wrong” under a natural surface: rot-moss sick green, abyss water, cold non-physical glows.
2. **Oppression** — low value, narrow temperature; scarce highlights for readability & danger only, never comfort.
3. **Erosion** — edges chewed by mold, bone-ash, cracks, alien matter; no pristine sunny grass/stone.

## Locked palette
| Hex | Role |
|-----|------|
| `#0B0E14` | BG / void |
| `#141820` | UI base / far fog |
| `#1A1518` | Wet crevice AO |
| `#2A3036` | Rock shadow |
| `#3A4248` | Rock base |
| `#4E565C` | Rock mid |
| `#6B757C` | Rock highlight (cold wet) |
| `#2F3A24` | Rot moss shadow |
| `#3D4A2E` | Rot moss base |
| `#5A6B3A` | Rot moss highlight |
| `#4A6B5C` | Sick teal bleed / erosion transition |
| `#0A2838` | Abyss water deep |
| `#1A3A4A` | Abyss water base |
| `#2E5A68` | Water specular |
| `#A8B8B0` | Scum foam / phosphor edge |
| `#8A8074` | Ash bone mid |
| `#C4B8A8` | Bone / lime highlight |
| `#4A3028` | Ruined wood / shrine wood |
| `#6B1C24` | Blood / danger dark |
| `#8B2A32` | Blood / danger bright |
| `#2A1830` | Corruption umbra |
| `#4A2A5A` | Corruption mid |
| `#6EC8B8` | Eldritch glow (primary) — **spot only** |
| `#9B7AE8` | Eldritch glow (secondary) — **spot only** |
| `#C9BFAF` | UI bone text |

**Discipline:** Large areas = rock / rot moss / abyss water only. `#6EC8B8` / `#9B7AE8` = interactive or alien point lights. Blood = damage/ritual only. No healthy saturated grass green.

## Line & silhouette
- No pure-black hard outlines; form via value masses + soft strokes.
- Thin cold edge only if needed (`#6B757C` or weak `#6EC8B8` halo).
- Stone joins: `#1A1518` crevice AO; no bright seam lines.
- Moss slightly overhangs stone; soft eroded edges — no vector 90° corners.

## Light
- Soft top-left key, depressed; highlights cold-wet; **no warm sun gold**.
- Ambient blue-black / purple-black; caves/deep water swallow detail into `#0B0E14`–`#1A1518`.
- Soft AO; short dirty contact shadows; no hard cartoon drop shadows.
- Eldritch lights: small radius, fast falloff, may lightly stain neighbors; never full-screen.

## Detail budget
- Terrain tile: 1 main silhouette + 2–4 stone clumps; moss = 1 texture layer + sparse mold dots; no full story scenes per tile.
- Props/shrines: silhouette first; rust/erosion ≤20%; runes = eldritch spots.
- Vegetation: 3–5 leaves per cluster, wilted sick green; no tropical tourist lush.
- Water: big masses + scum foam + few hanging streaks; foam `#A8B8B0`, never pure white.
- Characters ~128px: silhouette > face planes > tiny accents; 2–3 readable value groups when scaled down.
- VFX: few dirty particles; no flash-white.
- UI: `#141820` + `#C9BFAF` text; accent `#6EC8B8`; danger blood; no neon UI.

## Readability
- 64×64 walkable tops & collision outlines clear at game pixel scale.
- ~128px characters: standing silhouette readable in ~1s; held items / headgear ≥4px effective color blocks.
- Separate character vs footing, danger vs safe ground via value or blood/eldritch cues.

## Forbidden
- Cozy bright pastoral; healthy grass / sunny cyan sky
- Pixel art, hard vector outlines, cyber-neon full-screen UI
- Fresh vermilion lacquer / gilt glorious shrines (use ruined wood, peeling, eroded)
- Pure white foam, warm yellow sun spots, cute cartoon mushrooms (mushrooms OK if toxic grey-red)
- Tracing specific characters/levels/logos from refs — laws only
- Eldritch glow or debris covering footholds / platform tops

## Public refs (laws only, no tracing)
SILT · INSIDE lighting · Salt and Sanctuary · Source of Madness · Mandragora · Hollow Knight production design

## Naming
```
tile_rock_<variant>.png
tile_moss_<variant>.png
tile_water_<variant>.png
prop_shrine_<variant>.png
sheet_terrain_base.png
char_<name>_<anim>_<frame>.png
ui_<element>_<state>.png
```
