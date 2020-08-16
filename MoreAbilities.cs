using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.UI;
using Harmony;
using static Abilifier.Mod;
using static Abilifier.MoreAbilities;
using System.Reflection;
using BattleTech.Data;
using UnityEngine;

namespace Abilifier
{
	public class MoreAbilities
	{
		

		[HarmonyPatch(typeof(CombatHUDMechwarriorTray), "Init")]
		[HarmonyAfter(new string[] { "io.mission.modrepuation" })]
		public static void Postfix(CombatHUDMechwarriorTray __instance, CombatGameState Combat, CombatHUD HUD)
		{
			if (modSettings.extraAbilities > 0)
			{
				GameObject[] ActionButtonHolders = new GameObject[__instance.ActionButtonHolders.Length + 1];//
				__instance.ActionButtonHolders.CopyTo(ActionButtonHolders, 0);
				CombatHUDActionButton[] ActionButtons = new CombatHUDActionButton[__instance.ActionButtons.Length + 1];//
				__instance.ActionButtons.CopyTo(ActionButtons, 0);
				CombatHUDActionButton[] oldAbilityButtons = (CombatHUDActionButton[])typeof(CombatHUDMechwarriorTray).GetProperty("AbilityButtons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance, null);
				CombatHUDActionButton[] AbilityButtons = new CombatHUDActionButton[oldAbilityButtons.Length + 1];//
				oldAbilityButtons.CopyTo(AbilityButtons, 0);
				GameObject sourceButtonObject = __instance.ActionButtonHolders[9]; //add 1?

				CombatHUDActionButton sourceButton = sourceButtonObject.GetComponentInChildren<CombatHUDActionButton>(true);
				GameObject newButtonObject = UnityEngine.Object.Instantiate<GameObject>(sourceButtonObject, sourceButtonObject.transform.parent);
//				GameObject newButtonObject2 = UnityEngine.Object.Instantiate<GameObject>(sourceButtonObject, sourceButtonObject.transform.parent);//

				CombatHUDActionButton newButton = newButtonObject.GetComponentInChildren<CombatHUDActionButton>(true);
//				CombatHUDActionButton newButton2 = newButtonObject2.GetComponentInChildren<CombatHUDActionButton>(true);//

				ActionButtonHolders[__instance.ActionButtonHolders.Length] = newButtonObject;
//				ActionButtonHolders[__instance.ActionButtonHolders.Length + 1] = newButtonObject2;

				Vector3[] corners = new Vector3[4];
				sourceButtonObject.GetComponent<RectTransform>().GetWorldCorners(corners);
				float width = corners[2].x - corners[0].x;

				newButtonObject.transform.localPosition.Set(newButtonObject.transform.localPosition.x + (width), newButtonObject.transform.localPosition.y, newButtonObject.transform.localPosition.z);
//				newButtonObject2.transform.localPosition.Set(newButtonObject2.transform.localPosition.x + (width + width), newButtonObject2.transform.localPosition.y, newButtonObject2.transform.localPosition.z);

				CombatHUDSidePanelHoverElement panelHoverElement = newButton.GetComponentInChildren<CombatHUDSidePanelHoverElement>(true);
//				CombatHUDSidePanelHoverElement panelHoverElement2 = newButton2.GetComponentInChildren<CombatHUDSidePanelHoverElement>(true);

				if (panelHoverElement == null)
				{
					panelHoverElement = newButton.gameObject.AddComponent<CombatHUDSidePanelHoverElement>();
				}
//				if (panelHoverElement2 == null)
//				{
//					panelHoverElement2 = newButton2.gameObject.AddComponent<CombatHUDSidePanelHoverElement>();
//				}

				panelHoverElement.Init(HUD);
//				panelHoverElement2.Init(HUD);

				newButton.Init(Combat, HUD, BTInput.Instance.Key_None(), true);
//				newButton2.Init(Combat, HUD, BTInput.Instance.Key_None(), true);

				ActionButtonHolders[__instance.ActionButtonHolders.Length] = newButtonObject;
//				ActionButtonHolders[__instance.ActionButtonHolders.Length + 1] = newButtonObject2;

				ActionButtons[__instance.ActionButtons.Length] = newButton;
//				ActionButtons[__instance.ActionButtons.Length + 1] = newButton2;

				AbilityButtons[oldAbilityButtons.Length] = newButton;
//				AbilityButtons[oldAbilityButtons.Length + 1] = newButton2;

				__instance.ActionButtonHolders = ActionButtonHolders;

				PropertyInfo ActionButtonsProperty = typeof(CombatHUDMechwarriorTray).GetProperty("ActionButtons");
				ActionButtonsProperty.GetSetMethod(true).Invoke(__instance, new object[]
				{
					ActionButtons
				});
				typeof(CombatHUDMechwarriorTray).GetProperty("AbilityButtons", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, AbilityButtons, null);
			}
		}
		[HarmonyPatch(typeof(CombatHUDMechwarriorTray), "InitAbilityButtons")]
		[HarmonyAfter(new string[] { "io.mission.modrepuation" })]

		public static class CombatHUDMechwarriorTray_InitAbilityButtons
		{
			public static void Postfix(CombatHUDMechwarriorTray __instance, AbstractActor actor)
			{
				if (modSettings.extraAbilities > 0)
				{
					AbilityDef aDef = null;
					InitAbilityButtonsDelegate id = new InitAbilityButtonsDelegate(__instance, actor);

					if (!actor.Combat.DataManager.AbilityDefs.TryGet("AbilityDefAbilifierSelector", out aDef))
					{
						DataManager.InjectedDependencyLoadRequest dependencyLoad = new DataManager.InjectedDependencyLoadRequest(actor.Combat.DataManager);
						dependencyLoad.RequestResource(BattleTechResourceType.AbilityDef, CombatHUDMechwarriorTray_InitAbilityButtons.AbilityName);
						dependencyLoad.RegisterLoadCompleteCallback(new Action(id.OnAbilityLoad));
						actor.Combat.DataManager.InjectDependencyLoader(dependencyLoad, 1000U);
					}
					else
					{
						id.OnAbilityLoad();
					}
				}
			}
			public static readonly string AbilityName = "AbilityDefAbilifierSelector";
			public static Dictionary<string, Ability> abilifiedAbilities = new Dictionary<string, Ability>();
		}
	}
	[HarmonyPatch(typeof(CombatHUDMechwarriorTray), "ResetAbilityButtons")]
	[HarmonyAfter(new string[] { "io.mission.modrepuation" })]
	public static class CombatHUDMechwarriorTray_ResetAbilityButtons
	{
		public static void Postfix(CombatHUDMechwarriorTray __instance, AbstractActor actor)
		{
			if (modSettings.extraAbilities > 0)
			{
				AbilityDef aDef = null;
				if (actor.Combat.DataManager.AbilityDefs.TryGet("AbilityDefAbilifierSelector", out aDef))
				{
					Ability abilifAbility;
					if (!CombatHUDMechwarriorTray_InitAbilityButtons.abilifiedAbilities.ContainsKey(actor.GUID))
					{
						abilifAbility = new Ability(aDef);
						abilifAbility.Init(actor.Combat);
						CombatHUDMechwarriorTray_InitAbilityButtons.abilifiedAbilities.Add(actor.GUID, abilifAbility);
					}
					else
					{
						abilifAbility = CombatHUDMechwarriorTray_InitAbilityButtons.abilifiedAbilities[actor.GUID];
					}
					bool forceInactive = actor.HasActivatedThisRound || actor.MovingToPosition != null || (actor.Combat.StackManager.IsAnyOrderActive && actor.Combat.TurnDirector.IsInterleaved);

					CombatHUDActionButton[] AbilityButtons = (CombatHUDActionButton[])typeof(CombatHUDMechwarriorTray).GetProperty("AbilityButtons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance, null);
					typeof(CombatHUDMechwarriorTray).GetMethod("ResetAbilityButton", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[]
					{
					typeof(AbstractActor),
					typeof(CombatHUDActionButton),
					typeof(Ability),
					typeof(bool)
					}, null).Invoke(__instance, new object[]
					{
					actor,
					AbilityButtons[AbilityButtons.Length - 1],
					abilifAbility,
					forceInactive
					});

					if (forceInactive)
					{
						AbilityButtons[AbilityButtons.Length - 1].DisableButton();
					}

					if (!actor.Combat.TurnDirector.IsInterleaved && !actor.HasFiredThisRound && abilifAbility.IsAvailable && !actor.IsShutDown
						&& actor.MovingToPosition == null)
					{
						AbilityButtons[AbilityButtons.Length - 1].ResetButtonIfNotActive(actor);
					}
					else
					{
						AbilityButtons[AbilityButtons.Length - 1].DisableButton();
					}
				}
			}
		}
	}
	public class InitAbilityButtonsDelegate
	{

