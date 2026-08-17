using HarmonyLib;
using Ironhide.Legends.Model.Game.Units;

namespace LokrPatch.Patches
{
	/// <summary>Skips <see cref="EndTurnClassIcon.SetUnitClassIcon"/> when the unit is not on the skills bar yet.</summary>
	/// <remarks>
	/// Vanilla indexes <c>SkillsBar.skillPerUnits[unit.unitView]</c> with no TryGetValue.
	/// FightStarted can run before <c>AddSkillsBar</c>, and custom units never land in that
	/// map the same frame, so the coroutine dies and End Turn / hex targeting never finish setup.
	/// </remarks>
	[HarmonyPatch(typeof(EndTurnClassIcon), nameof(EndTurnClassIcon.SetUnitClassIcon))]
	internal static class EndTurnClassIconPatch
	{
		private static bool Prefix(Unit unit)
		{
			if (unit == null || unit.unitView == null || SkillsBar.InstSkill == null
				|| SkillsBar.InstSkill.skillPerUnits == null)
			{
				return false;
			}

			return SkillsBar.InstSkill.skillPerUnits.ContainsKey(unit.unitView);
		}
	}
}
