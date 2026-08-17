using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Ironhide.Legends.Model.Game.Units;
using Ironhide.Legends.Model.Metagame;
using Ironhide.Legends.Model.Metagame.Achievements;
using Ironhide.Legends.Model.Metagame.Adventures;
using Ironhide.Legends.Model.Metagame.Heroes;
using Ironhide.Legends.Model.Metagame.Inventory;
using Ironhide.Legends.Model.Metagame.Map;
using Ironhide.Legends.Model.Metagame.Map.Quests;
using Ironhide.Legends.Utils;
using UnityEngine;

namespace LokrPatch.Patches
{
	/// <summary>Keeps unknown adventure / hero / quest / item ids in the save instead of discarding the run.</summary>
	/// <remarks>
	/// Vanilla <see cref="SaveGameMetadata.Sanitize"/> calls <c>DiscardRun</c> (and can write
	/// <c>CreateStartingPartyIds</c>) when any id is unregistered. Stow bags are append-only on Save
	/// so a missing pack can be reinstalled without silently rewriting the blob. Party-list mutation
	/// is owned by <c>HeroRosterLoadPartyPatch</c> — this file never assigns the vanilla trio.
	/// </remarks>
	internal static class SaveGameSanitizePatch
	{
		private static readonly AccessTools.FieldRef<HeroManager, List<Hero>> HeroesField =
			AccessTools.FieldRefAccess<HeroManager, List<Hero>>("heroes");

		private static readonly AccessTools.FieldRef<HeroManager, List<Hero>> OldHeroesField =
			AccessTools.FieldRefAccess<HeroManager, List<Hero>>("oldHeroes");

		private static readonly AccessTools.FieldRef<HeroManager, List<string>> StartingHeroesField =
			AccessTools.FieldRefAccess<HeroManager, List<string>>("startingHeroes");

		private static readonly AccessTools.FieldRef<MapManager, Dictionary<string, MapQuestStatus>> QuestStatusesField =
			AccessTools.FieldRefAccess<MapManager, Dictionary<string, MapQuestStatus>>("questStatuses");

		private static readonly AccessTools.FieldRef<MapManager, string> MapPrefabNameField =
			AccessTools.FieldRefAccess<MapManager, string>("mapPrefabName");

		private static readonly AccessTools.FieldRef<MapManager, int> DarknessCounterField =
			AccessTools.FieldRefAccess<MapManager, int>("darknessCounter");

		private static readonly AccessTools.FieldRef<MapManager, int> DarknessLevelField =
			AccessTools.FieldRefAccess<MapManager, int>("darknessLevel");

		private static readonly AccessTools.FieldRef<MapManager, List<string>> QuestsInAdventureField =
			AccessTools.FieldRefAccess<MapManager, List<string>>("questsInAdventure");

		private static readonly AccessTools.FieldRef<MapManager, List<string>> QuestsInGameField =
			AccessTools.FieldRefAccess<MapManager, List<string>>("questsInGame");

		private static readonly AccessTools.FieldRef<MapManager, List<string>> EphemeralQuestsField =
			AccessTools.FieldRefAccess<MapManager, List<string>>("ephemeralQuests");

		private static readonly AccessTools.FieldRef<MapManager, string> ActiveEphemeralQuestField =
			AccessTools.FieldRefAccess<MapManager, string>("activeEphemeralQuest");

		private static readonly List<HeroDefinition> StowedHeroes = new List<HeroDefinition>();

		private static readonly List<HeroDefinition> StowedOldHeroes = new List<HeroDefinition>();

		private static readonly List<InventoryManager.InventoryItemSave> StowedItems =
			new List<InventoryManager.InventoryItemSave>();

		private static readonly List<MapQuestStatus> StowedQuestStatuses = new List<MapQuestStatus>();

