# TestArena Setup reserve — six official allies

**Project:** 命运残响 / Echoes of Fate  
**Scene:** `Assets/Scenes/TestArena.unity`

These six start on the Setup reserve (待命), just left of the ally 3×3. They are placeable copies for the greybox. Each row is the full numeric panel and the same skills as the named placeholder. Role is the existing `melee` / `ranged` tag mid-combat dice already use. Idle sprites stay the official stills already in the project.

They are not part of the default encounter. Leaving them on the reserve does not add them to March, so seed 424242 still replays `tests/Echoes.Simulation.Tests/golden/m0-midcombat-dice.txt` (defeat at 10.800s, gold 0).

| Unit id | zh-Hans | English | Role | Copied from |
|---|---|---|---|---|
| `unit_oathblade` | 烬誓剑士 | Oathblade | melee | `unit_placeholder_melee` |
| `unit_chainjudge` | 锁链判官 | Chainjudge | melee | `unit_placeholder_melee` |
| `unit_lampsister` | 墓灯修女 | Lampsister | ranged | `unit_placeholder_caster` |
| `unit_misthunter` | 雾行猎手 | Misthunter | ranged | `unit_placeholder_caster` |
| `unit_boneapostle` | 骨匣使徒 | Boneapostle | ranged | `unit_placeholder_caster` |
| `unit_bloodacolyte` | 血契侍从 | Bloodacolyte | ranged | `unit_placeholder_caster` |

Names live in `Assets/Resources/M0/M0Localization.asset` as `unit.{id}.name`, where `{id}` is the unit id without the `unit_` prefix (`unit_oathblade` → `unit.oathblade.name`). Idles are wired in `Assets/Resources/M0/M0UnitSprites.asset` to `Assets/_Project/Art/Characters/chr_ally_{id}_idle_01.png`.

Default March stays `unit_placeholder_melee` on `ally_c2` and `unit_placeholder_caster` on `ally_a2`, versus `unit_placeholder_enemy` on `enemy_c1` and `enemy_c2`. Placement rules stay: at most 5 allies on the grid, one unit per cell, an empty formation cannot March, and the grid locks at Countdown.
