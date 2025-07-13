using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolsmith.SmithingOverhaul.Utils
{
    public static class SmithingOverhaulAttributes
    {
        //Stress Strain handler attribute indexes
        public const string SmithingPropsAttr = "smithingProperties";
        public const string StressStrainAttr = "stressStrain";
        public const string MeltPointAttr = "meltPoint";
        public const string SpecificHeatCapacityAttr = "specificHC";
        public const string ElementalAttr = "elemental";
        public const string TensileStrengthAttr = "tensileStrength";
        public const string YieldStrengthAttr = "yieldStrength";
        public const string ElongationAttr = "elongation";
        public const string ElasticityModulusAttr = "elasticityModulus";
        public const string RecrystalizationTempAttr = "recrystalizationTemp";
        public const string HardeningCoeffAttr = "hardeningCoeff";
        public const string StrengthCoeffAttr = "strengthCoeff";
        public const string ForgingTempAttr = "workableTemperature";
        public const string PlasticStrainAttr = "plasticStrain";
        public const string StressStrainRefIdAttr = "stressStrainRefId";

        //Output Itemstack attribute indexes
        public const string SmithingOverhaulStatsAttr = "smithingOverhaulStats";
        public const string ToughnessAttr = "toughness";
        public const string HardnessAttr = "hardness";
        public const string MaxSharpnessAttr = "maxSharpness";
        public const string MaxDurabilityAttr = "maxDurability";

        //Temperature attribute indexes
        public const string TemperatureAttrTree = "temperature";
        public const string TempLastUpdateAttr = "temperatureLastUpdate";
        public const string TemperatureAttr = "temperature";
        public const string CooldownSpeedAttr = "cooldownSpeed";
        public const string TimeFrozenAttr = "timeFrozen";
    }
}
