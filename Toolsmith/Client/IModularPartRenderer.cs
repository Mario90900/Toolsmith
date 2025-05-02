using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Toolsmith.Client {
    public interface IModularPartRenderer {

        public abstract ITreeAttribute InitializeRenderTree(ITreeAttribute tree, Item item);

        public abstract void ResetRotationAndOffset(ItemStack stack);
    }
}
