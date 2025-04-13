using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Config;
using Toolsmith.Utils;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Toolsmith.ToolTinkering {
    public static class TinkeringUtility {

        public static void HandleBrokenTinkeredTool(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int remainingHeadDur, int remainingSharpness, int remainingHandleDur, int remainingBindingDur, bool headBroke, bool refillSlot) {
            ItemStack brokenToolStack = itemslot.Itemstack;
            CollectibleObject toolObject = brokenToolStack.Collectible;
            ItemStack toolHead = null;
            bool gaveHead = false;
            ItemStack toolHandle = null;
            bool gaveHandle = false;
            ItemStack toolBinding = brokenToolStack.GetToolbinding(); //This one is the only different one since it COULD be null representing a lack of a binding, but need to see if it even has a binding first by checking this fact
            bool gaveBinding = false;
            ItemStack bitsDrop = null;

            if (remainingHeadDur > 0 && !brokenToolStack.HasPlaceholderHead()) {
                toolHead = brokenToolStack.GetToolhead();
                toolHead.SetPartCurrentDurability(remainingHeadDur);
                toolHead.SetPartMaxDurability(brokenToolStack.GetToolheadMaxDurability());
                toolHead.SetPartCurrentSharpness(remainingSharpness);
                toolHead.SetPartMaxSharpness(brokenToolStack.GetToolMaxSharpness());
            } else {
                headBroke = true; //This right here might be key for compatability sake. The way I built the whole system runs off the assumption that the Tool's Head determines the tool.
                                  //Thus, it can be considered that a tool does not fully "break" in the vanilla sense until the Head itself breaks, it only "falls apart" ie: the tool head flies off the handle, there's possible durability left on both.
                                  //Because of this, always need to consider the possibility of dropping a handle or binder, but if the 'Head' is broken, we also want to run other mod's 'on damage' calls along with vanilla.
                                  //Anything below that checks for !headBroke is looking to see if the Tool should be "Broken" or simply "Fallen Apart" in this sense, if it's fallen apart, do similar checks to vanilla tool breaking locally here. Otherwise let Vanilla code deal with it, since it's all or nothing after this patch is done.
            }
            if (remainingHandleDur > 0) {
                toolHandle = brokenToolStack.GetToolhandle();
                toolHandle.SetPartCurrentDurability(remainingHandleDur);
                toolHandle.SetPartMaxDurability(brokenToolStack.GetToolhandleMaxDurability());
            }
            if (toolBinding != null) { //Binding doesn't always drop, only if the durability is above the threshold, and then if it's below, it breaks and if made of metal, drops some bits
                BindingStats bindingStats = ToolsmithModSystem.Stats.bindings.Get(ToolsmithModSystem.Config.BindingsWithStats.Get(toolBinding.Collectible.Code.Path).bindingStats);
                float bindingPercentRemains = (float)(remainingBindingDur) / (float)(brokenToolStack.GetToolbindingMaxDurability());
                if (bindingPercentRemains < bindingStats.recoveryPercent) { //If the remaining HP percent is less then the recovery percent, the binding is used up.
                    toolBinding = null; //Set it back to null to prevent dropping anything later! And then to see if Bits should drop!
                }

                if (toolBinding == null && bindingStats.isMetal) {
                    int numBits;
                    if (world.Rand.NextDouble() < 0.5) {
                        numBits = 3;
                    } else {
                        numBits = 4;
                    }
                    bitsDrop = new ItemStack(world.GetItem(new AssetLocation("game:metalbit-" + bindingStats.metalType)), numBits);
                }
            }

            if (ToolsmithModSystem.Config.DebugMessages && world.Api.Side.IsServer()) {
                ToolsmithModSystem.Logger.Debug("Tool broke!");
                ToolsmithModSystem.Logger.Debug("The tool head is " + (toolHead?.Collectible.Code.ToString()));
                ToolsmithModSystem.Logger.Debug("Head has durability: " + remainingHeadDur);
                ToolsmithModSystem.Logger.Debug("Tool Handle is " + (toolHandle?.Collectible.Code.ToString()));
                ToolsmithModSystem.Logger.Debug("Handle has durability: " + remainingHandleDur);
                ToolsmithModSystem.Logger.Debug("Tool Binding is " + (toolBinding?.Collectible.Code.ToString()));
                ToolsmithModSystem.Logger.Debug("Binding has durability: " + remainingBindingDur);
            }

            EntityPlayer player = byEntity as EntityPlayer;
            if (player != null) {
                //Try to give the player each part, if given successfully, set the stack to null again to represent this
                if (!headBroke && toolHead != null) {
                    gaveHead = player.TryGiveItemStack(toolHead);
                }
                if (toolHandle != null) {
                    gaveHandle = player.TryGiveItemStack(toolHandle);
                }
                if (toolBinding != null) {
                    gaveBinding = player.TryGiveItemStack(toolBinding);
                } else if (bitsDrop != null && player.TryGiveItemStack(bitsDrop)) {
                    bitsDrop = null;
                }

                if (!headBroke) { //Move this inside the loop and AFTER giving the itemstack to prevent it from ending up in the same slot that the tool was in. This prevents things like the Treecutting code from falsely assuming the axe is not broke when it actually is.
                    itemslot.Itemstack = null; //Actually 'break' the original item, but only if the head part isn't broken yet. Handle the 'falling apart' of the tools here, but let the 'breaking' happen elsewhere if the head DID fully break.

                    if (refillSlot && toolObject.Tool.HasValue) { //Attempt to refill the slot with a same tool only after the slot is emptied, otherwise it won't succeed.
                        string ident = toolObject.Attributes?["slotRefillIdentifier"].ToString();
                        toolObject.RefillSlotIfEmpty(itemslot, byEntity as EntityAgent, (ItemStack stack) => (ident == null) ? (stack.Collectible.Tool == toolObject.Tool) : (stack.ItemAttributes?["slotRefillIdentifier"]?.ToString() == ident));
                    }

                    if (world.Side.IsServer()) {
                        world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), player);
                    }
                } else {
                    itemslot.Itemstack.Attributes.SetInt(ToolsmithAttributes.Durability, 1); //Set it to 1 just in case setting it to 0 gets the game to just delete it from existance. This also works for checks similar to Smithing Plus, which checks if 'remaining dur' is greater then the damage it will take.
                                                                                             //But this is a failsafe if another mod does not use the Collectable.GetRemainingDurability call and instead just directly reads the Attributes. Smithing Plus... :P
                }
            } else {
                if (!headBroke) { //This needs to be in here as well since no matter what, this needs to run
                    itemslot.Itemstack = null; //Actually 'break' the original item, but only if the head part isn't broken yet. Handle the 'falling apart' of the tools here, but let the 'breaking' happen elsewhere if the head DID fully break.
                    if (world.Side.IsServer()) {
                        world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), byEntity.SidedPos.X, byEntity.SidedPos.Y, byEntity.SidedPos.Z, null, 1f, 16f);
                    }
                } else {
                    itemslot.Itemstack.Attributes.SetInt(ToolsmithAttributes.Durability, 1); //Set it to 1 just in case setting it to 0 gets the game to just delete it from existance. This also works for checks similar to Smithing Plus, which checks if 'remaining dur' is greater then the damage it will take.
                                                                                             //But this is a failsafe if another mod does not use the Collectable.GetRemainingDurability call and instead just directly reads the Attributes. Smithing Plus... :P
                }
            }

            //Drop any remaining tools not given to the player into the world
            if (!headBroke && toolHead != null && !gaveHead) {
                world.SpawnItemEntity(toolHead, byEntity.Pos.XYZ);
            }
            if (toolHandle != null && !gaveHandle) {
                world.SpawnItemEntity(toolHandle, byEntity.Pos.XYZ);
            }
            if (toolBinding != null && !gaveBinding) {
                world.SpawnItemEntity(toolBinding, byEntity.Pos.XYZ);
            } else if (bitsDrop != null) {
                world.SpawnItemEntity(bitsDrop, byEntity.Pos.XYZ);
            }
        }
    }
}
