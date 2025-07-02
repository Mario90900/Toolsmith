using HarmonyLib;
using SmithingOverhaul.Behaviour;
using SmithingOverhaul.Item;
using SmithingOverhaul.Property;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SmithingOverhaul.Patches
{
    [HarmonyPatch(typeof(BlockEntityAnvil))]
    [HarmonyPatchCategory(SmithingOverhaulModSystem.AnvilHammerHitPatches)]
    public class AnvilHammerHitPatches
    {

        [HarmonyTranspiler]
        [HarmonyPatch(nameof(BlockEntityAnvil.OnHit))]
        public static IEnumerable<CodeInstruction> OnHitTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            MethodInfo moveVoxelDown = AccessTools.Method(typeof(BlockEntityAnvil), "moveVoxelDownwards");
            MethodInfo getCollectible = AccessTools.PropertyGetter(typeof(ItemStack), nameof(ItemStack.Collectible));
            MethodInfo getWorkItem = AccessTools.PropertyGetter(typeof(BlockEntityAnvil), nameof(BlockEntityAnvil.WorkItemStack));
            MethodInfo afterOnHit = AccessTools.Method(typeof(SmithingWorkItem), nameof(SmithingWorkItem.AfterOnHit));
            MethodInfo isFracturing = AccessTools.Method(typeof(SmithingWorkItem), nameof(SmithingWorkItem.IsOverstrained));
            MethodInfo fracture = AccessTools.Method(typeof(SmithingUtils), nameof(SmithingUtils.Fracture));

            return new CodeMatcher(instructions, generator)
            //Create new local variables
            .DeclareLocal(typeof(int), out LocalBuilder voxelsDisplaced)
            .DeclareLocal(typeof(int), out LocalBuilder smithItem)
            //Move cursor to the start and instantiate new local variable to 0
            .Start().InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_0))
            .Insert(CodeInstruction.StoreLocal(voxelsDisplaced.LocalIndex))
            //Move to after the first nested loops that calculate voxelsMoved
            .MatchStartForward(CodeMatch.Calls(moveVoxelDown))
            .MatchStartForward(CodeMatch.Calls(moveVoxelDown))
            .MatchStartForward(CodeMatch.Branches()).Advance(-1)
            //Add number of voxelsMoved to voxelsDisplaced
            .InsertAndAdvance(CodeInstruction.LoadLocal(0))
            .InsertAndAdvance(CodeInstruction.LoadLocal(voxelsDisplaced.LocalIndex))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Add))
            .Insert(CodeInstruction.StoreLocal(voxelsDisplaced.LocalIndex))
            //Move to next nested loops to where a voxel is moved to an empty spot
            .MatchStartForward(new CodeMatch(CodeInstruction.LoadLocal(6)))
            .MatchStartForward(new CodeMatch(CodeInstruction.LoadLocal(6))).Advance(-1)
            //Increment voxelsDisplaced
            .InsertAndAdvance(CodeInstruction.LoadLocal(voxelsDisplaced.LocalIndex))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_1))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Add))
            .Insert(CodeInstruction.StoreLocal(voxelsDisplaced.LocalIndex))
            //Move to end of method
            .MatchStartForward(new CodeMatch(OpCodes.Ret))
            .MatchStartForward(new CodeMatch(OpCodes.Ret))
            //Increment voxelsDisplaced and create label to use later
            .Insert(CodeInstruction.LoadLocal(voxelsDisplaced.LocalIndex))
            .CreateLabel(out Label mylabel).Advance(1)
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_1))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Add))
            .Insert(CodeInstruction.StoreLocal(voxelsDisplaced.LocalIndex))
            //Move back to end of if statement to fix the control flow
            .MatchStartBackwards(new CodeMatch(OpCodes.Ret))
            .SetInstruction(new CodeInstruction(OpCodes.Br, mylabel))
            //Move back to end of method
            .End()
            //Check if workitemstack item class isnt null but dont jump yet
            .InsertAndAdvance(CodeInstruction.LoadArgument(0))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, getWorkItem))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, getCollectible))
            //Check if workitemstack is of class Smithing work item but dont jump yet
            .InsertAndAdvance(CodeInstruction.LoadArgument(0))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, getWorkItem))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, getCollectible))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Isinst, typeof(SmithingWorkItem)))
            //Finish the if block
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ret)).CreateLabel(out Label ret)
            //Mark this spot for if block above
            .Insert(CodeInstruction.LoadArgument(0)).CreateLabel(out Label mylabel2)
            //Go back and add the jumps
            .MatchStartBackwards(new CodeMatch(new CodeInstruction(OpCodes.Isinst,typeof(SmithingWorkItem))))
            .Advance(1)
            .Insert(new CodeInstruction(OpCodes.Brtrue_S, mylabel2))
            .MatchStartBackwards(new CodeMatch(new CodeInstruction(OpCodes.Callvirt, getCollectible)))
            .MatchStartBackwards(new CodeMatch(new CodeInstruction(OpCodes.Callvirt, getCollectible)))
            .Advance(1)
            .Insert(new CodeInstruction(OpCodes.Brfalse_S, ret))
            //Go back to the block inside the if statement
            .MatchStartForward(new CodeMatch(new CodeInstruction(OpCodes.Ret)))
            .MatchStartForward(new CodeMatch(CodeInstruction.LoadArgument(0)))
            .Advance(1)
            //Store smithItem class in local variable
            .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, getWorkItem))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, getCollectible))
            .InsertAndAdvance(CodeInstruction.StoreLocal(smithItem.LocalIndex))
            //Call the AfterOnHit method
            .InsertAndAdvance(CodeInstruction.LoadLocal(voxelsDisplaced.LocalIndex))
            .InsertAndAdvance(CodeInstruction.LoadArgument(0))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, getWorkItem))
            .InsertAndAdvance(CodeInstruction.LoadLocal(smithItem.LocalIndex))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, afterOnHit))
            //Check if the piece fractured
            .InsertAndAdvance(CodeInstruction.LoadArgument(0))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, getWorkItem))
            .InsertAndAdvance(CodeInstruction.LoadLocal(smithItem.LocalIndex))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, isFracturing))
            .CreateLabel(out Label finalRet)
            .InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, finalRet))
            .InsertAndAdvance(CodeInstruction.LoadArgument(1))
            .InsertAndAdvance(CodeInstruction.LoadArgument(0))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Call, fracture))
            //Finish Transpiller
            .InstructionEnumeration();
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BlockEntityAnvil.OnUpset))]
        public static void OnUpsetPostfix(Vec3i voxelPos, BlockEntityAnvil __instance)
        {
            if (__instance.WorkItemStack.Collectible != null && __instance.WorkItemStack.Collectible is SmithingWorkItem)
            {
                SmithingWorkItem item = (__instance.WorkItemStack.Collectible as SmithingWorkItem);
                item.AfterOnUpset(__instance.WorkItemStack);
                if (item.IsOverstrained(__instance.WorkItemStack)) SmithingUtils.Fracture(voxelPos, __instance);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BlockEntityAnvil.OnSplit))]
        public static void OnSplitPostfix(Vec3i voxelPos, BlockEntityAnvil __instance)
        {
            if (__instance.WorkItemStack.Collectible != null && __instance.WorkItemStack.Collectible is SmithingWorkItem)
            {
                SmithingWorkItem item = (__instance.WorkItemStack.Collectible as SmithingWorkItem);
                item.AfterOnUpset(__instance.WorkItemStack);
                if (item.IsOverstrained(__instance.WorkItemStack)) SmithingUtils.Fracture(voxelPos, __instance);
            }
        }
    }

    [HarmonyPatch(typeof(ItemWorkItem))]
    [HarmonyPatchCategory(SmithingOverhaulModSystem.ItemWorkItemPatches)]
    public class ItemWorkItemPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ItemWorkItem.CanWork))]
        public static void CanWork_Postfix(
            ItemStack stack,
            ItemWorkItem __instance,
            ref bool __result,
            ref ICoreAPI ___api,
            ref SmithingPropertyVariant ___SmithProps
        )
        {
            if (__instance is SmithingWorkItem)
            {
                bool preventDefault = false;
                bool canWork = false;
                SmithingWorkItem item = (SmithingWorkItem)__instance;

                foreach (SmithingBehavior behavior in item.SmithingBehaviors)
                {
                    EnumHandling handled = EnumHandling.PassThrough;
                    bool canWorkBh = behavior.CanWork(___api.World, stack, ref handled);
                    if (handled != EnumHandling.PassThrough)
                    {
                        canWork = canWorkBh;
                        preventDefault = true;
                    }

                    if (handled == EnumHandling.PreventSubsequent) __result = canWork;
                }

                if (preventDefault) __result = canWork;

                //Default Behaviour

                float temperature = stack.Collectible.GetTemperature(___api.World, stack);
                float workTemp = 0;

                if (___SmithProps != null)
                {
                    workTemp = ___SmithProps.WarmForgingTemp;
                }

                if (stack.Collectible.Attributes?["workableTemperature"].Exists == true)
                {
                    __result = stack.Collectible.Attributes["workableTemperature"].AsFloat(workTemp) <= temperature;
                }

                __result = temperature >= workTemp;
            }
        }
    }
}
