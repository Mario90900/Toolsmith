using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Toolsmith.ToolTinkering.Behaviors {
    public class CollectibleBehaviorOffhandDominantInteraction : CollectibleBehavior {

        public CollectibleBehaviorOffhandDominantInteraction(CollectibleObject collObj) : base(collObj) {

        }

        public void OnHeldOffhandDominantStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling) {
            var offhandSlot = byEntity.LeftHandItemSlot;
            byEntity.LeftHandItemSlot.Itemstack.Collectible.OnHeldInteractStart(offhandSlot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }

        public bool OnHeldOffhandDominantStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) {
            var offhandSlot = byEntity.LeftHandItemSlot;
            return byEntity.LeftHandItemSlot.Itemstack.Collectible.OnHeldInteractStep(secondsUsed, offhandSlot, byEntity, blockSel, entitySel);
        }

        public void OnHeldOffhandDominantStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) {
            var offhandSlot = byEntity.LeftHandItemSlot;
            byEntity.LeftHandItemSlot.Itemstack.Collectible.OnHeldInteractStop(secondsUsed, offhandSlot, byEntity, blockSel, entitySel);
        }

        public bool OnHeldOffhandDominantCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason) {
            var offhandSlot = byEntity.LeftHandItemSlot;
            return byEntity.LeftHandItemSlot.Itemstack.Collectible.OnHeldInteractCancel(secondsUsed, offhandSlot, byEntity, blockSel, entitySel, cancelReason);
        }

        //The job of this method is solely to relay this call through to the item itself.
        //This returns false if it should steal the call, and true if it should let it keep going. Same is true for HasOffhandInteractionAvailable
        public bool AskItemForHasInteractionAvailable(ItemSlot offhandSlot, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent = false) {
            var offhandItem = offhandSlot.Itemstack?.Item != null ? offhandSlot.Itemstack?.Item as IOffhandDominantInteractionItem : null;
            if (offhandItem != null) {
                return offhandItem.HasOffhandInteractionAvailable(slot, byEntity, blockSel, entitySel, firstEvent);
            }

            return true;
        }
    }
}
