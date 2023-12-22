using Abilifier.Patches;
using BattleTech;
using BattleTech.Save.SaveGameStructure;
using HBS.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Abilifier.Framework;
using static Abilifier.Patches.AbilityExtensions;
using static Org.BouncyCastle.Crypto.Modes.EaxBlockCipher;

namespace Abilifier
{
    public static class MEHelper
    {
        public static TagSet GetTags(this MechDef mechDef)
        {
            return mechDef.MechTags;
        }
        public static bool Filter(MechDef mechDef, MechComponentDef componentDef, EffectData effectData)
        {
            bool result = FilterReal(mechDef, componentDef, effectData);
            Mod.modLog?.Info?.Write($"Filter {mechDef.ChassisID} - component:{(componentDef==null?"null":componentDef.Description.Id)} - effect:{effectData.Description.Id} - result:{result}");
            return result;
        }

        public static bool FilterReal(MechDef mechDef, MechComponentDef componentDef, EffectData effectData)
        {
            if (mechDef == null)
            {
                return false;
            }

            var extension = effectData.getStatDataExtension();
            //return true;

            var targetCollectionsForSearch = extension.TargetCollectionsForSearch;
            if (targetCollectionsForSearch.Count > 0)
            {
                var foundMatch = false;
                var foundNotMatch = false;
                var matchComponentCollection = true;
                var matchUnitCollection = true;

                if (targetCollectionsForSearch.TryGetValue(EffectDataExtensionManager.EffectTargetTagSet.NotSet,
                        out var configNotSet))
                {
                    //found NotSet in config, skipping everything else?
                    goto skipCollections;
                }

                if (targetCollectionsForSearch.TryGetValue(EffectDataExtensionManager.EffectTargetTagSet.Component,
                        out var configComponent))
                {
                    if (!configComponent.MustMatchAll)
                    {
                        for (int i = 0; i < mechDef.Inventory.Length; i++)
                        {
                            foreach (var tag in mechDef.Inventory[i].Def.ComponentTags)
                            {
                                if (configComponent.TargetCollectionTagMatch
                                    .Contains(tag))
                                {
                                    Mod.modLog?.Trace?.Write($"[TRACE] MATCH check: {extension.id} - component tag {tag} found in {configComponent.TargetCollectionTagMatch}");
                                    foundMatch = true;
                                }

                                if (configComponent.TargetCollectionNotMatch
                                    .Contains(tag))
                                {
                                    Mod.modLog?.Trace?.Write($"[TRACE] NOT MATCH check: {extension.id} - component tag {tag} found in {configComponent.TargetCollectionNotMatch}");
                                    foundNotMatch = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        var flattenedComponentTags = new List<string>();
                        for (int i = 0; i < mechDef.Inventory.Length; i++)
                        {
                            foreach (var tag in mechDef.Inventory[i].Def.ComponentTags)
                            {
                                flattenedComponentTags.Add(tag);
                            }
                        }

                        if (configComponent.TargetCollectionTagMatch.All(x =>
                                flattenedComponentTags.Contains(x)))
                        {
                            Mod.modLog?.Trace?.Write($"[TRACE] MATCH check: {extension.id}.\r\nTags in Component TargetCollectionTagMatch: {string.Join(", ", configComponent.TargetCollectionTagMatch)}.\r\nTags in Component tags: {string.Join(", ", flattenedComponentTags)}");
                            foundMatch = true;
                        }

                        if (configComponent.TargetCollectionNotMatch.All(x =>
                                flattenedComponentTags.Contains(x)))
                        {
                            Mod.modLog?.Trace?.Write($"[TRACE] NOT MATCH check: {extension.id}.\r\nTags in Component TargetCollectionNotMatch: {string.Join(", ", configComponent.TargetCollectionNotMatch)}.\r\nTags in Component tags: {string.Join(", ", flattenedComponentTags)}");
                            foundNotMatch = true;
                        }
                    }

                    if (!configComponent.TargetCollectionTagMatch.Any()) foundMatch = true;
                    if (!configComponent.TargetCollectionNotMatch.Any()) foundNotMatch = false;
                    if (!foundMatch || foundNotMatch)
                    {
                        Mod.modLog?.Trace?.Write($"{extension.id} did not meet the tag matching criteria. Match found: {foundMatch}, Non-match found: {foundNotMatch}");
                        matchComponentCollection = false;
                    }
                }

                if (targetCollectionsForSearch.TryGetValue(EffectDataExtensionManager.EffectTargetTagSet.Unit,
                        out var configUnit))
                {
                    foundMatch = false;
                    foundNotMatch = false;
                    if (!configUnit.MustMatchAll)
                    {
                        foreach (var tag in mechDef.GetTags())
                        {
                            if (configUnit.TargetCollectionTagMatch.Contains(tag))
                            {
                                Mod.modLog?.Trace?.Write($"[TRACE] MATCH check: {extension.id} - component tag {tag} found in {configUnit.TargetCollectionTagMatch}");
                                foundMatch = true;
                            }

                            if (configUnit.TargetCollectionNotMatch.Contains(tag))
                            {
                                Mod.modLog?.Trace?.Write($"[TRACE] NOT MATCH check: {extension.id} - component tag {tag} found in {configUnit.TargetCollectionNotMatch}");
                                foundNotMatch = true;
                            }
                        }
                    }
                    else
                    {
                        if (configUnit.TargetCollectionTagMatch.All(x => mechDef.GetTags().Contains(x)))
                        {
                            Mod.modLog?.Trace?.Write($"[TRACE] MATCH check: {extension.id}.\r\nTags in Mech TargetCollectionTagMatch: {string.Join(", ", configUnit.TargetCollectionTagMatch)}.\r\nTags in Mech tags: {string.Join(", ", mechDef.GetTags())}");
                            foundMatch = true;
                        }

                        if (configUnit.TargetCollectionNotMatch.All(x => mechDef.GetTags().Contains(x)))
                        {
                            Mod.modLog?.Trace?.Write($"[TRACE] NOT MATCH check: {extension.id}.\r\nTags in Mech TargetCollectionNotMatch: {string.Join(", ", configUnit.TargetCollectionNotMatch)}.\r\nTags in Mech tags: {string.Join(", ", mechDef.GetTags())}");
                            foundNotMatch = true;
                        }
                    }

                    if (!configUnit.TargetCollectionTagMatch.Any()) foundMatch = true;
                    if (!configUnit.TargetCollectionNotMatch.Any()) foundNotMatch = false;
                    if (!foundMatch || foundNotMatch)
                    {
                        Mod.modLog?.Trace?.Write($"{extension.id} - The unit tag collection did not meet the matching criteria. Match found: {foundMatch}, Non-match found: {foundNotMatch}");
                        matchUnitCollection = false;
                    }
                }

                if (!matchComponentCollection || !matchUnitCollection)
                {
                    Mod.modLog?.Trace?.Write($"{extension.id} - Operation returned false because either the component or unit tag collections did not meet the matching criteria. Component match: {matchComponentCollection}, Unit match: {matchUnitCollection}");
                    return false;
                }
            }

            skipCollections:

            if (extension.TargetComponentTagMatch.Count == 0)
            {
                return true;
            }

            if (componentDef == null)
            {
                if (!extension.MustMatchAllComponent)
                {
                    return extension.TargetComponentTagMatch.Overlaps(mechDef.GetTags());
                }
                else
                {
                    return (extension.TargetComponentTagMatch.All(x => mechDef.GetTags().Contains(x)));
                }
            }
            else
            {
                if (!extension.MustMatchAllComponent)
                {
                    return extension.TargetComponentTagMatch.Overlaps(componentDef.ComponentTags);
                }
                else
                {
                    return (extension.TargetComponentTagMatch.All(x => componentDef.ComponentTags.Contains(x)));
                }
            }
        }

        public static void AttachTo()
            {
                try
                {
                    var assemblyName = "MechEngineer";
                    var typeName = "MechEngineer.Features.OverrideStatTooltips.Helper.MechDefStatisticModifier";

                    var MEHelperType = AppDomain.CurrentDomain
                        .GetAssemblies()
                        .Where(assembly => assembly.GetName().Name == assemblyName)
                        .Select(assembly => assembly.GetType(typeName))
                        .SingleOrDefault(type => type != null);

                    if (MEHelperType != null)
                    {
                        Mod.modLog?.Info?.Write($"{typeName} found");
                        MethodInfo RegisterFilter = AccessTools.Method(MEHelperType, "RegisterFilter");
                        if(RegisterFilter != null)
                        {
                            Mod.modLog?.Info?.Write($"{typeName}.RegisterFilter found");
                            RegisterFilter.Invoke(null, new object[] { "Abilifier", new Func<MechDef, MechComponentDef, EffectData, bool>(Filter) });
                        }
                    }

                }
                catch (Exception e)
                {
                    Mod.modLog?.Info?.Write(e.ToString());
                }

            }
    }
}