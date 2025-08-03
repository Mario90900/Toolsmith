using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.GameContent;

namespace Toolsmith.Config {

    public class ToolsmithPartStats {
        
        public Dictionary<string, HandleStatPair> BaseHandleRegistry = new() {
            ["stick"] = new() { handleStatTag = "stick", handleShapePath = "toolsmith:shapes/item/parts/handles/stick/handle" },
            ["bone"] = new() { handleStatTag = "bone", handleShapePath = "toolsmith:shapes/item/parts/handles/bone/handle" },
            ["crudehandle"] = new() { handleStatTag = "crude", canHaveGrip = true, handleShapePath = "toolsmith:shapes/item/parts/handles/crude/handle" },
            ["handle"] = new() { handleStatTag = "handle", canHaveGrip = true, handleShapePath = "toolsmith:shapes/item/parts/handles/regular/handle", canBeTreated = true },
            ["carpentedhandle"] = new() { handleStatTag = "professional", canHaveGrip = true, handleShapePath = "toolsmith:shapes/item/parts/handles/fine/handle", canBeTreated = true, dryingTimeMult = 2.0f }
        };

        public Dictionary<string, GripStatPair> GripRegistry = new() {
            ["flaxtwine"] = new() { gripStatTag = "twine", gripShapePath = "fabric" },
            ["linen-diamond-down"] = new() { gripStatTag = "cloth", gripShapePath = "fabric" },
            ["linen-normal-down"] = new() { gripStatTag = "cloth", gripShapePath = "fabric" },
            ["linen-offset-down"] = new() { gripStatTag = "cloth", gripShapePath = "fabric" },
            ["linen-square-down"] = new() { gripStatTag = "cloth", gripShapePath = "fabric" },
            ["leather-normal-plain"] = new() { gripStatTag = "leather", gripShapePath = "fabric" },
            ["leather-normal-orange"] = new() { gripStatTag = "leather", gripShapePath = "fabric", gripTextureOverride = "game:block/leather/orange" },
            ["leather-normal-black"] = new() { gripStatTag = "leather", gripShapePath = "fabric", gripTextureOverride = "game:block/leather/black" },
            ["leather-normal-red"] = new() { gripStatTag = "leather", gripShapePath = "fabric", gripTextureOverride = "game:block/leather/red" },
            ["leather-normal-blue"] = new() { gripStatTag = "leather", gripShapePath = "fabric", gripTextureOverride = "game:block/leather/blue" },
            ["leather-normal-purple"] = new() { gripStatTag = "leather", gripShapePath = "fabric", gripTextureOverride = "game:block/leather/purple" },
            ["leather-normal-pink"] = new() { gripStatTag = "leather", gripShapePath = "fabric", gripTextureOverride = "game:block/leather/pink" },
            ["leather-normal-white"] = new() { gripStatTag = "leather", gripShapePath = "fabric", gripTextureOverride = "game:block/leather/white" },
            ["leather-normal-yellow"] = new() { gripStatTag = "leather", gripShapePath = "fabric", gripTextureOverride = "game:block/leather/yellow" },
            ["leather-normal-gray"] = new() { gripStatTag = "leather", gripShapePath = "fabric", gripTextureOverride = "game:block/leather/gray" },
            ["leather-normal-green"] = new() { gripStatTag = "leather", gripShapePath = "fabric", gripTextureOverride = "game:block/leather/green" },
            ["leather-sturdy-plain"] = new() { gripStatTag = "sturdy", gripShapePath = "fabric" },
            ["twine-wool-plain"] = new() { gripStatTag = "twine", gripShapePath = "fabric" },
            ["twine-wool-black"] = new() { gripStatTag = "twine", gripShapePath = "fabric" },
            ["twine-wool-brown"] = new() { gripStatTag = "twine", gripShapePath = "fabric" },
            ["twine-wool-gray"] = new() { gripStatTag = "twine", gripShapePath = "fabric" },
            ["twine-wool-white"] = new() { gripStatTag = "twine", gripShapePath = "fabric" },
            ["twine-wool-yellow"] = new() { gripStatTag = "twine", gripShapePath = "fabric" },
        };

