using SmithingOverhaul.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Utils;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SmithingOverhaul.BlockEntity
{
    public class BlockEntitySmithingAnvil : BlockEntityAnvil
    {
        public new void OnHit(Vec3i voxelPos)
        {
            int voxelsDisplaced = 0;
            if (Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] != (byte)EnumVoxelMaterial.Metal) return;

            if (voxelPos.Y > 0)
            {
                int voxelsMoved = 0;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        if (voxelPos.X + dx < 0 || voxelPos.X + dx >= 16 || voxelPos.Z + dz < 0 || voxelPos.Z + dz >= 16) continue;

                        if (Voxels[voxelPos.X + dx, voxelPos.Y, voxelPos.Z + dz] == (byte)EnumVoxelMaterial.Metal)
                        {
                            voxelsMoved += moveVoxelDownwards(voxelPos.Clone().Add(dx, 0, dz), null, 1) ? 1 : 0;
                        }
                    }
                }

                if (Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] == (byte)EnumVoxelMaterial.Metal)
                {
                    voxelsMoved += moveVoxelDownwards(voxelPos.Clone(), null, 1) ? 1 : 0;
                }

                voxelsDisplaced += voxelsMoved;

                if (voxelsMoved == 0)
                {
                    Vec3i emptySpot = null;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0) continue;
                            if (voxelPos.X + 2 * dx < 0 || voxelPos.X + 2 * dx >= 16 || voxelPos.Z + 2 * dz < 0 || voxelPos.Z + 2 * dz >= 16) continue;

                            bool spotEmpty = Voxels[voxelPos.X + 2 * dx, voxelPos.Y, voxelPos.Z + 2 * dz] == (byte)EnumVoxelMaterial.Empty;

                            if (Voxels[voxelPos.X + dx, voxelPos.Y, voxelPos.Z + dz] == (byte)EnumVoxelMaterial.Metal && spotEmpty)
                            {
                                Voxels[voxelPos.X + dx, voxelPos.Y, voxelPos.Z + dz] = (byte)EnumVoxelMaterial.Empty;

                                if (Voxels[voxelPos.X + 2 * dx, voxelPos.Y - 1, voxelPos.Z + 2 * dz] == (byte)EnumVoxelMaterial.Empty)
                                {
                                    Voxels[voxelPos.X + 2 * dx, voxelPos.Y - 1, voxelPos.Z + 2 * dz] = (byte)EnumVoxelMaterial.Metal;
                                }
                                else
                                {
                                    Voxels[voxelPos.X + 2 * dx, voxelPos.Y, voxelPos.Z + 2 * dz] = (byte)EnumVoxelMaterial.Metal;
                                }

                                voxelsDisplaced += 1;

                            }
                            else
                            {
                                if (spotEmpty) emptySpot = voxelPos.Clone().Add(dx, 0, dz);

                            }
                        }
                    }

                    if (emptySpot != null && Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] == (byte)EnumVoxelMaterial.Metal)
                    {
                        Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] = (byte)EnumVoxelMaterial.Empty;

                        if (Voxels[emptySpot.X, emptySpot.Y - 1, emptySpot.Z] == (byte)EnumVoxelMaterial.Empty)
                        {
                            Voxels[emptySpot.X, emptySpot.Y - 1, emptySpot.Z] = (byte)EnumVoxelMaterial.Metal;
                        }
                        else
                        {
                            Voxels[emptySpot.X, emptySpot.Y, emptySpot.Z] = (byte)EnumVoxelMaterial.Metal;
                        }

                        voxelsDisplaced += 1;

                    }
                }
            }

            if (WorkItemStack.Collectible != null && WorkItemStack.Collectible is SmithingWorkItem)
            {
                SmithingWorkItem item = (WorkItemStack.Collectible as SmithingWorkItem);
                item.AfterOnHit(voxelsDisplaced, WorkItemStack);
                if (item.IsOverstrained(WorkItemStack)) SmithingUtils.Fracture(voxelPos, this);
            }
        }
        public new void OnUpset(Vec3i voxelPos, BlockFacing towardsFace)
        {
            base.OnUpset(voxelPos, towardsFace);

            if (WorkItemStack.Collectible != null && WorkItemStack.Collectible is SmithingWorkItem)
            {
                SmithingWorkItem item = (WorkItemStack.Collectible as SmithingWorkItem);
                item.AfterOnUpset(WorkItemStack);
                if (item.IsOverstrained(WorkItemStack)) SmithingUtils.Fracture(voxelPos, this);
            }
        }

        public new void OnSplit(Vec3i voxelPos)
        {
            base.OnSplit(voxelPos);

            if (WorkItemStack.Collectible != null && WorkItemStack.Collectible is SmithingWorkItem)
            {
                SmithingWorkItem item = (WorkItemStack.Collectible as SmithingWorkItem);
                item.AfterOnSplit(WorkItemStack);
                if (item.IsOverstrained(WorkItemStack)) SmithingUtils.Fracture(voxelPos, this);
            }
        }
        
    }
}
