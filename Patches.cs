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
using UnityEngine.Events;

using System.Reflection;

// ReSharper disable InconsistentNaming

namespace Abilifier
{
    public class Patches
    {

        [HarmonyPatch(typeof(SGBarracksSkillPip), "Initialize")]
        [HarmonyBefore(new string[] { "io.github.mpstark.AbilityRealizer" })]
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

        [HarmonyPatch(typeof(SGBarracksAdvancementPanel), "SetPips")]
        public static class SGBarracksAdvancementPanel_SetPips_Patch
        {
            public static bool Prefix(SGBarracksAdvancementPanel __instance,
                List<SGBarracksSkillPip> pips, int originalSkill, int curSkill, int idx, bool needsXP, bool isLocked, Pilot ___curPilot)

            {
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                SGBarracksSkillPip.PurchaseState purchaseState = SGBarracksSkillPip.PurchaseState.Unselected;
                if (originalSkill > idx)
                {
                    purchaseState = SGBarracksSkillPip.PurchaseState.Purchased;
                }
                else if (curSkill > idx)
                {
                    purchaseState = SGBarracksSkillPip.PurchaseState.Selected;
                }
                bool flag = false;
                if (isLocked && (idx + 1 == pips.Count || curSkill == idx + 1))
                {
                    flag = true;
                }
                if (pips[idx].Ability != null)
                {
                    bool flag2 = sim.CanPilotTakeAbility(___curPilot.pilotDef, pips[idx].Ability, pips[idx].SecondTierAbility);
                    bool flag3 = ___curPilot.pilotDef.abilityDefNames.Contains(pips[idx].Ability.Description.Id);
                    //                    bool flag3 = ___curPilot.pilotDef.abilityDefNames.Contains(pips[idx].Ability.Description.Id);

                    //think this might work, although may need to figure out how to get Traverse to run each time
                    var type = pips[idx].Ability.ReqSkill; //maybe try Ability from pip
                    var abilityDefs = ___curPilot.pilotDef.AbilityDefs.Where(x => x.ReqSkill == type
                    && x.ReqSkillLevel == idx + 1 && x.IsPrimaryAbility == true); ;
                    //                   bool flag4 = false;
                    bool flag4 = abilityDefs.Any();



                    pips[idx].Set(purchaseState, (curSkill == idx || curSkill == idx + 1) && !isLocked, curSkill == idx, needsXP, isLocked && flag);
                    pips[idx].SetActiveAbilityVisible(flag2 || flag3 || flag4);
                }
                pips[idx].Set(purchaseState, (curSkill == idx || curSkill == idx + 1) && !isLocked, curSkill == idx, needsXP, isLocked && flag);
                return false;
            }
        }

            //////////////////////////////////////working below///////////////////////////////////////////////
        [HarmonyPatch(typeof(SGBarracksMWDetailPanel), "SetPilot")]

        public static class SGBarracksMWDetailPanel_SetPilot_Patch
        {
            public static void Postfix(SGBarracksMWDetailPanel __instance,
                Pilot p,
                SGBarracksAdvancementPanel ___advancement
                )
            {
                var sim = UnityGameInstance.BattleTechGame.Simulation;


                var gunPips = Traverse.Create(___advancement).Field("gunPips").GetValue<List<SGBarracksSkillPip>>();
                var pilotPips = Traverse.Create(___advancement).Field("pilotPips").GetValue<List<SGBarracksSkillPip>>();
                var gutPips = Traverse.Create(___advancement).Field("gutPips").GetValue<List<SGBarracksSkillPip>>();
                var tacPips = Traverse.Create(___advancement).Field("tacPips").GetValue<List<SGBarracksSkillPip>>();




                var abilityDefs = p.pilotDef.AbilityDefs.Where(x => x.IsPrimaryAbility==true);
                
                foreach(AbilityDef ability in abilityDefs)
                {
                    if (ability.ReqSkill == SkillType.Gunnery)
                    {
                        Traverse.Create(gunPips[ability.ReqSkillLevel-1]).Field("abilityIcon").GetValue<SVGImage>().vectorGraphics = ability.AbilityIcon;
                        Traverse.Create(gunPips[ability.ReqSkillLevel - 1]).Field("AbilityTooltip").GetValue<HBSTooltip>()
                            .SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(ability.Description));
                    }
                }

                foreach (AbilityDef ability in abilityDefs)
                {
                    if (ability.ReqSkill == SkillType.Piloting)
                    {
                        Traverse.Create(pilotPips[ability.ReqSkillLevel - 1]).Field("abilityIcon").GetValue<SVGImage>().vectorGraphics = ability.AbilityIcon;
                        Traverse.Create(pilotPips[ability.ReqSkillLevel - 1]).Field("AbilityTooltip").GetValue<HBSTooltip>()
                            .SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(ability.Description));
                    }
                }

                foreach (AbilityDef ability in abilityDefs)
                {
                    if (ability.ReqSkill == SkillType.Guts)
                    {
                        Traverse.Create(gutPips[ability.ReqSkillLevel - 1]).Field("abilityIcon").GetValue<SVGImage>().vectorGraphics = ability.AbilityIcon;
                        Traverse.Create(gutPips[ability.ReqSkillLevel - 1]).Field("AbilityTooltip").GetValue<HBSTooltip>()
                            .SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(ability.Description));
                    }
                }

                foreach (AbilityDef ability in abilityDefs)
                {
                    if (ability.ReqSkill == SkillType.Tactics)
                    {
                        Traverse.Create(tacPips[ability.ReqSkillLevel - 1]).Field("abilityIcon").GetValue<SVGImage>().vectorGraphics = ability.AbilityIcon;
                        Traverse.Create(tacPips[ability.ReqSkillLevel - 1]).Field("AbilityTooltip").GetValue<HBSTooltip>()
                            .SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(ability.Description));
                    }
                }


            }
        }

        // rewrite of original
        [HarmonyPatch(typeof(SGBarracksAdvancementPanel), "OnValueClick")]
        [HarmonyBefore(new string[] { "io.github.mpstark.AbilityRealizer" })]
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
                    if (modSettings.debugXP == true)
                    {
                        ___curPilot.AddExperience(0, "", 100000);
                    }

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

                    //                    var abilityDefs = Helpers.ModAbilities
                    //                        .Where(x => x.ReqSkillLevel == value + 1 && x.ReqSkill.ToString() == type).ToList();

                    
                    var abilityDictionaries = sim.AbilityTree.Select(x => x.Value).ToList();

                    //List<AbilityDef> abilityDefs = sim.GetAbilityDefFromTree(type, value); //does same thing as below?
                    var abilityDefs = new List<AbilityDef>();
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
                    string abilityDescs = null;
                    foreach (var abilityDef in abilityDefs)
                    {
                        string abilityID = abilityDef.Id + "Desc";
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