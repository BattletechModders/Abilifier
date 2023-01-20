using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Abilifier.Framework;
using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;
using Harmony;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TB.ComponentModel;
using UnityEngine;
using Logger = Abilifier.Framework.Logger;

namespace Abilifier.Patches
{
    public enum TargetTeam
    {
        ENEMY,
        FRIENDLY,
        BOTH
    }
    public static class AbilityExtensions
    {
        public static class ModState
        {
            public static AbilityDefExtension PendingAbilityDefExtension = new AbilityDefExtension();
            public static List<AbilityUseInfo> AbilityUses = new List<AbilityUseInfo>();

            public static void Reset()
            {
                AbilityUses = new List<AbilityUseInfo>();
                PendingAbilityDefExtension = new AbilityDefExtension();
            }
        }

        public class AbilityDefExtension
        {
            public int ResolveCost { get; set; } = 0;
            public int CBillCost { get; set; } = 0;
            public string CMDPilotOverride { get; set; } = "";
            public TargetTeam TargetFriendlyUnit { get; set; } = TargetTeam.ENEMY;
            public bool TriggersUniversalCooldown { get; set; } = true;
            public bool IgnoresUniversalCooldown { get; set; } = false;
            public bool StartInCooldown { get; set; } = false;
        }

        public class AbilityUseInfo
        {
            public string AbilityID;
            public string AbilityName;
            public int UseCost;
            public int UseCount;
            public int TotalCost => UseCount * UseCost;

            public AbilityUseInfo(string abilityID, string abilityName, int useCost)
            {
                this.AbilityID = abilityID;
                this.AbilityName = abilityName;
                this.UseCost = useCost;
                this.UseCount = 1;
            }
        }

        public class SelectionStateMWTargetSingle : SelectionStateMWTargetSingleBase
        {
            public SelectionStateMWTargetSingle(CombatGameState Combat, CombatHUD HUD, CombatHUDActionButton FromButton) : base(Combat, HUD, FromButton)
            {
                base.SelectionType = SelectionType.TargetSingleEnemy;
                base.PriorityLevel = SelectionPriorityLevel.Ability;

                var abilityDef = FromButton.Ability.Def;
                if (abilityDef.getAbilityDefExtension().TargetFriendlyUnit == TargetTeam.BOTH)
                {
                    this.abilitySelectionText = "SELECT A TARGET";
                    return;
                }
                else if (abilityDef.getAbilityDefExtension().TargetFriendlyUnit == TargetTeam.FRIENDLY)

                {
                    this.abilitySelectionText = HUD.MechWarriorTray.targetAlliedAbilityText;
                    return;
                }
                this.abilitySelectionText = HUD.MechWarriorTray.targetEnemyAbilityText;
            }

            public override bool CanTargetCombatant(ICombatant potentialTarget)
            {
                var abilityDef = FromButton.Ability.Def;
                if (potentialTarget is AbstractActor actor)
                {
                    if (this.SelectedActor == actor)
                    {
                        return false;
                    }

                    if (abilityDef.IntParam2 > 0)
                    {
                        var distance = Vector3.Distance(potentialTarget.CurrentPosition,
                            this.SelectedActor.CurrentPosition);
                        if (distance > abilityDef.IntParam2) return false;
                    }
                }
                
                if (abilityDef.getAbilityDefExtension().TargetFriendlyUnit == TargetTeam.BOTH)
                {
                    return true;
                }

                if (abilityDef.getAbilityDefExtension().TargetFriendlyUnit == TargetTeam.ENEMY &&
                    this.SelectedActor.team.IsFriendly(potentialTarget.team))
                {
                    return false;
                }
                if (abilityDef.getAbilityDefExtension().TargetFriendlyUnit == TargetTeam.FRIENDLY &&
                    this.SelectedActor.team.IsEnemy(potentialTarget.team))
                {
                    return false;
                }
                return true;
            }
        }

