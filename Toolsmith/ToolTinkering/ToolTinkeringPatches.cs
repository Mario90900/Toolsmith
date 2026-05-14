using Cairo;
using HarmonyLib;
using ScientificSmithy.Behaviour;
using SmithingPlus.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
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

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.Equals))]
        private static bool EqualsModularPartPrefix(ItemStack thisStack, ItemStack otherStack, ref bool __result, params string[] ignoreAttributeSubTrees) {
            if (ignoreAttributeSubTrees != null && thisStack.Collectible.HasBehavior<ModularPartRenderingFromAttributes>() && thisStack.Collectible.MaxStackSize > 1) {
                if (thisStack.Class == otherStack.Class && thisStack.Id == otherStack.Id) {
                    var newIgnoreAttributes = ignoreAttributeSubTrees.Remove(ToolsmithAttributes.ModularMultiPartDataTree).Remove(ToolsmithAttributes.ModularPartDataTree);
                    __result = thisStack.Attributes.Equals(ToolsmithModSystem.Api.World, otherStack.Attributes, newIgnoreAttributes);
                    return false;
                }
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
            if (itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>() || TinkeringUtility.IsValidHead(itemstack) || TinkeringUtility.IsValidHandle(itemstack)) {
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
            //Guard against null slots / empty stacks / out-of-range slot indices. ComposeSlotOverlays runs
            //for every slot in every visible inventory grid (chests, storage vessels, etc.), so a slot
            //containing a non-Toolsmith item is the common case. Without these guards Toolsmith crashes
            //the game on any chest interaction; see issue #35.
            if (slot?.Itemstack == null) {
                return;
            }
            if (instance?.SlotBounds == null || slotIndex < 0 || slotIndex >= instance.SlotBounds.Length) {
                return;
            }
            if (TinkeringUtility.ShouldRenderSharpnessBar(slot.Itemstack)) {
                double x = ElementBounds.scaled(4);
                double y = (int)instance.SlotBounds[slotIndex].InnerHeight - ElementBounds.scaled(8) - ElementBounds.scaled(4);
                textCtx.SetSourceRGBA(GuiStyle.DialogStrongBgColor);
                double width = (instance.SlotBounds[slotIndex].InnerWidth - ElementBounds.scaled(8));
                double height = ElementBounds.scaled(4);
                GuiElement.RoundRectangle(textCtx, x, y, width, height, 1);
                textCtx.FillPreserve();
                instance.ShadePath(textCtx, 2);

                int maxSharp = slot.Itemstack.GetToolMaxSharpness();
                float remainingSharpness = (float)slot.Itemstack.GetToolCurrentSharpness() / maxSharp;
                width = remainingSharpness * (instance.SlotBounds[slotIndex].InnerWidth - ElementBounds.scaled(8));
                float[] color;

                if (ToolsmithModSystem.ClientConfig?.UseGradientForSharpnessInstead == true) {
                    if (TinkeringUtility.GradiantNeedsInit()) {
                        TinkeringUtility.InitializeSharpnessColorGradient();
                    }

                    color = ColorUtil.ToRGBAFloats(TinkeringUtility.GetItemSharpnessColor(slot.Itemstack));
                    textCtx.SetSourceRGB(color[0], color[1], color[2]);

                    GuiElement.RoundRectangle(textCtx, x, y, width, height, 1);
                    textCtx.FillPreserve();
                    instance.ShadePath(textCtx, 2);

                    return;
                }

                if (ToolsmithModSystem.ClientConfig?.ShowAllSharpnessBarSections == true) {
                    double totalBarWidth = (instance.SlotBounds[slotIndex].InnerWidth - ElementBounds.scaled(8));
                    double dx = x;
                    double dWidth;
                    int count = 0;
                    double widthPlotted = 0;

                    while (count < 5) {
                        color = ColorUtil.ToRGBAFloats(TinkeringUtility.GetFlatItemSharpnessColor(count));
                        textCtx.SetSourceRGB(color[0], color[1], color[2]);

                        if (count < 2) {
                            dWidth = 0.15 * totalBarWidth;
                        } else if (count < 3) {
                            dWidth = 0.3 * totalBarWidth;
                        } else {
                            dWidth = 0.2 * totalBarWidth;
                        }

                        if (widthPlotted + dWidth > width) {
                            dWidth = width - widthPlotted;
                        }

                        GuiElement.RoundRectangle(textCtx, dx, y, dWidth, height, 1);
                        textCtx.FillPreserve();
                        instance.ShadePath(textCtx, 2);
                        widthPlotted += dWidth;

                        if (widthPlotted == width) {
                            break;
                        }

                        dx += dWidth;
                        count++;
                    }
                } else {
                    if (remainingSharpness < 0.15) {
                        color = ColorUtil.ToRGBAFloats(TinkeringUtility.GetFlatItemSharpnessColor(0));
                    } else if (remainingSharpness < 0.3) {
                        color = ColorUtil.ToRGBAFloats(TinkeringUtility.GetFlatItemSharpnessColor(1));
                    } else if (remainingSharpness < 0.6) {
                        color = ColorUtil.ToRGBAFloats(TinkeringUtility.GetFlatItemSharpnessColor(2));
                    } else if (remainingSharpness < 0.8) {
                        color = ColorUtil.ToRGBAFloats(TinkeringUtility.GetFlatItemSharpnessColor(3));
                    } else {
                        color = ColorUtil.ToRGBAFloats(TinkeringUtility.GetFlatItemSharpnessColor(4));
                    }
                    textCtx.SetSourceRGB(color[0], color[1], color[2]);

                    GuiElement.RoundRectangle(textCtx, x, y, width, height, 1);
                    textCtx.FillPreserve();
                    instance.ShadePath(textCtx, 2);
                }
            }
        }
    }

    //Patching ItemAxe to ideally keep marking all Wood Blocks as Dirty to send them to the client, hopefully solving the Ghost Trees once and for all and not causing an RNG desync in the process.
    [HarmonyPatch(typeof(ItemAxe))]
    [HarmonyPatchCategory(ToolsmithModSystem.ToolTinkeringItemAxePatchCategory)]
    public class ItemAxePatches {

        [HarmonyTranspiler]
        [HarmonyPatch(nameof(ItemAxe.OnBlockBrokenWith))]
        public static IEnumerable<CodeInstruction> OnBlockBrokenWithTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator) {
            var codes = new List<CodeInstruction>(instructions);

            int indexBeforeBreakBlock = -1;
            int indexAfterBreakBlock = -1;
            var breakBlockMethod = AccessTools.Method(typeof(IBlockAccessor), "BreakBlock", new Type[] { typeof(BlockPos), typeof(IPlayer), typeof(float) });

            for (int i = 0; i < codes.Count(); i++) {
                if (codes[i].opcode == OpCodes.Callvirt && (MethodInfo)codes[i].operand == breakBlockMethod) {
                    indexAfterBreakBlock = i + 1;
                    if (codes[i - 12].opcode == OpCodes.Callvirt) {
                        indexBeforeBreakBlock = i - 13;
                    }
                }
            }

            if (indexAfterBreakBlock >= 0 && indexBeforeBreakBlock >= 0) {
                var newLabel = ilGenerator.DefineLabel();
                var serverSideBreakBlock = new List<CodeInstruction>() {
                    codes[indexAfterBreakBlock].Clone(),
                    codes[indexAfterBreakBlock+1].Clone(),
                    new CodeInstruction(OpCodes.Ldc_I4_1),
                    new CodeInstruction(OpCodes.Bne_Un, newLabel)
                };

                codes[indexAfterBreakBlock].labels.Add(newLabel);
                codes.InsertRange(indexBeforeBreakBlock, serverSideBreakBlock);
            } else {
                ToolsmithModSystem.Logger.Error("ItemAxe Transpiler had an error! Will not patch anything, and Ghost Trees will occur. More specifics will follow:");
                if (indexAfterBreakBlock < 0) {
                    ToolsmithModSystem.Logger.Error("Could not find the BreakBlock call.");
                }
                if (indexBeforeBreakBlock < 0) {
                    ToolsmithModSystem.Logger.Error("Could not locate the start of the BreakBlock call. Did something else change it?");
                }
            }

            return codes.AsEnumerable();
        }
    }
}
