#if NO_CAE
#else
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abilifier.Framework;
using BattleTech;
using BattleTech.UI;
using CustomActivatableEquipment;
using Harmony;

namespace Abilifier.Patches
{
    internal class ResolvePatch_CAE
    {
        [HarmonyPatch(typeof(CombatHUDEquipmentSlotEx), "ResetAbilityButton",
            new Type[] { typeof(AbstractActor), typeof(CombatHUDActionButton), typeof(Ability), typeof(bool) })]
        public static class CombatHUDEquipmentSlotEx_ResetAbilityButton_Patch
        {
            public static bool Prepare() => Mod.modSettings.enableResolverator;
            public static void Postfix(CombatHUDWeaponPanel __instance, AbstractActor actor, CombatHUDActionButton button, Ability ability, bool forceInactive)
            {
                if (ability.Def.getAbilityDefExtension().ResolveCost <= 0) return;
                if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                if (actor == null || ability == null) return;
                var actorKey = actor.GetPilot().Fetch_rGUID();
                var pilotResolveInfo = PilotResolveTracker.HolderInstance.pilotResolveDict[actorKey];
                if (pilotResolveInfo.PilotResolve < ability.Def.getAbilityDefExtension().ResolveCost)
                {
                    button.DisableButton();
                }
                if (pilotResolveInfo.Predicting && pilotResolveInfo.PredictedResolve <
                    ability.Def.getAbilityDefExtension().ResolveCost)
                {
                    button.DisableButton();
                }
            }
        }
    }
}
#endif