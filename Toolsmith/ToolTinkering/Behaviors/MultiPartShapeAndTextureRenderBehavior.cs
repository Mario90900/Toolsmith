using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Toolsmith.ToolTinkering.Behaviors {
    public class MultiPartShapeAndTextureRenderBehavior : CollectibleBehavior, IContainedMeshSource {
        public MultiPartShapeAndTextureRenderBehavior(CollectibleObject collObj) : base(collObj) {
        }

        public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos) {
            throw new NotImplementedException();
        }

        public string GetMeshCacheKey(ItemStack itemstack) {
            throw new NotImplementedException();
        }
    }
}
