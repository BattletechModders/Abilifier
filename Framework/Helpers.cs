using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using Harmony;
using UnityEngine;
using UnityEngine.Events;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming

namespace Abilifier.Framework
{
    public static class Helpers
    {
        //        internal static readonly List<AbilityDef> ModAbilities = new List<AbilityDef>();

        //        internal static void PopulateAbilities()
        //        {
        //            var jsonFiles = new DirectoryInfo(
        //                Path.Combine(modSettings.modDirectory, "Abilities")).GetFiles().ToList();
        //            foreach (var file in jsonFiles)
        //            {
        //                var abilityDef = new AbilityDef();
        //                abilityDef.FromJSON(File.ReadAllText(file.FullName));
        //                if (!ModAbilities.Contains(abilityDef))
        //                {
        //                    ModAbilities.Add(abilityDef);
        //                }
        //                else
        //                {
        //                    Log("Duplicate AbilityDef, id is " + abilityDef.Id);
        //                }
        //            }
        //        }

        // modified copy from assembly

        public static List<Ability> FetchAllActorAbilities(this AbstractActor @this)
        {
            var abilities = @this.ComponentAbilities;
            abilities.AddRange(@this.GetPilot().Abilities);
            return abilities;
        }
        private static bool HasExistingAbilityAtTier(PilotDef pilotDef, AbilityDef abilityToUse)
        {
            var result = pilotDef.AbilityDefs.Any(x =>
                x.IsPrimaryAbility &&
                //x.ReqSkill != SkillType.NotSet &&
                x.ReqSkill == abilityToUse.ReqSkill &&
                x.ReqSkillLevel == abilityToUse.ReqSkillLevel);
            return result;
        }

        public static bool CanPilotTakeAbilityPip(SimGameState sim, PilotDef p, AbilityDef newAbility, bool checkSecondTier = false)
        {
            if (!newAbility.IsPrimaryAbility)
            {
                return true;
            }

            List<AbilityDef> primaryPilotAbilities = SimGameState.GetPrimaryPilotAbilities(p);
            if (primaryPilotAbilities == null)
            {
                return true;
            }
            if (primaryPilotAbilities.Count >= 3 + Mod.modSettings.extraAbilities) //change max allowed abilities for pilot to take when lvling up
            {
                return false;
            }
            Dictionary<SkillType, int> sortedSkillCount = sim.GetSortedSkillCount(p);

            var tempResult = (sortedSkillCount.Count <= 1 + Mod.modSettings.extraFirstTierAbilities
                              || sortedSkillCount.ContainsKey(newAbility.ReqSkill))
                             && (!sortedSkillCount.ContainsKey(newAbility.ReqSkill) ||
                                 sortedSkillCount[newAbility.ReqSkill] <= 1 + Mod.modSettings.extraAbilitiesAllowedPerSkill)
                             && (!checkSecondTier || sortedSkillCount.ContainsKey(newAbility.ReqSkill) ||
                                 primaryPilotAbilities.Count <= 1 + Mod.modSettings.extraAbilitiesAllowedPerSkill);

            if ((p.SkillGunnery >= Mod.modSettings.skillLockThreshold) ||
                (p.SkillPiloting >= Mod.modSettings.skillLockThreshold) ||
                (p.SkillGuts >= Mod.modSettings.skillLockThreshold) ||
                (p.SkillTactics >= Mod.modSettings.skillLockThreshold))

            { tempResult = false; }

            if (sortedSkillCount.Count <= 1 + Mod.modSettings.extraFirstTierAbilities && newAbility.ReqSkillLevel < Mod.modSettings.skillLockThreshold)
            {
                return true;
            }

            if (sortedSkillCount.ContainsKey(newAbility.ReqSkill) && sortedSkillCount[newAbility.ReqSkill] < 1 + Mod.modSettings.extraAbilitiesAllowedPerSkill) tempResult = true;

            var ct = sortedSkillCount.Where(x => x.Value >= 1 + Mod.modSettings.extraAbilitiesAllowedPerSkill);

            if (ct.Count() >= 1 + Mod.modSettings.extraPreCapStoneAbilities) tempResult = false;

            if (sortedSkillCount.ContainsKey(newAbility.ReqSkill) && sortedSkillCount[newAbility.ReqSkill] == 1 + Mod.modSettings.extraAbilitiesAllowedPerSkill && newAbility.ReqSkillLevel >= Mod.modSettings.skillLockThreshold)
            { tempResult = true; }

            return tempResult;
        }
        internal static void ForceResetCharacter(SGBarracksAdvancementPanel panel)
        {
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            //var traverse = Traverse.Create(panel);
            var orderedDictionary = panel.upgradedSkills;//traverse.Field("upgradedSkills").GetValue<OrderedDictionary>();

            var upgradedPrimarySkills = panel.upgradedPrimarySkills;// traverse.Field("upgradedPrimarySkills").GetValue<List<AbilityDef>>();
            var upgradedAbilities = new List<AbilityDef>();
            upgradedAbilities.AddRange(upgradedPrimarySkills);

            panel.SetPilot(panel.basePilot);//traverse.Field("basePilot").GetValue<Pilot>());
            
            foreach (var obj in orderedDictionary.Values)
            {
                var keyValuePair = (KeyValuePair<string, int>)obj;

                var abilityDef = upgradedAbilities.FindAll(x => x.ReqSkill.ToString() == keyValuePair.Key && x.ReqSkillLevel == keyValuePair.Value + 1);
                if (abilityDef.Count > 0)
                {
                    Logger.LogTrace($"Resetting {keyValuePair.Key} {keyValuePair.Value}");
                    // this is the only change - calling internal implementation
                    SetTempPilotSkill(keyValuePair.Key, keyValuePair.Value, sim.GetLevelCost(keyValuePair.Value), abilityDef[0]);
                }
                else
                {
                    Logger.LogTrace($"Resetting {keyValuePair.Key} {keyValuePair.Value}");
                    // this is the only change - calling internal implementation
                    SetTempPilotSkill(keyValuePair.Key, keyValuePair.Value, sim.GetLevelCost(keyValuePair.Value));
                }
            }

            panel.OnValueChangeCB(panel.curPilot);
            //var callback = traverse.Field("OnValueChangeCB").GetValue<UnityAction<Pilot>>();
            //callback?.Invoke(traverse.Field("curPilot").GetValue<Pilot>());
        }

