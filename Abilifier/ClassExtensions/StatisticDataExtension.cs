using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleTech;
using HBS.Collections;
using Newtonsoft.Json;
using static Org.BouncyCastle.Crypto.Modes.EaxBlockCipher;

namespace Abilifier.Patches
{
    public class EffectDataExtensionManager
    {
        public enum EffectTargetTagSet
        {
            NotSet,
            Component,
            Pilot,
            Unit
        }

        public class EffectTargetCollectionConfig
        {
            public bool MustMatchAll = false;
            public TagSet TargetCollectionTagMatch = new TagSet();
            public TagSet TargetCollectionNotMatch = new TagSet();
        }
        public class EffectDataExtension
        {
            public string id;
            public Dictionary<EffectTargetTagSet, EffectTargetCollectionConfig> TargetCollectionsForSearch = new();
            //public List<EffectTargetTagSet> TargetCollectionsForSearch = new List<EffectTargetTagSet>();
            //public TagSet TargetCollectionTagMatch = new TagSet();
            //public TagSet TargetCollectionNotMatch = new TagSet();
            //public bool MustMatchAllCollection = false;
            public TagSet TargetComponentTagMatch = new TagSet();
            public TagSet TargetComponentTagNotMatch = new TagSet();
            public bool MustMatchAllComponent= false;

        }

        public static EffectDataExtensionManager _instance;

        public ConcurrentDictionary<string, EffectDataExtensionManager.EffectDataExtension> ExtendedEffectDataDict =
            new ConcurrentDictionary<string, EffectDataExtensionManager.EffectDataExtension>();

        public static EffectDataExtensionManager ManagerInstance
        {
            get
            {
                if (_instance == null) _instance = new EffectDataExtensionManager();
                return _instance;
            }
        }

        public void Initialize()
        {
            using (StreamReader reader = new StreamReader($"{Mod.modDir}/EffectDataExtensions.json"))
            {
                string jdata = reader.ReadToEnd(); //dictionary key should match EffectData.Description.Id of whatever Effect you want to, ahem, affect.
                ExtendedEffectDataDict = JsonConvert.DeserializeObject<ConcurrentDictionary<string, EffectDataExtension>>(jdata);
                //deser separate setting thing here
                Mod.modLog?.Info?.Write($"Adding effectData restriction for ExtendedEffectDataDict: \r{jdata}");
            }
        }
    }

    public static class EffectDataExtensions
    {
        public static EffectDataExtensionManager.EffectDataExtension getStatDataExtension(this EffectData statData)
        {
            var id = statData.statisticData.abilifierId;
            if (string.IsNullOrEmpty(id)) { id = statData.Description.Id; }
            if (string.IsNullOrEmpty(id)) { return new EffectDataExtensionManager.EffectDataExtension(); }
            if (EffectDataExtensionManager.ManagerInstance.ExtendedEffectDataDict.TryGetValue(id, out var result) == false)
            {
                result = new EffectDataExtensionManager.EffectDataExtension
                {
                    id = id
                };
                EffectDataExtensionManager.ManagerInstance.ExtendedEffectDataDict[id] = result;
            }
            if (string.IsNullOrEmpty(EffectDataExtensionManager.ManagerInstance.ExtendedEffectDataDict[id].id)) EffectDataExtensionManager.ManagerInstance.ExtendedEffectDataDict[id].id = id;
            return result;
        }

