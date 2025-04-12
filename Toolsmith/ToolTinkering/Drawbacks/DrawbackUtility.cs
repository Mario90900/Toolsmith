using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;

namespace Toolsmith.ToolTinkering.Drawbacks {
    public static class DrawbackUtility {

        //Check and see if a drawback is rolled and then have it applied.
        public static void TryChanceForDrawback(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, float sharpnessPercent) {
            int oneInThis = 1;
            if (sharpnessPercent > 0.55) {
                oneInThis = 20000;
            } else if (sharpnessPercent > 0.3) {
                oneInThis = 5000;
            } else if (sharpnessPercent > 0) {
                oneInThis = 1000;
            }

            if (oneInThis != 1) {
                if (world.Rand.Next(oneInThis) == 0) {
                    ApplyRandomDrawback(world, byEntity, itemslot, sharpnessPercent);
                }
            } else {
                ApplyRandomDrawback(world, byEntity, itemslot, sharpnessPercent);
            }
        }

        //Check for valid drawbacks for this tool type given, then try rolling for one to apply it.
        public static void ApplyRandomDrawback(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, float sharpnessPercent) {
            ToolsmithModSystem.Logger.Warning("A Tool should have had a Drawback applied!");
        }
    }
}