        // modified copy from assembly
        internal static void SetTempPilotSkill(string type, int skill, int expAmount, AbilityDef abilityDef = null)
        {
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            var abilityTree = sim.AbilityTree[type][skill];

            var panel = Resources.FindObjectsOfTypeAll<SGBarracksAdvancementPanel>().First();

            //var traverse = Traverse.Create(panel);

            //    var curPilot = Traverse.Create(panel).Field("curPilot").GetValue<Pilot>();
            var curPilot = panel.curPilot;//traverse.Field("curPilot").GetValue<Pilot>();

            var pilotDef = curPilot.ToPilotDef(true);
            pilotDef.DataManager = sim.DataManager;
            //    var upgradedSkills = Traverse.Create(panel).Field("upgradedSkills").GetValue<OrderedDictionary>();
            var upgradedSkills = panel.upgradedSkills;//traverse.Field("upgradedSkills").GetValue<OrderedDictionary>();

            //    var upgradedPrimarySkills = Traverse.Create(panel).Field("upgradedPrimarySkills").GetValue<List<AbilityDef>>();
            var upgradedPrimarySkills = panel.upgradedPrimarySkills;//traverse.Field("upgradedPrimarySkills").GetValue<List<AbilityDef>>();

            for (var i = 0; i < abilityTree.Count; i++)
            {
                Logger.LogTrace($"Looping {type} {skill}: {abilityTree[i].Id}");
                if (expAmount > 0)
                {
                    //    var skillKey = Traverse.Create(panel).Method("GetSkillKey", type, skill).GetValue<string>();
                    var skillKey = panel.GetSkillKey(type, skill);//traverse.Method("GetSkillKey", type, skill).GetValue<string>());

                    if (!upgradedSkills.Contains(skillKey))
                    {
                        upgradedSkills.Add(skillKey, new KeyValuePair<string, int>(type, skill));
                        Logger.LogTrace($"Add trait {abilityTree[i].Id}");
                    }

                    if (!pilotDef.abilityDefNames.Contains(abilityTree[i].Id) && !abilityTree[i].IsPrimaryAbility && sim.CanPilotTakeAbility(pilotDef, abilityTree[i]))
                    {
                        Logger.LogTrace("SAFETY FALLBACK Add trait " + abilityTree[i].Id);
                        pilotDef.abilityDefNames.Add(abilityTree[i].Id);
                    }
                    var abilityToUse = abilityDef ?? abilityTree[i];
                    Logger.LogTrace($"abilityToUse: {abilityToUse.Id}");
                pilotDef.ForceRefreshAbilityDefs();

                // extra condition blocks skills from being taken at incorrect location

// original before gnivler fix
//                if (expAmount > 0 &&
//                    abilityToUse.ReqSkillLevel == skill + 1 &&
//                    !pilotDef.abilityDefNames.Contains(abilityToUse.Id) &&
//                    sim.CanPilotTakeAbility(pilotDef, abilityToUse))

//gnivler fix, results in assignment of default lvl5 on player reverting lvl6; relies on new method at top

                    if (!pilotDef.abilityDefNames.Contains(abilityToUse.Id) &&
                    sim.CanPilotTakeAbility(pilotDef, abilityToUse) &&
                    !HasExistingAbilityAtTier(pilotDef, abilityToUse))

                    {
                        Logger.LogTrace("Add primary " + abilityToUse.Id);
                        pilotDef.abilityDefNames.Add(abilityToUse.Id);
                        if (abilityToUse.IsPrimaryAbility)
                        {
                            upgradedPrimarySkills.Add(abilityToUse);
                        }
                        else
                        {
                            // still need to add it, even if it can't be a primary at location
                            Logger.LogTrace("Add trait " + abilityToUse.Id);
                            pilotDef.abilityDefNames.Add(abilityToUse.Id);
                        }
                    }

                }
                else
                {
                    //    var skillKey2 = Traverse.Create(panel).Method("GetSkillKey", type, skill).GetValue<string>();
                    var skillKey2 = panel.GetSkillKey(type, skill);//traverse.Method("GetSkillKey", type, skill).GetValue<string>());
                    upgradedSkills.Remove(skillKey2);
                    upgradedPrimarySkills.Remove(abilityTree[i]);

                    Logger.LogTrace($"Removing {skillKey2}: {abilityTree[i].Id}");
                    return;
                }
            }

            Logger.LogTrace("\n");

            pilotDef.ForceRefreshAbilityDefs();


            var pilot = new Pilot(pilotDef, curPilot.GUID, true)
            {
                pilotDef = {DataManager = curPilot.pilotDef.DataManager}
            };
            pilot.FromPilotDef(pilotDef); // added this so RT CD can hook it?

            if (expAmount > 0)
            {
                pilot.SpendExperience(0, "Advancement", (uint)expAmount);
            }

            pilot.ModifyPilotStat_Barracks(0, "Advancement", type, (uint)(skill + 1));
            panel.SetPilot(pilot, false);
            panel.RefreshPanel();
            panel.OnValueChangeCB(pilot);
            //Traverse.Create(panel).Method("RefreshPanel").GetValue();
            //var callback = Traverse.Create(panel).Field("OnValueChangeCB").GetValue<UnityAction<Pilot>>();
            // callback sets the Reset / Confirm buttons states
            //callback.Invoke(pilot);
        }

