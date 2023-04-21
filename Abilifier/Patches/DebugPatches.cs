using BattleTech;
using SVGImporter.LibTessDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;

namespace Abilifier.Patches
{
    public class DebugPatches
    {
        [HarmonyPatch(typeof(EffectManager), "NotifyEndOfMovement", new Type[] {typeof(string)})]
        public static class EffectManager_NotifyEndOfMovement_DEBUG
        {
            public static bool Prepare() => Mod.modSettings.debugExpiration;
            public static void Prefix(EffectManager __instance)
            {
                foreach (var effectExp in __instance.expiringEffects)
                {
                    if (effectExp.target is Mech targetMech)
                    {
                        Mod.modLog.LogMessage($"[EffectManager_NotifyEndOfMovement_DEBUG - Prefix] Effect with ID {effectExp.EffectData.Description.Id} on unit " +
                                              $"{targetMech.DisplayName} {effectExp.targetID} should have expired");
                    }
                }

                foreach (var effectActive in __instance.effects)
                {
                    if (effectActive.target is Mech targetMech)
                    {
                        Mod.modLog.LogMessage($"[EffectManager_NotifyEndOfMovement_DEBUG - Prefix] Effect with ID {effectActive.EffectData.Description.Id} on unit " +
                                              $"{targetMech.DisplayName} {effectActive.targetID} should be active with\r{effectActive.eTimer.numActivationsRemaining} activations\r, {effectActive.eTimer.numMovementsRemaining} movements\r," +
                                              $"{effectActive.eTimer.numRoundsRemaining} rounds\r, and {effectActive.eTimer.numPhasesRemaining} phases remaining");
                    }
                }
            }