        public Dictionary<string, TreatmentStatPair> TreatmentRegistry = new() { //All treatments will require a drying time.
            ["fat"] = new() { treatmentStatTag = "fat", dryingHours = 12 },
            ["beeswax"] = new() { treatmentStatTag = "wax", dryingHours = 12 },
            ["foodoilportion-flax"] = new() { treatmentStatTag = "oil", dryingHours = 24, isLiquid = true, litersUsed = 0.1f },
            ["foodoilportion-seed"] = new() { treatmentStatTag = "oil", dryingHours = 24, isLiquid = true, litersUsed = 0.1f },
            ["plantoil-flax"] = new() { treatmentStatTag = "oil", dryingHours = 24, isLiquid = true, litersUsed = 0.1f },
            ["plantoil-walnut"] = new() { treatmentStatTag = "oil", dryingHours = 24, isLiquid = true, litersUsed = 0.1f },
            ["woodfinish-flax"] = new() { treatmentStatTag = "oil", dryingHours = 24, isLiquid = true, litersUsed = 0.05f },
            ["woodfinish-walnut"] = new() { treatmentStatTag = "oil", dryingHours = 24, isLiquid = true, litersUsed = 0.05f }
        };

        public Dictionary<string, BindingStatPair> BindingRegistry = new() {
            ["drygrass"] = new() { bindingStatTag = "reeds", bindingShapePath = "string" },
            ["cattailtops"] = new() { bindingStatTag = "reeds", bindingShapePath = "string" },
            ["papyrustops"] = new() { bindingStatTag = "reeds", bindingShapePath = "string" },
            ["flaxtwine"] = new() { bindingStatTag = "twine", bindingShapePath = "string" },
            ["rope"] = new() { bindingStatTag = "rope", bindingShapePath = "string" },
            ["leather-normal-plain"] = new() { bindingStatTag = "leather", bindingShapePath = "string" },
            ["leather-normal-orange"] = new() { bindingStatTag = "leather", bindingShapePath = "string", bindingTextureOverride = "game:block/leather/orange" },
            ["leather-normal-black"] = new() { bindingStatTag = "leather", bindingShapePath = "string", bindingTextureOverride = "game:block/leather/black" },
            ["leather-normal-red"] = new() { bindingStatTag = "leather", bindingShapePath = "string", bindingTextureOverride = "game:block/leather/red" },
            ["leather-normal-blue"] = new() { bindingStatTag = "leather", bindingShapePath = "string", bindingTextureOverride = "game:block/leather/blue" },
            ["leather-normal-purple"] = new() { bindingStatTag = "leather", bindingShapePath = "string", bindingTextureOverride = "game:block/leather/purple" },
            ["leather-normal-pink"] = new() { bindingStatTag = "leather", bindingShapePath = "string", bindingTextureOverride = "game:block/leather/pink" },
            ["leather-normal-white"] = new() { bindingStatTag = "leather", bindingShapePath = "string", bindingTextureOverride = "game:block/leather/white" },
            ["leather-normal-yellow"] = new() { bindingStatTag = "leather", bindingShapePath = "string", bindingTextureOverride = "game:block/leather/yellow" },
            ["leather-normal-gray"] = new() { bindingStatTag = "leather", bindingShapePath = "string", bindingTextureOverride = "game:block/leather/gray" },
            ["leather-normal-green"] = new() { bindingStatTag = "leather", bindingShapePath = "string", bindingTextureOverride = "game:block/leather/green" },
            ["glueportion-pitch-hot"] = new() { bindingStatTag = "glue" }, //Okay Pitch Glue is just weird as hell in vanilla. It'll probably be changed somewhere down the line but, right now it's just weird. It's both a liquid, but ALSO a physical item? Can't pick it up normally like a liquid, but cannot store in a bucket like an item...
            ["metalnailsandstrips-copper"] = new() { bindingStatTag = "coppernails", bindingShapePath = "metal" },
            ["metalnailsandstrips-tinbronze"] = new() { bindingStatTag = "tinbronzenails", bindingShapePath = "metal" },
            ["metalnailsandstrips-bismuthbronze"] = new() { bindingStatTag = "bismuthbronzenails", bindingShapePath = "metal" },
            ["metalnailsandstrips-blackbronze"] = new() { bindingStatTag = "blackbronzenails", bindingShapePath = "metal" },
            ["metalnailsandstrips-iron"] = new() { bindingStatTag = "ironnails", bindingShapePath = "metal" },
            ["metalnailsandstrips-cupronickel"] = new() { bindingStatTag = "cupronickelnails", bindingShapePath = "metal" },
            ["metalnailsandstrips-meteoriciron"] = new() { bindingStatTag = "meteoricironnails", bindingShapePath = "metal" },
            ["metalnailsandstrips-steel"] = new() { bindingStatTag = "steelnails", bindingShapePath = "metal" },
            ["metal-parts"] = new() { bindingStatTag = "cupronickelnails", bindingShapePath = "metal" },
            ["cordage"] = new() { bindingStatTag = "rope", bindingShapePath = "string" },
            ["sinew-dry"] = new() { bindingStatTag = "rope", bindingShapePath = "string", bindingTextureOverride = "butchering:item/resource/sinew" },
            ["glueportion-sinew-cold"] = new() { bindingStatTag = "glue" },
            ["glueportion-hide-hot"] = new() { bindingStatTag = "glue" },
            ["leatherstrips-plain"] = new() { bindingStatTag = "leather", bindingShapePath = "string" },
            ["twine-wool-plain"] = new() { bindingStatTag = "twine", bindingShapePath = "string" },
            ["twine-wool-black"] = new() { bindingStatTag = "twine", bindingShapePath = "string", bindingTextureOverride = "game:block/cloth/wool/black1" },
            ["twine-wool-brown"] = new() { bindingStatTag = "twine", bindingShapePath = "string", bindingTextureOverride = "game:block/cloth/wool/brown1" },
            ["twine-wool-gray"] = new() { bindingStatTag = "twine", bindingShapePath = "string", bindingTextureOverride = "game:block/cloth/basic/gray" },
            ["twine-wool-white"] = new() { bindingStatTag = "twine", bindingShapePath = "string", bindingTextureOverride = "game:block/cloth/wool/white1" },
            ["twine-wool-yellow"] = new() { bindingStatTag = "twine", bindingShapePath = "string", bindingTextureOverride = "game:block/cloth/basic/yellow" }
        };

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
            ["none"] = new() { id = "none", langTag = "binding-none", baseHPfactor = 0.5f, selfHPBonus = 0.0f, handleHPBonus = 0.0f, recoveryPercent = 1.0f, isMetal = false },
            ["reeds"] = new() { id = "reeds", texturePath = "game:block/cloth/reedrope", langTag = "binding-reeds", baseHPfactor = 1.0f, selfHPBonus = 0.0f, handleHPBonus = 0.0f, recoveryPercent = 1.0f, isMetal = false },
            ["twine"] = new() { id = "twine", texturePath = "game:block/cloth/basic/normal", langTag = "binding-twine", baseHPfactor = 1.2f, selfHPBonus = 0.1f, handleHPBonus = 0.05f, recoveryPercent = 0.9f, isMetal = false },
            ["rope"] = new() { id = "rope", texturePath = "game:block/cloth/basic/brown", langTag = "binding-rope", baseHPfactor = 1.25f, selfHPBonus = 0.15f, handleHPBonus = 0.05f, recoveryPercent = 0.7f, isMetal = false },
            ["leather"] = new() { id = "leather", texturePath = "game:block/leather/plain", langTag = "binding-leather", baseHPfactor = 1.5f, selfHPBonus = 0.3f, handleHPBonus = 0.1f, recoveryPercent = 0.6f, isMetal = false },
            ["glue"] = new() { id = "glue", langTag = "binding-glue", baseHPfactor = 2.0f, selfHPBonus = 0.3f, handleHPBonus = 0.3f, recoveryPercent = 1.0f, isMetal = false },
            ["coppernails"] = new() { id = "coppernails", texturePath = "game:block/metal/plate/copper", langTag = "binding-nails-copper", baseHPfactor = 1.4f, selfHPBonus = 0.1f, handleHPBonus = 0.1f, recoveryPercent = 0.9f, isMetal = true, metalType = "copper" },
            ["tinbronzenails"] = new() { id = "tinbronzenails", texturePath = "game:block/metal/plate/tinbronze", langTag = "binding-nails-tinbronze", baseHPfactor = 1.7f, selfHPBonus = 0.2f, handleHPBonus = 0.2f, recoveryPercent = 0.5f, isMetal = true, metalType = "tinbronze" },
            ["bismuthbronzenails"] = new() { id = "bismuthbronzenails", texturePath = "game:block/metal/plate/bismuthbronze", langTag = "binding-nails-bismuthbronze", baseHPfactor = 1.7f, selfHPBonus = 0.25f, handleHPBonus = 0.2f, recoveryPercent = 0.5f, isMetal = true, metalType = "bismuthbronze" },
            ["blackbronzenails"] = new() { id = "blackbronzenails", texturePath = "game:block/metal/plate/blackbronze", langTag = "binding-nails-blackbronze", baseHPfactor = 1.7f, selfHPBonus = 0.3f, handleHPBonus = 0.2f, recoveryPercent = 0.5f, isMetal = true, metalType = "blackbronze" },
            ["ironnails"] = new() { id = "ironnails", texturePath = "game:block/metal/plate/iron", langTag = "binding-nails-iron", baseHPfactor = 1.8f, selfHPBonus = 0.3f, handleHPBonus = 0.2f, recoveryPercent = 0.45f, isMetal = true, metalType = "iron" },
            ["cupronickelnails"] = new() { id = "cupronickelnails", texturePath = "game:block/metal/ingot/cupronickel", langTag = "binding-nails-cupronickel", baseHPfactor = 1.7f, selfHPBonus = 0.2f, handleHPBonus = 0.2f, recoveryPercent = 0.5f, isMetal = true, metalType = "cupronickel" },
            ["meteoricironnails"] = new() { id = "meteoricironnails", texturePath = "game:block/metal/plate/meteoriciron", langTag = "binding-nails-meteoriciron", baseHPfactor = 1.9f, selfHPBonus = 0.5f, handleHPBonus = 0.25f, recoveryPercent = 0.45f, isMetal = true, metalType = "meteoriciron" },
            ["steelnails"] = new() { id = "steelnails", texturePath = "game:block/metal/plate/steel", langTag = "binding-nails-steel", baseHPfactor = 2.2f, selfHPBonus = 0.6f, handleHPBonus = 0.4f, recoveryPercent = 0.35f, isMetal = true, metalType = "steel" }
        };
    }

    public class HandleStatPair {
        public string handleStatTag;
        public bool canHaveGrip = false;
        public string handleShapePath = "";
        public bool canBeTreated = false;
        public float dryingTimeMult = 1.0f;
    }

    public class GripStatPair {
        public string gripStatTag;
        public string gripShapePath = "";
        public string gripTextureOverride = "";
    }

    public class TreatmentStatPair {
        public string treatmentStatTag;
        public int dryingHours; //Base number of hours it takes to dry a handle, multiplied by the handle's drying time multiplier when applied.
        public bool isLiquid = false;
        public float litersUsed = 0.0f;
    }

    public class BindingStatPair {
        public string bindingStatTag;
        public string bindingShapePath = "";
        public string bindingTextureOverride = "";
    }

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
        public string texturePath = "plain";
        public string langTag = "";
        public float baseHPfactor;
        public float selfHPBonus;
        public float handleHPBonus;
        public float recoveryPercent; //If the HP is below this percent, then the binding is ruined if another part breaks
        public bool isMetal; //If true and the bindings break, try and return some bits
        public string metalType; //For ease of returning the bits, the material/metal variant of bits to return!
    }
}
