using System.Collections.Generic;
using Ironhide.Legends.Model.Metagame.Heroes;

namespace LokrPatch
{
	/// <summary>Removes duplicate skill ids on a hero save without dropping legitimate entries.</summary>
	internal static class HeroSkillSanitizer
	{
		/// <summary>Dedupes <see cref="HeroDefinition.skills"/> in first-seen order.</summary>
		internal static void Sanitize(Hero hero)
		{
			if (hero?.heroDefinition?.skills == null)
			{
				return;
			}

			List<string> deduped = new List<string>();
			HashSet<string> seen = new HashSet<string>();
			foreach (string skillId in hero.heroDefinition.skills)
			{
				if (string.IsNullOrEmpty(skillId))
				{
					continue;
				}
				if (!seen.Add(skillId))
				{
					continue;
				}
				deduped.Add(skillId);
			}
			hero.heroDefinition.skills = deduped;
		}

		/// <summary>Re-adds base trait ids from the archetype when an older sanitizer stripped them from the save.</summary>
		internal static void RepairMissingBaseSkills(Hero hero)
		{
			if (hero?.heroDefinition?.skills == null || hero.unitDefinition?.skills == null)
			{
				return;
			}

			HashSet<string> present = new HashSet<string>(hero.heroDefinition.skills);
			foreach (string skillId in hero.unitDefinition.skills)
			{
				if (!string.IsNullOrEmpty(skillId) && present.Add(skillId))
				{
					hero.heroDefinition.skills.Add(skillId);
				}
			}
		}
	}
}
