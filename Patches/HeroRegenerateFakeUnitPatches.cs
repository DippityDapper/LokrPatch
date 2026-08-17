using HarmonyLib;
using Ironhide.Legends.Model.Metagame.Heroes;

namespace LokrPatch.Patches
{
	[HarmonyPatch(typeof(Hero), nameof(Hero.RegenerateFakeUnit))]
	internal static class HeroRegenerateFakeUnitPatches
	{
		[HarmonyPrefix]
		private static void Prefix(Hero __instance)
		{
			HeroSkillSanitizer.RepairMissingBaseSkills(__instance);
			HeroSkillSanitizer.Sanitize(__instance);
		}
	}
}
