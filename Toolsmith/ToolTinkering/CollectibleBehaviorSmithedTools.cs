﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Toolsmith.ToolTinkering {
    public class CollectibleBehaviorSmithedTools : CollectibleBehavior {

        public CollectibleBehaviorSmithedTools(CollectibleObject collObj) : base(collObj) {
            
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            if (inSlot.Itemstack == null || inSlot.Inventory.GetType() == typeof(DummyInventory) || inSlot.Inventory.GetType() == typeof(CreativeInventoryTab)) {
                return;
            }

            var curSharp = inSlot.Itemstack.GetToolCurrentSharpness(); //All that needs to be added to the stringbuilder is the Sharpness.
            var maxSharp = inSlot.Itemstack.GetToolMaxSharpness();

            StringBuilder workingDsc = new StringBuilder();
            workingDsc.Append(dsc); //Still for safety sake, lets copy dsc into a temp one for active processing.
            int startIndex = 0;
            int endIndex = 0;

            StringHelpers.FindTooltipVanillaDurabilityLine(ref startIndex, ref endIndex, workingDsc, world, withDebugInfo); //Moved this code originally from TinkerTools into it's own helper function.

            workingDsc.Insert(startIndex, Lang.Get("toolsharpness", curSharp, maxSharp) + '\n');

            dsc.Clear();
            dsc.Append(workingDsc);
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