        public static List<MechComponent> GetTargetComponentsMatchingTags(ICombatant target,
            StatisticEffectData.TargetCollection targetCollection, WeaponSubType weaponSubType, WeaponType weaponType,
            WeaponCategoryValue weaponCategoryValue, AmmoCategoryValue ammoCategoryValue, TagSet tagSet, bool mustMatchAll)
        {
            List<MechComponent> list = new List<MechComponent>();
            if (targetCollection == StatisticEffectData.TargetCollection.SingleRandomWeapon)
            {
                if (target is AbstractActor abstractActor)
                {
                    List<Weapon> list2 = abstractActor.Weapons.FindAll((Weapon x) =>
                        !x.IsDisabled && (mustMatchAll ? tagSet.All(s => x.componentDef.ComponentTags.Contains(s)) : tagSet.Overlaps(x.componentDef.ComponentTags)));
                    if (list2.Count > 0)
                    {
                        list.Add(list2.GetRandomElement());
                    }
                    
                }
            }
            else if (targetCollection == StatisticEffectData.TargetCollection.StrongestWeapon)
            {
                if (target is AbstractActor abstractActor2)
                {
                    List<Weapon> list3 = abstractActor2.Weapons.FindAll((Weapon x) =>
                        !x.IsDisabled && (mustMatchAll ? tagSet.All(s => x.componentDef.ComponentTags.Contains(s)) : tagSet.Overlaps(x.componentDef.ComponentTags)));
                    if (list3.Count > 0)
                    {
                        list3.Sort((Weapon a, Weapon b) =>
                            b.DamagePerShot.CompareTo(a.DamagePerShot * (float)a.ShotsWhenFired));
                        list.Add(list3[0]);
                    }
                }
            }
            else if (targetCollection == StatisticEffectData.TargetCollection.Weapon)
            {
                if (target is AbstractActor abstractActor3)
                {
                    List<Weapon> list4 = new List<Weapon>();
                    if (weaponSubType != WeaponSubType.NotSet)
                    {
                        if (weaponSubType == WeaponSubType.Melee)
                        {
                            Mech mech = abstractActor3 as Mech;
                            if (mech != null)
                            {
                                list4.Add(mech.MeleeWeapon);
                            }
                        }
                        else if (weaponSubType == WeaponSubType.DFA)
                        {
                            Mech mech2 = abstractActor3 as Mech;
                            if (mech2 != null)
                            {
                                list4.Add(mech2.DFAWeapon);
                            }
                        }
                        else
                        {
                            list4 = abstractActor3.Weapons.FindAll((Weapon x) =>
                                x.WeaponSubType == weaponSubType && (mustMatchAll ? tagSet.All(s => x.componentDef.ComponentTags.Contains(s)) : tagSet.Overlaps(x.componentDef.ComponentTags)));
                        }
                    }
                    else if (weaponType != WeaponType.NotSet)
                    {
                        list4 = abstractActor3.Weapons.FindAll((Weapon x) =>
                            x.Type == weaponType && (mustMatchAll ? tagSet.All(s => x.componentDef.ComponentTags.Contains(s)) : tagSet.Overlaps(x.componentDef.ComponentTags)));
                    }
                    else if (!weaponCategoryValue.Is_NotSet)
                    {
                        list4 = abstractActor3.Weapons.FindAll((Weapon x) =>
                            x.WeaponCategoryValue.ID == weaponCategoryValue.ID &&
                            tagSet.Overlaps(x.componentDef.ComponentTags));
                    }
                    else
                    {
                        list4 = new List<Weapon>(
                            abstractActor3.Weapons.FindAll(x => (mustMatchAll ? tagSet.All(s => x.componentDef.ComponentTags.Contains(s)) : tagSet.Overlaps(x.componentDef.ComponentTags))));
                    }

                    for (int i = 0; i < list4.Count; i++)
                    {
                        list.Add(list4[i]);
                    }
                }
            }
            else if (targetCollection == StatisticEffectData.TargetCollection.AmmoBox)
            {
                if (target is AbstractActor abstractActor4)
                {
                    List<AmmunitionBox> list5 = new List<AmmunitionBox>();
                    if (!ammoCategoryValue.Is_NotSet)
                    {
                        list5 = abstractActor4.ammoBoxes.FindAll((AmmunitionBox x) =>
                            x.ammoCategoryValue.Equals(ammoCategoryValue) && (mustMatchAll ? tagSet.All(s => x.componentDef.ComponentTags.Contains(s)) :
                            tagSet.Overlaps(x.componentDef.ComponentTags)));
                    }
                    else
                    {
                        list5 = new List<AmmunitionBox>(
                            abstractActor4.ammoBoxes.FindAll(x => (mustMatchAll ? tagSet.All(s => x.componentDef.ComponentTags.Contains(s)) : tagSet.Overlaps(x.componentDef.ComponentTags))));
                    }

                    for (int j = 0; j < list5.Count; j++)
                    {
                        list.Add(list5[j]);
                    }
                }
            }

            return list;
        }

