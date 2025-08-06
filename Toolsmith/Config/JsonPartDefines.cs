using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolsmith.Config {

    public class HandlePartDefines {
        [JsonProperty]
        public string id;

        [JsonProperty]
        public string handleStatTag;

        [JsonProperty]
        public bool canHaveGrip = false;

        [JsonProperty]
        public string handleShapePath = "";

        [JsonProperty]
        public bool canBeTreated = false;

        [JsonProperty]
        public float dryingTimeMult = 1.0f;
    }

    public class GripPartDefines {
        [JsonProperty]
        public string id;

        [JsonProperty]
        public string gripStatTag;

        [JsonProperty]
        public string gripShapePath = "";

        [JsonProperty]
        public string gripTextureOverride = "";
    }

    public class TreatmentPartDefines {
        [JsonProperty]
        public string id;

        [JsonProperty]
        public string treatmentStatTag;

        [JsonProperty]
        public int dryingHours; //Base number of hours it takes to dry a handle, multiplied by the handle's drying time multiplier when applied.

        [JsonProperty]
        public bool isLiquid = false;

        [JsonProperty]
        public float litersUsed = 0.0f;
    }

    public class BindingPartDefines {
        [JsonProperty]
        public string id;

        [JsonProperty]
        public string bindingStatTag;

        [JsonProperty]
        public string bindingShapePath = "";

        [JsonProperty]
        public string bindingTextureOverride = "";
    }
}
