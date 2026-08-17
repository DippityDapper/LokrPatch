using HarmonyLib;
using Ironhide.Legends.Model.Game.Units.Abilities;
using KVLib;

namespace LokrPatch.Patches
{
	/// <summary>Injects default <c>0</c> for missing AOE center/affects keys before <see cref="AbilityParser.ParseAbility"/>.</summary>
	/// <remarks>
	/// Vanilla NullRefs on <c>kv["AbilityAOECenterOnCaster"].GetFloat()</c> and <c>AbilityAOEAffectsCaster</c>
	/// whenever <c>AbilityBehavior</c> includes <c>AOE</c> and the key is absent; the method catch then drops the skill.
	/// Default <c>0</c> matches missing <c>AbilityAOEMinRange</c> and C# bool false.
	/// </remarks>
	[HarmonyPatch(typeof(AbilityParser), nameof(AbilityParser.ParseAbility))]
	internal static class ParseAbilityAoeKeysPatch
	{
		private static bool Prefix(KeyValue kv)
		{
			if (kv == null)
			{
				return true;
			}

			KeyValue behaviorKv = kv["AbilityBehavior"];
			if (behaviorKv == null)
			{
				return true;
			}

			string behavior = behaviorKv.GetString();
			if (string.IsNullOrEmpty(behavior) || !PatchRules.HasAoeToken(behavior))
			{
				return true;
			}

			EnsureFloatChild(kv, "AbilityAOECenterOnCaster");
			EnsureFloatChild(kv, "AbilityAOEAffectsCaster");
			return true;
		}

		private static void EnsureFloatChild(KeyValue kv, string key)
		{
			if (kv[key] != null)
			{
				return;
			}

			kv.AddChild(new KeyValue(key).Set(0f));
		}
	}
}