        public class EffectDataExtensionPatches
        {
            [HarmonyPatch(typeof(EffectManager), "GetTargetStatCollections")]
            public static class EffectManager_GetTargetStatCollections
            {
                public static void Prefix(ref bool __runOriginal, EffectManager __instance, EffectData effectData, ICombatant target,
                    ref List<StatCollection> __result)
                {
                    if (!__runOriginal) return;
                    if (__result == null) __result = new List<StatCollection>();
                    List<StatCollection> list = new List<StatCollection>();
                    StatisticEffectData.TargetCollection targetCollection = effectData.statisticData.targetCollection;
                    WeaponSubType targetWeaponSubType = effectData.statisticData.targetWeaponSubType;
                    WeaponType targetWeaponType = effectData.statisticData.targetWeaponType;
                    WeaponCategoryValue targetWeaponCategoryValue = effectData.statisticData.TargetWeaponCategoryValue;
                    AmmoCategoryValue targetAmmoCategoryValue = effectData.statisticData.TargetAmmoCategoryValue;

                    if (target is AbstractActor targetActor)
                    {
                        var extension = effectData.getStatDataExtension();
                        Mod.modLog?.Trace?.Write($"[TRACE] Process extensions for {targetActor.Description.Id} {targetActor.GUID} extension {extension.id}");
                        var targetCollectionsForSearch = extension.TargetCollectionsForSearch;
                        if (targetCollectionsForSearch.Count > 0)
                        {
                            var foundMatch = false;
                            var foundNotMatch = false;
                            var collectionsToCheck = targetCollectionsForSearch.Count;
                            var collectionsSuccess = 0;

                            if (targetCollectionsForSearch.TryGetValue(EffectDataExtensionManager.EffectTargetTagSet.NotSet, out var configNotSet))
                            {
                                //found NotSet in config, skipping everything else?
                                goto skipCollections;
                            }

                            if (targetCollectionsForSearch.TryGetValue(EffectDataExtensionManager.EffectTargetTagSet.Component, out var configComponent))
                            {
                                foundMatch = false;
                                foundNotMatch = false;
                                if (!configComponent.MustMatchAll)
                                {
                                    for (int i = 0; i < targetActor.allComponents.Count; i++)
                                    {
                                        foreach (var tag in targetActor.allComponents[i].componentDef.ComponentTags)
                                        {
                                            if (configComponent.TargetCollectionTagMatch
                                                    .Contains(tag))
                                            {
                                                Mod.modLog?.Trace?.Write($"[TRACE] MATCH check {extension.id} component {tag} found in {configComponent.TargetCollectionTagMatch}");
                                                foundMatch = true;

                                            }

                                            if (configComponent.TargetCollectionNotMatch
                                                .Contains(tag))
                                            {
                                                Mod.modLog?.Trace?.Write($"[TRACE] NOT MATCH check {extension.id} component {tag} found in {configComponent.TargetCollectionNotMatch}");
                                                foundNotMatch = true;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    var flattenedComponentTags = new List<string>();
                                    for (int i = 0; i < targetActor.allComponents.Count; i++)
                                    {
                                        foreach (var tag in targetActor.allComponents[i].componentDef.ComponentTags)
                                        {
                                            flattenedComponentTags.Add(tag);
                                        }
                                    }

                                    if (configComponent.TargetCollectionTagMatch.All(x => flattenedComponentTags.Contains(x)))
                                    {
                                        Mod.modLog?.Trace?.Write($"[TRACE] MATCH check {extension.id} all tags in {string.Join(", ", configComponent.TargetCollectionTagMatch)} should be in {string.Join(", ", flattenedComponentTags)}");
                                        foundMatch = true;
                                    }
                                    if (configComponent.TargetCollectionNotMatch.All(x => flattenedComponentTags.Contains(x)))
                                    {
                                        Mod.modLog?.Trace?.Write($"[TRACE] NOT MATCH check {extension.id} all tags in {string.Join(", ", configComponent.TargetCollectionNotMatch)} should NOT be in {string.Join(", ", flattenedComponentTags)}");
                                        foundNotMatch = true;
                                    }
                                }
                                if (!configComponent.TargetCollectionTagMatch.Any()) foundMatch = true;
                                if (!configComponent.TargetCollectionNotMatch.Any()) foundNotMatch = false;
                                if (!foundMatch || foundNotMatch)
                                {
                                    Mod.modLog?.Trace?.Write($"{extension.id} matchComponentCollection false due to !foundMatch {foundMatch} or foundNotMatch {foundNotMatch}");
                                    __runOriginal = false;
                                    return;
                                }
                                collectionsSuccess++;
                            }

                            if (targetCollectionsForSearch.TryGetValue(EffectDataExtensionManager.EffectTargetTagSet.Pilot, out var configPilot))
                            {
                                foundMatch = false;
                                foundNotMatch = false;
                                if (!configPilot.MustMatchAll)
                                {
                                    foreach (var tag in targetActor.GetPilot().pilotDef.PilotTags)
                                    {
                                        if (configPilot.TargetCollectionTagMatch.Contains(tag))
                                        {
                                            Mod.modLog?.Trace?.Write($"[TRACE] MATCH check {extension.id} component {tag} found in {configPilot.TargetCollectionTagMatch}");
                                            foundMatch = true;
                                        }

                                        if (configPilot.TargetCollectionNotMatch.Contains(tag))
                                        {
                                            Mod.modLog?.Trace?.Write($"[TRACE] NOT MATCH check {extension.id} component {tag} found in {configPilot.TargetCollectionNotMatch}");
                                            foundNotMatch = true;
                                        }
                                    }
                                }
                                else
                                {
                                    if (configPilot.TargetCollectionTagMatch.All(x => targetActor.GetPilot().pilotDef.PilotTags.Contains(x)))
                                    {
                                        Mod.modLog?.Trace?.Write($"[TRACE] MATCH check {extension.id} all tags in {string.Join(", ", configPilot.TargetCollectionTagMatch)} should be in {string.Join(", ", targetActor.GetPilot().pilotDef.PilotTags)}");
                                        foundMatch = true;
                                    }

                                    if (configPilot.TargetCollectionNotMatch.All(x => targetActor.GetPilot().pilotDef.PilotTags.Contains(x)))
                                    {
                                        Mod.modLog?.Trace?.Write($"[TRACE] NOT MATCH check {extension.id} all tags in {string.Join(", ", configPilot.TargetCollectionNotMatch)} should be in {string.Join(", ", targetActor.GetPilot().pilotDef.PilotTags)}");
                                        foundNotMatch = true;
                                    }
                                }
                                if (!configPilot.TargetCollectionTagMatch.Any()) foundMatch = true;
                                if (!configPilot.TargetCollectionNotMatch.Any()) foundNotMatch = false;
                                if (!foundMatch || foundNotMatch)
                                {
                                    Mod.modLog?.Trace?.Write($"{extension.id} matchPilotCollection false due to !foundMatch {foundMatch} or foundNotMatch {foundNotMatch}");
                                    __runOriginal = false;
                                    return;
                                }
                                collectionsSuccess++;
                            }

                            if (targetCollectionsForSearch.TryGetValue(EffectDataExtensionManager.EffectTargetTagSet.Unit, out var configUnit))
                            {
                                foundMatch = false;
                                foundNotMatch = false;
                                if (!configUnit.MustMatchAll)
                                {
                                    foreach (var tag in targetActor.GetTags())
                                    {
                                        if (configUnit.TargetCollectionTagMatch.Contains(tag))
                                        {
                                            Mod.modLog?.Trace?.Write($"[TRACE] MATCH check {extension.id} component {tag} found in {configUnit.TargetCollectionTagMatch}");
                                            foundMatch = true;
                                        }

                                        if (configUnit.TargetCollectionNotMatch.Contains(tag))
                                        {
                                            Mod.modLog?.Trace?.Write($"[TRACE] NOT MATCH check {extension.id} component {tag} found in {configUnit.TargetCollectionNotMatch}");
                                            foundNotMatch = true;
                                        }
                                    }
                                }
                                else
                                {
                                    if (configUnit.TargetCollectionTagMatch.All(x => targetActor.GetTags().Contains(x)))
                                    {
                                        Mod.modLog?.Trace?.Write($"[TRACE] MATCH check {extension.id} all tags in {string.Join(", ", configUnit.TargetCollectionTagMatch)} should be in {string.Join(", ", targetActor.GetTags())}");
                                        foundMatch = true;
                                    }

                                    if (configUnit.TargetCollectionNotMatch.All(x => targetActor.GetTags().Contains(x)))
                                    {
                                        Mod.modLog?.Trace?.Write($"[TRACE] NOT MATCH check {extension.id} all tags in {string.Join(", ", configUnit.TargetCollectionNotMatch)} should NOT be in {string.Join(", ", targetActor.GetTags())}");
                                        foundNotMatch = true;
                                    }
                                }

                                if (!configUnit.TargetCollectionTagMatch.Any()) foundMatch = true;
                                if (!configUnit.TargetCollectionNotMatch.Any()) foundNotMatch = false;
                                if (!foundMatch || foundNotMatch)
                                {
                                    Mod.modLog?.Trace?.Write($"{extension.id} matchUnitCollection false due to !foundMatch {foundMatch} or foundNotMatch {foundNotMatch}");
                                    __runOriginal = false;
                                    return;
                                }
                                collectionsSuccess++;
                            }

                            if (collectionsSuccess < collectionsToCheck)
                            {
                                Mod.modLog?.Trace?.Write($"{extension.id} returned false due to collectionsSuccess {collectionsSuccess} < collectionsToCheck {collectionsToCheck}");
                                __runOriginal = false;
                                return;
                            }
                        }
                        skipCollections:
                        if (extension.TargetComponentTagMatch.Count <= 0)
                        {
                            Mod.modLog?.Trace?.Write($"{extension.id} return true due to extension.TargetComponentTagMatch.Count = {extension.TargetComponentTagMatch.Count}");
                            __runOriginal = true;
                            return;
                        }

                        if (targetCollection == StatisticEffectData.TargetCollection.NotSet && targetActor == null)
                        {
                            Mod.modLog?.Trace?.Write($"{extension.id} return true due to targetCollection = NotSet && actor null");
                            __runOriginal = true;
                            return;
                        }

                        if (targetCollection == StatisticEffectData.TargetCollection.NotSet && targetActor != null)
                        {
                            if (!extension.MustMatchAllComponent)
                            {
                                if (extension.TargetComponentTagMatch
                                    .Overlaps(targetActor.GetTags()))
                                    list.Add(target.StatCollection);
                            }
                            else
                            {
                                if (extension.TargetComponentTagMatch.All(x => targetActor.GetTags().Contains(x)))
                                {
                                    list.Add(target.StatCollection);
                                }
                            }
                        }

                        if (targetCollection == StatisticEffectData.TargetCollection.Pilot)
                        {
                            if (target.IsPilotable)
                            {
                                var pilot = target.GetPilot();
                                if (!extension.MustMatchAllComponent)
                                {
                                    if (extension.TargetComponentTagMatch
                                        .Overlaps(pilot.pilotDef.PilotTags))
                                        list.Add(target.GetPilot().StatCollection);
                                }
                                else
                                {
                                    if (extension.TargetComponentTagMatch.All(x => pilot.pilotDef.PilotTags.Contains(x)))
                                    {
                                        list.Add(target.GetPilot().StatCollection);
                                    }
                                }
                            }
                        }

                        else
                        {
                            List<MechComponent> targetComponents = GetTargetComponentsMatchingTags(target,
                                targetCollection,
                                targetWeaponSubType, targetWeaponType, targetWeaponCategoryValue,
                                targetAmmoCategoryValue,
                                extension.TargetComponentTagMatch, extension.MustMatchAllComponent);
                            for (int i = 0; i < targetComponents.Count; i++)
                            {
                                list.Add(targetComponents[i].StatCollection);
                            }
                        }

                        __result = list;
                        __runOriginal = false;
                        return;
                    }

                    __runOriginal = true;
                    return;
                }
            }
        }
    }
}
