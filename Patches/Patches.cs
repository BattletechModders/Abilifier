using System;
using System.Collections.Generic;
using System.Linq;
using Abilifier.Framework;
using BattleTech;
using BattleTech.Save;
using BattleTech.UI;
using BattleTech.UI.Tooltips;
using Harmony;
using SVGImporter;
using UnityEngine;
using static Abilifier.Mod;
using Logger = Abilifier.Framework.Logger;

// ReSharper disable InconsistentNaming

namespace Abilifier.Patches
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
                var flag = isLocked && (idx + 1 == pips.Count || curSkill == idx + 1);
                if (pips[idx].Ability != null)
                {
                    var flag2 = Helpers.CanPilotTakeAbilityPip(sim, ___curPilot.pilotDef, pips[idx].Ability, pips[idx].SecondTierAbility);
                    var flag3 = ___curPilot.pilotDef.abilityDefNames.Contains(pips[idx].Ability.Description.Id);

                    //this is the pertinent change, which checks if pilot has ANY ability of the correct type and level, and sets it to be visible if true
                    var type = pips[idx].Ability.ReqSkill;
                    var abilityDefs = ___curPilot.pilotDef.AbilityDefs.Where(x => x.ReqSkill == type
                        && x.ReqSkillLevel == idx + 1 && x.IsPrimaryAbility);
                    var flag4 = abilityDefs.Any();

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
                var gunPips = Traverse.Create(___advancement).Field("gunPips").GetValue<List<SGBarracksSkillPip>>();
                var pilotPips = Traverse.Create(___advancement).Field("pilotPips").GetValue<List<SGBarracksSkillPip>>();
                var gutPips = Traverse.Create(___advancement).Field("gutPips").GetValue<List<SGBarracksSkillPip>>();
                var tacPips = Traverse.Create(___advancement).Field("tacPips").GetValue<List<SGBarracksSkillPip>>();


                var abilityDefs = p.pilotDef.AbilityDefs.Where(x => x.IsPrimaryAbility).ToArray();

                //loop through abilities the pilot has, and place those ability icons/tooltips in the appropriate pip slot.
                foreach (var ability in abilityDefs)
                {
                    if (ability.ReqSkill == SkillType.Gunnery)
                    {
                        Traverse.Create(gunPips[ability.ReqSkillLevel - 1]).Field("abilityIcon").GetValue<SVGImage>().vectorGraphics = ability.AbilityIcon;
                        Traverse.Create(gunPips[ability.ReqSkillLevel - 1]).Field("AbilityTooltip").GetValue<HBSTooltip>()
                            .SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(ability.Description));
                    }
                }

                foreach (var ability in abilityDefs)
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
                    if (modSettings.debugXP)
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
                        Logger.LogTrace($"Removing {type} {value}");
                        Logger.LogTrace($"{pips[type][value].Ability}");
                        Helpers.SetTempPilotSkill(type, value, -sim.GetLevelCost(value));
                        ___curPilot.pilotDef.abilityDefNames.Do(Logger.LogTrace);
                        Logger.LogTrace("\n");
                        Helpers.ForceResetCharacter(__instance);
                        //    Traverse.Create(__instance).Method("ForceResetCharacter").GetValue();
                        return false;
                    }

                    // add non-ability pip
                    if (!Traverse.Create(pips[type][value]).Field("hasAbility").GetValue<bool>())
                    {
                        Logger.LogTrace("Non-ability pip");
                        Helpers.SetTempPilotSkill(type, value, sim.GetLevelCost(value));
                        ___curPilot.pilotDef.abilityDefNames.Do(Logger.LogTrace);
                        Logger.LogTrace("\n");
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
                        Logger.LogTrace($"Single ability for {type}|{value}, skipping");
                        Helpers.SetTempPilotSkill(type, value, sim.GetLevelCost(value));
                        return false;
                    }

                    // prevents which were ability buttons before other primaries were selected from being abilities
                    // not every ability button is visible all the time
                    var curButton = Traverse.Create(pips[type][value]).Field("curButton").GetValue<HBSDOTweenToggle>();
                    var skillButton = Traverse.Create(pips[type][value]).Field("skillButton").GetValue<HBSDOTweenToggle>();
                    if (curButton != skillButton)
                    {
                        Logger.LogTrace(new string('=', 50));
                        Logger.LogTrace("curButton != skillButton");
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
                        if (abilitiesWithReqs.Count > 0)
                        {
                            foreach (var abilityWithReq in abilitiesWithReqs)
                            {
                                if (!pilotAbilityDefNames.Contains(modSettings.abilityReqs
                                    .FirstOrDefault(x => x.Value.Contains(abilityWithReq.Id)).Key))
                                {
                                    abilityDefs.Remove(abilityWithReq);
                                }
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
                            if (modSettings.usePopUpsForAbilityDesc)
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
                            var abilityID = abilityDefDesc.Id + "Desc";
                            var abilityName = abilityDefDesc.Description.Name;

                            var reqAbilityName = modSettings.abilityReqs.FirstOrDefault(x => x.Value.Contains(abilityDefDesc.Id)).Key;

                            sim.DataManager.AbilityDefs.TryGet(reqAbilityName, out var reqAbility);

                            if (modSettings.usePopUpsForAbilityDesc)
                            {
                                //abilityDescs += "<color=#FF0000>(Requirements Unmet)</color> " + "[[DM.BaseDescriptionDefs[" + abilityID + "],<b>" + abilityName + "</b>]]" + "\n\n";
                                abilityDescs += "<color=#FF0000> Requires <u>" + reqAbility.Description.Name + "</u></color> " + "[[DM.BaseDescriptionDefs[" + abilityID + "],<b>" + abilityName + "</b>]]" + "\n\n";
                            }
                            else
                            {
                                //abilityDescs += "<color=#FF0000>(Requirements Unmet)</color> " + "<color=#0000FF>" + abilityDefDesc.Description.Name + ": </color>" + abilityDefDesc.Description.Details + "\n\n";
                                abilityDescs += "<color=#FF0000> Requires <u>" + reqAbility.Description.Name + "</u></color> " + "<color=#33f9ff>" + abilityDefDesc.Description.Name + ": </color>" + abilityDefDesc.Description.Details + "\n\n";
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
                    modLog.LogMessage(ex.Message);
                }

                return false;
            }

        }

        //lets try and see about displaying all available avbilities at a given level?
        [HarmonyPatch(typeof(SGBarracksSkillPip), "Set",
            new Type[]
            {
                typeof(SGBarracksSkillPip.PurchaseState), typeof(bool), typeof(bool), typeof(bool), typeof(bool)
            })]

        public static class SGBarracksSkillPip_Set_Patch
        {
            public static void Postfix(SGBarracksSkillPip __instance, SGBarracksSkillPip.PurchaseState purchaseState,
                bool canClick, bool showXP, bool needXP, bool isLocked, int ___idx, string ___type, HBSTooltip ___AbilityTooltip)
            {
                if (purchaseState == SGBarracksSkillPip.PurchaseState.Unselected)
                {
                    var sim = UnityGameInstance.BattleTechGame.Simulation;
                    var abilityDictionaries = sim.AbilityTree.Select(x => x.Value).ToList();

                    var abilityDefs = new List<AbilityDef>();
                    foreach (var abilityDictionary in abilityDictionaries)
                    {
                        abilityDefs.AddRange(abilityDictionary[___idx].Where(x => x.ReqSkill.ToString() == ___type));
                    }

                    var title = $"{___type}: Level {___idx+1} Ability Options";

                    var desc = "";
                    

                    foreach (var ability in abilityDefs)
                    {
                        var abilityFilter = modSettings.abilityReqs.Values.SelectMany(x => x).ToList();

                        List<AbilityDef> abilitiesWithReqs = abilityDefs.Where(x => abilityFilter.Any(y => y.Equals(x.Id))).ToList();

                        if (abilitiesWithReqs.Contains(ability))
                        {
                            var reqAbilityName = modSettings.abilityReqs.FirstOrDefault(x => x.Value.Contains(ability.Description.Id)).Key;

                            sim.DataManager.AbilityDefs.TryGet(reqAbilityName, out var reqAbility);

                            desc += "<b><u>" + ability.Description.Name + "</b></u> - Requires: " + reqAbility.Description.Name + "\n\n" + ability.Description.Details + "\n\n\n";
                        }
                        else
                        {
                            desc += "<b><u>" + ability.Description.Name + "</b></u>\n\n" + ability.Description.Details +"\n\n\n";
                        }
                    }

                    var descDef = new BaseDescriptionDef("PilotSpecs", title, desc, null);
                    ___AbilityTooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(descDef));
                }
            }
        }

        [HarmonyPatch(typeof(PilotGenerator), "GeneratePilots")]
        public static class PilotGenerator_GeneratePilots_Patch
        {
            public static bool Prepare() => !Mod.modSettings.usingHumanResources;
            public static void Postfix(PilotGenerator __instance, int numPilots, int systemDifficulty, float roninChance, List<PilotDef> __result)
            {// this patch can probably be disabled once we include HR?
                var SetPilotAbilitiesMethod = Traverse.Create(__instance).Method("SetPilotAbilities", new Type[] {typeof(PilotDef), typeof(string), typeof(int)});
                for (int i = 0; i < __result.Count; i++)
                {
                    __result[i].abilityDefNames.Clear();

                    for (int l = 1; l <= __result[i].BaseGunnery; l++)
                    {
                        SetPilotAbilitiesMethod.GetValue(__result[i], "Gunnery", l);
                    }
                    for (int l = 1; l <= __result[i].BaseGuts; l++)
                    {
                        SetPilotAbilitiesMethod.GetValue(__result[i], "Guts", l);
                    }
                    for (int l = 1; l <= __result[i].BasePiloting; l++)
                    {
                        SetPilotAbilitiesMethod.GetValue(__result[i], "Piloting", l);
                    }
                    for (int l = 1; l <= __result[i].BaseTactics; l++)
                    {
                        SetPilotAbilitiesMethod.GetValue(__result[i], "Tactics", l);
                    }
                }
            }
        }

        //this patch should hopefuly prevent AI generated (hiring hall) pilots from having too many abilities
                [HarmonyPatch(typeof(PilotGenerator), "SetPilotAbilities")]
        public static class PilotGenerator_SetPilotAbilities_Patch
        {
            public static bool Prefix(PilotGenerator __instance, PilotDef pilot, string type, int value)
            {

                if (pilot.PilotTags == null) return true;
                var needsDefault = true;
                foreach (var tagK in Mod.modSettings.tagTraitForTree.Keys)
                {
                    if (pilot.PilotTags.Contains(tagK))
                    {
                        pilot.abilityDefNames.Add(Mod.modSettings.tagTraitForTree[tagK]);
                        pilot.ForceRefreshAbilityDefs();
                        needsDefault = false;
                    }
                }
                if (needsDefault)
                {
                    pilot.abilityDefNames.Add(Mod.modSettings.defaultTagTrait);
                    pilot.ForceRefreshAbilityDefs();
                }
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
                    List<AbilityDef> listAbilities = list.FindAll(x => x.IsPrimaryAbility);//get primary abilities

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
                    foreach (var t in listTraits)
                    {
                        if (sim.CanPilotTakeAbility(pilot, t))
                        {
                            pilot.abilityDefNames.Add(t.Description.Id);
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

                List<string> pilotAbilityDefNames = p.abilityDefNames;

                var abilityFilter = modSettings.abilityReqs.Values.SelectMany(x => x).ToList();

                var abilityReq = abilityFilter.FirstOrDefault(x => x == newAbility.Id);

                if (!string.IsNullOrEmpty(abilityReq))
                {
                    if (!pilotAbilityDefNames.Contains(modSettings.abilityReqs
                        .FirstOrDefault(x => x.Value.Contains(abilityReq)).Key))
                    {
                        __result = false;
                        return false;
                    }
                }

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


                 //section allows you to set a threshold the "locks" the pilot into taking only abilities within that skill once the threshold has been reached.
                
                    if ((p.SkillGunnery >= modSettings.skillLockThreshold) ||
                       (p.SkillPiloting >= modSettings.skillLockThreshold) ||
                       (p.SkillGuts >= modSettings.skillLockThreshold) ||
                       (p.SkillTactics >= modSettings.skillLockThreshold))

                    { __result = false; }

                    if (sortedSkillCount.Count <= 1 + modSettings.extraFirstTierAbilities && newAbility.ReqSkillLevel < modSettings.skillLockThreshold)
                    {
                        __result = true;
                        return false;
                    }

                    if (sortedSkillCount.ContainsKey(newAbility.ReqSkill) && sortedSkillCount[newAbility.ReqSkill] < 1 + modSettings.extraAbilitiesAllowedPerSkill) __result = true;

                    var ct = sortedSkillCount.Where(x => x.Value >= 1 + modSettings.extraAbilitiesAllowedPerSkill);

                    if (ct.Count() >= 1 + modSettings.extraPreCapStoneAbilities) __result = false;     

                    if (sortedSkillCount.ContainsKey(newAbility.ReqSkill) && sortedSkillCount[newAbility.ReqSkill] == 1 + modSettings.extraAbilitiesAllowedPerSkill && newAbility.ReqSkillLevel >= modSettings.skillLockThreshold)
                    { __result = true; }
                
                return false;
            }
        }

        [HarmonyPatch(typeof(SimGameState), "Rehydrate")]
        public static class SimGameState_Rehydrate_Patch
        {
            public static void Postfix(SimGameState __instance, GameInstanceSave gameInstanceSave)
            {
                if (!__instance.CompanyTags.Contains("AbilifierLoaded"))
                {
                    __instance.CompanyTags.Add("AbilifierLoaded");
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "Dehydrate")]
        public static class SimGameState_Dehydrate_Patch
        {
            public static void Prefix(SimGameState __instance)
            {
                if (!__instance.CompanyTags.Contains("AbilifierLoaded"))
                {
                    __instance.CompanyTags.Add("AbilifierLoaded");
                }
            }
        }
    }
}