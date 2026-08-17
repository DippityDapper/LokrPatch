using System;
using System.Collections.Generic;
using HarmonyLib;
using Ironhide.Legends.Model.Game.Units;
using Ironhide.Legends.Model.Metagame;
using Ironhide.Legends.View.Metagame.Screens.Logic;

namespace LokrPatch.Patches
{
	/// <summary>Loads the party as slot-aligned known uniqueIds, stows the rest, and never forces a 3-hero trio.</summary>
	/// <remarks>
	/// Vanilla <see cref="HeroRosterManager.Load"/> drops unknown ids then replaces any party whose
	/// <c>Count != 3</c> with Gerald/Ranger/ArcaneMage. Compacting known ids shifts companions into
	/// the gold legend frame; Save used to append stowed ids and permute the blob. Live
	/// <c>Party</c> stays compacted (no holes) for vanilla consumers. Slot-aligned holes are kept
	/// for hero-room / adventure-brief paint and for Save splice. Must ship with
	/// <c>SaveGameSanitizePatch</c>.
	/// </remarks>
	internal static class HeroRosterLoadPartyPatch
	{
		private static readonly AccessTools.FieldRef<HeroRosterManager, List<HeroRosterManager.HeroRosterItemSave>> StowawayItemsField =
			AccessTools.FieldRefAccess<HeroRosterManager, List<HeroRosterManager.HeroRosterItemSave>>("stowawayItems");

		private static readonly AccessTools.FieldRef<HeroRosterManager, Dictionary<string, HeroRosterManager.HeroRosterItemSave>> HeroRosterStateField =
			AccessTools.FieldRefAccess<HeroRosterManager, Dictionary<string, HeroRosterManager.HeroRosterItemSave>>("heroRosterState");

		private static readonly AccessTools.FieldRef<HeroRosterManager, List<string>> PartyField =
			AccessTools.FieldRefAccess<HeroRosterManager, List<string>>("party");

		private static readonly List<StowedPartyMember> StowedPartyMembers = new List<StowedPartyMember>();

		/// <summary>Known uniqueIds in legend/companion slot order, with null holes for missing members.</summary>
		internal static readonly List<string> SlotAlignedParty = new List<string>();

		[HarmonyPatch(typeof(HeroRosterManager), nameof(HeroRosterManager.Load))]
		private static class LoadPatch
		{
			private static bool Prefix(HeroRosterManager __instance, HeroRosterManager.HeroRosterSave save)
			{
				StowedPartyMembers.Clear();
				SlotAlignedParty.Clear();
				if (save == null)
				{
					return true;
				}

				List<HeroRosterManager.HeroRosterItemSave> stowawayItems = StowawayItemsField(__instance);
				if (stowawayItems == null)
				{
					stowawayItems = new List<HeroRosterManager.HeroRosterItemSave>();
					StowawayItemsField(__instance) = stowawayItems;
				}
				else
				{
					stowawayItems.Clear();
				}

				Dictionary<string, HeroRosterManager.HeroRosterItemSave> heroRosterState =
					new Dictionary<string, HeroRosterManager.HeroRosterItemSave>();
				if (save.data != null)
				{
					foreach (HeroRosterManager.HeroRosterItemSave heroRosterItemSave in save.data)
					{
						if (heroRosterItemSave == null)
						{
							continue;
						}

						if (UnityDefinitionsParser.instance.DefinitionsByUnique.ContainsKey(heroRosterItemSave.id))
						{
							heroRosterState.Add(heroRosterItemSave.id, heroRosterItemSave.Clone());
						}
						else
						{
							stowawayItems.Add(heroRosterItemSave);
						}
					}
				}

				HeroRosterStateField(__instance) = heroRosterState;

				HashSet<string> registered = new HashSet<string>(UnityDefinitionsParser.instance.DefinitionsByUnique.Keys);
				List<string> splitSlots = new List<string>();
				if (save.party != null)
				{
					PatchRules.SplitSaveParty(
						save.party,
						registered,
						splitSlots,
						StowedPartyMembers);
					foreach (StowedPartyMember member in StowedPartyMembers)
					{
						LokrPatchPlugin.Log.LogWarning(
							"HeroRosterManager.Load: stowed unknown party uniqueId '" + member.UniqueId + "'.");
					}
				}

				HashSet<string> legendIds = CollectLegendIds(splitSlots, __instance);
				List<string> aligned = PatchRules.AlignPartySlotsByRole(splitSlots, legendIds);
				SlotAlignedParty.AddRange(aligned);
				PartyField(__instance) = PatchRules.CompactPartyIds(aligned);
				return false;
			}
		}

