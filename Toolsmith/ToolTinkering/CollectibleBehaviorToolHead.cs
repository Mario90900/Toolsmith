using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.Server;

namespace Toolsmith.ToolTinkering {
    public class CollectibleBehaviorToolHead : CollectibleBehaviorToolPartWithHealth {

        private bool crafting = false;
        private bool sharpening = false;
        protected float deltaLastTick = 0;
        protected float lastInterval = 0;

        public CollectibleBehaviorToolHead(CollectibleObject collObj) : base(collObj) {

        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling) { //Handle the grinding code here as well as the tool itself! Probably can offload the core interaction to a helper utility function?
            if (ValidHandleInOffhand(byEntity)) { //Check for Handle in Offhand
                handHandling = EnumHandHandling.PreventDefault;
                handling = EnumHandling.PreventSubsequent;
                if (byEntity.World.Side == EnumAppSide.Server) {
                    byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/messycraft.ogg"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z, null, true, 32f, 1f);
                }
                crafting = true;
                return;
            } else if (TinkeringUtility.WhetstoneInOffhand(byEntity) != null && TinkeringUtility.ToolOrHeadNeedsSharpening(slot.Itemstack, byEntity.World)) {
                handHandling = EnumHandHandling.PreventDefault;
                handling = EnumHandling.PreventSubsequent;
                sharpening = true;
                return;
            }

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling) {
            if (crafting) {
                handling = EnumHandling.PreventSubsequent;
                return (crafting && secondsUsed < 4.5f); //Crafting a toolhead into a tool takes around 4.5s
            } else if (sharpening) {
                handling = EnumHandling.PreventSubsequent;
                return TinkeringUtility.TryWhetstoneSharpening(ref deltaLastTick, ref lastInterval, secondsUsed, slot, byEntity, ref handling);
            }

            return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling) {
            if (crafting && secondsUsed >= 4.4f) { //If they were crafting, verify that the countdown is up, and if so, craft it (if there still is a valid offhand handle!)
                handling = EnumHandling.PreventDefault;
                if (byEntity.World.Side.IsServer() && ValidHandleInOffhand(byEntity)) {
                    CraftTool(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
                }
                crafting = false;
                return;
            } else if (sharpening) {
                handling = EnumHandling.PreventDefault;
                deltaLastTick = 0;
                lastInterval = 0;
                var whetstone = TinkeringUtility.WhetstoneInOffhand(byEntity);
                if (whetstone != null) {
                    whetstone.DoneSharpening();
                }
                sharpening = false;
            }

            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }

        private void CraftTool(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling) {
            CollectibleObject craftedTool;
            var success = RecipeRegisterModSystem.TinkerToolGridRecipes.TryGetValue(slot.Itemstack.Collectible.Code.ToString(), out craftedTool);
            if (success) {
                IPlayer player = ((EntityPlayer)byEntity).Player; //I THINK only a player can ever craft something? I dunno. This aint modded Minecraft though so maybe no fake players to account for woo.
                ItemSlot bindingSlot = SearchForPossibleBindings(player); //Does the Player have a valid Binding in their hotbar? Note, this bindingSlot can be null! That means no binding found and not used.
                if (bindingSlot == null) { //If so, grab it and the handle...
                    bindingSlot = new DummySlot();
                }
                ItemSlot handleSlot = byEntity.LeftHandItemSlot;

                ItemStack craftedItemStack = new ItemStack(byEntity.World.GetItem(craftedTool.Code), 1); //Create the tool in question
                ItemSlot placeholderOutput = new ItemSlot(new DummyInventory(ToolsmithModSystem.Api));
                placeholderOutput.Itemstack = craftedItemStack;

                GridRecipe DummyRecipe = new() {
                    AverageDurability = false,
                    Output = new() {
                        ResolvedItemstack = craftedItemStack
                    }
                };
                ItemSlot[] inputSlots;
                if (bindingSlot.GetType() != typeof(DummySlot)) {
                    inputSlots = new ItemSlot[] { slot, handleSlot, bindingSlot };
                } else {
                    inputSlots = new ItemSlot[] { slot, handleSlot };
                }

                craftedItemStack.Collectible.ConsumeCraftingIngredients(inputSlots, placeholderOutput, DummyRecipe);
                craftedItemStack.Collectible.OnCreatedByCrafting(inputSlots, placeholderOutput, DummyRecipe); //Hopefully call this just like it would if properly crafted in the grid!

                handleSlot.TakeOut(1); //Decrement inputs, and place the finished item in the ToolHead's Slot
                handleSlot.MarkDirty();
                if (bindingSlot.GetType() != typeof(DummySlot)) {
                    bindingSlot.TakeOut(1);
                    bindingSlot.MarkDirty();
                }
                ItemStack tempHolder = slot.Itemstack; //I don't believe any Toolhead will actually stack more then once -- Actually they do. Huh. I never tried before. Good thing I had this!
                slot.Itemstack = craftedItemStack; //Above holds the possible multiple-stacked Toolheads, this finally gives the crafted tool to slot that previously had the head(s)
                slot.MarkDirty();
                if (tempHolder.StackSize > 1) {
                    tempHolder.StackSize -= 1;
                    if (!byEntity.TryGiveItemStack(tempHolder)) { //This should hopefully return any remainder!
                        byEntity.World.SpawnItemEntity(tempHolder, byEntity.Pos.XYZ);
                    }
                }
            }
        }

        private bool ValidHandleInOffhand(EntityAgent byEntity) {
            var offhandItem = byEntity.LeftHandItemSlot?.Itemstack?.Collectible;
            if (offhandItem == null) {
                return false;
            }
            bool result = offhandItem.HasBehavior<CollectibleBehaviorToolHandle>();
            return result;
        }

        private ItemSlot SearchForPossibleBindings(IPlayer player) { //Searches only the Hotbar just for efficiency sake! Also kinda ease of use that you don't have to dump EVERYTHING on the ground that might be a binding. Just store it in bags.
            IInventory hotbar = player.InventoryManager.GetHotbarInventory();
            foreach (var slot in hotbar.Where(s => s.Itemstack != null)) {
                if (slot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolBinding>()) {
                    return slot;
                }
            }
            return null;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            dsc.AppendLine(Lang.Get("toolheaddirections"));
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }

        public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, ref EnumHandling bhHandling) { //TODO - This isn't called when a tool head is smithed. Buh! Have to move it elsewhere, or otherwise get this called... Where does XSkills patch in the Quality for Smithed tools?
            //I do hope this also gets called when Smithing/Knapping completes. I think it should?

            bool isToolMetal = outputSlot.Itemstack.Collectible.IsCraftableMetal();
            int baseDur = 1000; //Since we don't know the actual base durability YET for the tool, until it is crafted. So this is a placeholder.
            int partDur = (int)(baseDur * ToolsmithModSystem.Config.HeadDurabilityMult);
            int sharpness = (int)(baseDur * ToolsmithModSystem.Config.SharpnessMult);
            int startingSharpness;
            if (isToolMetal) {
                startingSharpness = (int)(sharpness * ToolsmithConstants.StartingSharpnessMult);
            } else {
                startingSharpness = (int)(sharpness * ToolsmithConstants.NonMetalStartingSharpnessMult);
            }

            outputSlot.Itemstack.SetPartCurrentDurability(partDur);
            outputSlot.Itemstack.SetPartMaxDurability(partDur);
            outputSlot.Itemstack.SetToolCurrentSharpness(startingSharpness);
            outputSlot.Itemstack.SetToolMaxSharpness(sharpness);

            if (ToolsmithModSystem.Config.DebugMessages) {
                ToolsmithModSystem.Logger.Debug("The starting Sharpness is: " + startingSharpness);
                ToolsmithModSystem.Logger.Debug("Finally the max Sharpness is: " + sharpness);
            }
        }
    }
}
