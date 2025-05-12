using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace Toolsmith.ToolTinkering.Behaviors {
    public interface IOffhandDominantInteractionItem {

        //It is important to know that this will send the OffhandItem the Main Hand slot! And not the Offhand one like in the _actual_ calls.
        //This returns false if it should steal the call, and true if it should let it keep going.
        public abstract bool HasOffhandInteractionAvailable(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent);
    }
}
