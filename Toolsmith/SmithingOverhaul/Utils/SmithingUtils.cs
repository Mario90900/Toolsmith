using HarmonyLib;
using MathNet.Numerics;
using MathNet.Numerics.Random;
using SmithingOverhaul.Behaviour;
using SmithingOverhaul.Item;
using SmithingOverhaul.Property;
using SmithingPlus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using Toolsmith.SmithingOverhaul.Utils;
using Toolsmith.ToolTinkering.Drawbacks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.GameContent;
using static System.Net.Mime.MediaTypeNames;

namespace Toolsmith.Utils
{
    public static class SmithingUtils
    {
        public static float STRAINMULT = 0.02f;
        public static float CRYSTALRECOVERY = 0.01f;

        public static int FindTopmostVoxel(byte[,,]voxels, int x, int z, int minimumY = 0)
        {
            int topY = -1;

            for (int y = minimumY; y < 6; y++)
            {
                EnumVoxelMaterial mat = (EnumVoxelMaterial)voxels[x, y, z];

                if (mat != EnumVoxelMaterial.Empty)
                {
                    if (y + 1 > 5)
                    {
                        topY = y;
                        break;
                    }
                    else if ((EnumVoxelMaterial)voxels[x, y + 1, z] == EnumVoxelMaterial.Empty)
                    {
                        topY = y;
                        break;
                    }
                }
            }

            return topY;
        }
        public static void Fracture(this BlockEntityAnvil anvil, Vec3i origin)
        {
            byte[,,] voxels = anvil.Voxels;
            ThreadSafeRandom FractureRand = new ThreadSafeRandom();
            int maxFractureSize = (int)(ReforgingUtility.TotalVoxelsInWorkItem(anvil.WorkItemStack) * 0.05);
            
            if (maxFractureSize == 0)
            {
                DestroyItem(anvil, voxels);
                return;
            }

            //Choose a voxel as orgin of crack
            List<Vec3i> possibleCrackStarts = new List<Vec3i>();
            for (int x = -1; x <= 1; x++)
            {
                int _X = origin.X + x;
                if (_X < 0 || _X > 16) continue;

                for (int z = -1; z <= 1; z++)
                {
                    int _Z = origin.Z + z;
                    if (_Z < 0 || _Z > 16) continue;

                    int _Y = FindTopmostVoxel(voxels, _X, _Z, origin.Y);
                    if( _Y != -1)
                    {
                        possibleCrackStarts.Add(new Vec3i(_X, _Y, _Z));
                        break;
                    }
                }
            }

            if(possibleCrackStarts.Count == 0)
            {
                DestroyItem(anvil, voxels);
                return;
            }

            int voxelsRemoved = 0;
            //Pick a voxel to start with
            
            int index = FractureRand.Next(0, possibleCrackStarts.Count);
            Vec3i startVoxel = possibleCrackStarts[index];

            
            //Create a random direction
            Vec2d fractureDirection = new Vec2d(
                (FractureRand.NextDouble() - 0.5) * 2.5,
                (FractureRand.NextDouble() - 0.5) * 2.5
            ).Normalize();

            //Turn vector into line function properties
            double slope = fractureDirection.Y / fractureDirection.X;

            //Remove starting voxel
            voxels[startVoxel.X, startVoxel.Y, startVoxel.Z] = (byte)EnumVoxelMaterial.Empty;
            voxelsRemoved++;

            Vec2d fracTraveler = new Vec2d(startVoxel.X, startVoxel.Z);
            List<Vec3i> fracVoxels = new List<Vec3i>();

            while (voxelsRemoved < maxFractureSize)
            {
                fracTraveler += fractureDirection;
                int xOffset = (int)Math.Floor(fracTraveler.X);
                int zOffset = (int)Math.Floor(fracTraveler.Y);

                int yOffset = FindTopmostVoxel(voxels, xOffset, zOffset);

                if (xOffset > 0 && xOffset < 16 && zOffset > 0 && zOffset < 16)
                {
                    Vec3i newVoxel = new Vec3i(xOffset, yOffset, zOffset);

                    if ((EnumVoxelMaterial)voxels[xOffset, yOffset, zOffset] != EnumVoxelMaterial.Empty)
                    {
                        if (!fracVoxels.Contains(newVoxel)) 
                        {
                            fracVoxels.Add(new Vec3i(xOffset, yOffset, zOffset));
                            voxelsRemoved++;
                        }
                        
                    }
                    else break;
                }
                else break;
            }

            fracTraveler = new Vec2d(startVoxel.X, startVoxel.Z);

            while (voxelsRemoved < maxFractureSize)
            {
                fracTraveler -= fractureDirection;
                int xOffset = (int)Math.Floor(fracTraveler.X);
                int zOffset = (int)Math.Floor(fracTraveler.Y);

                int yOffset = FindTopmostVoxel(voxels, xOffset, zOffset);

                if (xOffset > 0 && xOffset < 16 && zOffset > 0 && zOffset < 16)
                {
                    Vec3i newVoxel = new Vec3i(xOffset, yOffset, zOffset);

                    if ((EnumVoxelMaterial)voxels[xOffset, yOffset, zOffset] != EnumVoxelMaterial.Empty)
                    {
                        if (!fracVoxels.Contains(newVoxel))
                        {
                            fracVoxels.Add(new Vec3i(xOffset, yOffset, zOffset));
                            voxelsRemoved++;
                        }
                    }
                    else break;
                }
                else break;
            }

            for (int i = 0; i < fracVoxels.Count; i++)
            {
                Vec3i tmp = fracVoxels[i];
                voxels[tmp.X, tmp.Y, tmp.Z] = (byte)EnumVoxelMaterial.Empty;
            }

            return;
        }

