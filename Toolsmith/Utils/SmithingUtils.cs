using MathNet.Numerics;
using SmithingOverhaul.BlockEntity;
using SmithingOverhaul.Property;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

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

        public static void Fracture(Vec3i origin, BlockEntityAnvil anvil)
        {

        }
    }
}
