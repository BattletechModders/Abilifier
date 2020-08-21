using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using BattleTech.UI.Tooltips;
using Harmony;
using SVGImporter;
using static Abilifier.Mod;

// ReSharper disable InconsistentNaming

namespace Abilifier
{
    public class Patches
    {

        //SetPips patch so that Icons for non-taken abilities do not show up after 3 primary abilities taken, AND to ensure that icons for TAKEN abilities DO show up
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

                    //this is the pertinent change, which checks if pilot has ANY ability of the correct type and level, and sets it to be visible if true
                    var type = pips[idx].Ability.ReqSkill; 
                    var abilityDefs = ___curPilot.pilotDef.AbilityDefs.Where(x => x.ReqSkill == type
                    && x.ReqSkillLevel == idx + 1 && x.IsPrimaryAbility == true);
                    bool flag4 = abilityDefs.Any();

                    pips[idx].Set(purchaseState, (curSkill == idx || curSkill == idx + 1) && !isLocked, curSkill == idx, needsXP, isLocked && flag);
                    pips[idx].SetActiveAbilityVisible(flag2 || flag3 || flag4);
                }
                pips[idx].Set(purchaseState, (curSkill == idx || curSkill == idx + 1) && !isLocked, curSkill == idx, needsXP, isLocked && flag);
                return false;
            }
        }


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
                
                //loop through abilities the pilot has, and place those ability icons/tooltips in the appropriate pip slot.
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
                    //    Traverse.Create(__instance).Method("ForceResetCharacter").GetValue();
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

                    //new code below//
                    //new code below for ability requirements
                    List<string> pilotAbilityDefNames = ___curPilot.pilotDef.abilityDefNames;
                    
                    var abilityFilter = modSettings.abilityReqs.Values.SelectMany(x => x).ToList();

                    List<AbilityDef> abilitiesWithReqs = abilityDefs.Where(ability => abilityFilter.Any(filter => filter.Equals(ability.Id))).ToList();

                    var abilityDefsForDesc = new List<AbilityDef>();
                    abilityDefsForDesc.AddRange(abilityDefs);
                    foreach (var abilityWithReq in abilitiesWithReqs)
                    {
                        if (!pilotAbilityDefNames.Contains(modSettings.abilityReqs.FirstOrDefault(x => x.Value.Contains(abilityWithReq.Id)).Key))
                        {
                            abilityDefs.Remove(abilityWithReq);
                        }
                    }

                    //original code continues below//
                    string abilityDescs = null;
                        foreach (var abilityDefDesc in abilityDefsForDesc)
                        {
                            if (abilityDefs.Contains(abilityDefDesc))
                            {
                                string abilityID = abilityDefDesc.Id + "Desc";
                                string abilityName = abilityDefDesc.Description.Name;
                                if (modSettings.usePopUpsForAbilityDesc == true)
                                {
                                    abilityDescs += "[[DM.BaseDescriptionDefs[" + abilityID + "],<b>" + abilityName + "</b>]]" + "\n\n";
                                }
                                else
                                {
                                    abilityDescs += "<color=#33f9ff>" + abilityDefDesc.Description.Name + ": </color>" + abilityDefDesc.Description.Details + "\n\n";
                                }
                            }
                            else
                            {
                                var dm = UnityGameInstance.BattleTechGame.DataManager;
                                string abilityID = abilityDefDesc.Id + "Desc";
                                string abilityName = abilityDefDesc.Description.Name;

                                var reqAbilityName = modSettings.abilityReqs.FirstOrDefault(x => x.Value.Contains(abilityDefDesc.Id)).Key;
                                var allAbilities = new List<AbilityDef>();

                                allAbilities = sim.AbilityTree[type].SelectMany(x => x.Value).ToList();
                            // allAbilities.AddRange(Traverse.Create(dm).Field("abilityDefs").GetValue<List<AbilityDef>>());

                            var reqAbility = allAbilities.Find(x => x.Id == reqAbilityName);


                                if (modSettings.usePopUpsForAbilityDesc == true)
                                {
                                    //abilityDescs += "<color=#FF0000>(Requirements Unmet)</color> " + "[[DM.BaseDescriptionDefs[" + abilityID + "],<b>" + abilityName + "</b>]]" + "\n\n";
                                    abilityDescs += "<color=#FF0000> Requires <u>" + reqAbility.Description.Name + "</u></color> " + "[[DM.BaseDescriptionDefs[" + abilityID + "],<b>" + abilityName + "</b>]]" + "\n\n";
                                }
                                else
                                {
                                    //abilityDescs += "<color=#FF0000>(Requirements Unmet)</color> " + "<color=#0000FF>" + abilityDefDesc.Description.Name + ": </color>" + abilityDefDesc.Description.Details + "\n\n";
                                    abilityDescs += "<color=#FF0000> Requires <u>"+reqAbility.Description.Name+"</u></color> " + "<color=#33f9ff>" + abilityDefDesc.Description.Name + ": </color>" + abilityDefDesc.Description.Details + "\n\n";
                                }
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

        //this patch should hopefuly prevent AI generated (hiring hall) pilots from having too many abilities
        [HarmonyPatch(typeof(PilotGenerator), "SetPilotAbilities")]
        public static class PilotGenerator_SetPilotAbilities_Patch
        {
            public static bool Prefix(PilotGenerator __instance, PilotDef pilot, string type, int value)
            {
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                value--;
                if (value < 0)
                {
                    return false;
                }
                if (!sim.AbilityTree.ContainsKey(type))
                {
                    return false;
                }
                if (sim.AbilityTree[type].Count <= value)
                {
                    return false;
                }

                List<AbilityDef> list = sim.AbilityTree[type][value];

                if (list.Count == 0)
                {
                    return false;
                }

                else
                {
                    List<AbilityDef> listAbilities = list.FindAll(x => x.IsPrimaryAbility == true);//get primary abilities

                    List<string> pilotAbilityDefNames = pilot.abilityDefNames;
                    var abilityFilter = modSettings.abilityReqs.Values.SelectMany(x => x).ToList();

                    List<AbilityDef> abilitiesWithReqs = listAbilities.Where(ability => abilityFilter.Any(filter => filter.Equals(ability.Id))).ToList();

                    foreach (var abilityWithReq in abilitiesWithReqs)
                    {
                        if (!pilotAbilityDefNames.Contains(modSettings.abilityReqs.FirstOrDefault(x => x.Value.Contains(abilityWithReq.Id)).Key))
                        {
                            listAbilities.Remove(abilityWithReq);
                        }
                    }

                    List<AbilityDef> listTraits = list.FindAll(x => x.IsPrimaryAbility != true);//need to keep all traits
                    if (listAbilities.Count > 0)
                    {
                        int idx = UnityEngine.Random.Range(0, listAbilities.Count);//pick a random primary of the options
                        listTraits.Add(listAbilities[idx]);//add only that random primary
                    }
                    pilot.DataManager = sim.DataManager;
                    pilot.ForceRefreshAbilityDefs();
                    for (int i = 0; i < listTraits.Count; i++)
                    {
                        if (sim.CanPilotTakeAbility(pilot, listTraits[i], false))
                        {
                            pilot.abilityDefNames.Add(listTraits[i].Description.Id);
                        }
                    }
                    pilot.ForceRefreshAbilityDefs();
                    return false;
                }
                
            }
        }
        [HarmonyPatch(typeof(SimGameState), "CanPilotTakeAbility")]
        [HarmonyAfter(new string[] { "io.github.mpstark.AbilityRealizer" })]
        public static class SimGameState_CanPilotTakeAbility_Patch
        {
            public static bool Prefix(SimGameState __instance, ref bool __result, PilotDef p, AbilityDef newAbility, bool checkSecondTier = false)
            {
                if (!newAbility.IsPrimaryAbility)
                {
                    __result = true;
                    return false;
                }
                List<AbilityDef> primaryPilotAbilities = SimGameState.GetPrimaryPilotAbilities(p);
                if (primaryPilotAbilities == null)
                {
                    __result = true;
                    return false;
                }
                if (primaryPilotAbilities.Count >= 3 + modSettings.extraAbilities) //change max allowed abilities for pilot to take when lvling up
                {
                    __result = false;
                    return false;
                }
                Dictionary<SkillType, int> sortedSkillCount = __instance.GetSortedSkillCount(p);
                __result = (sortedSkillCount.Count <= 1 + modSettings.extraFirstTierAbilities
                    || sortedSkillCount.ContainsKey(newAbility.ReqSkill))
                    && (!sortedSkillCount.ContainsKey(newAbility.ReqSkill) || sortedSkillCount[newAbility.ReqSkill] <= 1 + modSettings.extraAbilitiesAllowedPerSkill)
                    && (!checkSecondTier || sortedSkillCount.ContainsKey(newAbility.ReqSkill) || primaryPilotAbilities.Count <= 1 + modSettings.extraAbilitiesAllowedPerSkill);                 //change max # abilities per-skill type (default is 2, so only allowed to take if currently have <=1)
                //       && (modSettings.skillLockThreshold <= 0 || ((primaryPilotAbilities.Count == 2 + modSettings.extraAbilities) ? (newAbility.ReqSkillLevel > modSettings.skillLockThreshold && sortedSkillCount[newAbility.ReqSkill] == 1 + modSettings.extraAbilitiesAllowedPerSkill) : true));//added part for skill locking?

                if (modSettings.skillLockThreshold > 0) //section allows you to set a threshold the "locks" the pilot into taking only abilities within that skill once the threshold has been reached.
                {
                    if (p.SkillGunnery >= modSettings.skillLockThreshold && newAbility.ReqSkill != SkillType.Gunnery)
                        __result = false;
                    if (p.SkillPiloting >= modSettings.skillLockThreshold && newAbility.ReqSkill != SkillType.Piloting)
                        __result = false;
                    if (p.SkillGuts >= modSettings.skillLockThreshold && newAbility.ReqSkill != SkillType.Guts)
                        __result = false;
                    if (p.SkillTactics >= modSettings.skillLockThreshold && newAbility.ReqSkill != SkillType.Tactics)
                        __result = false;
                }

                return false;
            }
        }
    }
}