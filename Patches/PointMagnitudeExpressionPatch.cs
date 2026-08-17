using HarmonyLib;
using Ironhide.Legends.Model.Game.Units.Abilities;

namespace LokrPatch.Patches
{
	/// <summary>Maps ability KV <c>pointMagnitude</c> to <see cref="FunctionPointMagnitude"/>.</summary>
	/// <remarks>
	/// Vanilla <see cref="AbilityParser"/> inherits <c>expressionFunctions["pointMagnitude"] = typeof(FunctionPointMult)</c>
	/// from <c>BaseLogicParser</c>, so a one-arg length call throws and a two-arg call scalar-multiplies.
	/// Postfix runs after the ability-only <c>MergeWith</c> so that dictionary stays intact.
	/// </remarks>
	[HarmonyPatch(typeof(AbilityParser), MethodType.Constructor)]
	internal static class PointMagnitudeExpressionPatch
	{
		private static void Postfix(AbilityParser __instance)
		{
			if (__instance == null || __instance.expressionFunctions == null)
			{
				return;
			}

			__instance.expressionFunctions[PatchRules.PointMagnitudeKvKey] = typeof(FunctionPointMagnitude);
		}
	}
}
