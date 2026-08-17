using System.Collections.Generic;
using HarmonyLib;
using Ironhide.Legends.Model.Game.Units;
using Ironhide.Legends.Model.Game.Units.Abilities;
using Ironhide.Legends.Model.Game.Units.ActivityInterfaces;
using UnityEngine;

namespace LokrPatch.Patches
{
	/// <summary>Null-checks <c>BaseActivityInterface.targetFilter</c> on the four methods that NRE for POINT_TARGET.</summary>
	/// <remarks>
	/// <c>AbilityBehavior.POINT_TARGET</c> skips <c>CreateTargetFilter</c>, so <c>targetFilter</c> stays null.
	/// <c>IsPossibleTarget(Unit)</c> already guards; melee select and <c>GetCloseToUnitAI</c> do not.
	/// A dummy filter is not created — that would make hex skills start targeting units.
	/// </remarks>
	internal static class PointTargetFilterNullPatch
	{
		[HarmonyPatch(typeof(BaseActivityInterface), nameof(BaseActivityInterface.IsPossibleTargetIgnoringPosition))]
		private static class IsPossibleTargetIgnoringPositionNullFilter
		{
			private static bool Prefix(BaseActivityInterface __instance, ref bool __result)
			{
				if (!PatchRules.ShouldSkipNullTargetFilter(GetTargetFilter(__instance) == null))
				{
					return true;
				}

				__result = false;
				return false;
			}
		}

		[HarmonyPatch(typeof(BaseActivityInterface), nameof(BaseActivityInterface.GetValidTargets))]
		private static class GetValidTargetsNullFilter
		{
			private static bool Prefix(BaseActivityInterface __instance, ref List<Unit> __result)
			{
				if (!PatchRules.ShouldSkipNullTargetFilter(GetTargetFilter(__instance) == null))
				{
					return true;
				}

				__result = new List<Unit>();
				return false;
			}
		}

		[HarmonyPatch(typeof(BaseActivityInterface), nameof(BaseActivityInterface.GetValidTargetsIgnoringRange))]
		private static class GetValidTargetsIgnoringRangeNullFilter
		{
			private static bool Prefix(BaseActivityInterface __instance, ref List<Unit> __result)
			{
				if (!PatchRules.ShouldSkipNullTargetFilter(GetTargetFilter(__instance) == null))
				{
					return true;
				}

				__result = new List<Unit>();
				return false;
			}
		}

		[HarmonyPatch(typeof(BaseActivityInterface), nameof(BaseActivityInterface.SetCenter), typeof(Vector2))]
		private static class SetCenterNullFilter
		{
			private static bool Prefix(BaseActivityInterface __instance)
			{
				return !PatchRules.ShouldSkipNullTargetFilter(GetTargetFilter(__instance) == null);
			}
		}

		private static UnitFilter GetTargetFilter(BaseActivityInterface instance)
		{
			if (instance == null)
			{
				return null;
			}

			return Traverse.Create(instance).Field<UnitFilter>("targetFilter").Value;
		}
	}
}
