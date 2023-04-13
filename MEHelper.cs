using Abilifier.Patches;
using BattleTech;
using BattleTech.Framework;

using HBS.Collections;
using System;
using System.Linq;
using System.Reflection;

namespace Abilifier
{
    public static class MEHelper
    {
        public static TagSet GetTags(this MechDef mechDef)
        {
            return mechDef.MechTags;
        }
        public static bool Filter(MechDef mechDef, MechComponentDef componentDef, EffectData effectData)
        {
            bool result = FilterReal(mechDef, componentDef, effectData);
            Mod.modLog.LogMessage($"Filter {mechDef.ChassisID} component:{(componentDef==null?"null":componentDef.Description.Id)} effect:{effectData.Description.Id} result:{result}");
            return result;
        }
        private static bool FilterReal(MechDef mechDef, MechComponentDef componentDef, EffectData effectData)
        {
            if (mechDef == null) { return false; }
            var extData = effectData.getStatDataExtension();
            //return true;
            switch (extData.TargetCollectionForSearch)
            {
                case EffectDataExtensionManager.EffectTargetTagSet.NotSet: break;
                case EffectDataExtensionManager.EffectTargetTagSet.Pilot: return false;
                case EffectDataExtensionManager.EffectTargetTagSet.Unit:
                    if (extData.TargetCollectionTagMatch.Overlaps(mechDef.GetTags()) == false) { return false; };
                    break;
                case EffectDataExtensionManager.EffectTargetTagSet.Component:
                    {
                        bool found = false;
                        foreach (var componentRef in mechDef.inventory)
                        {
                            if (componentRef == null) { continue; }
                            if (componentRef.Def == null) { continue; }
                            if (extData.TargetCollectionTagMatch.Overlaps(componentRef.Def.ComponentTags))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found == false) { return false; }
                    }; break;
            }
            if (extData.TargetComponentTagMatch.Count == 0) { return true; }
            if (componentDef == null)
            {
                return extData.TargetComponentTagMatch.Overlaps(mechDef.GetTags());
            }
            else
            {
                return extData.TargetComponentTagMatch.Overlaps(componentDef.ComponentTags);
            }
        }
        public static void AttachTo()
        {
            try
            {
                var assemblyName = "MechEngineer";
                var typeName = "MechEngineer.Features.OverrideStatTooltips.Helper.MechDefStatisticModifier";

                var MEHelperType = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(assembly => assembly.GetName().Name == assemblyName)
                    .Select(assembly => assembly.GetType(typeName))
                    .SingleOrDefault(type => type != null);

                if (MEHelperType != null)
                {
                    Mod.modLog.LogMessage($"{typeName} found");
                    MethodInfo RegisterFilter = AccessTools.Method(MEHelperType, "RegisterFilter");
                    if(RegisterFilter != null)
                    {
                        Mod.modLog.LogMessage($"{typeName}.RegisterFilter found");
                        RegisterFilter.Invoke(null, new object[] { "Abilifier", new Func<MechDef, MechComponentDef, EffectData, bool>(Filter) });
                    }
                }

            }
            catch (Exception e)
            {
                Mod.modLog.LogMessage(e.ToString());
            }

        }
    }
}