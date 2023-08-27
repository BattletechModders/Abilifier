## StatisticEffectData Extensions

Abilifier now ships with an additional config .json called `EffectDataExtensions.json`. This file defines optional new capabilities and restrictions for EffectData affecting StatCollections in game. 

The config comprises a Dictionary, where the "key" is equal to the EffectData.Description.Id for which you want to impose additional restrictions.

**Formatting change in 1.4.0.0**

For example, in `EffectDataExtensions.json` I have:

```
{
	"StatusEffect-PotatoAccuracy": {
		"TargetCollectionsForSearch": {
			"Component": {
				"MustMatchAll": true,
				"TargetCollectionTagMatch": [
					"test_has_tag",
					"test_has_also_tag"
				],
				"TargetCollectionNotMatch": [
					"test_has_not_tag1",
					"test_has_not_tag2"
				]
			},
			"Unit": {
				"MustMatchAll": false,
				"TargetCollectionTagMatch": [
					"test_unit_tag1",
					"test_unit_tag2"
				],
				"TargetCollectionNotMatch": [
					"test_unit_not_tag1",
					"test_unit_not_tag2"
				]
			}
		},
		"TargetComponentTagMatch": [
			"Potato_Gun",
			"Tomato_Gun"
		],
		"MustMatchAllComponent": false
	},
	"AccMod1": {
		"TargetCollectionsForSearch": {
			"Component": {
				"MustMatchAll": true,
				"TargetCollectionTagMatch": [
					"test_has_tag",
					"test_has_also_tag"
				],
				"TargetCollectionNotMatch": [
					"test_has_not_tag1",
					"test_has_not_tag2"
				]
			},
			"Unit": {
				"MustMatchAll": false,
				"TargetCollectionTagMatch": [
					"test_unit_tag1",
					"test_unit_tag2"
				],
				"TargetCollectionNotMatch": [
					"test_unit_not_tag1",
					"test_unit_not_tag2"
				]
			}
		},
		"TargetComponentTagMatch": [
			"Potato_Gun",
			"Tomato_Gun"
		],
		"MustMatchAllComponent": false
	}
}
```

On its own this will do nothing, but lets say I have an AbilityDef that contains EffectData of the following:

```
{
	"durationData": {
		"duration": 1,
		"stackLimit": 1
	},
	"targetingData": {
		"effectTriggerType": "OnActivation",
		"effectTargetType": "Creator",
		"showInStatusPanel": true
	},
	"effectType": "StatisticEffect",
	"Description": {
		"Id": "StatusEffect-PotatoAccuracy",
		"Name": "Overcharged Targeting",
		"Details": "+20 Accuracy",
		"Icon": "uixSvgIcon_PotatoGun"
	},
	"nature": "Buff",
	"statisticData": {
		"abilifierId": "AccMod1", // if this defined, Description.Id will be ignored even if it exists in EffectDataExtensions!
		"statName": "AccuracyModifier",
		"operation": "Float_Add",
		"modValue": "-20",
		"modType": "System.Single",
		"additionalRules": "NotSet",
		"targetCollection": "Weapon",
		"targetWeaponCategory": "NotSet",
		"targetWeaponType": "NotSet",
		"targetAmmoCategory": "NotSet",
		"targetWeaponSubType": "NotSet"
	}
}
```

**Important** With v1.2.0.3, StatisticEffectData blocks now have new field `abilifierId`. If defined, StatisticEffectExtensions will search for this field ID key rather than `EffectData.Description.Id`.This allows more "compact" StatisticEffectExtensions settings for effects that use the same matching logic but need different `EffectData.Description.Id`s for stackLimit purposes.

If `abilifierId` is defined, Description.Id will be ignored even if it exists in EffectDataExtensions!

Normally, the above ability would give +20 accuracy to <i>all</i> weapons on the affected unit. But because `abilifierId` "AccMod1" or (if abilifierId was not defined) `"Id": "StatusEffect-PotatoAccuracy",` from the abilitydef matches one of those keys from EffectDataExtensions (either "StatusEffect-PotatoAccuracy" or "AccMod1" in this case), the EffectDataExtensions process applies.

This is essentially a two-step process. Step one uses the `TargetCollectionsForSearch` (**note the rename in v1.4.0.0!**) to determine if the given unit is eligible for the effect to apply. Step two uses `TargetComponentTagMatch` to determine _what specific things_ on the unit the effect will apply to (generally specific weapons or components).

`TargetCollectionsForSearch` is a dictionary which can have entries with keys `NotSet`, `Component`, `Unit`, and/or `Pilot`. Using key `NotSet` will disable this first check. If multiple collections are configured as in the above example, all collections must contain a match for the effect to apply.
- `MustMatchAll` determines whether _all_ configured tags must be matched, or if _any_ single match is sufficient for a given collection.
- `TargetCollectionMatch` lists tags which must be present, while `TargetCollectionNotMatch` lists tags which must not be present.
- **NOTE**: Leaving `TargetCollectionsForSearch` null or empty, or using the `NotSet` key skips this first step of filtering, and will proceed directly to processing `TargetComponentTagMatch`. 

In the above example config for "StatusEffect-PotatoAccuracy", `TargetCollectionsForSearch` contains `Component`, with `MustMatchAll` is set to true.
- For a potential unit to meet the requirements, it must contain components
 with `test_has_tag` AND `test_has_also_tag`, and it MUST NOT contain components with `test_has_not_tag1` AND `test_has_not_tag2`.
- In addition, `TargetCollectionsForSearch` contains `Unit`, but with `MustMatchAll` set false. For a potential unit to meet the requirements, its MechDef or VehicleDef tags must contain either `test_unit_tag1`, OR `test_unit_tag2`.
- It likewise cannot have either of `test_unit_not_tag1` or `test_unit_not_tag2`. Again, requirements for both configured collections, Component and Unit, must be met.

Ok, so lets say all those requirements are met. The 2nd step filters using `TargetComponentTagMatch`.
- In this case, we have `Potato_Gun` amd `Tomato_Gun`, and `MustMatchAllComponent` is set to false. This means that components containing either the `Potato_Gun` or `Tomato_Gun` tags will receive the +20 accuracy bonus.
- Similar to above, if `MustMatchAllComponent` was set to true, only components containing _both_ `Potato_Gun` AND `Tomato_Gun` will receive the effect.
- If `TargetComponentTagMatch` is left empty, all valid components (see below note) would receive the effect.

Note: this function still respects the existing restrictions of targetCollection, targetAmmoCategory, etc., with a few differences based on what TargetCollection, if any, is selected.
- For example, the above is using `"targetCollection": "Weapon",`, so only Weapons are available to be used here, and are just filtered further by the TargetComponentTagMatch match requirement.

If targetCollection = NotSet (or missing from the EffectData), TargetComponentTagMatch searches the unit tags of the target if it is a valid AbstractActor. So if it is a Mech, it will search for a matching MechDef tag, Vehicle: VehicleDef Tag, and Turret: TurretDef Tag.

If targetCollection = Pilot, TargetComponentTagMatch searches the pilotTags of the targets pilot.