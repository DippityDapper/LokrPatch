using System;
using HarmonyLib;
using Lean.Touch;

namespace LokrPatch.Patches
{
	/// <summary>Swallows LeanTouch NREs when the game's input stack is in a bad state (e.g. overlay mod menus).</summary>
	[HarmonyPatch(typeof(LeanTouch), "Update")]
	internal static class LeanTouchUpdateSuppressNullRef
	{
		[HarmonyFinalizer]
		private static Exception Finalizer(Exception __exception)
		{
			if (__exception is NullReferenceException || __exception is InvalidOperationException)
			{
				return null;
			}

			return __exception;
		}
	}
}
