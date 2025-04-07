using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Config;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Toolsmith.ToolTinkering {

    [HarmonyPatch(typeof (CollectibleObject))]
    [HarmonyPatchCategory(ToolsmithModSystem.ToolTinkeringPatchCategory)]
    public class ToolTinkeringPatches {

        //This Prefix Patch is for Smithed Tools, the ones that are simply smithed on an anvil and then you get the finished item. Mostly for just checking for 'Blunt' Tools.
        //Since it is probably impossible to tell what called to damage the tool, through an attack or just using the tool, it might just be simpler to render blunt tools undamagable.
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.DamageItem)), HarmonyPriority(Priority.High)]
        private static bool SmithedToolsDamageItemPrefix(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount, CollectibleObject __instance) {
            if (itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorSmithedTool>() && itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolNoDamageOnUse>() && !itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>() && world.Api.Side.IsServer()) {
                //If the tools is a Smithed Tool but not a Tinkered Tool with three parts, then simply don't damage it and interrupt the rest of the calls.
                itemslot.MarkDirty();
                return false; //Skip default and others
            } else { //If it's not a Smithed Tool, don't touch anything.
                return true; //Run default and others
            }
        }

        //This Prefix Patch is entirely to hook into the DamageItem calls and see if the item in question is a Tinkered Tool, and if it is, manage the Damage to the 3 tool parts instead of the base item durability
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.DamageItem)), HarmonyPriority(Priority.High)]
        private static bool TinkeredToolDamageItemPrefix(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount, CollectibleObject __instance) { //The Itemslot in question has to finish this call Null if the item is broken, other parts of the game, IE Treecutting code, only check for if the slot is null to keep on cutting the tree. This works for vanilla, cause when a tool breaks, it WILL be gone.
            if (itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>() && ((world.Side.IsClient()) || (world.Side.IsServer() && (ToolsmithModSystem.IgnoreCodes.Count == 0 || !ToolsmithModSystem.IgnoreCodes.Contains(itemslot.Itemstack.Collectible.Code.ToString()))))) { //Important to check if it even is a Tinkered Tool, as well as making sure it isn't on the ignore list.
                ItemStack itemStack = itemslot.Itemstack;
                int remainingHeadDur = itemStack.GetToolheadCurrentDurability(); //Grab all the current durabilities of the parts!
                int remainingHandleDur = itemStack.GetToolhandleCurrentDurability(); //But none should be -1 already, if any are, it means it's likely a Creative-spawned tool, or the mod was added to a world
                int remainingBindingDur = itemStack.GetToolbindingCurrentDurability();
                float chanceToDamage = itemStack.GetGripChanceToDamage();
                bool headBroke = false;

                //Handle damaging each part, the handle only if it should based on the chance to damage it
                if (!itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolNoDamageOnUse>()) { //If this Tinkered Tool is also marked as a tool to not damage, then simply don't damage the head. Damage the other parts though!
                    remainingHeadDur -= amount;
                }
                itemStack.SetToolheadCurrentDurability(remainingHeadDur);

                if (chanceToDamage < 1.0f) {
                    var damageToTake = 0;
                    if (world.Rand.NextDouble() <= chanceToDamage) {
                        damageToTake++;
                    }

                    if (amount > 1) { //Only run this if it's actually needed, to actually 'roll' for damage each point of damage. Is there a better way to do this?
                        var count = 1;
                        while (count < amount) {
                            if (world.Rand.NextDouble() <= chanceToDamage) { //For each point of damage, roll for change to damage
                                damageToTake++;
                            }
                            count++;
                        }
                    }
                    remainingHandleDur -= damageToTake;
                } else {
                    remainingHandleDur -= amount;
                }
                itemStack.SetToolhandleCurrentDurability(remainingHandleDur);

                remainingBindingDur -= amount;
                itemStack.SetToolbindingCurrentDurability(remainingBindingDur);

                //Check each part and see if the health of any of them is <= 0, thus the tool broke, handle it
                //Any or all parts COULD hit 0 at the same time, technically. I'd love to see it though, but it needs to be possible!
                if (remainingBindingDur <= 0 || remainingHandleDur <= 0 || remainingHeadDur <= 0) {
                    ItemStack toolHead = null;
                    bool gaveHead = false;
                    ItemStack toolHandle = null;
                    bool gaveHandle = false;
                    ItemStack toolBinding = itemStack.GetToolbinding(); //This one is the only different one since it COULD be null representing a lack of a binding, but need to see if it even has a binding first by checking this fact
                    bool gaveBinding = false;
                    ItemStack bitsDrop = null;

                    if (remainingHeadDur > 0 && !itemStack.HasPlaceholderHead()) {
                        toolHead = itemStack.GetToolhead();
                        toolHead.SetCurrentPartDurability(remainingHeadDur);
                        toolHead.SetMaxPartDurability(itemStack.GetToolheadMaxDurability());
                    } else {
                        headBroke = true; //This right here might be key for compatability sake. The way I built the whole system runs off the assumption that the Tool's Head determines the tool.
                                          //Thus, it can be considered that a tool does not fully "break" in the vanilla sense until the Head itself breaks, it only "falls apart" ie: the tool head flies off the handle, there's possible durability left on both.
                                          //Because of this, always need to consider the possibility of dropping a handle or binder, but if the 'Head' is broken, we also want to run other mod's 'on damage' calls along with vanilla.
                                          //Anything below that checks for !headBroke is looking to see if the Tool should be "Broken" or simply "Fallen Apart" in this sense, if it's fallen apart, do similar checks to vanilla tool breaking locally here. Otherwise let Vanilla code deal with it, since it's all or nothing after this patch is done.
                    }
                    if (remainingHandleDur > 0) {
                        toolHandle = itemStack.GetToolhandle();
                        toolHandle.SetCurrentPartDurability(remainingHandleDur);
                        toolHandle.SetMaxPartDurability(itemStack.GetToolhandleMaxDurability());
                    }
                    if (toolBinding != null) { //Binding doesn't always drop, only if the durability is above the threshold, and then if it's below, it breaks and if made of metal, drops some bits
                        BindingStats bindingStats = ToolsmithModSystem.Stats.bindings.Get(ToolsmithModSystem.Config.BindingsWithStats.Get(toolBinding.Collectible.Code.Path).bindingStats);
                        float bindingPercentRemains = (float)(remainingBindingDur) / (float)(itemStack.GetToolbindingMaxDurability());
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

                    IPlayer player = (byEntity as EntityPlayer)?.Player;
                    if (player != null) {
                        //Try to give the player each part, if given successfully, set the stack to null again to represent this
                        if (!headBroke && toolHead != null) {
                            gaveHead = player.InventoryManager.TryGiveItemstack(toolHead, slotNotifyEffect: true);
                        }
                        if (toolHandle != null) {
                            gaveHandle = player.InventoryManager.TryGiveItemstack(toolHandle, slotNotifyEffect: true);
                        }
                        if (toolBinding != null) {
                            gaveBinding = player.InventoryManager.TryGiveItemstack(toolBinding, slotNotifyEffect: true);
                        } else if (bitsDrop != null && player.InventoryManager.TryGiveItemstack(bitsDrop, slotNotifyEffect: true)) {
                            bitsDrop = null;
                        }

                        if (!headBroke) { //Move this inside the loop and AFTER giving the itemstack to prevent it from ending up in the same slot that the tool was in. This prevents things like the Treecutting code from falsely assuming the axe is not broke when it actually is.
                            itemslot.Itemstack = null; //Actually 'break' the original item, but only if the head part isn't broken yet. Handle the 'falling apart' of the tools here, but let the 'breaking' happen elsewhere if the head DID fully break.
                            
                            if (__instance.Tool.HasValue) { //Attempt to refill the slot with a same tool only after the slot is emptied, otherwise it won't succeed.
                                string ident = __instance.Attributes?["slotRefillIdentifier"].ToString();
                                __instance.RefillSlotIfEmpty(itemslot, byEntity as EntityAgent, (ItemStack stack) => (ident == null) ? (stack.Collectible.Tool == __instance.Tool) : (stack.ItemAttributes?["slotRefillIdentifier"]?.ToString() == ident));
                            }

                            if (world.Side.IsServer()) {
                                world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), player);
                            }
                        } else {
                            itemslot.Itemstack.Attributes.SetInt("durability", 1); //Set it to 1 just in case setting it to 0 gets the game to just delete it from existance. This also works for checks similar to Smithing Plus, which checks if 'remaining dur' is greater then the damage it will take.
                                                                                   //But this is a failsafe if another mod does not use the Collectable.GetRemainingDurability call and instead just directly reads the Attributes. Smithing Plus... :P
                        }
                    } else {
                        if (!headBroke) { //This needs to be in here as well since no matter what, this needs to run
                            itemslot.Itemstack = null; //Actually 'break' the original item, but only if the head part isn't broken yet. Handle the 'falling apart' of the tools here, but let the 'breaking' happen elsewhere if the head DID fully break.
                            if (world.Side.IsServer()) {
                                world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), byEntity.SidedPos.X, byEntity.SidedPos.Y, byEntity.SidedPos.Z, null, 1f, 16f);
                            }
                        } else {
                            itemslot.Itemstack.Attributes.SetInt("durability", 1); //Set it to 1 just in case setting it to 0 gets the game to just delete it from existance. This also works for checks similar to Smithing Plus, which checks if 'remaining dur' is greater then the damage it will take.
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

                itemslot.MarkDirty();
                if (!headBroke) {
                    if (itemslot.Itemstack != null && itemslot.Itemstack.Collectible.Tool.HasValue && (!itemslot.Itemstack.Attributes.HasAttribute("durability") || itemslot.Itemstack.Attributes.GetInt("durability") == 0)) {
                        ItemStack itemstack = itemslot.Itemstack;
                        itemstack.Attributes.SetInt("durability", itemstack.Collectible.GetMaxDurability(itemstack));
                    }
                    return false; //Skip default and others
                }
            }
            //If it's not a tinkered tool, or the head did break, then let everything else run as well!
            return true;
        }

        //Harmony Prefix Patch to intercept the GetMaxDurability calls, if it's a Tinkered Tool, instead it will need to check the different part's durabilities over the default 'durability'
        //Returns whatever part has the lowest max durability, to make rendering the damage bar both make sense, and surprisingly simple to tweak what it's looking at. Plus, this might help with crafting using tools, maybe? Anything that calls GetMax separately.
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.GetMaxDurability))]
        private static bool TinkeredToolGetMaxDurabilityPrefix(ItemStack itemstack, ref int __result) {
            if (itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                if (ToolsmithModSystem.Api.Side.IsServer() && itemstack.HasPlaceholderHead()) { //If this tool has a PlaceholderHead, ie a Candle, that likely means the item is broken.
                    return true; //Default to vanilla behavior here.
                } //But if it's the Clientside, it COULD have a candle because it hasn't recieved an update packet yet.

                var headMax = itemstack.GetToolheadMaxDurability();
                var handleMax = itemstack.GetToolhandleMaxDurability();
                var bindingMax = itemstack.GetToolbindingMaxDurability();
                int lowestMax;

                if (bindingMax < handleMax) {
                    lowestMax = bindingMax;
                } else {
                    lowestMax = handleMax;
                }
                if (lowestMax < headMax) { //Find the lowest and set it as the result, then block any subsequent calls
                    __result = lowestMax;
                } else {
                    __result = headMax;
                }

                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.GetDurability))] //This method is technically depricated for GetRemainingDurability but patching it just incase as well. Base call returns Max Durability.
        private static bool TinkeredToolGetDurabilityPrefix(ItemStack itemstack, ref int __result) {
            if (itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                if (ToolsmithModSystem.Api.Side.IsServer() && itemstack.HasPlaceholderHead()) { //If this tool has a PlaceholderHead, ie a Candle, that likely means the item is broken.
                    return true; //Default to vanilla behavior here.
                } //But if it's the Clientside, it COULD have a candle because it hasn't recieved an update packet yet.

                var headMax = itemstack.GetToolheadMaxDurability();
                var handleMax = itemstack.GetToolhandleMaxDurability();
                var bindingMax = itemstack.GetToolbindingMaxDurability();
                int lowestMax;

                if (bindingMax < handleMax) {
                    lowestMax = bindingMax;
                } else {
                    lowestMax = handleMax;
                }
                if (lowestMax < headMax) { //Find the lowest and set it as the result, then block any subsequent calls
                    __result = lowestMax;
                } else {
                    __result = headMax;
                }

                return false;
            }

            return true;
        }

        //This Prefix Patch is pretty much identical to the Max Durability one! Just, you know, returning the current instead, hah.
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.GetRemainingDurability))]
        private static bool TinkeredToolGetRemainingDurabilityPrefix(ItemStack itemstack, ref int __result) {
            if (itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                if (ToolsmithModSystem.Api.Side.IsServer() && itemstack.HasPlaceholderHead()) { //If this tool has a PlaceholderHead, ie a Candle, that likely means the item is broken.
                    return true; //Default to vanilla behavior here.
                } //But if it's the Clientside, it COULD have a candle because it hasn't recieved an update packet yet.

                var headCur = itemstack.GetToolheadCurrentDurability();
                var handleCur = itemstack.GetToolhandleCurrentDurability();
                var bindingCur = itemstack.GetToolbindingCurrentDurability();
                int lowestCur;

                if (bindingCur < handleCur) {
                    lowestCur = bindingCur;
                } else {
                    lowestCur = handleCur;
                }
                if (lowestCur < headCur) {
                    __result = lowestCur;
                } else {
                    __result = headCur;
                }

                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.SetDurability))]
        private static bool TinkeredToolSetDurabilityPrefix(ItemStack itemstack, int amount) {
            if (itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) { //Since most other mods will expect the vanilla durability, best to just have this repair the Head durability only.
                itemstack.SetToolheadCurrentDurability(amount);
                return false;
            }
            return true;
        }

        //The Postfix Patch that handles the Mining Speed (ms) Boost from any Tinkered Tools, simply just takes the output of the original call and if it's a Tinkered Tool? Add ms + ms*bonus
        [HarmonyPostfix]
        [HarmonyPatch(nameof(CollectibleObject.GetMiningSpeed))]
        private static float TinkeredToolMiningSpeedPostfix(float miningSpeed, IItemStack itemstack, BlockSelection blockSel, Block block, IPlayer forPlayer) {
            if (itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                var speedBonus = ((ItemStack)itemstack).GetSpeedBonus();
                return (miningSpeed + (miningSpeed * speedBonus));
            }

            return miningSpeed;
        }
    }
}