using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Ironhide.Legends.Model.Game.Units;
using Ironhide.Legends.Model.Metagame;
using Ironhide.Legends.Model.Metagame.Heroes;
using Ironhide.Legends.Utils;
using Ironhide.Legends.View.Map;

namespace LokrPatch.Patches
{
	/// <summary>Skips unknown <c>StartingHeroes</c> uniqueIds on map start instead of null-dereferencing <c>.id</c>.</summary>
	/// <remarks>
	/// This is the empty-party new-run path, not save load. Unknown ids stay on
	/// <c>HeroManager.startingHeroes</c> so a reinstalled pack can spawn them later; the live party
	/// is never rewritten to Gerald/Ranger/ArcaneMage.
	/// </remarks>
	[HarmonyPatch(typeof(NewMapManagerComponent), "Start")]
	internal static class MapStartStartingHeroesPatch
	{
		private static bool attemptedStartingHeroFallback;

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo ourAdd = AccessTools.Method(typeof(MapStartStartingHeroesPatch), nameof(TryAddStartingHero));
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			for (int i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode != OpCodes.Ldftn)
				{
					continue;
				}

				MethodBase target = codes[i].operand as MethodBase;
				if (target == null || target.GetParameters().Length != 1
					|| target.GetParameters()[0].ParameterType != typeof(string))
				{
					continue;
				}

				if (i > 0 && (codes[i - 1].opcode == OpCodes.Ldsfld || codes[i - 1].opcode == OpCodes.Ldarg_0))
				{
					codes[i - 1] = new CodeInstruction(OpCodes.Ldnull);
				}

				codes[i].operand = ourAdd;
			}

			return codes;
		}

		private static void Postfix()
		{
			if (!attemptedStartingHeroFallback)
			{
				return;
			}

			attemptedStartingHeroFallback = false;
			if (MetagameManager.instance == null || MetagameManager.instance.HeroManager == null)
			{
				return;
			}

			List<Hero> heroes = MetagameManager.instance.HeroManager.GetAllHeroes();
			if (heroes != null && !heroes.IsEmpty())
			{
				return;
			}

			LokrPatchPlugin.Log.LogWarning(
				"NewMapManagerComponent.Start: no spawnable StartingHeroes uniqueIds; live party left empty.");
		}

		/// <summary>Adds a starting hero when the uniqueId is registered; logs and skips otherwise.</summary>
		public static void TryAddStartingHero(string uniqueId)
		{
			attemptedStartingHeroFallback = true;
			UnitDefinition definition = UnityDefinitionsParser.instance.DefinitionsByUnique.GetValueOrDefault(uniqueId, null);
			if (PatchRules.ShouldSkipUnknownStartingHero(definition != null))
			{
				LokrPatchPlugin.Log.LogWarning(
					"NewMapManagerComponent.Start: skipped unknown StartingHeroes uniqueId '" + uniqueId + "'.");
				return;
			}

			MetagameManager.instance.HeroManager.AddHero(definition.id);
		}
	}
}