        private static ConcurrentDictionary<string, AbilityDefExtension> abilityDefExtensionDict = new ConcurrentDictionary<string, AbilityDefExtension>();
        public static AbilityDefExtension getAbilityDefExtension(this AbilityDef abilityDef)
        {
            if (abilityDefExtensionDict.TryGetValue(abilityDef.Id, out var abilityDefExtension))
            {
                return abilityDefExtension;
            }

            return new AbilityDefExtension();
        }

        public class AbilityExtensionPatches
        {
            [HarmonyPatch(typeof(AbilityDef), "FromJSON")]
            public static class AbilityDef_FromJSON_ABILIFIER
            {
                public static void Prefix(AbilityDef __instance, ref string json)
                {
                    var abilityDefJO = JObject.Parse(json);
                    var state = new AbilityDefExtension();
                    ModState.PendingAbilityDefExtension = new AbilityDefExtension();
                    if (abilityDefJO["CBillCost"] != null)
                    {
                        state.CBillCost = (int)abilityDefJO["CBillCost"];
                        abilityDefJO.Remove("CBillCost");
                    }
                    if (abilityDefJO["ResolveCost"] != null)
                    {
                        state.ResolveCost = (int)abilityDefJO["ResolveCost"];
                        abilityDefJO.Remove("ResolveCost");
                    }
                    if (abilityDefJO["CMDPilotOverride"] != null)
                    {
                        state.CMDPilotOverride = (string)abilityDefJO["CMDPilotOverride"];
                        abilityDefJO.Remove("CMDPilotOverride");
                    }
                    if (abilityDefJO["TargetFriendlyUnit"] != null)
                    {
                        state.TargetFriendlyUnit = (TargetTeam) Enum.Parse(typeof(TargetTeam), (string)abilityDefJO["TargetFriendlyUnit"]);
                        abilityDefJO.Remove("TargetFriendlyUnit");
                    }

                    if (abilityDefJO["TriggersUniversalCooldown"] != null)
                    {
                        state.TriggersUniversalCooldown = (bool)abilityDefJO["TriggersUniversalCooldown"];
                        abilityDefJO.Remove("TriggersUniversalCooldown");
                    }
                    if (abilityDefJO["IgnoresUniversalCooldown"] != null)
                    {
                        state.IgnoresUniversalCooldown = (bool)abilityDefJO["IgnoresUniversalCooldown"];
                        abilityDefJO.Remove("IgnoresUniversalCooldown");
                    }
                    if (abilityDefJO["StartInCooldown"] != null)
                    {
                        state.StartInCooldown = (bool)abilityDefJO["StartInCooldown"];
                        abilityDefJO.Remove("StartInCooldown");
                    }

                    ModState.PendingAbilityDefExtension = state;
                    json = abilityDefJO.ToString(Formatting.Indented);
                    Logger.LogTrace($"[INFO] AbilityDef_FromJSON PREFIX RAN");
                }

                public static void Postfix(AbilityDef __instance, string json)
                {
                    //if (abilityDefExtensionDict.ContainsKey(__instance.Id))
                    //{
                    //    abilityDefExtensionDict[__instance.Id] = __state;
                    //    return;
                    //}
                    abilityDefExtensionDict.AddOrUpdate(__instance.Id, ModState.PendingAbilityDefExtension, (k, v) => ModState.PendingAbilityDefExtension);
                    Logger.LogTrace($"[INFO] AbilityDef_FromJSON - added {__instance.Id} to dict with values CBillCost {ModState.PendingAbilityDefExtension.CBillCost}, CMDPilotOverride {ModState.PendingAbilityDefExtension.CMDPilotOverride}, IgnoresUniversalCooldown {ModState.PendingAbilityDefExtension.IgnoresUniversalCooldown}, ResolveCost {ModState.PendingAbilityDefExtension.ResolveCost}, StartInCooldown {ModState.PendingAbilityDefExtension.StartInCooldown}, TargetFriendlyUnit {ModState.PendingAbilityDefExtension.TargetFriendlyUnit}, and TriggersUniversalCooldown {ModState.PendingAbilityDefExtension.TriggersUniversalCooldown}");
                }
            }

