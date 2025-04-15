using Cairo;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Config;
using Toolsmith.ToolTinkering.Drawbacks;
using Toolsmith.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Toolsmith.ToolTinkering {

    [HarmonyPatch(typeof (CollectibleObject))]
    [HarmonyPatchCategory(ToolsmithModSystem.ToolTinkeringPatchCategory)]
    public class ToolTinkeringPatches {

        //This Prefix Patch is for Smithed Tools, the ones that are simply smithed on an anvil and then you get the finished item. Mostly for just checking for 'Blunt' Tools.
        //Since it is probably impossible to tell what called to damage the tool, through an attack or just using the tool, it might just be simpler to render blunt tools undamagable.
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.DamageItem)), HarmonyPriority(Priority.High)]
        private static bool SmithedToolsDamageItemPrefix(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount, CollectibleObject __instance) {
            if (itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>() && itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolBlunt>() && !itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>() && world.Api.Side.IsServer()) {
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
            if (world.Side.IsServer() && !itemslot.Itemstack.GetBrokeWhileSharpeningFlag() && itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>() && (ToolsmithModSystem.IgnoreCodes.Count == 0 || !ToolsmithModSystem.IgnoreCodes.Contains(itemslot.Itemstack.Collectible.Code.ToString()))) { //Important to check if it even is a Tinkered Tool, as well as making sure it isn't on the ignore list.
                ItemStack itemStack = itemslot.Itemstack;
                int remainingHeadDur = itemStack.GetToolheadCurrentDurability(); //Grab all the current durabilities of the parts!
                int remainingHandleDur = itemStack.GetToolhandleCurrentDurability(); //But none should be -1 already, if any are, it means it's likely a Creative-spawned tool, or the mod was added to a world -- ((world.Side.IsClient()) || (
                int remainingBindingDur = itemStack.GetToolbindingCurrentDurability();
                float chanceToDamage = itemStack.GetGripChanceToDamage();
                bool isBluntTool = itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolBlunt>();
                bool headBroke = false;

                //Time for SHARPNESS and WEAR! Lets a go!
                bool doDamageHead = false;
                bool chanceForDoubleHeadDamage = false;
                bool doDamageHandle = false;
                bool doDamageBinding = false;

                //First, the most important question, what is the current sharpness percentage? Then lower the sharpness by amount.
                int currentSharpness = itemStack.GetToolCurrentSharpness();
                int maxSharpness = itemStack.GetToolMaxSharpness();
                float sharpnessPer = itemStack.GetToolSharpnessPercent();

                if (maxSharpness <= 1) { //If the Sharpness Max is 1, likely means something got marked improperly. I don't think it could be 1 otherwise?
                    sharpnessPer = 0f; //Set the percent to one as a placeholder to just avoid infinite sharpness.
                }
                if (currentSharpness > 0) {
                    currentSharpness -= amount;
                } else {
                    chanceForDoubleHeadDamage = true;
                }

                if (!isBluntTool) {
                    itemStack.SetToolCurrentSharpness(currentSharpness);
                }

                //Then based on the percentage, which parts do we actually damage?
                //Above 0.98, nothing on the tool takes durability damage as a little bonus
                if (sharpnessPer >= 0.98f) { //Since even if the tool isn't going to regularly take damage to all parts, we still want to damage the binding by 1 point just cause it's been 'used' once. It won't get damaged again anyway.
                    if (remainingBindingDur == itemStack.GetToolbindingMaxDurability()) {
                        remainingBindingDur -= 1;
                    }
                } else if (sharpnessPer >= 0.95f) {
                    doDamageBinding = true;
                } else {
                    doDamageBinding = true;
                    doDamageHandle = true;
                }

                if (sharpnessPer < 0.98f && sharpnessPer >= 0.33) {
                    //Starting at 5% chance-to-damage at 0.98, quartic curve to get the chance-to-damage the head up to 100% at 0.33 sharpness left.
                    doDamageHead = MathUtility.ShouldDamageHeadFromCurveChance(world, sharpnessPer);
                } else if (sharpnessPer < 0.33) {
                    doDamageHead = true;
                }

                //Handle damaging each part, the handle only if it should based on the chance to damage it
                if (doDamageHead && (!isBluntTool || world.Rand.NextDouble() <= ToolsmithModSystem.Config.BluntWear)) { //If this Tinkered Tool is also marked as a blunted tool, then apply the much much smaller chance to damage it. Damage the other parts though!
                    remainingHeadDur -= amount;
                    if (chanceForDoubleHeadDamage && world.Rand.NextDouble() <= 0.5) {
                        remainingHeadDur -= amount;
                    }
                }
                itemStack.SetToolheadCurrentDurability(remainingHeadDur);

                if (doDamageHandle && chanceToDamage < 1.0f) {
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
                } else if (doDamageHandle) {
                    remainingHandleDur -= amount;
                }
                itemStack.SetToolhandleCurrentDurability(remainingHandleDur);

                if (doDamageBinding) {
                    remainingBindingDur -= amount;
                }
                itemStack.SetToolbindingCurrentDurability(remainingBindingDur);

                if (sharpnessPer < 0.8 && remainingHeadDur > 0) {
                    DrawbackUtility.TryChanceForDrawback(world, byEntity, itemslot, sharpnessPer);
                }

                //Check each part and see if the health of any of them is <= 0, thus the tool broke, handle it
                //Any or all parts COULD hit 0 at the same time, technically. I'd love to see it though, but it needs to be possible!
                if (remainingBindingDur <= 0 || remainingHandleDur <= 0 || remainingHeadDur <= 0) {
                    TinkeringUtility.HandleBrokenTinkeredTool(world, byEntity, itemslot, remainingHeadDur, currentSharpness, remainingHandleDur, remainingBindingDur, headBroke, !headBroke);
                }

                itemslot.MarkDirty();
                if (!headBroke) { //If the head did not break, then don't run everything!
                    if (itemslot.Itemstack != null && itemslot.Itemstack.Collectible.Tool.HasValue && (!itemslot.Itemstack.Attributes.HasAttribute(ToolsmithAttributes.Durability) || itemslot.Itemstack.Attributes.GetInt(ToolsmithAttributes.Durability) == 0)) {
                        ItemStack itemstack = itemslot.Itemstack;
                        itemstack.Attributes.SetInt(ToolsmithAttributes.Durability, itemstack.Collectible.GetMaxDurability(itemstack));
                    }
                    return false; //Skip default and others
                }
            } else if (world.Side.IsServer() && !itemslot.Itemstack.GetBrokeWhileSharpeningFlag() && itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>()) { //If it's a smithed tool, only need to deal with the Sharpness, and any extra "head" damage. Head in this case is just the tool as a whole.
                ItemStack itemStack = itemslot.Itemstack;
                bool isBluntTool = itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolBlunt>();
                var currentDur = itemStack.GetSmithedDurability();

                //Time for SHARPNESS and WEAR! Lets a go!
                bool doDamageTool = false;
                bool chanceForDoubleToolDamage = false;
                int currentSharpness = itemStack.GetToolCurrentSharpness();
                int maxSharpness = itemStack.GetToolMaxSharpness();
                float sharpnessPer = itemStack.GetToolSharpnessPercent();

                if (maxSharpness <= 1) { //If the Sharpness Max is 1, likely means something got marked improperly. I don't think it could be 1 otherwise?
                    sharpnessPer = 0f; //Set the percent to one as a placeholder to just avoid infinite sharpness.
                }
                if (currentSharpness > 0) {
                    currentSharpness -= amount;
                } else {
                    chanceForDoubleToolDamage = true;
                }

                if (!isBluntTool) {
                    itemStack.SetToolCurrentSharpness(currentSharpness);
                }

                if (sharpnessPer < 0.98f && sharpnessPer >= 0.33) {
                    //Starting at 5% chance-to-damage at 0.98, quartic curve to get the chance-to-damage the head up to 100% at 0.33 sharpness left.
                    doDamageTool = MathUtility.ShouldDamageHeadFromCurveChance(world, sharpnessPer);
                } else if (sharpnessPer < 0.33) {
                    doDamageTool = true;
                }

                //Handle damaging each part, the handle only if it should based on the chance to damage it
                if (doDamageTool && isBluntTool && world.Rand.NextDouble() >= ToolsmithModSystem.Config.BluntWear) { //If this Tinkered Tool is also marked as a blunted tool, then apply the much much smaller chance to damage it. Damage the other parts though!
                    doDamageTool = false;
                }

                if (chanceForDoubleToolDamage && doDamageTool && world.Rand.NextDouble() <= 0.5) { //The 50/50 chance for double damage roll
                    currentDur -= amount;
                }

                return doDamageTool;
            } else if (!world.Side.IsServer() && !itemslot.Itemstack.GetBrokeWhileSharpeningFlag() && (itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>() || itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>())) {
                return false; //Clientside Catch for hitting this point, wait for the server sync to update everything to hopefully prevent that desync from the client
            }
            //If it's not a tinkered or smithed tool, then let everything else run as well!
            return true;
        }

        //This prefix and postfix hopefully will account for the Item Rarity Mod, maybe others as well.
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.ConsumeCraftingIngredients)), HarmonyPriority(Priority.High)]
        private static void TinkeredToolConsumeCraftingIngredientsPrefix(ItemSlot[] slots, ItemSlot outputSlot, GridRecipe matchingRecipe) {
            if (outputSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                outputSlot.Itemstack.SetMaxDurBypassFlag(); //For at least Item Rarity, it hooks into this patch to do the rarity applying on a craft. Set the flag here to hopefully prevent it from hitting the MaxDurability patch below.
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(CollectibleObject.ConsumeCraftingIngredients)), HarmonyPriority(-100)]
        private static void TinkeredToolConsumeCraftingIngredientsPostfix(ItemSlot[] slots, ItemSlot outputSlot, GridRecipe matchingRecipe) {
            if (outputSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                outputSlot.Itemstack.ClearMaxDurBypassFlag(); //Make sure to clear the flag afterwards, too. Ideally after Item Rarity is done!
            }
        }


        //Harmony Prefix Patch to intercept the GetMaxDurability calls, if it's a Tinkered Tool, instead it will need to check the different part's durabilities over the default 'durability'
        //Returns whatever part has the lowest max durability, to make rendering the damage bar both make sense, and surprisingly simple to tweak what it's looking at. Plus, this might help with crafting using tools, maybe? Anything that calls GetMax separately.
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.GetMaxDurability))]
        private static bool TinkeredToolGetMaxDurabilityPrefix(ItemStack itemstack, ref int __result) {
            if (itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                if (itemstack.GetMaxDurBypassFlag()) { //If this itemstack has the BypassFlag Attribute, that means it has been flagged as a 'first time call' to Collectable.Durability, but with the intent to account for other mod's changes that might hook in here.
                    return true;
                }

                if (itemstack.HasPlaceholderHead()) { //If this tool has a PlaceholderHead, ie a Candle, that likely means the item is broken.
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
            } else if (itemstack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>()) {
                if (itemstack.GetMaxDurBypassFlag()) { //If this itemstack has the BypassFlag Attribute, that means it has been flagged as a 'first time call' to Collectable.Durability, but with the intent to account for other mod's changes that might hook in here.
                    return true;
                }

                __result = (int)(itemstack.Collectible.Durability * ToolsmithModSystem.Config.HeadDurabilityMult);
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.GetDurability))] //This method is technically depricated for GetRemainingDurability but patching it just incase as well. Base call returns Max Durability.
        private static bool TinkeredToolGetDurabilityPrefix(ItemStack itemstack, ref int __result) {
            if (itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                if (itemstack.HasPlaceholderHead()) { //If this tool has a PlaceholderHead, ie a Candle, that likely means the item is broken.
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
            } else if (itemstack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>()) {
                if (itemstack.GetMaxDurBypassFlag()) { //If this itemstack has the BypassFlag Attribute, that means it has been flagged as a 'first time call' to Collectable.Durability, but with the intent to account for other mod's changes that might hook in here.
                    return true;
                }

                __result = (int)(itemstack.Collectible.Durability * ToolsmithModSystem.Config.HeadDurabilityMult);
                return false;
            }

            return true;
        }

        //This Prefix Patch is pretty much identical to the Max Durability one! Just, you know, returning the current instead, hah.
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.GetRemainingDurability))]
        private static bool TinkeredToolGetRemainingDurabilityPrefix(ItemStack itemstack, ref int __result) {
            if (itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                if (itemstack.HasPlaceholderHead()) { //If this tool has a PlaceholderHead, ie a Candle, that likely means the item is broken.
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
            var isTinkeredTool = itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>();
            var isSmithedTool = itemstack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>();
            float newMiningSpeed = miningSpeed;

            if (isTinkeredTool || isSmithedTool) {
                var sharpnessPer = ((ItemStack)itemstack).GetToolSharpnessPercent();
                if (sharpnessPer >= 0.9) {
                    newMiningSpeed += newMiningSpeed * ToolsmithConstants.HighSharpnessSpeedBonusMult;
                } else if (sharpnessPer <= 0.33) {
                    newMiningSpeed += newMiningSpeed * ToolsmithConstants.LowSharpnessSpeedMalusMult;
                }
            }
            
            if (isTinkeredTool) {
                var speedBonus = ((ItemStack)itemstack).GetSpeedBonus();
                newMiningSpeed += newMiningSpeed * speedBonus;
            }

            return newMiningSpeed;
        }

        //Patching the GuiElementItemSlotGridBase now! Anything patching CollectibleObject is above!
        [HarmonyPatch(typeof(GuiElementItemSlotGridBase))]
        [HarmonyTranspiler]
        [HarmonyPatch("ComposeSlotOverlays")]
        private static IEnumerable<CodeInstruction> ComposeSlotOverlaysTranspiler(IEnumerable<CodeInstruction> instructions) { //WOW Transpilers are FUN. And actually I do understand them better now having written this.

            int retCount = 0;
            int shadePathCount = 0;
            int index = -1;
            int indexOfThirdRet = -1;

            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++) {
                if (retCount < 3 && codes[i].opcode == OpCodes.Ret) { //Don't need to look at anything until after we have found three 'ret' calls
                    retCount++;
                    if (retCount == 3 && codes[i - 1].opcode == OpCodes.Ldc_I4_1) {
                        indexOfThirdRet = i;
                    }
                    continue;
                }

                if (retCount == 3 && shadePathCount < 2 && codes[i].opcode == OpCodes.Call) {
                    if (codes[i - 1].opcode == OpCodes.Ldc_R8 && (double)codes[i - 1].operand == (double)(2)) { //If a Call code is preceeded by a float 2 being loaded, it is likely the ShadePath call we are looking for.
                        if (codes[i - 2].opcode == OpCodes.Ldloc_2) { //Then just in case, lets see if before THAT was loading the textCtx on the stack. THEN we are certain. (probably? Hopefully.)
                            shadePathCount++;
                            continue;
                        }
                    }
                }

                if (shadePathCount == 2) {
                    index = i;
                    break;
                }
            }

            var codeAddition = new List<CodeInstruction> {
                CodeInstruction.LoadArgument(1),
                CodeInstruction.LoadArgument(2),
                CodeInstruction.LoadArgument(3),
                CodeInstruction.LoadLocal(2),
                CodeInstruction.LoadArgument(0),
                CodeInstruction.Call(typeof(ToolTinkeringPatches), "DrawSharpnessBar", new Type[5] { typeof(ItemSlot), typeof(int), typeof(int), typeof(Context), typeof(GuiElementItemSlotGridBase) })
            };

            if (index >= 0 && indexOfThirdRet >= 0) {
                codeAddition[0].MoveLabelsFrom(codes[index]);
                codes[indexOfThirdRet].opcode = OpCodes.Nop;
                codes[indexOfThirdRet - 1].opcode = OpCodes.Nop;
                codes.InsertRange(index, codeAddition);
            } else {
                if (index < 0) {
                    ToolsmithModSystem.Logger.Error("Sharpness Bar Transpiler had an error! Could not find the second call to ShadePath for the Damage Bar rendering. The Sharpness Bar will not function!");
                }
                if (indexOfThirdRet < 0) {
                    ToolsmithModSystem.Logger.Error("Sharpness Bar Transpiler had an error! Could not locate the third return call. The Sharpness Bar will not function!");
                }
            }
            
            return codes.AsEnumerable();
        }

        //This is basically the vanilla way of handling the Durability bar, but instead I tweaked it to be a little above, and also look at the Sharpness instead of Durability values. ShouldRenderSharpness checks if it's even a tool with sharpness, so this shouldn't run on anything that doesn't actually have it.
        private static void DrawSharpnessBar(ItemSlot slot, int slotId, int slotIndex, Context textCtx, GuiElementItemSlotGridBase instance) {
            if (TinkeringUtility.ShouldRenderSharpnessBar(slot.Itemstack)) {
                double x = ElementBounds.scaled(4);
                double y = (int)instance.SlotBounds[slotIndex].InnerHeight - ElementBounds.scaled(8) - ElementBounds.scaled(4);
                textCtx.SetSourceRGBA(GuiStyle.DialogStrongBgColor);
                double width = (instance.SlotBounds[slotIndex].InnerWidth - ElementBounds.scaled(8));
                double height = ElementBounds.scaled(4);
                GuiElement.RoundRectangle(textCtx, x, y, width, height, 1);
                textCtx.FillPreserve();
                instance.ShadePath(textCtx, 2);


                float[] color = ColorUtil.ToRGBAFloats(TinkeringUtility.GetItemSharpnessColor(slot.Itemstack));
                textCtx.SetSourceRGB(color[0], color[1], color[2]);

                int maxSharp = slot.Itemstack.GetToolMaxSharpness();
                float remainingSharpness = (float)slot.Itemstack.GetToolCurrentSharpness() / maxSharp;

                width = remainingSharpness * (instance.SlotBounds[slotIndex].InnerWidth - ElementBounds.scaled(8));

                GuiElement.RoundRectangle(textCtx, x, y, width, height, 1);
                textCtx.FillPreserve();
                instance.ShadePath(textCtx, 2);
            }
        }
    }
}