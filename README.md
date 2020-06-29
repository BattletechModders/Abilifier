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
Using the above, a player reaching Gunnery 5 will have the choice between the abilities with Ids `AbilityDefG5` and `AbilityDefG5a`. Using the existing naming scheme (appending "a", "b", etc. for new abilities in a skill tree) is recommended.

Lastly, developers have a choice between the display style for the ability selection pop-up. The default method is to display the entire body of the Ability details in the AbilityDef .json, e.g:
![TextPop](https://github.com/BattletechModders/Abilifier/blob/master/doc/textpopup.png)

The alternative style is to use a hover tooltip, but this requires a little extra work on the developers' part:
![HoverPop](https://github.com/BattletechModders/Abilifier/blob/master/doc/tooltippopup.png)

To use a hover tooltip, you will need to create BaseDescriptionDef (essentially lore hover items) .jsons for <b>all</b> abilities, including vanilla abilities, and place them in `Abilifier/AbilityDescs`. The BaseDescriptionDef Id <b>must</b> be identical to the AbilityDef Id, ending in "Desc" as below: 
```
{
    "Id" : "AbilityDefP5aDesc",
    "Name" : "MOSTEST SUREST FOOTING",
    "Details" : "PASSIVE: 'Mechs piloted by this MechWarrior gain ALL THE EVASIVE CHARGES after moving (can exceed the unit's maximum). If the move is not a sprint, jump, or charge to melee, the 'Mech also gains ENTRENCHED (50% stability damage reduction).",
    "Icon" : ""
}
```
