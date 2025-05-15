using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Client;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Toolsmith.ToolTinkering.Items {
    public class ItemTinkerToolParts : Item, IModularPartRenderer {
        protected bool crafting = false;

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling) {
            var entPlayer = (byEntity as EntityPlayer);

            if (firstEvent && entPlayer != null && !entPlayer.Controls.ShiftKey && TinkeringUtility.ValidBindingInOffhand(byEntity)) { //Check for Handle in Offhand
                handling = EnumHandHandling.PreventDefault;
                if (byEntity.World.Side == EnumAppSide.Server) {
                    byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/messycraft.ogg"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z, null, true, 32f, 1f);
                }
                crafting = true;
                return;
            }

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) {
            if (crafting) {
                return (crafting && secondsUsed < ToolsmithConstants.TimeToCraftTinkerTool); //Time for crafting is now a constant variable!
            }

            return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) {
            if (crafting && secondsUsed >= (ToolsmithConstants.TimeToCraftTinkerTool - 0.1)) { //If they were crafting, verify that the countdown is up, and if so, craft it (if there still is a valid offhand handle!)
                if (byEntity.World.Side.IsServer() && TinkeringUtility.ValidBindingInOffhand(byEntity)) {
                    TinkeringUtility.AssembleFullTool(slot, byEntity, blockSel);
                }
                crafting = false;
                return;
            }

            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            var head = inSlot.Itemstack.GetToolheadForData();
            var handle = inSlot.Itemstack.GetToolhandleForData();

            if ( head != null && handle != null ) {
                dsc.AppendLine(Lang.Get("tinkertoolpartscontains", head.GetName(), handle.GetName()));
            } else {
                dsc.AppendLine("This Tinker Tool Part Bundle is lacking either a head or handle! Something went wrong, craft at your own risk.");
            }

            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }

        public ITreeAttribute InitializeRenderTree(ITreeAttribute tree, Item item) { //This probably won't get called, cause it's used for initializing the Creative stacks, and this is set to skip that. Keeping it here just in case though!
            return tree;
        }

        public void ResetRotationAndOffset(ItemStack stack) {
            return;
        }
    }
}
