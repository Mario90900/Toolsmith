using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Toolsmith.ToolTinkering.Items {
    public class ItemTinkerToolParts : Item {
        protected bool crafting = false;

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling) {
            if (TinkeringUtility.ValidBindingInOffhand(byEntity)) { //Check for Handle in Offhand
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
            var head = inSlot.Itemstack.GetToolhead();
            var handle = inSlot.Itemstack.GetToolhandle();
            dsc.AppendLine(Lang.Get("tinkertoolpartscontains", head.GetName(), handle.GetName()));

            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }
    }
}
