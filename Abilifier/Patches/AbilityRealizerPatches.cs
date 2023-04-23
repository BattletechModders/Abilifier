using BattleTech.UI;
using BattleTech;
using System.Linq;
using BattleTech.UI.Tooltips;
using HBS.Extensions;
using HBS;

namespace Abilifier.Patches
{
    public  class AbilityRealizerPatches
    {
        [HarmonyPatch(typeof(MainMenu), "Init")]
        public static class MainMenu_Init_Patch
        {
            public static void Postfix()
            {
                Framework.AbilityRealizerFramework.Setup();
            }
        }
        
        [HarmonyPatch(typeof(Pilot), "InitAbilities")]
        public static class Pilot_InitAbilities_Patch
        {
            public static bool Prepare() => false; //disabled, moved to main patch
            public static void Prefix(Pilot __instance)
            {
                Framework.AbilityRealizerFramework.TryUpdateAbilities(__instance);
            }
        }

        [HarmonyPatch(typeof(Pilot), "CombatInitFromSave")]
        public static class Pilot_CombatInitFromSave_Patch
        {
            public static void Prefix(Pilot __instance)
            {
                Framework.AbilityRealizerFramework.TryUpdateAbilities(__instance);
            }
        }

        [HarmonyPatch(typeof(Pilot), "AddToTeam")]
        public static class Pilot_AddToTeam_Patch
        {
            public static void Postfix(Pilot __instance)
            {
                Framework.AbilityRealizerFramework.TryUpdateAbilities(__instance);
                if (__instance.ParentActor == null && !Mod.AbilityRealizerSettings.DumpAbilityDefNamesAtAddToTeam) return;
                    Mod.HBSLog.Log($"[Pilot.AddToTeam] Dumping pilot abilitydefnames for {__instance.ParentActor?.DisplayName} - {__instance.ParentActor?.GUID}\n{string.Join(", ", __instance.pilotDef.abilityDefNames)}");
            }
        }

        [HarmonyPatch(typeof(SGBarracksSkillPip), "Initialize")]
        public static class SGBarracksSkillPip_Initialize_Patch
        {
            public static void Postfix(SGBarracksSkillPip __instance, string type, int index, bool hasPassives, AbilityDef ability)
            {
                if (!hasPassives)
                    return;

                var simGame = LazySingletonBehavior<UnityGameInstance>.Instance.Game.Simulation;
                if (simGame == null)
                    return;

                // get the abilities that are not primary
                var abilities = simGame.GetAbilityDefFromTree(type, index).Where(x => !x.IsPrimaryAbility).ToList();

                // gets the first ability that has a tooltip
                var passiveAbility = abilities.Find(x => !(string.IsNullOrEmpty(x.Description.Name) || string.IsNullOrEmpty(x.Description.Details)));

                // clear the dot on tooltip-less dots
                if (passiveAbility == null)
                {
                    __instance.skillPassiveTraitDot.gameObject.SetActive(false);
                }
                    //Traverse.Create(__instance).Field("skillPassiveTraitDot").GetValue<SVGImage>().gameObject.SetActive(false);

                if (passiveAbility != null)
                    __instance.gameObject.FindFirstChildNamed("obj-pip").GetComponent<HBSTooltip>()
                        .SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(passiveAbility.Description));
            }
        }

        [HarmonyPatch(typeof(StarSystem), "HirePilot")]
        public static class StarSystem_HirePilot_Patch
        {
            public static bool Prefix(StarSystem __instance, PilotDef def)
            {
                //		if (!__instance.AvailablePilots.Contains(def))
                if (__instance.AvailablePilots.Any(x => x.Description.Id == def.Description.Id))

                {
                    __instance.AvailablePilots.RemoveAll(x => x.Description.Id == def.Description.Id);
                    //			if (__instance.PermanentRonin.Contains(def))
                    if (__instance.PermanentRonin.Any(x => x.Description.Id == def.Description.Id))
                    {
                        __instance.PermanentRonin.RemoveAll(x => x.Description.Id == def.Description.Id);
                        __instance.Sim.UsedRoninIDs.Add(def.Description.Id);
                    }
                    def.SetDayOfHire(__instance.Sim.DaysPassed);
                    __instance.Sim.AddPilotToRoster(def, true, false);
                    int purchaseCostAfterReputationModifier = __instance.GetPurchaseCostAfterReputationModifier(__instance.Sim.GetMechWarriorHiringCost(def));
                    __instance.Sim.AddFunds(-purchaseCostAfterReputationModifier, null, true, true);
                }
                return false;
            }
        }
    }
}
