using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Toolsmith.ToolTinkering {
    public class CollectibleBehaviorToolNoDamageOnUse : CollectibleBehavior {
        public CollectibleBehaviorToolNoDamageOnUse(CollectibleObject collObj) : base(collObj) {

        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            if (inSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                dsc.AppendLine(Lang.Get("tinkeredtoolnodamage"));
            } else if (inSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorSmithedTool>()) {
                dsc.AppendLine(Lang.Get("smithedtoolnodamage"));
            } else {
                dsc.AppendLine(Lang.Get("toolheadnodamage"));
            }
        }
    }
}
