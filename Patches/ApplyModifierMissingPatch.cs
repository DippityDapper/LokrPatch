using System.Collections.Generic;
using HarmonyLib;
using Ironhide.Legends.Model.Common.Parsers;
using Ironhide.Legends.Model.Game.Units.Abilities;

namespace LokrPatch.Patches
{
	/// <summary>Skips <see cref="ApplyModifierAction"/> when the modifier id is not loaded.</summary>
	/// <remarks>
	/// Vanilla throws and aborts the rest of the ability event. A missing imported modifier
	/// then leaves UnitController mid-cast so hex / skill clicks die.
	/// </remarks>
	[HarmonyPatch(typeof(ApplyModifierAction), nameof(ApplyModifierAction.Execute))]
	internal static class ApplyModifierMissingPatch
	{
		private static bool Prefix(ApplyModifierAction __instance, AbilityContext context)
		{
			if (AbilitiesDefinitions.instance == null || AbilitiesDefinitions.instance.ability_modifiers == null
				|| context == null || __instance == null)
			{
				return true;
			}

			IDictionary<string, IExpression> attributes = Traverse.Create(__instance)
				.Field<IDictionary<string, IExpression>>("attributes").Value;
			IExpression nameExpr;
			if (attributes == null || !attributes.TryGetValue("ModifierName", out nameExpr) || nameExpr == null)
			{
				return true;
			}

			object raw = nameExpr.GetObject(context);
			string modifierName = raw != null ? raw.ToString() : null;
			if (string.IsNullOrEmpty(modifierName))
			{
				return true;
			}

			if (AbilitiesDefinitions.instance.ability_modifiers.ContainsKey(modifierName))
			{
				return true;
			}

			LokrPatchPlugin.Log.LogWarning(
				"ApplyModifier: skipped missing modifier '" + modifierName + "'.");
			return false;
		}
	}
}
