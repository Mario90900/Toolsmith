using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.ToolTinkering.Behaviors;
using Toolsmith.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace Toolsmith.ToolTinkering.Blocks {
    public class BlockWorkbench : Block, IMultiBlockColSelBoxes, IMultiBlockInteract {

        WorldInteraction[] viseInteractions;
        WorldInteraction[] craftingSlotsInteraction;
        WorldInteraction[] reforgeStagingInteraction;
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

            craftingSlotsInteraction = ObjectCacheUtil.GetOrCreate(capi, "workbenchCraftingSlotsInteraction", () => {
                List<ItemStack> toolParts = new List<ItemStack>();

                foreach (Item i in capi.World.Items) {
                    if (i.Code == null) {
                        continue;
                    }

                    if (i.HasBehavior<CollectibleBehaviorToolHead>() || i.HasBehavior<CollectibleBehaviorToolHandle>() || i.HasBehavior<CollectibleBehaviorToolBinding>()) {
                        toolParts.Add(new ItemStack(i)); //Contains all the possible parts that can be crafting with on the bench! Will need to expand this when adding new parts.
                    }

                    return new WorldInteraction[] {
                        new WorldInteraction() {
                            ActionLangCode = "blockhelp-workbench-craftingspot",
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = toolParts.ToArray()
                        }
                    };
                }
            });

            reforgeStagingInteraction = ObjectCacheUtil.GetOrCreate(capi, "workbenchReforgeStagingInteraction", () => {
                List<ItemStack> toolHeads = new List<ItemStack>();

                foreach (Item i in capi.World.Items) {
                    if (i.Code == null) {
                        continue;
                    }

                    if (i.HasBehavior<CollectibleBehaviorToolHead>()) {
                        toolHeads.Add(new ItemStack(i)); //And all tool heads can be converted into a workpiece when you want to reforge.
                    }
                }

                return new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-workbench-reforge",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = toolHeads.ToArray()
                    }
                };
            });
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
            if (selection.SelectionBoxIndex >= (int)WorkbenchSlots.CraftingSlot1 && selection.SelectionBoxIndex <= (int)WorkbenchSlots.CraftingSlot5) {
                return craftingSlotsInteraction.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
            } else if (selection.SelectionBoxIndex == (int)WorkbenchSlots.Vise) {
                return viseInteractions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
            } else if (selection.SelectionBoxIndex == (int)WorkbenchSlots.ReforgeStaging) {
                return reforgeStagingInteraction.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
            }

            return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            BlockEntityWorkbench workbenchEnt = GetBlockEntity<BlockEntityWorkbench>(blockSel.Position);
            var entPlayer = byPlayer.Entity;
            if (SelectionBoxes.Length > 0) {
                ToolsmithModSystem.Logger.Warning("Looking at Selection Box " + blockSel.SelectionBoxIndex);
            }
            if (!entPlayer.Controls.ShiftKey && workbenchEnt != null) {
                
            } else if (entPlayer.Controls.ShiftKey && blockSel.SelectionBoxIndex == (int)WorkbenchSlots.Vise) {
                if (byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack != null) {
                    int isTool = TinkeringUtility.IsValidSharpenTool(byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack.Collectible, world);
                    if (world.Side == EnumAppSide.Server && isTool == 1) {
                        world.PlaySoundAt(new AssetLocation("sounds/player/messycraft.ogg"), entPlayer.Pos.X, entPlayer.Pos.Y, entPlayer.Pos.Z, null, true, 32f, 1f);
                        return true;
                    }
                }
            }

            return false;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            if (byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack != null) { //Make sure the slot isn't empty
                int isTool = TinkeringUtility.IsValidSharpenTool(byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack.Collectible, world);
                if (byPlayer.Entity.Controls.ShiftKey && isTool == 1) {
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


            return true;
        }

        //This is only hit if it actually times out from someone holding it for too long - or the tool/head is finished repairing
        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            
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
