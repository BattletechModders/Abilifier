using System;
using System.Collections.Generic;
using System.Reflection;
using Abilifier.Framework;
using Harmony;
using Newtonsoft.Json;

// ReSharper disable UnassignedField.Global
// ReSharper disable InconsistentNaming

namespace Abilifier
{
    public static class Mod
    {
        internal static Logger modLog;
        private static string modDir;

        internal static Settings modSettings;

        public static void Init(string directory, string settings)
        {
            modDir = directory;
            modLog = new Logger(modDir, "Abilifier", true);
            // read settings
            try
            {
                modSettings = JsonConvert.DeserializeObject<Settings>(settings);
                modSettings.modDirectory = modDir;
            }
            catch (Exception)
            {
                modSettings = new Settings();
            }

            modSettings.abilityReqs = modSettings.abilityReqs ?? new Dictionary<string, List<string>> { { "potato", new List<string> { "potahto" } } };


            Mod.modLog.LogMessage($"Initializing Abilifier - Version {typeof(Settings).Assembly.GetName().Version}");
            //            Helpers.PopulateAbilities();

            PilotResolveTracker.HolderInstance.Initialize();
            var harmony = HarmonyInstance.Create("ca.gnivler.BattleTech.Abilifier");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

        }
        public class Settings
        {
            public bool enableTrace;
            public bool enableLog;
            public string modDirectory;
            public bool usePopUpsForAbilityDesc = false;
            public bool debugXP = false;
            public bool enableResolverator = true;
            public float resolveGenTacticsMult = 0.1f;
            public float resolveCostTacticsMult = 0.05f;
            public float resolveGenBaseMult = 1.0f;
            public float resolveCostBaseMult = 1.0f;
            public int extraFirstTierAbilities = 0;
            public int extraAbilities = 0;
            public int extraAbilitiesAllowedPerSkill = 0;
            public int nonTreeAbilities = 0;
            public bool cleanUpCombatUI;
            public int skillLockThreshold = 10;
            public int extraPreCapStoneAbilities = 0;
            public bool usingCACabilitySelector = false;
            public Dictionary<string, List<string>> abilityReqs;
        };
            
       
        
        
    }
}