		[HarmonyPatch(typeof(HeroRosterManager), nameof(HeroRosterManager.Save))]
		private static class SavePatch
		{
			private static void Postfix(HeroRosterManager.HeroRosterSave __result)
			{
				if (__result == null)
				{
					return;
				}

				IList<string> slots = SlotAlignedParty.Count > 0 ? SlotAlignedParty : __result.party;
				__result.party = PatchRules.MergeStowedPartyIds(slots, StowedPartyMembers);
			}
		}

		/// <summary>Copies portrait uniqueIds (holes included) into slot-aligned state and compacts the live party.</summary>
		internal static void CaptureSlotsFromUi(IList<string> portraitIds)
		{
			SlotAlignedParty.Clear();
			if (portraitIds != null)
			{
				foreach (string id in portraitIds)
				{
					SlotAlignedParty.Add(PatchRules.ShouldOmitPartyId(id) ? null : id);
				}
			}

			HeroRosterManager roster = TryGetRoster();
			if (roster != null)
			{
				PartyField(roster) = PatchRules.CompactPartyIds(SlotAlignedParty);
			}
		}

		/// <summary>Strips null uniqueIds from the live party after vanilla OnDone may have copied them.</summary>
		internal static void CompactLiveParty()
		{
			HeroRosterManager roster = TryGetRoster();
			if (roster == null)
			{
				return;
			}

			PartyField(roster) = PatchRules.CompactPartyIds(PartyField(roster));
		}

		/// <summary>Places known uniqueIds into legend slot 0 and companion slots 1–2 for portrait chrome that is index-based.</summary>
		internal static List<string> AlignPartyForDisplay(IList<string> ids)
		{
			return PatchRules.AlignPartySlotsByRole(ids, CollectLegendIds(ids, TryGetRoster()));
		}

		private static HeroRosterManager TryGetRoster()
		{
			try
			{
				return RosterFrom(MetagameManager.instanceNoLoad);
			}
			catch (Exception)
			{
				return null;
			}
		}

		private static HeroRosterManager RosterFrom(MetagameManager manager)
		{
			if (manager == null || manager.Player == null)
			{
				return null;
			}

			return manager.Player.HeroRosterManager as HeroRosterManager;
		}

		private static HashSet<string> CollectLegendIds(IList<string> knownSlots, HeroRosterManager roster)
		{
			HashSet<string> legends = new HashSet<string>();
			if (knownSlots != null)
			{
				foreach (string id in knownSlots)
				{
					if (IsLegendUniqueId(id, roster))
					{
						legends.Add(id);
					}
				}
			}

			return legends;
		}

		private static bool IsLegendUniqueId(string uniqueId, HeroRosterManager roster)
		{
			if (string.IsNullOrEmpty(uniqueId))
			{
				return false;
			}

			UnityDefinitionsParser parser = UnityDefinitionsParser.instance;
			if (parser != null && parser.DefinitionsByUnique != null)
			{
				UnitDefinition definition;
				if (parser.DefinitionsByUnique.TryGetValue(uniqueId, out definition)
					&& definition != null
					&& definition.cinematicTags != null
					&& definition.cinematicTags.Contains("Legend"))
				{
					return true;
				}
			}

			if (roster != null && roster.HeroRosterConfig != null && roster.HeroRosterConfig.legends != null)
			{
				foreach (HeroRosterConfig.HeroConfig config in roster.HeroRosterConfig.legends)
				{
					if (config != null && config.id == uniqueId)
					{
						return true;
					}
				}
			}

			return false;
		}
	}
}
