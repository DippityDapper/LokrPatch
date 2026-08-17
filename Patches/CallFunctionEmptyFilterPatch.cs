using System.Collections.Generic;
using HarmonyLib;
using Ironhide.Legends.Content.Abilities;
using Ironhide.Legends.Model.Common.Parsers;
using Ironhide.Legends.Model.Game;
using Ironhide.Legends.Model.Game.Units;
using Ironhide.Legends.Model.Game.Units.Abilities;

namespace LokrPatch.Patches
{
	/// <summary>Skips six CallFunction helpers when their unit filter matches nobody.</summary>
	/// <remarks>
	/// Vanilla indexes <c>list[0]</c>, <c>Random</c>-then-NRE, or <c>ToList()[0]</c> / <c>heroes.Min</c> on an empty
	/// match. Lab sandbox fights often have no heroes or tentacle markers. Skip-and-log leaves <c>Actions</c>
	/// unrun instead of throwing. <c>SBFAspectMagicTeleportTarget</c> is not patched (empty filters do not throw).
	/// </remarks>
	internal static class CallFunctionEmptyFilterPatch
	{
		[HarmonyPatch(typeof(ClosestTargetPreferNoFlip), nameof(ClosestTargetPreferNoFlip.Execute))]
		private static class ClosestTargetPreferNoFlipEmptyFilter
		{
			private static bool Prefix(ClosestTargetPreferNoFlip __instance, AbilityContext context)
			{
				return !ShouldSkipEmptyFilter(__instance, context, "UnitFilter", "ClosestTargetPreferNoFlip");
			}
		}

		[HarmonyPatch(typeof(KrumSelectTargets), nameof(KrumSelectTargets.Execute))]
		private static class KrumSelectTargetsEmptyFilter
		{
			private static bool Prefix(KrumSelectTargets __instance, AbilityContext context)
			{
				return !ShouldSkipEmptyFilter(__instance, context, "UnitFilter", "KrumSelectTargets");
			}
		}

		[HarmonyPatch(typeof(SBFAspectPhysicalTeleportTarget), nameof(SBFAspectPhysicalTeleportTarget.Execute))]
		private static class SBFAspectPhysicalTeleportTargetEmptyFilter
		{
			private static bool Prefix(SBFAspectPhysicalTeleportTarget __instance, AbilityContext context)
			{
				return !ShouldSkipEmptyFilter(__instance, context, "HeroFilter", "SBFAspectPhysicalTeleportTarget");
			}
		}

		[HarmonyPatch(typeof(SBFAspectSummonerTeleportTarget), nameof(SBFAspectSummonerTeleportTarget.Execute))]
		private static class SBFAspectSummonerTeleportTargetEmptyFilter
		{
			private static bool Prefix(SBFAspectSummonerTeleportTarget __instance, AbilityContext context)
			{
				return !ShouldSkipEmptyFilter(__instance, context, "HeroFilter", "SBFAspectSummonerTeleportTarget");
			}
		}

		[HarmonyPatch(typeof(WBFIrizaTeleportTarget), nameof(WBFIrizaTeleportTarget.Execute))]
		private static class WBFIrizaTeleportTargetEmptyFilter
		{
			private static bool Prefix(WBFIrizaTeleportTarget __instance, AbilityContext context)
			{
				return !ShouldSkipEmptyFilter(__instance, context, "HeroFilter", "WBFIrizaTeleportTarget");
			}
		}

		[HarmonyPatch(typeof(WBFOverseerSelectTentacleSpawn), nameof(WBFOverseerSelectTentacleSpawn.Execute))]
		private static class WBFOverseerSelectTentacleSpawnEmptyFilter
		{
			private static bool Prefix(WBFOverseerSelectTentacleSpawn __instance, AbilityContext context)
			{
				if (ShouldSkipEmptyFilter(__instance, context, "TargetMarkerFilter", "WBFOverseerSelectTentacleSpawn"))
				{
					return false;
				}

				return !ShouldSkipEmptyFilter(__instance, context, "HeroFilter", "WBFOverseerSelectTentacleSpawn");
			}
		}

		private static bool ShouldSkipEmptyFilter(object instance, AbilityContext context, string filterKey, string typeName)
		{
			List<Unit> units;
			if (!TryFilterUnits(instance, context, filterKey, out units))
			{
				return false;
			}

			if (units.Count > 0)
			{
				return false;
			}

			LokrPatchPlugin.Log.LogWarning(typeName + ": skipped empty " + filterKey + ".");
			return PatchRules.ShouldSkipEmptyUnitFilter(units.Count);
		}

		private static bool TryFilterUnits(object instance, AbilityContext context, string filterKey, out List<Unit> units)
		{
			units = null;
			if (instance == null || context == null || Stage.instance == null || Stage.instance.units == null)
			{
				return false;
			}

			IDictionary<string, IExpression> attributes = Traverse.Create(instance)
				.Field<IDictionary<string, IExpression>>("attributes").Value;
			IExpression expr;
			if (attributes == null || !attributes.TryGetValue(filterKey, out expr) || expr == null)
			{
				return false;
			}

			UnitTargetHelper helper = expr.GetObject(context) as UnitTargetHelper;
			if (helper == null)
			{
				return false;
			}

			UnitFilter filter = helper.Execute(context);
			units = filter != null ? filter.Filter(Stage.instance.units) : new List<Unit>();
			if (units == null)
			{
				units = new List<Unit>();
			}

			return true;
		}
	}
}
