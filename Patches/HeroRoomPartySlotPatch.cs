using System.Collections.Generic;
using HarmonyLib;
using Ironhide.Legends.Model.Metagame;
using Ironhide.Legends.Model.Metagame.Heroes;
using Ironhide.Legends.View.Metagame.Screens.HeroRoom;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LokrPatch.Patches
{
	/// <summary>Paints party portraits from slot-aligned holes and keeps null uniqueIds out of the live party.</summary>
	/// <remarks>
	/// Vanilla <c>UIHeroRoom.LoadData</c> writes <c>party[i]</c> into slot <c>i</c> and never
	/// clears unused portraits, so a compacted list puts a companion in the gold frame and leaves
	/// a leftover sprite with a null <c>heroId</c>. An in-progress run must paint <c>HeroManager</c>
	/// heroes, not the metagame roster, or a newly equipped legend appears on the brief without
	/// being in the run. Core slots 0–2 stay visible with DEFAULT_MINI when empty; the
	/// in-adventure fourth recruit slot stays hidden until a fourth hero exists.
	/// </remarks>
	internal static class HeroRoomPartySlotPatch
	{
		private static readonly AccessTools.FieldRef<UIHeroRoom, UIHeroRoomCurrentAdventurers> HeroRoomAdventurersField =
			AccessTools.FieldRefAccess<UIHeroRoom, UIHeroRoomCurrentAdventurers>("currentAdventurers");

		private static readonly AccessTools.FieldRef<UIAdventureBrief, UIHeroRoomCurrentAdventurers> AdventureBriefAdventurersField =
			AccessTools.FieldRefAccess<UIAdventureBrief, UIHeroRoomCurrentAdventurers>("currentAdventurers");

		private static readonly AccessTools.FieldRef<UIHeroRoomHeroBarPortrait, string> PortraitHeroIdField =
			AccessTools.FieldRefAccess<UIHeroRoomHeroBarPortrait, string>("heroId");

		private static readonly AccessTools.FieldRef<UIHeroRoomHeroBarPortrait, Image> PortraitImageField =
			AccessTools.FieldRefAccess<UIHeroRoomHeroBarPortrait, Image>("image");

		private static readonly AccessTools.FieldRef<UIHeroRoomHeroBarPortrait, UIHeroRoomSmallXPBar> PortraitXpBarField =
			AccessTools.FieldRefAccess<UIHeroRoomHeroBarPortrait, UIHeroRoomSmallXPBar>("xpBar");

		private static readonly AccessTools.FieldRef<UIHeroRoomHeroBarPortrait, Image> PortraitBannerField =
			AccessTools.FieldRefAccess<UIHeroRoomHeroBarPortrait, Image>("bannerImage");

		private static readonly AccessTools.FieldRef<UIHeroRoomHeroBarPortrait, TextMeshProUGUI> PortraitNameField =
			AccessTools.FieldRefAccess<UIHeroRoomHeroBarPortrait, TextMeshProUGUI>("heroNameText");

		private static readonly AccessTools.FieldRef<UIHeroRoomHeroBarPortrait, GameObject> PortraitLockedField =
			AccessTools.FieldRefAccess<UIHeroRoomHeroBarPortrait, GameObject>("lockedImage");

		[HarmonyPatch(typeof(UIHeroRoom), "LoadData")]
		private static class LoadDataPatch
		{
			private static void Postfix(UIHeroRoom __instance)
			{
				ApplySlotAlignedPortraits(HeroRoomAdventurersField(__instance), false, false, null);
			}
		}

		[HarmonyPatch(typeof(UIAdventureBrief), "SetupParty")]
		private static class SetupPartyPatch
		{
			private static void Postfix(UIAdventureBrief __instance, bool withFlash, bool isTutorialAdventure)
			{
				bool runInProgress = MetagameManager.instance != null
					&& MetagameManager.instance.SaveGameManager != null
					&& MetagameManager.instance.SaveGameManager.HasRunInProgress();
				bool isLocked = isTutorialAdventure || runInProgress;
				IList<string> runSlots = runInProgress ? AlignRunHeroes() : null;
				ApplySlotAlignedPortraits(AdventureBriefAdventurersField(__instance), withFlash, isLocked, runSlots);
			}
		}

		[HarmonyPatch(typeof(UIHeroRoom), nameof(UIHeroRoom.OnDone))]
		private static class OnDonePatch
		{
			private static void Prefix(UIHeroRoom __instance)
			{
				UIHeroRoomCurrentAdventurers adventurers = HeroRoomAdventurersField(__instance);
				if (adventurers == null)
				{
					return;
				}

				HeroRosterLoadPartyPatch.CaptureSlotsFromUi(adventurers.GetParty());
			}

			private static void Postfix()
			{
				HeroRosterLoadPartyPatch.CompactLiveParty();
			}
		}

		[HarmonyPatch(typeof(UIHeroRoomCurrentAdventurers), nameof(UIHeroRoomCurrentAdventurers.SetHero))]
		private static class SetHeroClearPatch
		{
			private static bool Prefix(UIHeroRoomCurrentAdventurers __instance, int index, string heroId, bool isLocked)
			{
				if (!PatchRules.ShouldOmitPartyId(heroId))
				{
					return true;
				}

				if (__instance.portraits == null || index < 0 || index >= __instance.portraits.Count)
				{
					return false;
				}

				UIHeroRoomHeroBarPortrait portrait = __instance.portraits[index];
				if (portrait == null)
				{
					return false;
				}

				if (!PatchRules.IsCorePartySlot(index))
				{
					PortraitHeroIdField(portrait) = null;
					portrait.gameObject.SetActive(false);
					return false;
				}

				ClearEmptyPortrait(portrait, isLocked);
				portrait.gameObject.SetActive(true);
				return false;
			}
		}

		[HarmonyPatch(typeof(UIHeroRoomHeroBarPortrait), nameof(UIHeroRoomHeroBarPortrait.CheckRankedUpState))]
		private static class CheckRankedUpStatePatch
		{
			private static bool Prefix(UIHeroRoomHeroBarPortrait __instance)
			{
				return !PatchRules.ShouldOmitPartyId(__instance.HeroId);
			}
		}

		[HarmonyPatch(typeof(UIHeroRoom), "HeroSelectedInCurrentAdventurers")]
		private static class HeroSelectedInCurrentAdventurersPatch
		{
			private static bool Prefix(string heroId)
			{
				return !PatchRules.ShouldOmitPartyId(heroId);
			}
		}

		[HarmonyPatch(typeof(UISlot), nameof(UISlot.UpdateContent))]
		private static class SaveSlotMiniPortraitsPatch
		{
			private static void Postfix(UISlot __instance, SaveGameMetadata loc)
			{
				if (loc == null)
				{
					return;
				}

				List<string> source = null;
				if (loc.status == SaveGameStatus.VALID && loc.heroParty != null && loc.heroParty.Count > 0)
				{
					source = loc.heroParty;
				}
				else if (loc.parsedSaveGame != null
					&& loc.parsedSaveGame.heroRosterData != null
					&& loc.parsedSaveGame.heroRosterData.party != null
					&& loc.parsedSaveGame.heroRosterData.party.Count > 0)
				{
					source = loc.parsedSaveGame.heroRosterData.party;
				}

				if (source == null)
				{
					return;
				}

				Transform miniPortraits = __instance.transform.Find("SlotButton/UsedSlot/MiniPortraits");
				if (miniPortraits == null)
				{
					return;
				}

				List<string> aligned = HeroRosterLoadPartyPatch.AlignPartyForDisplay(source);
				for (int i = 0; i < miniPortraits.childCount; i++)
				{
					GameObject child = miniPortraits.GetChild(i).gameObject;
					string heroId = i < aligned.Count ? aligned[i] : null;
					bool hasHero = !PatchRules.ShouldOmitPartyId(heroId);
					if (!hasHero && !PatchRules.IsCorePartySlot(i))
					{
						child.SetActive(false);
						continue;
					}

					child.SetActive(true);
					Image image = child.GetComponent<Image>();
					if (image == null)
					{
						continue;
					}

					image.sprite = hasHero
						? DataHelper.LoadMiniPortrait(heroId)
						: DataHelper.LoadMiniPortrait(string.Empty);
				}
			}
		}

		private static void ApplySlotAlignedPortraits(
			UIHeroRoomCurrentAdventurers adventurers,
			bool withFlash,
			bool isLocked,
			IList<string> overrideSlots)
		{
			if (adventurers == null || adventurers.portraits == null)
			{
				return;
			}

			IList<string> slots = overrideSlots;
			if (slots == null)
			{
				slots = HeroRosterLoadPartyPatch.SlotAlignedParty.Count > 0
					? HeroRosterLoadPartyPatch.SlotAlignedParty
					: TryLiveParty();
			}

			int portraitCount = adventurers.portraits.Count;
			for (int i = 0; i < portraitCount; i++)
			{
				string heroId = slots != null && i < slots.Count ? slots[i] : null;
				if (PatchRules.ShouldOmitPartyId(heroId))
				{
					adventurers.SetHero(i, null, null, false, isLocked);
				}
				else
				{
					Sprite mini = DataHelper.LoadMiniPortrait(heroId);
					adventurers.SetHero(i, heroId, mini, withFlash, isLocked);
				}
			}
		}

		private static List<string> AlignRunHeroes()
		{
			if (MetagameManager.instance == null || MetagameManager.instance.HeroManager == null)
			{
				return null;
			}

			List<Hero> heroes = MetagameManager.instance.HeroManager.GetAllHeroes();
			if (heroes == null || heroes.Count == 0)
			{
				return null;
			}

			List<string> ids = new List<string>();
			foreach (Hero hero in heroes)
			{
				if (hero != null && hero.unitDefinition != null
					&& !PatchRules.ShouldOmitPartyId(hero.unitDefinition.uniqueId))
				{
					ids.Add(hero.unitDefinition.uniqueId);
				}
			}

			return HeroRosterLoadPartyPatch.AlignPartyForDisplay(ids);
		}

		private static void ClearEmptyPortrait(UIHeroRoomHeroBarPortrait portrait, bool isLocked)
		{
			PortraitHeroIdField(portrait) = null;
			Image image = PortraitImageField(portrait);
			if (image != null)
			{
				image.sprite = DataHelper.LoadMiniPortrait(string.Empty);
			}

			UIHeroRoomSmallXPBar xpBar = PortraitXpBarField(portrait);
			if (xpBar != null)
			{
				xpBar.gameObject.SetActive(false);
			}

			Image banner = PortraitBannerField(portrait);
			if (banner != null)
			{
				banner.gameObject.SetActive(false);
			}

			TextMeshProUGUI nameText = PortraitNameField(portrait);
			if (nameText != null)
			{
				nameText.text = string.Empty;
			}

			GameObject locked = PortraitLockedField(portrait);
			if (locked != null)
			{
				locked.SetActive(isLocked);
			}
		}

		private static List<string> TryLiveParty()
		{
			if (MetagameManager.instance == null || MetagameManager.instance.Player == null
				|| MetagameManager.instance.Player.HeroRosterManager == null)
			{
				return null;
			}

			return MetagameManager.instance.Player.HeroRosterManager.Party;
		}
	}
}
