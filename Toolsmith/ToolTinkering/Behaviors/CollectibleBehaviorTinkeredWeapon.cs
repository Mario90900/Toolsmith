using ItemRarity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace Toolsmith.ToolTinkering.Behaviors {
    public class CollectibleBehaviorTinkeredWeapon : CollectibleBehavior {



        public CollectibleBehaviorTinkeredWeapon(CollectibleObject collObj) : base(collObj) {

        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {

        }

        protected void HandleExtraModCompat(ItemSlot[] allInputslots, ItemSlot outputSlot) { //Adding this here so that all weapons can call it from their respective
            if (ToolsmithModSystem.Api.ModLoader.IsModEnabled("xskills")) { //Copy over the Quality Attribute (if it exists) onto the output item so that the GetMaxDurability will account for it here!
                HandleXSkillsCompat(allInputslots, outputSlot);
            }

            if (ToolsmithModSystem.Api.ModLoader.IsModEnabled("itemrarity")) {
                HandleItemRarityCompat(allInputslots, outputSlot);
            }

            if (ToolsmithModSystem.Api.ModLoader.IsModEnabled("canjewelry")) {
                foreach (var input in allInputslots.Where(i => !i.Empty && i.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolHead>())) {
                    TinkeringUtility.CheckAndHandleJewelryStatTransfer(input.Itemstack, outputSlot.Itemstack);
                    break;
                }
            }
        }

        private void HandleXSkillsCompat(ItemSlot[] allInputslots, ItemSlot outputSlot) {
            float quality = 0.0f;
            foreach (var input in allInputslots.Where(i => !i.Empty)) {
                if (input.Itemstack.Attributes.HasAttribute("quality")) {
                    quality = input.Itemstack.Attributes.GetFloat("quality");
                    break;
                }
            }

            if (quality > 0.0f) {
                outputSlot.Itemstack.Attributes.SetFloat("quality", quality); //Doesn't appear to double up or anything, thankfully!
            }
        }

        private void HandleItemRarityCompat(ItemSlot[] allInputslots, ItemSlot outputSlot) {
            var itemStack = outputSlot.Itemstack;
            if (itemStack == null || itemStack.Item?.Tool == null || itemStack.Attributes.HasAttribute(ModAttributes.Guid)) {
                return;
            }
            var rarity = Rarity.GetRandomRarity();
            itemStack.SetRarity(rarity.Key);
        }
    }
}
