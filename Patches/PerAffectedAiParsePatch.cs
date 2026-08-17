using System.Collections.Generic;
using HarmonyLib;
using Ironhide.Legends.Model.Common.Parsers;
using Ironhide.Legends.Model.Game.Units.Abilities;
using KVLib;

namespace LokrPatch.Patches
{
	/// <summary>Drops <c>PerAffectedAI</c> blocks from action lists so parse does not InvalidCast.</summary>
	/// <remarks>
	/// Vanilla registers <c>PerAffectedAI</c> in <c>genericClassConfigs</c>, but the class is
	/// <c>PerAffectedAIEvaluator : AIEvaluator</c>. <c>ParseAction</c> casts to <see cref="AbilityAction"/>,
	/// throws, and <c>ParseAbility</c> returns null. Skipping the child keeps the rest of the ability.
	/// Unregistering the type is a second guard if a child is missed.
	/// </remarks>
	internal static class PerAffectedAiParsePatch
	{
		[HarmonyPatch(typeof(AbilityParser), nameof(AbilityParser.ParseActionList))]
		private static class SkipPerAffectedAiInActionList
		{
			private static bool Prefix(KeyValue kv)
			{
				if (kv == null || !kv.HasChildren)
				{
					return true;
				}

				List<KeyValue> toRemove = new List<KeyValue>();
				foreach (KeyValue child in kv.Children)
				{
					if (child != null && PatchRules.IsSkippedActionKey(child.Key))
					{
						toRemove.Add(child);
					}
				}

				for (int i = 0; i < toRemove.Count; i++)
				{
					kv.RemoveChild(toRemove[i]);
					LokrPatchPlugin.Log.LogWarning(
						"PerAffectedAI is an AIEvaluator, not an AbilityAction; skipped.");
				}

				return true;
			}
		}

		[HarmonyPatch(typeof(AbilityParser), MethodType.Constructor)]
		private static class UnregisterPerAffectedAi
		{
			private static void Postfix(AbilityParser __instance)
			{
				if (__instance == null || __instance.genericClassConfigs == null)
				{
					return;
				}

				__instance.genericClassConfigs.Remove(PatchRules.PerAffectedAiKey);
			}
		}
	}
}
