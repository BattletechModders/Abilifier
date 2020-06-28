using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using BattleTech.UI.Tooltips;
using Harmony;
using SVGImporter;
using static Abilifier.Mod;
using HBS.Data;

// ReSharper disable InconsistentNaming

namespace Abilifier
{
    public class Patches
    {
        [HarmonyPatch(typeof(SGBarracksAdvancementPanel), "Initialize")]
        public static class SGBarracksAdvancementPanel_Initialize_Patch
        {
            public static void Prefix()
            {
                try
                {
                    Helpers.PreloadIcons();
                    Helpers.InsertAbilities();
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        [HarmonyPatch(typeof(SGBarracksSkillPip), "Initialize")]
        public static class SGBarracksSkillPip_Initialize_Patch
        {
            public static void Prefix(int index, ref AbilityDef ability)
            {
                // prevent Ability icons appearing at non-tier locations
                // the actual AbilityDef is going to be provided later anyway
                if (index != 4 && index != 7 && ability != null)
                {
                    Trace($"nulling {ability.ReqSkill}|{index}");
                    ability = null;
                }
            }
        }

        // rewrite of original
        [HarmonyPatch(typeof(SGBarracksAdvancementPanel), "OnValueClick")]
        public static class SGBarracksAdvancementPanel_OnValueClick_Patch
        {
            public static bool Prefix(
                SGBarracksAdvancementPanel __instance,
                Pilot ___curPilot,
                List<SGBarracksSkillPip> ___gunPips,
                List<SGBarracksSkillPip> ___pilotPips,
                List<SGBarracksSkillPip> ___gutPips,
                List<SGBarracksSkillPip> ___tacPips,
                string type,
                int value)
            {
                try
                {
                    ___curPilot.AddExperience(0, "", 100000);

                    var sim = UnityGameInstance.BattleTechGame.Simulation;
                    var pips = new Dictionary<string, List<SGBarracksSkillPip>>
                    {
                        {"Gunnery", ___gunPips},
                        {"Piloting", ___pilotPips},
                        {"Guts", ___gutPips},
                        {"Tactics", ___tacPips},
                    };

                    // removal of pip
                    if (___curPilot.StatCollection.GetValue<int>(type) > value)
                    {
                        Trace($"Removing {type} {value}");
                        Trace(pips[type][value].Ability);
                        Helpers.SetTempPilotSkill(type, value, -sim.GetLevelCost(value));
                        ___curPilot.pilotDef.abilityDefNames.Do(Trace);
                        Log("\n");
                        Helpers.ForceResetCharacter(__instance);
                        Traverse.Create(__instance).Method("ForceResetCharacter").GetValue();
                        return false;
                    }

                    // add non-ability pip
                    if (!Traverse.Create(pips[type][value]).Field("hasAbility").GetValue<bool>())
                    {
                        Trace("Non-ability pip");
                        Helpers.SetTempPilotSkill(type, value, sim.GetLevelCost(value));
                        ___curPilot.pilotDef.abilityDefNames.Do(Trace);
                        Trace("\n");
                        return false;
                    }

                    // Ability pips
                    // build complete list of defs from HBS and imported json

                    var abilityDefs = Helpers.ModAbilities
                        .Where(x => x.ReqSkillLevel == value + 1 && x.ReqSkill.ToString() == type).ToList();

                    var abilityDictionaries = sim.AbilityTree.Select(x => x.Value).ToList();
                    foreach (var abilityDictionary in abilityDictionaries)
                    {
                        abilityDefs.AddRange(abilityDictionary[value].Where(x => x.ReqSkill.ToString() == type));
                    }

                    // don't create choice popups with 1 option
                    if (abilityDefs.Count <= 1)
                    {
                        Trace($"Single ability for {type}|{value}, skipping");
                        Helpers.SetTempPilotSkill(type, value, sim.GetLevelCost(value));
                        return false;
                    }

                    // prevents which were ability buttons before other primaries were selected from being abilities
                    // not every ability button is visible all the time
                    var curButton = Traverse.Create(pips[type][value]).Field("curButton").GetValue<HBSDOTweenToggle>();
                    var skillButton = Traverse.Create(pips[type][value]).Field("skillButton").GetValue<HBSDOTweenToggle>();
                    if (curButton != skillButton)
                    {
                        Trace(new string('=', 50));
                        Trace("curButton != skillButton");
                        Helpers.SetTempPilotSkill(type, value, sim.GetLevelCost(value));
                        return false;
                    }

                    // dynamic buttons based on available abilities
                    string abilityDescs=null;
                    foreach (var abilityDef in abilityDefs)
                    {
                        string abilityID = abilityDef.Id+"Desc";
                        string abilityName = abilityDef.Description.Name;
                        if (Mod.modSettings.usePopUpsForAbilityDesc == true)
                        {
                            abilityDescs += "[[DM.BaseDescriptionDefs[" + abilityID + "],<b>" + abilityName + "</b>]]" + "\n\n";
                        }
                        else
                        {
                            abilityDescs += abilityDef.Description.Name + ": " + abilityDef.Description.Details + "\n\n";
                        }
                    }
                    //    [[DM.BaseDescriptionDefs[TBoneLoreRoberts],Roberts]]
                    var popup = GenericPopupBuilder
                        .Create("Select an ability",
                        abilityDescs)
                        .AddFader();
                    popup.AlwaysOnTop = true;
                    var pip = pips[type][value];
                    foreach (var abilityDef in abilityDefs)
                    {
                        popup.AddButton(abilityDef.Description.Name,
                            () =>
                            {
                                // have to change the Ability so SetPips later, SetActiveAbilityVisible works
                                Traverse.Create(pip).Field("thisAbility").SetValue(abilityDef);
                                Traverse.Create(pip).Field("abilityIcon").GetValue<SVGImage>().vectorGraphics = abilityDef.AbilityIcon;
                                Traverse.Create(pip).Field("AbilityTooltip").GetValue<HBSTooltip>()
                                    .SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(abilityDef.Description));
                                Helpers.SetTempPilotSkill(type, value, sim.GetLevelCost(value), abilityDef);
                            });
                    }

                    popup.Render();
                }
                catch (Exception ex)
                {
                    Log(ex);
                }

                return false;
            }
        }
    }
}