using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using Harmony;
using HBS.Logging;
using UnityEngine;
using static Abilifier.Framework.GlobalVars;

namespace Abilifier.Framework
{
    public static class GlobalVars
    {
        internal const string aiPilotFlag = "_AI_TEMP_RESOLVERATOR";
        internal const string rGUID = "RESOLVERATOR_";
    }

    public static class CombatHUDMoraleBarInstance
    {
        public static CombatHUDMoraleBar CHMB;
    }
    public class PilotResolveInfo
    {
        public int PilotResolve;
        public bool CanInspire;
        public int PilotMaxResolve;

    }

    public class PilotResolveTracker
    {
        public int selectedAbilityResolveCost;
        private static PilotResolveTracker _instance;
        public Dictionary<string, PilotResolveInfo> pilotResolveDict;

        public static PilotResolveTracker HolderInstance
        {
            get
            {
                if (_instance == null) _instance = new PilotResolveTracker();
                return _instance;
            }
        }

        internal void Initialize()
        {
            pilotResolveDict = new Dictionary<string, PilotResolveInfo>();
            selectedAbilityResolveCost = 0;
        }

        internal void ModifyPendingMoraleOverride(ref Dictionary<AbstractActor, int> moraleChanges, AbstractActor attacker, AbstractActor target, int enemyAmount, int allyAmount, bool targetAlliedToAttacker, bool isSameTeam)
        {
            if (!targetAlliedToAttacker && !isSameTeam)
            {
                this.ModifyPendingMoraleForUnit(ref moraleChanges, attacker, enemyAmount);
            }
            if (targetAlliedToAttacker || isSameTeam)
            {
                this.ModifyPendingMoraleForUnit(ref moraleChanges, attacker, allyAmount);
            }
            if (targetAlliedToAttacker && !isSameTeam)
            {
                this.ModifyPendingMoraleForUnit(ref moraleChanges, target, allyAmount);
            }
        }

        internal void ModifyPendingMoraleForUnit(ref Dictionary<AbstractActor, int> moraleChanges, AbstractActor unit, int amount)
        {
            moraleChanges.TryGetValue(unit, out var num);
            Mod.modLog.LogMessage($"Tried to get current moraleChanges for unit {unit.DisplayName}: {num} to add {amount}");
            num += amount;
            moraleChanges[unit] = num;
            Mod.modLog.LogMessage($"moraleChanges should now be {moraleChanges[unit]}");
        }
    }

    public static class ActorExtensions
    {
        internal static float getTeamMoraleMultiplier(this AbstractActor actor)
        {
            var combat = UnityGameInstance.BattleTechGame.Combat;
            MoraleConstantsDef activeMoraleDef = combat.Constants.GetActiveMoraleDef(combat);
            return 1f + combat.TurnDirector.NumInspiredActionsTaken * activeMoraleDef.InspirationAccelerationMultiplier;
        }

        internal static void ModifyResolve(this AbstractActor actor, int amt)
        {
            if (amt > 0)
            {
                amt = (int)(amt * actor.getTeamMoraleMultiplier());
            }
            actor.ModifyResolve("", -1, amt);
        }

        internal static void ModifyResolve(this AbstractActor actor, AttackDirector.AttackSequence sequence, int amt)
        {
            amt = (int)(amt * actor.getTeamMoraleMultiplier());
            actor.ModifyResolve(sequence.id.ToString(), sequence.stackItemUID, amt);
        }

