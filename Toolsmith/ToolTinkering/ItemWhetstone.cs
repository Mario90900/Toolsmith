using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace Toolsmith.ToolTinkering {
    public class ItemWhetstone : Item {
        protected float totalSharpnessHoned = 0;

        //Copied over from the Grindstone but changed to instead be done in-hand with the items instead of on a block. It's slightly different handling.
        public void HandleSharpenTick(float secondsUsed, ItemSlot mainHandSlot, ItemSlot offhandSlot, EntityAgent byEntity, int isTool) { //"isTool" is fed by the respective items in question when they call this to try and sharpen.
            int curDur = 0;
            int maxDur = 0;
            int curSharp = 0;
            int maxSharp = 0;
            ItemStack item = mainHandSlot.Itemstack;
            ItemStack whetstone = offhandSlot.Itemstack;

            TinkeringUtility.RecieveDurabilitiesAndSharpness(ref curDur, ref maxDur, ref curSharp, ref maxSharp, item, isTool);

            TinkeringUtility.ActualSharpenTick(ref curDur, ref curSharp, ref totalSharpnessHoned, maxSharp, byEntity);

            ToolsmithModSystem.Logger.Warning("Total Sharpness Percent recovered this action: " + totalSharpnessHoned);
            ToolsmithModSystem.Logger.Warning("Seconds the Whetstone has been going: " + secondsUsed);
            whetstone.Collectible.DamageItem(byEntity.World, byEntity, offhandSlot);
            
            TinkeringUtility.SetResultsOfSharpening(curDur, curSharp, item, byEntity, mainHandSlot, isTool);

            mainHandSlot.MarkDirty();
            offhandSlot.MarkDirty();
        }

        public void DoneSharpening() {
            totalSharpnessHoned = 0;
        }
    }
}
