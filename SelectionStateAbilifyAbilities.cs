using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.UI;
using Harmony;
using static Abilifier.Mod;
using static Abilifier.MoreAbilities;
using System.Reflection;
using BattleTech.Data;
using UnityEngine;

namespace Abilifier
{
	public class SelectionStateAbilifyAbilities : SelectionStateAbilityInstant
	{
		public SelectionStateAbilifyAbilities(CombatGameState Combat, CombatHUD HUD, CombatHUDActionButton FromButton, AbstractActor actor) : base(Combat, HUD, FromButton)
		{
			base.SelectedActor = actor;
			this.HasActivated = false;
		}
		public override bool ConsumesMovement
		{
			get
			{
				return false;
			}
		}
		public override bool ConsumesFiring
		{
			get
			{
				return false;
			}
		}
		public override bool CanBackOut
		{
			get
			{
				return base.Orders == null;
			}
		}
		public override bool CanActorUseThisState(AbstractActor actor)
		{
			return true;
		}
		public override bool CanDeselect
		{
			get
			{
				if (!base.CanDeselect)
				{
					return false;
				}
				else
				{
					return (!base.SelectedActor.HasMovedThisRound || !base.SelectedActor.CanMoveAfterShooting);
				}
			}
		}
		public override bool ProcessPressedButton(string button)
		{
			if (button == "BTN_FireConfirm")
			{
				this.HideFireButton(false);

				Pilot pilot = SelectedActor.GetPilot();
				List<Ability> extraAbilities = pilot.Abilities.FindAll(x => x.Def.IsPrimaryAbility == true);
				extraAbilities.RemoveRange(0, 2);
				extraAbilities = extraAbilities.FindAll(x => x.Def.ActivationTime != AbilityDef.ActivationTiming.Passive);


				string abilityDescs = null;

				foreach (var ability in extraAbilities)
				{
					string abilityID = ability.Def.Id + "Desc";
					string abilityName = ability.Def.Description.Name;
					if (Mod.modSettings.usePopUpsForAbilityDesc == true)
					{
						abilityDescs += "[[DM.BaseDescriptionDefs[" + abilityID + "],<b>" + abilityName + "</b>]]" + "\n\n";
					}
					else
					{
						abilityDescs += ability.Def.Description.Name + ": " + ability.Def.Description.Details + "\n\n";
					}
				}

				if (wasAbilitySelected == false)
                {
					var popup = GenericPopupBuilder
					.Create("Select an ability",
					abilityDescs)
					.AddFader();
					popup.AlwaysOnTop = true;
					foreach (var ability in extraAbilities)
					{
						popup.AddButton(ability.Def.Description.Name,
							() =>
							{
								base.FromButton.InitButton(CombatHUDMechwarriorTray.GetSelectionTypeFromTargeting(ability.Def.Targeting), ability,
									ability.Def.AbilityIcon, FromButton.GUID, ability.Def.Description.Name, SelectedActor);
								wasAbilitySelected = true;
							});
					}

					popup.Render();
				}
				base.FromButton.OnClick();
				wasAbilitySelected = false;

				base.FromButton.ActivateAbility(base.SelectedActor.GUID, base.SelectedActor.GUID);
				base.isCommandComplete = true;
				this.OnInactivate();
				return true;
			}
			return base.ProcessPressedButton(button);
		}

		public bool wasAbilitySelected = false;
		private bool HasActivated = false;
	}
}
