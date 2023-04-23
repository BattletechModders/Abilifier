using BattleTech.Data;
using BattleTech;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HBS;
using Newtonsoft.Json.Linq;

namespace Abilifier.Framework
{
    public class AbilityRealizerFramework
    {
        public static bool IsSetup;
        public static SimGameConstants constants;
        public static DataManager dataManager;
        public static List<string> progressionAbilities;
        public class UpgradeAbility
        {
            public string Prereq;
            public string Skill;
            public int Level;
            public string Upgrade;
        }
        public class ModSettings
        {
            public bool DumpAbilityDefNamesAtAddToTeam = false;
            public bool DumpFixedPilotDefMerges = false;
            public bool AddTreeAbilities = true;
            public bool RemoveNonTreeAbilities = false;
            public bool RemoveDuplicateAbilities = true;
            public List<string> IgnoreAbilities = new List<string>();
            public List<string> IgnorePilotsWithTags = new List<string>();
            public Dictionary<string, List<string>> FactionAbilities = new Dictionary<string, List<string>>();
            public Dictionary<string, List<string>> TagAbilities = new Dictionary<string, List<string>>();
            public Dictionary<string, string> SwapAIAbilities = new Dictionary<string, string>();
            public List<UpgradeAbility> UpgradeAbilities = new List<UpgradeAbility>();

            public void InitAR()
            {
                using (StreamReader reader = new StreamReader($"{Mod.modDir}/AbilityRealizerSettings.json"))
                {
                    string jdata = reader.ReadToEnd(); //dictionary key should match EffectData.Description.Id of whatever Effect you want to, ahem, affect.
                    Mod.AbilityRealizerSettings = JsonConvert.DeserializeObject<ModSettings>(jdata);
                    //deser separate setting thing here
                    Mod.modLog.LogMessage($"Parsed AbilityRealizerSettings: {jdata}");
                }
            }
        }


        public static bool IsFirstLevelAbility(AbilityDef ability)
        {
            return ability.IsPrimaryAbility && ability.ReqSkillLevel < 8;
        }

        public static AbilityDef GetAbilityDef(DataManager dm, string abilityName)
        {
            var hasAbility = dm.AbilityDefs.TryGet(abilityName, out var abilityDef);
            return hasAbility ? abilityDef : null;
        }

        public static bool HasAbilityDef(DataManager dm, string abilityName)
        {
            return dm.AbilityDefs.TryGet(abilityName, out var _);
        }

        public static List<string> GetPrimaryAbilitiesForPilot(DataManager dm, PilotDef pilotDef)
        {
            var primaryAbilities = new List<string>();
            foreach (var abilityName in pilotDef.abilityDefNames)
            {
                var abilityDef = GetAbilityDef(dm, abilityName);
                if (abilityDef != null && abilityDef.IsPrimaryAbility)
                    primaryAbilities.Add(abilityName);
            }

            return primaryAbilities;
        }

        public static bool CanLearnAbility(DataManager dm, PilotDef pilotDef, string abilityName)
        {
            var hasAbility = dm.AbilityDefs.TryGet(abilityName, out var abilityDef);

            if (!hasAbility)
            {
                Mod.HBSLog.Log($"\tCANNOT FIND {abilityName}");
                return false;
            }

            // can always learn non-primary
            if (!abilityDef.IsPrimaryAbility)
                return true;

            // can only have 3 primary abilities
            var primaryAbilities = GetPrimaryAbilitiesForPilot(dm, pilotDef);
            if (primaryAbilities.Count >= 3)
                return false;

            // can only have 2 first level abilities
            var firstLevelAbilities = 0;
            foreach (var ability in primaryAbilities)
            {
                if (IsFirstLevelAbility(GetAbilityDef(dm, ability)))
                    firstLevelAbilities++;
            }

            return firstLevelAbilities < 2;
        }

        public static void CheckAbilitiesFromProgression(List<string> pilotAbilityNames, string[][] progressionTable,
            int skillLevel, List<string> missingAbilities, List<string> matchingAbilities)
        {
            for (var i = 0; i < progressionTable.Length && i < skillLevel; i++)
            {
                for (var j = 0; j < progressionTable[i].Length; j++)
                {
                    var abilityName = progressionTable[i][j];

                    if (pilotAbilityNames.Contains(abilityName))
                        matchingAbilities.Add(abilityName);
                    else
                        missingAbilities.Add(abilityName);
                }
            }
        }

