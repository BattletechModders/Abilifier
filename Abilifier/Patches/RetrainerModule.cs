using BattleTech.UI;
using BattleTech;
using HBS;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine;
using Strings = Localize.Strings;

namespace Abilifier.Patches
{

    public class RetrainerSettings
    {
        public bool enableRetrainer = true;
        public int cost;
        public bool onceOnly;
        public bool trainingModuleRequired;
        public List<string> ignoreSuffix = new List<string>();
        public List<string> ignoredAbilities = new List<string>();
        public string confirmAbilityText =
            "Confirming this Ability selection is permanent. You may only have two Primary Abilities and one Specialist Ability, and MechWarriors cannot be retrained.";
    }

    public static class RetrainerModule
    {
        public static T? FindObject<T>(this GameObject go, string name) where T : Component
        {
            T?[] arr = go.GetComponentsInChildren<T>(true);
            foreach (T? component in arr)
            {
                if (component?.gameObject.transform.name == name)
                {
                    return component;
                }
            }
            return null;
        }

        public static bool Retrain(SGBarracksMWDetailPanel instance)
        {
            UIColorRef backfill = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill;
            var simState = instance.simState;
            if (Mod.modSettings.retrainerSettings.onceOnly && instance.curPilot.pilotDef.PilotTags.Contains("HasRetrained"))
            {
                GenericPopupBuilder
                    .Create(Strings.CurrentCulture == Strings.Culture.CULTURE_RU_RU
                            ? "Не могу переподготовить"
                            : "Unable To Retrain"
                        , Strings.CurrentCulture == Strings.Culture.CULTURE_RU_RU ? "Каждый пилот может пилот может быть переподготовлен только один раз" : "Each pilot can only retrain once."
                    )
                    .AddButton(Strings.CurrentCulture == Strings.Culture.CULTURE_RU_RU ? "Понятненько" : "Acknowledged")
                    .CancelOnEscape()
                    .AddFader(backfill)
                    .Render();
                return false;
            }

            if (Mod.modSettings.retrainerSettings.trainingModuleRequired &&
                !simState.ShipUpgrades.Any(u => u.Tags.Any(t => t.Contains("argo_trainingModule2"))))
            {
                GenericPopupBuilder
                    .Create(
                        Strings.CurrentCulture == Strings.Culture.CULTURE_RU_RU
                            ? "Не могу переподготовить"
                            : "Unable To Retrain",
                        Strings.CurrentCulture == Strings.Culture.CULTURE_RU_RU
                            ? "Нужно иметь дважды улучшенный тренировочный модуль"
                            : "You must have built the Training Module 2 upgrade aboard the Argo."
                    )
                    .AddButton(Strings.CurrentCulture == Strings.Culture.CULTURE_RU_RU ? "Понятненько" : "Acknowledged")
                    .CancelOnEscape()
                    .AddFader(backfill)
                    .Render();
                return false;
            }

            if (simState.Funds < Mod.modSettings.retrainerSettings.cost)
            {
                GenericPopupBuilder
                    .Create(
                        Strings.CurrentCulture == Strings.Culture.CULTURE_RU_RU
                            ? "Не могу переподготовить"
                            : "Unable To Retrain",
                        Strings.CurrentCulture == Strings.Culture.CULTURE_RU_RU
                            ? $"Нужно иметь ¢{Mod.modSettings.retrainerSettings.cost:N0}."
                            : $"You need ¢{Mod.modSettings.retrainerSettings.cost:N0}.")
                    .AddButton(Strings.CurrentCulture == Strings.Culture.CULTURE_RU_RU ? "Понятненько" : "Acknowledged")
                    .CancelOnEscape()
                    .AddFader(backfill)
                    .Render();
                return false;
            }

            var message = Mod.modSettings.retrainerSettings.onceOnly
                ? Strings.CurrentCulture == Strings.Culture.CULTURE_RU_RU
                    ? $"Переподготовка сбросит все навыки на 1 и вернет очки опыта\nОбойдется в ¢{Mod.modSettings.retrainerSettings.cost:N0}, каждый пилот может быть переподготовлен один раз"
                    : $"This will set skills to 1 and refund all XP.\nIt will cost ¢{Mod.modSettings.retrainerSettings.cost:N0} and each pilot can only retrain once."
                
                : Strings.CurrentCulture == Strings.Culture.CULTURE_RU_RU
                    ? $"Переподготовка сбросит все навыки на 1 и вернет очки опыта\nОбойдется в ¢{Mod.modSettings.retrainerSettings.cost:N0}."
                    : $"This will set skills to 1 and refund all XP.\nIt will cost ¢{Mod.modSettings.retrainerSettings.cost:N0}"
                ;

            GenericPopupBuilder
                .Create(Strings.CurrentCulture == Strings.Culture.CULTURE_RU_RU ? "Переподготовка" : "Retrain", message)
                .AddButton("Cancel")
                .AddButton(
                    Strings.CurrentCulture == Strings.Culture.CULTURE_RU_RU
                        ? "Переподготовить пилота"
                        : "Retrain Pilot", () => RespecAndRefresh(instance, instance.curPilot))
                .CancelOnEscape()
                .AddFader(backfill)
                .Render();
            return true;
        }

