using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using UnityEngine;
using static Abilifier.Framework.GlobalVars;

namespace Abilifier.Framework
{
    public static class GlobalVars
    {
        public const string aiPilotFlag = "_AI_TEMP_RESOLVERATOR";
        public const string rGUID = "RESOLVERATOR_";
    }

    public static class CombatHUDMoraleBarInstance
    {
        public static CombatHUDMoraleBar CHMB;
    }
    public class PilotResolveInfo
    {
        public int PilotResolve;
        public bool CanInspire;
        public bool Predicting;
        public int PilotMaxResolve;
        public int PredictedResolve;
    }

    public class PilotResolveTracker
    {
        public int selectedAbilityResolveCost;
        public static PilotResolveTracker _instance;
        public Dictionary<string, PilotResolveInfo> pilotResolveDict;

        public static PilotResolveTracker HolderInstance
        {
            get
            {
                if (_instance == null) _instance = new PilotResolveTracker();
                return _instance;
            }
        }

        public void Initialize()
        {
            pilotResolveDict = new Dictionary<string, PilotResolveInfo>();
            selectedAbilityResolveCost = 0;
        }

        public void ModifyPendingMoraleOverride(ref Dictionary<AbstractActor, int> moraleChanges, AbstractActor attacker, AbstractActor target, int enemyAmount, int allyAmount, bool targetAlliedToAttacker, bool isSameTeam)
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

        public void ModifyPendingMoraleForUnit(ref Dictionary<AbstractActor, int> moraleChanges, AbstractActor unit, int amount)
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
        public static float GetResolveGenBaseMult(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<float>("resolveGenBaseMult");
        }

        public static float GetResolveCostBaseMult(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<float>("resolveCostBaseMult");
        }

        public static float GetResolveRoundBaseMod(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<float>("resolveRoundBaseMod");
        }

        public static float getTeamMoraleMultiplier(this AbstractActor actor)
        {
            var combat = UnityGameInstance.BattleTechGame.Combat;
            MoraleConstantsDef activeMoraleDef = combat.Constants.GetActiveMoraleDef(combat);
            return 1f + combat.TurnDirector.NumInspiredActionsTaken * activeMoraleDef.InspirationAccelerationMultiplier;
        }

        public static void ModifyResolve(this AbstractActor actor, int amt)
        {
            if (amt > 0)
            {
                amt = (int)(amt * actor.getTeamMoraleMultiplier());
            }
            actor.ModifyResolve("", -1, amt);
        }

        public static void ModifyResolve(this AbstractActor actor, AttackDirector.AttackSequence sequence, int amt)
        {
            amt = (int)(amt * actor.getTeamMoraleMultiplier());
            actor.ModifyResolve(sequence.id.ToString(), sequence.stackItemUID, amt);
        }

        public static void ModifyResolve(this AbstractActor actor, string id, int stackItemUID, int amt)
        {
            var actorKey = actor.GetPilot().Fetch_rGUID();
            if (!PilotResolveTracker.HolderInstance.pilotResolveDict.TryGetValue(actorKey, out var pilotResolveInfo))
                return;
            var combat = UnityGameInstance.BattleTechGame.Combat;
            var actorTeam = actor.team;
            var moraleLogger = Team.moraleLogger;//Traverse.Create(actorTeam).Field("moraleLogger").GetValue<ILog>();
            var attackLogger = Team.attackLogger;//Traverse.Create(actorTeam).Field("attackLogger").GetValue<ILog>();
            var resolveCostBaseMult = actor.GetResolveCostBaseMult();
            var resolveGenBaseMult = actor.GetResolveGenBaseMult();
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
                //var resolveGenBaseMult = actor.StatCollection.GetValue<float>("resolveGenBaseMult");
                //var resolveGenTacticsMult = actor.StatCollection.GetValue<float>("resolveGenTacticsMult");
                Mod.modLog.LogMessage($"Unit {actor.DisplayName} Base resolve gain: {amt} * resolveGenBaseMult {resolveGenBaseMult} = {Mathf.RoundToInt(amt * resolveGenBaseMult)}");
                amt = Mathf.RoundToInt(amt * resolveGenBaseMult);
                
                //if (resolveGenTacticsMult != 0)
                //{
                //    Mod.modLog.LogMessage($"Unit {actor.DisplayName} Base resolve gain: {amt} * Tactics {actor.SkillTactics} * resolveGenTacticsMult {resolveGenTacticsMult} = {Mathf.RoundToInt(amt * actor.SkillTactics * resolveGenTacticsMult)} + {amt} = {amt + Mathf.RoundToInt(amt * actor.SkillTactics * resolveGenTacticsMult)}");
                //    amt += Mathf.RoundToInt(amt * actor.SkillTactics * resolveGenTacticsMult);
                //}
            }

