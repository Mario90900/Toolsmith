using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Toolsmith.ToolTinkering.Behaviors {
    public class CollectibleBehaviorToolBlunt : CollectibleBehavior {
        public CollectibleBehaviorToolBlunt(CollectibleObject collObj) : base(collObj) {

        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            if (inSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                dsc.AppendLine(Lang.Get("tinkeredtoolverylowdamage"));
            } else if (inSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>()) {
                dsc.AppendLine(Lang.Get("smithedtoolverylowdamage"));
            } else {
                dsc.AppendLine(Lang.Get("toolheadverylowdamage"));
            }
        }
    }
}
