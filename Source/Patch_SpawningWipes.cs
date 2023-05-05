using HarmonyLib;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;
using RimWorld;
 
namespace UndergroundConduits
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            new Harmony("owlchemist.undergroundpowerconduits").PatchAll();
        }
    }
    
    [HarmonyPatch (typeof(GenSpawn), nameof(GenSpawn.SpawningWipes))]
    class Patch_GenSpawn_SpawningWipes
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var editor = new CodeMatcher(instructions);
            System.Object label = null, label2 = null;
            // --------------------------ORIGINAL--------------------------
            // if (thingDef.IsFrame && GenSpawn.SpawningWipes(thingDef.entityDefToBuild, oldEntDef))
            
            editor.Start().MatchEndForward(
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ThingDef), nameof(ThingDef.EverTransmitsPower))),
                new CodeMatch(i => {
                    if (i.opcode == OpCodes.Brfalse_S)
                    {
                        label = i.operand;
                        return true;
                    }
                    return false;
                })
            );
            if (!editor.IsInvalid)
            {
                // --------------------------MODIFIED--------------------------
                // if (thingDef.IsFrame && Validate(oldEntDef))

                editor
                .Advance(1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_1))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_GenSpawn_SpawningWipes), nameof(Validate))))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, label))
                .RemoveInstructions(3);
            }

            editor.MatchStartForward(
                // --------------------------ORIGINAL--------------------------
                // return thingDef2.entityDefToBuild == ThingDefOf.PowerConduit &&...

                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(ThingDef), nameof(ThingDefOf.PowerConduit))),
                new CodeMatch(i => {
                    if (i.opcode == OpCodes.Bne_Un_S)
                    {
                        label2 = i.operand;
                        return true;
                    }
                    return false;
                })
            );
            if (!editor.IsInvalid)
            {
                // --------------------------MODIFIED--------------------------
                // return Patch_GenSpawn_SpawningWipes.ValidateBuildableDef(thingDef2.entityDefToBuild)) &&...

                return editor
                .Advance(2)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_GenSpawn_SpawningWipes), nameof(ValidateBuildableDef))))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, label2))
                .RemoveInstructions(2)
                .InstructionEnumeration();
            }
            
            Log.Error("[Underground Power Conduits] Transpiler could not find target. There may be a mod conflict, or RimWorld updated?");
            return editor.InstructionEnumeration();
        }

        public static bool Validate(ThingDef thingDef)
        {
            if (thingDef == ThingDefOf.PowerConduit) return true;
            return thingDef.IsBlueprint ? thingDef.entityDefToBuild == ThingDefOf.PowerConduit || thingDef.entityDefToBuild.HasModExtension<PowerConduit>() : thingDef.HasModExtension<PowerConduit>();
        }
        public static bool ValidateBuildableDef(BuildableDef thingDef)
        {
            return thingDef == ThingDefOf.PowerConduit || (thingDef != null && thingDef.HasModExtension<PowerConduit>());
        }
    }

    public class PowerConduit : DefModExtension { }
}