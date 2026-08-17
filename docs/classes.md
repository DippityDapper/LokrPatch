# LokrPatch — Classes

## `LokrPatchPlugin`

`Guid = "com.lokrmodding.patch"`, no plugin dependencies. `Harmony.PatchAll()` on
Awake — expect about ten patched `Debug.*` methods plus skill / LeanTouch / End Turn / ApplyModifier / metagame / ability / save / progression-help / party-slot / EventSystem LateUpdate / achievement NRE patches at v1.0.11.

## `HeroSkillSanitizer`

Dedupes `hero.heroDefinition.skills` before dummy unit build. Used by
`HeroRegenerateFakeUnitPatches`.

## `UnitAddSkillPatches`

Prefix on both `Unit.AddSkill` overloads — skip duplicate ids with warning.

## `HeroRegenerateFakeUnitPatches`

Prefix on `Hero.RegenerateFakeUnit` — sanitize skills before `new Unit(...)`.

## `SuppressedUnityLogPatches`

Prefix patches on `Debug.Log`, `LogFormat`, `LogWarning`, `LogError`, `LogErrorFormat`,
`LogException` — suppresses known-harmless messages:

- Ability parser `Skipped Action: #…`
- `UNITDEFINITION:` migration/disabling warnings
- `AssetBundleManager - Could't load asset`
- `MasterAudio: … were busy`
- Any message/stack containing `Lean.Touch.LeanTouch`

Direct patches on generic `AssetBundleManager.LoadAsset<T>` were **removed** — they fail
under the game's MonoMod stack (IL compile error at startup).

## `LeanTouchUpdatePatch`

Harmony finalizer on `LeanTouch.Update` — swallows `NullReferenceException` when the
game's input stack is inconsistent (e.g. overlay mod menus blocking EventSystems).

## `EndTurnClassIconPatch`

Prefix on `EndTurnClassIcon.SetUnitClassIcon` — skip when `skillPerUnits` does not
contain the unit (FightStarted before AddSkillsBar, or a custom unit).

## `ApplyModifierMissingPatch`

Prefix on `ApplyModifierAction.Execute` — if `ability_modifiers` has no entry
for `ModifierName`, log a warning and skip. Vanilla threw and aborted the
ability event, which left UnitController mid-cast.

## `MetagameManagerInstanciatingPatch`

Finalizer on `MetagameManager`'s constructor. If construction throws, vanilla
leaves `instanciating` true, so later `instanceNoLoad` throws "already being
instanciated" and Stage cannot find a host quest. This patch clears the flag
and rethrows so a later access can retry.

## `LootAnyOfChancePatch`

Prefix-replaces `LootItemGeneratorAnyOf.AddItems` so `chance` uses
`Random.Range(0f, 1f)` instead of int `Range(0, 1)`.

## `DialogFirstFallbackPatch`

Prefix-replaces `Dialog.Start` / `HandleReply` / `HandleContinue`:
`FirstOrDefault` then `ExitDialog` when no child passes.

## `FightStartedActiveUnitPatch`

Null-checks `ActiveUnit` in `FightStartedHandler` and `Stage.StartFight` so
an empty initiative list does not abort later `FightStartedEvent` listeners.

## `PointMagnitudeExpressionPatch`

AbilityParser ctor postfix maps KV `pointMagnitude` to `FunctionPointMagnitude`.

## `ParseAbilityAoeKeysPatch`

Prefix on `ParseAbility`: missing `AbilityAOECenterOnCaster` /
`AbilityAOEAffectsCaster` default to `0` when Behavior includes `AOE`.

## `RetreatIfWeakAiAliasPatch`

AbilityParser ctor postfix registers `RetreatIfWeakAI` as an alias; vanilla
`RetreatIfWeekAI` stays.

## `PerAffectedAiParsePatch`

Skips `PerAffectedAI` in `ParseActionList` (it is an `AIEvaluator`, not an
`AbilityAction`) and unregisters it from the action map.

## `CallFunctionEmptyFilterPatch`

Prefixes on six CallFunction `Execute` methods: empty filter skip-and-log.

## `AiEvalEmptyConsiderationsPatch`

`AIDecisionScoreEvaluator.Eval` prefix returns 0 when considerations are empty.

## `FunctionEqualsObjectNullPatch`

`FunctionEqualsObjectExpression.GetFloat` uses null-safe `object.Equals`.

## `EachInListActionsIfEmptyPatch`

`EachInListAction.Execute` runs `ActionsIfEmpty` only when `list.Count == 0`.

## `ResolveFloatVariableMissingPatch`

Missing tooltip variable keys return 0 instead of 999.

## `PointTargetFilterNullPatch`

Null-checks `targetFilter` on the four POINT_TARGET NRE methods.

## `StatsApplyModifierMissingStatPatch`

`Stats.ApplyModifier` skip-and-logs missing *stat keys* (not modifier ids).

## `SaveGameSanitizePatch`

Prefix-replaces `Sanitize`: never `DiscardRun`; logs unknown ids and leaves
the run. Load/Save stow unknown heroes/items/quests append-only.

## `HeroRosterLoadPartyPatch`

`HeroRosterManager.Load` keeps known party uniqueIds in legend/companion
slot order (holes for missing members), stows the rest, and does not reset
when `Count != 3`. Save splices stowed ids into those holes instead of
appending. Live `Party` stays compacted. Ships with `SaveGameSanitizePatch`.

## `HeroRoomPartySlotPatch`

Paints `UIHeroRoom` / `UIAdventureBrief` portraits from slot-aligned holes,
keeps empty slots visible (chrome only), skips `CheckRankedUpState` /
detail-panel open on empty ids, stops `OnDone` from writing nulls into
the live party, paints an in-progress adventure brief from run heroes
(not the roster), and realigns title-screen `UISlot` MiniPortraits so a
companion is not drawn in the gold frame.

## `InventoryAddItemIdPatch`

`AddItem` postfix assigns a GUID when `ItemInstance.id` is null.

## `HeroUpdateSkillsPatch`

`UpdateSkills` skips a missing ability after the vanilla log; clamps unused
level indexers.

## `HeroProgressWindowPatch`

`ShowHeroProgress` drops unknown uniqueIds; skips null unlock-skill anim.

## `MapStartStartingHeroesPatch`

Empty-party `StartingHeroes` skip-and-log unknown uniqueIds; does not rewrite
the field to the vanilla trio.

## `MapHudModifierConfigPatch`

Map hero bar, initiative, unit-detail, and reward UI skip a null
`HeroModifiersAsset` config.

## `PatchRules`

Unity-free skip/clamp/map helpers the prefixes call and xUnit tests.
Loot chance vs roll, dialog empty child, AOE token, PerAffectedAI skip,
empty filter/brain, null-safe equals, ActionsIfEmpty, missing tooltip
var, null targetFilter, missing stat, sanitize/party keep, item GUID,
unknown skill/uniqueId/StartingHeroes, null modifier config, empty
ActiveUnit, progression-help index clamp, party slot holes / stow splice.

## `ProgressionHelpPopupPatch`

Prefixes `UIProgressionHelpPopup.ShowPage` / `Next` using `PatchRules`
so a shorter `titles` list does not throw.

## `EventSystemLateUpdatePatch`

Prefix on `UIHeroRoom.LateUpdate` and `TooltipManager.LateUpdate` — skip
when `EventSystem.currentInputModule` or `.input` is null.

## `AchievementsNrePatch`

Prefix on `UIAchievements.Start` when `AchievementListener` is missing.
Replaces `FullMetagameSessionData.CheckAchievements` so null
`wasteland_completed_with_*` instances are skipped.
