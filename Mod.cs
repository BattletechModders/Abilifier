using System;
using System.IO;
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

            Trace("Starting up " + DateTime.Now.ToShortTimeString());
            Helpers.PopulateAbilities();
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
            public string modDirectory;
        }
    }
}