		public InitAbilityButtonsDelegate(CombatHUDMechwarriorTray hud, AbstractActor unit)
		{
			this.__instance = hud;
			this.actor = unit;
			this.abilityDef = null;
		}


		public void OnAbilityLoad()
		{
			this.abilityDef = this.actor.Combat.DataManager.AbilityDefs.Get(CombatHUDMechwarriorTray_InitAbilityButtons.AbilityName);

			bool flag = this.abilityDef.DependenciesLoaded(1000U);
			if (flag)
			{

				this.OnAbilityFullLoad();
			}
			else
			{

				DataManager.InjectedDependencyLoadRequest dependencyLoad = new DataManager.InjectedDependencyLoadRequest(this.actor.Combat.DataManager);
				this.abilityDef.GatherDependencies(this.actor.Combat.DataManager, dependencyLoad, 1000U);
				dependencyLoad.RegisterLoadCompleteCallback(new Action(this.OnAbilityFullLoad));
				this.actor.Combat.DataManager.InjectDependencyLoader(dependencyLoad, 1000U);
			}
		}


		public void OnAbilityFullLoad()
		{

			bool flag = !CombatHUDMechwarriorTray_InitAbilityButtons.abilifiedAbilities.ContainsKey(this.actor.GUID);
			Ability abilifAbility;
			if (flag)
			{

				abilifAbility = new Ability(this.abilityDef);
				abilifAbility.Init(this.actor.Combat);
				CombatHUDMechwarriorTray_InitAbilityButtons.abilifiedAbilities.Add(this.actor.GUID, abilifAbility);
			}
			else
			{

				abilifAbility = CombatHUDMechwarriorTray_InitAbilityButtons.abilifiedAbilities[this.actor.GUID];
			}

			CombatHUDActionButton[] AbilityButtons = (CombatHUDActionButton[])typeof(CombatHUDMechwarriorTray).GetProperty("AbilityButtons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this.__instance, null);
			AbilityButtons[AbilityButtons.Length - 1].InitButton((SelectionType)typeof(CombatHUDMechwarriorTray).GetMethod("GetSelectionTypeFromTargeting", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[]
			{
				this.abilityDef.Targeting,
				false
			}), abilifAbility, this.abilityDef.AbilityIcon, AbilityButtons[AbilityButtons.Length - 1].GUID, this.abilityDef.Description.Name, this.actor);

			AbilityButtons[AbilityButtons.Length - 1].isClickable = true;
			AbilityButtons[AbilityButtons.Length - 1].RefreshUIColors();

		}

		public CombatHUDMechwarriorTray __instance;

		public AbstractActor actor;

		public AbilityDef abilityDef;
	}

	[HarmonyPatch(typeof(SelectionState))]
	[HarmonyPatch("GetNewSelectionStateByType")]
	[HarmonyBefore(new string[] { "io.mission.modrepuation" })]
	[HarmonyPatch(MethodType.Normal)]
	[HarmonyPatch(new Type[]
	{
		typeof(SelectionType),
		typeof(CombatGameState),
		typeof(CombatHUD),
		typeof(CombatHUDActionButton),
		typeof(AbstractActor)
	})]


	public static class SelectionState_GetNewSelectionStateByType
	{
		public static bool Prefix(SelectionType type, CombatGameState Combat, CombatHUD HUD, CombatHUDActionButton FromButton, AbstractActor actor, ref SelectionState __result)
		{
			if (type == SelectionType.MWInstant && FromButton.Ability.Def.Id== "AbilityDefAbilifierSelector")
			{
				__result = new SelectionStateAbilifyAbilities(Combat, HUD, FromButton, actor);
				return false;
			}
            else
            {
				return true;
            }
		}
	}
}


