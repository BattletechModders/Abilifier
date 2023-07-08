#if NO_CAE
#else
using System;
using System.Linq;
using Abilifier.Framework;
using BattleTech;
using BattleTech.UI;
using CustomActivatableEquipment;
using UnityEngine;

namespace Abilifier.Patches
{
    public class ResolvePatch_CAE
    {
[HarmonyPatch(typeof(SelectionStateActiveProbeArc), "CreateFiringOrders")]
            public static class SelectionStateActiveProbeArc_CreateFiringOrders
            {
                //public static bool Prepare() => Mod.modSettings.enableResolverator;
                public static void Postfix(SelectionStateActiveProbeArc __instance, string button)
                {
                    if (button == "BTN_FireConfirm")
                    {
                        __instance.FromButton.Ability.ActivateCooldown();
                        var combat = UnityGameInstance.BattleTechGame.Combat;
                        if (combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                        var abilityDef = __instance.FromButton?.Ability?.Def;
                        if (abilityDef == null) return;
                        Mod.modLog?.Info?.Write($"Processing resolve costs for {abilityDef.Description.Name}");
                        var HUD = __instance.HUD;
                        var theActor = HUD.SelectedActor;
                        if (theActor == null) return;
                        if (!Mod.modSettings.enableResolverator)
                        {
                            var amt = -Mathf.RoundToInt(abilityDef.getAbilityDefExtension().ResolveCost);
                            theActor.team.ModifyMorale(amt);
                        }
                        else
                        {
                            var amt = -Mathf.RoundToInt(abilityDef.getAbilityDefExtension().ResolveCost);
                            theActor.ModifyResolve(amt);
                        }
                        HUD.MechWarriorTray.ResetMechwarriorButtons(theActor);

                        if (!Mod.modSettings.disableCalledShotExploit) return;
                        var selectionStack = HUD.selectionHandler.SelectionStack;
                        var moraleState = selectionStack.FirstOrDefault(x => x is SelectionStateMoraleAttack);
                        if (moraleState != null)
                        {
                            moraleState.OnInactivate();
                            moraleState.OnRemoveFromStack();
                            selectionStack.Remove(moraleState);
                        }
                        if (Mod.modLog?.IsTrace != null && Mod.modLog.IsTrace)
                        {
                            Mod.modLog?.Trace?.Write($"[SelectionStateActiveProbeArc_CreateFiringOrders] Dumping selection stack for {theActor.DisplayName}");
                            foreach (var selection in selectionStack)
                            {
                                Mod.modLog?.Trace?.Write($"--- {selection.SelectionType}");
                            }
                        }
                    }
                }
            }
        [HarmonyPatch(typeof(CombatHUDEquipmentSlotEx), "ResetAbilityButton",
            new Type[] { typeof(AbstractActor), typeof(CombatHUDActionButton), typeof(Ability), typeof(bool) })]
        public static class CombatHUDEquipmentSlotEx_ResetAbilityButton_Patch
        {
            public static bool Prepare() => Mod.modSettings.enableResolverator;
            public static void Postfix(CombatHUDWeaponPanel __instance, AbstractActor actor, CombatHUDActionButton button, Ability ability, bool forceInactive)
            {
                if (ability.Def.getAbilityDefExtension().ResolveCost <= 0) return;
                if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                if (actor == null || ability == null) return;
                var actorKey = actor.GetPilot().Fetch_rGUID();
                if (!PilotResolveTracker.HolderInstance.pilotResolveDict.TryGetValue(actorKey, out var pilotResolveInfo))
                    return;
                if (pilotResolveInfo.PilotResolve < Mathf.RoundToInt(ability.Def.getAbilityDefExtension().ResolveCost * actor.GetResolveCostBaseMult()))
                {
                    button.DisableButton();
                }
                if (false && pilotResolveInfo.Predicting && pilotResolveInfo.PredictedResolve <
                    Mathf.RoundToInt(ability.Def.getAbilityDefExtension().ResolveCost * actor.GetResolveCostBaseMult()))
                {
                    button.DisableButton();
                }
            }
        }
    }
}
#endif