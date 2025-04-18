using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Toolsmith.ToolTinkering.Behaviors {
    public class CollectibleBehaviorToolHandle : CollectibleBehaviorToolPartWithHealth { //Mostly here just to allow for easy detection if something is a tool handle!
        public CollectibleBehaviorToolHandle(CollectibleObject collObj) : base(collObj) {

        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            dsc.AppendLine(Lang.Get("toolhandledirections"));
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }
    }
}
