using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.ToolTinkering.Behaviors;
using Toolsmith.ToolTinkering.Drawbacks;
using Toolsmith.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Collectible.Block;
using Vintagestory.GameContent;

namespace Toolsmith.ToolTinkering.Blocks {
    public class BlockWorkbench : Block, IMultiBlockColSelBoxes, IMultiBlockInteract {

        WorldInteraction[] viseInteractions;
        WorldInteraction[] emptyHeadCraftingSlotsInteraction;
        WorldInteraction[] emptyHandleCraftingSlotsInteraction;
        WorldInteraction[] emptyBindingCraftingSlotsInteraction;
        WorldInteraction[] emptyMergableCraftingSlotsInteraction;
        WorldInteraction[] emptyReforgeStagingInteraction;
        WorldInteraction[] fullSlotGetItemInteraction;
        WorldInteraction[] fullReforgeStagingInteraction;
        WorldInteraction[] fullSlotCraftingInteraction;
        Cuboidf[] offsetHalfSelections;
        float offsetInteractionYOffset = 0f;

        Dictionary<string, List<ItemStack>> bundledItemLists;
        List<ItemStack> tinkerableTools;
        List<ItemStack> toolHeads;
        List<ItemStack> toolHandles;
        List<ItemStack> toolBindings;
        List<ItemStack> reforgables;
        List<ItemStack> hammers;

