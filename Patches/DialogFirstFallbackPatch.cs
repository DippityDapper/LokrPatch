using System.Linq;
using System.Reflection;
using HarmonyLib;
using Ironhide.Legends.Model.Metagame.Dialogs;

namespace LokrPatch.Patches
{
	/// <summary>Ends a dialog when Start / HandleReply / HandleContinue have no passing child, instead of throwing.</summary>
	/// <remarks>
	/// Vanilla picks the next node with LINQ <c>First</c> and no fallback. A graph where every child
	/// fails <c>CheckCondition</c> (or a continue with no Dialog-typed child) throws
	/// <c>InvalidOperationException</c> mid-conversation. <c>FirstOrDefault</c> plus private
	/// <c>ExitDialog</c> closes the graph the same way a normal exit node would.
	/// </remarks>
	internal static class DialogFirstFallbackPatch
	{
		private static readonly MethodInfo ExitDialogMethod =
			AccessTools.Method(typeof(Dialog), "ExitDialog");

		private static readonly MethodInfo MoveToNodeMethod =
			AccessTools.Method(typeof(Dialog), "MoveToNode", new[] { typeof(DialogNode) });

		private static readonly MethodInfo SetExitMethod =
			AccessTools.PropertySetter(typeof(Dialog), nameof(Dialog.Exit));

		[HarmonyPatch(typeof(Dialog), nameof(Dialog.Start))]
		private static class StartPatch
		{
			private static bool Prefix(Dialog __instance)
			{
				SetExitMethod.Invoke(__instance, new object[] { false });
				__instance.flags.Clear();
				__instance.data.Clear();
				__instance.rootNode.PrepareChildren(__instance);
				if (__instance.startAction != null)
				{
					__instance.startAction(__instance);
				}

				DialogNode node = __instance.rootNode.children.FirstOrDefault(
					child => child.CheckCondition(__instance));
				if (PatchRules.ShouldExitDialogWhenNoPassingChild(node != null))
				{
					ExitInsteadOfThrow(__instance, "Start");
					return false;
				}

				MoveToNodeMethod.Invoke(__instance, new object[] { node });
				return false;
			}
		}

		[HarmonyPatch(typeof(Dialog), nameof(Dialog.HandleReply))]
		private static class HandleReplyPatch
		{
			private static bool Prefix(Dialog __instance, int index)
			{
				__instance.currentNode.After(__instance);
				DialogNode dialogNode = __instance.responses[index];
				dialogNode.DoAction(__instance);
				dialogNode.After(__instance);
				if (dialogNode.exit)
				{
					ExitDialogMethod.Invoke(__instance, null);
					return false;
				}

				dialogNode.PrepareChildren(__instance);
				DialogNode node = dialogNode.children.FirstOrDefault(
					child => child.CheckCondition(__instance));
				if (PatchRules.ShouldExitDialogWhenNoPassingChild(node != null))
				{
					ExitInsteadOfThrow(__instance, "HandleReply");
					return false;
				}

				MoveToNodeMethod.Invoke(__instance, new object[] { node });
				return false;
			}
		}

		[HarmonyPatch(typeof(Dialog), nameof(Dialog.HandleContinue))]
		private static class HandleContinuePatch
		{
			private static bool Prefix(Dialog __instance)
			{
				__instance.currentNode.After(__instance);
				if (__instance.currentNode.exit)
				{
					ExitDialogMethod.Invoke(__instance, null);
					return false;
				}

				__instance.currentNode.PrepareChildren(__instance);
				DialogNode node = __instance.currentNode.children.FirstOrDefault(
					child => child.type == DialogNode.DialogNodeType.Dialog && child.CheckCondition(__instance));
				if (PatchRules.ShouldExitDialogWhenNoPassingChild(node != null))
				{
					ExitInsteadOfThrow(__instance, "HandleContinue");
					return false;
				}

				MoveToNodeMethod.Invoke(__instance, new object[] { node });
				return false;
			}
		}

		private static void ExitInsteadOfThrow(Dialog dialog, string where)
		{
			string nodeId = dialog.currentNode != null ? dialog.currentNode.id : null;
			LokrPatchPlugin.Log.LogWarning(string.Format(
				"Dialog '{0}' node '{1}': {2} had no child passing CheckCondition; exiting instead of throwing.",
				dialog.id,
				nodeId,
				where));
			ExitDialogMethod.Invoke(dialog, null);
		}
	}
}
