using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Toolsmith.ToolTinkering.Blocks {
    public class WorkbenchInventory : InventoryGeneric {

        public ItemSlot reforgeStagingSlot => slots[0]; //Each slot can only hold 1 item each.
        public ItemSlot craftingSlot1 => slots[1];
        public ItemSlot craftingSlot2 => slots[2];
        public ItemSlot craftingSlot3 => slots[3];
        public ItemSlot craftingSlot4 => slots[4];
        public ItemSlot craftingSlot5 => slots[5];

        public WorkbenchInventory(ICoreAPI api, BlockPos? pos = null) : base(6, "ToolsmithWorkbench", pos?.ToString() ?? "-fake", api, OnNewSlot) {
            Pos = pos;
        }

        public ItemSlot? GetSlotFromSelectionID(int slotID) {
            return slotID switch {
                1 => craftingSlot1,
                2 => craftingSlot2,
                3 => craftingSlot3,
                4 => craftingSlot4,
                5 => craftingSlot5,
                7 => reforgeStagingSlot,
                _ => null
            };
        }

        public ItemStack? GetItemFromSlot(int slotID) {
            ItemSlot? slot = GetSlotFromSelectionID(slotID);

            if (slot == null || slot.Empty) { 
                return null;
            }

            return slot.TakeOut(1);
        }

        public bool AddItemToSlot(int slotID, ItemSlot fromSlot) {
            ItemSlot? slot = GetSlotFromSelectionID(slotID);

            if (slot == null || fromSlot.Empty) {
                return false;
            }

            slot.Itemstack = fromSlot.TakeOut(1);
            return true;
        }

        private static ItemSlot OnNewSlot(int id, InventoryGeneric self) {
            return new ItemSlot((WorkbenchInventory)self) {
                MaxSlotStackSize = 1
            };
        }
    }
}
