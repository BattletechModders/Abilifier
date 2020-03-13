using System;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using Harmony;
using static Abilifier.Mod;

// ReSharper disable InconsistentNaming

namespace Abilifier
{
    public class Patches
    {
        [HarmonyPatch(typeof(SGBarracksAdvancementPanel), "Initialize")]
        public static class SGBarracksAdvancementPanel_Initialize_Patch
        {
            public static void Prefix()
            {
                try
                {
                    Helpers.PreloadIcons();
                    Helpers.InsertAbilities();
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        [HarmonyPatch(typeof(SGBarracksSkillPip), "Initialize")]
        public static class SGBarracksSkillPip_Initialize_Patch
        {
            public static void Prefix(int index, ref AbilityDef ability)
            {
                // prevent Ability icons appearing at non-tier locations
                // the actual AbilityDef is going to be provided later anyway
                if (index != 4 && index != 7 && ability != null)
                {
                    ability = null;
                }
            }
        }

        [HarmonyPatch(typeof(SGBarracksSkillPip), "SetActiveAbilityVisible")]
        public static class SGBarracksSkillPip_SetActiveAbilityVisible_Patch
        {
            public static void Postfix(SGBarracksSkillPip __instance)
            {
                var @this = Traverse.Create(__instance);
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                var index = @this.Field("idx").GetValue<int>();
                var type = @this.Field("type").GetValue<string>();
                var cost = int.Parse(@this.Field("experienceCost").GetValue<LocalizableText>().text.Split(' ')[0]);
                var curButton = @this.Field("curButton").GetValue<HBSDOTweenToggle>();
                curButton.OnClicked.AddListener(BuildPopup);

                void BuildPopup()
                {
                    try
                    {
                        // build complete list of defs from HBS and imported json
                        var abilityDefs = Helpers.ModAbilities
                            .Where(x => x.ReqSkillLevel == index + 1 && x.ReqSkill.ToString() == type).ToList();
                        var abilityDictionaries = sim.AbilityTree.Select(x => x.Value).ToList();
                        foreach (var abilityDictionary in abilityDictionaries)
                        {
                            abilityDefs.AddRange(abilityDictionary[index].Where(x => x.ReqSkill.ToString() == type));
                        }

                        // dynamic buttons based on available abilities
                        var popup = GenericPopupBuilder
                            .Create("", "Select an ability")
                            .AddFader();
                        popup.AlwaysOnTop = true;
                        foreach (var abilityDef in abilityDefs)
                        {
                            popup.AddButton(abilityDef.Description.Name,
                                () => Helpers.SetTempPilotSkill(abilityDef, type, index, cost));
                        }

                        popup.Render();
                    }
                    catch (Exception ex)
                    {
                        Log(ex);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SGBarracksAdvancementPanel), "OnValueClick")]
        public static class SGBarracksAdvancementPanel_OnValueClick_Patch
        {
            public static void Prefix(Pilot ___curPilot) => ___curPilot.AddExperience(0, "", 100000);
        }
    }
}
