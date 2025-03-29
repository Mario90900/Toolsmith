using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace Toolsmith.ToolTinkering {
    public class CollectibleBehaviorSmithedTool : CollectibleBehavior {

        public CollectibleBehaviorSmithedTool(CollectibleObject collObj) : base(collObj) {
            
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            if (ToolsmithModSystem.Config.DebugMessages) {
                dsc.AppendLine("This is a Smithed Tool!");
            }
        }
    }
}
