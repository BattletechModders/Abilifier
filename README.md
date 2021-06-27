# Abilifier

This is a mod for HBS Battletech that allows developers to give players <b>choices</b> when it comes to leveling up abilities. Special thanks to kMiSSioN for allowing the reuse of their code adding additional ability buttons to the Combat HUD.

To use, place your new AbilityDefs in an appropriate subfolder, either in the Abilifier folder or elsewhere in your modpack, and ensure you have the appropriate manifest entry in your mod.json so modtek will load the new AbilityDefs, e.g.
```
 "Manifest": [
		{ "Type": "AbilityDef", "Path": "Abilities" },
```
<b>In addition</b>, you must add the new Ability Ids to `SimGameConstants` under the `Progression` entry, e.g.:
```
	"Progression" : {
		"GunnerySkills" :[
			[
				"TraitDefWeaponHit1"
			],
			[
				"TraitDefWeaponHit2"
			],
			[
				"TraitDefWeaponHit3"
			],
			[
				"TraitDefWeaponHit4"
			],
			[
				"TraitDefWeaponHit5",
				"AbilityDefG5",
				"AbilityDefG5a"
			],
```
Using the above, a player reaching Gunnery 5 will have the choice between the abilities with Ids `AbilityDefG5` and `AbilityDefG5a`. Using the existing naming scheme (appending "a", "b", etc. for new abilities in a skill tree) is not required, but is recommended for potential future compatibility.

