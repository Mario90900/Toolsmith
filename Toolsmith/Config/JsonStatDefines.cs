using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolsmith.Config {

    public class HandleStatDefines { //In an effort to keep things similarly vanilla for durability values, the baseHPfactor is a multiplier on the base durability of the tool-to-be-crafted
        [JsonProperty]
        public string id; //An ID to help access and find what it is - make sure this is the same as the Dictionary Key. It might help to keep an id associated with the stats.

        [JsonProperty]
        public float baseHPfactor; //It's the main part of Handles and Bindings.
        
        [JsonProperty]
        public float selfHPBonus; //For more advanced handles, provides an additional multiplier for the handle's health as a bonus ontop

        [JsonProperty]
        public float bindingHPBonus; //Advanced handles can provide a small bonus to the Binding's HP

        [JsonProperty]
        public float speedBonus; //Advanced handles can make it easier to use the tool as well!
    }

    public class GripStatDefines {
        [JsonProperty]
        public string id;

        [JsonProperty]
        public string texturePath = "plain";

        [JsonProperty]
        public string langTag = ""; //A tag to set for localization purposes that describes the grip on the tool IE: "grip-cloth" for cloth

        [JsonProperty]
        public float speedBonus; //The best speed bonuses come from the grip of the tool. If you can hold it better, you can use it faster...

        [JsonProperty]
        public float chanceToDamage; //And more efficiently too. Gives the handle a chance to ignore damage!
    }

    public class TreatmentStatDefines {
        [JsonProperty]
        public string id;

        [JsonProperty]
        public string langTag = ""; //A tag to set for localization purposes that describes the treatment on the tool IE: "treatment-wax" for wax

        [JsonProperty]
        public float handleHPbonus; //Treating the handle makes it last longer
    }

    public class BindingStatDefines {
        [JsonProperty]
        public string id;

        [JsonProperty]
        public string texturePath = "plain";

        [JsonProperty]
        public string langTag = "";

        [JsonProperty]
        public float baseHPfactor;

        [JsonProperty]
        public float selfHPBonus;

        [JsonProperty]
        public float handleHPBonus;

        [JsonProperty]
        public float recoveryPercent; //If the HP is below this percent, then the binding is ruined if another part breaks

        [JsonProperty]
        public bool isMetal; //If true and the bindings break, try and return some bits

        [JsonProperty]
        public string metalType; //For ease of returning the bits, the material/metal variant of bits to return!
    }
}
