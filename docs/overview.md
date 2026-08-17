# LokrPatch — Overview

**Base-game bug fixes and defensive error handling** for Legends of Kingdom Rush.
This plugin patches vanilla Ironhide code where missing guards, duplicate-key
throws, or silent assumptions would otherwise crash or soft-lock the game —
especially when mod tooling (Character Lab reload, edited hero definitions, or
dirty save data) exposes edge cases the original game never handled gracefully.

It is intentionally **not** a content loader. It does not register characters,
abilities, portraits, or UI features. Anything that *adds* mod functionality
belongs in `LokrCharacterLoader`, `LokrCharacterLab`, or a dedicated feature
plugin. **`LokrPatch` only makes the stock game more resilient.**

## When to put a fix here

Add a patch to `LokrPatch` when the change:

- Fixes a **vanilla bug** or brittle assumption in `Ironhide.Legends` (duplicate
  dictionary keys, missing null checks, incorrect teardown, etc.).
- Improves **error handling** so a bad state logs a warning and continues
  instead of throwing and blocking load/combat.
- Benefits **all players** (vanilla or modded) and has **no dependency** on
  mod content APIs.

Keep the fix elsewhere when it:

- Loads or transforms mod content (`LokrCharacterLoader`).
- Adds editor or menu UI (`LokrCharacterLab`, `SimpleUI` consumers).
- Unlocks a shipped-but-disabled feature (`LokrEncyclopedia`).

## Current patches (v1.0.11)

**Duplicate hero skills on save load** — `UnitAddSkillPatches` + `HeroRegenerateFakeUnitPatches` + `HeroSkillSanitizer`. See [`classes.md`](classes.md).

**Unity log noise reduction** — `SuppressedUnityLogPatches` filters ability `#` debug actions, UNITDEFINITION migration lines, AssetBundle missing-asset errors, MasterAudio voice exhaustion, and LeanTouch stack traces.

**LeanTouch stability** — `LeanTouchUpdatePatch` prevents per-frame NRE /
`InvalidOperationException` spam when foreign EventSystems are disabled or
RaycastAll returns no hits (overlay tools / embedded fight hole).

**Hero-room / tooltip LateUpdate** — `EventSystemLateUpdatePatch` skips
`UIHeroRoom.LateUpdate` and `TooltipManager.LateUpdate` when
`EventSystem.currentInputModule` is missing (Lab close / SRDebugger
default EventSystem).

**Atlas achievement NREs** — `AchievementsNrePatch` skips
`UIAchievements.Start` subscribe when `AchievementListener` is missing,
and skips null `wasteland_completed_with_*` instances in
`CheckAchievements` (Lab legend ids).

**End Turn class icon** — `EndTurnClassIconPatch` skips the skills-bar
dictionary lookup when the unit is not registered yet (FightStarted vs
AddSkillsBar race, custom unit classes).

**Missing ApplyModifier** — `ApplyModifierMissingPatch` logs and skips
when the modifier id is not in `ability_modifiers`, instead of throwing
and aborting the rest of the ability (and leaving fight input stuck).

**Metagame constructor** — `MetagameManagerInstanciatingPatch` clears the
vanilla `instanciating` flag if construction throws, then rethrows so a
later metagame access can retry instead of staying soft-locked.

**Ability parse/runtime guards (1.0.5)** — missing AOE keys, `pointMagnitude`,
`RetreatIfWeakAI` alias, skip `PerAffectedAI` as an action, empty CallFunction
filters, empty AI brains, null-safe `equal`, inverted `EachInList` fallback,
missing tooltip vars, POINT_TARGET null `targetFilter`, missing stat keys on
`Stats.ApplyModifier`. See [`classes.md`](classes.md).

**Loot / dialog / empty fight (1.0.5)** — `anyOf` chance uses float
`Random.Range`; dialog `FirstOrDefault` then `ExitDialog`; empty initiative
skips `ActiveUnit` so `FightStartedEvent` still runs.

**Save / metagame (1.0.5)** — `Sanitize` never `DiscardRun`; party load keeps
known ids and does not force count == 3; inventory `AddItem` assigns a GUID (confirmed: [`../../docs/issues/resolved/inventory-additem-never-sets-id.md`](../../docs/issues/resolved/inventory-additem-never-sets-id.md));
skip unknown skills, victory uniqueIds, StartingHeroes, and map-HUD modifier
configs. Sanitize and party-load must ship together.

**Progression help (1.0.6)** — `ProgressionHelpPopupPatch` clamps `ShowPage`
against `titles`/`pages` and calls `Finished` when Next would walk off
either list.

**Party slots (1.0.10)** — stowed uniqueIds keep their save index; companions
do not shift into the gold legend frame; core empty slots show Official Pack
`DEFAULT_MINI`; the in-adventure fourth slot stays hidden until a fourth hero
exists; an in-progress adventure brief paints run heroes, not the roster. See
[`../../docs/issues/resolved/party-stow-shifts-remaining-into-wrong-slots.md`](../../docs/issues/resolved/party-stow-shifts-remaining-into-wrong-slots.md).

## In this folder

- [`layout.md`](layout.md) — file structure
- [`classes.md`](classes.md) — entry point, sanitizer, and every patch class
- [`conventions.md`](conventions.md) — scope rules and patch style for this plugin
- [`cross-references.md`](cross-references.md) — base-game types and related mod docs

## Plugin metadata

`LokrPatchPlugin.cs`: `Guid = "com.lokrmodding.patch"`,
`Name = "LoKR Patch"`, `Version = "1.0.11"`.

**No `[BepInDependency]`** — loads with BepInEx alone; references only
`Ironhide.Legends` and Harmony. Safe to ship alongside every other plugin in
this solution; uninstalling mod content plugins does not remove base-game fixes.
