using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.GameContent;

namespace Toolsmith.Config {
    public class HandleStats { //In an effort to keep things similarly vanilla for durability values, the baseHPfactor is a multiplier on the base durability of the tool-to-be-crafted
        public string id; //An ID to help access and find what it is - make sure this is the same as the Dictionary Key. It might help to keep an id associated with the stats.
        public float baseHPfactor; //It's the main part of Handles and Bindings.
        public float selfHPBonus; //For more advanced handles, provides an additional multiplier for the handle's health as a bonus ontop
        public float bindingHPBonus; //Advanced handles can provide a small bonus to the Binding's HP
        public float speedBonus; //Advanced handles can make it easier to use the tool as well!
    }

    public class GripStats {
        public string id;
        public string texturePath = "plain";
        public string langTag = ""; //A tag to set for localization purposes that describes the grip on the tool IE: "grip-cloth" for cloth
        public float speedBonus; //The best speed bonuses come from the grip of the tool. If you can hold it better, you can use it faster...
        public float chanceToDamage; //And more efficiently too. Gives the handle a chance to ignore damage!
    }

    public class TreatmentStats {
        public string id;
        public string langTag = ""; //A tag to set for localization purposes that describes the treatment on the tool IE: "treatment-wax" for wax
        public float handleHPbonus; //Treating the handle makes it last longer
    }

    public class BindingStats {
        public string id;
        public float baseHPfactor;
        public float selfHPBonus;
        public float handleHPBonus;
        public float recoveryPercent; //If the HP is below this percent, then the binding is ruined if another part breaks
        public bool isMetal; //If true and the bindings break, try and return some bits
        public string metalType; //For ease of returning the bits, the material/metal variant of bits to return!
    }

    public class ToolsmithPartStats { //A Tool's head HP is always 5 times the base tool's HP level, and always ticks down 1 durability a use, since metal is easy to grindstone-fix
        public Dictionary<string, HandleStats> baseHandles = new() {
            ["stick"] = new() { id = "stick", baseHPfactor = 1.0f, selfHPBonus = 0.0f, bindingHPBonus = 0.0f, speedBonus = 0.0f },
            ["bone"] = new() { id = "bone", baseHPfactor = 1.0f, selfHPBonus = 0.05f, bindingHPBonus = 0.05f, speedBonus = 0.0f },
            ["crude"] = new() { id = "crude", baseHPfactor = 1.0f, selfHPBonus = 0.0f, bindingHPBonus = 0.05f, speedBonus = 0.0f },
            ["handle"] = new() { id = "handle", baseHPfactor = 1.2f, selfHPBonus = 0.05f, bindingHPBonus = 0.2f, speedBonus = 0.05f },
            ["professional"] = new() { id = "professional", baseHPfactor = 1.5f, selfHPBonus = 0.1f, bindingHPBonus = 0.4f, speedBonus = 0.1f }
        };
        public Dictionary<string, GripStats> grips = new() {
            ["plain"] = new() { id = "plain", speedBonus = 0.0f, chanceToDamage = 1.0f },
            ["twine"] = new() { id = "twine", texturePath = "game:block/cloth/reedrope", langTag = "grip-twine", speedBonus = 0.0f, chanceToDamage = 0.95f },
            ["cloth"] = new() { id = "cloth", texturePath = "game:block/cloth/linen/normal1", langTag = "grip-cloth", speedBonus = 0.1f, chanceToDamage = 0.9f },
            ["leather"] = new() { id = "leather", texturePath = "game:block/leather/plain", langTag = "grip-leather", speedBonus = 0.2f, chanceToDamage = 0.8f },
            ["sturdy"] = new() { id = "sturdy", texturePath = "game:block/leather/chromium", langTag = "grip-sturdy", speedBonus = 0.3f, chanceToDamage = 0.65f }
        };
        public Dictionary<string, TreatmentStats> treatments = new() {
            ["none"] = new() { id = "none", handleHPbonus = 0.0f },
            ["fat"] = new() { id = "fat", langTag = "treatment-fat", handleHPbonus = 0.2f },
            ["wax"] = new() { id = "wax", langTag = "treatment-wax", handleHPbonus = 0.5f },
            ["oil"] = new() { id = "oil", langTag = "treatment-oil", handleHPbonus = 0.65f }
        };
        public Dictionary<string, BindingStats> bindings = new() {
            ["none"] = new() { id = "none", baseHPfactor = 0.5f, selfHPBonus = 0.0f, handleHPBonus = 0.0f, recoveryPercent = 1.0f, isMetal = false },
            ["reeds"] = new() { id = "reeds", baseHPfactor = 1.0f, selfHPBonus = 0.0f, handleHPBonus = 0.0f, recoveryPercent = 1.0f, isMetal = false },
            ["twine"] = new() { id = "twine", baseHPfactor = 1.2f, selfHPBonus = 0.1f, handleHPBonus = 0.05f, recoveryPercent = 0.9f, isMetal = false },
            ["rope"] = new() { id = "rope", baseHPfactor = 1.25f, selfHPBonus = 0.15f, handleHPBonus = 0.05f, recoveryPercent = 0.7f, isMetal = false },
            ["leather"] = new() { id = "leather", baseHPfactor = 1.5f, selfHPBonus = 0.3f, handleHPBonus = 0.1f, recoveryPercent = 0.6f, isMetal = false },
            ["glue"] = new() { id = "glue", baseHPfactor = 2.0f, selfHPBonus = 0.3f, handleHPBonus = 0.3f, recoveryPercent = 1.0f, isMetal = false },
            ["coppernails"] = new() { id = "coppernails", baseHPfactor = 1.4f, selfHPBonus = 0.1f, handleHPBonus = 0.1f, recoveryPercent = 0.9f, isMetal = true, metalType = "copper" },
            ["tinbronzenails"] = new() { id = "tinbronzenails", baseHPfactor = 1.7f, selfHPBonus = 0.2f, handleHPBonus = 0.2f, recoveryPercent = 0.5f, isMetal = true, metalType = "tinbronze" },
            ["bismuthbronzenails"] = new() { id = "bismuthbronzenails", baseHPfactor = 1.7f, selfHPBonus = 0.25f, handleHPBonus = 0.2f, recoveryPercent = 0.5f, isMetal = true, metalType = "bismuthbronze" },
            ["blackbronzenails"] = new() { id = "blackbronzenails", baseHPfactor = 1.7f, selfHPBonus = 0.3f, handleHPBonus = 0.2f, recoveryPercent = 0.5f, isMetal = true, metalType = "blackbronze" },
            ["ironnails"] = new() { id = "ironnails", baseHPfactor = 1.8f, selfHPBonus = 0.3f, handleHPBonus = 0.2f, recoveryPercent = 0.45f, isMetal = true, metalType = "iron" },
            ["cupronickelnails"] = new() { id = "cupronickelnails", baseHPfactor = 1.7f, selfHPBonus = 0.2f, handleHPBonus = 0.2f, recoveryPercent = 0.5f, isMetal = true, metalType = "cupronickel" },
            ["meteoricironnails"] = new() { id = "meteoricironnails", baseHPfactor = 1.9f, selfHPBonus = 0.5f, handleHPBonus = 0.25f, recoveryPercent = 0.45f, isMetal = true, metalType = "meteoriciron" },
            ["steelnails"] = new() { id = "steelnails", baseHPfactor = 2.2f, selfHPBonus = 0.6f, handleHPBonus = 0.4f, recoveryPercent = 0.35f, isMetal = true, metalType = "steel" }
        };
    }
}
