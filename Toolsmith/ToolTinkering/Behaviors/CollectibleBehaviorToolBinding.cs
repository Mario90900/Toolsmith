using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Toolsmith.ToolTinkering.Behaviors {
    public class CollectibleBehaviorToolBinding : CollectibleBehavior { //Mostly here just for easy simple detection if something is or is not a tool binding!
        public CollectibleBehaviorToolBinding(CollectibleObject collObj) : base(collObj) {

        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            if (world.Side.IsClient()) {
                dsc.AppendLine(Lang.Get("toolbindingdirections"));
                if (ToolsmithModSystem.BindingTiers != null) {
                    dsc.AppendLine(Lang.Get("toolbindingtier", ToolsmithModSystem.BindingTiers.Get(inSlot.Itemstack.Collectible.Code.Path)));
                }

                var bindingStats = ToolsmithModSystem.Stats.bindings.Get(ToolsmithModSystem.Config.BindingRegistry.Get(inSlot.Itemstack.Collectible.Code.Path).bindingStatTag);
                if (bindingStats != null) {
                    var totalMult = bindingStats.baseHPfactor * (1 + bindingStats.selfHPBonus);
                    dsc.AppendLine("");
                    dsc.AppendLine(Lang.Get("toolbindingtotalmult", totalMult.ToString("0.00")));
                    dsc.AppendLine(Lang.Get("toolbindinghandlebonus", Math.Round(bindingStats.handleHPBonus * 100)));
                    dsc.AppendLine(Lang.Get("toolbindingrecoverychance", Math.Round(bindingStats.recoveryPercent * 100)));
                    if (bindingStats.isMetal) {
                        dsc.AppendLine(Lang.Get("toolbindingmetaldrops"));
                    }
                }
            }
        }
    }
}
