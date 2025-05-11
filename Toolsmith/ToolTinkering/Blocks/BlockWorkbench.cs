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
        WorldInteraction[] emptyCraftingSlotsInteraction;
        WorldInteraction[] emptyReforgeStagingInteraction;
        WorldInteraction[] fullSlotGetItemInteraction;
        WorldInteraction[] fullReforgeStagingInteraction;
        WorldInteraction[] fullSlotCraftingInteraction;
        Cuboidf[] offsetHalfSelections;

        public override void OnLoaded(ICoreAPI api) {
            base.OnLoaded(api);

            if (api.Side.IsServer()) {
                return;
            }
            
            ICoreClientAPI capi = api as ICoreClientAPI;

            viseInteractions = ObjectCacheUtil.GetOrCreate(capi, "workbenchViseInteraction", () => {
                List<ItemStack> tinkerableTools = new List<ItemStack>();

                foreach (Item i in capi.World.Items) {
                    if (i.Code == null) {
                        continue;
                    }

                    if (i.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                        tinkerableTools.Add(new ItemStack(i)); //All tinkered tools can be deconstructed!
                    }
                }

                return new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-workbench-deconstruct",
                        HotKeyCode = "shift",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = tinkerableTools.ToArray()
                    }
                };
            });

            emptyCraftingSlotsInteraction = ObjectCacheUtil.GetOrCreate(capi, "workbenchEmptyCraftingSlotsInteraction", () => {
                List<ItemStack> toolParts = new List<ItemStack>();

                foreach (Item i in capi.World.Items) {
                    if (i.Code == null) {
                        continue;
                    }

                    if (i.HasBehavior<CollectibleBehaviorToolHead>() || i.HasBehavior<CollectibleBehaviorToolHandle>() || i.HasBehavior<CollectibleBehaviorToolBinding>()) {
                        toolParts.Add(new ItemStack(i)); //Contains all the possible parts that can be crafting with on the bench! Will need to expand this when adding new parts.
                    }
                }

                return new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-workbench-craftingspot",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = toolParts.ToArray()
                    }
                };
            });

            emptyReforgeStagingInteraction = ObjectCacheUtil.GetOrCreate(capi, "workbenchEmptyReforgeStagingInteraction", () => {
                List<ItemStack> reforgables = new List<ItemStack>();

                foreach (Item i in capi.World.Items) {
                    if (i.Code == null) {
                        continue;
                    }
                    if ((i.HasBehavior<CollectibleBehaviorToolHead>() || i.HasBehavior<CollectibleBehaviorSmithedTools>()) && i.IsCraftableMetal()) {
                        reforgables.Add(new ItemStack(i)); //And all tool heads can be converted into a workpiece when you want to reforge.
                    }
                }

                return new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-workbench-reforge",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = reforgables.ToArray()
                    }
                };
            });

            List<ItemStack> hammers = new List<ItemStack>();

            foreach (Item i in capi.World.Items) {
                if (i.Code == null) {
                    continue;
                }

                if (i.Tool == EnumTool.Hammer) {
                    hammers.Add(new ItemStack(i));
                }
            }

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
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
            BlockEntityWorkbench beWorkbench = GetBlockEntity<BlockEntityWorkbench>(selection.Position);
            if (beWorkbench == null) {
                return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
            }

            var slotIndex = selection.SelectionBoxIndex;
            if (slotIndex >= (int)WorkbenchSlots.CraftingSlot1 && slotIndex <= (int)WorkbenchSlots.CraftingSlot5) {
                if (beWorkbench.IsSelectSlotEmpty(slotIndex)) {
                    return emptyCraftingSlotsInteraction.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
                } else {
                    return fullSlotGetItemInteraction.Append(fullSlotCraftingInteraction.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer))); //Don't forget to update this when the Workbench can craft tools!
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
                            }
                            workbenchEnt.MarkDirty(redrawOnClient: true);
                            return true;
                        }
                    } else {
                        if (TryPlaceOrGetItemCraftingSlots(world, byPlayer, blockSel, workbenchEnt)) {
                            if (world.Side.IsServer()) {
                                world.PlaySoundAt(new AssetLocation("sounds/player/build"), entPlayer.Pos.X, entPlayer.Pos.Y, entPlayer.Pos.Z, null, true, 32f, 1f);
                            }
                            workbenchEnt.MarkDirty(redrawOnClient: true);
                            return true;
                        }
                    }
                } else if (blockSel.SelectionBoxIndex == (int)WorkbenchSlots.ReforgeStaging) { //Player is attempting to place something in the Reforging spot!
                    if (byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible?.Tool == EnumTool.Hammer) {
                        if (AttemptReforgingToolHead(world, byPlayer, blockSel, workbenchEnt)) {
                            if (world.Side.IsServer()) {
                                world.PlaySoundAt(new AssetLocation("sounds/block/meteoriciron-hit-pickaxe"), entPlayer.Pos.X, entPlayer.Pos.Y, entPlayer.Pos.Z, null, true, 32f, 1f);
                            }
                            return true;
                        }
                    } else /*if (byPlayer.InventoryManager.ActiveHotbarSlot != null)*/ {
                        if (TryPlaceOrGetItemReforgeSlot(world, byPlayer, blockSel, workbenchEnt)) {
                            if (world.Side.IsServer()) {
                                world.PlaySoundAt(new AssetLocation("sounds/player/build"), entPlayer.Pos.X, entPlayer.Pos.Y, entPlayer.Pos.Z, null, true, 32f, 1f);
                            }
                            workbenchEnt.MarkDirty(redrawOnClient: true);
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
                if (byPlayer.Entity.Controls.ShiftKey && TinkeringUtility.IsDeconstructableTool(byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack.Collectible, world)) {
                    if (world.Side.IsServer() && blockSel.SelectionBoxIndex == (int)WorkbenchSlots.Vise && secondsUsed > 4.5) {
                        TinkeringUtility.DisassembleTool(secondsUsed, world, byPlayer, blockSel);
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
                if (TinkeringUtility.IsAnyToolPart(stack.Collectible, world) || ReforgingUtility.IsPossibleMergeItem(stack, world)) {
                    return bench.TryGetOrPutItemOnWorkbench(blockSel.SelectionBoxIndex, byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer, world);
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
                if ((stack.Collectible.HasBehavior<CollectibleBehaviorToolHead>() || stack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>()) && stack.Collectible.IsCraftableMetal()) {
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
            return base.GetSelectionBoxes(blockAccessor, pos);
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

            if (blockAccessor.GetBlockEntity(pos) is BlockEntityWorkbench) {
                return SelectionBoxes;
            } else {
                return offsetHalfSelections;
            }
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
