using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Toolsmith.ToolTinkering {

    //[HarmonyPatch(typeof(CollectibleObject))]
    //[HarmonyPatchCategory(ToolsmithModSystem.ToolTinkeringDamagePatchCategory)]
    public class ToolRenderingPatches { //For generic patches that might be rquired for Tool Rendering purposes! Not needed yet so leaving it commented out.

    }

    [HarmonyPatch(typeof(ItemHoe))]
    [HarmonyPatchCategory(ToolsmithModSystem.ToolTinkeringDamagePatchCategory)]
    public class ToolRenderingHoePatches {

        [HarmonyTranspiler]
        [HarmonyPatch(nameof(ItemHoe.OnLoaded))]
        public static IEnumerable<CodeInstruction> ItemHoeOnLoadedCallBase(IEnumerable<CodeInstruction> instructions) {
            var codes = new List<CodeInstruction>(instructions);

            var addBaseCall = new List<CodeInstruction> {
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadArgument(1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CollectibleObject), "OnLoaded", new Type[1] { typeof(ICoreAPI) }))
            };

            codes.InsertRange(5, addBaseCall);

            return codes.AsEnumerable();
        }
    }
}
