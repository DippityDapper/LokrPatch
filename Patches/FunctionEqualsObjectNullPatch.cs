using HarmonyLib;
using Ironhide.Legends.Model.Common.Parsers;
using Ironhide.Legends.Model.Game.Units.Abilities;
using Ironhide.Legends.Utils;

namespace LokrPatch.Patches
{
	/// <summary>Evaluates <c>equal()</c> with null-safe <c>object.Equals</c>.</summary>
	/// <remarks>
	/// Vanilla <see cref="FunctionEqualsObjectExpression.GetFloat"/> calls <c>a.Equals(b)</c>, which NullRefs
	/// when the left-hand context key is missing. <c>GetInt</c> / <c>GetObject</c> already call <c>GetFloat</c>,
	/// so one prefix covers them. Both-null is true; null vs non-null is false.
	/// </remarks>
	[HarmonyPatch(typeof(FunctionEqualsObjectExpression), nameof(FunctionEqualsObjectExpression.GetFloat))]
	internal static class FunctionEqualsObjectNullPatch
	{
		private static bool Prefix(FunctionEqualsObjectExpression __instance, IAbilityContext context, ref float __result)
		{
			if (__instance == null)
			{
				return true;
			}

			IExpression[] expressions = Traverse.Create(__instance).Field<IExpression[]>("expressions").Value;
			if (expressions == null || expressions.Length < 2 || expressions[0] == null || expressions[1] == null)
			{
				return true;
			}

			object a = expressions[0].GetObject(context);
			object b = expressions[1].GetObject(context);
			__result = Util.BoolToFloat(PatchRules.NullSafeEquals(a, b));
			return false;
		}
	}
}
