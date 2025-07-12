using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Toolsmith.SmithingOverhaul.Behaviour;
using Toolsmith.SmithingOverhaul.Item;
using Toolsmith.SmithingOverhaul.Property;
using Toolsmith.SmithingOverhaul.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static HarmonyLib.Code;

namespace Toolsmith.SmithingOverhaul.Patches
{
    [HarmonyPatch(typeof(BlockEntityAnvil))]
    [HarmonyPatchCategory(SmithingOverhaulModSystem.AnvilPatches)]
    public class AnvilPatches
    {
        static readonly MethodInfo getCollectible = AccessTools.PropertyGetter(typeof(ItemStack), nameof(ItemStack.Collectible));
        static readonly FieldInfo workstack = AccessTools.Field(typeof(ItemStack), "workItemStack");
        static readonly MethodInfo getWorkItem = AccessTools.PropertyGetter(typeof(BlockEntityAnvil), nameof(BlockEntityAnvil.WorkItemStack));
        static readonly FieldInfo getApi = AccessTools.Field(typeof(BlockEntityAnvil), nameof(BlockEntityAnvil.Api));
        static readonly MethodInfo moveVoxelDown = AccessTools.Method(typeof(BlockEntityAnvil), "moveVoxelDownwards");
        static readonly MethodInfo afterOnHit = AccessTools.Method(typeof(SmithingWorkItem), nameof(SmithingWorkItem.AfterOnHit));
        static readonly MethodInfo isFracturing = AccessTools.Method(typeof(SmithingWorkItem), nameof(SmithingWorkItem.IsOverstrained));
        static readonly MethodInfo fracture = AccessTools.Method(typeof(SmithingUtils), nameof(SmithingUtils.Fracture));
        static readonly MethodInfo addSmithAttr = AccessTools.Method(typeof(SmithingUtils), nameof(SmithingUtils.AddSmithingOutputAttr));
        
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(BlockEntityAnvil.OnHit))]
        private static IEnumerable<CodeInstruction> OnHitTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {

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

        [HarmonyTranspiler]
        [HarmonyPatch(nameof(BlockEntityAnvil.CheckIfFinished))]
        private static IEnumerable<CodeInstruction> CheckIfFinishedOutputHook(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);

            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldarg_0 && i+1 < codes.Count)
                {
                    if (codes[i+1].opcode == OpCodes.Ldnull && i+2 < codes.Count)
                    {
                        if (codes[i+2].opcode == OpCodes.Stfld &&
                            codes[i+2].operand is FieldInfo &&
                           (codes[i+2].operand as FieldInfo) == workstack)
                        {
                            return codeMatcher.CreateLabelAt(i, out Label ifend).Start().Advance(i)
                                        .InsertAndAdvance(CodeInstruction.LoadArgument(0))
                                        .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, getWorkItem))
                                        .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, getCollectible))
                                        .InsertAndAdvance(new CodeInstruction(OpCodes.Isinst, typeof(SmithingWorkItem)))
                                        .InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, ifend))
                                        .InsertAndAdvance(CodeInstruction.LoadLocal(0))
                                        .InsertAndAdvance(CodeInstruction.LoadArgument(0))
                                        .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, getWorkItem))
                                        .InsertAndAdvance(CodeInstruction.LoadArgument(0))
                                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, getApi))
                                        .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, addSmithAttr))
                                        .InstructionEnumeration();
                        }
                    }
                }
            }

            return null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BlockEntityAnvil.OnUpset))]
        private static void OnUpsetPostfix(Vec3i voxelPos, BlockEntityAnvil __instance)
        {
            if (__instance.WorkItemStack.Collectible != null && __instance.WorkItemStack.Collectible is SmithingWorkItem)
            {
                SmithingWorkItem item = (__instance.WorkItemStack.Collectible as SmithingWorkItem);
                item.AfterOnUpset(__instance.WorkItemStack);
                if (item.IsOverstrained(__instance.WorkItemStack)) SmithingUtils.Fracture(__instance, voxelPos);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BlockEntityAnvil.OnSplit))]
        private static void OnSplitPostfix(Vec3i voxelPos, BlockEntityAnvil __instance)
        {
            if (__instance.WorkItemStack.Collectible != null && __instance.WorkItemStack.Collectible is SmithingWorkItem)
            {
                SmithingWorkItem item = (__instance.WorkItemStack.Collectible as SmithingWorkItem);
                item.AfterOnUpset(__instance.WorkItemStack);
                if (item.IsOverstrained(__instance.WorkItemStack)) SmithingUtils.Fracture(__instance, voxelPos));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BlockEntityAnvil.ditchWorkItemStack))]
        private static void AddStressStrainHandlerToAttributesAfterDitching(BlockEntityAnvil __instance)
        {
            if(__instance.WorkItemStack.Collectible is SmithingWorkItem)
            {
                StressStrainHandler ssh = __instance.WorkItemStack.GetStressStrainHandler(__instance.Api);
                if (ssh == null) return;

                ssh.ToTreeAttributes(__instance.WorkItemStack.Attributes);
            }
        }
    }

    [HarmonyPatch(typeof(ItemWorkItem))]
    [HarmonyPatchCategory(SmithingOverhaulModSystem.ItemWorkItemPatches)]
    public class ItemWorkItemPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ItemWorkItem.CanWork))]
        private static void CanWork_Postfix(
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
                StressStrainHandler ssh = stack.GetStressStrainHandler(___api);
                if (ssh == null) return;

                foreach (SmithingBehavior behavior in item.SmithingBehaviors)
                {
                    EnumHandling handled = EnumHandling.PassThrough;
                    bool canWorkBh = behavior.OnCanWork(ssh, stack, ___api.World, ref handled);
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

                float workTemp = ssh.ForgingTemp;

                __result = temperature >= workTemp;
            }
        }
    }

    [HarmonyPatch(typeof(CollectibleObject))]
    [HarmonyPatchCategory(SmithingOverhaulModSystem.WorkItemStatsPatches)]
    public class WorkItemStatsPatches
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(nameof(CollectibleObject.GetMaxDurability))]
        private static void OverrideDefaultDurability(ref int __result, ItemStack itemstack, ICoreAPI ___api)
        {
            if(itemstack.Collectible is SmithingWorkItem && SmithingOverhaulModSystem.Config.EnableSmithingOverhaul)
            {
                __result = (int)(itemstack.GetStressStrainHandler(___api).GetToughness() * 0.1);
            }
        }
    }
}