		[HarmonyPatch(typeof(SaveGameMetadata), nameof(SaveGameMetadata.Sanitize))]
		private static class SanitizePatch
		{
			private static bool Prefix(SaveGameMetadata __instance, ref bool __result)
			{
				SaveGame saveGame = __instance.parsedSaveGame;
				if (saveGame == null)
				{
					Debug.LogError("Sanitize: parsedSaveGame is null");
					__result = false;
					return false;
				}

				if (saveGame.run == null)
				{
					Debug.LogError("Sanitize: run is null");
					__result = false;
					return false;
				}

				LogUnknownPartyIds(saveGame);
				MergeAchievementStates(__instance, saveGame);
				LogUnknownAdventure(saveGame);
				LogUnknownHeroArchetypes(saveGame);
				LogUnknownStartingHeroes(saveGame);
				LogUnknownQuestIds(saveGame);
				LogUnknownItemArchetypes(saveGame);

				__result = true;
				return false;
			}
		}

		[HarmonyPatch(typeof(SaveGameMetadata), nameof(SaveGameMetadata.ExtractInfo))]
		private static class ExtractInfoPatch
		{
			private static bool Prefix(SaveGameMetadata __instance, ref bool __result)
			{
				try
				{
					SaveGame saveGame = __instance.parsedSaveGame;
					if (saveGame == null)
					{
						__result = false;
						return false;
					}

					if (saveGame.run != null && saveGame.run.activeRun && saveGame.saveVersion == -990)
					{
						__instance.runAdventure = saveGame.run.gameData.currentAdventure ?? "";
						__instance.runNode = saveGame.run.mapSave.lastNodeVisited ?? "";
						__instance.heroParty = KnownHeroPartyUniqueIds(saveGame);
						__instance.runQuestsTotal = saveGame.run.mapSave.questStatuses != null
							? saveGame.run.mapSave.questStatuses.Count
							: 0;
						__instance.runQuestsDone = 0;
						if (saveGame.run.mapSave.questStatuses != null)
						{
							foreach (MapQuestStatus status in saveGame.run.mapSave.questStatuses)
							{
								if (status != null && status.visited)
								{
									__instance.runQuestsDone++;
								}
							}
						}
					}
					else
					{
						__instance.lastAdventure = "tutorial";
						if (saveGame.adventureData != null && saveGame.adventureData.adventures != null)
						{
							foreach (AdventureManager.AdventureSaveItem adventureSaveItem in saveGame.adventureData.adventures)
							{
								if (adventureSaveItem == null)
								{
									continue;
								}

								AdventureDefinitionConfig config = MetagameManager.instanceNoLoad.Player.AdventureManager.AdventuresConfig;
								if (config != null
									&& config.adventuresById.GetValueOrDefault(adventureSaveItem.id, null) != null
									&& adventureSaveItem.seenUnlock)
								{
									__instance.lastAdventure = adventureSaveItem.id;
								}
							}
						}
					}

					IAchievementManager achMan = MetagameManager.instanceNoLoad.Player.AchievementManager;
					__instance.achievementsTotal = achMan.GetAllDefinitions()
						.FindAll(ach => ach.notificationLocation != AchievementNotificationLocation.Hidden)
						.Count;
					__instance.achievementsDone = saveGame.achievementData.states.FindAll(ach =>
						achMan.GetDefinition(ach.id) != null
						&& achMan.GetDefinition(ach.id).notificationLocation != AchievementNotificationLocation.Hidden
						&& ach.status > AchievementStateStatus.Uncompleted
						&& ach.status < AchievementStateStatus.Ignore).Count;
					__result = true;
				}
				catch (Exception)
				{
					Debug.Log("ExtractInfo: extracting savegame information. guid:{saveGuid} version:{saveVersion}");
					__result = false;
				}

				return false;
			}
		}

