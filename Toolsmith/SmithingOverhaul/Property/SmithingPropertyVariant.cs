using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Vintagestory.ServerMods;

namespace SmithingOverhaul.Property
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SmithingPropertyVariant : WorldWoodPropertyVariant
    {
        [JsonProperty]
        public float MeltPoint;
        [JsonProperty]
        public float SpecificHeatCapacity;
        [JsonProperty]
        public float Density;
        [JsonProperty]
        public bool Elemental;
        [JsonProperty]
        public int TensileStrength; // in MPa
        [JsonProperty]
        public int YieldStrength; // in MPa
        [JsonProperty]
        public float Elongation; // in %
        [JsonProperty]
        public int YoungsModulus; // in GPa

        public float HardeningCoeff => (float)(Math.Log(TensileStrength / YieldStrength) / Math.Log(Elongation / 0.2));
        public float StrengthCoeff => (float)(YieldStrength / Math.Pow(0.2, HardeningCoeff));
        public float RecrystalizationTemp
        {
            get
            {
                if (Elemental) return 0.35f * (MeltPoint + 273.15f) - 273.15f;
                else return 0.5f * (MeltPoint + 273.15f) -273.15f;
            }
        }
        public float WarmForgingTemp => 0.6f * (RecrystalizationTemp + 273.15f) - 273.15f;
    }
}

