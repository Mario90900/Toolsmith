using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.Server;

namespace Toolsmith.ToolTinkering.Behaviors {
    public class CollectibleBehaviorToolHead : CollectibleBehaviorToolPartWithHealth {

        //private bool crafting = false;

        public CollectibleBehaviorToolHead(CollectibleObject collObj) : base(collObj) {

        }

        /*public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity, ref EnumHandling bhHandling) {
            if (crafting == true) {
                bhHandling = EnumHandling.PreventSubsequent;
                return "crafting";
            }
            
            return null;
        }*/ //Could this be what actually shows any animation for other players on a server? And the reason it was failing to sync the animation end was cause of this? Perhaps as is with this commented out, it will not display the animation for other players. Will need to test.
        
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling) { //Handle the grinding code here as well as the tool itself! Probably can offload the core interaction to a helper utility function?
            var entPlayer = (byEntity as EntityPlayer);

            if (TinkeringUtility.ValidHandleInOffhand(byEntity)) { //Check for Handle in Offhand
                handHandling = EnumHandHandling.PreventDefault;
                handling = EnumHandling.PreventSubsequent;
                if (byEntity.World.Side == EnumAppSide.Server) {
                    byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/messycraft.ogg"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z, null, true, 32f, 1f);
                }
                byEntity.StartAnimation("crafting");
                slot.Itemstack.SetPartBeingCrafted();
                return;
            }

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling) {
            var crafting = slot.Itemstack.PartBeingCrafted();
            if (crafting) {
                handling = EnumHandling.PreventSubsequent;
                return crafting && secondsUsed < ToolsmithConstants.TimeToCraftTinkerTool; //Time for crafting is now a constant variable!
            }

            return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling) {
            if (slot.Itemstack.PartBeingCrafted() && secondsUsed >= ToolsmithConstants.TimeToCraftTinkerTool - 0.1) { //If they were crafting, verify that the countdown is up, and if so, craft it (if there still is a valid offhand handle!)
                handling = EnumHandling.PreventDefault;
                if (byEntity.World.Side.IsServer() && TinkeringUtility.ValidHandleInOffhand(byEntity)) {
                    TinkeringUtility.AssemblePartBundle(slot, byEntity, blockSel);
                }
                byEntity.StopAnimation("crafting");
                slot.Itemstack.ClearPartBeingCrafted();
                return;
            }

            byEntity.StopAnimation("crafting");
            slot.Itemstack.ClearPartBeingCrafted();
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handled) {
            if (slot.Itemstack.PartBeingCrafted()) {
                if (ToolsmithModSystem.Config.AccessibilityDisableNeedToHoldClick) {
                    handled = EnumHandling.PreventSubsequent;
                    return false;
                } else if (secondsUsed >= ToolsmithConstants.TimeToCraftTinkerTool - 0.1) {
                    handled = EnumHandling.PreventSubsequent;
                    if (byEntity.World.Side.IsServer() && TinkeringUtility.ValidHandleInOffhand(byEntity)) {
                        TinkeringUtility.AssemblePartBundle(slot, byEntity, blockSel);
                    }
                }
            }

            byEntity.StopAnimation("crafting");
            slot.Itemstack.ClearPartBeingCrafted();
            return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason, ref handled);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            dsc.AppendLine(Lang.Get("toolheaddirections"));
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }

        public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe, ref EnumHandling bhHandling) { //TODO - This isn't called when a tool head is smithed. Buh! Have to move it elsewhere, or otherwise get this called.
            //This is still possibly important if somehow someone crafts a Tool Head.

            bool isToolMetal = outputSlot.Itemstack.Collectible.IsCraftableMetal();
            int baseDur = 1000; //Since we don't know the actual base durability YET for the tool, until it is crafted. So this is a placeholder.
            int partDur = (int)(baseDur * ToolsmithModSystem.Config.HeadDurabilityMult);
            int sharpness = (int)(baseDur * ToolsmithModSystem.Config.SharpnessMult);
            int startingSharpness;
            if (isToolMetal) {
                startingSharpness = (int)(sharpness * ToolsmithConstants.StartingSharpnessMult);
            } else {
                startingSharpness = (int)(sharpness * ToolsmithConstants.NonMetalStartingSharpnessMult);
            }

            outputSlot.Itemstack.SetPartCurrentDurability(partDur);
            outputSlot.Itemstack.SetPartMaxDurability(partDur);
            outputSlot.Itemstack.SetToolCurrentSharpness(startingSharpness);
            outputSlot.Itemstack.SetToolMaxSharpness(sharpness);

            if (ToolsmithModSystem.Config.DebugMessages) {
                ToolsmithModSystem.Logger.Debug("The starting Sharpness is: " + startingSharpness);
                ToolsmithModSystem.Logger.Debug("Finally the max Sharpness is: " + sharpness);
            }
        }
    }
}
