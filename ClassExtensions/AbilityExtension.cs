using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Abilifier.Framework;
using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;
using Harmony;
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
            public static List<AbilityUseInfo> AbilityUses = new List<AbilityUseInfo>();

            public static void Reset()
            {
                AbilityUses = new List<AbilityUseInfo>();
            }
        }

        public class AbilityDefExtension
        {
            public int ResolveCost = 0;
            public int CBillCost = 0;
            public string CMDPilotOverride = "";
            public TargetTeam TargetFriendlyUnit = TargetTeam.ENEMY;
            public bool TriggersUniversalCooldown = true;
            public bool IgnoresUniversalCooldown = false;
            public bool StartsInCooldown = false;
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

            protected override bool CanTargetCombatant(ICombatant potentialTarget)
            {
                if (potentialTarget is AbstractActor actor)
                {
                    if (this.SelectedActor == actor)
                    {
                        return false;
                    }
                }
                var abilityDef = FromButton.Ability.Def;
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
            return abilityDefExtensionDict.ContainsKey(abilityDef.Id) ? abilityDefExtensionDict[abilityDef.Id] : new AbilityDefExtension();
        }

        public class AbilityExtensionPatches
        {
            [HarmonyPatch(typeof(AbilityDef), "FromJSON")]
            public static class AbilityDef_FromJSON
            {
                public static void Prefix(AbilityDef __instance, string json, ref AbilityDefExtension __state)
                {
                    var abilityDefJO = JObject.Parse(json);
                    __state = new AbilityDefExtension { CBillCost = 0, ResolveCost = 0 };
                    if (abilityDefJO["CBillCost"] != null)
                    {
                        __state.CBillCost = (int)abilityDefJO["CBillCost"];
                    }
                    if (abilityDefJO["ResolveCost"] != null)
                    {
                        __state.ResolveCost = (int)abilityDefJO["ResolveCost"];
                    }
                    if (abilityDefJO["CMDPilotOverride"] != null)
                    {
                        __state.CMDPilotOverride = (string)abilityDefJO["CMDPilotOverride"];
                    }
                    if (abilityDefJO["TargetFriendlyUnit"] != null)
                    {
                        __state.TargetFriendlyUnit = (TargetTeam) Enum.Parse(typeof(TargetTeam), (string)abilityDefJO["TargetFriendlyUnit"]);
                    }

                    if (abilityDefJO["TriggersUniversalCooldown"] != null)
                    {
                        __state.TriggersUniversalCooldown = (bool)abilityDefJO["TriggersUniversalCooldown"];
                    }
                    if (abilityDefJO["IgnoresUniversalCooldown"] != null)
                    {
                        __state.IgnoresUniversalCooldown = (bool)abilityDefJO["IgnoresUniversalCooldown"];
                    }
                    if (abilityDefJO["StartsInCooldown"] != null)
                    {
                        __state.StartsInCooldown = (bool)abilityDefJO["StartsInCooldown"];
                    }
                }

                public static void Postfix(AbilityDef __instance, string json, AbilityDefExtension __state)
                {
                    //if (abilityDefExtensionDict.ContainsKey(__instance.Id))
                    //{
                    //    abilityDefExtensionDict[__instance.Id] = __state;
                    //    return;
                    //}
                    abilityDefExtensionDict.AddOrUpdate(__instance.Id, __state, (k, v) => { return __state; });
                }
            }

            [HarmonyPatch(typeof(Ability), "Init", new Type[] { typeof(CombatGameState) })]
            public static class Init
            {
                public static void Postfix(Ability __instance, CombatGameState Combat)
                {
                    if (__instance.Def.getAbilityDefExtension().StartsInCooldown)
                    {
                        if (Combat.TurnDirector.CurrentRound < 1)
                        {
                            Traverse.Create(__instance).Property("CurrentCooldown")
                                .SetValue(__instance.Def.ActivationCooldown + 1);
                        }
                        else
                        {
                            Traverse.Create(__instance).Property("CurrentCooldown")
                                .SetValue(__instance.Def.ActivationCooldown);
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
                    var addObjectiveMethod = Traverse.Create(__instance).Method("AddObjective", new Type[] { typeof(MissionObjectiveResult) });
                    foreach (var abilityUse in ModState.AbilityUses)
                    {
                        if (abilityUse.TotalCost <= 0) continue;
                        var abilityUseCost = $"Ability Costs for {abilityUse.AbilityName}: {abilityUse.UseCount} Uses x {abilityUse.UseCost} ea. = ¢-{abilityUse.TotalCost}";

                        var abilityUseCostResult = new MissionObjectiveResult($"{abilityUseCost}", Guid.NewGuid().ToString(), false, true, ObjectiveStatus.Ignored, false);
                        addObjectiveMethod.GetValue(abilityUseCostResult);
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
                    var finalAbilityCosts = 0;
                    foreach (var abilityUse in ModState.AbilityUses)
                    {
                        finalAbilityCosts += abilityUse.TotalCost;
                        Mod.modLog.LogMessage($"{abilityUse.TotalCost} in command costs for {abilityUse.AbilityName}: {abilityUse.UseCost}. Current Total Command Cost: {finalAbilityCosts}");
                    }
                    var moneyResults = __instance.MoneyResults - finalAbilityCosts;
                    Traverse.Create(__instance).Property("MoneyResults").SetValue(moneyResults);
                }
            }
        }
    }
}