		[HarmonyPatch(typeof(HeroManager), nameof(HeroManager.Load))]
		private static class HeroManagerLoadPatch
		{
			private static bool Prefix(HeroManager __instance, HeroManager.HeroesSave save)
			{
				StowedHeroes.Clear();
				StowedOldHeroes.Clear();
				if (save == null)
				{
					return true;
				}

				List<Hero> heroes = new List<Hero>();
				if (save.heroes != null)
				{
					foreach (HeroDefinition definition in save.heroes)
					{
						if (definition == null)
						{
							continue;
						}

						if (IsKnownArchetype(definition.archetype))
						{
							heroes.Add(new Hero(definition.Clone(), __instance, __instance.upgradeManager));
						}
						else
						{
							StowedHeroes.Add(definition.Clone());
							LokrPatchPlugin.Log.LogWarning(
								"HeroManager.Load: stowed unknown hero archetype '" + definition.archetype + "'.");
						}
					}
				}

				List<Hero> oldHeroes = new List<Hero>();
				if (save.oldHeroes != null)
				{
					foreach (HeroDefinition definition in save.oldHeroes)
					{
						if (definition == null)
						{
							continue;
						}

						if (IsKnownArchetype(definition.archetype))
						{
							oldHeroes.Add(new Hero(definition.Clone(), __instance, __instance.upgradeManager));
						}
						else
						{
							StowedOldHeroes.Add(definition.Clone());
							LokrPatchPlugin.Log.LogWarning(
								"HeroManager.Load: stowed unknown old-hero archetype '" + definition.archetype + "'.");
						}
					}
				}

				HeroesField(__instance) = heroes;
				OldHeroesField(__instance) = oldHeroes;
				StartingHeroesField(__instance) = save.startingHeroes != null
					? save.startingHeroes.ToList()
					: new List<string>();
				return false;
			}
		}

		[HarmonyPatch(typeof(HeroManager), nameof(HeroManager.Save))]
		private static class HeroManagerSavePatch
		{
			private static void Postfix(HeroManager.HeroesSave __result)
			{
				if (__result == null)
				{
					return;
				}

				AppendHeroDefinitions(__result.heroes, StowedHeroes);
				if (__result.oldHeroes == null)
				{
					__result.oldHeroes = new List<HeroDefinition>();
				}

				AppendHeroDefinitions(__result.oldHeroes, StowedOldHeroes);
			}
		}

		[HarmonyPatch(typeof(InventoryManager), nameof(InventoryManager.Load))]
		private static class InventoryManagerLoadPatch
		{
			private static bool Prefix(InventoryManager __instance, InventoryManager.InventorySave save)
			{
				StowedItems.Clear();
				if (save == null)
				{
					return true;
				}

				List<ItemInstance> inventory = new List<ItemInstance>();
				if (save.inventory != null)
				{
					foreach (InventoryManager.InventoryItemSave itemSave in save.inventory)
					{
						if (itemSave == null)
						{
							continue;
						}

						if (__instance.itemDefinitions != null
							&& __instance.itemDefinitions.ContainsKey(itemSave.itemArchetype))
						{
							inventory.Add(new ItemInstance
							{
								id = itemSave.id,
								quantity = itemSave.quantity,
								itemDefinition = __instance.GetItemDefinition(itemSave.itemArchetype)
							});
						}
						else
						{
							StowedItems.Add(CopyItemSave(itemSave));
							LokrPatchPlugin.Log.LogWarning(
								"InventoryManager.Load: stowed unknown itemArchetype '" + itemSave.itemArchetype + "'.");
						}
					}
				}

				__instance.inventory = inventory;
				return false;
			}
		}

		[HarmonyPatch(typeof(InventoryManager), nameof(InventoryManager.Save))]
		private static class InventoryManagerSavePatch
		{
			private static void Postfix(InventoryManager.InventorySave __result)
			{
				if (__result == null)
				{
					return;
				}

				if (__result.inventory == null)
				{
					__result.inventory = new List<InventoryManager.InventoryItemSave>();
				}

				foreach (InventoryManager.InventoryItemSave row in StowedItems)
				{
					__result.inventory.Add(CopyItemSave(row));
				}
			}
		}