        internal static void ModifyResolve(this AbstractActor actor, string id, int stackItemUID, int amt)
        {
            var actorKey = actor.GetPilot().Fetch_rGUID();
            var pilotResolveInfo = PilotResolveTracker.HolderInstance.pilotResolveDict[actorKey];
            var combat = UnityGameInstance.BattleTechGame.Combat;
            var actorTeam = actor.team;
            var moraleLogger = Traverse.Create(actorTeam).Field("moraleLogger").GetValue<ILog>();
            var attackLogger = Traverse.Create(actorTeam).Field("attackLogger").GetValue<ILog>();
            if (actorTeam.IsMoraleSuppressed)
            {
                moraleLogger.Log("Morale is suppressed for this mission; doing nothing");
                Mod.modLog.LogMessage("Morale is suppressed for this mission; doing nothing");

                return;
            }
            if (amt == 0)
            {
                moraleLogger.Log("Attempted to modify morale by 0; doing nothing");
                Mod.modLog.LogMessage("Attempted to modify morale by 0; doing nothing");
                return;
            }
            MoraleConstantsDef activeMoraleDef = combat.Constants.GetActiveMoraleDef(combat);
            if (!activeMoraleDef.CanAIBeInspired && actorTeam is AITeam)
            {
                moraleLogger.Log("Attempted to modify AI team morale when it cannot be inspired; doing nothing");
                Mod.modLog.LogMessage("Attempted to modify AI team morale when it cannot be inspired; doing nothing");
                return;
            }

            if (amt > 0)
            {
                var resolveGenBaseMult = actor.StatCollection.GetValue<float>("resolveGenBaseMult");
                var resolveGenTacticsMult = actor.StatCollection.GetValue<float>("resolveGenTacticsMult");
                Mod.modLog.LogMessage($"Unit {actor.DisplayName} Base resolve gain: {amt} * resolveGenBaseMult {resolveGenBaseMult} = {Mathf.RoundToInt(amt * resolveGenBaseMult)}");
                amt = Mathf.RoundToInt(amt * resolveGenBaseMult);
                
                if (resolveGenTacticsMult != 0)
                {
                    Mod.modLog.LogMessage($"Unit {actor.DisplayName} Base resolve gain: {amt} * Tactics {actor.SkillTactics} * resolveGenTacticsMult {resolveGenTacticsMult} = {Mathf.RoundToInt(amt * actor.SkillTactics * resolveGenTacticsMult)} + {amt} = {amt + Mathf.RoundToInt(amt * actor.SkillTactics * resolveGenTacticsMult)}");
                    amt += Mathf.RoundToInt(amt * actor.SkillTactics * resolveGenTacticsMult);
                }
            }

            else
            {
                var resolveCostBaseMult = actor.StatCollection.GetValue<float>("resolveCostBaseMult");
                var resolveCostTacticsMult = actor.StatCollection.GetValue<float>("resolveCostTacticsMult");
                Mod.modLog.LogMessage($"Unit {actor.DisplayName} Base resolve loss: {amt} * resolveCostBaseMult {resolveCostBaseMult} = {Mathf.RoundToInt(amt * resolveCostBaseMult)}");
                amt = Mathf.RoundToInt(amt * resolveCostBaseMult);
                if (resolveCostTacticsMult != 0)
                {
                    Mod.modLog.LogMessage($"Unit {actor.DisplayName} Base resolve loss: {amt} * Tactics {actor.SkillTactics} * resolveCostTacticsMult {resolveCostTacticsMult} = {Mathf.RoundToInt(amt * actor.SkillTactics * resolveCostTacticsMult)} + {amt} = {amt + Mathf.RoundToInt(amt * actor.SkillTactics * resolveCostTacticsMult)}");
                    amt += Mathf.RoundToInt(amt * actor.SkillTactics * resolveCostTacticsMult);
                }
            }

            attackLogger.Log(
                $"RESOLVE: Unit {actor.DisplayName} has current {pilotResolveInfo.PilotResolve} resolve; adding {amt} new resolve");
            if (pilotResolveInfo.PilotResolve + amt >= pilotResolveInfo.PilotMaxResolve)
            {
                pilotResolveInfo.PilotResolve = pilotResolveInfo.PilotMaxResolve;
            }
            else
            {
                pilotResolveInfo.PilotResolve += amt;
            }

            combat.MessageCenter.PublishMessage(new MoraleChangedMessage(actorTeam.GUID));
            attackLogger.Log($"RESOLVE: Unit {actor.DisplayName} now has {pilotResolveInfo.PilotResolve} resolve");
            Mod.modLog.LogMessage($"RESOLVE: Unit {actor.DisplayName} now has {pilotResolveInfo.PilotResolve} resolve");

            if (pilotResolveInfo.PilotResolve >= pilotResolveInfo.PilotMaxResolve)
            {
                combat.MessageCenter.PublishMessage(new ReportMoraleMaxMessage());
            }

            if (pilotResolveInfo.PilotResolve >= activeMoraleDef.CanUseInspireLevel)
            {
                pilotResolveInfo.CanInspire = true;
            }
            else if (pilotResolveInfo.PilotResolve < activeMoraleDef.CanUseInspireLevel)
            {
                pilotResolveInfo.CanInspire = false;
            }

            if (pilotResolveInfo.CanInspire)
            {
                if (actorTeam.LocalPlayerControlsTeam)
                {
                    AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "mechwarrior_morale_up");
                }
                if (activeMoraleDef.AutoInspire)
                {
                    actorTeam.InspireUnit(actor);
                }
            }
            else if (!actorTeam.CanInspire && activeMoraleDef.AutoInspire)
            {
                actorTeam.UnInspireUnit(actor);
            }
        }
    }

    public static class PilotExtensions
    {
        internal static string Fetch_rGUID(this Pilot pilot)
        {
            var guid = pilot.pilotDef.PilotTags.FirstOrDefault(x => x.StartsWith(rGUID));
            if (string.IsNullOrEmpty(guid))
            {
                Mod.modLog.LogMessage($"WTF IS {pilot.Callsign}'s GUID NULL?!");
            }
            return guid;
        }
    }
}