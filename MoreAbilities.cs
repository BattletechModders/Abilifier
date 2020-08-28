using System;
using BattleTech;
using BattleTech.UI;
using Harmony;
using static Abilifier.Mod;
using System.Reflection;
using UnityEngine;

namespace Abilifier
{
	public class MoreAbilities
	{
		[HarmonyPatch(typeof(CombatHUDMechwarriorTray), "Init")]
		[HarmonyPatch(MethodType.Normal)]
		[HarmonyPatch(new Type[] { typeof(CombatGameState), typeof(CombatHUD) })]
		[HarmonyAfter(new string[] { "io.mission.modrepuation" })]
		public static class CombatHUDMechwarriorTray_Init_Patch
		{
			public static void Postfix(CombatHUDMechwarriorTray __instance, CombatGameState Combat, CombatHUD HUD)
			{
				if (modSettings.extraAbilities > 0 || modSettings.nonTreeAbilities > 0)
				{
                    if (modSettings.cleanUpCombatUI == true)
                    {
						GameObject.Find("AT_OuterFrameL").SetActive(false);
						GameObject.Find("AT_OuterFrameR").SetActive(false);
						GameObject.Find("braceL (1)").SetActive(false);
						GameObject.Find("braceR (1)").SetActive(false);
						GameObject.Find("actionButton_DLine1").SetActive(false);
						GameObject.Find("actionButton_DLine2").SetActive(false);
						GameObject.Find("actionButton_DLine3").SetActive(false);
					}
					for (int i = 1; i <= modSettings.extraAbilities + modSettings.nonTreeAbilities; i++)
					{//thanks to kmission for doing the hard part of writing all this, and letting me use my elite copypaste skills//
						GameObject[] ActionButtonHolders = new GameObject[__instance.ActionButtonHolders.Length + 1];
						__instance.ActionButtonHolders.CopyTo(ActionButtonHolders, 0);
						CombatHUDActionButton[] ActionButtons = new CombatHUDActionButton[__instance.ActionButtons.Length + 1];
						__instance.ActionButtons.CopyTo(ActionButtons, 0);
						CombatHUDActionButton[] oldAbilityButtons = (CombatHUDActionButton[])typeof(CombatHUDMechwarriorTray).GetProperty("AbilityButtons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance, null);
						CombatHUDActionButton[] AbilityButtons = new CombatHUDActionButton[oldAbilityButtons.Length + 1];
						oldAbilityButtons.CopyTo(AbilityButtons, 0);
						GameObject sourceButtonObject = __instance.ActionButtonHolders[9];
						CombatHUDActionButton sourceButton = sourceButtonObject.GetComponentInChildren<CombatHUDActionButton>(true);
						GameObject newButtonObject = UnityEngine.Object.Instantiate<GameObject>(sourceButtonObject, sourceButtonObject.transform.parent);
						CombatHUDActionButton newButton = newButtonObject.GetComponentInChildren<CombatHUDActionButton>(true);
						ActionButtonHolders[__instance.ActionButtonHolders.Length] = newButtonObject;
						Vector3[] corners = new Vector3[4];
						sourceButtonObject.GetComponent<RectTransform>().GetWorldCorners(corners);
						float width = corners[2].x - corners[0].x;
						newButtonObject.transform.localPosition.Set(newButtonObject.transform.localPosition.x + (width), newButtonObject.transform.localPosition.y, newButtonObject.transform.localPosition.z);
						CombatHUDSidePanelHoverElement panelHoverElement = newButton.GetComponentInChildren<CombatHUDSidePanelHoverElement>(true);
						if (panelHoverElement == null)
						{
							panelHoverElement = newButton.gameObject.AddComponent<CombatHUDSidePanelHoverElement>();
						}
						panelHoverElement.Init(HUD);
						newButton.Init(Combat, HUD, BTInput.Instance.Key_None(), true);
						ActionButtonHolders[__instance.ActionButtonHolders.Length] = newButtonObject;
						ActionButtons[__instance.ActionButtons.Length] = newButton;
						AbilityButtons[oldAbilityButtons.Length] = newButton;
						__instance.ActionButtonHolders = ActionButtonHolders;
						PropertyInfo ActionButtonsProperty = typeof(CombatHUDMechwarriorTray).GetProperty("ActionButtons");
						ActionButtonsProperty.GetSetMethod(true).Invoke(__instance, new object[]
						{
						ActionButtons
						});
						typeof(CombatHUDMechwarriorTray).GetProperty("AbilityButtons", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, AbilityButtons, null);
					}
				}
			}
		}
	}
}


