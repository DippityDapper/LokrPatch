using System;
using HarmonyLib;
using Ironhide.Legends.Model.Metagame.Inventory;

namespace LokrPatch.Patches
{
	/// <summary>Assigns a GUID to <see cref="ItemInstance.id"/> when <see cref="InventoryManager.AddItem"/> left it null.</summary>
	/// <remarks>
	/// Vanilla only copies <c>id</c> on Load; mid-run grants serialize a null instance id. This postfix
	/// fills a GUID only when the live row is empty — it does not walk inventory on load or remap
	/// ids that already came from the save.
	/// </remarks>
	[HarmonyPatch(typeof(InventoryManager), nameof(InventoryManager.AddItem))]
	internal static class InventoryAddItemIdPatch
	{
		private static void Postfix(InventoryManager __instance, string itemDefinitionId)
		{
			ItemInstance instance = __instance.GetItem(itemDefinitionId);
			if (instance == null || !PatchRules.ShouldAssignItemInstanceId(instance.id))
			{
				return;
			}

			instance.id = Guid.NewGuid().ToString();
		}
	}
}
