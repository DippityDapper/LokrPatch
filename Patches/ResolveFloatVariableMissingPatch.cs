using HarmonyLib;
using Ironhide.Legends.Model.Game.Units;
using Ironhide.Legends.Model.Game.Units.Abilities;

namespace LokrPatch.Patches
{
	/// <summary>Resolves missing tooltip variables to 0 instead of 999.</summary>
	/// <remarks>
	/// Vanilla <see cref="AbilityInstance.ResolveFloatVariable"/> and <see cref="ModifierInstance.ResolveFloatVariable"/>
	/// log an error and return <c>999f</c> when the named key is absent. Prefix (not postfix) so a real configured
	/// value of 999 is left alone. <c>SkillActivityPointer.ResolveVariable</c> forwards here, so encyclopedia /
	/// sandbox tooltips pick up the 0.
	/// </remarks>
	internal static class ResolveFloatVariableMissingPatch
	{
		[HarmonyPatch(typeof(AbilityInstance), nameof(AbilityInstance.ResolveFloatVariable))]
		private static class AbilityInstanceMissingVariable
		{
			private static bool Prefix(AbilityInstance __instance, string variableName, ref float __result)
			{
				if (__instance == null)
				{
					return true;
				}

				AbilityContext instanceContext = Traverse.Create(__instance)
					.Field<AbilityContext>("instanceContext").Value;
				if (instanceContext == null)
				{
					return true;
				}

				object value = instanceContext.GetObject(variableName);
				if (!PatchRules.MissingVariableReturnsZero(value))
				{
					return true;
				}

				Ability ability = Traverse.Create(__instance).Field<Ability>("ability").Value;
				Unit owner = Traverse.Create(__instance).Field<Unit>("owner").Value;
				string skillId = ability != null ? ability.abilityId : "?";
				string unitId = owner != null && owner.unitDefinition != null ? owner.unitDefinition.id : "?";
				LokrPatchPlugin.Log.LogWarning(
					"SkillTooltip: missing variable '" + variableName + "' in skill $" + skillId
					+ " on unit " + unitId + "; returning 0.");
				__result = PatchRules.MissingVariableFallback;
				return false;
			}
		}

		[HarmonyPatch(typeof(ModifierInstance), nameof(ModifierInstance.ResolveFloatVariable))]
		private static class ModifierInstanceMissingVariable
		{
			private static bool Prefix(ModifierInstance __instance, string variableName, ref float __result)
			{
				if (__instance == null || __instance.sourceTargetContext == null)
				{
					return true;
				}

				object value = __instance.sourceTargetContext.GetObject(variableName);
				if (!PatchRules.MissingVariableReturnsZero(value))
				{
					return true;
				}

				Modifier modifier = Traverse.Create(__instance).Field<Modifier>("modifier").Value;
				string modifierId = modifier != null ? modifier.modifierId : "?";
				LokrPatchPlugin.Log.LogWarning(
					"SkillTooltip: missing variable '" + variableName + "' in modifier $" + modifierId
					+ "; returning 0.");
				__result = PatchRules.MissingVariableFallback;
				return false;
			}
		}
	}
}
