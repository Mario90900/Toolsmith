using MathNet.Numerics;
using Toolsmith.SmithingOverhaul.Item;
using Toolsmith.SmithingOverhaul.Property;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using static Toolsmith.SmithingOverhaul.Utils.SmithingOverhaulAttributes;

namespace Toolsmith.SmithingOverhaul.Utils
{
    public class StressStrainHandler
    {
        public float MeltPoint;
        public float SpecificHeatCapacity;
        public bool Elemental;
        public int TensileStrength; // in MPa
        public int YieldStrength; // in MPa
        public float Elongation; // in %
        public int ElasticityModulus; // in GPa
        public bool IsOverstrained => Hardness > TensileStrength;

        private float plasticStrain;
        
        public float PlasticStrainPrct => plasticStrain / Elongation;

        private float? hardeningCoeff = null;
        private float? strengthCoeff = null;
        private float? crystalTemp = null;
        private float? forgingTemp = null;

        public int Hardness => GetHardness();
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
        public float ForgingTemp 
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
            Elemental = true;
            TensileStrength = 0;
            YieldStrength = 0;
            Elongation = 0;
            ElasticityModulus = 0;
            plasticStrain = 0;
        }
        public StressStrainHandler(ItemStack stack)
        {
            if (stack.Collectible is not SmithingWorkItem) return;

            SmithingPropertyVariant props = (stack.Collectible as SmithingWorkItem).smithProps;

            MeltPoint = props.MeltPoint;
            SpecificHeatCapacity = props.SpecificHeatCapacity;
            Elemental = props.Elemental;
            TensileStrength = props.TensileStrength;
            YieldStrength = props.YieldStrength;
            Elongation = props.Elongation;
            ElasticityModulus = props.YoungsModulus;

            if ((bool)stack.ItemAttributes?[ForgingTempAttr].Exists)
            {
                forgingTemp = stack.ItemAttributes[ForgingTempAttr].AsFloat();
            }
            if ((bool)stack.ItemAttributes?[SmithingPropsAttr].Exists)
            {
                JsonObject _props = stack.ItemAttributes[SmithingPropsAttr];
                if (_props.KeyExists(MeltPointAttr))
                {
                    MeltPoint = _props[MeltPointAttr].AsFloat();
                }
                if (_props.KeyExists(SpecificHeatCapacityAttr))
                {
                    SpecificHeatCapacity = _props[SpecificHeatCapacityAttr].AsFloat();
                }
                if (_props.KeyExists(ElementalAttr))
                {
                    Elemental = _props[ElementalAttr].AsBool();
                }
                if (_props.KeyExists(TensileStrengthAttr))
                {
                    TensileStrength = _props[TensileStrengthAttr].AsInt();
                }
                if (_props.KeyExists(YieldStrengthAttr))
                {
                    YieldStrength = _props[YieldStrengthAttr].AsInt();
                }
                if (_props.KeyExists(ElongationAttr))
                {
                    Elongation = _props[ElongationAttr].AsFloat();
                }
                if (_props.KeyExists(ElasticityModulusAttr))
                {
                    ElasticityModulus = _props[ElasticityModulusAttr].AsInt();
                }
                if (_props.KeyExists(RecrystalizationTempAttr))
                {
                    crystalTemp = _props[RecrystalizationTempAttr].AsFloat();
                }
                if (_props.KeyExists(HardeningCoeffAttr))
                {
                    hardeningCoeff = _props[HardeningCoeffAttr].AsFloat();
                }
                if (_props.KeyExists(StrengthCoeffAttr))
                {
                    strengthCoeff = _props[StrengthCoeffAttr].AsFloat();
                }
            }
        }

        public void ToTreeAttributes(ITreeAttribute attr)
        {
            attr.RemoveAttribute(StressStrainAttr);
            ITreeAttribute new_attr = attr.GetOrAddTreeAttribute(StressStrainAttr);
            new_attr.SetFloat(MeltPointAttr, MeltPoint);
            new_attr.SetFloat(SpecificHeatCapacityAttr, SpecificHeatCapacity);
            new_attr.SetBool(ElementalAttr, Elemental);
            new_attr.SetInt(TensileStrengthAttr, TensileStrength);
            new_attr.SetInt(YieldStrengthAttr, YieldStrength);
            new_attr.SetFloat(ElongationAttr, Elongation);
            new_attr.SetInt(ElasticityModulusAttr, ElasticityModulus);
            if(crystalTemp.HasValue) new_attr.SetFloat(RecrystalizationTempAttr, crystalTemp.Value);
            if(hardeningCoeff.HasValue) new_attr.SetFloat(HardeningCoeffAttr, hardeningCoeff.Value);
            if(strengthCoeff.HasValue) new_attr.SetFloat(StrengthCoeffAttr, strengthCoeff.Value);
            if(forgingTemp.HasValue) new_attr.SetFloat(ForgingTempAttr, forgingTemp.Value);
            new_attr.SetFloat(PlasticStrainAttr, plasticStrain);
        }

        public static StressStrainHandler FromTreeAttribute(ITreeAttribute attr)
        {
            StressStrainHandler ssh = new StressStrainHandler();
            if (!attr.HasAttribute(StressStrainAttr))
            {
                return null;
            }

            ITreeAttribute new_attr = attr.GetOrAddTreeAttribute(StressStrainAttr);
            ssh.MeltPoint = new_attr.GetFloat(MeltPointAttr);
            ssh.SpecificHeatCapacity = new_attr.GetFloat(SpecificHeatCapacityAttr);
            ssh.Elemental = new_attr.GetBool(ElementalAttr);
            ssh.TensileStrength = new_attr.GetInt(TensileStrengthAttr);
            ssh.YieldStrength = new_attr.GetInt(YieldStrengthAttr);
            ssh.Elongation = new_attr.GetFloat(ElongationAttr);
            ssh.ElasticityModulus = new_attr.GetInt(ElasticityModulusAttr);
            ssh.crystalTemp = new_attr.TryGetFloat(RecrystalizationTempAttr);
            ssh.hardeningCoeff = new_attr.TryGetFloat(HardeningCoeffAttr);
            ssh.strengthCoeff = new_attr.TryGetFloat(StrengthCoeffAttr);
            ssh.forgingTemp = new_attr.TryGetFloat(ForgingTempAttr);
            ssh.plasticStrain = new_attr.GetFloat(PlasticStrainAttr);
            return ssh;
        }

        public int GetHardness()
        {
            float totalStrain = (float)(StrengthCoeff / (ElasticityModulus * 10) * Math.Pow(plasticStrain, HardeningCoeff) + plasticStrain);
            return (int)(StrengthCoeff * Math.Pow(totalStrain, HardeningCoeff));
        }
        public int GetToughness()
        {
            System.Func<double, double> stressStrainCurve = stress => stress / (ElasticityModulus * 10) + Math.Pow(stress / StrengthCoeff, 1 / HardeningCoeff);
            double upper = Integrate.DoubleExponential(stressStrainCurve, 0.0d, (double)TensileStrength);
            return (int)Math.Round(TensileStrength * Elongation - Hardness * plasticStrain - upper);
        }
        public virtual double GetMaxSharpness()
        {
            return Math.Cbrt(Hardness + 100) / 500;
        }
        public virtual int GetMaxDurability()
        {
            return (int)(GetToughness() * 0.1);
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
