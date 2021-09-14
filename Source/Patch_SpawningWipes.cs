using Verse;
using HarmonyLib;
 
namespace UndergroundConduits
{
    public class Mod_UndergroundConduits : Mod
	{	
		public Mod_UndergroundConduits(ModContentPack content) : base(content)
		{
			new Harmony(this.Content.PackageIdPlayerFacing).PatchAll();
		}
    }
    
    [HarmonyPatch (typeof(GenSpawn), "SpawningWipes")]
    static class Patch_SpawningWipes
    {
        private static void Postfix(BuildableDef newEntDef, BuildableDef oldEntDef, ref bool __result)
        {
            if (newEntDef.ChangeType<ThingDef>().EverTransmitsPower && oldEntDef.HasModExtension<PowerConduit>()) __result = true;
        }
    }

    public class PowerConduit : DefModExtension 
	{
	}
}