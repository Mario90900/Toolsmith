using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Toolsmith.Utils;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;
using Toolsmith.ToolTinkering.Behaviors;

namespace Toolsmith.ToolTinkering.Blocks {
    public class BlockGrindstone : Block {

        protected bool doneSharpening = false;
        protected bool firstHoning = false;
        protected float deltaLastTick = 0;
        protected float lastInterval = 0;
        protected float totalSharpnessHoned = 0;
        protected float repairInterval = 0.4f;

        WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api) {
            base.OnLoaded(api);

            if (api.Side.IsServer()) {
                return;
            }

            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(capi, "grindstoneInteractions", () => {
                List<ItemStack> honeables = new List<ItemStack>();
                List<ItemStack> tinkerableTools = new List<ItemStack>();

                foreach (Item i in capi.World.Items) {
                    if (i.Code == null) {
                        continue;
                    }

                    if (i.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                        if (!i.HasBehavior<CollectibleBehaviorToolBlunt>()) { //If it's a tinkered tool and it's not blunt, it can be honed.
                            honeables.Add(new ItemStack(i));
                        }
                        tinkerableTools.Add(new ItemStack(i)); //All tinkered tools can be deconstructed!
                        continue;
                    } else if (i.HasBehavior<CollectibleBehaviorSmithedTools>() && !i.HasBehavior<CollectibleBehaviorToolBlunt>()) { //If it's a smithed tool and not blunt, it can be honed.
                        honeables.Add(new ItemStack(i));
                        continue;
                    } else if (i.HasBehavior<CollectibleBehaviorToolHead>() && !i.HasBehavior<CollectibleBehaviorToolBlunt>()) {
                        honeables.Add(new ItemStack(i));
                        continue;
                    }
                }

                return new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-grindstone-hone",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = honeables.ToArray()
                    },
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-grindstone-deconstruct",
                        HotKeyCode = "shift",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = tinkerableTools.ToArray()
                    }
                };
            });
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            BlockEntityGrindstone grindstoneEnt = GetBlockEntity<BlockEntityGrindstone>(blockSel.Position);
            var entPlayer = byPlayer.Entity;
            if (!entPlayer.Controls.ShiftKey && grindstoneEnt != null) {
                grindstoneEnt.OnBlockInteractStart();
            } else if (entPlayer.Controls.ShiftKey) {
                if (byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack != null) {
                    if (world.Side == EnumAppSide.Server && TinkeringUtility.IsDeconstructableTool(byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack.Collectible, world)) {
                        world.PlaySoundAt(new AssetLocation("sounds/player/messycraft.ogg"), entPlayer.Pos.X, entPlayer.Pos.Y, entPlayer.Pos.Z, null, true, 32f, 1f);
                    }
                }
            }
            return true;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            if (byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack != null && TinkeringUtility.ToolOrHeadNeedsSharpening(byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack, world, byPlayer.Entity)) { //Make sure the slot isn't empty
                int isTool = TinkeringUtility.IsValidSharpenTool(byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack.Collectible, world);
                BlockEntityGrindstone grindstoneEnt = GetBlockEntity<BlockEntityGrindstone>(blockSel.Position);
                if (grindstoneEnt != null && !byPlayer.Entity.Controls.ShiftKey && isTool > 0) {
                    grindstoneEnt.ToggleHoningSound(true);
                }
                if (world.Side.IsServer() && !byPlayer.Entity.Controls.ShiftKey && isTool > 0) { //Check if it's a valid tool for repair, is made of metal and has one of the 2 behaviors, if so...
                    ItemStack item = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;

                    deltaLastTick = secondsUsed - lastInterval;
                    if (deltaLastTick >= repairInterval) { //Try not to repair EVERY single tick to space it out some. Cause of this, repair 5 durability each time so it doesn't take forever.
                        int curDur = 0;
                        int maxDur = 0;
                        int curSharp = 0;
                        int maxSharp = 0;
                        firstHoning = !(item.HasTotalHoneValue());

                        TinkeringUtility.RecieveDurabilitiesAndSharpness(ref curDur, ref maxDur, ref curSharp, ref maxSharp, ref totalSharpnessHoned, item, isTool);

                        TinkeringUtility.ActualSharpenTick(ref curDur, ref curSharp, maxSharp, ref totalSharpnessHoned, firstHoning, byPlayer.Entity);

                        if (ToolsmithModSystem.Config.DebugMessages) {
                            ToolsmithModSystem.Logger.Warning("Total Sharpness Percent recovered this action: " + totalSharpnessHoned);
                            ToolsmithModSystem.Logger.Warning("Seconds the Grindstone has been going: " + secondsUsed);
                        }

                        TinkeringUtility.SetResultsOfSharpening(curDur, curSharp, totalSharpnessHoned, firstHoning, item, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot, isTool);

                        byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();

                        deltaLastTick = 0;
                        lastInterval = MathUtility.FloorToNearestMult(secondsUsed, repairInterval);

                        if (secondsUsed > 300 || !TinkeringUtility.ToolOrHeadNeedsSharpening(item, world)) { //Just in case a way to break out if someone's been holding down the repair for over a set time, so nothing gets too overloaded, or the tool is done repairing!
                            doneSharpening = true;
                        }
                    }
                } else if (byPlayer.Entity.Controls.ShiftKey && TinkeringUtility.IsDeconstructableTool(byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack.Collectible, world)) {
                    if (world.Side.IsServer() && secondsUsed > 4.5) {
                        TinkeringUtility.DisassembleTool(secondsUsed, world, byPlayer, blockSel);
                    }
                }
            }

            if (doneSharpening) {
                world.PlaySoundAt(new AssetLocation("toolsmith:sounds/honing-finish.ogg"), blockSel.Position, 0, randomizePitch: false);
                return false;
            }

            return true;
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason) {
            if (ToolsmithModSystem.Config.AccessibilityDisableNeedToHoldClick && byPlayer.Entity.Controls.ShiftKey) {
                return false;
            }

            BlockEntityGrindstone grindstoneEnt = GetBlockEntity<BlockEntityGrindstone>(blockSel.Position);
            if (grindstoneEnt != null) {
                grindstoneEnt.OnBlockInteractStop();
                grindstoneEnt.ToggleHoningSound(false);
            }

            deltaLastTick = 0; //Make sure to reset these any time it's canceled
            lastInterval = 0;
            totalSharpnessHoned = 0;
            doneSharpening = false;

            return true;
        }

        //This is only hit if it actually times out from someone holding it for too long - or the tool/head is finished repairing
        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            BlockEntityGrindstone grindstoneEnt = GetBlockEntity<BlockEntityGrindstone>(blockSel.Position);
            if (grindstoneEnt != null) {
                grindstoneEnt.OnBlockInteractStop();
                grindstoneEnt.ToggleHoningSound(false);
            }

            deltaLastTick = 0; //Make sure to reset these any time it's canceled
            lastInterval = 0;
            totalSharpnessHoned = 0;
            doneSharpening = false;
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode) {
            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak)) {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return false;
            }

            if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) {
                BlockFacing[] horVer = SuggestedHVOrientation(byPlayer, blockSel);
                Block orientedBlock = world.BlockAccessor.GetBlock(CodeWithParts(horVer[0].Code));
                orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
                return true;
            }
            return false;
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
            return new ItemStack[] { new ItemStack(world.BlockAccessor.GetBlock(CodeWithParts("north"))) };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos) {
            return new ItemStack(world.BlockAccessor.GetBlock(CodeWithParts("north")));
        }
    }
}