		[HarmonyPatch(typeof(MapManager), nameof(MapManager.Load))]
		private static class MapManagerLoadPatch
		{
			private static bool Prefix(MapManager __instance, MapManager.MapSave save)
			{
				StowedQuestStatuses.Clear();
				if (save == null)
				{
					return true;
				}

				Dictionary<string, MapQuestStatus> live = new Dictionary<string, MapQuestStatus>();
				if (save.questStatuses != null)
				{
					foreach (MapQuestStatus status in save.questStatuses)
					{
						if (status == null)
						{
							continue;
						}

						MapQuestStatus clone = status.Clone();
						MapQuestDefinition definition = __instance.GetMapQuestDefinition(clone.questName);
						if (definition != null)
						{
							clone.quest = definition;
							live[clone.questInstanceId] = clone;
						}
						else
						{
							StowedQuestStatuses.Add(clone);
							LokrPatchPlugin.Log.LogWarning(
								"MapManager.Load: stowed unknown quest '" + clone.questName + "'.");
						}
					}
				}

				QuestStatusesField(__instance) = live;
				__instance.LastQuestVisited = save.lastQuestVisited;
				__instance.LastNodeVisited = save.lastNodeVisited;
				__instance.PreviousNormalNode = save.previousNormalNode;
				MapPrefabNameField(__instance) = save.mapPrefab;
				__instance.ReachPending = save.reachPending;
				__instance.GoToNode = save.goToNode;
				DarknessCounterField(__instance) = save.darknessCounter;
				DarknessLevelField(__instance) = save.darknessLevel;
				QuestsInAdventureField(__instance) = CopyStringList(save.questsInAdventure);
				QuestsInGameField(__instance) = CopyStringList(save.questsInGame);
				EphemeralQuestsField(__instance) = CopyStringList(save.ephemeralQuests);
				ActiveEphemeralQuestField(__instance) = save.activeEphemeralQuest;
				return false;
			}
		}

		[HarmonyPatch(typeof(MapManager), nameof(MapManager.Save))]
		private static class MapManagerSavePatch
		{
			private static void Postfix(MapManager.MapSave __result)
			{
				if (__result == null)
				{
					return;
				}

				if (__result.questStatuses == null)
				{
					__result.questStatuses = new List<MapQuestStatus>();
				}

				HashSet<string> present = new HashSet<string>();
				foreach (MapQuestStatus status in __result.questStatuses)
				{
					if (status != null && !string.IsNullOrEmpty(status.questInstanceId))
					{
						present.Add(status.questInstanceId);
					}
				}

				foreach (MapQuestStatus stowed in StowedQuestStatuses)
				{
					if (stowed == null || present.Contains(stowed.questInstanceId))
					{
						continue;
					}

					__result.questStatuses.Add(stowed.Clone());
				}
			}
		}

		private static void LogUnknownPartyIds(SaveGame saveGame)
		{
			if (saveGame.heroRosterData == null || saveGame.heroRosterData.party == null)
			{
				return;
			}

			List<string> missing = new List<string>();
			foreach (string id in saveGame.heroRosterData.party)
			{
				if (!UnityDefinitionsParser.instance.DefinitionsByUnique.ContainsKey(id))
				{
					missing.Add(id);
				}
			}

			if (missing.Count > 0)
			{
				LokrPatchPlugin.Log.LogWarning(
					"Sanitize: unknown party uniqueId(s) " + JoinIds(missing) + "; leaving party list unchanged.");
			}
		}

		private static void MergeAchievementStates(SaveGameMetadata metadata, SaveGame saveGame)
		{
			IAchievementManager achievementManager = MetagameManager.instanceNoLoad.Player.AchievementManager;
			if (saveGame.achievementData == null || saveGame.achievementData.states == null)
			{
				return;
			}

			foreach (AchievementState achievementDataState in saveGame.achievementData.states)
			{
				AchievementDefinition definition = achievementManager.GetDefinition(achievementDataState.id);
				if (definition == null)
				{
					continue;
				}

				if (definition.scope == AchievementScope.Slot)
				{
					UpdateAchievementState(achievementDataState, achievementDataState, definition);
				}
				else if (definition.scope == AchievementScope.Run
					&& metadata.parsedSaveGame.run != null
					&& metadata.parsedSaveGame.run.achievementData != null
					&& metadata.parsedSaveGame.run.achievementData.states != null)
				{
					AchievementState achievementState = metadata.parsedSaveGame.run.achievementData.states
						.FirstOrDefault(state => state.id == achievementDataState.id);
					if (achievementState != null)
					{
						UpdateAchievementState(achievementDataState, achievementState, definition);
					}
				}
			}
		}

