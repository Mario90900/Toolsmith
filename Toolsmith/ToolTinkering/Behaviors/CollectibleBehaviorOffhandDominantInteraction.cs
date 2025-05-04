using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

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
    }
}