        public static void Setup()
        {
            if (IsSetup)
                return;

            constants = SimGameConstants.GetInstance(LazySingletonBehavior<UnityGameInstance>.Instance.Game);
            dataManager = LazySingletonBehavior<UnityGameInstance>.Instance.Game.DataManager;

            // make sure that datamanager has gotten all of the abilities
            var loadRequest = dataManager.CreateLoadRequest(x =>
            {
                Mod.HBSLog.Log("AbilityDefs Loaded");

                if (!Mod.AbilityRealizerSettings.DumpFixedPilotDefMerges)
                    return;

                Mod.HBSLog.Log("PilotDefs loaded");
                DumpFixedPilotDefMerges();
            });

            loadRequest.AddAllOfTypeBlindLoadRequest(BattleTechResourceType.AbilityDef);

            // if dumping pilotDefs, request them
            if (Mod.AbilityRealizerSettings.DumpFixedPilotDefMerges)
                loadRequest.AddAllOfTypeBlindLoadRequest(BattleTechResourceType.PilotDef);

            loadRequest.ProcessRequests();

            progressionAbilities = new List<string>();

            // read in progression tables
            var progressionTables = new List<string[][]>
            {
                constants.Progression.GunnerySkills, constants.Progression.PilotingSkills,
                constants.Progression.GutsSkills, constants.Progression.TacticsSkills
            };
            foreach (var progressionTable in progressionTables)
            {
                foreach (var abilityTable in progressionTable)
                {
                    foreach (var abilityName in abilityTable)
                        progressionAbilities.Add(abilityName);
                }
            }

            IsSetup = true;
        }


        // MEAT
        public static void DumpFixedPilotDefMerges()
        {
            if (dataManager == null) return;

            var directory = Path.Combine(Mod.modDir, "PilotDefDump");
            Directory.CreateDirectory(directory);
            Mod.HBSLog.Log($"Dumping fixed PilotDef merges to {directory}");

            foreach (var pilotID in dataManager.PilotDefs.Keys)
            {
                var pilotDef = dataManager.PilotDefs.Get(pilotID);
                var pilotDefCopy = new PilotDef(pilotDef, pilotDef.ExperienceSpent, pilotDef.ExperienceUnspent,
                    pilotDef.Injuries, pilotDef.LethalInjury, pilotDef.LifetimeInjuries, pilotDef.MechKills,
                    pilotDef.OtherKills, pilotDef.PilotTags);

                if (!UpdateAbilitiesFromTree(pilotDefCopy))
                    continue;

                // pilotDef updated, dump it out to dir
                pilotDefCopy.abilityDefNames.Sort();

                var pilotDefJObject = new JObject { { "abilityDefNames", new JArray(pilotDefCopy.abilityDefNames) } };
                using (var writer = File.CreateText(Path.Combine(directory, pilotID + ".json")))
                {
                    var jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented };
                    pilotDefJObject.WriteTo(jsonWriter);
                    jsonWriter.Close();
                }
            }
        }

        public static void TryUpdateAbilities(Pilot pilot)
        {
            if (dataManager.PilotDefs.Exists(pilot.pilotDef.Description.Id)
                && pilot.pilotDef == dataManager.PilotDefs.Get(pilot.pilotDef.Description.Id))

            {
                // the pilot is set to use the actual pilotdef object in datamanager!
                // need to make sure that this pilot has it's own unique pilot def before we modify it
                pilot.ForceRefreshDef();
            }

            var pilotDef = pilot.pilotDef;
            var reloadAbilities = false;

            // skip pilots with specified pilot tags
            foreach (var tag in pilot.pilotDef.PilotTags)
            {
                if (Mod.AbilityRealizerSettings.IgnorePilotsWithTags.Exists(x => tag.StartsWith(x)))
                    return;
            }


            reloadAbilities |= UpdateAbilitiesFromTree(pilotDef);
            reloadAbilities |= UpdateAbilitiesFromTags(pilotDef);

            var duplicateAbilities =
                pilotDef.abilityDefNames.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key);
            foreach (var abilityName in duplicateAbilities)
            {
                if (!Mod.AbilityRealizerSettings.IgnoreAbilities.Exists(x => abilityName.StartsWith(x)) &&
                    Mod.AbilityRealizerSettings.RemoveDuplicateAbilities)
                {
                    Mod.HBSLog.Log($"{pilotDef.Description.Id}: Removing duplicate '{abilityName}'s");
                    pilotDef.abilityDefNames.RemoveAll(x => x == abilityName);
                    pilotDef.abilityDefNames.Add(abilityName);
                    reloadAbilities = true;
                }
            }

            if (pilot.Team != null)
            {
                reloadAbilities = UpdateAbilitiesFromFaction(pilotDef, pilot.Team.FactionValue) | reloadAbilities;

                if (pilot.Team.TeamController == TeamController.Computer)
                    reloadAbilities |= SwapAIAbilities(pilotDef);
            }

            reloadAbilities = UpgradeAbilities(pilotDef);

