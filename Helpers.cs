using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
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
        internal static readonly List<AbilityDef> ModAbilities = new List<AbilityDef>();

        internal static void PopulateAbilities()
        {
            var jsonFiles = new DirectoryInfo(
                Path.Combine(modSettings.modDirectory, "Abilities")).GetFiles().ToList();
            foreach (var file in jsonFiles)
            {
                var abilityDef = new AbilityDef();
                using (var stream = file.OpenRead())
                {
                    var buffer = new byte[stream.Length];
                    var json = "";
                    var encoding = new UTF8Encoding(true);
                    while (stream.Read(buffer, 0, (int) stream.Length) > 0)
                    {
                        json += encoding.GetString(buffer);
                    }

                    abilityDef.FromJSON(json);
                    if (!ModAbilities.Contains(abilityDef))
                    {
                        ModAbilities.Add(abilityDef);
                    }
                    else
                    {
                        Log("Duplicate AbilityDef, id is " + abilityDef.Description.Id);
                    }
                }
            }
        }

        internal static void LogAbilityDictionary()
        {
            foreach (var abilityDef in ModAbilities)
            {
                Log($"{abilityDef.Description.Id} {abilityDef.ReqSkill} {abilityDef.ReqSkillLevel}");
            }
        }

        //internal static void Choose(AbilityDef abilityDef, HBSDOTweenToggle pip)
        //{
        //    try
        //    {
        //        var sim = UnityGameInstance.BattleTechGame.Simulation;
        //        var ability = UnityGameInstance.BattleTechGame.DataManager.AbilityDefs
        //            .Select(x => x.Value)
        //            .First(x => x.Description.Name == abilityName);
        //        ___gutPips[7].Initialize(
        //            "Guts",
        //            7,
        //            sim.GetLevelCost(7),
        //            abilityName != "COOLANT VENT",
        //            Helpers.OnValueClick,
        //            (str, num) => AccessTools.Method(typeof(SGBarracksAdvancementPanel), "OnPipHoverEnter")
        //                .Invoke(__instance, new object[] {typeof(string), typeof(int)}),
        //            (str, num) => AccessTools.Method(typeof(SGBarracksAdvancementPanel), "OnPipHoverExit")
        //                .Invoke(__instance, new object[] {typeof(string), typeof(int)}),
        //            ability,
        //            true);
        //        Traverse.Create(__instance).Method("RefreshPanel").GetValue();
        //        var curPilot = Traverse.Create(__instance).Field("curPilot").GetValue<Pilot>();
        //        if (curPilot.StatCollection.GetValue<int>(type) > value)
        //        {
        //            Traverse.Create(__instance).Method("SetTempPilotSkill", type, value, -sim.GetLevelCost(value), true).GetValue();
        //            Traverse.Create(__instance).Method("ForceResetCharacter").GetValue();
        //            return;
        //        }
        //        Helpers.SetTempPilotSkill(__instance, ability, type, value, sim.GetLevelCost(value));
        //    }
        //    catch (Exception ex)
        //    {
        //        Log(ex);
        //    }
        //}

        internal static void SetTempPilotSkill(AbilityDef abilityDef, string type, int skill, int expAmount, bool updateScreen = true)
        {
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            var panel = Resources.FindObjectsOfTypeAll<SGBarracksAdvancementPanel>().First();
            var curPilot = Traverse.Create(panel).Field("curPilot").GetValue<Pilot>();
            PilotDef pilotDef = curPilot.ToPilotDef(true);
            pilotDef.DataManager = sim.DataManager;
            pilotDef.abilityDefNames.Add(abilityDef.Id);
            pilotDef.ForceRefreshAbilityDefs();
            var upgradedSkills = Traverse.Create(panel).Field("upgradedSkills").GetValue<OrderedDictionary>();
            if (expAmount > 0)
            {
                var upgradedPrimarySkills = Traverse.Create(panel).Field("upgradedPrimarySkills").GetValue<List<AbilityDef>>();
                //if (sim.CanPilotTakeAbility(pilotDef, abilityDef))
                {
                    Log("Adding " + abilityDef.Description.Id);
                    pilotDef.abilityDefNames.Add(abilityDef.Description.Id);
                    upgradedPrimarySkills.Add(abilityDef);
                }

                var skillKey = Traverse.Create(panel).Method("GetSkillKey", type, skill).GetValue<string>();
                if (!upgradedSkills.Contains(skillKey))
                {
                    upgradedSkills.Add(skillKey, new KeyValuePair<string, int>(type, skill));
                }
            }
            else if (expAmount < 0)
            {
                var skillKey2 = Traverse.Create(panel).Method("GetSkillKey", type, skill).GetValue<string>();
                if (upgradedSkills.Contains(skillKey2))
                {
                    upgradedSkills.Remove(skillKey2);
                }

                return;
            }

            pilotDef.ForceRefreshAbilityDefs();
            var pilot = new Pilot(pilotDef, curPilot.GUID, true);
            pilot.pilotDef.DataManager = curPilot.pilotDef.DataManager;
            if (expAmount > 0)
            {
                pilot.SpendExperience(0, "Advancement", (uint) expAmount);
            }

            pilot.ModifyPilotStat_Barracks(0, "Advancement", type, (uint) (skill + 1));
            panel.SetPilot(pilot);
            Traverse.Create(panel).Method("RefreshPanel").GetValue();
            var callback = Traverse.Create(panel).Field("OnValueChangeCB").GetValue<UnityAction<Pilot>>();
            if (updateScreen)
            {
                Log("callback " + callback.Method);
                //callback?.Invoke(pilot);
            }
        }

        internal static void PreloadIcons()
        {
            var dm = UnityGameInstance.BattleTechGame.DataManager;
            var loadRequest = dm.CreateLoadRequest();
            foreach (var abilityDef in ModAbilities)
            {
                loadRequest.AddLoadRequest<SVGAsset>(BattleTechResourceType.SVGAsset, abilityDef.Description.Icon, null);
            }

            loadRequest.ProcessRequests();
        }

        public static void InsertAbilities()
        {
            var dm = UnityGameInstance.BattleTechGame.DataManager;
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            var abilityDefs = Traverse.Create(dm).Field("abilityDefs").GetValue<DictionaryStore<AbilityDef>>();
            foreach (var abilityDef in ModAbilities)
            {
                abilityDefs.Add(abilityDef.Id, abilityDef);
                var load = new DataManager.DependencyLoadRequest(dm);
                abilityDef.GatherDependencies(dm, load, 0);
                sim.AbilityTree[abilityDef.ReqSkill.ToString()][abilityDef.ReqSkillLevel].Add(abilityDef);
            }
        }
    }
}
