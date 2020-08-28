using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;
using Newtonsoft.Json;

// ReSharper disable UnassignedField.Global
// ReSharper disable InconsistentNaming

namespace Abilifier
{
    public static class Mod
    {
        internal static Settings modSettings;

        public static void Init(string modDir, string settings)
        {

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

            var logFile = modSettings.modDirectory + "/log.txt";
            if (File.Exists(logFile))
            {
                File.Delete(logFile);
            }
            modSettings.abilityReqs = modSettings.abilityReqs ?? new Dictionary<string, List<string>> { { "potato", new List<string> { "potahto" } } };


            Trace("Starting up " + DateTime.Now.ToShortTimeString());
            //            Helpers.PopulateAbilities();
            var harmony = HarmonyInstance.Create("ca.gnivler.BattleTech.Abilifier");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

        }

        internal static void Log(object input)
        {
            if (modSettings.enableLog)
            {
                using (var writer = new StreamWriter(modSettings.modDirectory + "/log.txt"))
                {
                    writer.WriteLine($"[Abilifier] {input ?? "NULL"}");
                }
            }

            if (modSettings.enableTrace)
            {
                Trace(input);
            }
        }

        internal static void Trace(object input)
        {
            if (modSettings.enableTrace)
            {
                FileLog.Log($"[Abilifier] Trace: {input ?? "NULL"}");
            }
        }

        public class Settings
        {
            public bool enableTrace;
            public bool enableLog;
            public bool usePopUpsForAbilityDesc = false;
            public bool debugXP = false;
            public int extraFirstTierAbilities = 0;
            public int extraAbilities = 0;
            public int extraAbilitiesAllowedPerSkill = 0;
            public int nonTreeAbilities = 0;
            public string modDirectory;
            public bool cleanUpCombatUI;
            public int skillLockThreshold = 0;
            public Dictionary<string, List<string>> abilityReqs;
        };
            
       
        
        
    }
}