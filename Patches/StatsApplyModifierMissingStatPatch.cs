using System.Collections.Generic;
using HarmonyLib;
using Ironhide.Legends.Model.Game.Units;

namespace LokrPatch.Patches
{
	/// <summary>Applies a <see cref="StatModifier"/> while skipping additive/mult keys the live stats dictionary lacks.</summary>
	/// <remarks>
	/// Vanilla throws <c>could not find stat with key …</c> and aborts the rest of the modifier (and the ability event).
	/// Remaining valid keys on the same modifier still apply. Unknown <em>modifier ids</em> are a different patch
	/// (<c>ApplyModifierMissingPatch</c> on <c>ApplyModifierAction.Execute</c>).
	/// </remarks>
	[HarmonyPatch(typeof(Stats), nameof(Stats.ApplyModifier))]
	internal static class StatsApplyModifierMissingStatPatch
	{
		private static bool Prefix(Stats __instance, StatModifier statModifier)
		{
			if (__instance == null || statModifier == null || __instance.stats == null)
			{
				return true;
			}

			if (statModifier.additiveModifiers != null)
			{
				for (int i = 0; i < statModifier.additiveModifiers.Count; i++)
				{
					KeyValuePair<string, float> pair = statModifier.additiveModifiers[i];
					Stat stat;
					if (PatchRules.ShouldSkipMissingStat(__instance.stats.TryGetValue(pair.Key, out stat) && stat != null))
					{
						LokrPatchPlugin.Log.LogWarning(
							"Stats.ApplyModifier: skipped missing stat '" + pair.Key + "'");
						continue;
					}

					stat.AddAdditiveModifier(statModifier.id, pair.Value);
				}
			}

			if (statModifier.multiplicativeModifiers != null)
			{
				for (int j = 0; j < statModifier.multiplicativeModifiers.Count; j++)
				{
					KeyValuePair<string, float> pair = statModifier.multiplicativeModifiers[j];
					Stat stat;
					if (PatchRules.ShouldSkipMissingStat(__instance.stats.TryGetValue(pair.Key, out stat) && stat != null))
					{
						LokrPatchPlugin.Log.LogWarning(
							"Stats.ApplyModifier: skipped missing stat '" + pair.Key + "'");
						continue;
					}

					stat.AddMultiplicativeModifier(statModifier.id, pair.Value);
				}
			}

			return false;
		}
	}
}
