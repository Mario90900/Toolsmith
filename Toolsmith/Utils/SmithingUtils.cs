using HarmonyLib;
using MathNet.Numerics;
using MathNet.Numerics.Random;
using SmithingOverhaul.BlockEntity;
using SmithingOverhaul.Property;
using SmithingPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using Toolsmith.ToolTinkering.Drawbacks;
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
        
        public static int GetHardness(float plasticStrain, float strengthCoeff, float youngsModulus, float hardeningCoeff)
        {
            float totalStrain = (float)(strengthCoeff / (youngsModulus * 10) * Math.Pow(plasticStrain, hardeningCoeff) + plasticStrain);
            return (int)(strengthCoeff * Math.Pow(totalStrain, hardeningCoeff));
        }

        public static int GetToughness(float youngsModulus, float strengthCoeff, float hardeningCoeff, float tensileStrength, float elongation)
        {
            System.Func<double, double> stressStrainCurve = stress => stress / (youngsModulus * 10) + Math.Pow(stress / strengthCoeff, 1 / hardeningCoeff);
            double upper = Integrate.DoubleExponential(stressStrainCurve, 0.0d, (double)tensileStrength);
            return (int)Math.Round(tensileStrength * elongation - upper);
        }
        public static float GetRecrystalization(SmithingPropertyVariant smithProps, ItemStack stack, float temp, float strain, double hourDiff)
        {
            float crystalTemp = 0f;

            if (smithProps != null)
            {
                crystalTemp = smithProps.RecrystalizationTemp;
            }

            if (stack.ItemAttributes?["smithingProperties"].Exists == true)
            {
                JsonObject props = stack.ItemAttributes["smithingProperties"];
                if (props.KeyExists("recrystalizationTemp"))
                {
                    crystalTemp = props["recrystalizationTemp"].AsFloat();
                }
            }

            if (temp < crystalTemp) return 0f;

            float strainFactor = strain * 10f + 1f;
            float tempFactor = Math.Clamp(temp / crystalTemp - 1, 0, 0.1f) * 10f + 1f;

            return (float)(CRYSTALRECOVERY * (strainFactor + tempFactor) * hourDiff * 60f);
        }

        public static void SetStrain(SmithingPropertyVariant smithProps, ItemStack stack, float strain)
        {
            float hardeningCoeff = 0f;
            float strengthCoeff = 0f;
            float youngsModulus = 0f;
            int tensileStrength = 0;

            if (smithProps != null)
            {
                hardeningCoeff = smithProps.HardeningCoeff;
                strengthCoeff = smithProps.StrengthCoeff;
                youngsModulus = smithProps.YoungsModulus;
                tensileStrength = smithProps.TensileStrength;
            }

            if (stack.ItemAttributes?["smithingProperties"].Exists == true)
            {
                JsonObject props = stack.ItemAttributes["smithingProperties"];
                if (props.KeyExists("hardeningCoeff"))
                {
                    hardeningCoeff = props["hardeningCoeff"].AsFloat();
                }
                if (props.KeyExists("strengthCoeff"))
                {
                    strengthCoeff = props["strengthCoeff"].AsFloat();
                }
                if (props.KeyExists("youngsModulus"))
                {
                    youngsModulus = props["youngsModulus"].AsFloat();
                }
                if (props.KeyExists("tensileStrength"))
                {
                    tensileStrength = props["tensileStrength"].AsInt();
                }
            }

            int hardness = GetHardness(strain, strengthCoeff, youngsModulus, hardeningCoeff);
            if (hardness > tensileStrength)
            {   
                stack.Attributes.SetFloat("plasticStrain", strain);
                stack.Attributes.SetInt("hardenedYieldStrength", tensileStrength);
                stack.Attributes.SetBool("isOverstrained", true);
            }
            else
            {
                stack.Attributes.SetFloat("plasticStrain", strain);
                stack.Attributes.SetInt("hardenedYieldStrength", hardness);
                stack.Attributes.SetBool("isOverstrained", false);
            }
        }

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
        public static void Fracture(Vec3i origin, BlockEntityAnvil anvil)
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

        public static void DestroyItem(BlockEntityAnvil anvil, byte[,,] voxels)
        {
            int numVoxels = voxels.Cast<byte>().Count();
            int numBits = (int)(numVoxels / SmithingPlus.Core.Config.VoxelsPerBit);

            ThreadSafeRandom rand = new ThreadSafeRandom();
            string code = anvil.WorkItemStack.Collectible.Variant["metal"];

            if (numBits > 2)
            {
                for (int i = 0; i < 1, i++)
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
                anvil.Api.World.SpawnItemEntity(bit, anvil.Pos.Up(), fractureDirection.Scale(3d))
            }
        }
    }
}
