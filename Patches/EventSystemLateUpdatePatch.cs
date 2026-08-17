using HarmonyLib;
using Ironhide.Legends.View.Metagame.Screens.HeroRoom;
using UnityEngine.EventSystems;

namespace LokrPatch.Patches
{
	/// <summary>Skips vanilla LateUpdate input when EventSystem has no input module.</summary>
	/// <remarks>
	/// UIHeroRoom and TooltipManager both do EventSystem.current.currentInputModule.input
	/// with no null check. After Lab close, SRDebugger may create a bare EventSystem, or
	/// LabEventSystem is already destroyed, so that chain NREs every frame while the hero
	/// room is open. Same class of miss as LeanTouchUpdatePatch.
	/// </remarks>
	internal static class EventSystemLateUpdatePatch
	{
		/// <summary>True when the current EventSystem can answer mouse/button queries.</summary>
		internal static bool HasInput()
		{
			EventSystem system = EventSystem.current;
			return system != null
				&& system.currentInputModule != null
				&& system.currentInputModule.input != null;
		}

		/// <summary>Harmony target for UIHeroRoom.LateUpdate.</summary>
		[HarmonyPatch(typeof(UIHeroRoom), "LateUpdate")]
		private static class UIHeroRoomLateUpdatePatch
		{
			/// <summary>Skips UIHeroRoom.LateUpdate when input is unavailable.</summary>
			private static bool Prefix()
			{
				return HasInput();
			}
		}

		/// <summary>Harmony target for TooltipManager.LateUpdate.</summary>
		[HarmonyPatch(typeof(TooltipManager), "LateUpdate")]
		private static class TooltipManagerLateUpdatePatch
		{
			/// <summary>Skips TooltipManager.LateUpdate when input is unavailable.</summary>
			private static bool Prefix()
			{
				return HasInput();
			}
		}
	}
}
