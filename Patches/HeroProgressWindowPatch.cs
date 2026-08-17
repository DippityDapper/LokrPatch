using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Ironhide.Legends.Model.Game.Units;
using Ironhide.Legends.Model.Game.Units.Abilities;
using Ironhide.Legends.View.Hud.VictoryWindow.HeroProgress;

namespace LokrPatch.Patches
{
	/// <summary>Skips victory XP bars and unlock anims when a uniqueId or unlock ability is missing.</summary>
	/// <remarks>
	/// Vanilla logs <c>Could not find unit with uniqueId</c> then reads <c>metaExo</c> on the null
	/// definition. Mutating the incoming list keeps <c>progressData</c> aligned with <c>heroBars</c>.
	/// The anim transpiler must still set <c>gainedLevel</c> so the XP loop does not spin.
	/// </remarks>
	internal static class HeroProgressWindowPatch
	{
		[HarmonyPatch(typeof(HeroProgressWindow), nameof(HeroProgressWindow.ShowHeroProgress))]
		private static class ShowHeroProgressPatch
		{
			private static bool Prefix(List<HeroProgressWindow.HeroProgressInfo> data)
			{
				if (data == null || UnityDefinitionsParser.instance == null
					|| UnityDefinitionsParser.instance.DefinitionsByUnique == null)
				{
					return true;
				}

				data.RemoveAll(delegate(HeroProgressWindow.HeroProgressInfo info)
				{
					if (info == null || !PatchRules.ShouldDropUnknownUniqueId(
						UnityDefinitionsParser.instance.DefinitionsByUnique.ContainsKey(info.id)))
					{
						return false;
					}

					LokrPatchPlugin.Log.LogWarning(
						"HeroProgressWindow.ShowHeroProgress: skipped unknown uniqueId '" + info.id + "'.");
					return true;
				});
				return true;
			}
		}

		[HarmonyPatch]
		private static class ShowHeroProgressAnimPatch
		{
			private static MethodBase TargetMethod()
			{
				return AccessTools.EnumeratorMoveNext(
					AccessTools.Method(typeof(HeroProgressWindow), nameof(HeroProgressWindow.ShowHeroProgressAnim)));
			}

			private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				MethodInfo iconGet = AccessTools.PropertyGetter(typeof(Ability), nameof(Ability.Icon));
				MethodInfo safeIcon = AccessTools.Method(typeof(HeroProgressWindowPatch), nameof(SafeAbilityIcon));
				foreach (CodeInstruction instruction in instructions)
				{
					if (instruction.Calls(iconGet))
					{
						yield return new CodeInstruction(OpCodes.Call, safeIcon);
						continue;
					}

					yield return instruction;
				}
			}
		}

		[HarmonyPatch(typeof(HeroProgressWindow), "ShowUnlockSkillAnimation")]
		private static class ShowUnlockSkillAnimationPatch
		{
			private static bool Prefix(string icon, ref IEnumerator __result)
			{
				if (!string.IsNullOrEmpty(icon))
				{
					return true;
				}

				__result = EmptyCoroutine();
				return false;
			}
		}

		/// <summary>Returns the ability icon, or null when the unlock ability was not registered.</summary>
		public static string SafeAbilityIcon(Ability ability)
		{
			if (ability == null)
			{
				LokrPatchPlugin.Log.LogWarning(
					"HeroProgressWindow.ShowHeroProgressAnim: skipped unlock skill animation (ability missing).");
				return null;
			}

			return ability.Icon;
		}

		private static IEnumerator EmptyCoroutine()
		{
			yield break;
		}
	}
}