        public static void DestroyItem(this BlockEntityAnvil anvil, byte[,,] voxels)
        {
            int numVoxels = voxels.Cast<byte>().Count();
            int numBits = (int)(numVoxels / SmithingPlus.Core.Config.VoxelsPerBit);

            ThreadSafeRandom rand = new ThreadSafeRandom();
            string code = anvil.WorkItemStack.Collectible.Variant["metal"];

            if (numBits > 2)
            {
                for (int i = 0; i < 1; i++)
                {
                    Vec3d fractureDirection = new Vec3d(
                        (rand.NextDouble() - 0.5) * 2.5,
                        rand.NextDouble(),
                        (rand.NextDouble() - 0.5) * 2.5
                    ).Normalize();

                    ItemStack bit = new ItemStack(anvil.Api.World.GetItem(new AssetLocation("metalbit-" + code)));
                    anvil.Api.World.SpawnItemEntity(bit, anvil.Pos.Up(), fractureDirection.Scale(3d));
                    numBits--;
                }
            }
            
            for (int i = 0; i < numBits; i++)
            {
                ItemStack bit = new ItemStack(anvil.Api.World.GetItem(new AssetLocation("metalbit-" + code)));
                anvil.Api.World.SpawnItemEntity(bit, anvil.Pos.Up());
            }

            anvil.Voxels = new byte[16, 6, 16];

            return;
        }
        private static StressStrainHandler AssignStressStrainHandler(this ItemStack stack, ICoreAPI api)
        {
            StressStrainHandler ssh = null;
            if (stack.Collectible is SmithingWorkItem)
            {
                ssh = StressStrainHandler.FromTreeAttribute(stack.Attributes);

                int id = stack.Attributes.GetInt("stressStrainRefId", -1);
                if (id == -1)
                {
                    id = SmithingWorkItem.nextHandlerRefId;
                    ++SmithingWorkItem.nextHandlerRefId;
                }
                
                ssh = ObjectCacheUtil.GetOrCreate(api, "stressStrainHandler" + id.ToString(), () =>
                {
                    if (ssh == null)
                    {
                        return new StressStrainHandler(
                        (stack.Collectible as SmithingWorkItem).smithProps,
                        stack);
                    }
                    else return ssh;
                });

                stack.Attributes.SetInt("stressStrainRefId", id);
                ssh.ToTreeAttributes(stack.Attributes);
            }
            return ssh;

        }
        public static StressStrainHandler GetStressStrainHandler(this ItemStack stack, ICoreAPI api)
        {
            if(stack.Collectible is SmithingWorkItem)
            {
                StressStrainHandler ssh = null;
                int id = stack.Attributes.GetInt("stressStrainRefId", -1);
                ssh = ObjectCacheUtil.TryGet<StressStrainHandler>(api, "stressStrainHandler" + id.ToString());
                if (ssh == default(StressStrainHandler))
                {
                    ssh = stack.AssignStressStrainHandler(api);
                }
                
                return ssh;
            }
            else return null;
        }

        public static void AddStrain(this ItemStack stack, ICoreAPI api, float changeInStrain)
        {
            if (stack.Collectible is not SmithingWorkItem) return;

            bool preventDefault = false;

            StressStrainHandler ssh = stack.GetStressStrainHandler(api);
            if (ssh == null) return;

            SmithingWorkItem obj = stack.Collectible as SmithingWorkItem;
            foreach (SmithingBehavior behavior in obj.SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.OnAddStrain(ssh, stack, api.World, changeInStrain, ref handled);

                if (handled != EnumHandling.PassThrough) preventDefault = true;

                if (handled == EnumHandling.PreventSubsequent) return;
            }
            if (preventDefault) return;

            //Default Behaviour

            ssh.AddStrain(changeInStrain);
            return;
        }
        public static void RecoverStrain(ItemStack stack, ICoreAPI api, float temperature, double hourDiff)
        {
            if (stack.Collectible is not SmithingWorkItem) return;

            bool preventDefault = false;
            StressStrainHandler ssh = stack.GetStressStrainHandler(api);
            if (ssh == null) return;

            SmithingWorkItem obj = stack.Collectible as SmithingWorkItem;
            foreach (SmithingBehavior behavior in obj.SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.OnRecoverStrain(ssh, stack, api.World, temperature, hourDiff, ref handled);

                if (handled != EnumHandling.PassThrough) preventDefault = true;

                if (handled == EnumHandling.PreventSubsequent) return;
            }
            if (preventDefault) return;


            //Default Behaviour

            ssh.RecoverStrain(stack, temperature, hourDiff);
            return;
        }
    }
}
