using System;
using System.Collections.Generic;

namespace LokrPatch
{
	/// <summary>Unity-free skip/clamp/map rules used by LokrPatch prefixes and the xUnit suite.</summary>
	/// <remarks>
	/// Harmony prefixes read vanilla fields, call these helpers, then set <c>__result</c> or return
	/// false. Tests compile this file as linked source so they never load player UnityEngine.
	/// </remarks>
	internal static class PatchRules
	{
		/// <summary>KV expression key that vanilla maps to FunctionPointMult instead of FunctionPointMagnitude.</summary>
		internal const string PointMagnitudeKvKey = "pointMagnitude";

		/// <summary>Correct expression type name for <see cref="PointMagnitudeKvKey"/>.</summary>
		internal const string PointMagnitudeTypeName = "FunctionPointMagnitude";

		/// <summary>Correctly spelled RetreatIfWeakAI KV type key (vanilla only registers the Week typo).</summary>
		internal const string RetreatIfWeakCorrectKey = "RetreatIfWeakAI";

		/// <summary>Vanilla typo key that still must stay registered so shipped KV loads.</summary>
		internal const string RetreatIfWeakTypoKey = "RetreatIfWeekAI";

		/// <summary>Parse-list key that is an AIEvaluator, not an AbilityAction.</summary>
		internal const string PerAffectedAiKey = "PerAffectedAI";

		/// <summary>Fallback tooltip value when a named variable is missing (vanilla returns 999).</summary>
		internal const float MissingVariableFallback = 0f;

		/// <summary>True when an anyOf loot child should Process for this roll (float Range, not int 0..1).</summary>
		internal static bool LootChildFires(float chance, float roll)
		{
			return roll < chance;
		}

		/// <summary>True when a dialog graph has no child passing CheckCondition and must ExitDialog instead of First().</summary>
		internal static bool ShouldExitDialogWhenNoPassingChild(bool anyChildPassed)
		{
			return !anyChildPassed;
		}

