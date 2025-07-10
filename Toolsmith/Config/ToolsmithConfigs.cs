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
        public double HoningDamageMult = 1.0;
        public double SharpWear = 0.15;
        public double BluntWear = 0.02;
        public float PercentDamageForReforge = 1.0f;
        public bool ShouldHoningDamageHead = true;
        public bool NoBitLossAlternateReforgeGen = false;
        public bool UseBitsForSmithing = true;
        public float ExtraBitVoxelChance = 0.1f;
        public string ToolHeads = "@.*(head|blade|thorn|finishingchiselhead|wedgechiselhead|toolhead).*";
        public string TinkerableTools = "@.*:(axe|hammer|hoe|knife|pickaxe|prospectingpick|saw|scythe|shovel|adze|mallet|awl|chisel-finishing|chisel-wedge|rubblehammer|forestaxe|grubaxe|maul|hayfork|bonepickaxe|huntingknife|paxel|chiselpick).*";
        public string SinglePartTools = "@.*:(chisel|cleaver|shears|wrench|wedge|truechisel|rollingpin|handplaner|handwedge|laddermaker|paintbrush|paintscraper|pantograph|pathmaker|spyglass|creaser|flail|cangemchisel).*";
        public string BluntHeadedTools = "@.*:(hammer|wrench|mallet|rubblehammer|rollingpin|handwedge|laddermaker|paintbrush|pantograph|pathmaker|spyglass|creaser|flail).*";

        public string PartBlacklist = "@.*(helve|-wet-|chiseledblock|stickslayer|scrap|ruined|wfradmin|chiseled|chiselmold|wrenchmold|knifemold|armory|awl-bone|awl-horn|awl-flint|awl-obsidian|sawmill|sawbuck|sawhorse|sawdust|wooden).*";

        //Might want to move this into the Part Stats file honestly. It's getting important that probably no one should touch anything in here without knowing what they are doing.
        public Dictionary<string, HandleStatPair> BaseHandleRegistry = new() { //Both for the Handles and Bindings, it should be simple enough to just find each
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
        public string bindingShapePath = "";
        public string bindingTextureOverride = "";
    }
}
