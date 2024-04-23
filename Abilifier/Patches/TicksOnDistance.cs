using BattleTech;
using System;
using UnityEngine;

namespace Abilifier.Patches
{
    public static class TicksOnDistance
    {
        public static bool DecrementMovementsByDistance(this ETimer timer, AbstractActor actor)
        {
            if (timer.numMovementsRemaining > 0)
            {
                var dist = Mathf.RoundToInt(actor.DistMovedThisRound);
                timer.numMovementsRemaining = Mathf.Max(0, timer.numMovementsRemaining - dist);
                Mod.modLog?.Info?.Write($"[DecrementMovementsByDistance] Processed {actor.DisplayName}, remaining dist in effect {timer.numMovementsRemaining} from {dist} movement");
                if (timer.numMovementsRemaining <= 0)
                {
                    timer.isRunning = false;
                    return false;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(EffectManager), "NotifyEndOfMovement", new Type[] { typeof(string) })]
        public static class EffectManager_NotifyEndOfMovement
        {
            public static void Prefix(ref bool __runOriginal, EffectManager __instance, string targetGUID)
            {
                if (!__runOriginal) return;
                var actor = __instance.Combat.FindActorByGUID(targetGUID);
                if (actor == null)
                {
                    __runOriginal = true;
                    return;
                }
                //Mod.modLog?.Info?.Write($"[EffectManager_NotifyEndOfMovement] Processing end of movement for {actor.DisplayName} - {actor.GetPilot().Callsign}");
                
                __instance.expiringEffects.Clear();
                for (int i = 0; i < __instance.effects.Count; i++)
                {
                    Effect effect = __instance.effects[i];

                    //Mod.modLog?.Info?.Write($"[EffectManager_NotifyEndOfMovement] checking {effect.EffectData.Description.Id}");
                    if (Mod.modSettings.ticksOnMovementDistanceIDs.Contains(effect.EffectData.Description.Id))
                    {
                        Mod.modLog?.Info?.Write($"[EffectManager_NotifyEndOfMovement] found settings for {effect.EffectData.Description.Id}");
                        if (effect.targetID == targetGUID)
                        {
                            if (effect.Duration.isRunning)
                            {
                                effect.OnEffectMovementEnd();
                            }
                            if (!effect.Duration.DecrementMovementsByDistance(actor))
                            {
                                __instance.expiringEffects.Add(effect);
                            }
                        }
                    }
                    else
                    {
                        if (effect.Duration.activationActorGUID == targetGUID)
                        {
                            if (effect.Duration.isRunning)
                            {
                                effect.OnEffectMovementEnd();
                            }
                            if (!effect.Duration.DecrementMovements())
                            {
                                __instance.expiringEffects.Add(effect);
                            }
                        }
                    }
                }

                for (int j = __instance.expiringEffects.Count - 1; j >= 0; j--)
                {
                    __instance.expiringEffects[j].OnEffectExpiration();
                }
                __runOriginal = false;
                return;
            }
        }
    }
}
