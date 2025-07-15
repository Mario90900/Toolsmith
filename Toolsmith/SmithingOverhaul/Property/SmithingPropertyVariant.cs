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
    }
}

