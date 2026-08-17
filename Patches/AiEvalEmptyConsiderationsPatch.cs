using HarmonyLib;
using Ironhide.Legends.Controller.Game.AI;

namespace LokrPatch.Patches
{
	/// <summary>Returns 0 from <see cref="AIDecisionScoreEvaluator.Eval"/> when considerations are empty.</summary>
	/// <remarks>
	/// Vanilla does <c>1f - 1f / considerations.Count</c> with no empty check, so a parsed
	/// <c>Considerations { }</c> block throws <c>DivideByZeroException</c> on the first AI think.
	/// Returning 0 (not <c>weight</c>) keeps an empty brain from dominating action pick.
	/// </remarks>
	[HarmonyPatch(typeof(AIDecisionScoreEvaluator), nameof(AIDecisionScoreEvaluator.Eval))]
	internal static class AiEvalEmptyConsiderationsPatch
	{
		private static bool Prefix(AIDecisionScoreEvaluator __instance, ref float __result)
		{
			if (__instance == null || __instance.considerations == null
				|| PatchRules.EmptyConsiderationsReturnsZero(__instance.considerations.Count))
			{
				LokrPatchPlugin.Log.LogWarning(
					"AIDecisionScoreEvaluator.Eval: empty considerations; returning 0.");
				__result = 0f;
				return false;
			}

			return true;
		}
	}
}
