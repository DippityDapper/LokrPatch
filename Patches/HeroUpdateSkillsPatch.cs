using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Ironhide.Legends.Model.Game.Units.Abilities;
using Ironhide.Legends.Model.Metagame;
using Ironhide.Legends.Model.Metagame.Heroes;
using Ironhide.Legends.Utils;
using UnityEngine;

namespace LokrPatch.Patches
{
	/// <summary>Skips unknown skill ids in <see cref="Hero.UpdateSkills"/> and clamps the unused level indexers.</summary>
	/// <remarks>
	/// Vanilla logs <c>HERO: Can't find skill</c> then reads <c>AbilityBehavior</c> on the null entry.
	/// Unknown ids stay on <c>heroDefinition.skills</c> so the save is not rewritten. A missing
	/// <c>skillProgression</c> key still throws as vanilla — that is a different bug.
	/// </remarks>
	[HarmonyPatch(typeof(Hero), nameof(Hero.UpdateSkills))]
	internal static class HeroUpdateSkillsPatch
	{
		private static bool Prefix(Hero __instance)
		{
			if (__instance.heroDefinition.skills == null)
			{
				__instance.heroDefinition.skills = new List<string>();
			}

			foreach (string item in __instance.unitDefinition.skills)
			{
				if (!__instance.heroDefinition.skills.Contains(item))
				{
					__instance.heroDefinition.skills.Add(item);
				}
			}

			if (__instance.unitDefinition.skillProgression != null)
			{
				int num = Mathf.RoundToInt(__instance.stats.GetStatValue("level"));
				int[] array = new int[]
				{
					1,
					2,
					3
				};
				_ = array[Mathf.Clamp(num, 1, 3) - 1];
				int count = __instance.heroDefinition.skills.Where(delegate(string s)
				{
					return IsNonPassiveSkill(__instance, s);
				}).ToList().Count;
				int num2 = array.ToList().IndexOf(count) + 1;
				int skillIndex2;
				int skillIndex;
				for (skillIndex = num2 + 1; skillIndex <= num; skillIndex = skillIndex2 + 1)
				{
					List<string> valueOrDefault = __instance.unitDefinition.skillProgression.GetValueOrDefault(skillIndex, null);
					if (valueOrDefault == null)
					{
						throw new Exception(string.Format("Hero: could not find skill options for hero level {0}", skillIndex));
					}

					string item2 = valueOrDefault.Where((string s, int i) =>
						!MetagameManager.instance.Player.HeroRosterManager.IsSkillVariantLockedInRun(
							__instance.unitDefinition.uniqueId, skillIndex, i)).Random();
					__instance.heroDefinition.skills.Add(item2);
					skillIndex2 = skillIndex;
				}

				return false;
			}

			if (__instance.unitDefinition.skillPool.IsEmpty())
			{
				__instance.heroDefinition.skills = __instance.unitDefinition.skills.ToList();
				return false;
			}

			int num3 = Mathf.RoundToInt(__instance.stats.GetStatValue("level"));
			int clampedPoolLevel = Mathf.Clamp(num3, 1, 4);
			int num4 = new int[]
			{
				0,
				1,
				2,
				3
			}[clampedPoolLevel - 1];
			List<string> currentSkillsActive = __instance.heroDefinition.skills.Where(delegate(string s)
			{
				return IsNonPassiveSkill(__instance, s);
			}).ToList();
			List<string> source = (from s in __instance.unitDefinition.skillPool
				where !currentSkillsActive.Contains(s)
				select s).ToList();
			__instance.heroDefinition.skills.AddRange(source.Take(num4 - currentSkillsActive.Count));
			return false;
		}

		private static bool IsNonPassiveSkill(Hero hero, string skillId)
		{
			Ability valueOrDefault2 = AbilitiesDefinitions.instance.abilities.GetValueOrDefault(skillId, null);
			if (PatchRules.IsUnknownSkill(valueOrDefault2 != null))
			{
				Debug.LogErrorFormat("HERO: Can't find skill: {0} in hero: {1}", new object[]
				{
					skillId,
					hero.Name
				});
				LokrPatchPlugin.Log.LogWarning(
					"Hero.UpdateSkills: skipped unknown skill '" + skillId + "' on hero '" + hero.Name + "'.");
				return false;
			}

			return !valueOrDefault2.AbilityBehavior.HasFlag(AbilityBehavior.PASSIVE);
		}
	}
}