            [HarmonyPatch(typeof(Ability), "Init", new Type[] { typeof(CombatGameState) })]
            public static class Ability_Init
            {
                public static bool Prepare() => true;
                public static void Postfix(Ability __instance, CombatGameState Combat)
                {
                    if (Combat != null && __instance.Def.getAbilityDefExtension().StartInCooldown)
                    {
                        if (Combat.TurnDirector.CurrentRound < 1)
                        {
                            __instance.CurrentCooldown = __instance.Def.ActivationCooldown + 1;
                            //Traverse.Create(__instance).Property("CurrentCooldown").SetValue(__instance.Def.ActivationCooldown + 1);
                        }
                        else
                        {
                            __instance.CurrentCooldown = __instance.Def.ActivationCooldown;
                            //Traverse.Create(__instance).Property("CurrentCooldown").SetValue(__instance.Def.ActivationCooldown);
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(AbstractActor), "CooldownAllAbilities")]
            public static class AbstractActor_CooldownAllAbilities
            {
                public static bool Prefix(AbstractActor __instance)
                {
                    foreach (var ability in __instance.ComponentAbilities)
                    {
                        if (!ability.Def.getAbilityDefExtension().IgnoresUniversalCooldown)
                        {
                            ability.ActivateMiniCooldown();
                        }
                    }
                    return false;
                }
            }

            [HarmonyPatch(typeof(Pilot), "CooldownAllAbilities")]
            public static class Pilot_CooldownAllAbilities
            {
                public static bool Prefix(Pilot __instance)
                {
                    foreach (var ability in __instance.ActiveAbilities)
                    {
                        if (!ability.Def.getAbilityDefExtension().IgnoresUniversalCooldown)
                        {
                            ability.ActivateMiniCooldown();
                        }
                    }
                    return false;
                }
            }

            [HarmonyPatch(typeof(Team), "CooldownAllAbilities")]
            public static class Team_CooldownAllAbilities
            {
                public static bool Prefix(Team __instance)
                {
                    foreach (var ability in __instance.CommandAbilities)
                    {
                        if (!ability.Def.getAbilityDefExtension().IgnoresUniversalCooldown)
                        {
                            ability.ActivateMiniCooldown();
                        }
                    }
                    return false;
                }
            }


            [HarmonyPatch(typeof(Pilot), "ConfirmAbility", new Type[] { typeof(AbstractActor), typeof(string), typeof(string) })]
            public static class Pilot_ConfirmAbility
            {
                public static bool Prefix(Pilot __instance, AbstractActor pilotedActor, string abilityName, string targetGUID)
                {
                    if (pilotedActor == null || string.IsNullOrEmpty(abilityName))
                    {
                        Mod.modLog.LogMessage("[ERROR] Invalid parameters passes to ConfirmAbility");
                        return false;
                    }
                    Ability ability = __instance.Abilities.Find((Ability x) => x.Def.Id == abilityName);
                    if (ability == null)
                    {
                        Mod.modLog.LogMessage("[ERROR] ConfirmAbility: pilot " + __instance.Description.Name + " does not have ability " + abilityName);
                        return false;
                    }

                    if (!ability.Def.getAbilityDefExtension().TriggersUniversalCooldown)
                    {
                        if (targetGUID == pilotedActor.GUID)
                        {
                            ability.Confirm(pilotedActor, pilotedActor);
                            return false;
                        }

                        var combat = UnityGameInstance.BattleTechGame.Combat;
                        ICombatant combatant = combat.FindCombatantByGUID(targetGUID, false);
                        if (combatant == null)
                        {
                            Mod.modLog.LogMessage("[ERROR] ConfirmAbility: no valid target found for id " + targetGUID);
                            return false;
                        }

                        ability.Confirm(pilotedActor, combatant);
                        return false;
                    }
                    return true;
                }
            }


            [HarmonyPatch(typeof(AbstractActor), "ConfirmAbility", new Type[] { typeof(AbstractActor), typeof(string), typeof(string) })]
            public static class AbstractActor_ConfirmAbility
            {
                public static bool Prefix(AbstractActor __instance, AbstractActor pilotedActor, string abilityName, string targetGUID)
                {
                    if (pilotedActor == null || string.IsNullOrEmpty(abilityName))
                    {
                        AbstractActor.activationLogger.LogError("Invalid parameters passes to ConfirmAbility");
                        return false;
                    }
                    Ability ability = __instance.ComponentAbilities.Find((Ability x) => x.Def.Id == abilityName);
                    if (ability == null)
                    {
                        AbstractActor.activationLogger.LogError("ConfirmAbility: pilot " + __instance.Description.Name + " does not have ability " + abilityName);
                        return false;
                    }

                    if (!ability.Def.getAbilityDefExtension().TriggersUniversalCooldown)
                    {
                        if (targetGUID == pilotedActor.GUID)
                        {
                            ability.Confirm(pilotedActor, pilotedActor);
                            return false;
                        }
                        ICombatant combatant = __instance.Combat.FindCombatantByGUID(targetGUID, false);
                        if (combatant == null)
                        {
                            AbstractActor.activationLogger.LogError("ConfirmAbility: no valid target found for id " + targetGUID);
                            return false;
                        }
                        ability.Confirm(pilotedActor, combatant);
                        return false;
                    }
                    return true;
                }
            }

            [HarmonyPatch(typeof(SelectionState), "GetNewSelectionStateByType", new Type[] { typeof(SelectionType), typeof(CombatGameState), typeof(CombatHUD), typeof(CombatHUDActionButton), typeof(AbstractActor) })]
            public static class SelectionState_GetNewSelectionStateByType
            {
                public static bool Prefix(SelectionState __instance, SelectionType type, CombatGameState Combat, CombatHUD HUD, CombatHUDActionButton FromButton, AbstractActor actor, ref SelectionState __result)
                {
                    if (!FromButton || FromButton.Ability == null) return true;
                    if (type == SelectionType.TargetSingleEnemy || type == SelectionType.TargetSingleAlly)
                    {
                        __result = new SelectionStateMWTargetSingle(Combat, HUD, FromButton);
                        return false;
                    }
                    return true;
                }
            }

            [HarmonyPatch(typeof(CombatHUDActionButton), "ActivateAbility", new Type[] { typeof(string), typeof(string) })]
            public static class CombatHUDActionButton_ActivateAbility_Confirmed
            {
                public static void Postfix(CombatHUDActionButton __instance, string creatorGUID, string targetGUID)
                {
                    if (ModState.AbilityUses.All(x => x.AbilityID != __instance.Ability.Def.Id))
                    {
                        var abilityUse = new AbilityUseInfo(__instance.Ability.Def.Id, __instance.Ability.Def.Description.Name, __instance.Ability.Def.getAbilityDefExtension().CBillCost);

                        ModState.AbilityUses.Add(abilityUse);
                        Mod.modLog.LogMessage($"Added usage cost for {abilityUse.AbilityName} - {abilityUse.UseCost}");
                    }
                    else
                    {
                        var abilityUse = ModState.AbilityUses.FirstOrDefault(x => x.AbilityID == __instance.Ability.Def.Id);
                        if (abilityUse == null)
                        {
                            Mod.modLog.LogMessage($"ERROR: AbilityUseInfo was null");
                        }
                        else
                        {
                            abilityUse.UseCount += 1;
                            Mod.modLog.LogMessage($"Added usage cost for {abilityUse.AbilityName} - {abilityUse.UseCost}, used {abilityUse.UseCount} times");
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(CombatHUDEquipmentSlot), "ActivateAbility", new Type[] { typeof(string), typeof(string) })]
            public static class CombatHUDEquipmentSlot_ActivateAbility_Confirmed
            {
                public static void Postfix(CombatHUDEquipmentSlot __instance, string creatorGUID, string targetGUID)
                {
                    if (ModState.AbilityUses.All(x => x.AbilityID != __instance.Ability.Def.Id))
                    {
                        var abilityUse = new AbilityUseInfo(__instance.Ability.Def.Id, __instance.Ability.Def.Description.Name, __instance.Ability.Def.getAbilityDefExtension().CBillCost);

                        ModState.AbilityUses.Add(abilityUse);
                        Mod.modLog.LogMessage($"Added usage cost for {abilityUse.AbilityName} - {abilityUse.UseCost}");
                    }
                    else
                    {
                        var abilityUse = ModState.AbilityUses.FirstOrDefault(x => x.AbilityID == __instance.Ability.Def.Id);
                        if (abilityUse == null)
                        {
                            Mod.modLog.LogMessage($"ERROR: AbilityUseInfo was null");
                        }
                        else
                        {
                            abilityUse.UseCount += 1;
                            Mod.modLog.LogMessage($"Added usage cost for {abilityUse.AbilityName} - {abilityUse.UseCost}, used {abilityUse.UseCount} times");
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(AAR_ContractObjectivesWidget), "FillInObjectives")]
            public static class AAR_ContractObjectivesWidget_FillInObjectives_Patch
            {
                public static void Postfix(AAR_ContractObjectivesWidget __instance)
                {
                    if (UnityGameInstance.BattleTechGame.Simulation == null) return;
                    if (ModState.AbilityUses.Count <= 0) return;
                    //var addObjectiveMethod = Traverse.Create(__instance).Method("AddObjective", new Type[] { typeof(MissionObjectiveResult) });
                    foreach (var abilityUse in ModState.AbilityUses)
                    {
                        if (abilityUse.TotalCost <= 0) continue;
                        var abilityUseCost = $"Ability Costs for {abilityUse.AbilityName}: {abilityUse.UseCount} Uses x {abilityUse.UseCost} ea. = ¢-{abilityUse.TotalCost}";

                        var abilityUseCostResult = new MissionObjectiveResult($"{abilityUseCost}", Guid.NewGuid().ToString(), false, true, ObjectiveStatus.Ignored, false);
                        __instance.AddObjective(abilityUseCostResult);
                        //addObjectiveMethod.GetValue(abilityUseCostResult);
                    }
                }
            }

            [HarmonyPatch(typeof(Contract), "CompleteContract", new Type[] { typeof(MissionResult), typeof(bool) })]
            public static class Contract_CompleteContract_Patch
            {
                public static void Postfix(Contract __instance, MissionResult result, bool isGoodFaithEffort)
                {
                    if (UnityGameInstance.BattleTechGame.Simulation == null) return;
                    PilotResolveTracker.HolderInstance.pilotResolveDict = new Dictionary<string, PilotResolveInfo>();
                    if (ModState.AbilityUses.Count <= 0) return;
                    ModState.Reset();
                    var finalAbilityCosts = 0;
                    foreach (var abilityUse in ModState.AbilityUses)
                    {
                        finalAbilityCosts += abilityUse.TotalCost;
                        Mod.modLog.LogMessage($"{abilityUse.TotalCost} in command costs for {abilityUse.AbilityName}: {abilityUse.UseCost}. Current Total Command Cost: {finalAbilityCosts}");
                    }
                    var moneyResults = __instance.MoneyResults - finalAbilityCosts;
                    //Traverse.Create(__instance).Property("MoneyResults").SetValue(moneyResults);
                    __instance.MoneyResults = moneyResults;
                }
            }
        }
    }
}
