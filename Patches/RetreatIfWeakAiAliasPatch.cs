using HarmonyLib;
using Ironhide.Legends.Controller.Game.AI;
using Ironhide.Legends.Model.Game.Units.Abilities;

namespace LokrPatch.Patches
{
	/// <summary>Registers KV type <c>RetreatIfWeakAI</c> as an alias of vanilla <c>RetreatIfWeekAI</c>.</summary>
	/// <remarks>
	/// Vanilla only maps the typo key <c>RetreatIfWeekAI</c> to <see cref="RetreatIfWeakAI"/>. A correctly spelled
	/// type throws <c>Could not parse action</c> and drops the parent ability. The typo key stays so shipped
	/// <c>retreat_if_weak_troll_ai.txt</c> still loads.
	/// </remarks>
	[HarmonyPatch(typeof(AbilityParser), MethodType.Constructor)]
	internal static class RetreatIfWeakAiAliasPatch
	{
		private static void Postfix(AbilityParser __instance)
		{
			if (__instance == null || __instance.genericClassConfigs == null)
			{
				return;
			}

			__instance.genericClassConfigs[PatchRules.RetreatIfWeakCorrectKey] = RetreatIfWeakAI.GetParseConfig();
		}
	}
}
