# LokrPatch — Conventions

## Scope

- **In scope:** defensive Harmony patches on `Ironhide.Legends` — fix crashes,
  infinite loads, duplicate-key throws, incorrect teardown, noisy errors that
  indicate a fixable vanilla mistake.
- **Out of scope:** mod content, editor features, asset loading, config facades.
  Those live in their owning plugins.

When unsure, ask: *"Would this help a player with zero mods installed?"* If yes,
it likely belongs here. If it only matters when mod content is present, put the
guard in the mod plugin **and** consider whether vanilla deserves the same fix
in `LokrPatch` anyway (duplicate skills affect vanilla saves too).

## Patch style

- One concern per file under `Patches/` — e.g. all `Unit.AddSkill` overloads
  share one static class with nested patch types.
- Prefer **skip-and-log** over **throw** for recoverable collisions (keep first
  registration, warn in `LokrPatchPlugin.Log`).
- Prefer **postfix normalization** only at stable boundaries (e.g. immediately
  before dummy unit construction), not after every vanilla method that might
  run many times during a session — avoid silently rewriting save data.
- Document the vanilla bug in XML `<remarks>` on the patch class and in
  [`classes.md`](classes.md) when adding a new fix.

## Plugin bootstrap

Same shape as other plugins (`[BepInPlugin]` + `Harmony.PatchAll()` in
`Awake`), but **no `[BepInDependency]`** unless a future patch genuinely needs
another LoKR plugin (unlikely — keep this assembly dependency-free).

## Logging

Use `LokrPatchPlugin.Log.LogWarning` for recovered errors the player should
know about; `LogInfo` only for one-time startup. Avoid per-frame spam.
