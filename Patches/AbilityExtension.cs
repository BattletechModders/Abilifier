using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abilifier.Framework;
using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;
using Harmony;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Abilifier.Patches
{
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

        private static Dictionary<string, AbilityDefExtension> abilityDefExtensionDict = new Dictionary<string, AbilityDefExtension>();
        public static AbilityDefExtension getAbilityDefExtension(this AbilityDef abilityDef)
        {
            return abilityDefExtensionDict.ContainsKey(abilityDef.Id) ? abilityDefExtensionDict[abilityDef.Id] : new AbilityDefExtension();
        }

        [HarmonyPatch(typeof(AbilityDef), "FromJSON")]
        public static class AbilityDef_FromJSON
        {
            public static bool Prepare() => Mod.modSettings.enableResolverator;

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
            }

            public static void Postfix(AbilityDef __instance, string json, AbilityDefExtension __state)
            {
                if (abilityDefExtensionDict.ContainsKey(__instance.Id))
                {
                    abilityDefExtensionDict[__instance.Id] = __state;
                    return;
                }
                abilityDefExtensionDict.Add(__instance.Id, __state);
            }
        }


        [HarmonyPatch(typeof(CombatHUDActionButton), "ActivateAbility", new Type[] {typeof(string), typeof(string)})]
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
                    var abilityUseCost = $"Ability Costs for {abilityUse.AbilityName}: {abilityUse.UseCount} Uses x {abilityUse.UseCost} ea. = ¢-{abilityUse.TotalCost}";

                    var abilityUseCostResult = new MissionObjectiveResult($"{abilityUseCost}", Guid.NewGuid().ToString(), false, true, ObjectiveStatus.Failed, false);

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