        // thanks kmission for fancy button
        public class RetrainButtonSupervisor : MonoBehaviour
        {
            public SGBarracksMWDetailPanel parent { get; set; } = null;
            public HBSDOTweenButton retrainButton = null;
            public bool ui_inited = true;

            public void initUI()
            {
                try
                {
                    if (retrainButton == null)
                    {
                        return;
                    }

                    HorizontalLayoutGroup layout =
                        retrainButton.transform.parent.gameObject.GetComponent<HorizontalLayoutGroup>();
                    layout.spacing = 15f;
                    var pos = retrainButton.transform.parent.transform.localPosition;
                    pos.x -= retrainButton.gameObject.GetComponent<RectTransform>().sizeDelta.x * 0.6f;
                    retrainButton.transform.parent.transform.localPosition = pos;
                }
                catch (Exception e)
                {
                    Framework.Logger.LogException(e);
                }

                ui_inited = true;
            }

            public void Update()
            {
                if (ui_inited == false)
                {
                    initUI();
                }
            }

            public void OnClicked()
            {
                if (Retrain(parent))
                {
                    parent.DisplayPilot(parent.curPilot);
                }
            }

            public void Instantine()
            {
                retrainButton = Instantiate(parent.advancementReset.gameObject)
                    .GetComponent<HBSDOTweenButton>();
                retrainButton.gameObject.transform.SetParent(parent.advancementReset.gameObject.transform
                    .parent);
                retrainButton.gameObject.transform.SetAsFirstSibling();
                retrainButton.OnClicked = new UnityEngine.Events.UnityEvent();
                retrainButton.OnClicked.AddListener(new UnityEngine.Events.UnityAction(OnClicked));
                retrainButton.SetText(Strings.CurrentCulture == Strings.Culture.CULTURE_RU_RU
                    ? "ПЕРЕПОДГОТОВКА"
                    : "RETRAIN");
                retrainButton.SetState(ButtonState.Enabled);
                parent.advancement.gameObject.FindObject<Image>("line")?.gameObject.SetActive(false);
                ui_inited = false;
            }

            public void Init(SGBarracksMWDetailPanel parent)
            {
                this.parent = parent;
                if (retrainButton == null)
                {
                    Instantine();
                }
            }
        }

        [HarmonyPatch(typeof(SGBarracksMWDetailPanel))]
        [HarmonyPatch("Initialize")]
        [HarmonyPatch(MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(SimGameState) })]
        public static class MechComponent_InitPassiveSelfEffects
        {
            public static bool Prepare() => Mod.modSettings.retrainerSettings.enableRetrainer;
            public static void Prefix(SGBarracksMWDetailPanel __instance, SimGameState state)
            {
                try
                {
                    RetrainButtonSupervisor retrainBnt = __instance.gameObject.GetComponent<RetrainButtonSupervisor>();
                    if (retrainBnt == null)
                    {
                        retrainBnt = __instance.gameObject.AddComponent<RetrainButtonSupervisor>();
                    }

                    retrainBnt.Init(__instance);
                }
                catch (Exception e)
                {
                    Framework.Logger.LogException(e);
                }
            }
        }



        [HarmonyPatch(typeof(SGBarracksMWDetailPanel), nameof(SGBarracksMWDetailPanel.OnSkillsSectionClicked),
            MethodType.Normal)]
        public static class SGBarracksMWDetailPanel_OnSkillsSectionClicked_Patch
        {
            public static bool Prepare() => Mod.modSettings.retrainerSettings.enableRetrainer;
            public static void Prefix(SGBarracksMWDetailPanel __instance, ref bool __runOriginal)
            {
                if (!__runOriginal) return;
                var hotkeyPerformed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (!hotkeyPerformed)
                {
                    __runOriginal = true;
                    return;
                }

                __runOriginal = Retrain(__instance) == false;
                return;
            }
        }

