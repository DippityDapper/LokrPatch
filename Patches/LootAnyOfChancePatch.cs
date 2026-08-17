using HarmonyLib;
using Ironhide.Legends.Model.Metagame.Map.Loot;

namespace LokrPatch.Patches
{
	/// <summary>Rolls <see cref="LootItemGeneratorAnyOf"/> child chance with Unity's float <c>Random.Range</c>.</summary>
	/// <remarks>
	/// Vanilla casts <c>Random.Range(0, 1)</c> — the int overload excludes max, so the roll is always 0
	/// and every child with <c>chance &gt; 0</c> always processes. The float overload is what the comparison
	/// was written for.
	/// </remarks>
	[HarmonyPatch(typeof(LootItemGeneratorAnyOf), "AddItems", typeof(LootTable.LootTableResult))]
	internal static class LootAnyOfChancePatch
	{
		private static bool Prefix(LootItemGeneratorAnyOf __instance, LootTable.LootTableResult loot)
		{
			if (__instance.generators == null)
			{
				LokrPatchPlugin.Log.LogWarning(
					"LootItemGeneratorAnyOf.AddItems: generators is null; skipping.");
				return false;
			}

			foreach (LootItemGeneratorAnyOf.Data data in __instance.generators)
			{
				if (PatchRules.LootChildFires(data.chance, UnityEngine.Random.Range(0f, 1f)))
				{
					data.generator.Process(loot);
				}
			}

			return false;
		}
	}
}