            if (reloadAbilities)
            {
                if (pilotDef.AbilityDefs != null)
                    pilotDef.AbilityDefs.Clear();

                if (pilotDef.DataManager == null)
                    pilotDef.DataManager = dataManager;

                pilotDef.ForceRefreshAbilityDefs();
            }
        }

        public static bool UpdateAbilitiesFromTree(PilotDef pilotDef)
        {
            if (pilotDef.abilityDefNames == null)
                return false;

            var matchingAbilities = new List<string>();
            var missingAbilities = new List<string>();

            CheckAbilitiesFromProgression(pilotDef.abilityDefNames, constants.Progression.GunnerySkills,
                pilotDef.SkillGunnery, missingAbilities, matchingAbilities);
            CheckAbilitiesFromProgression(pilotDef.abilityDefNames, constants.Progression.PilotingSkills,
                pilotDef.SkillPiloting, missingAbilities, matchingAbilities);
            CheckAbilitiesFromProgression(pilotDef.abilityDefNames, constants.Progression.GutsSkills,
                pilotDef.SkillGuts, missingAbilities, matchingAbilities);
            CheckAbilitiesFromProgression(pilotDef.abilityDefNames, constants.Progression.TacticsSkills,
                pilotDef.SkillTactics, missingAbilities, matchingAbilities);

            var reloadAbilities = false;
            var extraAbilities = pilotDef.abilityDefNames.Except(matchingAbilities).ToList();

            if (GetPrimaryAbilitiesForPilot(dataManager, pilotDef).Count > 3)
                Mod.HBSLog.Log($"{pilotDef.Description.Id}: Has too many primary abilities -- not doing anything about it");

            // remove abilities that don't exist anymore
            foreach (var abilityName in extraAbilities)
            {
                if (!Mod.AbilityRealizerSettings.IgnoreAbilities.Exists(x => abilityName.StartsWith(x)) &&
                    ((Mod.AbilityRealizerSettings.RemoveNonTreeAbilities && !progressionAbilities.Contains(abilityName))
                     || !HasAbilityDef(dataManager, abilityName)))
                {
                    Mod.HBSLog.Log($"{pilotDef.Description.Id}: Removing '{abilityName}'");
                    pilotDef.abilityDefNames.RemoveAll(x => x == abilityName);
                    reloadAbilities = true;
                }
            }

            // add the missing abilities
            foreach (var abilityName in missingAbilities)
            {
                if (!Mod.AbilityRealizerSettings.IgnoreAbilities.Exists(x => abilityName.StartsWith(x)) &&
                    Mod.AbilityRealizerSettings.AddTreeAbilities && CanLearnAbility(dataManager, pilotDef, abilityName))
                {
                    Mod.HBSLog.Log($"{pilotDef.Description.Id}: Adding '{abilityName}' from tree");
                    pilotDef.abilityDefNames.Add(abilityName);
                    reloadAbilities = true;
                }
            }



            return reloadAbilities;
        }

        public static bool UpdateAbilitiesFromTags(PilotDef pilotDef)
        {
            var reloadAbilities = false;

            foreach (var tag in pilotDef.PilotTags)
            {
                if (!Mod.AbilityRealizerSettings.TagAbilities.ContainsKey(tag))
                    continue;

                foreach (var abilityName in Mod.AbilityRealizerSettings.TagAbilities[tag])
                {
                    if (!HasAbilityDef(dataManager, abilityName))
                    {
                        Mod.HBSLog.LogWarning($"Tried to add {abilityName} from tag {tag}, but ability not found!");
                        continue;
                    }

                    var Upgraded = false;
                    foreach (var upgrade in Mod.AbilityRealizerSettings.UpgradeAbilities)
                    {
                        if (upgrade.Prereq == abilityName && pilotDef.abilityDefNames.Contains(upgrade.Upgrade))
                        {
                            Upgraded = true;
                        }
                    }

                    if (!pilotDef.abilityDefNames.Contains(abilityName) && !Upgraded)
                    {
                        Mod.HBSLog.Log($"{pilotDef.Description.Id}: Adding '{abilityName}' from tag '{tag}'");
                        pilotDef.abilityDefNames.Add(abilityName);
                        reloadAbilities = true;
                    }
                }
            }

            return reloadAbilities;
        }

        public static bool UpdateAbilitiesFromFaction(PilotDef pilotDef, FactionValue faction)
        {
            var reloadAbilities = false;

            if (!Mod.AbilityRealizerSettings.FactionAbilities.ContainsKey(faction.Name))
                return false;

            foreach (var abilityName in Mod.AbilityRealizerSettings.FactionAbilities[faction.Name])
            {
                if (!HasAbilityDef(dataManager, abilityName))
                {
                    Mod.HBSLog.LogWarning(
                        $"Tried to add {abilityName} from faction {faction.Name}, but ability not found!");
                    continue;
                }

                if (!pilotDef.abilityDefNames.Contains(abilityName))
                {
                    Mod.HBSLog.Log($"{pilotDef.Description.Id}: Adding '{abilityName}' from faction '{faction.Name}'");
                    pilotDef.abilityDefNames.Add(abilityName);
                    reloadAbilities = true;
                }
            }

            return reloadAbilities;
        }

        public static bool SwapAIAbilities(PilotDef pilotDef)
        {
            var reloadAbilities = false;

            var addAbilities = new List<string>();
            var removeAbilities = new List<string>();

            foreach (var abilityName in pilotDef.abilityDefNames)
            {
                if (!Mod.AbilityRealizerSettings.SwapAIAbilities.ContainsKey(abilityName))
                    continue;

                var swappedAbilityName = Mod.AbilityRealizerSettings.SwapAIAbilities[abilityName];

                if (!HasAbilityDef(dataManager, swappedAbilityName))
                {
                    Mod.HBSLog.LogWarning(
                        $"Tried to swap {swappedAbilityName} for {abilityName} for AI, but ability not found!");
                    continue;
                }

                if (!pilotDef.abilityDefNames.Contains(swappedAbilityName))
                {
                    Mod.HBSLog.Log(
                        $"{pilotDef.Description.Id}: Swapping '{swappedAbilityName}' for '{abilityName}' for AI");
                    removeAbilities.Add(abilityName);
                    addAbilities.Add(swappedAbilityName);
                    reloadAbilities = true;
                }
            }

            foreach (var abilityName in removeAbilities)
                pilotDef.abilityDefNames.Remove(abilityName);

            foreach (var abilityName in addAbilities)
                pilotDef.abilityDefNames.Add(abilityName);

            return reloadAbilities;
        }

        public static bool UpgradeAbilities(PilotDef pilotDef)
        {
            var reloadAbilities = false;

            foreach (var upgrade in Mod.AbilityRealizerSettings.UpgradeAbilities)
            {
                if (pilotDef.abilityDefNames.Contains(upgrade.Upgrade)) continue;
                if (!HasAbilityDef(dataManager, upgrade.Upgrade))
                {
                    Mod.HBSLog.LogWarning($"Was going to upgrade an ability to {upgrade.Upgrade}, but ability not found! Skipping!");
                    continue;
                }
                if (pilotDef.abilityDefNames.Contains(upgrade.Prereq))
                {
                    if (upgrade.Skill == "Guts")
                    {
                        if (pilotDef.SkillGuts >= upgrade.Level)
                        {

                            pilotDef.abilityDefNames.RemoveAll(x => x == upgrade.Prereq);
                            pilotDef.abilityDefNames.Add(upgrade.Upgrade);
                            reloadAbilities = true;
                            Mod.HBSLog.Log($"[{pilotDef.Description.Callsign}] Upgraded skill: {upgrade.Prereq} removed, {upgrade.Upgrade} added.");
                        }
                    }
                    if (upgrade.Skill == "Gunnery")
                    {
                        if (pilotDef.SkillGunnery >= upgrade.Level)
                        {

                            pilotDef.abilityDefNames.RemoveAll(x => x == upgrade.Prereq);
                            pilotDef.abilityDefNames.Add(upgrade.Upgrade);
                            reloadAbilities = true;
                            Mod.HBSLog.Log($"[{pilotDef.Description.Callsign}] Upgraded skill: {upgrade.Prereq} removed, {upgrade.Upgrade} added.");
                        }
                    }
                    if (upgrade.Skill == "Piloting")
                    {
                        if (pilotDef.SkillPiloting >= upgrade.Level)
                        {
                            pilotDef.abilityDefNames.RemoveAll(x => x == upgrade.Prereq);
                            pilotDef.abilityDefNames.Add(upgrade.Upgrade);
                            reloadAbilities = true;
                            Mod.HBSLog.Log($"[{pilotDef.Description.Callsign}] Upgraded skill: {upgrade.Prereq} removed, {upgrade.Upgrade} added.");
                        }
                    }
                    if (upgrade.Skill == "Tactics")
                    {
                        if (pilotDef.SkillTactics >= upgrade.Level)
                        {
                            pilotDef.abilityDefNames.RemoveAll(x => x == upgrade.Prereq);
                            pilotDef.abilityDefNames.Add(upgrade.Upgrade);
                            reloadAbilities = true;
                            Mod.HBSLog.Log($"[{pilotDef.Description.Callsign}] Upgraded skill: {upgrade.Prereq} removed, {upgrade.Upgrade} added.");
                        }
                    }
                }
            }
            return reloadAbilities;
        }
    }
}
