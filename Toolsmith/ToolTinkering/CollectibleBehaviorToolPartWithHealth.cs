using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Toolsmith.ToolTinkering {
    public class CollectibleBehaviorToolPartWithHealth : CollectibleBehavior {
        public CollectibleBehaviorToolPartWithHealth(CollectibleObject collObj) : base(collObj) {

        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            if (!inSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolBlunt>()) {
                var remainingPercent = inSlot.Itemstack.GetPartRemainingHPPercent(); //Is there ANY way this could possibly have a null itemstack and still get called...? I'd be shocked honestly, hah!
                if (remainingPercent <= 0.0f || remainingPercent >= 1.0f) { //If this returns 0 or less, assume it has not been used or is at full durability! If it's 1.0 or more (somehow?), well, similarly :P
                    dsc.AppendLine(Lang.Get("pristinecondition"));
                } else {
                    var percent = Math.Floor(remainingPercent * 100);
                    dsc.AppendLine(Lang.Get("partiallydamaged", percent));
                }
            }
        }
    }
}