		/// <summary>True when AbilityBehavior includes a standalone AOE token (pipe-separated flags).</summary>
		internal static bool HasAoeToken(string behavior)
		{
			if (string.IsNullOrEmpty(behavior))
			{
				return false;
			}

			string[] tokens = behavior.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < tokens.Length; i++)
			{
				if (tokens[i].Trim() == "AOE")
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>True when a parse-list child key must be skipped because it is not an AbilityAction.</summary>
		internal static bool IsSkippedActionKey(string key)
		{
			return key == PerAffectedAiKey;
		}

		/// <summary>True when a CallFunction helper's unit filter matched nobody and Execute must not index [0].</summary>
		internal static bool ShouldSkipEmptyUnitFilter(int matchCount)
		{
			return matchCount <= 0;
		}

		/// <summary>True when AIDecisionScoreEvaluator.Eval must return 0 instead of dividing by considerations.Count.</summary>
		internal static bool EmptyConsiderationsReturnsZero(int considerationCount)
		{
			return considerationCount <= 0;
		}

		/// <summary>Null-safe object.Equals for equal() so a missing LHS does not NRE.</summary>
		internal static bool NullSafeEquals(object a, object b)
		{
			return object.Equals(a, b);
		}

		/// <summary>True when EachInList ActionsIfEmpty should run (vanilla inverted this to Count &gt; 0).</summary>
		internal static bool ActionsIfEmptyShouldRun(int listCount)
		{
			return listCount == 0;
		}

		/// <summary>True when ResolveFloatVariable must return <see cref="MissingVariableFallback"/> instead of 999.</summary>
		internal static bool MissingVariableReturnsZero(object value)
		{
			return value == null;
		}

		/// <summary>True when POINT_TARGET left targetFilter null and the method must not dereference it.</summary>
		internal static bool ShouldSkipNullTargetFilter(bool filterIsNull)
		{
			return filterIsNull;
		}

		/// <summary>True when Stats.ApplyModifier must skip this additive/mult key because the live dictionary lacks it.</summary>
		internal static bool ShouldSkipMissingStat(bool statExists)
		{
			return !statExists;
		}

		/// <summary>Sanitize never DiscardRun for unknown ids; unknown rows are stowed instead.</summary>
		internal static bool SanitizeDiscardsRunOnUnknownIds
		{
			get { return false; }
		}

		/// <summary>Party load never replaces a non-3 known list with Gerald/Ranger/ArcaneMage.</summary>
		internal static bool ShouldResetPartyToVanillaTrio(int knownPartyCount)
		{
			return false;
		}

		/// <summary>Keeps uniqueIds that are in <paramref name="registered"/>, in save order.</summary>
		internal static List<string> FilterKnownIds(IEnumerable<string> ids, ICollection<string> registered)
		{
			List<string> kept = new List<string>();
			if (ids == null || registered == null)
			{
				return kept;
			}

			foreach (string id in ids)
			{
				if (!string.IsNullOrEmpty(id) && registered.Contains(id))
				{
					kept.Add(id);
				}
			}

			return kept;
		}

		/// <summary>True when a party uniqueId must not be written to the live roster or painted on a portrait.</summary>
		internal static bool ShouldOmitPartyId(string uniqueId)
		{
			return string.IsNullOrEmpty(uniqueId);
		}

		/// <summary>True when this party-portrait index is a persistent legend/companion frame (0–2), not the in-adventure fourth recruit slot.</summary>
		internal static bool IsCorePartySlot(int index)
		{
			return index >= 0 && index < 3;
		}

		/// <summary>Drops null and empty uniqueIds so vanilla party consumers never see holes.</summary>
		internal static List<string> CompactPartyIds(IList<string> slots)
		{
			List<string> kept = new List<string>();
			if (slots == null)
			{
				return kept;
			}

			foreach (string id in slots)
			{
				if (!string.IsNullOrEmpty(id))
				{
					kept.Add(id);
				}
			}

			return kept;
		}

		/// <summary>Splits a save party into slot-aligned known ids (holes for missing) and stowed unknown uniqueIds.</summary>
		/// <remarks>Empty entries become holes and are not stowed, so a bad OnDone cannot persist a null id.</remarks>
		internal static void SplitSaveParty(
			IEnumerable<string> saveParty,
			ICollection<string> registered,
			List<string> slotAligned,
			List<StowedPartyMember> stowed)
		{
			if (slotAligned != null)
			{
				slotAligned.Clear();
			}

			if (stowed != null)
			{
				stowed.Clear();
			}

			if (saveParty == null)
			{
				return;
			}

			int index = 0;
			foreach (string uniqueId in saveParty)
			{
				if (string.IsNullOrEmpty(uniqueId))
				{
					if (slotAligned != null)
					{
						slotAligned.Add(null);
					}
				}
				else if (registered != null && registered.Contains(uniqueId))
				{
					if (slotAligned != null)
					{
						slotAligned.Add(uniqueId);
					}
				}
				else
				{
					if (slotAligned != null)
					{
						slotAligned.Add(null);
					}

					if (stowed != null)
					{
						stowed.Add(new StowedPartyMember(index, uniqueId));
					}
				}

				index++;
			}
		}

		/// <summary>Places the first known legend in slot 0 and companions in slots 1 and 2, leaving holes instead of shifting.</summary>
		/// <remarks>Repairs a compacted save where a companion occupies the gold legend frame. Extra legends and companions append after slot 2.</remarks>
		internal static List<string> AlignPartySlotsByRole(IList<string> ids, ICollection<string> legendIds)
		{
			List<string> legends = new List<string>();
			List<string> companions = new List<string>();
			if (ids != null)
			{
				foreach (string id in ids)
				{
					if (string.IsNullOrEmpty(id))
					{
						continue;
					}

					if (legendIds != null && legendIds.Contains(id))
					{
						legends.Add(id);
					}
					else
					{
						companions.Add(id);
					}
				}
			}

			List<string> result = new List<string>();
			result.Add(legends.Count > 0 ? legends[0] : null);
			result.Add(companions.Count > 0 ? companions[0] : null);
			result.Add(companions.Count > 1 ? companions[1] : null);
			for (int i = 1; i < legends.Count; i++)
			{
				result.Add(legends[i]);
			}

			for (int i = 2; i < companions.Count; i++)
			{
				result.Add(companions[i]);
			}

			return result;
		}

		/// <summary>Writes stowed uniqueIds into empty slots (recorded index, else first hole) and appends when every slot is filled.</summary>
		internal static List<string> MergeStowedPartyIds(IList<string> slots, IList<StowedPartyMember> stowed)
		{
			List<string> result = new List<string>();
			if (slots != null)
			{
				foreach (string id in slots)
				{
					result.Add(id);
				}
			}

			if (stowed != null)
			{
				foreach (StowedPartyMember member in stowed)
				{
					if (string.IsNullOrEmpty(member.UniqueId) || ListContainsId(result, member.UniqueId))
					{
						continue;
					}

					int idx = member.Index;
					if (idx < 0)
					{
						idx = 0;
					}

					while (result.Count <= idx)
					{
						result.Add(null);
					}

					if (string.IsNullOrEmpty(result[idx]))
					{
						result[idx] = member.UniqueId;
					}
					else
					{
						int hole = IndexOfEmptySlot(result);
						if (hole >= 0)
						{
							result[hole] = member.UniqueId;
						}
						else
						{
							result.Add(member.UniqueId);
						}
					}
				}
			}

			return CompactPartyIds(result);
		}

		private static bool ListContainsId(List<string> ids, string uniqueId)
		{
			for (int i = 0; i < ids.Count; i++)
			{
				if (ids[i] == uniqueId)
				{
					return true;
				}
			}

			return false;
		}

		private static int IndexOfEmptySlot(List<string> ids)
		{
			for (int i = 0; i < ids.Count; i++)
			{
				if (string.IsNullOrEmpty(ids[i]))
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>True when InventoryManager.AddItem left ItemInstance.id empty and a GUID must be assigned.</summary>
		internal static bool ShouldAssignItemInstanceId(string existingId)
		{
			return string.IsNullOrEmpty(existingId);
		}

		/// <summary>True when Hero.UpdateSkills must skip this skill id because AbilitiesDefinitions has no entry.</summary>
		internal static bool IsUnknownSkill(bool abilityFound)
		{
			return !abilityFound;
		}

		/// <summary>True when a victory uniqueId is not in DefinitionsByUnique and must be dropped from the progress list.</summary>
		internal static bool ShouldDropUnknownUniqueId(bool isRegistered)
		{
			return !isRegistered;
		}

		/// <summary>True when a StartingHeroes uniqueId is unregistered and map start must skip spawning it.</summary>
		internal static bool ShouldSkipUnknownStartingHero(bool isRegistered)
		{
			return !isRegistered;
		}

		/// <summary>True when HeroModifiersAsset.GetConfigByKey returned null and the HUD must skip the icon.</summary>
		internal static bool ShouldSkipNullModifierConfig(bool configIsNull)
		{
			return configIsNull;
		}

		/// <summary>True when FightStarted must skip ActiveUnit because initiative is empty.</summary>
		internal static bool ShouldSkipNullActiveUnit(bool activeIsNull)
		{
			return activeIsNull;
		}

		/// <summary>True when ShowPage(index) is inside both titles and pages.</summary>
		internal static bool IsProgressionHelpIndexInRange(int index, int titlesCount, int pagesCount)
		{
			if (index < 0 || titlesCount <= 0 || pagesCount <= 0)
			{
				return false;
			}

			int n = Math.Min(titlesCount, pagesCount);
			return index < n;
		}

		/// <summary>True when Next would walk off titles or pages and must call Finished instead of ShowPage.</summary>
		internal static bool ProgressionHelpNextShouldFinish(int currentPageIndex, int titlesCount, int pagesCount)
		{
			int next = currentPageIndex + 1;
			return next >= pagesCount || next >= titlesCount;
		}

		/// <summary>Writes the pointMagnitude mapping onto an expression-function dictionary without loading Unity types.</summary>
		internal static void ApplyPointMagnitudeMapping(IDictionary<string, string> expressionFunctions)
		{
			if (expressionFunctions == null)
			{
				return;
			}

			expressionFunctions[PointMagnitudeKvKey] = PointMagnitudeTypeName;
		}
	}

	/// <summary>An unregistered party uniqueId together with the save index it occupied.</summary>
	internal readonly struct StowedPartyMember
	{
		/// <summary>Zero-based index in the save party list.</summary>
		internal int Index { get; }

		/// <summary>The uniqueId that was not in DefinitionsByUnique this load.</summary>
		internal string UniqueId { get; }

		/// <summary>Records <paramref name="index"/> and <paramref name="uniqueId"/> for Save splice.</summary>
		internal StowedPartyMember(int index, string uniqueId)
		{
			Index = index;
			UniqueId = uniqueId;
		}
	}
}