		private static void UpdateAchievementState(
			AchievementState mainState,
			AchievementState secondaryState,
			AchievementDefinition definition)
		{
			int[] currents = secondaryState.currents;
			int[] totals = definition.totals;
			Array.Resize(ref currents, totals.Length);
			for (int i = 0; i < currents.Length; i++)
			{
				currents[i] = Math.Min(currents[i], totals[i]);
			}

			secondaryState.currents = currents;
			if (mainState.status == AchievementStateStatus.Uncompleted)
			{
				int num = totals.Sum();
				if (currents.Sum() >= num)
				{
					mainState.status = AchievementStateStatus.CompletedAndNotified;
				}
			}
			else
			{
				secondaryState.currents = totals.ToArray();
			}
		}

		private static void LogUnknownAdventure(SaveGame saveGame)
		{
			string currentAdventure = saveGame.run.gameData != null
				? saveGame.run.gameData.currentAdventure
				: null;
			if (string.IsNullOrEmpty(currentAdventure))
			{
				return;
			}

			Dictionary<string, AdventureDefinition> adventuresById =
				MetagameManager.instanceNoLoad.Player.AdventureManager.AdventuresConfig.adventuresById;
			if (adventuresById != null && adventuresById.ContainsKey(currentAdventure))
			{
				return;
			}

			LokrPatchPlugin.Log.LogWarning(
				"Sanitize: unknown currentAdventure '" + currentAdventure + "'; leaving run intact.");
		}

		private static void LogUnknownHeroArchetypes(SaveGame saveGame)
		{
			if (saveGame.run.heroesData == null || saveGame.run.heroesData.heroes == null)
			{
				return;
			}

			List<string> missing = new List<string>();
			foreach (HeroDefinition heroDefinition in saveGame.run.heroesData.heroes)
			{
				if (heroDefinition != null && !IsKnownArchetype(heroDefinition.archetype))
				{
					missing.Add(heroDefinition.archetype);
				}
			}

			if (missing.Count > 0)
			{
				LokrPatchPlugin.Log.LogWarning(
					"Sanitize: unknown hero archetype(s) " + JoinIds(missing) + "; leaving run intact.");
			}
		}

		private static void LogUnknownStartingHeroes(SaveGame saveGame)
		{
			if (saveGame.run.heroesData == null || saveGame.run.heroesData.startingHeroes == null)
			{
				return;
			}

			List<string> missing = new List<string>();
			foreach (string uniqueId in saveGame.run.heroesData.startingHeroes)
			{
				if (!UnityDefinitionsParser.instance.DefinitionsByUnique.ContainsKey(uniqueId))
				{
					missing.Add(uniqueId);
				}
			}

			if (missing.Count > 0)
			{
				LokrPatchPlugin.Log.LogWarning(
					"Sanitize: unknown startingHeroes uniqueId(s) " + JoinIds(missing) + "; leaving run intact.");
			}
		}

