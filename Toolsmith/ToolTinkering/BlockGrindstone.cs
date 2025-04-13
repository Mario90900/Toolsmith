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

namespace Toolsmith.ToolTinkering {
    public class BlockGrindstone : Block {

        protected bool doneSharpening = false;
        protected float deltaLastTick = 0;
        protected float lastInterval = 0;
        protected float totalSharpnessHoned = 0;
        protected float repairInterval = 0.4f;

        public override void OnLoaded(ICoreAPI api) {
            base.OnLoaded(api);

            if (api.Side.IsClient()) {
                ICoreClientAPI capi = api as ICoreClientAPI;
            }
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            BlockEntityGrindstone grindstoneEnt = GetBlockEntity<BlockEntityGrindstone>(blockSel.Position);
            var entPlayer = byPlayer.Entity;
            if (!entPlayer.Controls.ShiftKey && grindstoneEnt != null) {
                grindstoneEnt.OnBlockInteractStart();
            } else if (entPlayer.Controls.ShiftKey) {
                if (byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack != null) {
                    int isTool = IsValidRepairTool(byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack.Collectible, world);
                    if (world.Side == EnumAppSide.Server && isTool == 1) {
                        world.PlaySoundAt(new AssetLocation("sounds/player/messycraft.ogg"), entPlayer.Pos.X, entPlayer.Pos.Y, entPlayer.Pos.Z, null, true, 32f, 1f);
                    }
                }
            }
            return true;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            if (byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack != null) { //Make sure the slot isn't empty
                int isTool = IsValidRepairTool(byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack.Collectible, world);
                if (world.Side.IsServer() && !byPlayer.Entity.Controls.ShiftKey && isTool > 0) { //Check if it's a valid tool for repair, is made of metal and has one of the 2 behaviors, if so...
                    ItemStack item = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;

                    deltaLastTick = secondsUsed - lastInterval;
                    if (deltaLastTick >= repairInterval) { //Try not to repair EVERY single tick to space it out some. Cause of this, repair 5 durability each time so it doesn't take forever.
                        int curDur;
                        int maxDur;
                        int curSharp;
                        int maxSharp;

                        if (isTool == 1) { //The item is a Tinkered Tool! Use the extensions for the tool's head durability.
                            curDur = item.GetToolheadCurrentDurability();
                            maxDur = item.GetToolheadMaxDurability();
                            if (item.HasPlaceholderHead()) { //If the tool still has no proper head item saved to it, something went wrong and an error should have been printed.
                                return false;
                            }
                            var handleDur = item.GetToolhandleCurrentDurability(); //This is mostly just being called to test that the tools are fully initialized.
                            var bindingDur = item.GetToolbindingCurrentDurability(); //^^^
                            curSharp = item.GetToolCurrentSharpness();
                            maxSharp = item.GetToolMaxSharpness();
                        } else if (isTool == 2) { //The item is a Smithed Tool! Instead use the default base durability system.
                            curDur = item.Collectible.GetRemainingDurability(item);
                            maxDur = item.Collectible.GetMaxDurability(item);
                            curSharp = item.GetToolCurrentSharpness();
                            maxSharp = item.GetToolMaxSharpness();
                        } else { //The item is just a Tool Head, not on a tool put together. Use the extensions for Part Durability.
                            curDur = item.GetPartCurrentDurability();
                            maxDur = item.GetPartMaxDurability();
                            curSharp = item.GetPartCurrentSharpness();
                            maxSharp = item.GetPartMaxSharpness();
                        }

                        if (curSharp < maxSharp && curDur > 0) {
                            float percent = 1.0f;
                            if (ToolsmithModSystem.Config.GrindstoneSharpenPerTick >= 1 && ToolsmithModSystem.Config.GrindstoneSharpenPerTick <= 100) {
                                percent = ((float)ToolsmithModSystem.Config.GrindstoneSharpenPerTick / 100f);
                            }

                            int percentSharpen = (int)(percent * maxSharp);
                            curSharp += percentSharpen;
                            if (curSharp >= maxSharp) {
                                curSharp = maxSharp;
                                doneSharpening = true;
                            } else {
                                world.PlaySoundAt(new AssetLocation("sounds/block/anvil1.ogg"), blockSel.Position, 0);
                                totalSharpnessHoned += percent;
                            }

                            bool damageDurability = true;
                            if (totalSharpnessHoned > 0.66) {
                                damageDurability = (world.Rand.NextDouble() <= 0.05f);
                            } else if (totalSharpnessHoned > 0.33) {
                                damageDurability = MathUtility.ShouldDamageFromSharpening(world, totalSharpnessHoned);
                            }

                            if (damageDurability) {
                                curDur -= percentSharpen;
                            }
                            ToolsmithModSystem.Logger.Warning("Total Sharpness Percent recovered this action: " + totalSharpnessHoned);
                            ToolsmithModSystem.Logger.Warning("Seconds the Grindstone has been going: " + secondsUsed);
                        }

                        if (isTool == 1) {
                            item.SetToolheadCurrentDurability(curDur);
                            item.SetToolCurrentSharpness(curSharp);
                            if (curDur <= 0) {
                                CollectibleObject toolObj = item.Collectible;
                                TinkeringUtility.HandleBrokenTinkeredTool(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot, 0, curSharp, item.GetToolhandleCurrentDurability(), item.GetToolbindingCurrentDurability(), true, false);
                                item.SetBrokeWhileSharpeningFlag();
                                toolObj.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot);
                                if (item != null) {
                                    item.ClearBrokeWhileSharpeningFlag();
                                }
                            }
                        } else if (isTool == 2) {
                            item.SetSmithedDurability(curDur);
                            item.SetToolCurrentSharpness(curSharp);
                            if (curDur <= 0) {
                                item.SetSmithedDurability(1);
                                item.SetBrokeWhileSharpeningFlag();
                                item.Collectible.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot);
                                if (item != null) {
                                    item.ClearBrokeWhileSharpeningFlag();
                                }
                            }
                        } else {
                            item.SetPartCurrentDurability(curDur);
                            item.SetPartCurrentSharpness(curSharp);
                            if (curDur <= 0) {
                                //Wait... This might be silly and may be hacky but... Can I just make it into an item AND break it right here right now? Lmao
                                CollectibleObject toolToBreakObj; //This might proc other mod's on damage stuff for the head as if it were a real tool.
                                var success = RecipeRegisterModSystem.TinkerToolGridRecipes.TryGetValue(item.Collectible.Code.ToString(), out toolToBreakObj);
                                if (success) {
                                    ItemStack toolToBreak = new ItemStack(world.GetItem(toolToBreakObj.Code), 1);
                                    var toolType = IsValidRepairTool(toolToBreak.Collectible, world);
                                    if (toolType == 1) { //Just to make sure it isn't on the ignore list already, but it should only come back as a Tinkered Tool.
                                        //Initiate that JUST to insure it breaks now! Haha!
                                        item = null;
                                        byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack = toolToBreak.Clone();
                                        byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Attributes.SetInt(ToolsmithAttributes.Durability, 1);
                                        byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.SetBrokeWhileSharpeningFlag();
                                        byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible.DamageItem(world, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot);
                                        if (!byPlayer.InventoryManager.ActiveHotbarSlot.Empty) {
                                            byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.ClearBrokeWhileSharpeningFlag();
                                            byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack = null;
                                        }
                                    } else { //Just in case, if all else fails, just destroy the head. But man I hope this works, haha.
                                        item = null;
                                    }
                                }
                            }
                        }
                        byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();

                        deltaLastTick = 0;
                        lastInterval = FloorToNearestMult(secondsUsed, repairInterval);

                        if (secondsUsed > 300) { //Just in case a way to break out if someone's been holding down the repair for over a set time, so nothing gets too overloaded, or the tool is done repairing!
                            doneSharpening = true;
                        }
                    }
                } else if (byPlayer.Entity.Controls.ShiftKey && isTool == 1) {
                    if (world.Side.IsServer() && secondsUsed > 4.5) {
                        DisassembleTool(secondsUsed, world, byPlayer, blockSel);
                    }
                }
            }

