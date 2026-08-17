using HarmonyLib;
using Ironhide.Legends.Model.Game.Units;
using Ironhide.Legends.Model.Game.Units.Abilities;
using Ironhide.Legends.Model.Game.Units.Activities;

namespace LokrPatch.Patches
{
	/// <summary>Prevents duplicate skill ids from crashing unit construction.</summary>
	/// <remarks>
	/// Vanilla <see cref="Unit.AddSkill(string, Ability, bool)"/> always dictionary-adds and throws on
	/// collision. Hero saves with duplicated progression picks (or default skill also listed in skills)
	/// hard-crash load; skipping the duplicate keeps the first registration.
	/// </remarks>
	internal static class UnitAddSkillPatches
	{
		[HarmonyPatch(typeof(Unit), nameof(Unit.AddSkill), typeof(string), typeof(Ability), typeof(bool))]
		private static class AbilityOverload
		{
			[HarmonyPrefix]
			private static bool Prefix(Unit __instance, string skillId)
			{
				return !TrySkipDuplicate(__instance, skillId);
			}
		}

		[HarmonyPatch(typeof(Unit), nameof(Unit.AddSkill), typeof(string), typeof(Activity))]
		private static class ActivityOverload
		{
			[HarmonyPrefix]
			private static bool Prefix(Unit __instance, string skillId)
			{
				return !TrySkipDuplicate(__instance, skillId);
			}
		}

		private static bool TrySkipDuplicate(Unit unit, string skillId)
		{
			if (unit == null || string.IsNullOrEmpty(skillId) || !unit.HasSkill(skillId))
			{
				return false;
			}

			LokrPatchPlugin.Log.LogWarning(string.Format(
				"Unit({0}): skipped duplicate skill '{1}'",
				unit.DefinitionId,
				skillId));
			return true;
		}
	}
}
