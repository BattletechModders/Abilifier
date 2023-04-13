using System;
using System.Collections.Generic;
using System.Linq;
using Abilifier.Framework;
using BattleTech;
using BattleTech.Save;
using BattleTech.UI;
using BattleTech.UI.Tooltips;

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
            public static void Prefix(SGBarracksAdvancementPanel __instance,
                List<SGBarracksSkillPip> pips, int originalSkill, int curSkill, int idx, bool needsXP, bool isLocked, ref bool __runOriginal)
            {
                if (!__runOriginal) return;
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
                    var flag2 = Helpers.CanPilotTakeAbilityPip(sim, __instance.curPilot.pilotDef, pips[idx].Ability,
                        pips[idx].SecondTierAbility);
                    var flag3 = __instance.curPilot.pilotDef.abilityDefNames.Contains(pips[idx].Ability.Description.Id);

                    //this is the pertinent change, which checks if pilot has ANY ability of the correct type and level, and sets it to be visible if true
                    var type = pips[idx].Ability.ReqSkill;
                    var abilityDefs = __instance.curPilot.pilotDef.AbilityDefs.Where(x => x.ReqSkill == type
                        && x.ReqSkillLevel == idx + 1 && x.IsPrimaryAbility);
                    var flag4 = abilityDefs.Any();

                    pips[idx].Set(purchaseState, (curSkill == idx || curSkill == idx + 1) && !isLocked, curSkill == idx,
                        needsXP, isLocked && flag);
                    pips[idx].SetActiveAbilityVisible(flag2 || flag3 || flag4);
                }

                pips[idx].Set(purchaseState, (curSkill == idx || curSkill == idx + 1) && !isLocked, curSkill == idx,
                    needsXP, isLocked && flag);
                __runOriginal = false;
                return;
            }
        }


        [HarmonyPatch(typeof(SGBarracksMWDetailPanel), "SetPilot")]

        public static class SGBarracksMWDetailPanel_SetPilot_Patch
        {
            public static void Postfix(SGBarracksMWDetailPanel __instance, Pilot p)
            {
                var gunPips =
                    __instance.advancement
                        .gunPips; //Traverse.Create(___advancement).Field("gunPips").GetValue<List<SGBarracksSkillPip>>();
                var pilotPips =
                    __instance.advancement
                        .pilotPips; //Traverse.Create(___advancement).Field("pilotPips").GetValue<List<SGBarracksSkillPip>>();
                var gutPips =
                    __instance.advancement
                        .gutPips; //Traverse.Create(___advancement).Field("gutPips").GetValue<List<SGBarracksSkillPip>>();
                var tacPips =
                    __instance.advancement
                        .tacPips; //Traverse.Create(___advancement).Field("tacPips").GetValue<List<SGBarracksSkillPip>>();


                var abilityDefs = p.pilotDef.AbilityDefs.Where(x => x.IsPrimaryAbility).ToArray();

                //loop through abilities the pilot has, and place those ability icons/tooltips in the appropriate pip slot.
                foreach (var ability in abilityDefs)
                {
                    if (ability.ReqSkill == SkillType.Gunnery)
                    {

                        gunPips[ability.ReqSkillLevel - 1].abilityIcon.vectorGraphics = ability.AbilityIcon;
                        gunPips[ability.ReqSkillLevel - 1].AbilityTooltip
                            .SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(ability.Description));
                        //Traverse.Create(gunPips[ability.ReqSkillLevel - 1]).Field("abilityIcon").GetValue<SVGImage>().vectorGraphics = ability.AbilityIcon;
                        //Traverse.Create(gunPips[ability.ReqSkillLevel - 1]).Field("AbilityTooltip").GetValue<HBSTooltip>().SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(ability.Description));
                    }
                }

                foreach (var ability in abilityDefs)
                {
                    if (ability.ReqSkill == SkillType.Piloting)
                    {
                        pilotPips[ability.ReqSkillLevel - 1].abilityIcon.vectorGraphics = ability.AbilityIcon;
                        pilotPips[ability.ReqSkillLevel - 1].AbilityTooltip
                            .SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(ability.Description));

                        //Traverse.Create(pilotPips[ability.ReqSkillLevel - 1]).Field("abilityIcon").GetValue<SVGImage>().vectorGraphics = ability.AbilityIcon;
                        //Traverse.Create(pilotPips[ability.ReqSkillLevel - 1]).Field("AbilityTooltip").GetValue<HBSTooltip>().SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(ability.Description));
                    }
                }

                foreach (AbilityDef ability in abilityDefs)
                {
                    if (ability.ReqSkill == SkillType.Guts)
                    {
                        gutPips[ability.ReqSkillLevel - 1].abilityIcon.vectorGraphics = ability.AbilityIcon;
                        gutPips[ability.ReqSkillLevel - 1].AbilityTooltip
                            .SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(ability.Description));

                        //Traverse.Create(gutPips[ability.ReqSkillLevel - 1]).Field("abilityIcon").GetValue<SVGImage>().vectorGraphics = ability.AbilityIcon;
                        //Traverse.Create(gutPips[ability.ReqSkillLevel - 1]).Field("AbilityTooltip").GetValue<HBSTooltip>().SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(ability.Description));
                    }
                }

                foreach (AbilityDef ability in abilityDefs)
                {
                    if (ability.ReqSkill == SkillType.Tactics)
                    {
                        tacPips[ability.ReqSkillLevel - 1].abilityIcon.vectorGraphics = ability.AbilityIcon;
                        tacPips[ability.ReqSkillLevel - 1].AbilityTooltip
                            .SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(ability.Description));

                        //Traverse.Create(tacPips[ability.ReqSkillLevel - 1]).Field("abilityIcon").GetValue<SVGImage>().vectorGraphics = ability.AbilityIcon;
                        //Traverse.Create(tacPips[ability.ReqSkillLevel - 1]).Field("AbilityTooltip").GetValue<HBSTooltip>().SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(ability.Description));
                    }
                }
            }
        }

        // rewrite of original
        [HarmonyPatch(typeof(SGBarracksAdvancementPanel), "OnValueClick")]
        [HarmonyBefore(new string[] {"io.github.mpstark.AbilityRealizer"})]
        public static class SGBarracksAdvancementPanel_OnValueClick_Patch
        {
            [HarmonyWrapSafe]
            public static void Prefix(
                SGBarracksAdvancementPanel __instance, string type, int value, ref bool __runOriginal)
            {
                if (!__runOriginal) return;
                if (modSettings.debugXP)
                {
                    __instance.curPilot.AddExperience(0, "", 100000);
                }

                var sim = UnityGameInstance.BattleTechGame.Simulation;
                var pips = new Dictionary<string, List<SGBarracksSkillPip>>
                {
                    {"Gunnery", __instance.gunPips},
                    {"Piloting", __instance.pilotPips},
                    {"Guts", __instance.gutPips},
                    {"Tactics", __instance.tacPips},
                };

                // removal of pip
                if (__instance.curPilot.StatCollection.GetValue<int>(type) > value)
                {
                    Logger.LogTrace($"Removing {type} {value}");
                    Logger.LogTrace($"{pips[type][value].Ability}");
                    Helpers.SetTempPilotSkill(type, value, -sim.GetLevelCost(value));
                    __instance.curPilot.pilotDef.abilityDefNames.Do(Logger.LogTrace);
                    Logger.LogTrace("\n");
                    Helpers.ForceResetCharacter(__instance);
                    //    Traverse.Create(__instance).Method("ForceResetCharacter").GetValue();
                    __runOriginal = false;
                    return;
                }

                // add non-ability pip
                if (!pips[type][value]
                        .hasAbility) //!Traverse.Create(pips[type][value]).Field("hasAbility").GetValue<bool>())
                {
                    Logger.LogTrace("Non-ability pip");
                    Helpers.SetTempPilotSkill(type, value, sim.GetLevelCost(value));
                    __instance.curPilot.pilotDef.abilityDefNames.Do(Logger.LogTrace);
                    Logger.LogTrace("\n");
                    __runOriginal = false;
                    return;
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
                    __runOriginal = false;
                    return;
                }

                // prevents which were ability buttons before other primaries were selected from being abilities
                // not every ability button is visible all the time
                var curButton =
                    pips[type][value]
                        .curButton; //Traverse.Create(pips[type][value]).Field("curButton").GetValue<HBSDOTweenToggle>();
                var skillButton =
                    pips[type][value]
                        .skillButton; //Traverse.Create(pips[type][value]).Field("skillButton").GetValue<HBSDOTweenToggle>();
                if (curButton != skillButton)
                {
                    Logger.LogTrace(new string('=', 50));
                    Logger.LogTrace("curButton != skillButton");
                    Helpers.SetTempPilotSkill(type, value, sim.GetLevelCost(value));
                    __runOriginal = false;
                    return;
                }

                // dynamic buttons based on available abilities

                //new code below//
                //new code below for ability requirements
                List<string> pilotAbilityDefNames = __instance.curPilot.pilotDef.abilityDefNames;

                var abilityFilter = modSettings.abilityReqs.Values.SelectMany(x => x).ToList();

                List<AbilityDef> abilitiesWithReqs = abilityDefs
                    .Where(ability => abilityFilter.Any(filter => filter.Equals(ability.Id))).ToList();

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
                            abilityDescs += "[[DM.BaseDescriptionDefs[" + abilityID + "],<b>" + abilityName +
                                            "</b>]]" + "\n\n";
                        }
                        else
                        {
                            abilityDescs += "<color=#33f9ff>" + abilityDefDesc.Description.Name + ": </color>" +
                                            Helpers.ProcessAbilityDefDetailString(abilityDefDesc) + "\n\n";
                        }
                    }
                    else
                    {
                        var abilityID = abilityDefDesc.Id + "Desc";
                        var abilityName = abilityDefDesc.Description.Name;

                        var reqAbilityName = modSettings.abilityReqs
                            .FirstOrDefault(x => x.Value.Contains(abilityDefDesc.Id)).Key;

                        sim.DataManager.AbilityDefs.TryGet(reqAbilityName, out var reqAbility);

                        if (modSettings.usePopUpsForAbilityDesc)
                        {
                            //abilityDescs += "<color=#FF0000>(Requirements Unmet)</color> " + "[[DM.BaseDescriptionDefs[" + abilityID + "],<b>" + abilityName + "</b>]]" + "\n\n";
                            abilityDescs += "<color=#FF0000> Requires <u>" + reqAbility.Description.Name +
                                            "</u></color> " + "[[DM.BaseDescriptionDefs[" + abilityID + "],<b>" +
                                            abilityName + "</b>]]" + "\n\n";
                        }
                        else
                        {
                            //abilityDescs += "<color=#FF0000>(Requirements Unmet)</color> " + "<color=#0000FF>" + abilityDefDesc.Description.Name + ": </color>" + abilityDefDesc.Description.Details + "\n\n";
                            abilityDescs += "<color=#FF0000> Requires <u>" + reqAbility.Description.Name +
                                            "</u></color> " + "<color=#33f9ff>" + abilityDefDesc.Description.Name +
                                            ": </color>" + Helpers.ProcessAbilityDefDetailString(abilityDefDesc) + "\n\n";
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
                            pip.thisAbility = abilityDef;
                            pip.abilityIcon.vectorGraphics = abilityDef.AbilityIcon;
                            pip.AbilityTooltip.SetDefaultStateData(
                                TooltipUtilities.GetStateDataFromObject(abilityDef.Description));
                            //Traverse.Create(pip).Field("thisAbility").SetValue(abilityDef);
                            //Traverse.Create(pip).Field("abilityIcon").GetValue<SVGImage>().vectorGraphics = abilityDef.AbilityIcon;
                            //Traverse.Create(pip).Field("AbilityTooltip").GetValue<HBSTooltip>().SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(abilityDef.Description));
                            Helpers.SetTempPilotSkill(type, value, sim.GetLevelCost(value), abilityDef);
                        });
                }

                popup.Render();
                __runOriginal = false;
                return;
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
                bool canClick, bool showXP, bool needXP, bool isLocked)
            {
                if (purchaseState == SGBarracksSkillPip.PurchaseState.Unselected)
                {
                    var sim = UnityGameInstance.BattleTechGame.Simulation;
                    var abilityDictionaries = sim.AbilityTree.Select(x => x.Value).ToList();

                    var abilityDefs = new List<AbilityDef>();
                    foreach (var abilityDictionary in abilityDictionaries)
                    {
                        abilityDefs.AddRange(abilityDictionary[__instance.idx]
                            .Where(x => x.ReqSkill.ToString() == __instance.type));
                    }

                    var title = $"{__instance.type}: Level {__instance.idx + 1} Ability Options";

                    var desc = "";


                    foreach (var ability in abilityDefs)
                    {
                        var abilityFilter = modSettings.abilityReqs.Values.SelectMany(x => x).ToList();

                        List<AbilityDef> abilitiesWithReqs =
                            abilityDefs.Where(x => abilityFilter.Any(y => y.Equals(x.Id))).ToList();

                        if (abilitiesWithReqs.Contains(ability))
                        {
                            var reqAbilityName = modSettings.abilityReqs
                                .FirstOrDefault(x => x.Value.Contains(ability.Description.Id)).Key;

                            sim.DataManager.AbilityDefs.TryGet(reqAbilityName, out var reqAbility);

                            desc += "<b><u>" + ability.Description.Name + "</b></u> - Requires: " +
                                    reqAbility.Description.Name + "\n\n" + Helpers.ProcessAbilityDefDetailString(ability) + "\n\n\n";
                        }
                        else
                        {
                            desc += "<b><u>" + ability.Description.Name + "</b></u>\n\n" + Helpers.ProcessAbilityDefDetailString(ability) +
                                    "\n\n\n";
                        }
                    }

                    var descDef = new BaseDescriptionDef("PilotSpecs", title, desc, null);
                    __instance.AbilityTooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(descDef));
                }
            }
        }

        [HarmonyPatch(typeof(PilotGenerator), "GeneratePilots")]
        public static class PilotGenerator_GeneratePilots_Patch
        {

            public static void Postfix(PilotGenerator __instance, int numPilots, int systemDifficulty,
                float roninChance, List<PilotDef> __result)
            {
                //               if (Mod.modSettings.usingHumanResources) return;
                // this below can probably be disabled once we include HR? maybe not since i think CU adds its tonk tag too late.
                //var SetPilotAbilitiesMethod = Traverse.Create(__instance).Method("SetPilotAbilities", new Type[] {typeof(PilotDef), typeof(string), typeof(int)});
                foreach (var pilot in __result)
                {
                    foreach (var tagClean in Mod.modSettings.proceduralTagCleanup.Keys)
                    {
                        if (pilot.PilotTags.All(x => x != tagClean)) continue;
                        foreach (var removal in Mod.modSettings.proceduralTagCleanup[tagClean])
                        {
                            if (pilot.PilotTags.Remove(removal))
                            {
                                Mod.modLog.LogMessage(
                                    $"Removed {removal} from {pilot.Description.Callsign} due to proceduralTagCleanup");
                            }
                        }
                    }

                    pilot.abilityDefNames.Clear();

                    for (int l = 1; l <= pilot.BaseGunnery; l++)
                    {
                        __instance.SetPilotAbilities(pilot, "Gunnery", l);
                        //SetPilotAbilitiesMethod.GetValue(pilot, "Gunnery", l);
                    }

                    for (int l = 1; l <= pilot.BaseGuts; l++)
                    {
                        __instance.SetPilotAbilities(pilot, "Guts", l);
                        //SetPilotAbilitiesMethod.GetValue(pilot, "Guts", l);
                    }

                    for (int l = 1; l <= pilot.BasePiloting; l++)
                    {
                        __instance.SetPilotAbilities(pilot, "Piloting", l);
                        //SetPilotAbilitiesMethod.GetValue(pilot, "Piloting", l);
                    }

                    for (int l = 1; l <= pilot.BaseTactics; l++)
                    {
                        __instance.SetPilotAbilities(pilot, "Tactics", l);
                        // SetPilotAbilitiesMethod.GetValue(pilot, "Tactics", l);
                    }
                }
            }
        }

        //this patch should hopefuly prevent AI generated (hiring hall) pilots from having too many abilities
        [HarmonyPatch(typeof(PilotGenerator), "SetPilotAbilities")]
        public static class PilotGenerator_SetPilotAbilities_Patch
        {
            public static void Prefix(PilotGenerator __instance, PilotDef pilot, string type, int value, ref bool __runOriginal)
            {
                if (!__runOriginal) return;
                if (pilot.PilotTags == null)
                {
                    __runOriginal = true;
                    return;
                }
                
                if (Mod.modSettings.tagTraitForTree.Count > 0)
                {
                    foreach (var tagK in Mod.modSettings.tagTraitForTree.Keys)
                    {
                        if (pilot.PilotTags.Contains(tagK) &&
                            !pilot.abilityDefNames.Contains(Mod.modSettings.tagTraitForTree[tagK]))
                        {
                            pilot.abilityDefNames.Add(Mod.modSettings.tagTraitForTree[tagK]);
                            pilot.ForceRefreshAbilityDefs();
                        }
                    }

                    if (Mod.modSettings.defaultTagTraitForTree.Count > 0)
                    {
                        if (!pilot.PilotTags.Contains(Mod.modSettings.defaultTagTraitForTree.FirstOrDefault().Key) &&
                            !pilot.PilotTags.Contains(Mod.modSettings.defaultTagTraitException))
                        {
                            {
                                pilot.PilotTags.Add(Mod.modSettings.defaultTagTraitForTree.FirstOrDefault().Key);
                                pilot.abilityDefNames.Add(Mod.modSettings.defaultTagTraitForTree.FirstOrDefault()
                                    .Value);
                                pilot.ForceRefreshAbilityDefs();
                            }
                        }
                    }
                }

                var sim = UnityGameInstance.BattleTechGame.Simulation;
                value--;
                if (value < 0)
                {
                    __runOriginal = false;
                    return;
                }

                if (!sim.AbilityTree.ContainsKey(type))
                {
                    __runOriginal = false;
                    return;
                }

                if (sim.AbilityTree[type].Count <= value)
                {
                    __runOriginal = false;
                    return;
                }

                List<AbilityDef> list = sim.AbilityTree[type][value];

                if (list.Count == 0)
                {
                    __runOriginal = false;
                    return;
                }

                else
                {
                    List<AbilityDef> listAbilities = list.FindAll(x => x.IsPrimaryAbility); //get primary abilities

                    List<string> pilotAbilityDefNames = pilot.abilityDefNames;
                    var abilityFilter = modSettings.abilityReqs.Values.SelectMany(x => x).ToList();

                    List<AbilityDef> abilitiesWithReqs = listAbilities
                        .Where(ability => abilityFilter.Any(filter => filter.Equals(ability.Id))).ToList();

                    foreach (var abilityWithReq in abilitiesWithReqs)
                    {
                        if (!pilotAbilityDefNames.Contains(modSettings.abilityReqs
                                .FirstOrDefault(x => x.Value.Contains(abilityWithReq.Id)).Key))
                        {
                            listAbilities.Remove(abilityWithReq);
                        }
                    }

                    List<AbilityDef>
                        listTraits = list.FindAll(x => x.IsPrimaryAbility != true); //need to keep all traits
                    if (listAbilities.Count > 0)
                    {
                        int idx = UnityEngine.Random.Range(0,
                            listAbilities.Count); //pick a random primary of the options
                        listTraits.Add(listAbilities[idx]); //add only that random primary
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
                    __runOriginal = false;
                    return;
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "CanPilotTakeAbility")]
        [HarmonyAfter(new string[] {"io.github.mpstark.AbilityRealizer"})]
        public static class SimGameState_CanPilotTakeAbility_Patch
        {
            public static void Prefix(SimGameState __instance, ref bool __result, PilotDef p, AbilityDef newAbility, ref bool __runOriginal,
            bool checkSecondTier = false)
            {
                if (!__runOriginal) return;
                List<string> pilotAbilityDefNames = p.abilityDefNames;
                if (pilotAbilityDefNames.Contains(newAbility.Description.Id))
                {
                    __result = false;
                    __runOriginal = false;
                    return;
                }

                var abilityFilter = modSettings.abilityReqs.Values.SelectMany(x => x).ToList();
                var abilityReq = abilityFilter.FirstOrDefault(x => x == newAbility.Id);
                if (!string.IsNullOrEmpty(abilityReq))
                {
                    if (!pilotAbilityDefNames.Contains(modSettings.abilityReqs
                            .FirstOrDefault(x => x.Value.Contains(abilityReq)).Key))
                    {
                        __result = false;
                        __runOriginal = false;
                        return;
                    }
                }

                if (!newAbility.IsPrimaryAbility)
                {
                    __result = true;
                    __runOriginal = false;
                    return;
                }

                List<AbilityDef> primaryPilotAbilities = SimGameState.GetPrimaryPilotAbilities(p);
                if (primaryPilotAbilities == null)
                {
                    __result = true;
                    __runOriginal = false;
                    return;
                }

                if (primaryPilotAbilities.Count >=
                    3 + modSettings.extraAbilities) //change max allowed abilities for pilot to take when lvling up
                {
                    __result = false;
                    __runOriginal = false;
                    return;
                }

                Dictionary<SkillType, int> sortedSkillCount = __instance.GetSortedSkillCount(p);

                __result = (sortedSkillCount.Count <= 1 + modSettings.extraFirstTierAbilities
                            || sortedSkillCount.ContainsKey(newAbility.ReqSkill))
                           && (!sortedSkillCount.ContainsKey(newAbility.ReqSkill) ||
                               sortedSkillCount[newAbility.ReqSkill] <= 1 + modSettings.extraAbilitiesAllowedPerSkill)
                           && (!checkSecondTier || sortedSkillCount.ContainsKey(newAbility.ReqSkill) ||
                               primaryPilotAbilities.Count <=
                               1 + modSettings
                                   .extraAbilitiesAllowedPerSkill); //change max # abilities per-skill type (default is 2, so only allowed to take if currently have <=1)
                //       && (modSettings.skillLockThreshold <= 0 || ((primaryPilotAbilities.Count == 2 + modSettings.extraAbilities) ? (newAbility.ReqSkillLevel > modSettings.skillLockThreshold && sortedSkillCount[newAbility.ReqSkill] == 1 + modSettings.extraAbilitiesAllowedPerSkill) : true));//added part for skill locking?


                //section allows you to set a threshold the "locks" the pilot into taking only abilities within that skill once the threshold has been reached.

                if ((p.SkillGunnery >= modSettings.skillLockThreshold) ||
                    (p.SkillPiloting >= modSettings.skillLockThreshold) ||
                    (p.SkillGuts >= modSettings.skillLockThreshold) ||
                    (p.SkillTactics >= modSettings.skillLockThreshold))

                {
                    __result = false;
                }

                if (sortedSkillCount.Count <= 1 + modSettings.extraFirstTierAbilities &&
                    newAbility.ReqSkillLevel < modSettings.skillLockThreshold)
                {
                    __result = true;
                    __runOriginal = false;
                    return;
                }

                if (sortedSkillCount.ContainsKey(newAbility.ReqSkill) && sortedSkillCount[newAbility.ReqSkill] <
                    1 + modSettings.extraAbilitiesAllowedPerSkill) __result = true;

                var ct = sortedSkillCount.Where(x => x.Value >= 1 + modSettings.extraAbilitiesAllowedPerSkill);

                if (ct.Count() >= 1 + modSettings.extraPreCapStoneAbilities) __result = false;

                if (sortedSkillCount.ContainsKey(newAbility.ReqSkill) &&
                    sortedSkillCount[newAbility.ReqSkill] == 1 + modSettings.extraAbilitiesAllowedPerSkill &&
                    newAbility.ReqSkillLevel >= modSettings.skillLockThreshold)
                {
                    __result = true;
                }

                __runOriginal = false;
                return;
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

                if (Mod.modSettings.tagTraitForTree.Count > 0)
                {
                    foreach (var pilot in __instance.PilotRoster)
                    {

                        foreach (var tagK in Mod.modSettings.tagTraitForTree.Keys)
                        {
                            if (pilot.pilotDef.PilotTags.Contains(tagK) &&
                                !pilot.pilotDef.abilityDefNames.Contains(Mod.modSettings.tagTraitForTree[tagK]))
                            {
                                pilot.pilotDef.abilityDefNames.Add(Mod.modSettings.tagTraitForTree[tagK]);
                                pilot.pilotDef.ForceRefreshAbilityDefs();
                            }
                        }

                        if (Mod.modSettings.defaultTagTraitForTree.Count > 0)
                        {
                            if (!pilot.pilotDef.PilotTags.Contains(Mod.modSettings.defaultTagTraitForTree
                                    .FirstOrDefault().Key) &&
                                !pilot.pilotDef.PilotTags.Contains(Mod.modSettings.defaultTagTraitException))
                            {
                                {
                                    pilot.pilotDef.PilotTags.Add(Mod.modSettings.defaultTagTraitForTree.FirstOrDefault()
                                        .Key);
                                    pilot.pilotDef.abilityDefNames.Add(Mod.modSettings.defaultTagTraitForTree
                                        .FirstOrDefault()
                                        .Value);
                                    pilot.pilotDef.ForceRefreshAbilityDefs();
                                }
                            }
                        }
                    }
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

        [HarmonyPatch(typeof(ActiveProbeSequence), "OnAdded")]
        public static class ActiveProbeSequence_OnAdded_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
//                var codes = instructions.ToList();
//                for (var index = 0; index < codes.Count; index++)
//                {
//                    if (codes[index].opcode == OpCodes.Ldfld)
//                    {
                return instructions.MethodReplacer(
                    AccessTools.Property(typeof(AbstractActor), nameof(AbstractActor.ComponentAbilities))
                        .GetGetMethod(),
                    AccessTools.Method(typeof(Helpers), nameof(Helpers.FetchAllActorAbilities))
                );
//                    }
//                }
//                return codes.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(Pilot), "InitAbilities", new Type[] {typeof(bool), typeof(bool)})]
        public static class Pilot_InitAbilities
        {
            public static void Prefix(ref Pilot __instance)
            {
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                if (sim == null || __instance == null) return;
                if (__instance.pilotDef.dataManager == null) return;
                __instance.pilotDef.AutofillNonProceduralTraits();
            }
        }
    }
}