            public static void Postfix(EffectManager __instance)
            {
                foreach (var effectExp in __instance.expiringEffects)
                {
                    if (effectExp.target is Mech targetMech)
                    {
                        Mod.modLog.LogMessage($"[EffectManager_NotifyEndOfMovement_DEBUG - Postfix] Effect with ID {effectExp.EffectData.Description.Id} on unit " +
                                              $"{targetMech.DisplayName} {effectExp.targetID} should have expired");
                    }
                }

                foreach (var effectActive in __instance.effects)
                {
                    if (effectActive.target is Mech targetMech)
                    {
                        Mod.modLog.LogMessage($"[EffectManager_NotifyEndOfMovement_DEBUG - Postfix] Effect with ID {effectActive.EffectData.Description.Id} on unit " +
                                              $"{targetMech.DisplayName} {effectActive.targetID} should be active with\r{effectActive.eTimer.numActivationsRemaining} activations\r, {effectActive.eTimer.numMovementsRemaining} movements\r," +
                                              $"{effectActive.eTimer.numRoundsRemaining} rounds\r, and {effectActive.eTimer.numPhasesRemaining} phases remaining");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(EffectManager), "NotifyEndOfObjectActivation", new Type[] { typeof(string) })]
        public static class EffectManager_NotifyEndOfObjectActivation_DEBUG
        {
            public static bool Prepare() => Mod.modSettings.debugExpiration;
            public static void Prefix(EffectManager __instance)
            {
                foreach (var effectExp in __instance.expiringEffects)
                {
                    if (effectExp.target is Mech targetMech)
                    {
                        Mod.modLog.LogMessage($"[EffectManager_NotifyEndOfObjectActivation_DEBUG - Prefix] Effect with ID {effectExp.EffectData.Description.Id} on unit " +
                                              $"{targetMech.DisplayName} {effectExp.targetID} should have expired");
                    }
                }

                foreach (var effectActive in __instance.effects)
                {
                    if (effectActive.target is Mech targetMech)
                    {
                        Mod.modLog.LogMessage($"[EffectManager_NotifyEndOfObjectActivation_DEBUG - Prefix] Effect with ID {effectActive.EffectData.Description.Id} on unit " +
                                              $"{targetMech.DisplayName} {effectActive.targetID} should be active with\r{effectActive.eTimer.numActivationsRemaining} activations\r, {effectActive.eTimer.numMovementsRemaining} movements\r," +
                                              $"{effectActive.eTimer.numRoundsRemaining} rounds\r, and {effectActive.eTimer.numPhasesRemaining} phases remaining");
                    }
                }
            }

            public static void Postfix(EffectManager __instance)
            {
                foreach (var effectExp in __instance.expiringEffects)
                {
                    if (effectExp.target is Mech targetMech)
                    {
                        Mod.modLog.LogMessage($"[EffectManager_NotifyEndOfObjectActivation_DEBUG - Postfix] Effect with ID {effectExp.EffectData.Description.Id} on unit " +
                                              $"{targetMech.DisplayName} {effectExp.targetID} should have expired");
                    }
                }

                foreach (var effectActive in __instance.effects)
                {
                    if (effectActive.target is Mech targetMech)
                    {
                        Mod.modLog.LogMessage($"[EffectManager_NotifyEndOfObjectActivation_DEBUG - Postfix] Effect with ID {effectActive.EffectData.Description.Id} on unit " +
                                              $"{targetMech.DisplayName} {effectActive.targetID} should be active with\r{effectActive.eTimer.numActivationsRemaining} activations\r, {effectActive.eTimer.numMovementsRemaining} movements\r," +
                                              $"{effectActive.eTimer.numRoundsRemaining} rounds\r, and {effectActive.eTimer.numPhasesRemaining} phases remaining");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(EffectManager), "OnPhaseBegin", new Type[] { typeof(int) })]
        public static class EffectManager_OnPhaseBegin_DEBUG
        {
            public static bool Prepare() => Mod.modSettings.debugExpiration;
            public static void Prefix(EffectManager __instance)
            {
                foreach (var effectExp in __instance.expiringEffects)
                {
                    if (effectExp.target is Mech targetMech)
                    {
                        Mod.modLog.LogMessage($"[EffectManager_OnPhaseBegin_DEBUG - Prefix] Effect with ID {effectExp.EffectData.Description.Id} on unit " +
                                              $"{targetMech.DisplayName} {effectExp.targetID} should have expired");
                    }
                }

                foreach (var effectActive in __instance.effects)
                {
                    if (effectActive.target is Mech targetMech)
                    {
                        Mod.modLog.LogMessage($"[EffectManager_OnPhaseBegin_DEBUG - Prefix] Effect with ID {effectActive.EffectData.Description.Id} on unit " +
                                              $"{targetMech.DisplayName} {effectActive.targetID} should be active with\r{effectActive.eTimer.numActivationsRemaining} activations\r, {effectActive.eTimer.numMovementsRemaining} movements\r," +
                                              $"{effectActive.eTimer.numRoundsRemaining} rounds\r, and {effectActive.eTimer.numPhasesRemaining} phases remaining");
                    }
                }
            }

            public static void Postfix(EffectManager __instance)
            {
                foreach (var effectExp in __instance.expiringEffects)
                {
                    if (effectExp.target is Mech targetMech)
                    {
                        Mod.modLog.LogMessage($"[EffectManager_OnPhaseBegin_DEBUG - Postfix] Effect with ID {effectExp.EffectData.Description.Id} on unit " +
                                              $"{targetMech.DisplayName} {effectExp.targetID} should have expired");
                    }
                }

                foreach (var effectActive in __instance.effects)
                {
                    if (effectActive.target is Mech targetMech)
                    {
                        Mod.modLog.LogMessage($"[EffectManager_OnPhaseBegin_DEBUG - Postfix] Effect with ID {effectActive.EffectData.Description.Id} on unit " +
                                              $"{targetMech.DisplayName} {effectActive.targetID} should be active with\r{effectActive.eTimer.numActivationsRemaining} activations\r, {effectActive.eTimer.numMovementsRemaining} movements\r," +
                                              $"{effectActive.eTimer.numRoundsRemaining} rounds\r, and {effectActive.eTimer.numPhasesRemaining} phases remaining");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(EffectManager), "OnRoundEnd", new Type[] { typeof(int) })]
        public static class EffectManager_OnRoundEnd_DEBUG
        {
            public static bool Prepare() => Mod.modSettings.debugExpiration;
            public static void Prefix(EffectManager __instance)
            {
                foreach (var effectExp in __instance.expiringEffects)
                {
                    if (effectExp.target is Mech targetMech)
                    {
                        Mod.modLog.LogMessage($"[EffectManager_OnRoundEnd_DEBUG - Prefix] Effect with ID {effectExp.EffectData.Description.Id} on unit " +
                                              $"{targetMech.DisplayName} {effectExp.targetID} should have expired");
                    }
                }

                foreach (var effectActive in __instance.effects)
                {
                    if (effectActive.target is Mech targetMech)
                    {
                        Mod.modLog.LogMessage($"[EffectManager_OnRoundEnd_DEBUG - Prefix] Effect with ID {effectActive.EffectData.Description.Id} on unit " +
                                              $"{targetMech.DisplayName} {effectActive.targetID} should be active with\r{effectActive.eTimer.numActivationsRemaining} activations\r, {effectActive.eTimer.numMovementsRemaining} movements\r," +
                                              $"{effectActive.eTimer.numRoundsRemaining} rounds\r, and {effectActive.eTimer.numPhasesRemaining} phases remaining");
                    }
                }
            }

            public static void Postfix(EffectManager __instance)
            {
                foreach (var effectExp in __instance.expiringEffects)
                {
                    if (effectExp.target is Mech targetMech)
                    {
                        Mod.modLog.LogMessage($"[EffectManager_OnRoundEnd_DEBUG - Postfix] Effect with ID {effectExp.EffectData.Description.Id} on unit " +
                                              $"{targetMech.DisplayName} {effectExp.targetID} should have expired");
                    }
                }

                foreach (var effectActive in __instance.effects)
                {
                    if (effectActive.target is Mech targetMech)
                    {
                        Mod.modLog.LogMessage($"[EffectManager_OnRoundEnd_DEBUG - Postfix] Effect with ID {effectActive.EffectData.Description.Id} on unit " +
                                              $"{targetMech.DisplayName} {effectActive.targetID} should be active with\r{effectActive.eTimer.numActivationsRemaining} activations\r, {effectActive.eTimer.numMovementsRemaining} movements\r," +
                                              $"{effectActive.eTimer.numRoundsRemaining} rounds\r, and {effectActive.eTimer.numPhasesRemaining} phases remaining");
                    }
                }
            }
        }
    }
}