Lastly, developers have a choice between the display style for the ability selection pop-up. The default method is to display the entire body of the Ability details in the AbilityDef .json, e.g:
![TextPop](https://github.com/BattletechModders/Abilifier/blob/master/doc/textpopup.png)

The alternative style is to use a hover tooltip, but this requires a little extra work on the developers' part:
![HoverPop](https://github.com/BattletechModders/Abilifier/blob/master/doc/tooltippopup.png)

To use a hover tooltip, you will need to create BaseDescriptionDef (essentially lore hover items) .jsons for <b>all</b> abilities, including any vanilla abilities being used, and place them in `Abilifier/AbilityDescs`. The BaseDescriptionDef Id <b>must</b> be identical to the AbilityDef Id, ending in "Desc" as below: 
```
{
    "Id" : "AbilityDefP5aDesc",
    "Name" : "MOSTEST SUREST FOOTING",
    "Details" : "PASSIVE: 'Mechs piloted by this MechWarrior gain ALL THE EVASIVE CHARGES after moving (can exceed the unit's maximum). If the move is not a sprint, jump, or charge to melee, the 'Mech also gains ENTRENCHED (50% stability damage reduction).",
    "Icon" : ""
}
```

## New Module - Resolverator!

If enabled, this module tracks resolve separately per-pilot rather than as a team. In addition, <i>regular</i> abilities can now have a resolve cost associated with them. Abilities' resolve cost is dictated by adding a "ResolveCost" field to the AbilityDef:
eg
```
{
    "Description": {},
    "DisplayParams": "ShowInMWTRay",
    "ReqSkill": "Gunnery",
    "ReqSkillLevel": 5,
    "ActivationTime": "ConsumedByFiring",
    "ActivationCooldown": 4,
    "Targeting": "ActorSelf",
    "ResolveCost": 25,
```

Tracking resolve costs per-pilot means a hefty rebalance of resolve generation will likely be needed. In addition to the values in CombatGameConstants under `"MoraleConstants": {`, other values that may need changing are:
```
    "MoraleCostAttackDescription": {
      "Name": "PRECISION STRIKE COST",
      "Details": "Cost: 30 Resolve"
    },
    "MoraleCostAttackDescriptionLow": {
      "Name": "PRECISION STRIKE COST LOW",
      "Details": "Cost: <color=#F04228FF>40 Resolve (this MechWarrior has Low Spirits)</color>"
    },
    "MoraleCostAttackDescriptionHigh": {
      "Name": "PRECISION STRIKE COST HIGH",
      "Details": "Cost: <color=#85DBF6FF>20 Resolve (this MechWarrior has High Spirits)</color>"
    },
    "MoraleCostDefendDescription": {
      "Name": "VIGILANCE COST",
      "Details": "Cost: 30 Resolve"
    },
    "MoraleCostDefendDescriptionLow": {
      "Name": "VIGILANCE COST LOW",
      "Details": "Cost: <color=#F04228FF>40 Resolve (this MechWarrior has Low Spirits)</color>"
    },
    "MoraleCostDefendDescriptionHigh": {
      "Name": "VIGILANCE COST HIGH",
      "Details": "Cost: <color=#85DBF6FF>20 Resolve (this MechWarrior has High Spirits)</color>"
    },
```

### Settings

Settings available in the mod.json:
```
  "Settings": {
	"enableTrace": false,
	"enableLog": false,
	"enableResolverator": true,
	"resolveGenTacticsMult": 0.1,
	"resolveCostTacticsMult": 0.05,
	"resolveGenBaseMult": 1.0,
	"resolveCostBaseMult": 1.0,
	"usePopUpsForAbilityDesc": false,
	"debugXP": false,
	"extraFirstTierAbilities": 0,
	"extraAbilities": 1,
	"extraAbilitiesAllowedPerSkill": 1,
	"cleanUpCombatUI": true,
	"nonTreeAbilities": 1,
	"skillLockThreshold":8,
	"extraPreCapStoneAbilities":0,
	"usingCACabilitySelector":false,
	"abilityReqs":
		{
		"AbilityDefG5a":["AbilityDefG8a","AbilityDefG8b"],
		"AbilityDefGu5a":["AbilityDefGu8a"]
		}
	},
```
`enableTrace` and `enableLog` (bools) allow logging.

`enableResolverator` - bool, enables pilot resolve overhaul module

`resolveGenTacticsMult` - float, multiplier modifies resolve generation according to tactics skill

`resolveCostTacticsMult` - float, multiplier modifies resolve costs of abilities according to tactics skill

`resolveGenBaseMult` - float, base multiplier for all resolve generation

`resolveCostBaseMult` - float, base multiplier for all resolve costs

`usePopUpsForAbilityDesc` bool, sets Abilifier to use hover tooltips for Ability descriptions as described above.

`debugXP` bool, grants 100000 XP when XP is spent (useful for testing newly added abilities).

`extraFirstTierAbilities` int, allows players to take additional 1st tier abilities within the limit of total abilities.

`extraAbilities` int, allows players to take additional (>3) abilities. Theoretically only limited by screen space. Individual buttons get closer to eachother as more abilites are added, but usable with 5 additional abilities. Image below is with 5 additional abilities and `cleanUpCombatUI` set to true.

![HoverPop](https://github.com/BattletechModders/Abilifier/blob/master/doc/alltheabilities.png)

`extraAbilitiesAllowedPerSkill` int, allows players to take additional (>2) abilities within a given skill area. <b>Assumes developer has included new abilities at skill levels other than 5 and 8.</b> Only 1 ability per-skill-level may be taken, however.

`cleanUpCombatUI` bool, if `true`, removes decorative chevrons and vertical bars from the ability tray in combat. Recommend setting to `true` unless NOT enabling CAC Attack Ground button AND `extraAbilities` <=1.

`nonTreeAbilities` int; adds extra button slots in combat UI for abilities to be added to PilotDefs <i>independent</i> of the ability tree. Think Victoria's Fire and Steel ability, that sort of thing. Abilities added to pilots in this way <b>must not</b> have `IsPrimaryAbility`, `ReqSkill`, or `ReqSkillLevel` in their AbilityDefs. Likewise, they should <b>not</b> be added to the Progression section of SimGameConstants. They <b>do</b>, however need `"DisplayParams" : "ShowInMWTRay",` if you want to be able to see them in the ability tray in missions.

`skillLockThreshold` int, defines a threshold level for skills; once a pilot has reached that skill level, they can *only* take abilities above that level in that skill. Intended for use with additional lvl 10 abilities; in the settings above, once a pilot reaches lvl 8 (2nd tier ability by default), the only abilities at or above lvl 8 that they can take must be in that skill. However, pilots can always take abilities *below* the threshold (within the constraints of the above settings). Set to 10 (effectively nonfunctional) by default.

To clarify: a pilot could, for example, have abilities at the following levels:
```
Gunnery: 5, 8, 10
Guts: 5
```
but you could not have 
```
Gunnery: 5, 8
Guts: 5, 8
```

`extraPreCapStoneAbilities` int. Decides additional number of skills allowed to give abilities before the CapStone ability (works in concert with skillLockThreshold).

`usingCACabilitySelector` bool. if true, makes Abilifier compatible with newer versions of CustomBundle/CAC that incorporate an "ability selector" for the combat UI. If false, Abilifer will create `extraAbilities + nonTreeAbilities` traditional ability button slots.

`abilityReqs` dictionary, strings. new in 1.05, allows devs to set up true ability "trees", where the 1st ability (dictionary key) is required for the player to take any of the subsequently listed abilities (dictionary value, list of strings). For example, in the above settings, a player can <i>only</i> take `AbilityDefG8a` or `AbilityDefG8b` if they had previously taken `AbilityDefG5a`; all other abilities for this level will be available. At present, abilities with requirements must be of the same skill type as their required abilities (e.g., Gunnery8b cannot require Guts5c).

Unavailable abilities will still be shown in the ability-chooser dialogue box, with text added indicating what missing ability is required, but they will not be selectable:

![HoverPop](https://github.com/BattletechModders/Abilifier/blob/master/doc/abilityReqs.png)