            if (doneSharpening) {
                world.PlaySoundAt(new AssetLocation("sounds/block/hoppertumble.ogg"), blockSel.Position, 0);
                return false;
            }

            return true;
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason) {
            BlockEntityGrindstone grindstoneEnt = GetBlockEntity<BlockEntityGrindstone>(blockSel.Position);
            if (grindstoneEnt != null) {
                grindstoneEnt.OnBlockInteractStop();
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
            }

            deltaLastTick = 0; //Make sure to reset these any time it's canceled
            lastInterval = 0;
            totalSharpnessHoned = 0;
            doneSharpening = false;
        }

        //This checks if it is a valid repair tool as well as if it is a fully tinkered tool or if it is just a tool's head, since the durabilities are stored under different attributes
        private int IsValidRepairTool(CollectibleObject item, IWorldAccessor world) {
            if (world.Side.IsServer() && ToolsmithModSystem.IgnoreCodes.Count > 0 && ToolsmithModSystem.IgnoreCodes.Contains(item.Code.ToString())) { //First check if the ignore list has any entries, and ensure this one isn't on it. Likely means something got improperly given the Behavior on init.
                return 0;
            } else if (item.HasBehavior<CollectibleBehaviorTinkeredTools>()) { //This one stores it under 'tinkeredToolHead' durability
                if (!item.HasBehavior<CollectibleBehaviorToolBlunt>() && item.IsCraftableMetal()) {
                    return 1;
                }
            } else if (item.HasBehavior<CollectibleBehaviorSmithedTools>()) { //And this one just uses the regular durability values since it's just a single solid tool, no parts
                if (!item.HasBehavior<CollectibleBehaviorToolBlunt>() && item.IsCraftableMetal()) {
                    return 2;
                }
            } else if (item.HasBehavior<CollectibleBehaviorToolHead>()) { //While this stores it as just 'toolPartDurability', since not every part will be a head, but every head will have this behavior
                if (!item.HasBehavior<CollectibleBehaviorToolBlunt>() && item.IsCraftableMetal()) {
                    return 3;
                }
            }

            return 0;
        }

        private void DisassembleTool(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            //Actually take apart the tool here!
            //Get the parts of the tool from it
            var tool = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            var head = tool.GetToolhead();
            var handle = tool.GetToolhandle();
            var binding = tool.GetToolbinding(); //Might be null if there is no binding.

            //Check if the Binding is null, if not, see if it is still at full durability - otherwise don't return it.
            if (binding != null) {
                if (tool.GetToolbindingCurrentDurability() != tool.GetToolbindingMaxDurability()) {
                    binding = null; //If it's null, no binding drop!
                }
            }

            //For both the Head and Handle, set the part durabilities (and sharpness for head!)
            head.SetPartCurrentDurability(tool.GetToolheadCurrentDurability());
            head.SetPartMaxDurability(tool.GetToolheadMaxDurability());
            head.SetPartCurrentSharpness(tool.GetToolCurrentSharpness());
            head.SetPartMaxSharpness(tool.GetToolMaxSharpness());
            handle.SetPartCurrentDurability(tool.GetToolhandleCurrentDurability());
            handle.SetPartMaxDurability(tool.GetToolhandleMaxDurability());

            //Return it all to the player, and get rid of the tool.
            bool gaveHead = false;
            bool gaveHandle = false;
            bool gaveBinding = false;
            var player = byPlayer.Entity;

            if (player != null) {
                gaveHead = player.TryGiveItemStack(head);
                gaveHandle = player.TryGiveItemStack(handle);
                if (binding != null) {
                    gaveBinding = player.TryGiveItemStack(binding);
                }
            }

            byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack = null;

            //If no room in inventory, drop in world instead.
            if (!gaveHead) {
                player.World.SpawnItemEntity(head, player.Pos.XYZ);
            }
            if (!gaveHandle) {
                player.World.SpawnItemEntity(handle, player.Pos.XYZ);
            }
            if (binding != null && !gaveBinding) {
                player.World.SpawnItemEntity(binding, player.Pos.XYZ);
            }

            byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
        }

        private float FloorToNearestMult(float secondsUsed, float mult) {
            return MathF.Floor(secondsUsed / mult) * mult;
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
