using BattleTech;
using BattleTech.Save.SaveGameStructure;
using ShaderControl;
using SVGImporter.LibTessDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;
using Effect = BattleTech.Effect;

namespace Abilifier.Patches
{
    public class DebugPatches
    {
        [HarmonyPatch(typeof(Effect), MethodType.Constructor,
            new Type[] {typeof(CombatGameState), typeof(string), typeof(int),
                typeof(object), typeof(object), typeof(string), typeof(string), typeof(EffectData), typeof(WeaponHitInfo), typeof(int)})]
        public static class Effect_cctor
        {
            public static bool Prepare() => Mod.modSettings.debugExpiration;
            public static void Prefix(Effect __instance, CombatGameState combat, string effectID, int stackItemUID, object creator, object target,
                string creatorID, string targetID, EffectData effectData, WeaponHitInfo hitInfo, int attackIndex = -1)
            {
                if (target is Mech mech)
                {
                    Mod.modLog.LogMessage($"[Effect_cctor - Prefix] Constrtucting Effect with ID {effectID} / {effectData.Description.Id} on target {mech.DisplayName} {mech.GUID}");
                    Mod.modLog.LogMessage($"[Effect_cctor - Prefix] Effect with ID {effectData.Description.Id} should have following duration: {effectData.durationData.duration} with\r ticksOnActivations {effectData.durationData.ticksOnActivations}\r ticksOnMovements {effectData.durationData.ticksOnMovements}\r ticksOnEndOfRound {effectData.durationData.ticksOnEndOfRound} or default phases");
                }
            }
            public static void Postfix(Effect __instance, CombatGameState combat, string effectID, int stackItemUID, object creator, object target,
                string creatorID, string targetID, EffectData effectData, WeaponHitInfo hitInfo, int attackIndex = -1)
            {
                if (target is Mech mech)
                {
                    Mod.modLog.LogMessage($"[Effect_cctor - Postfix] Constructed Effect with ID {effectID} / {__instance.effectData.Description.Id} on target {mech.DisplayName} {mech.GUID}");
                    Mod.modLog.LogMessage($"[Effect_cctor - Postfix] Effect with ID {__instance.effectData.Description.Id} should have following durations: {__instance.eTimer.numActivationsRemaining} activations\r, {__instance.eTimer.numMovementsRemaining} movements\r," +
                                          $"{__instance.eTimer.numRoundsRemaining} rounds\r, and {__instance.eTimer.numPhasesRemaining} phases remaining");
                }
            }
        }


        [HarmonyPatch(typeof(ETimer), "DurationInRounds", new Type[] {typeof(int), typeof(string)})]
        public static class ETimer_DurationInRounds
        {
            public static bool Prepare() => Mod.modSettings.debugExpiration;
            public static void Postfix(int numRoundsRemaining, string effectCreatorGUID, ref ETimer __result)
            {
                Mod.modLog.LogMessage($"[DurationInRounds - Postfix] Constructed ETimer with {numRoundsRemaining} for creator {effectCreatorGUID}. {__result.numRoundsRemaining} rounds, isRunning {__result.isRunning}");
            }
        }

        [HarmonyPatch(typeof(ETimer), "DurationInMovements", new Type[] { typeof(int), typeof(string) })]
        public static class ETimer_DurationInMovements
        {
            public static bool Prepare() => Mod.modSettings.debugExpiration;
            public static void Postfix(int numMovementsRemaining, string effectCreatorGUID, ref ETimer __result)
            {
                Mod.modLog.LogMessage($"[DurationInMovements - Postfix] Constructed ETimer with {numMovementsRemaining} for creator {effectCreatorGUID}. {__result.numMovementsRemaining} movements, isRunning {__result.isRunning}");
            }
        }

        [HarmonyPatch(typeof(ETimer), "DurationInPhases", new Type[] { typeof(int), typeof(string) })]
        public static class ETimer_DurationInPhases
        {
            public static bool Prepare() => Mod.modSettings.debugExpiration;
            public static void Postfix(int numPhasesRemaining, string effectCreatorGUID, ref ETimer __result)
            {
                Mod.modLog.LogMessage($"[ETimer_DurationInPhases - Postfix] Constructed ETimer with {numPhasesRemaining} for creator {effectCreatorGUID}. {__result.numPhasesRemaining} phases, isRunning {__result.isRunning}");
            }
        }

        [HarmonyPatch(typeof(ETimer), "DurationInActivations", new Type[] { typeof(int), typeof(string), typeof(string)})]
        public static class ETimer_DurationInActivations
        {
            public static bool Prepare() => Mod.modSettings.debugExpiration;
            public static void Postfix(int numActivationsRemaining, string activationActorGUID, string effectCreatorGUID, ref ETimer __result)
            {
                Mod.modLog.LogMessage($"[ETimer_DurationInActivations - Postfix] Constructed ETimer with {numActivationsRemaining} for creator {effectCreatorGUID}. {__result.numActivationsRemaining} activations, isRunning {__result.isRunning}");
            }
        }

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
