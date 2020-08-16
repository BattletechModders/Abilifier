# Abilifier

This is a mod for HBS Battletech that allows developers to give players <b>choices</b> when it comes to leveling up abilities. To use, place your new AbilityDefs in an appropriate subfolder, either in the Abilifier folder or elsewhere in your modpack, and ensure you have the appropriate manifest entry in your mod.json so modtek will load the new AbilityDefs, e.g.
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
Using the above, a player reaching Gunnery 5 will have the choice between the abilities with Ids `AbilityDefG5` and `AbilityDefG5a`. Using the existing naming scheme (appending "a", "b", etc. for new abilities in a skill tree) is not required, but is recommended for potential future compatibiliy.

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

Settings available in the mod.json:
```
  "Settings": {
	"enableTrace": false,
	"enableLog": false,
	"usePopUpsForAbilityDesc": false,
	"debugXP": true,
	"extraFirstTierAbilities": 1,
	"extraAbilities": 0,
	"extraAbilitiesAllowedPerSkill": 0,
	"abilityReqs":
		{
		"AbilityDefG5a":["AbilityDefG8a","AbilityDefG8b"],
		"AbilityDefGu5a":["AbilityDefGu8a"]
		}
	},
```
`enableTrace` and `enableLog` (bools) allow logging.

`usePopUpsForAbilityDesc` bool, sets Abilifier to use hover tooltips for Ability descriptions as described above.

`debugXP` bool, grants 100000 XP when XP is spent (useful for testing newly added abilities).

`extraFirstTierAbilities` int, allows players to take additional 1st tier abilities within the limit of total abilities.

`extraAbilities` int, allows players to take additional (>3) abilities. <b>Currently, only the first 3 abilities selected will be visible and (if an activated ability) usable in combat. extra passive abilities will work, however. THIS IS VERY MUCH A WIP!</b>

`extraAbilitiesAllowedPerSkill` int, allows players to take additional (>2) abilities within a given skill area. <b>Assumes developer has included new abilities at skill levels other than 5 and 8, also VERY MUCH A WIP.</b> Only 1 ability per-skill-level may be taken, however.

`abilityReqs` dictionary, strings. new in 1.05, allows devs to set up true ability "trees", where the 1st ability (dictionary key) is required for the player to take any of the subsequently listed abilities (dictionary value, list of strings). For example, in the above settings, a player can <i>only</i> take `AbilityDefG8a` or `AbilityDefG8b` if they had previously taken `AbilityDefG5a`; all other abilities for this level will be available. At present, abilities with requirements must be of the same skill type as their required abilities (e.g., Gunnery8b cannot require Guts5c).

Unavailable abilities will still be shown in the ability-chooser dialogue box, with text added indicating what missing ability is required, but they will not be selectable:
![HoverPop](https://github.com/BattletechModders/Abilifier/blob/master/doc/abilityReqs.png)
