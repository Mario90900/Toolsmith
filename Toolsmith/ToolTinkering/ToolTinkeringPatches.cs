using Cairo;
using HarmonyLib;
using SmithingPlus.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Client.Behaviors;
using Toolsmith.Config;
using Toolsmith.ToolTinkering.Behaviors;
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

    [HarmonyPatch(typeof(CollectibleObject))]
    [HarmonyPatchCategory(ToolsmithModSystem.ToolTinkeringDamagePatchCategory)]
    public class ToolTinkeringPatches {

        //This Prefix Patch is for Smithed Tools, the ones that are simply smithed on an anvil and then you get the finished item. Mostly for just checking for 'Blunt' Tools.
        //Since it is probably impossible to tell what called to damage the tool, through an attack or just using the tool, it might just be simpler to render blunt tools undamagable.
        /*[HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.DamageItem)), HarmonyPriority(Priority.High)]
        private static bool SmithedToolsDamageItemPrefix(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount, CollectibleObject __instance) {
            if (itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>() && itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolBlunt>() && !itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>() && world.Api.Side.IsServer()) {
                //If the tools is a Smithed Tool but not a Tinkered Tool with three parts, then simply don't damage it and interrupt the rest of the calls.
                itemslot.MarkDirty();
                return false; //Skip default and others
            } else { //If it's not a Smithed Tool, don't touch anything.
                return true; //Run default and others
            }
        }*/

        //This Prefix Patch is entirely to hook into the DamageItem calls and see if the item in question is a Tinkered Tool, and if it is, manage the Damage to the 3 tool parts instead of the base item durability
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.DamageItem)), HarmonyPriority(Priority.High)]
        private static bool TinkeredToolDamageItemPrefix(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount, CollectibleObject __instance) { //The Itemslot in question has to finish this call Null if the item is broken, other parts of the game, IE Treecutting code, only check for if the slot is null to keep on cutting the tree. This works for vanilla, cause when a tool breaks, it WILL be gone.
            if (!itemslot.Itemstack.GetBrokeWhileSharpeningFlag() && itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>() && (!world.Side.IsServer() || (ToolsmithModSystem.IgnoreCodes.Count == 0 || !ToolsmithModSystem.IgnoreCodes.Contains(itemslot.Itemstack.Collectible.Code.ToString())))) { //Important to check if it even is a Tinkered Tool, as well as making sure it isn't on the ignore list.
                ItemStack itemStack = itemslot.Itemstack;
                int remainingHeadDur = itemStack.GetToolheadCurrentDurability(); //Grab all the current durabilities of the parts!
                int remainingHandleDur = itemStack.GetToolhandleCurrentDurability(); //But none should be -1 already, if any are, it means it's likely a Creative-spawned tool, or the mod was added to a world -- ((world.Side.IsClient()) || (
                int remainingBindingDur = itemStack.GetToolbindingCurrentDurability();
                float chanceToDamage = itemStack.GetGripChanceToDamage();
                bool isBluntTool = itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolBlunt>();
                bool headBroke = false;

                //Time for SHARPNESS and WEAR! Lets a go!
                bool doDamageHead = false;
                bool doubleHeadDamage = false;
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
                    doubleHeadDamage = true;
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

                if (sharpnessPer < 0.98f) {
                    doDamageHead = true;
                }

                //Handle damaging each part, the handle only if it should based on the chance to damage it
                if (doDamageHead && (doubleHeadDamage || (!isBluntTool && world.Rand.NextDouble() <= ToolsmithModSystem.Config.SharpWear) || world.Rand.NextDouble() <= ToolsmithModSystem.Config.BluntWear)) { //If this Tinkered Tool is also marked as a blunted tool, then apply the much much smaller chance to damage it. Damage the other parts though!
                    remainingHeadDur -= amount;
                    if (doubleHeadDamage) {
                        remainingHeadDur -= amount;
                    }
                }
                itemStack.SetToolheadCurrentDurability(remainingHeadDur);
                if (remainingHeadDur <= 0) {
                    headBroke = true;
                }

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

                if (remainingHeadDur > 0) {
                    DrawbackUtility.TryChanceForDrawback(world, byEntity, itemslot, sharpnessPer);
                }

                //Check each part and see if the health of any of them is <= 0, thus the tool broke, handle it
                //Any or all parts COULD hit 0 at the same time, technically. I'd love to see it though, but it needs to be possible!
                if (remainingBindingDur <= 0 || remainingHandleDur <= 0 || remainingHeadDur <= 0) {
                    TinkeringUtility.HandleBrokenTinkeredTool(world, byEntity, itemslot, remainingHeadDur, currentSharpness, remainingHandleDur, remainingBindingDur, headBroke, !headBroke);
                }

                itemslot.MarkDirty();
                if (!headBroke) { //If the head did not break, then don't run everything!
                    return false; //Skip default and others
                }
            } else if (!itemslot.Itemstack.GetBrokeWhileSharpeningFlag() && itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>()) { //If it's a smithed tool, only need to deal with the Sharpness, and any extra "head" damage. Head in this case is just the tool as a whole.
                ItemStack itemStack = itemslot.Itemstack;
                bool isBluntTool = itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolBlunt>();
                var currentDur = itemStack.GetSmithedDurability();

                //Time for SHARPNESS and WEAR! Lets a go!
                bool doDamageTool = false;
                bool doubleToolDamage = false;
                int currentSharpness = itemStack.GetToolCurrentSharpness();
                int maxSharpness = itemStack.GetToolMaxSharpness();
                float sharpnessPer = itemStack.GetToolSharpnessPercent();

                if (maxSharpness <= 1) { //If the Sharpness Max is 1, likely means something got marked improperly. I don't think it could be 1 otherwise?
                    sharpnessPer = 0f; //Set the percent to one as a placeholder to just avoid infinite sharpness.
                }
                if (currentSharpness > 0) {
                    currentSharpness -= amount;
                } else {
                    doubleToolDamage = true;
                }

                if (!isBluntTool) {
                    itemStack.SetToolCurrentSharpness(currentSharpness);
                }

                if (sharpnessPer < 0.98f) {
                    if (!isBluntTool) {
                        doDamageTool = world.Rand.NextDouble() <= ToolsmithModSystem.Config.SharpWear;
                    } else {
                        doDamageTool = world.Rand.NextDouble() <= ToolsmithModSystem.Config.BluntWear;
                    }
                }

                if (doubleToolDamage && doDamageTool) { //The 50/50 chance for double damage roll
                    currentDur -= amount;
                }

                return doDamageTool;
            } /*else if (!world.Side.IsServer() && !itemslot.Itemstack.GetBrokeWhileSharpeningFlag() && (itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>() || itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>())) {
                return false; //Clientside Catch for hitting this point, wait for the server sync to update everything to hopefully prevent that desync from the client
            }*/
            //If it's not a tinkered or smithed tool, then let everything else run as well!
            return true;
        }
    }

    [HarmonyPatch(typeof(CollectibleObject))]
    [HarmonyPatchCategory(ToolsmithModSystem.ToolTinkeringToolUseStatsPatchCategory)]
    public class ToolTinkeringDurabilityPatches {

        [HarmonyPostfix]
        [HarmonyPatch(nameof(CollectibleObject.GetMaxDurability))]
        private static void TinkeredToolGetMaxDurabilityPostfix(ref int __result, ItemStack itemstack) {
            if (itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>() || itemstack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>()) {
                __result = (int)((double)__result * ToolsmithModSystem.Config.HeadDurabilityMult);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.SetDurability))]
        private static bool TinkeredToolSetDurabilityPrefix(ItemStack itemstack, int amount) {
            if (itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) { //Since Smithing Plus has a similar check, just running a check here to make sure no part durability is over the maximum somehow.
                var maxDur = itemstack.GetToolheadMaxDurability();
                if (amount > maxDur) {
                    amount = maxDur;
                }
                itemstack.EnsureSharpnessIsNotOverMax();
                itemstack.EnsureHandleIsNotOverMax();
                itemstack.EnsureBindingIsNotOverMax();
            } else if (itemstack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>()) {
                var maxDur = itemstack.GetSmithedDurability();
                if (amount > maxDur) {
                    amount = maxDur;
                }
                itemstack.EnsureSharpnessIsNotOverMax();
            }
            return true;
        }

        //The Postfix Patch that handles the Mining Speed (ms) Boost from any Tinkered Tools, simply just takes the output of the original call and if it's a Tinkered Tool? Add ms + ms*bonus
        [HarmonyPostfix]
        [HarmonyPatch(nameof(CollectibleObject.GetMiningSpeed))]
        private static void TinkeredToolMiningSpeedPostfix(ref float __result, IItemStack itemstack, BlockSelection blockSel, Block block, IPlayer forPlayer) {
            var isTinkeredTool = itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>();
            var isSmithedTool = itemstack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>();
            float newMiningSpeed = __result;

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

            __result = newMiningSpeed;
        }
    }

    [HarmonyPatch(typeof(CollectibleObject))]
    [HarmonyPatchCategory(ToolsmithModSystem.ToolTinkeringTransitionalPropsPatchCategory)]
    public class ToolTinkeringTransitionalPropsPatches {

        [HarmonyPostfix]
        [HarmonyPatch(nameof(CollectibleObject.GetTransitionableProperties))] //For NOW lets assume Result will likely always be Null and ignore the Result, just override it with the part's own.
        private static void ToolPartTransitionalOverridePostfix(ref TransitionableProperties[] __result, IWorldAccessor world, ItemStack itemstack, Entity forEntity) {
            if (itemstack.Collectible.HasBehavior<ModularPartRenderingFromAttributes>()) {
                if (itemstack.HasWetTreatment()) {
                    var itemCopy = itemstack.Clone();
                    itemCopy.RemoveWetTreatment();
                    var transProp = new TransitionableProperties {
                        Type = EnumTransitionType.Dry,
                        FreshHours = NatFloat.createUniform(0, 0),
                        TransitionHours = NatFloat.createUniform(itemstack.GetWetTreatment(), 0f),
                        TransitionedStack = new JsonItemStack { ResolvedItemstack = itemCopy },
                        TransitionRatio = 1
                    };

                    __result = new TransitionableProperties[] { transProp };
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(CollectibleObject.RequiresTransitionableTicking))]
        private static void ToolPartRequiresTransitionOverridePostfix(ref bool __result, IWorldAccessor world, ItemStack itemstack) {
            if (itemstack.Collectible.HasBehavior<ModularPartRenderingFromAttributes>()) {
                if (itemstack.HasWetTreatment()) {
                    __result = true;
                }
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch("UpdateAndGetTransitionStatesNative")]
        private static IEnumerable<CodeInstruction> UpdateAndGetTransitionStatesNativeTranspiler(IEnumerable<CodeInstruction> instructions) {
            var targetTransitionNow = AccessTools.Method(typeof(CollectibleObject), nameof(CollectibleObject.OnTransitionNow));
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Callvirt && (MethodInfo)codes[i].operand == targetTransitionNow) {
                    int j = i - 1;
                    while (j > 0) {
                        if (codes[j].opcode == OpCodes.Ldloc_0) {
                            codes[j].opcode = OpCodes.Ldloc_S;
                            object operand = null;
                            int k = j - 1;
                            while (k > 0) {
                                if (codes[k].opcode == OpCodes.Ldfld) {
                                    operand = codes[k - 1].operand;
                                    break;
                                }
                                k--;
                            }
                            codes[j].operand = operand;
                            break;
                        } else {
                            codes[j].opcode = OpCodes.Nop;
                        }
                        j--;
                    }
                    break;
                }
            }

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(CollectibleObject))]
    [HarmonyPatchCategory(ToolsmithModSystem.ToolTinkeringCraftingPatchCategory)]
    public class ToolTinkeringCraftingPatches {

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.ConsumeCraftingIngredients))]
        private static bool ConsumeCraftingIngredientsModularPartPrefix(ItemSlot[] slots, ItemSlot outputSlot, GridRecipe matchingRecipe, ref bool __result) {
            if (outputSlot.Itemstack.HasDisposeMeNowPlease()) {
                outputSlot.Itemstack = null;
                outputSlot.MarkDirty();

                __result = true;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(CollectibleObject))]
    [HarmonyPatchCategory(ToolsmithModSystem.ToolTinkeringRenderPatchCategory)]
    public class ToolTinkeringRenderPatches {

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.ShouldDisplayItemDamage))]
        private static bool TinkeredToolShouldDisplayItemDamagePrefix(ItemStack itemstack, ref bool __result) {
            if (itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                var lowestCurrent = TinkeringUtility.FindLowestCurrentDurabilityForBar(itemstack);
                var lowestMax = TinkeringUtility.FindLowestMaxDurabilityForBar(itemstack);

                __result = (lowestCurrent != lowestMax);

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(CollectibleObject))]
    [HarmonyPatchCategory(ToolsmithModSystem.OffhandDominantInteractionUsePatchCategory)]
    public class OffhandDominantInteractionUsePatches {

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.OnHeldUseStart))]
        private static bool OnHeldUseStartDominantOffhandInteractionPrefix(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling) {
            if (useType == EnumHandInteract.HeldItemInteract) {
                if (byEntity != null && byEntity.LeftHandItemSlot?.Empty == false && byEntity.LeftHandItemSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorOffhandDominantInteraction>()) {
                    var bh = byEntity.LeftHandItemSlot.Itemstack.Collectible.GetBehavior<CollectibleBehaviorOffhandDominantInteraction>();
                    if (bh.AskItemForHasInteractionAvailable(byEntity.LeftHandItemSlot, slot, byEntity, blockSel, entitySel, firstEvent)) {
                        return true;
                    }
                    bh.OnHeldOffhandDominantStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                    return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.OnHeldUseStep))]
        private static bool OnHeldUseStepDominantOffhandInteractionPrefix(ref EnumHandInteract __result, float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) {
            if (byEntity != null && byEntity.LeftHandItemSlot?.Empty == false && byEntity.LeftHandItemSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorOffhandDominantInteraction>()) {
                EnumHandInteract handUse = byEntity.Controls.HandUse;
                if (handUse != EnumHandInteract.HeldItemAttack) {
                    var bh = byEntity.LeftHandItemSlot.Itemstack.Collectible.GetBehavior<CollectibleBehaviorOffhandDominantInteraction>();
                    if (bh.AskItemForHasInteractionAvailable(byEntity.LeftHandItemSlot, slot, byEntity, blockSel, entitySel)) {
                        return true;
                    }
                    var retBool = bh.OnHeldOffhandDominantStep(secondsPassed, slot, byEntity, blockSel, entitySel);
                    if (retBool) {
                        __result = handUse;
                    } else {
                        __result = EnumHandInteract.None;
                    }
                    return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.OnHeldUseCancel))]
        private static bool OnHeldUseCancelDominantOffhandInteractionPrefix(ref EnumHandInteract __result, float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason) {
            if (byEntity != null && byEntity.LeftHandItemSlot?.Empty == false && byEntity.LeftHandItemSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorOffhandDominantInteraction>()) {
                EnumHandInteract handUse = byEntity.Controls.HandUse;
                if (handUse != EnumHandInteract.HeldItemAttack) {
                    var bh = byEntity.LeftHandItemSlot.Itemstack.Collectible.GetBehavior<CollectibleBehaviorOffhandDominantInteraction>();
                    if (bh.AskItemForHasInteractionAvailable(byEntity.LeftHandItemSlot, slot, byEntity, blockSel, entitySel)) {
                        return true;
                    }
                    var retBool = bh.OnHeldOffhandDominantCancel(secondsPassed, slot, byEntity, blockSel, entitySel, cancelReason);
                    if (retBool) {
                        __result = EnumHandInteract.None;
                    } else {
                        __result = handUse;
                    }
                    return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.OnHeldUseStop))]
        private static bool OnHeldUseStopDominantOffhandInteractionPrefix(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType) {
            if (useType == EnumHandInteract.HeldItemInteract) {
                if (byEntity != null && byEntity.LeftHandItemSlot?.Empty == false && byEntity.LeftHandItemSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorOffhandDominantInteraction>()) {
                    var bh = byEntity.LeftHandItemSlot.Itemstack.Collectible.GetBehavior<CollectibleBehaviorOffhandDominantInteraction>();
                    if (bh.AskItemForHasInteractionAvailable(byEntity.LeftHandItemSlot, slot, byEntity, blockSel, entitySel)) {
                        return true;
                    }
                    bh.OnHeldOffhandDominantStop(secondsPassed, slot, byEntity, blockSel, entitySel);
                    return false;
                }
            }

            return true;
        }
    }

    //Patching the GuiElementItemSlotGridBase now! Anything patching CollectibleObject is above!
    [HarmonyPatch(typeof(GuiElementItemSlotGridBase))]
    [HarmonyPatchCategory(ToolsmithModSystem.ToolTinkeringGuiElementPatchCategory)]
    public class ToolTinkeringGuiElementPatches {

        [HarmonyTranspiler]
        [HarmonyPatch("ComposeSlotOverlays")]
        private static IEnumerable<CodeInstruction> ComposeSlotOverlaysTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator) { //WOW Transpilers are FUN. And actually I do understand them better now having written this.

            int retCount = 0;
            int shadePathCount = 0;
            int index = -1;
            int indexOfSecondRet = -1;
            int indexOfShouldRenderDamageCheck = -1;
            int indexOfDamageColor = -1;
            var targetDamageColor = AccessTools.Method(typeof(CollectibleObject), nameof(CollectibleObject.GetItemDamageColor));
            int indexOfGetMaxDur = -1;
            var targetGetMaxDur = AccessTools.Method(typeof(CollectibleObject), nameof(CollectibleObject.GetMaxDurability));
            int indexOfGetRemainingDur = -1;
            var targetGetRemainingDur = AccessTools.Method(typeof(CollectibleObject), nameof(CollectibleObject.GetRemainingDurability));

            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++) {
                if (retCount < 2 && codes[i].opcode == OpCodes.Ret) { //Don't need to look at anything until after we have found three 'ret' calls
                    retCount++;
                    if (retCount == 2 && codes[i - 1].opcode == OpCodes.Ldc_I4_1) {
                        indexOfSecondRet = i; //Since I don't need to change this now anymore, using it as a stepping stone to find the nearest entry point to the check for if damage should be rendered
                    }
                    continue;
                }

                if (retCount == 2 && indexOfSecondRet > 1 && codes[i].opcode == OpCodes.Brtrue_S) {
                    indexOfShouldRenderDamageCheck = i;
                    indexOfSecondRet = 1;
                    continue;
                }

                if (retCount == 2 && shadePathCount < 2 && codes[i].opcode == OpCodes.Callvirt) {
                    if ((MethodInfo)codes[i].operand == targetDamageColor) {
                        indexOfDamageColor = i;
                        continue;
                    }
                    if ((MethodInfo)codes[i].operand == targetGetMaxDur) {
                        indexOfGetMaxDur = i;
                        continue;
                    }
                    if ((MethodInfo)codes[i].operand == targetGetRemainingDur) {
                        indexOfGetRemainingDur = i;
                        continue;
                    }
                }

                if (retCount == 2 && shadePathCount < 2 && codes[i].opcode == OpCodes.Call) {
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
                CodeInstruction.Call(typeof(ToolTinkeringGuiElementPatches), "DrawSharpnessBar", new Type[5] { typeof(ItemSlot), typeof(int), typeof(int), typeof(Context), typeof(GuiElementItemSlotGridBase) })
            };

            var toolsmithGetItemDamage = AccessTools.Method(typeof(TinkeringUtility), "ToolsmithGetItemDamageColor", new Type[1] { typeof(ItemStack) });
            var toolsmithGetMaxDur = AccessTools.Method(typeof(TinkeringUtility), "FindLowestMaxDurabilityForBar", new Type[1] { typeof(ItemStack) });
            var toolsmithGetRemainingDur = AccessTools.Method(typeof(TinkeringUtility), "FindLowestCurrentDurabilityForBar", new Type[1] { typeof(ItemStack) });
            var getItemStack = AccessTools.Method(typeof(ItemSlot), "get_Itemstack");
            var toolsmithShouldRenderSharpness = AccessTools.Method(typeof(TinkeringUtility), "ShouldRenderSharpnessBar", new Type[1] { typeof(ItemStack) });

            var shouldRenderSharpnessAddition = new List<CodeInstruction> {
                CodeInstruction.LoadArgument(1),
                new CodeInstruction(OpCodes.Call, getItemStack),
                new CodeInstruction(OpCodes.Call, toolsmithShouldRenderSharpness),
                new CodeInstruction(OpCodes.Brtrue_S, codes[indexOfShouldRenderDamageCheck].operand)
            };

            if (index >= 0 && indexOfSecondRet >= 0 && indexOfShouldRenderDamageCheck >= 0 && indexOfDamageColor >= 0 && indexOfGetMaxDur >= 0 && indexOfGetRemainingDur >= 0) {
                codeAddition[0].MoveLabelsFrom(codes[index]);
                codes.InsertRange(index, codeAddition);
                codes[indexOfDamageColor - 5].opcode = OpCodes.Nop;
                codes[indexOfDamageColor - 4].opcode = OpCodes.Nop;
                codes[indexOfDamageColor - 3].opcode = OpCodes.Nop;
                codes[indexOfDamageColor].opcode = OpCodes.Call;
                codes[indexOfDamageColor].operand = toolsmithGetItemDamage;
                codes[indexOfGetMaxDur - 5].opcode = OpCodes.Nop;
                codes[indexOfGetMaxDur - 4].opcode = OpCodes.Nop;
                codes[indexOfGetMaxDur - 3].opcode = OpCodes.Nop;
                codes[indexOfGetMaxDur].opcode = OpCodes.Call;
                codes[indexOfGetMaxDur].operand = toolsmithGetMaxDur;
                codes[indexOfGetRemainingDur - 5].opcode = OpCodes.Nop;
                codes[indexOfGetRemainingDur - 4].opcode = OpCodes.Nop;
                codes[indexOfGetRemainingDur - 3].opcode = OpCodes.Nop;
                codes[indexOfGetRemainingDur].opcode = OpCodes.Call;
                codes[indexOfGetRemainingDur].operand = toolsmithGetRemainingDur;
                codes.InsertRange(indexOfShouldRenderDamageCheck + 1, shouldRenderSharpnessAddition);
            } else {
                ToolsmithModSystem.Logger.Error("Durability and Sharpness Bar Transpiler had an error!  Will not patch anything, errors will follow:");
                if (index < 0) {
                    ToolsmithModSystem.Logger.Error("Could not find the second call to ShadePath for the Damage Bar rendering.");
                }
                if (indexOfSecondRet < 0) {
                    ToolsmithModSystem.Logger.Error("Could not locate the second return call.");
                }
                if (indexOfShouldRenderDamageCheck < 0) {
                    ToolsmithModSystem.Logger.Error("Could not locate the render damage check.");
                }
                if (indexOfDamageColor < 0) {
                    ToolsmithModSystem.Logger.Error("Could not locate the Damage Color call.");
                }
                if (indexOfGetMaxDur < 0) {
                    ToolsmithModSystem.Logger.Error("Could not locate the GetMaxDurability call.");
                }
                if (indexOfGetRemainingDur < 0) {
                    ToolsmithModSystem.Logger.Error("Could not locate the GetRemainingDurability call.");
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