# LokrPatch — Layout

```
LokrPatch/
├── LokrPatchPlugin.cs
├── HeroSkillSanitizer.cs
├── PatchRules.cs
└── Patches/
    ├── UnitAddSkillPatches.cs           ← skip duplicate skill ids
    ├── HeroRegenerateFakeUnitPatches.cs ← dedupe hero save skill list
    ├── SuppressedUnityLogPatches.cs     ← filter Debug.* log spam
    ├── LeanTouchUpdatePatch.cs          ← swallow LeanTouch NRE in Update
    ├── EndTurnClassIconPatch.cs
    ├── ApplyModifierMissingPatch.cs     ← skip unknown modifier ids
    ├── MetagameManagerInstanciatingPatch.cs ← clear stuck instantiating flag on ctor throw
    ├── LootAnyOfChancePatch.cs
    ├── DialogFirstFallbackPatch.cs
    ├── FightStartedActiveUnitPatch.cs
    ├── PointMagnitudeExpressionPatch.cs
    ├── ParseAbilityAoeKeysPatch.cs
    ├── RetreatIfWeakAiAliasPatch.cs
    ├── PerAffectedAiParsePatch.cs
    ├── CallFunctionEmptyFilterPatch.cs
    ├── AiEvalEmptyConsiderationsPatch.cs
    ├── FunctionEqualsObjectNullPatch.cs
    ├── EachInListActionsIfEmptyPatch.cs
    ├── ResolveFloatVariableMissingPatch.cs
    ├── PointTargetFilterNullPatch.cs
    ├── StatsApplyModifierMissingStatPatch.cs
    ├── SaveGameSanitizePatch.cs
    ├── HeroRosterLoadPartyPatch.cs
    ├── HeroRoomPartySlotPatch.cs
    ├── InventoryAddItemIdPatch.cs
    ├── HeroUpdateSkillsPatch.cs
    ├── HeroProgressWindowPatch.cs
    ├── MapStartStartingHeroesPatch.cs
    ├── MapHudModifierConfigPatch.cs
    └── ProgressionHelpPopupPatch.cs
```

New vanilla fixes → focused class under `Patches/`.
