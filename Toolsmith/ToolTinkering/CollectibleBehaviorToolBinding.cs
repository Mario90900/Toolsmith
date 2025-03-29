using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Toolsmith.ToolTinkering {
    public class CollectibleBehaviorToolBinding : CollectibleBehavior { //Mostly here just for easy simple detection if something is or is not a tool binding!
        public CollectibleBehaviorToolBinding(CollectibleObject collObj) : base(collObj) {

        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            dsc.AppendLine(Lang.Get("toolbindingdirections"));
        }
    }
}
