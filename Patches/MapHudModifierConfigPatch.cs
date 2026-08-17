using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Ironhide.Legends.Model.Game.Units;
using Ironhide.Legends.Model.Game.Units.Abilities;
using Ironhide.Legends.Model.Metagame;
using Ironhide.Legends.Model.Metagame.Heroes;
using Ironhide.Legends.Model.Metagame.Results;
using Ironhide.Legends.Utils;
using Ironhide.Legends.View.Map;
using Ironhide.Legends.View.Map.Screens.Rewards;
using Ironhide.Legends.View.Utils;
using Ironhide.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace LokrPatch.Patches
{
	/// <summary>Skips map HUD / reward modifier icons when <see cref="HeroModifiersAsset.GetConfigByKey"/> returns null.</summary>
	/// <remarks>
	/// Vanilla already returns null on a miss; the callers then read <c>overheadIcon</c> /
	/// <c>modifierIcon</c> with no check. A dummy config is not used — the else branches would
	/// enable a blank icon. Combat <c>ApplyModifier</c> is a different patch.
	/// </remarks>
	internal static class MapHudModifierConfigPatch
	{
		[HarmonyPatch(typeof(MapHeroBarPortraitModifiers), nameof(MapHeroBarPortraitModifiers.RefreshModifiers))]
		private static class MapHeroBarPortraitModifiersPatch
		{
			private static bool Prefix(MapHeroBarPortraitModifiers __instance)
			{
				if (__instance.hero == null && __instance.unit == null)
				{
					return false;
				}

				Hero hero = __instance.hero;
				if (__instance.unit != null && __instance.hero == null)
				{
					hero = MetagameManager.instance.HeroManager.GetAllHeroes().FirstOrDefault(
						h => h.unitDefinition.id == __instance.unit.unitDefinition.id);
				}

				if (hero == null)
				{
					return false;
				}

				List<string> modifierCategories = MetagameManager.instance.HeroManager.GetModifierCategories();
				__instance.CleanModifiers();
				for (int i = 0; i < modifierCategories.Count; i++)
				{
					string category = modifierCategories[i];
					MapHeroModifierInstance modifierInCategory = hero.GetModifierInCategory(category);
					if (modifierInCategory == null)
					{
						continue;
					}

					HeroModifiersConfig configByKey = __instance.modifierAsset.GetConfigByKey(modifierInCategory.modifier.id);
					if (PatchRules.ShouldSkipNullModifierConfig(configByKey == null))
					{
						LokrPatchPlugin.Log.LogWarning(
							"MapHeroBarPortraitModifiers: skipped missing HeroModifiersAsset key '"
							+ modifierInCategory.modifier.id + "'.");
						continue;
					}

					string title = LocalizationManager.instance.LocalizeString(modifierInCategory.modifier.Name);
					object extra = ExtraMapHeroModifierData(modifierInCategory);
					string description = LocalizationManager.instance.TryLocalizeString(
						modifierInCategory.modifier.Description,
						new object[] { extra });
					if (configByKey.overheadIcon != null && configByKey.isLeft)
					{
						if (__instance.overheadLeftHolder != null)
						{
							__instance.overheadLeftHolder.transform.parent.gameObject.SetActive(true);
						}

						__instance.overheadLeft.enabled = true;
						__instance.overheadLeft.sprite = configByKey.overheadIcon;
						if (__instance.overheadLeftButton != null)
						{
							__instance.overheadLeftButton.interactable = true;
						}

						TooltipContentAndVisibilityController tooltipLeft = __instance.tooltipContentLeft;
						if (tooltipLeft != null)
						{
							tooltipLeft.SetupTooltipContent(title, description, null, null, null);
						}
					}
					else
					{
						if (__instance.overheadRightHolder != null)
						{
							__instance.overheadRightHolder.transform.parent.gameObject.SetActive(true);
						}

						__instance.overleftRight.enabled = true;
						__instance.overleftRight.sprite = configByKey.overheadIcon;
						if (__instance.overheadRightButton != null)
						{
							__instance.overheadRightButton.interactable = true;
						}

						TooltipContentAndVisibilityController tooltipRight = __instance.tooltipContentRight;
						if (tooltipRight != null)
						{
							tooltipRight.SetupTooltipContent(title, description, null, null, null);
						}
					}
				}

				return false;
			}
		}

		[HarmonyPatch(typeof(PortraitInitiativeMapModifiers), nameof(PortraitInitiativeMapModifiers.UpdateModifiers))]
		private static class PortraitInitiativeMapModifiersPatch
		{
			private static bool Prefix(PortraitInitiativeMapModifiers __instance, Unit unit)
			{
				__instance.CleanModifiers();
				if (unit == null || unit.modifiers == null)
				{
					return false;
				}

				foreach (ModifierInstance modifierInstance in unit.modifiers)
				{
					if (modifierInstance == null || modifierInstance.Modifier == null
						|| string.IsNullOrEmpty(modifierInstance.Modifier.mapMapping))
					{
						continue;
					}

					HeroModifiersConfig configByKey = __instance.modifierAsset.GetConfigByKey(
						modifierInstance.Modifier.mapMapping);
					if (PatchRules.ShouldSkipNullModifierConfig(configByKey == null))
					{
						LokrPatchPlugin.Log.LogWarning(
							"PortraitInitiativeMapModifiers: skipped missing HeroModifiersAsset key '"
							+ modifierInstance.Modifier.mapMapping + "'.");
						continue;
					}

					if (configByKey.overheadIcon != null && configByKey.isLeft)
					{
						__instance.overheadLeft.enabled = true;
						__instance.overheadLeft.sprite = configByKey.overheadIcon;
					}
					else
					{
						__instance.overleftRight.enabled = true;
						__instance.overleftRight.sprite = configByKey.overheadIcon;
					}
				}

				return false;
			}
		}

		[HarmonyPatch(typeof(UnitDetailMapModifiers), nameof(UnitDetailMapModifiers.SetMapModifiers))]
		private static class UnitDetailMapModifiersPatch
		{
			private static bool Prefix(UnitDetailMapModifiers __instance, Unit unit)
			{
				__instance.CleanModifiers();
				if (unit == null || unit.modifiers == null)
				{
					return false;
				}

				foreach (ModifierInstance modifierInstance in unit.modifiers)
				{
					if (modifierInstance == null || modifierInstance.Modifier == null
						|| string.IsNullOrEmpty(modifierInstance.Modifier.mapMapping))
					{
						continue;
					}

					HeroModifiersConfig configByKey = __instance.modifierAsset.GetConfigByKey(
						modifierInstance.Modifier.mapMapping);
					if (PatchRules.ShouldSkipNullModifierConfig(configByKey == null))
					{
						LokrPatchPlugin.Log.LogWarning(
							"UnitDetailMapModifiers: skipped missing HeroModifiersAsset key '"
							+ modifierInstance.Modifier.mapMapping + "'.");
						continue;
					}

					__instance.separator.SetActive(true);
					string mapMapping = modifierInstance.Modifier.mapMapping;
					string title = LocalizationManager.instance.LocalizeString("MAPBUFF_" + mapMapping + "_NAME");
					object extra = ExtraModifierInstanceData(modifierInstance);
					string description = LocalizationManager.instance.TryLocalizeString(
						"MAPBUFF_" + mapMapping + "_DESCRIPTION",
						new object[] { extra });
					if (configByKey.overheadIcon != null && configByKey.isLeft)
					{
						__instance.overheadLeft.gameObject.SetActive(true);
						__instance.overheadLeft.sprite = configByKey.overheadIcon;
						__instance.overheadLeft.gameObject.transform.Find("FrameSelected").gameObject.GetComponent<Image>().sprite =
							configByKey.overheadSelected;
						if (__instance.tooltipContentLeft == null)
						{
							__instance.tooltipContentLeft = MonoSingleton<TooltipManager>.Instance.CreateAndSetupTooltip(
								__instance.overheadLeft.transform, null);
						}

						TooltipContentAndVisibilityController tooltipLeft = __instance.tooltipContentLeft;
						if (tooltipLeft != null)
						{
							tooltipLeft.SetupTooltipContent(title, description, null, null, null);
						}
					}
					else
					{
						__instance.overleftRight.gameObject.SetActive(true);
						__instance.overleftRight.sprite = configByKey.overheadIcon;
						__instance.overleftRight.gameObject.transform.Find("FrameSelected").gameObject.GetComponent<Image>().sprite =
							configByKey.overheadSelected;
						if (__instance.tooltipContentRight == null)
						{
							__instance.tooltipContentRight = MonoSingleton<TooltipManager>.Instance.CreateAndSetupTooltip(
								__instance.overleftRight.transform, null);
						}

						TooltipContentAndVisibilityController tooltipRight = __instance.tooltipContentRight;
						if (tooltipRight != null)
						{
							tooltipRight.SetupTooltipContent(title, description, null, null, null);
						}
					}
				}

				return false;
			}
		}

		[HarmonyPatch(typeof(RewardViewComponent), nameof(RewardViewComponent.SetReward))]
		private static class RewardViewComponentPatch
		{
			private static bool Prefix(RewardViewComponent __instance, ResultDescription rewardDescription)
			{
				if (rewardDescription == null || rewardDescription.kind != "applyModifier")
				{
					return true;
				}

				MapHeroModifierDefinition modifier = MetagameManager.instance.HeroManager.GetModifier(
					rewardDescription.stringValue);
				if (modifier == null)
				{
					LokrPatchPlugin.Log.LogWarning(
						"RewardViewComponent.SetReward: skipped missing modifier '"
						+ rewardDescription.stringValue + "'.");
					ApplyModifierRewardWithoutIcon(__instance, rewardDescription, null);
					return false;
				}

				HeroModifiersAsset modifierIcons = Traverse.Create(__instance).Field<HeroModifiersAsset>("modifierIcons").Value;
				HeroModifiersConfig configByKey = modifierIcons != null
					? modifierIcons.GetConfigByKey(modifier.id)
					: null;
				if (configByKey != null)
				{
					return true;
				}

				LokrPatchPlugin.Log.LogWarning(
					"RewardViewComponent.SetReward: skipped missing HeroModifiersAsset key '" + modifier.id + "'.");
				ApplyModifierRewardWithoutIcon(__instance, rewardDescription, modifier);
				return false;
			}
		}

		private static void ApplyModifierRewardWithoutIcon(
			RewardViewComponent view,
			ResultDescription rewardDescription,
			MapHeroModifierDefinition modifier)
		{
			Traverse t = Traverse.Create(view);
			HideRewardChrome(t);
			if (modifier != null)
			{
				LocalizationManager.instance.LocalizeString(modifier.Name);
			}

			RewardViewConfigAsset config = t.Field<RewardViewConfigAsset>("config").Value;
			if (rewardDescription.target == null)
			{
				if (config != null && config.applyModifierConfig != null)
				{
					view.SetTargetPortrait(config.applyModifierConfig.teamIcon);
				}
			}
			else
			{
				view.SetTargetPortrait(rewardDescription.target);
			}

			t.Field("rewardDescription").SetValue(rewardDescription);
		}

		private static void HideRewardChrome(Traverse t)
		{
			SetInactive(t.Field<Image>("background").Value);
			SetRectInactive(t.Field<RectTransform>("targetPortraitRect").Value);
			SetRectInactive(t.Field<RectTransform>("mainIconRect").Value);
			SetRectInactive(t.Field<RectTransform>("secondaryIconRect").Value);
			SetRectInactive(t.Field<RectTransform>("numberRect").Value);
		}

		private static void SetInactive(Image image)
		{
			if (image != null)
			{
				image.gameObject.SetActive(false);
			}
		}

		private static void SetRectInactive(RectTransform rect)
		{
			if (rect != null)
			{
				rect.gameObject.SetActive(false);
			}
		}

		private static object ExtraMapHeroModifierData(MapHeroModifierInstance mapHeroModifierInstance)
		{
			object result = null;
			if (mapHeroModifierInstance.combatModifier != null)
			{
				result = CollectModifierProperties(mapHeroModifierInstance.combatModifier);
			}

			return result;
		}

		private static object ExtraModifierInstanceData(ModifierInstance modifierInstance)
		{
			return CollectModifierProperties(modifierInstance.Modifier);
		}

		private static Dictionary<string, float> CollectModifierProperties(Modifier modifier)
		{
			Dictionary<string, float> dictionary = new Dictionary<string, float>();
			if (modifier == null)
			{
				return dictionary;
			}

			if (modifier.propertiesAdditive != null)
			{
				foreach (KeyValuePair<string, IExpression> pair in modifier.propertiesAdditive)
				{
					dictionary[pair.Key] = pair.Value.GetFloat(null);
				}
			}

			if (modifier.propertiesMultiplicative != null)
			{
				foreach (KeyValuePair<string, IExpression> pair in modifier.propertiesMultiplicative)
				{
					dictionary[pair.Key] = pair.Value.GetFloat(null);
				}
			}

			return dictionary;
		}
	}
}
