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
            /*if (byEntity == null || byEntity.LeftHandItemSlot?.Empty != true) {
                return;
            }*/

            byEntity.LeftHandItemSlot.Itemstack.Collectible.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);

            /*EnumHandHandling bhHandHandling = EnumHandHandling.NotHandled;
            bool preventDefault = false;

            foreach (CollectibleBehavior behavior in byEntity.LeftHandItemSlot.Itemstack.Collectible.CollectibleBehaviors) {
                EnumHandling bhHandling = EnumHandling.PassThrough;

                behavior.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref bhHandHandling, ref bhHandling);
                if (bhHandling != EnumHandling.PassThrough) {
                    handling = bhHandHandling;
                    preventDefault = true;
                }

                if (bhHandling == EnumHandling.PreventSubsequent) return;
            }

            if (!preventDefault) {
                handling = bhHandHandling;
            }*/
        }

        public bool OnHeldOffhandDominantStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) {
            /*if (byEntity == null || byEntity.LeftHandItemSlot?.Empty != true) {
                return true;
            }*/

            return byEntity.LeftHandItemSlot.Itemstack.Collectible.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);

            /*bool result = true;
            bool preventDefault = false;

            foreach (CollectibleBehavior behavior in byEntity.LeftHandItemSlot.Itemstack.Collectible.CollectibleBehaviors) {
                EnumHandling handled = EnumHandling.PassThrough;

                bool behaviorResult = behavior.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handled);
                if (handled != EnumHandling.PassThrough) {
                    result &= behaviorResult;
                    preventDefault = true;
                }

                if (handled == EnumHandling.PreventSubsequent) return result;
            }

            if (preventDefault) return result;

            return true;*/
        }

        public void OnHeldOffhandDominantStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) {
            /*if (byEntity == null || byEntity.LeftHandItemSlot?.Empty != true) {
                return;
            }*/

            byEntity.LeftHandItemSlot.Itemstack.Collectible.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);

            /*foreach (CollectibleBehavior behavior in byEntity.LeftHandItemSlot.Itemstack.Collectible.CollectibleBehaviors) {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handled);

                if (handled == EnumHandling.PreventSubsequent) return;
            }

            return;*/
        }

        public bool OnHeldOffhandDominantCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason) {
            /*if (byEntity == null || byEntity.LeftHandItemSlot?.Empty != true) {
                return true;
            }*/

            return byEntity.LeftHandItemSlot.Itemstack.Collectible.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason);

            /*bool result = true;
            bool preventDefault = false;

            foreach (CollectibleBehavior behavior in byEntity.LeftHandItemSlot.Itemstack.Collectible.CollectibleBehaviors) {
                if (behavior is CollectibleBehaviorsOffhandDominantInteraction) {
                    continue;
                }

                EnumHandling handled = EnumHandling.PassThrough;

                bool behaviorResult = behavior.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason, ref handled);
                if (handled != EnumHandling.PassThrough) {
                    result &= behaviorResult;
                    preventDefault = true;
                }

                if (handled == EnumHandling.PreventSubsequent) return result;
            }

            if (preventDefault) return result;

            return true;*/
        }
    }
}