		private static void LogUnknownQuestIds(SaveGame saveGame)
		{
			if (saveGame.run.mapSave == null)
			{
				return;
			}

			HashSet<string> questIds = new HashSet<string>();
			AddQuestIds(questIds, saveGame.run.mapSave.questsInAdventure);
			AddQuestIds(questIds, saveGame.run.mapSave.questsInGame);
			AddQuestIds(questIds, saveGame.run.mapSave.ephemeralQuests);
			if (saveGame.run.mapSave.questStatuses != null)
			{
				foreach (MapQuestStatus status in saveGame.run.mapSave.questStatuses)
				{
					if (status != null && !string.IsNullOrEmpty(status.questName))
					{
						questIds.Add(status.questName);
					}
				}
			}

			List<string> missing = new List<string>();
			foreach (string questId in questIds)
			{
				if (MetagameManager.instanceNoLoad.MapManager.GetMapQuestDefinition(questId) == null)
				{
					missing.Add(questId);
				}
			}

			if (missing.Count > 0)
			{
				LokrPatchPlugin.Log.LogWarning(
					"Sanitize: unknown map quest id(s) " + JoinIds(missing) + "; leaving run intact.");
			}
		}

		private static void LogUnknownItemArchetypes(SaveGame saveGame)
		{
			if (saveGame.run.inventoryData == null || saveGame.run.inventoryData.inventory == null)
			{
				return;
			}

			HashSet<string> allItems = new HashSet<string>(
				from itemDef in MetagameManager.instanceNoLoad.InventoryManager.GetAllItemDefinitions()
				select itemDef.id);
			List<string> missing = new List<string>();
			foreach (InventoryManager.InventoryItemSave item in saveGame.run.inventoryData.inventory)
			{
				if (item != null && !allItems.Contains(item.itemArchetype))
				{
					missing.Add(item.itemArchetype);
				}
			}

			if (missing.Count > 0)
			{
				LokrPatchPlugin.Log.LogWarning(
					"Sanitize: unknown inventory itemArchetype(s) " + JoinIds(missing) + "; leaving run intact.");
			}
		}

		private static List<string> KnownHeroPartyUniqueIds(SaveGame saveGame)
		{
			List<string> party = new List<string>();
			if (saveGame.run.heroesData == null || saveGame.run.heroesData.heroes == null)
			{
				return party;
			}

			foreach (HeroDefinition def in saveGame.run.heroesData.heroes)
			{
				if (def == null || !IsKnownArchetype(def.archetype))
				{
					continue;
				}

				UnitDefinition unitDefinition;
				if (UnityDefinitionsParser.instance.Definitions.TryGetValue(def.archetype, out unitDefinition)
					&& unitDefinition != null)
				{
					party.Add(unitDefinition.uniqueId);
				}
			}

			return party;
		}

		private static bool IsKnownArchetype(string archetype)
		{
			return !string.IsNullOrEmpty(archetype)
				&& UnityDefinitionsParser.instance.Definitions.ContainsKey(archetype);
		}

		private static void AppendHeroDefinitions(List<HeroDefinition> target, List<HeroDefinition> stowed)
		{
			if (target == null)
			{
				return;
			}

			HashSet<string> present = new HashSet<string>();
			foreach (HeroDefinition definition in target)
			{
				if (definition != null && !string.IsNullOrEmpty(definition.guid))
				{
					present.Add(definition.guid);
				}
			}

			foreach (HeroDefinition stowedDefinition in stowed)
			{
				if (stowedDefinition == null || present.Contains(stowedDefinition.guid))
				{
					continue;
				}

				target.Add(stowedDefinition.Clone());
			}
		}

		private static InventoryManager.InventoryItemSave CopyItemSave(InventoryManager.InventoryItemSave row)
		{
			return new InventoryManager.InventoryItemSave
			{
				id = row.id,
				itemArchetype = row.itemArchetype,
				quantity = row.quantity
			};
		}

		private static List<string> CopyStringList(List<string> source)
		{
			return source != null ? source.ToList() : new List<string>();
		}

		private static void AddQuestIds(HashSet<string> dest, List<string> source)
		{
			if (source == null)
			{
				return;
			}

			foreach (string id in source)
			{
				if (!string.IsNullOrEmpty(id))
				{
					dest.Add(id);
				}
			}
		}

		private static string JoinIds(List<string> ids)
		{
			return "'" + string.Join("', '", ids.ToArray()) + "'";
		}
	}
}