            else
            {
                //var resolveCostTacticsMult = actor.StatCollection.GetValue<float>("resolveCostTacticsMult");
                Mod.modLog.LogMessage($"Unit {actor.DisplayName} Base resolve loss: {amt} * resolveCostBaseMult {resolveCostBaseMult} = {Mathf.RoundToInt(amt * resolveCostBaseMult)}");
                amt = Mathf.RoundToInt(amt * resolveCostBaseMult);
                //if (resolveCostTacticsMult != 0)
               // {
               //     Mod.modLog.LogMessage($"Unit {actor.DisplayName} Base resolve loss: {amt} * Tactics {actor.SkillTactics} * resolveCostTacticsMult {resolveCostTacticsMult} = {Mathf.RoundToInt(amt * actor.SkillTactics * resolveCostTacticsMult)} + {amt} = {amt + Mathf.RoundToInt(amt * actor.SkillTactics * resolveCostTacticsMult)}");
               //     amt += Mathf.RoundToInt(amt * actor.SkillTactics * resolveCostTacticsMult);
               // }
            }

            attackLogger.Log(
                $"RESOLVE: Unit {actor.DisplayName} has current {pilotResolveInfo.PilotResolve} resolve; adding {amt} new resolve. Total to be bounded 0 - {pilotResolveInfo.PilotMaxResolve}");
            var totalResolve = Mathf.Clamp(pilotResolveInfo.PilotResolve + amt, 0, pilotResolveInfo.PilotMaxResolve);
            pilotResolveInfo.PilotResolve = totalResolve;
            combat.MessageCenter.PublishMessage(new MoraleChangedMessage(actorTeam.GUID));
            attackLogger.Log($"RESOLVE: Unit {actor.DisplayName} now has {pilotResolveInfo.PilotResolve} resolve");
            Mod.modLog.LogMessage($"RESOLVE: Unit {actor.DisplayName} now has {pilotResolveInfo.PilotResolve} resolve");

            if (pilotResolveInfo.PilotResolve >= pilotResolveInfo.PilotMaxResolve)
            {
                combat.MessageCenter.PublishMessage(new ReportMoraleMaxMessage());
            }

            if (pilotResolveInfo.PilotResolve >= (activeMoraleDef.CanUseInspireLevel * resolveCostBaseMult) || activeMoraleDef.CanUseInspireLevel <= 0)
            {
                pilotResolveInfo.CanInspire = true;
            }
            else if (pilotResolveInfo.PilotResolve < (activeMoraleDef.CanUseInspireLevel * resolveCostBaseMult))
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
                    if (actor is Mech mech)
                    {
                        mech.InspireResolverator("", -1);
                        mech.Combat.MessageCenter.PublishMessage(new MoraleChangedMessage(mech.GUID));
                    }
                }
            }
            else if (!actorTeam.CanInspire && activeMoraleDef.AutoInspire)
            {
                actorTeam.UnInspireUnit(actor);
            }
        }

        public static void InspireResolverator(this Mech mech, string sourceID, int stackItemID)
        {
            AbstractActor.attackLogger.Log(string.Format("MORALE: {0} is inspired", mech.LogDisplayName));
            MoraleConstantsDef activeMoraleDef = mech.Combat.Constants.GetActiveMoraleDef(mech.Combat);
            mech.CreateEffect(activeMoraleDef.InspiredEffect, null, sourceID, stackItemID, mech, false);
            if (activeMoraleDef.FreeStandUpAction)
            {
                mech.DoFreeStandUpAction();
            }
            bool flag = activeMoraleDef.CameraHighlight && (mech.Combat.LocalPlayerTeam.IsFriendly(mech.team) || mech.Combat.LocalPlayerTeam.VisibilityToTarget(mech) == VisibilityLevel.LOSFull);
            string name = activeMoraleDef.InspiredEffect.Description.Name;
            mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(mech, name, FloatieMessage.MessageNature.Inspiration, flag)));
            WwiseManager.PostEvent<AudioEventList_ui>(AudioEventList_ui.ui_callout_inspired_buff, WwiseManager.GlobalAudioObject, null, null);
            AudioEventManager.PlayPilotVO(VOEvents.Pilot_Inspired, mech, null, null, true);
            mech.Combat.TurnDirector.NotifyInspiredAction();
            if (activeMoraleDef.AutoInspire && activeMoraleDef.InspireCost > 0)
            {
                AbstractActor.attackLogger.LogError("ERROR: morale is set to AutoInspire, but cost is greater than 0... could cause a loop!");
            }
            mech.ModifyResolve(activeMoraleDef.InspireCost * -1);
        }
    }

    public static class PilotExtensions
    {
        public static string Fetch_rGUID(this Pilot pilot)
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