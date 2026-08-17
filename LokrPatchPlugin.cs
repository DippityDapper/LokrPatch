using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace LokrPatch
{
	/// <summary>Base-game bug fixes and quality-of-life patches that are not tied to mod content loading.</summary>
	[BepInPlugin(Guid, Name, Version)]
	public class LokrPatchPlugin : BaseUnityPlugin
	{
		/// <summary>This plugin's BepInEx GUID.</summary>
		public const string Guid = "com.lokrmodding.patch";
		/// <summary>This plugin's display name.</summary>
		public const string Name = "LoKR Patch";
		/// <summary>This plugin's version string.</summary>
		public const string Version = "1.0.11";

		/// <summary>Shared log source for patch diagnostics.</summary>
		internal static ManualLogSource Log;

		private Harmony harmony;

		private void Awake()
		{
			Log = Logger;

			harmony = new Harmony(Guid);
			harmony.PatchAll();

			Log.LogInfo(string.Format(
				"{0} v{1} loaded — {2} method(s) patched.",
				Name, Version, harmony.GetPatchedMethods().Count()));
		}
	}
}
