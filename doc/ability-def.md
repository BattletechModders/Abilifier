## Additional AbilityDef Fields

A few new fields have been added to AbilityDefs, providing some additional functionality.

### CBill Costs

An optional new field in AbilityDefs "CBillCost" defines a per-use cbill cost for using the ability. Does not need to be paired with ResolveCost or use the Resolverator module as discussed below.

### Ability Single Targeting Fix

An optional new field in AbilityDefs "TargetFriendlyUnit" allows abilities with `"Targeting": "ActorTarget",` and `"effectTargetType": "SingleTarget",` to target friendly units, enemies, or both. 

`"TargetFriendlyUnit": "BOTH",` - ability can target both Friendly and Enemy units

`"TargetFriendlyUnit": "FRIENDLY",` - ability can target only Friendly units

`"TargetFriendlyUnit": "ENEMY",` - ability can target only Enemy units

Does not need to be paired with ResolveCost or use the Resolverator module as discussed below.

### Universal Cooldown Tweaks

An optional new field in AbilityDefs "TriggersUniversalCooldown" determines whether an ability triggers the vanilla "universal cooldown" when activated. Vanilla behavior = true, where activating an ability makes all other abilities unavailable for that activation. Adding this field and setting it to false in the AbilityDef will prevent that ability from triggering the universal cooldown for all other abilities.

Additionally, an optional new field in AbilityDefs "IgnoresUniversalCooldown" determines whether <i>that specific ability</i> is subject to the universal cooldown. Adding this field and setting it to true in the AbilityDef will prevent this ability from being subject to the universal cooldown.

#### Start in Cooldown

An optional new field in AbilityDefs "StartInCooldown" will start the contract with the ability in its cooldown period if set to true.

#### Ability use restricted by tags

An optional new array of strings in AbilityDefs "RestrictedTags" will set the Ability to be unavailable if the unit or the units pilot has any of the defined tags. In addition, ability description text can now accept either `[RestrictedTags]` or `{11}` to parse the description tags into the description so you don't have to manually type them. 
