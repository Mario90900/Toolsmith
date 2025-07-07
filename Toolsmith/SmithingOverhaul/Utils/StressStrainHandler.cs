using MathNet.Numerics;
using Newtonsoft.Json;
using SmithingOverhaul.Behaviour;
using SmithingOverhaul.Item;
using SmithingOverhaul.Property;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using static Vintagestory.Server.Timer;

namespace Toolsmith.SmithingOverhaul.Utils
{
    public class StressStrainHandler
    {
        public const string AttrName = "stressStrainObj";

        public float MeltPoint;
        public float SpecificHeatCapacity;
        public float Density;
        public bool Elemental;
        public int TensileStrength; // in MPa
        public int YieldStrength; // in MPa
        public float Elongation; // in %
        public int YoungsModulus; // in GPa
        public bool IsOverstrained => GetHardness() > TensileStrength;

        private float plasticStrain;
        public float PlasticStrainPrct => plasticStrain / Elongation;

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
            plasticStrain = 0;
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
            attr.RemoveAttribute(AttrName);
            ITreeAttribute new_attr = attr.GetOrAddTreeAttribute(AttrName);
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
            if (!attr.HasAttribute(AttrName))
            {
                return null;
            }

            ITreeAttribute new_attr = attr.GetOrAddTreeAttribute(AttrName);
            ssh.MeltPoint = new_attr.GetFloat("meltPoint");
            ssh.SpecificHeatCapacity = new_attr.GetFloat("specificHC");
            ssh.Density = new_attr.GetFloat("density");
            ssh.Elemental = new_attr.GetBool("elemental");
            ssh.TensileStrength = new_attr.GetInt("tensileStrength");
            ssh.YieldStrength = new_attr.GetInt("yieldStrength");
            ssh.Elongation = new_attr.GetFloat("elongation");
            ssh.YoungsModulus = new_attr.GetInt("youngsModulus");
            ssh.crystalTemp = new_attr.TryGetFloat("recrystalizationTemp");
            ssh.hardeningCoeff = new_attr.TryGetFloat("hardeningCoeff");
            ssh.strengthCoeff = new_attr.TryGetFloat("strengthCoeff");
            ssh.forgingTemp = new_attr.TryGetFloat("forgingTemperature");
            ssh.plasticStrain = new_attr.GetFloat("plasticStrain");
            return ssh;
        }

        public virtual int GetHardness()
        {
            float totalStrain = (float)(StrengthCoeff / (YoungsModulus * 10) * Math.Pow(plasticStrain, HardeningCoeff) + plasticStrain);
            return (int)(StrengthCoeff * Math.Pow(totalStrain, HardeningCoeff));
        }

        public virtual int GetToughness()
        {
            System.Func<double, double> stressStrainCurve = stress => stress / (YoungsModulus * 10) + Math.Pow(stress / StrengthCoeff, 1 / HardeningCoeff);
            double upper = Integrate.DoubleExponential(stressStrainCurve, 0.0d, (double)TensileStrength);
            return (int)Math.Round(TensileStrength * Elongation - upper);
        }
        public virtual float GetRecrystalization(float temp, double hourDiff)
        {
            if (temp < RecrystalizationTemp) return 0f;

            float strainFactor = plasticStrain * 10f + 1f;
            float tempFactor = Math.Clamp(temp / RecrystalizationTemp - 1, 0, 0.1f) * 10f + 1f;

            return (float)(SmithingUtils.CRYSTALRECOVERY * (strainFactor + tempFactor) * hourDiff * 60f);
        }

        public virtual void AddStrain(float changeInStrain)
        {
            plasticStrain += changeInStrain;
            return;
        }

        public virtual void RecoverStrain(ItemStack stack, float temp, double hourDiff)
        {
            float strain_recovered = GetRecrystalization(temp, hourDiff);
            plasticStrain -= strain_recovered;
            return;
        }
    }
}