        public override void OnLoaded(ICoreAPI api) {
            base.OnLoaded(api);
            InteractionHelpYOffset = 1.3f;

            if (api.Side.IsServer()) {
                return;
            }

            ICoreClientAPI capi = api as ICoreClientAPI;

            GetOrInitItemLists(capi);

            viseInteractions = ObjectCacheUtil.GetOrCreate(capi, "workbenchViseInteraction", () => {
                return new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-workbench-deconstruct",
                        HotKeyCode = "shift",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = tinkerableTools.ToArray()
                    },
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-workbench-breakdownworkpiece",
                        HotKeyCode = "shift",
                        MouseButton = EnumMouseButton.Right
                    }
                };
            });

            emptyHeadCraftingSlotsInteraction = ObjectCacheUtil.GetOrCreate(capi, "workbenchEmptyHeadCraftingSlotsInteraction", () => {
                return new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-workbench-craftingspot-head",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = toolHeads.ToArray()
                    }
                };
            });

            emptyHandleCraftingSlotsInteraction = ObjectCacheUtil.GetOrCreate(capi, "workbenchEmptyHandleCraftingSlotsInteraction", () => {
                return new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-workbench-craftingspot-handle",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = toolHandles.ToArray()
                    }
                };
            });

            emptyBindingCraftingSlotsInteraction = ObjectCacheUtil.GetOrCreate(capi, "workbenchEmptyBindingCraftingSlotsInteraction", () => {
                return new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-workbench-craftingspot-binding",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = toolBindings.ToArray()
                    }
                };
            });

            emptyMergableCraftingSlotsInteraction = ObjectCacheUtil.GetOrCreate(capi, "workbenchEmptyMergableCraftingSlotsInteraction", () => {
                return new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-workbench-craftingspot-mergable",
                        MouseButton = EnumMouseButton.Right
                    }
                };
            });

            emptyReforgeStagingInteraction = ObjectCacheUtil.GetOrCreate(capi, "workbenchEmptyReforgeStagingInteraction", () => {
                return new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-workbench-reforge",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = reforgables.ToArray()
                    }
                };
            });

            fullSlotGetItemInteraction = ObjectCacheUtil.GetOrCreate(capi, "workbenchFullSlotGetItemInteraction", () => {
                return new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-workbench-retrieve-item",
                        MouseButton = EnumMouseButton.Right
                    }
                };
            });

            fullSlotCraftingInteraction = ObjectCacheUtil.GetOrCreate(capi, "workbenchStrikeCraftingInteraction", () => {
                return new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-workbench-strike-craft",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = hammers.ToArray()
                    }
                };
            });

            fullReforgeStagingInteraction = ObjectCacheUtil.GetOrCreate(capi, "workbenchReadyToReforge", () => {

                return new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-workbench-reforge-head",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = hammers.ToArray()
                    }
                };
            });

            CleanUpItemLists();
        }

        protected void GetOrInitItemLists(ICoreClientAPI capi) {
            bundledItemLists = ObjectCacheUtil.GetOrCreate(capi, "workbenchDisplayTinkerableToolsList", () => {
                List<ItemStack> tinkerables = new List<ItemStack>();
                List<ItemStack> heads = new List<ItemStack>();
                List<ItemStack> handles = new List<ItemStack>();
                List<ItemStack> bindings = new List<ItemStack>();
                List<ItemStack> reforge = new List<ItemStack>();
                List<ItemStack> craftTools = new List<ItemStack>();

                foreach (Item i in capi.World.Items) {
                    if (i.Code == null) {
                        continue;
                    }

                    if (i.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                        tinkerables.Add(new ItemStack(i)); //All tinkered tools can be deconstructed!
                    }

                    if (i.HasBehavior<CollectibleBehaviorToolHead>()) {
                        heads.Add(new ItemStack(i)); //All tool heads!
                    }

                    if (i.HasBehavior<CollectibleBehaviorToolHandle>()) {
                        handles.Add(new ItemStack(i)); //All handles!
                    }

                    if (i.HasBehavior<CollectibleBehaviorToolBinding>()) {
                        bindings.Add(new ItemStack(i)); //All bindings!
                    }

                    if ((i.HasBehavior<CollectibleBehaviorToolHead>() || i.HasBehavior<CollectibleBehaviorSmithedTools>()) && i.IsCraftableMetal()) {
                        reforge.Add(new ItemStack(i)); //And all tool heads can be converted into a workpiece when you want to reforge.
                    }

                    if (i.Tool == EnumTool.Hammer) {
                        craftTools.Add(new ItemStack(i));
                    }
                }

                return new Dictionary<string, List<ItemStack>>() {
                    ["tinkerableTools"] = tinkerables,
                    ["heads"] = heads,
                    ["handles"] = handles,
                    ["bindings"] = bindings,
                    ["reforgables"] = reforge,
                    ["hammers"] = craftTools
                };
            });

            bundledItemLists.TryGetValue("tinkerableTools", out tinkerableTools);
            bundledItemLists.TryGetValue("heads", out toolHeads);
            bundledItemLists.TryGetValue("handles", out toolHandles);
            bundledItemLists.TryGetValue("bindings", out toolBindings);
            bundledItemLists.TryGetValue("reforgables", out reforgables);
            bundledItemLists.TryGetValue("hammers", out hammers);
        }

        protected void CleanUpItemLists() {
            bundledItemLists.Clear();
            bundledItemLists = null;
            tinkerableTools?.Clear();
            tinkerableTools = null;
            toolHeads?.Clear();
            toolHeads = null;
            toolHandles?.Clear();
            toolHandles = null;
            toolBindings?.Clear();
            toolBindings = null;
            reforgables?.Clear();
            reforgables = null;
            hammers?.Clear();
            hammers = null;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
            BlockEntityWorkbench beWorkbench = GetBlockEntity<BlockEntityWorkbench>(selection.Position);
            if (beWorkbench == null) {
                return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
            }

            var slotIndex = selection.SelectionBoxIndex;
            if (slotIndex >= (int)WorkbenchSlots.CraftingSlot1 && slotIndex <= (int)WorkbenchSlots.CraftingSlot5) {
                if (beWorkbench.IsSelectSlotEmpty(slotIndex)) {
                    var slotExpecting = beWorkbench.WhatSlotMarkerIndicator(slotIndex);
                    switch (slotExpecting) {
                        case "head":
                            return emptyHeadCraftingSlotsInteraction.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
                        case "handle":
                            return emptyHandleCraftingSlotsInteraction.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
                        case "binding":
                            return emptyBindingCraftingSlotsInteraction.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
                        default:
                            return emptyMergableCraftingSlotsInteraction.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
                    }
                } else {
                    return fullSlotGetItemInteraction.Append(fullSlotCraftingInteraction.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer)));
                }
            } else if (slotIndex == (int)WorkbenchSlots.Vise) {
                return viseInteractions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
            } else if (slotIndex == (int)WorkbenchSlots.ReforgeStaging) {
                if (beWorkbench.IsSelectSlotEmpty(slotIndex)) {
                    return emptyReforgeStagingInteraction.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
                } else {
                    return fullSlotGetItemInteraction.Append(fullReforgeStagingInteraction.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer)));
                }
            }

            return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            BlockEntityWorkbench workbenchEnt = GetBlockEntity<BlockEntityWorkbench>(blockSel.Position);
            var entPlayer = byPlayer.Entity;
            if (!entPlayer.Controls.ShiftKey && workbenchEnt != null) {
                if (blockSel.SelectionBoxIndex >= (int)WorkbenchSlots.CraftingSlot1 && blockSel.SelectionBoxIndex <= (int)WorkbenchSlots.CraftingSlot5) { //Player is attempting to place something in one of the 5 crafting spots!
                    if (byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible?.Tool == EnumTool.Hammer) {
                        if (TryCraftingAction(world, byPlayer, blockSel, workbenchEnt)) {
                            if (world.Side.IsServer()) {
                                world.PlaySoundAt(new AssetLocation("sounds/block/meteoriciron-hit-pickaxe"), entPlayer.Pos.X, entPlayer.Pos.Y, entPlayer.Pos.Z, null, true, 32f, 1f);
                                workbenchEnt.MarkDirty(redrawOnClient: true);
                            }
                            return true;
                        }
                    } else {
                        if (TryPlaceOrGetItemCraftingSlots(world, byPlayer, blockSel, workbenchEnt)) {
                            if (world.Side.IsServer()) {
                                world.PlaySoundAt(new AssetLocation("sounds/player/build"), entPlayer.Pos.X, entPlayer.Pos.Y, entPlayer.Pos.Z, null, true, 32f, 1f);
                                workbenchEnt.MarkDirty(redrawOnClient: true);
                            }
                            return true;
                        }
                    }
                } else if (blockSel.SelectionBoxIndex == (int)WorkbenchSlots.ReforgeStaging) { //Player is attempting to place something in the Reforging spot!
                    if (byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible?.Tool == EnumTool.Hammer) {
                        if (AttemptReforgingToolHead(world, byPlayer, blockSel, workbenchEnt)) {
                            if (world.Side.IsServer()) {
                                world.PlaySoundAt(new AssetLocation("sounds/block/meteoriciron-hit-pickaxe"), entPlayer.Pos.X, entPlayer.Pos.Y, entPlayer.Pos.Z, null, true, 32f, 1f);
                                workbenchEnt.MarkDirty(redrawOnClient: true);
                            }
                            return true;
                        }
                    } else {
                        if (TryPlaceOrGetItemReforgeSlot(world, byPlayer, blockSel, workbenchEnt)) {
                            if (world.Side.IsServer()) {
                                world.PlaySoundAt(new AssetLocation("sounds/player/build"), entPlayer.Pos.X, entPlayer.Pos.Y, entPlayer.Pos.Z, null, true, 32f, 1f);
                                workbenchEnt.MarkDirty(redrawOnClient: true);
                            }
                            return true;
                        }
                    }
                }
            } else if (entPlayer.Controls.ShiftKey && blockSel.SelectionBoxIndex == (int)WorkbenchSlots.Vise) {
                if (byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack != null) {
                    if (TinkeringUtility.IsDeconstructableTool(byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack.Collectible, world)) {
                        if (world.Side == EnumAppSide.Server) {
                            world.PlaySoundAt(new AssetLocation("sounds/player/messycraft.ogg"), entPlayer.Pos.X, entPlayer.Pos.Y, entPlayer.Pos.Z, null, true, 32f, 1f);
                        }
                        return true;
                    }
                }
            }

            return false;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            if (byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack != null) { //Make sure the slot isn't empty
                if (byPlayer.Entity.Controls.ShiftKey && TinkeringUtility.IsDeconstructableTool(byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible, world)) {
                    if (world.Side.IsServer() && blockSel.SelectionBoxIndex == (int)WorkbenchSlots.Vise && secondsUsed > 4.5) {
                        TinkeringUtility.HandleBreakdown(secondsUsed, world, byPlayer, blockSel);
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason) {
            if (ToolsmithModSystem.Config.AccessibilityDisableNeedToHoldClick && byPlayer.Entity.Controls.ShiftKey) {
                return false;
            }

            return true;
        }

        //This is only hit if it actually times out from someone holding it for too long - or the tool/head is finished repairing
        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {

        }

        //Run the checks to see if it's a valid item to even put in the selected slot here.
        protected bool TryPlaceOrGetItemCraftingSlots(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityWorkbench bench) {
            if (byPlayer.InventoryManager.ActiveHotbarSlot.Empty) { //They can only be trying to take an item out of the slot.
                return bench.TryGetItemFromWorkbench(blockSel.SelectionBoxIndex, byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer, world);
            } else {
                ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
                var isPart = TinkeringUtility.IsAnyToolPart(stack, world);
                if (bench.IsSelectSlotEmpty(blockSel.SelectionBoxIndex) && (isPart > 0 || ReforgingUtility.IsPossibleMergeItem(stack, world))) {
                    if (isPart == 0) {
                        if (world.Side.IsClient()) {
                            return true;
                        }
                        return bench.TryGetOrPutItemOnWorkbench(blockSel.SelectionBoxIndex, byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer, world);
                    } else {
                        if (blockSel.SelectionBoxIndex == (int)WorkbenchSlots.CraftingSlot3) {
                            if (isPart == 1) {
                                return bench.TryGetOrPutItemOnWorkbench(blockSel.SelectionBoxIndex, byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer, world);
                            } else {
                                return false;
                            }
                        } else if (blockSel.SelectionBoxIndex == (int)WorkbenchSlots.CraftingSlot2) {
                            if (bench.GetSlotsHoldsString((int)WorkbenchSlots.CraftingSlot3) == "head" && isPart == 2) {
                                return bench.TryGetOrPutItemOnWorkbench(blockSel.SelectionBoxIndex, byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer, world);
                            } else {
                                return false;
                            }
                        } else if (blockSel.SelectionBoxIndex == (int)WorkbenchSlots.CraftingSlot4) {
                            if (bench.GetSlotsHoldsString((int)WorkbenchSlots.CraftingSlot3) == "head" && isPart == 3) {
                                return bench.TryGetOrPutItemOnWorkbench(blockSel.SelectionBoxIndex, byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer, world);
                            } else {
                                return false;
                            }
                        } else {
                            return false;
                        }
                    }
                } else {
                    return bench.TryGetItemFromWorkbench(blockSel.SelectionBoxIndex, byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer, world);
                }
            }
        }

        protected bool TryCraftingAction(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityWorkbench bench) {
            if (!bench.IsSelectSlotEmpty(blockSel.SelectionBoxIndex)) {
                return bench.AttemptToCraft(world, byPlayer, blockSel);
            }

            return false;
        }

        protected bool TryPlaceOrGetItemReforgeSlot(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityWorkbench bench) {
            if (byPlayer.InventoryManager.ActiveHotbarSlot.Empty) {
                return bench.TryGetItemFromWorkbench(blockSel.SelectionBoxIndex, byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer, world);
            } else {
                ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
                if ((TinkeringUtility.IsValidHead(stack) || stack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>()) && stack.Collectible.IsCraftableMetal()) {
                    return bench.TryGetOrPutItemOnWorkbench(blockSel.SelectionBoxIndex, byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer, world);
                } else {
                    return bench.TryGetItemFromWorkbench(blockSel.SelectionBoxIndex, byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer, world);
                }
            }
        }

        protected bool AttemptReforgingToolHead(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityWorkbench bench) {
            if (world.Side.IsClient()) {
                return true; //This needs to return true to sync the attempt with the server. Then the server will handle all the actual processing work, if it is valid.
            }

            return bench.InitiateReforgeAttempt(world, byPlayer, blockSel);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
            var originalBoxes = base.GetSelectionBoxes(blockAccessor, pos);
            if (originalBoxes == null) {
                return null;
            }

            var selectionBoxes = originalBoxes.FastCopy(originalBoxes.Length);
            var workbenchEnt = blockAccessor.GetBlockEntity(pos) as BlockEntityWorkbench;

            if (workbenchEnt != null) {
                var slotsToShow = workbenchEnt.GetWhatSlotsAreVisible();
                for (int i = 1; i < 6; i++) {
                    if (!slotsToShow.Contains(i)) {
                        selectionBoxes[i] = new Cuboidf(0, 0, 0, 0, 0, 0);
                    }
                }
            }

            return selectionBoxes;
        }

        public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
            return GetCollisionBoxes(blockAccessor, pos + offset.AsBlockPos);
        }

        public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
            if (offsetHalfSelections == null || offsetHalfSelections.Length == 0) {
                offsetHalfSelections = new Cuboidf[SelectionBoxes.Length];
                for (int i = 0; i < SelectionBoxes.Length; i++) {
                    if (i == 0) {
                        offsetHalfSelections[i] = SelectionBoxes[i].OffsetCopy(0, 0, 0);
                    } else {
                        offsetHalfSelections[i] = SelectionBoxes[i].OffsetCopy(offset);
                    }
                }
            }

            var mainSelection = pos.Copy();
            mainSelection += offset.AsBlockPos;
            var workbenchEnt = blockAccessor.GetBlockEntity(mainSelection) as BlockEntityWorkbench;
            var offsetHalfSelectionsCopy = offsetHalfSelections.FastCopy(offsetHalfSelections.Length);

            if (workbenchEnt != null) {
                var slotsToShow = workbenchEnt.GetWhatSlotsAreVisible();
                for (int i = 1; i < 6; i++) {
                    if (!slotsToShow.Contains(i)) {
                        offsetHalfSelectionsCopy[i] = new Cuboidf(0, 0, 0, 0, 0, 0);
                    }
                }
            }

            return offsetHalfSelectionsCopy;
        }

        public bool MBDoParticalSelection(IWorldAccessor world, BlockPos pos, Vec3i offset) {
            return DoParticalSelection(world, pos + offset.AsBlockPos);
        }

        public bool MBOnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, Vec3i offset) {
            var mainSelection = blockSel.Clone();
            mainSelection.Position += offset.AsBlockPos;
            return OnBlockInteractStart(world, byPlayer, mainSelection);
        }

        public bool MBOnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, Vec3i offset) {
            var mainSelection = blockSel.Clone();
            mainSelection.Position += offset.AsBlockPos;
            return OnBlockInteractStep(secondsUsed, world, byPlayer, mainSelection);
        }

        public void MBOnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, Vec3i offset) {
            var mainSelection = blockSel.Clone();
            mainSelection.Position += offset.AsBlockPos;
            OnBlockInteractStop(secondsUsed, world, byPlayer, mainSelection);
        }

        public bool MBOnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason, Vec3i offset) {
            var mainSelection = blockSel.Clone();
            mainSelection.Position += offset.AsBlockPos;
            return OnBlockInteractCancel(secondsUsed, world, byPlayer, mainSelection, cancelReason);
        }

        public ItemStack MBOnPickBlock(IWorldAccessor world, BlockPos pos, Vec3i offset) {
            return OnPickBlock(world, pos + offset.AsBlockPos);
        }

        public WorldInteraction[] MBGetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection blockSel, IPlayer forPlayer, Vec3i offset) {
            var mainSelection = blockSel.Clone();
            mainSelection.Position += offset.AsBlockPos;
            if (offsetInteractionYOffset == 0) {
                var offsetBlock = world.BlockAccessor.GetBlock(blockSel.Position);
                if (offsetBlock.Code != "air") {
                    offsetInteractionYOffset = InteractionHelpYOffset;
                    offsetBlock.InteractionHelpYOffset = InteractionHelpYOffset;
                }
            }
            return GetPlacedBlockInteractionHelp(world, mainSelection, forPlayer);
        }

        public BlockSounds MBGetSounds(IBlockAccessor blockAccessor, BlockSelection blockSel, ItemStack stack, Vec3i offset) {
            var mainSelection = blockSel.Clone();
            mainSelection.Position += offset.AsBlockPos;
            return GetSounds(blockAccessor, mainSelection, stack);
        }
    }

    enum WorkbenchSlots : int {
        MainBody = 0,
        CraftingSlot1 = 1,
        CraftingSlot2 = 2,
        CraftingSlot3 = 3,
        CraftingSlot4 = 4,
        CraftingSlot5 = 5,
        Vise = 6,
        ReforgeStaging = 7
    }
}
