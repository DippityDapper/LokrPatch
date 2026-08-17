using System.Linq;
using HarmonyLib;
using Ironhide.Legends.Model.Metagame;
using Ironhide.Legends.Model.Metagame.Achievements;
using Ironhide.Legends.Model.Metagame.Adventures;
using Ironhide.Legends.Utils;
using Ironhide.Legends.View.Metagame.Screens.Achievements;

namespace LokrPatch.Patches
{
	/// <summary>Guards atlas achievement startup against missing listener and Lab legend ids.</summary>
	/// <remarks>
	/// UIAchievements.Start subscribes to AchievementListener.Instance with no null check;
	/// that singleton is not in krlegendsatlasscreen. CheckAchievements Count()s
	/// wasteland_completed_with_&lt;legendId&gt; and calls IsCompleted on null when a Lab
	/// legend has no matching Steam row. See docs/issues/unresolved/achievements-nre-on-atlas-load.md.
	/// </remarks>
	internal static class AchievementsNrePatch
	{
		/// <summary>Harmony target for UIAchievements.Start.</summary>
		[HarmonyPatch(typeof(UIAchievements), "Start")]
		private static class UIAchievementsStartPatch
		{
			/// <summary>Skips the IncrementProgressEvent subscribe when AchievementListener is missing.</summary>
			private static bool Prefix(UIAchievements __instance)
			{
				if (!MonoSingleton<AchievementListener>.IsInstanceValid)
				{
					__instance.Invoke("Init", 0f);
					return false;
				}

				return true;
			}
		}

		/// <summary>Harmony target for FullMetagameSessionData.CheckAchievements.</summary>
		[HarmonyPatch(typeof(FullMetagameSessionData), "CheckAchievements")]
		private static class CheckAchievementsPatch
		{
			/// <summary>Replaces CheckAchievements so missing achievement instances are skipped.</summary>
			private static bool Prefix(ref bool __result)
			{
				__result = RunNullSafe();
				return false;
			}

			/// <summary>Null-safe copy of vanilla CheckAchievements.</summary>
			private static bool RunNullSafe()
			{
				bool flag = false;
				IAchievementManager achievementManager = MetagameManager.instance.Player.AchievementManager;
				if (!achievementManager.IsCompleted("migration_easy_mode"))
				{
					foreach (AdventureDefinition adventureDefinition in MetagameManager.instance.Player.AdventureManager.AdventuresConfig.adventures)
					{
						foreach (string text in adventureDefinition.victoryAchievements)
						{
							if (achievementManager.IsCompleted(text))
							{
								achievementManager.IncrementProgress(
									AchievementHelper.MapGeneralToDifficultyAdventure(text, AdventureDifficulty.Normal),
									1);
							}
						}
					}

					achievementManager.IncrementProgress("migration_easy_mode", 1);
					flag = true;
				}

				int unlockedHeroesCount = MetagameManager.instance.Player.HeroRosterManager.GetUnlockedHeroesCount();
				AchievementInstance manyHeroes = achievementManager.GetAchievementInstance("many_heroes_unlocked");
				if (manyHeroes != null)
				{
					int num2 = unlockedHeroesCount - manyHeroes.Current;
					if (num2 > 0)
					{
						flag |= achievementManager.IncrementProgress("many_heroes_unlocked", num2);
					}
				}

				AchievementInstance multipleLegends = achievementManager.GetAchievementInstance("wasteland_multiple_legends");
				if (multipleLegends == null)
				{
					return flag;
				}

				int completedWith = MetagameManager.instance.Player.HeroRosterManager.HeroRosterConfig.legends
					.Select(config => achievementManager.GetAchievementInstance("wasteland_completed_with_" + config.id.ToLower()))
					.Count(instance => instance != null && instance.IsCompleted());
				int delta = completedWith - multipleLegends.Current;
				if (delta > 0)
				{
					flag |= achievementManager.IncrementProgress("wasteland_multiple_legends", delta);
				}

				return flag;
			}
		}
	}
}
