using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
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

        public bool IsSelectSlotEmpty(int slotID) {
            ItemSlot? slot = GetSlotFromSelectionID(slotID);
            if (slot == null) {
                return false;
            }

            return slot.Empty;
        }

        public bool AllSlotsEmpty() {
            return !slots.Any(x => x.Empty);
        }

        public string GetIDFromSlots() {
            string id = "workbenchinventory-";
            int count = 0;
            foreach (var slot in slots) {
                if (!slot.Empty) {
                    id += slot.Itemstack.Collectible.Code;
                } else {
                    id += "empty";
                }

                count++;
                if (count < slots.Length) {
                    id += "-";
                }
            }

            return id;
        }

        public ItemSlot[] GetFullCraftingSlots() {
            List<ItemSlot> slots = new List<ItemSlot>();
            if (!IsSelectSlotEmpty(1)) {
                slots.Add(GetSlotFromSelectionID(1));
            }
            if (!IsSelectSlotEmpty(2)) {
                slots.Add(GetSlotFromSelectionID(2));
            }
            if (!IsSelectSlotEmpty(3)) {
                slots.Add(GetSlotFromSelectionID(3));
            }
            if (!IsSelectSlotEmpty(4)) {
                slots.Add(GetSlotFromSelectionID(4));
            }
            if (!IsSelectSlotEmpty(5)) {
                slots.Add(GetSlotFromSelectionID(5));
            }

            return slots.ToArray();
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

        public bool AddAdditionalToSlot(int slotID, ItemSlot fromSlot) { //Currently only the Reforging Slot can hold more then one item at a time
            if (slotID == (int)WorkbenchSlots.ReforgeStaging) {
                ItemSlot? slot = GetSlotFromSelectionID(slotID);

                if (slot == null || fromSlot.Itemstack.Collectible.Code != slot.Itemstack.Collectible.Code || slot.MaxSlotStackSize == slot.StackSize) {
                    return false;
                }

                var count = fromSlot.TryPutInto(Api.World, slot, quantity : 1);
                if (count > 0) {
                    return true;
                }
            }

            return false;
        }

        private static ItemSlot OnNewSlot(int id, InventoryGeneric self) {
            if (id == 0) { //Kinda have to hardcode this index, but it's probably fine since it's pretty unlikely I'll be reusing this anywhere?
                return new ItemSlot((WorkbenchInventory)self) {
                    MaxSlotStackSize = 4
                };
            }

            return new ItemSlot((WorkbenchInventory)self) {
                MaxSlotStackSize = 1
            };
        }

        public override void FromTreeAttributes(ITreeAttribute treeAttribute) {
            base.FromTreeAttributes(treeAttribute);
        }

        public override void ToTreeAttributes(ITreeAttribute invtree) {
            base.ToTreeAttributes(invtree);
        }
    }
}
