using System;
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

            Log("Starting up");
            Helpers.PopulateAbilities();
            Helpers.LogAbilityDictionary();
            var harmony = HarmonyInstance.Create("ca.gnivler.BattleTech.Abilifier");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        internal static void Log(object input)
        {
            FileLog.Log($"[Abilifier] {input ?? "NULL"}");
        }

        public class Settings
        {
            public bool enableDebug;
            public string modDirectory;
        }
    }
}