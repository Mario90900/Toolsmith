using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolsmith.Config {
    public class ToolsmithConfigs {
        public bool AutoUpdateConfigsOnVersionChange = true;
        public bool AccessibilityDisableNeedToHoldClick = false;
        public bool PrintAllParsedToolsAndParts = false;
        public bool DebugMessages = false;
        public double HeadDurabilityMult = 5.0;
        public double SharpnessMult = 1.5;
        public double GrindstoneSharpenPerTick = 1;
        public double SharpWear = 0.15;
        public double BluntWear = 0.02;
        public float PercentDamageForReforge = 1.0f;
        public bool ShouldHoningDamageHead = true;
        public bool NoBitLossAlternateReforgeGen = false;
        public bool UseBitsForSmithing = true;
        public float ExtraBitVoxelChance = 0.1f;
        public string ToolHeads = "@.*(head|blade|thorn|finishingchiselhead|wedgechiselhead).*";
        public string TinkerableTools = "@.*:(axe|hammer|hoe|knife|pickaxe|prospectingpick|saw|scythe|shovel|adze|mallet|awl|chisel-finishing|chisel-wedge|rubblehammer|forestaxe|grubaxe|maul|hayfork).*";
        public string SinglePartTools = "@.*:(chisel|cleaver|shears|wrench|wedge|truechisel|rollingpin|handplaner|handwedge|laddermaker|paintbrush|paintscraper|pantograph|pathmaker|spyglass|creaser|flail|cangemchisel).*";
        public string BluntHeadedTools = "@.*:(hammer|wrench|mallet|rubblehammer|rollingpin|handwedge|laddermaker|paintbrush|pantograph|pathmaker|spyglass|creaser|flail).*";

        public string PartBlacklist = "@.*(helve|-wet-|chiseledblock|stickslayer|scrap|ruined|wfradmin|chiseled|chiselmold|wrenchmold|knifemold|armory|awl-bone|awl-horn|awl-flint|awl-obsidian|sawmill|sawbuck|sawhorse|sawdust|wooden).*";

        //Might want to move this into the Part Stats file honestly. It's getting important that probably no one should touch anything in here without knowing what they are doing.
        public Dictionary<string, HandleStatPair> BaseHandleRegistry = new() { //Both for the Handles and Bindings, it should be simple enough to just find each
            ["stick"] = new() { handleStatTag = "stick" },
            ["bone"] = new() { handleStatTag = "bone" },
            ["crudehandle"] = new() { handleStatTag = "crude", canHaveGrip = true, handleShapePath = "toolsmith:shapes/item/crudehandle" },
            ["handle"] = new() { handleStatTag = "handle", canHaveGrip = true, handleShapePath = "toolsmith:shapes/item/handle", canBeTreated = true },
            ["carpentedhandle"] = new() { handleStatTag = "professional", canHaveGrip = true, handleShapePath = "toolsmith:shapes/item/carpentedhandle", canBeTreated = true, dryingTimeMult = 2.0f }
        };

        public Dictionary<string, GripStatPair> GripRegistry = new() {
            ["flaxtwine"] = new() { gripStatTag = "twine", gripShapePath = "toolsmith:shapes/item/gripfabric" },
            ["linen-diamond-down"] = new() { gripStatTag = "cloth", gripShapePath = "toolsmith:shapes/item/gripfabric" },
            ["linen-normal-down"] = new() { gripStatTag = "cloth", gripShapePath = "toolsmith:shapes/item/gripfabric" },
            ["linen-offset-down"] = new() { gripStatTag = "cloth", gripShapePath = "toolsmith:shapes/item/gripfabric" },
            ["linen-square-down"] = new() { gripStatTag = "cloth", gripShapePath = "toolsmith:shapes/item/gripfabric" },
            ["leather-normal-plain"] = new() { gripStatTag = "leather", gripShapePath = "toolsmith:shapes/item/gripfabric" },
            ["leather-normal-orange"] = new() { gripStatTag = "leather", gripShapePath = "toolsmith:shapes/item/gripfabric", gripTextureOverride = "game:block/leather/orange" },
            ["leather-normal-black"] = new() { gripStatTag = "leather", gripShapePath = "toolsmith:shapes/item/gripfabric", gripTextureOverride = "game:block/leather/black" },
            ["leather-normal-red"] = new() { gripStatTag = "leather", gripShapePath = "toolsmith:shapes/item/gripfabric", gripTextureOverride = "game:block/leather/red" },
            ["leather-normal-blue"] = new() { gripStatTag = "leather", gripShapePath = "toolsmith:shapes/item/gripfabric", gripTextureOverride = "game:block/leather/blue" },
            ["leather-normal-purple"] = new() { gripStatTag = "leather", gripShapePath = "toolsmith:shapes/item/gripfabric", gripTextureOverride = "game:block/leather/purple" },
            ["leather-normal-pink"] = new() { gripStatTag = "leather", gripShapePath = "toolsmith:shapes/item/gripfabric", gripTextureOverride = "game:block/leather/pink" },
            ["leather-normal-white"] = new() { gripStatTag = "leather", gripShapePath = "toolsmith:shapes/item/gripfabric", gripTextureOverride = "game:block/leather/white" },
            ["leather-normal-yellow"] = new() { gripStatTag = "leather", gripShapePath = "toolsmith:shapes/item/gripfabric", gripTextureOverride = "game:block/leather/yellow" },
            ["leather-normal-gray"] = new() { gripStatTag = "leather", gripShapePath = "toolsmith:shapes/item/gripfabric", gripTextureOverride = "game:block/leather/gray" },
            ["leather-normal-green"] = new() { gripStatTag = "leather", gripShapePath = "toolsmith:shapes/item/gripfabric", gripTextureOverride = "game:block/leather/green" },
            ["leather-sturdy-plain"] = new() { gripStatTag = "sturdy", gripShapePath = "toolsmith:shapes/item/gripfabric" },
            ["twine-wool-plain"] = new() { gripStatTag = "twine", gripShapePath = "toolsmith:shapes/item/gripfabric" },
            ["twine-wool-black"] = new() { gripStatTag = "twine", gripShapePath = "toolsmith:shapes/item/gripfabric" },
            ["twine-wool-brown"] = new() { gripStatTag = "twine", gripShapePath = "toolsmith:shapes/item/gripfabric" },
            ["twine-wool-gray"] = new() { gripStatTag = "twine", gripShapePath = "toolsmith:shapes/item/gripfabric" },
            ["twine-wool-white"] = new() { gripStatTag = "twine", gripShapePath = "toolsmith:shapes/item/gripfabric" },
            ["twine-wool-yellow"] = new() { gripStatTag = "twine", gripShapePath = "toolsmith:shapes/item/gripfabric" },
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
            ["drygrass"] = new() { bindingStatTag = "reeds" },
            ["cattailtops"] = new() { bindingStatTag = "reeds" },
            ["papyrustops"] = new() { bindingStatTag = "reeds" },
            ["flaxtwine"] = new() { bindingStatTag = "twine" },
            ["rope"] = new() { bindingStatTag = "rope" },
            ["leather-normal-plain"] = new() { bindingStatTag = "leather" },
            ["leather-normal-orange"] = new() { bindingStatTag = "leather" },
            ["leather-normal-black"] = new() { bindingStatTag = "leather" },
            ["leather-normal-red"] = new() { bindingStatTag = "leather" },
            ["leather-normal-blue"] = new() { bindingStatTag = "leather" },
            ["leather-normal-purple"] = new() { bindingStatTag = "leather" },
            ["leather-normal-pink"] = new() { bindingStatTag = "leather" },
            ["leather-normal-white"] = new() { bindingStatTag = "leather" },
            ["leather-normal-yellow"] = new() { bindingStatTag = "leather" },
            ["leather-normal-gray"] = new() { bindingStatTag = "leather" },
            ["leather-normal-green"] = new() { bindingStatTag = "leather" },
            ["glueportion-pitch-hot"] = new() { bindingStatTag = "glue" }, //Okay Pitch Glue is just weird as hell in vanilla. It'll probably be changed somewhere down the line but, right now it's just weird. It's both a liquid, but ALSO a physical item? Can't pick it up normally like a liquid, but cannot store in a bucket like an item...
            ["metalnailsandstrips-copper"] = new() { bindingStatTag = "coppernails" },
            ["metalnailsandstrips-tinbronze"] = new() { bindingStatTag = "tinbronzenails" },
            ["metalnailsandstrips-bismuthbronze"] = new() { bindingStatTag = "bismuthbronzenails" },
            ["metalnailsandstrips-blackbronze"] = new() { bindingStatTag = "blackbronzenails" },
            ["metalnailsandstrips-iron"] = new() { bindingStatTag = "ironnails" },
            ["metalnailsandstrips-cupronickel"] = new() { bindingStatTag = "cupronickelnails" },
            ["metalnailsandstrips-meteoriciron"] = new() { bindingStatTag = "meteoricironnails" },
            ["metalnailsandstrips-steel"] = new() { bindingStatTag = "steelnails" },
            ["metal-parts"] = new() { bindingStatTag = "cupronickelnails" },
            ["cordage"] = new() { bindingStatTag = "rope" },
            ["glueportion-sinew-cold"] = new() { bindingStatTag = "glue" },
            ["glueportion-hide-hot"] = new() { bindingStatTag = "glue" },
            ["leatherstrips-plain"] = new() { bindingStatTag = "leather" },
            ["twine-wool-plain"] = new() { bindingStatTag = "twine" },
            ["twine-wool-black"] = new() { bindingStatTag = "twine" },
            ["twine-wool-brown"] = new() { bindingStatTag = "twine" },
            ["twine-wool-gray"] = new() { bindingStatTag = "twine" },
            ["twine-wool-white"] = new() { bindingStatTag = "twine" },
            ["twine-wool-yellow"] = new() { bindingStatTag = "twine" }
        };

        public string ModVersionNumber = "1.0.0"; //To force a reload if an old config that doesn't have this segment in it yet gains it.
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
    }
}
