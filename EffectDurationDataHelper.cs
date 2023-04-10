using System.Reflection.Emit;
using System.Reflection;
using System;

namespace Abilifier
{
    public static class EffectDurationDataHelper
    {
        private delegate string d_Field_get(BattleTech.EffectDurationData src);
        private delegate void d_Field_set(BattleTech.EffectDurationData src, string value);
        private static d_Field_get i_stackId_get = null;
        private static d_Field_set i_stackId_set = null;
        private static d_Field_get i_abilifierId_get = null;
        private static d_Field_set i_abilifierId_set = null;
        public static string stackId(this BattleTech.EffectDurationData src)
        {
            if (i_stackId_get == null) { return string.Empty; }
            return i_stackId_get(src);
        }
        public static void stackId(this BattleTech.EffectDurationData src, string value)
        {
            if (i_stackId_set == null) { return; }
            i_stackId_set(src, value);
        }
        public static string abilifierId(this BattleTech.EffectDurationData src)
        {
            if (i_abilifierId_get == null) { return string.Empty; }
            return i_abilifierId_get(src);
        }
        public static void abilifierId(this BattleTech.EffectDurationData src, string value)
        {
            if (i_abilifierId_set == null) { return; }
            i_abilifierId_set(src, value);
        }
        public static void Prepare()
        {
            FieldInfo stackId = typeof(BattleTech.EffectDurationData).GetField("stackId", BindingFlags.Public | BindingFlags.Instance);
            Mod.modLog.LogMessage($"EffectDurationData.stackId {(stackId == null ? "not found" : "found")}");
            if (stackId != null)
            {
                {
                    var dm = new DynamicMethod("get_stackId", typeof(string), new Type[] { typeof(BattleTech.EffectDurationData) });
                    var gen = dm.GetILGenerator();
                    gen.Emit(OpCodes.Ldarg_0);
                    gen.Emit(OpCodes.Ldfld, stackId);
                    gen.Emit(OpCodes.Ret);
                    i_stackId_get = (d_Field_get)dm.CreateDelegate(typeof(d_Field_get));
                }
                {
                    var dm = new DynamicMethod("set_stackId", null, new Type[] { typeof(BattleTech.EffectDurationData), typeof(string) });
                    var gen = dm.GetILGenerator();
                    gen.Emit(OpCodes.Ldarg_0);
                    gen.Emit(OpCodes.Ldarg_1);
                    gen.Emit(OpCodes.Stfld, stackId);
                    gen.Emit(OpCodes.Ret);
                    i_stackId_set = (d_Field_set)dm.CreateDelegate(typeof(d_Field_set));
                }
            }
            FieldInfo abilifierId = typeof(BattleTech.EffectDurationData).GetField("abilifierId", BindingFlags.Public | BindingFlags.Instance);
            Mod.modLog.LogMessage($"EffectDurationData.abilifierId {(abilifierId == null ? "not found" : "found")}");
            if (abilifierId != null)
            {
                {
                    var dm = new DynamicMethod("get_abilifierId", typeof(string), new Type[] { typeof(BattleTech.EffectDurationData) });
                    var gen = dm.GetILGenerator();
                    gen.Emit(OpCodes.Ldarg_0);
                    gen.Emit(OpCodes.Ldfld, abilifierId);
                    gen.Emit(OpCodes.Ret);
                    i_abilifierId_get = (d_Field_get)dm.CreateDelegate(typeof(d_Field_get));
                }
                {
                    var dm = new DynamicMethod("set_abilifierId", null, new Type[] { typeof(BattleTech.EffectDurationData), typeof(string) });
                    var gen = dm.GetILGenerator();
                    gen.Emit(OpCodes.Ldarg_0);
                    gen.Emit(OpCodes.Ldarg_1);
                    gen.Emit(OpCodes.Stfld, abilifierId);
                    gen.Emit(OpCodes.Ret);
                    i_abilifierId_set = (d_Field_set)dm.CreateDelegate(typeof(d_Field_set));
                }
            }
        }
    }

}