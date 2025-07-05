using MathNet.Numerics;
using Newtonsoft.Json;
using SmithingOverhaul.Property;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Toolsmith.SmithingOverhaul.Utils
{
    public class StressStrainHandler
    {
        public float MeltPoint;
        public float SpecificHeatCapacity;
        public float Density;
        public bool Elemental;
        public int TensileStrength; // in MPa
        public int YieldStrength; // in MPa
        public float Elongation; // in %
        public int YoungsModulus; // in GPa

        public const string StressStrainHandlerAttr = "stressStrainObj";
        
        private float? hardeningCoeff = null;
        private float? strengthCoeff = null;
        private float? crystalTemp = null;
        private float? forgingTemp = null;
        public float HardeningCoeff 
        {
            get
            {
                if (hardeningCoeff != null) return hardeningCoeff.Value;
                else return (float)(Math.Log(TensileStrength / YieldStrength) / Math.Log(Elongation / 0.2));
            }
        } 
        public float StrengthCoeff 
        {
            get
            {
                if(strengthCoeff != null) return strengthCoeff.Value;
                else return (float)(YieldStrength / Math.Pow(0.2, HardeningCoeff));
            }
        }
        public float RecrystalizationTemp
        {
            get
            {
                if (crystalTemp != null) return crystalTemp.Value;
                else if (Elemental) return 0.35f * (MeltPoint + 273.15f) - 273.15f;
                else return 0.5f * (MeltPoint + 273.15f) - 273.15f;
            }
        }
        public float WarmForgingTemp 
        {
            get
            {
                if(forgingTemp != null) return forgingTemp.Value;
                else return 0.6f * (RecrystalizationTemp + 273.15f) - 273.15f;
            }
        }

        private float plasticStrain;
        private StressStrainHandler()
        {
            MeltPoint = 0;
            SpecificHeatCapacity = 0;
            Density = 0;
            Elemental = true;
            TensileStrength = 0;
            YieldStrength = 0;
            Elongation = 0;
            YoungsModulus = 0;
        }
        public StressStrainHandler(SmithingPropertyVariant props, ItemStack stack)
        {
            MeltPoint = props.MeltPoint;
            SpecificHeatCapacity = props.SpecificHeatCapacity;
            Density = props.Density;
            Elemental = props.Elemental;
            TensileStrength = props.TensileStrength;
            YieldStrength = props.YieldStrength;
            Elongation = props.Elongation;
            YoungsModulus = props.YoungsModulus;

            if ((bool)stack.ItemAttributes?["workableTemperature"].Exists)
            {
                forgingTemp = stack.ItemAttributes["workableTemperature"].AsFloat();
            }
            if ((bool)stack.ItemAttributes?["smithingProperties"].Exists)
            {
                JsonObject _props = stack.ItemAttributes["smithingProperties"];
                if (_props.KeyExists("meltPoint"))
                {
                    MeltPoint = _props["meltPoint"].AsFloat();
                }
                if (_props.KeyExists("specificHC"))
                {
                    SpecificHeatCapacity = _props["specificHC"].AsFloat();
                }
                if (_props.KeyExists("density"))
                {
                    Density = _props["density"].AsFloat();
                }
                if (_props.KeyExists("elemental"))
                {
                    Elemental = _props["elemental"].AsBool();
                }
                if (_props.KeyExists("tensileStrength"))
                {
                    TensileStrength = _props["tensileStrength"].AsInt();
                }
                if (_props.KeyExists("yieldStrength"))
                {
                    YieldStrength = _props["yieldStrength"].AsInt();
                }
                if (_props.KeyExists("elongation"))
                {
                    Elongation = _props["elongation"].AsFloat();
                }
                if (_props.KeyExists("youngsModulus"))
                {
                    YoungsModulus = _props["youngsModulus"].AsInt();
                }
                if (_props.KeyExists("recrystalizationTemp"))
                {
                    crystalTemp = _props["recrystalizationTemp"].AsFloat();
                }
                if (_props.KeyExists("hardeningCoeff"))
                {
                    hardeningCoeff = _props["hardeningCoeff"].AsFloat();
                }
                if (_props.KeyExists("strengthCoeff"))
                {
                    strengthCoeff = _props["strengthCoeff"].AsFloat();
                }
            }
        }

        public virtual void ToTreeAttributes(ITreeAttribute attr)
        {
            attr.RemoveAttribute(StressStrainHandlerAttr);
            ITreeAttribute new_attr = attr.GetOrAddTreeAttribute(StressStrainHandlerAttr);
            new_attr.SetFloat("meltPoint", MeltPoint);
            new_attr.SetFloat("specificHC", SpecificHeatCapacity);
            new_attr.SetFloat("density", Density);
            new_attr.SetBool("elemental", Elemental);
            new_attr.SetInt("tensileStrength", TensileStrength);
            new_attr.SetInt("yieldStrength", YieldStrength);
            new_attr.SetFloat("elongation", Elongation);
            new_attr.SetInt("youngsModulus", YoungsModulus);
        }

        public static StressStrainHandler FromTreeAttribute(ITreeAttribute attr)
        {
            StressStrainHandler ssh = new StressStrainHandler();
            if (!attr.HasAttribute(StressStrainHandlerAttr))
            {
                return null;
            }

            ITreeAttribute new_attr = attr.GetOrAddTreeAttribute(StressStrainHandlerAttr);
            ssh.MeltPoint = new_attr.GetFloat("meltPoint");
            ssh.SpecificHeatCapacity = new_attr.GetFloat("specificHC");
            ssh.Density = new_attr.GetFloat("density");
            ssh.Elemental = new_attr.GetBool("elemental");
            ssh.TensileStrength = new_attr.GetInt("tensileStrength");
            ssh.YieldStrength = new_attr.GetInt("yieldStrength");
            ssh.Elongation = new_attr.GetFloat("elongation");
            ssh.YoungsModulus = new_attr.GetInt("youngsModulus");

            return ssh;
        }

        public static int GetHardness()
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
        public static float GetRecrystalization(this ItemStack stack, SmithingPropertyVariant smithProps, float temp, float strain, double hourDiff)
        {
            float crystalTemp = 0f;

            if (temp < crystalTemp) return 0f;

            float strainFactor = strain * 10f + 1f;
            float tempFactor = Math.Clamp(temp / crystalTemp - 1, 0, 0.1f) * 10f + 1f;

            return (float)(CRYSTALRECOVERY * (strainFactor + tempFactor) * hourDiff * 60f);
        }

        public static void SetStrain(this ItemStack stack, SmithingPropertyVariant smithProps, float strain)
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
    }
}
