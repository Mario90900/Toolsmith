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

namespace Toolsmith.ToolTinkering.Blocks {
    public class BlockWorkbench : Block {

        WorldInteraction[] interactions;
        Cuboidf[] workbenchSlots;

        public override void OnLoaded(ICoreAPI api) {
            base.OnLoaded(api);

            if (api.Side.IsServer()) {
                workbenchSlots = new Cuboidf[] { //Hmm. This doesn't actually register the selection boxes. Wierd. TODO - Gotta figure out what's wrong here tomorrow! Would be nice to add the Selection Box to the Vise specifically to take apart the tools...
                    new() { X1 = 0.4f, X2 = 0.6f, Y1 = 1, Y2 = 1.1f, Z1 = 0.6f, Z2 = 0.8f },
                    new() { X1 = 0.6f, X2 = 0.8f, Y1 = 1, Y2 = 1.1f, Z1 = 0.3f, Z2 = 0.5f },
                    new() { X1 = 0.8f, X2 = 1.0f, Y1 = 1, Y2 = 1.1f, Z1 = 0.6f, Z2 = 0.8f },
                    new() { X1 = 1.0f, X2 = 1.2f, Y1 = 1, Y2 = 1.1f, Z1 = 0.3f, Z2 = 0.5f },
                    new() { X1 = 1.2f, X2 = 1.4f, Y1 = 1, Y2 = 1.1f, Z1 = 0.6f, Z2 = 0.8f },
                    new() { X1 = 0.1f, X2 = 0.3f, Y1 = 1, Y2 = 1.2f, Z1 = 0.1f, Z2 = 0.3f }
                };

                SelectionBoxes.Append(workbenchSlots);
                return;
            }

            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(api, "workbenchInteractions", () => {
                List<ItemStack> tinkerableTools = new List<ItemStack>();
                List<ItemStack> toolHeads = new List<ItemStack>();

                foreach (Item i in api.World.Items) {
                    if (i.Code == null) {
                        continue;
                    }

                    if (i.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                        tinkerableTools.Add(new ItemStack(i)); //All tinkered tools can be deconstructed!
                    } else if (i.HasBehavior<CollectibleBehaviorToolHead>()) {
                        toolHeads.Add(new ItemStack(i)); //And all tool heads can be converted into a workpiece when you want to reforge.
                    }
                }

                return new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-workbench-reforge",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = toolHeads.ToArray()
                    },
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-workbench-deconstruct",
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
            BlockEntityWorkbench workbenchEnt = GetBlockEntity<BlockEntityWorkbench>(blockSel.Position);
            var entPlayer = byPlayer.Entity;
            if (SelectionBoxes.Length > 0) {
                ToolsmithModSystem.Logger.Warning("There are " + SelectionBoxes.Length + " selection boxes!");
            }
            if (!entPlayer.Controls.ShiftKey && workbenchEnt != null) {
                
            } else if (entPlayer.Controls.ShiftKey) {
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
                    if (world.Side.IsServer() && secondsUsed > 4.5) {
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
    }
}