        //       internal static void PreloadIcons()
        //       {
        //           var dm = UnityGameInstance.BattleTechGame.DataManager;
        //           var loadRequest = dm.CreateLoadRequest();
        //           foreach (var abilityDef in ModAbilities)
        //           {
        //               loadRequest.AddLoadRequest<SVGAsset>(BattleTechResourceType.SVGAsset, abilityDef.Description.Icon, null);
        //           }

        //            loadRequest.ProcessRequests();
        //        }

        //        public static void InsertAbilities()
        //        {
        //            var dm = UnityGameInstance.BattleTechGame.DataManager;
        //            var sim = UnityGameInstance.BattleTechGame.Simulation;

        //            var abilityDefs = Traverse.Create(dm).Field("abilityDefs").Field("items").GetValue<Dictionary<string,AbilityDef>>();


        //            foreach (var abilityDef in ModAbilities)
        //            {
        //                abilityDefs.Add(abilityDef.Id, abilityDef);
        //                var load = new DataManager.DependencyLoadRequest(dm);
        //                abilityDef.GatherDependencies(dm, load, 0);
        //                sim.AbilityTree[abilityDef.ReqSkill.ToString()][abilityDef.ReqSkillLevel].Add(abilityDef);
        //            }
        //Traverse.Create(dm).Field("AbilityDefs").SetValue(abilityDefs);
        //        }
    }
}