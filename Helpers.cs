using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using Harmony;
using HBS.Data;
using SVGImporter;
using UnityEngine;
using UnityEngine.Events;
using static Abilifier.Mod;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming

namespace Abilifier
{
    public class Helpers
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
        public static Pilot selectedPilot;
        internal static void ForceResetCharacter(SGBarracksAdvancementPanel panel)
        {
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            var traverse = Traverse.Create(panel);
            var orderedDictionary = traverse.Field("upgradedSkills").GetValue<OrderedDictionary>();
            panel.SetPilot(traverse.Field("basePilot").GetValue<Pilot>(), true);
            foreach (var obj in orderedDictionary.Values)
            {
                var keyValuePair = (KeyValuePair<string, int>)obj;
                Trace($"Resetting {keyValuePair.Key} {keyValuePair.Value}");
                // this is the only change - calling internal implementation
                SetTempPilotSkill(keyValuePair.Key, keyValuePair.Value, sim.GetLevelCost(keyValuePair.Value));
            }

            var callback = traverse.Field("OnValueChangeCB").GetValue<UnityAction<Pilot>>();
            callback?.Invoke(traverse.Field("curPilot").GetValue<Pilot>());
        }

        // modified copy from assembly
        internal static void SetTempPilotSkill(string type, int skill, int expAmount, AbilityDef abilityDef = null)
        {
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            var abilityTree = sim.AbilityTree[type][skill];
            var panel = Resources.FindObjectsOfTypeAll<SGBarracksAdvancementPanel>().First();

            var traverse = Traverse.Create(panel);
            
        //    var curPilot = Traverse.Create(panel).Field("curPilot").GetValue<Pilot>();
            var curPilot = traverse.Field("curPilot").GetValue<Pilot>();
            
            var pilotDef = curPilot.ToPilotDef(true);
            pilotDef.DataManager = sim.DataManager;
        //    var upgradedSkills = Traverse.Create(panel).Field("upgradedSkills").GetValue<OrderedDictionary>();
            var upgradedSkills = traverse.Field("upgradedSkills").GetValue<OrderedDictionary>();
            
        //    var upgradedPrimarySkills = Traverse.Create(panel).Field("upgradedPrimarySkills").GetValue<List<AbilityDef>>();
            var upgradedPrimarySkills = traverse.Field("upgradedPrimarySkills").GetValue<List<AbilityDef>>();
            
            for (var i = 0; i < abilityTree.Count; i++)
            {
                Trace($"Looping {type} {skill}: {abilityTree[i].Id}");
                if (expAmount > 0)
                {
                //    var skillKey = Traverse.Create(panel).Method("GetSkillKey", type, skill).GetValue<string>();
                    var skillKey = traverse.Method("GetSkillKey", type, skill).GetValue<string>();
                    
                    if (!upgradedSkills.Contains(skillKey))
                    {
                        upgradedSkills.Add(skillKey, new KeyValuePair<string, int>(type, skill));
                        Trace($"Add trait {abilityTree[i].Id}");
                    }
                }
                else
                {
                //    var skillKey2 = Traverse.Create(panel).Method("GetSkillKey", type, skill).GetValue<string>();
                    var skillKey2 = traverse.Method("GetSkillKey", type, skill).GetValue<string>();
                    upgradedSkills.Remove(skillKey2);
                    upgradedPrimarySkills.Remove(abilityTree[i]);
                    Trace($"Removing {skillKey2}: {abilityTree[i].Id}");
                    return;
                }

                var abilityToUse = abilityDef ?? abilityTree[i];
                Trace($"abilityToUse: {abilityToUse.Id}");
                pilotDef.ForceRefreshAbilityDefs();
                // extra condition blocks skills from being taken at incorrect location
                if (expAmount > 0 &&
                    abilityToUse.ReqSkillLevel == skill + 1 &&
                    !pilotDef.abilityDefNames.Contains(abilityToUse.Id) &&
                    sim.CanPilotTakeAbility(pilotDef, abilityToUse))
                {
                    Trace("Add primary " + abilityToUse.Id);
                    pilotDef.abilityDefNames.Add(abilityToUse.Id);
                    if (abilityToUse.IsPrimaryAbility)
                    {
                        upgradedPrimarySkills.Add(abilityToUse);
                    }
                    else
                    {
                        // still need to add it, even if it can't be a primary at location
                        Trace("Add trait " + abilityToUse.Id);
                        pilotDef.abilityDefNames.Add(abilityToUse.Id);
                    }
                }
            }

            Trace("\n");

            pilotDef.ForceRefreshAbilityDefs();
            var pilot = new Pilot(pilotDef, curPilot.GUID, true);
            pilot.pilotDef.DataManager = curPilot.pilotDef.DataManager;
            if (expAmount > 0)
            {
                pilot.SpendExperience(0, "Advancement", (uint)expAmount);
            }

            pilot.ModifyPilotStat_Barracks(0, "Advancement", type, (uint)(skill + 1));
            panel.SetPilot(pilot, false);
            Traverse.Create(panel).Method("RefreshPanel").GetValue();
            var callback = Traverse.Create(panel).Field("OnValueChangeCB").GetValue<UnityAction<Pilot>>();
            // callback sets the Reset / Confirm buttons states
            callback.Invoke(pilot);
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