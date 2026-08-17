using System;
using HarmonyLib;
using UnityEngine;

namespace LokrPatch.Patches
{
	/// <summary>Suppresses known-harmless Unity log spam from modded content and base-game load noise.</summary>
	/// <remarks>
	/// Direct Harmony patches on generic game methods (e.g. AssetBundleManager.LoadAsset&lt;T&gt;) fail under
	/// the game's MonoMod stack; filtering Debug.* call sites is the supported approach here.
	/// </remarks>
	internal static class SuppressedUnityLogMessage
	{
		internal static bool ShouldSuppressMessage(string message)
		{
			if (string.IsNullOrEmpty(message))
			{
				return false;
			}

			if (message.IndexOf("Skipped Action:", StringComparison.Ordinal) >= 0)
			{
				return true;
			}

			if (message.StartsWith("UNITDEFINITION: Migrating to skillProgression unit:", StringComparison.Ordinal)
				|| message.StartsWith("UNITDEFINITION: Disabling lvl4 unit:", StringComparison.Ordinal))
			{
				return true;
			}

			if (message.IndexOf("MasterAudio:", StringComparison.Ordinal) >= 0
				&& message.IndexOf("were busy", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}

			if (message.StartsWith("AssetBundleManager - Could't load asset", StringComparison.Ordinal))
			{
				return true;
			}

			if (message.IndexOf("UnloadAsset can only be used on assets", StringComparison.Ordinal) >= 0)
			{
				return true;
			}

			if (message.IndexOf("Lean.Touch.LeanTouch", StringComparison.Ordinal) >= 0)
			{
				return true;
			}

			if (message.StartsWith("NullReferenceException: Object reference not set to an instance of an object", StringComparison.Ordinal)
				&& message.IndexOf("Lean.Touch.LeanTouch", StringComparison.Ordinal) >= 0)
			{
				return true;
			}

			return false;
		}

		internal static bool ShouldSuppressLogFormat(string format, object[] args)
		{
			if (format == null)
			{
				return false;
			}

			if (format.IndexOf("Skipped Action:", StringComparison.Ordinal) >= 0)
			{
				return true;
			}

			if (!format.Contains("Could't load asset"))
			{
				return false;
			}

			return args != null && args.Length > 0 && "AssetBundleManager".Equals(args[0]);
		}

		internal static bool ShouldSuppressException(Exception exception)
		{
			if (exception == null)
			{
				return false;
			}

			string text = exception.ToString();
			return text.IndexOf("Lean.Touch.LeanTouch", StringComparison.Ordinal) >= 0;
		}
	}

	[HarmonyPatch(typeof(Debug), nameof(Debug.Log), typeof(string))]
	internal static class SuppressDebugLogString
	{
		[HarmonyPrefix]
		private static bool Prefix(string message) => !SuppressedUnityLogMessage.ShouldSuppressMessage(message);
	}

	[HarmonyPatch(typeof(Debug), nameof(Debug.Log), typeof(object))]
	internal static class SuppressDebugLogObject
	{
		[HarmonyPrefix]
		private static bool Prefix(object message) =>
			!SuppressedUnityLogMessage.ShouldSuppressMessage(message?.ToString());
	}

	[HarmonyPatch(typeof(Debug), nameof(Debug.LogFormat), typeof(string), typeof(object[]))]
	internal static class SuppressDebugLogFormat
	{
		[HarmonyPrefix]
		private static bool Prefix(string format, object[] args) =>
			!SuppressedUnityLogMessage.ShouldSuppressLogFormat(format, args);
	}

	[HarmonyPatch(typeof(Debug), nameof(Debug.LogWarning), typeof(string))]
	internal static class SuppressDebugLogWarningString
	{
		[HarmonyPrefix]
		private static bool Prefix(string message) => !SuppressedUnityLogMessage.ShouldSuppressMessage(message);
	}

	[HarmonyPatch(typeof(Debug), nameof(Debug.LogWarning), typeof(object))]
	internal static class SuppressDebugLogWarningObject
	{
		[HarmonyPrefix]
		private static bool Prefix(object message) =>
			!SuppressedUnityLogMessage.ShouldSuppressMessage(message?.ToString());
	}

	[HarmonyPatch(typeof(Debug), nameof(Debug.LogError), typeof(string))]
	internal static class SuppressDebugLogErrorString
	{
		[HarmonyPrefix]
		private static bool Prefix(string message) => !SuppressedUnityLogMessage.ShouldSuppressMessage(message);
	}

	[HarmonyPatch(typeof(Debug), nameof(Debug.LogError), typeof(object))]
	internal static class SuppressDebugLogErrorObject
	{
		[HarmonyPrefix]
		private static bool Prefix(object message) =>
			!SuppressedUnityLogMessage.ShouldSuppressMessage(message?.ToString());
	}

	[HarmonyPatch(typeof(Debug), nameof(Debug.LogErrorFormat), typeof(string), typeof(object[]))]
	internal static class SuppressDebugLogErrorFormat
	{
		[HarmonyPrefix]
		private static bool Prefix(string format, object[] args) =>
			!SuppressedUnityLogMessage.ShouldSuppressLogFormat(format, args);
	}

	[HarmonyPatch(typeof(Debug), nameof(Debug.LogException), typeof(Exception))]
	internal static class SuppressDebugLogException
	{
		[HarmonyPrefix]
		private static bool Prefix(Exception exception) =>
			!SuppressedUnityLogMessage.ShouldSuppressException(exception);
	}

	[HarmonyPatch(typeof(Debug), nameof(Debug.LogException), typeof(Exception), typeof(UnityEngine.Object))]
	internal static class SuppressDebugLogExceptionContext
	{
		[HarmonyPrefix]
		private static bool Prefix(Exception exception) =>
			!SuppressedUnityLogMessage.ShouldSuppressException(exception);
	}
}
