## Trait Pip Generation

As part of [Ability Realizer](doc/ability_realizer-json.md) functionality, tooltips **can** be generated for `TraitDef` files. However, tooltips require the following data:

```
"Description" : {
		"Id" : "TraitDef__________",
		"Name" : "NAME OF TRAIT",
		"Details" : "DESCRIPTION OF TRAIT",
		"Icon" : ""
	},
```

If `Description` fields `"Name"` and `"Details"` are left unpopulated, no pip will be generated, as seen with several stock traits:

```
"Description" : {
		"Id" : "TraitDefMeleeHit1",
		"Name" : "",
		"Details" : "",
		"Icon" : ""
	},
```

