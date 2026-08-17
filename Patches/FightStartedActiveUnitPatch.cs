using HarmonyLib;
using Ironhide.Legends;
using Ironhide.Legends.Controller.Game;
using Ironhide.Legends.Model.Game;
using Ironhide.Legends.Model.Game.Units;
using Ironhide.Legends.Model.Metagame;
using Ironhide.Legends.View.Utils.Random;
using UnityEngine;

namespace LokrPatch.Patches
{
	/// <summary>Skips <c>ActiveUnit</c> calls when initiative is empty so <c>FightStartedEvent</c> listeners still run.</summary>
	/// <remarks>
	/// Vanilla <see cref="NewInitiativeHandler.FightStartedHandler"/> and <see cref="Stage.StartFight"/>
	/// dereference <c>ActiveUnit</c> with no null check. An empty initiative list NREs inside
	/// <c>Events.Raise</c> (multicast, no per-listener try/catch), which aborts later
	/// <c>FightStartedEvent</c> listeners. Guarding the call lets those listeners finish; this
	/// patch does not change when units are spawned.
	/// </remarks>
	internal static class FightStartedActiveUnitPatch
	{
		private static readonly AccessTools.FieldRef<NewInitiativeHandler, bool> FightStarted =
			AccessTools.FieldRefAccess<NewInitiativeHandler, bool>("fightStarted");

		private static readonly AccessTools.FieldRef<Stage, GameObject> DustParticlesPrefab =
			AccessTools.FieldRefAccess<Stage, GameObject>("dustParticlesPrefab");

		[HarmonyPatch(typeof(NewInitiativeHandler), nameof(NewInitiativeHandler.FightStartedHandler))]
		private static class FightStartedHandlerPatch
		{
			private static bool Prefix(NewInitiativeHandler __instance)
			{
				if (!__instance.testSkipRandomize)
				{
					RandomProvider randomProvider = MetagameManager.instance.EncounterManager.Random;
					__instance.units.ForEach(delegate(Unit unit)
					{
						unit.CalculateInitiative(randomProvider);
					});
				}

				__instance.units.Sort((Unit x, Unit y) =>
					x.stats.GetStatValue("initiative").CompareTo(y.stats.GetStatValue("initiative")));
				FightStarted(__instance) = true;
				__instance.index = 0;

				Unit active = __instance.ActiveUnit;
				if (PatchRules.ShouldSkipNullActiveUnit(active == null))
				{
					LokrPatchPlugin.Log.LogWarning(
						"NewInitiativeHandler.FightStartedHandler: ActiveUnit is null (empty initiative); skipped IncreaseTurnCount.");
				}
				else
				{
					active.IncreaseTurnCount();
				}

				return false;
			}
		}

		[HarmonyPatch(typeof(Stage), nameof(Stage.StartFight))]
		private static class StartFightPatch
		{
			private static bool Prefix(Stage __instance)
			{
				__instance.isFighting = true;
				Events.instance.Raise(new FightStartedEvent(__instance));

				Unit active = __instance.initiative.ActiveUnit;
				if (PatchRules.ShouldSkipNullActiveUnit(active == null))
				{
					LokrPatchPlugin.Log.LogWarning(
						"Stage.StartFight: ActiveUnit is null (empty initiative); skipped TurnStarted.");
				}
				else
				{
					__instance.TurnStarted(active);
					active.TurnStarted();
				}

				DustParticlesPrefab(__instance) = DataHelper.LoadDustRunFX();
				__instance.unitController = Object.FindObjectOfType<UnitController>();
				return false;
			}
		}
	}
}
