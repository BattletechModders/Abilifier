## AbilityRealizer

As of v1.3.0.0, Abilifier has absorbed the AbilityRealizer mod. Settings for AbilityRealizer module can be found in `AbilityRealizerSettings.json` in the Abilifier mod folder. All existing settings and behavior have been maintained.

## Settings clarification:

Provide support for modding the ability tree and abilities without requiring modders to completely redo all of the PilotDefs, as well as providing a mechanism for updating pilots that are already stored in saves.

`DumpAbilityDefNamesAtAddToTeam` - bool, if true all pilot AbilityDefNames will be dumped to log when added to teams for debugging purposes

`AddTreeAbilities` - bool, if true pilots will be given abilities as appropriate according to the ability tree in simgameconstants. doesnt play nicely with core Abilifier functionality of allowing multiple ability options, and may result in pilots being given moore abilities than they can legally have. No, I'm not updating it to work.

`RemoveNonTreeAbilities` - bool, if true pilots will have abilities removed which are not present in the SGC ability tree. Really only useful if you change/deprecate an abilitydef ID, but can result in pilots losing unique abilities that are not in the ability tree.

`IgnoreAbilities` - list<string> - list of abilitydef IDs which are ignored by above Add/Remove settings. 

Original readme for AbilityRealizer follows:

### What It Currently Does

* Keeps all pilots/pilot defs up-to-date with the current state of the ability tree (stored in SimGameConstants)

* Prevent crashes/save game loss from changing the ability tree

* Changes the barracks UI to show tooltips for passive abilities that are not primary abilities

* Can add abilities based on Faction or Tag

* Can swap abilities for the AI (until adding to the AI is added)

### Ignoring Pilots By Tag

Pilots that have any of these tags will be ignored

```json
"IgnorePilotsWithTags": [ "pilot_release_skirmish", "pilot_release_ksbeta" ]
```

### Adding Abilities based on Faction/Tag

Add to the `FactionAbilities` or `TagAbilities` in the settings

```json
"FactionAbilities": {
    "AuriganPirates": [ "MyAbilityDef1", "MyAbilityDef2" ]
},
```

```json
"TagAbilities": {
    "commander_career_soldier": [ "MyAbilityDef3", "MyAbilityDef4" ]
},
```

### Swapping AI Abilities

Add to the `SwapAIAbilities` in the settings

```json
"SwapAIAbilities": {
    "AbilityDefG8": "AbilityDefG8AI"
},
```

### Upgrading Abilities

The setting "UpgradeAbilities" takes an array of "UpgradeAbiility" values which must have the following defined:

"Prereq" - AbilityDef ID of the prerequisite ability which will be replaced
"Skill" - skill type, e.g. Guts
"Level" - level of the skill, e.g. 7
"Upgrade" - AbilityDef ID of the "upgraded" ability that will replace the "Prereq" ability.

e.g.:
```
"UpgradeAbilities": [
			{
				"Prereq": "AbilityDef_CarefulManeuvers",
				"Skill": "Piloting",
				"Level": 5,
				"Upgrade": "AbilityDef_LessCarefulMoreManeuvers"
			}
		]
```