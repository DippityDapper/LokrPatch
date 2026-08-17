using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Ironhide.Legends.Model.Common.Parsers;
using Ironhide.Legends.Model.Game.Units.Abilities;
using Ironhide.Legends.Utils;

namespace LokrPatch.Patches
{
	/// <summary>Runs <c>EachInList</c> <c>ActionsIfEmpty</c> only when the list count is 0.</summary>
	/// <remarks>
	/// Vanilla inverts the parse-key name: <c>ActionsIfEmpty</c> fires when <c>list.Count &gt; 0</c>.
	/// Shipped KV omits the key, so <c>list3</c> is null and the branch never runs either way.
	/// If <c>List</c> is null, this prefix lets vanilla throw.
	/// </remarks>
	[HarmonyPatch(typeof(EachInListAction), nameof(EachInListAction.Execute))]
	internal static class EachInListActionsIfEmptyPatch
	{
		private static bool Prefix(EachInListAction __instance, AbilityContext context)
		{
			if (__instance == null)
			{
				return true;
			}

			IDictionary<string, IExpression> attributes = Traverse.Create(__instance)
				.Field<IDictionary<string, IExpression>>("attributes").Value;
			if (attributes == null || !attributes.ContainsKey("List") || attributes["List"] == null)
			{
				return true;
			}

			object rawList = attributes["List"].GetObject(context);
			if (rawList == null)
			{
				return true;
			}

			IList list = (IList)rawList;
			string id = (string)attributes.GetObject("IteratorName", context, null);
			List<AbilityAction> actions = (List<AbilityAction>)attributes.GetObject("Actions", context, null);
			List<AbilityAction> actionsIfEmpty = (List<AbilityAction>)attributes.GetObject("ActionsIfEmpty", context, null);
			string indexName = (string)attributes.GetObject("IteratorIndexName", context, null);
			if (actions != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					object value = list[i];
					AbilityContext abilityContext = new AbilityContext();
					abilityContext.SetObject(id, value);
					if (indexName != null)
					{
						abilityContext.SetInt(indexName, i);
					}

					abilityContext.ReadOnly = true;
					abilityContext.Parent = context;
					foreach (AbilityAction abilityAction in actions)
					{
						abilityAction.Execute(abilityContext);
					}
				}
			}

			if (actionsIfEmpty != null && PatchRules.ActionsIfEmptyShouldRun(list.Count))
			{
				foreach (AbilityAction abilityAction in actionsIfEmpty)
				{
					abilityAction.Execute(context);
				}
			}

			return false;
		}
	}
}
