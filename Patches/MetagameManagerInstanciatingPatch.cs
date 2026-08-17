using System;
using HarmonyLib;
using Ironhide.Legends.Model.Metagame;

namespace LokrPatch.Patches
{
	/// <summary>Clears <c>instanciating</c> when the metagame constructor throws.</summary>
	/// <remarks>
	/// Vanilla sets the flag true, then constructs. A throw (duplicate hero roster id) leaves
	/// the flag true forever, so later Stage/quest lookup fails with "already being instanciated"
	/// instead of retrying.
	/// </remarks>
	[HarmonyPatch(typeof(MetagameManager), MethodType.Constructor)]
	internal static class MetagameManagerInstanciatingPatch
	{
		/// <summary>Resets the vanilla instantiating flag, then rethrows.</summary>
		private static Exception Finalizer(Exception __exception)
		{
			if (__exception == null)
			{
				return null;
			}

			Traverse.Create(typeof(MetagameManager)).Field("instanciating").SetValue(false);
			LokrPatchPlugin.Log.LogWarning(
				"MetagameManager constructor failed; cleared instantiating flag so later access can retry. "
				+ __exception.Message);
			return __exception;
		}
	}
}
