using System.Collections.Generic;
using HarmonyLib;
using Ironhide.Legends.View.Metagame.Screens.ProgressionHelp;
using UnityEngine;

namespace LokrPatch.Patches
{
	/// <summary>Clamps progression-help ShowPage / Next against titles and pages so a shorter titles list does not throw.</summary>
	/// <remarks>
	/// Vanilla Next increments then ShowPage, which indexes titles[index] with no length check.
	/// When titles.Count is smaller than pages.Count, Next walks off titles. This prefix calls
	/// Finished instead of inventing pages. Matching lists still walk to Finished on the last page.
	/// </remarks>
	internal static class ProgressionHelpPopupPatch
	{
		[HarmonyPatch(typeof(UIProgressionHelpPopup), "ShowPage")]
		private static class ShowPagePatch
		{
			private static bool Prefix(UIProgressionHelpPopup __instance, int index)
			{
				int titlesCount;
				int pagesCount;
				ReadCounts(__instance, out titlesCount, out pagesCount);
				if (PatchRules.IsProgressionHelpIndexInRange(index, titlesCount, pagesCount))
				{
					return true;
				}

				LokrPatchPlugin.Log.LogWarning(string.Format(
					"UIProgressionHelpPopup.ShowPage: index {0} out of range (titles {1}, pages {2}); skipped.",
					index, titlesCount, pagesCount));
				return false;
			}
		}

		[HarmonyPatch(typeof(UIProgressionHelpPopup), nameof(UIProgressionHelpPopup.Next))]
		private static class NextPatch
		{
			private static bool Prefix(UIProgressionHelpPopup __instance)
			{
				Traverse traverse = Traverse.Create(__instance);
				int pageIndex = traverse.Field<int>("pageIndex").Value;
				int titlesCount;
				int pagesCount;
				ReadCounts(__instance, out titlesCount, out pagesCount);
				if (!PatchRules.ProgressionHelpNextShouldFinish(pageIndex, titlesCount, pagesCount))
				{
					return true;
				}

				LokrPatchPlugin.Log.LogWarning(string.Format(
					"UIProgressionHelpPopup.Next: pageIndex {0} would leave titles {1} / pages {2}; Finished.",
					pageIndex, titlesCount, pagesCount));
				__instance.Finished();
				return false;
			}
		}

		private static void ReadCounts(UIProgressionHelpPopup instance, out int titlesCount, out int pagesCount)
		{
			Traverse traverse = Traverse.Create(instance);
			List<string> titles = traverse.Field<List<string>>("titles").Value;
			List<GameObject> pages = traverse.Field<List<GameObject>>("pages").Value;
			titlesCount = titles != null ? titles.Count : 0;
			pagesCount = pages != null ? pages.Count : 0;
		}
	}
}
