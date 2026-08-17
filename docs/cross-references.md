# LokrPatch — Cross-references

## Base-game types (decompiled)

| Type | Relevant behavior |
|------|-------------------|
| `Ironhide.Legends.Model.Game.Units.Unit` | Constructor adds `definition.defaultSkill`, then every id in `heroDefinition.skills` (heroes) or `definition.skills` (units). `AddSkill` uses `Dictionary.Add` — duplicate id throws. |
| `Ironhide.Legends.Model.Metagame.Heroes.Hero` | Constructor calls `UpdateSkills()` then `RegenerateFakeUnit()`. |
| `Ironhide.Legends.Model.Metagame.Heroes.Hero.UpdateSkills` | Merges `unitDefinition.skills`, then replays progression picks; latter path appends without deduping. |
| `Ironhide.Legends.Model.Metagame.Heroes.HeroManager.Load` | Reconstructs heroes from save → triggers the crash path when duplicate skills exist. |

Source: `lokr-modding/ih-original/...` (see [`../../docs/reference/README.md`](../../docs/reference/README.md)).

## Related mod documentation

| Doc | Relationship |
|-----|----------------|
| [`../../docs/roadmaps/started/live-reload.md`](../../docs/roadmaps/started/live-reload.md) | Character Lab "Reload in Game" exposed duplicate skill ids on saves; `MetagameHeroReloader` no longer calls `UpdateSkills`; **`LokrPatch`** handles remaining dirty data. |
| [`../../LokrCharacterLoader/docs/patches.md`](../../LokrCharacterLoader/docs/patches.md) | Content/reload patches — **not** base-game bug fixes. Do not move defensive guards there unless they are content-pipeline specific. |
| [`../../docs/unit-load-path.md`](../../docs/unit-load-path.md) | How `UnitDefinition.defaultSkill` and hero skills reach runtime units. |

## Candidate future patches (not implemented)

Documented here as reminders; implement in `LokrPatch` when prioritized:

- **`UnloadAsset can only be used on assets`** — spam during ability reload
  when synthetic `TextAsset` wrappers are torn down (see ability loader patches
  in `LokrCharacterLoader`). Fix belongs at the unload call site or a guard
  patch, not in content registration.