        public static void RespecAndRefresh(SGBarracksMWDetailPanel __instance, Pilot pilot)
        {
            WipePilotStats(pilot);
            UnityGameInstance.BattleTechGame.Simulation.AddFunds(-Mod.modSettings.retrainerSettings.cost);
            pilot.pilotDef.PilotTags.Add("HasRetrained");
            __instance.DisplayPilot(pilot);
        }

        public static void OnAcceptPilotConfirm(SGBarracksAdvancementPanel advancement, SimGameState simState,
            SGBarracksWidget barracks, Pilot tempPilot)
        {
            advancement.Close();
            simState.UpgradePilot(tempPilot);
            barracks.Reset(tempPilot);
        }

        [HarmonyPatch(typeof(SGBarracksMWDetailPanel), "OnPilotConfirmed")]
        public static class SGBarracksMWDetailPanel_OnPilotConfirmed
        {
            public static bool Prepare() => Mod.modSettings.retrainerSettings.enableRetrainer && !string.IsNullOrEmpty(Mod.modSettings.retrainerSettings.confirmAbilityText);
            public static void Prefix(SGBarracksMWDetailPanel __instance, ref bool __runOriginal)
            {
                if (!__runOriginal) return;
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                if (__instance.advancement.PendingPrimarySkillUpgrades())
                {
                    GenericPopupBuilder.Create("Complete Training?", $"{Mod.modSettings.retrainerSettings.confirmAbilityText}")
                        .AddButton("Cancel", null, true, null)
                        .AddFader(
                            new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants
                                .FadeToHalfBlack), 0f, true).CancelOnEscape().AddButton("Confirm",
                            delegate
                            {
                                OnAcceptPilotConfirm(__instance.advancement, sim, __instance.barracks,
                                    __instance.tempPilot);
                            }, true, null)
                        .AddFader(
                            new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants
                                .PopupBackfill), 0f, true).Render();
                    __runOriginal = false;
                    return;
                }

                OnAcceptPilotConfirm(__instance.advancement, sim, __instance.barracks, __instance.tempPilot);
                __runOriginal = false;
                return;
            }
        }

        // copied and changed from RespecPilot()
        public static void WipePilotStats(Pilot pilot)
        {
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            var pilotDef = pilot.pilotDef.CopyToSim();

            foreach (var value in sim.Constants.Story.CampaignCommanderUpdateTags)
            {
                if (!sim.CompanyTags.Contains(value))
                {
                    sim.CompanyTags.Add(value);
                }
            }

            try
            {

                Mod.modLog.LogMessage($"Base:\t {pilotDef.BaseGunnery} / {pilotDef.BasePiloting} / {pilotDef.BaseGuts} / {pilotDef.BaseTactics}");
                Mod.modLog.LogMessage($"Bonus:\t {pilotDef.BonusGunnery} / {pilotDef.BonusPiloting} / {pilotDef.BonusGuts} / {pilotDef.BonusTactics}");

                var num = 0;
                num += sim.GetLevelRangeCost(1, pilotDef.SkillGunnery - 1);
                num += sim.GetLevelRangeCost(1, pilotDef.SkillPiloting - 1);
                num += sim.GetLevelRangeCost(1, pilotDef.SkillGuts - 1);
                num += sim.GetLevelRangeCost(1, pilotDef.SkillTactics - 1);

                pilotDef.BaseGunnery = 1;
                pilotDef.BasePiloting = 1;
                pilotDef.BaseGuts = 1;
                pilotDef.BaseTactics = 1;
                pilotDef.BonusGunnery = 1;
                pilotDef.BonusPiloting = 1;
                pilotDef.BonusGuts = 1;
                pilotDef.BonusTactics = 1;

                // pilotDef.abilityDefNames.Clear();
                for (var index = pilotDef.abilityDefNames.Count - 1; index >= 0; index--)
                {
                    var abilityName = pilotDef.abilityDefNames[index];
                    if (!Mod.modSettings.retrainerSettings.ignoredAbilities.Contains(abilityName) &&
                        !Mod.modSettings.retrainerSettings.ignoreSuffix.Any(x => abilityName.EndsWith(x)))
                    {
                        pilotDef.abilityDefNames.Remove(abilityName);
                    }
                }
                //pilotDef.abilityDefNames.RemoveAll(x => !Mod.modSettings.retrainerSettings.ignoredAbilities.Contains(x));


                pilotDef.SetSpentExperience(0);
                pilotDef.ForceRefreshAbilityDefs();
                pilotDef.ResetBonusStats();
                pilot.FromPilotDef(pilotDef);
                pilot.AddExperience(0, "Respec", num);
            }
            catch (Exception e)
            {
                Framework.Logger.LogException(e);
            }
        }
    }
}