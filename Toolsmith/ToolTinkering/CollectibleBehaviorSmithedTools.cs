using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Utils;
using Vintagestory.API.Common;

namespace Toolsmith.ToolTinkering {
    public class CollectibleBehaviorSmithedTools : CollectibleBehavior {

        public CollectibleBehaviorSmithedTools(CollectibleObject collObj) : base(collObj) {
            
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            if (ToolsmithModSystem.Config.DebugMessages) {
                dsc.AppendLine("This is a Smithed Tool!");
            }
        }

        public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, ref EnumHandling bhHandling) {
            ItemStack foundToolInput = null; //I do hope this gets called when Smithing completes. I think it should?
            if (allInputslots.Length > 0) {
                foreach (var slot in allInputslots.Where(i => i.Itemstack != null)) {
                    if (slot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>()) {
                        foundToolInput = slot.Itemstack.Clone();
                    }
                }
            }

            bool isToolMetal = false;
            if (foundToolInput != null) { //If it's a recipe where a tool is being converted or repaired I guess? Kind of a sanity check, but I know a few things like QP Pantograph might get caught by this.
                outputSlot.Itemstack.SetSmithedDurability(foundToolInput.GetSmithedDurability());
                outputSlot.Itemstack.SetToolCurrentSharpness(foundToolInput.GetToolCurrentSharpness());
                outputSlot.Itemstack.SetToolMaxSharpness(foundToolInput.GetToolMaxSharpness());
                return;
            } else {
                isToolMetal = outputSlot.Itemstack.Collectible.IsCraftableMetal();
            }

            var baseDur = outputSlot.Itemstack.Collectible.GetBaseMaxDurability(outputSlot.Itemstack);
            var toolDur = (int)(baseDur * ToolsmithModSystem.Config.HeadDurabilityMult);
            int sharpness = (int)(baseDur * ToolsmithModSystem.Config.SharpnessMult);
            int startingSharpness;
            if (isToolMetal) {
                startingSharpness = (int)(sharpness * ToolsmithConstants.StartingSharpnessMult);
            } else {
                startingSharpness = (int)(sharpness * ToolsmithConstants.NonMetalStartingSharpnessMult);
            }

            outputSlot.Itemstack.SetSmithedDurability(toolDur);
            outputSlot.Itemstack.SetToolCurrentSharpness(startingSharpness);
            outputSlot.Itemstack.SetToolMaxSharpness(sharpness);

            if (ToolsmithModSystem.Config.DebugMessages) {
                ToolsmithModSystem.Logger.Debug("Tool's durability is: " + baseDur);
                ToolsmithModSystem.Logger.Debug("So the Smithed Durability is: " + toolDur);
                ToolsmithModSystem.Logger.Debug("And the starting Sharpness is: " + startingSharpness);
                ToolsmithModSystem.Logger.Debug("Finally the max Sharpness is: " + sharpness);
                if (allInputslots.Length > 0) {
                    ToolsmithModSystem.Logger.Debug("This tool had input slots! Was it grid crafted?");
                } else {
                    ToolsmithModSystem.Logger.Debug("This tool had no input slots! Was it smithed?");
                }
            }
        }
    }
}
