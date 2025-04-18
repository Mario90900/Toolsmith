using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolsmith.Config {
    public class ToolsmithConfigs { //TODO for tomorrow: Time to refactor the original config file woo. Probably can/should clean this up...
        public bool PrintAllParsedToolsAndParts = false;
        public bool DebugMessages = false;
        public double HeadDurabilityMult = 5.0;
        public double SharpnessMult = 1.5;
        public int GrindstoneSharpenPerTick = 1;
        public double BluntWear = 0.02;
        public string ToolHeads = "@.*(head|blade|thorn|finishingchiselhead|wedgechiselhead).*";
        public string TinkerableTools = "@.*:(axe|hammer|hoe|knife|pickaxe|prospectingpick|saw|scythe|shovel|adze|mallet|awl|chisel-finishing|chisel-wedge|rubblehammer|forestaxe|grubaxe|maul|hayfork).*";
        public string SinglePartTools = "@.*:(chisel|cleaver|shears|wrench|wedge|truechisel|rollingpin|handplaner|handwedge|laddermaker|paintbrush|paintscraper|pantograph|pathmaker|spyglass|creaser).*";
        public string BluntHeadedTools = "@.*:(hammer|wrench|mallet|rubblehammer|rollingpin|handwedge|laddermaker|paintbrush|pantograph|pathmaker|spyglass|creaser).*";

        public string PartBlacklist = "@.*(helve|-bone|-bone-|-wet-|chiseledblock|stickslayer|scrap|ruined|wfradmin|chiseled|chiselmold|wrenchmold|armory|awl-bone|awl-horn|awl-flint|awl-obsidian|sawbuck|sawhorse|sawdust|wooden).*";

        public Dictionary<string, HandleWithStats> ToolHandlesWithStats = new() { //Both for the Handles and Bindings, it should be simple enough to just find each
            ["stick"] = new() { handleStats = "stick", gripStats = "plain", treatmentStats = "none" },
            ["bone"] = new() { handleStats = "bone", gripStats = "plain", treatmentStats = "none" },
            ["crudehandle-plain"] = new() { handleStats = "crude", gripStats = "plain", treatmentStats = "none" },
            ["crudehandle-twine"] = new() { handleStats = "crude", gripStats = "twine", treatmentStats = "none" },
            ["crudehandle-cloth"] = new() { handleStats = "crude", gripStats = "cloth", treatmentStats = "none" },
            ["crudehandle-leather"] = new() { handleStats = "crude", gripStats = "leather", treatmentStats = "none" },
            ["handle-none-finished-plain"] = new() { handleStats = "handle", gripStats = "plain", treatmentStats = "none" },
            ["handle-none-finished-twine"] = new() { handleStats = "handle", gripStats = "twine", treatmentStats = "none" },
            ["handle-none-finished-cloth"] = new() { handleStats = "handle", gripStats = "cloth", treatmentStats = "none" },
            ["handle-none-finished-leather"] = new() { handleStats = "handle", gripStats = "leather", treatmentStats = "none" },
            ["handle-none-finished-sturdy"] = new() { handleStats = "handle", gripStats = "sturdy", treatmentStats = "none" },
            ["handle-fat-finished-plain"] = new() { handleStats = "handle", gripStats = "plain", treatmentStats = "fat" },
            ["handle-fat-finished-twine"] = new() { handleStats = "handle", gripStats = "twine", treatmentStats = "fat" },
            ["handle-fat-finished-cloth"] = new() { handleStats = "handle", gripStats = "cloth", treatmentStats = "fat" },
            ["handle-fat-finished-leather"] = new() { handleStats = "handle", gripStats = "leather", treatmentStats = "fat" },
            ["handle-fat-finished-sturdy"] = new() { handleStats = "handle", gripStats = "sturdy", treatmentStats = "fat" },
            ["handle-wax-finished-plain"] = new() { handleStats = "handle", gripStats = "plain", treatmentStats = "wax" },
            ["handle-wax-finished-twine"] = new() { handleStats = "handle", gripStats = "twine", treatmentStats = "wax" },
            ["handle-wax-finished-cloth"] = new() { handleStats = "handle", gripStats = "cloth", treatmentStats = "wax" },
            ["handle-wax-finished-leather"] = new() { handleStats = "handle", gripStats = "leather", treatmentStats = "wax" },
            ["handle-wax-finished-sturdy"] = new() { handleStats = "handle", gripStats = "sturdy", treatmentStats = "wax" },
            ["handle-oil-finished-plain"] = new() { handleStats = "handle", gripStats = "plain", treatmentStats = "oil" },
            ["handle-oil-finished-twine"] = new() { handleStats = "handle", gripStats = "twine", treatmentStats = "oil" },
            ["handle-oil-finished-cloth"] = new() { handleStats = "handle", gripStats = "cloth", treatmentStats = "oil" },
            ["handle-oil-finished-leather"] = new() { handleStats = "handle", gripStats = "leather", treatmentStats = "oil" },
            ["handle-oil-finished-sturdy"] = new() { handleStats = "handle", gripStats = "sturdy", treatmentStats = "oil" },
            ["carpentedhandle-none-finished-plain"] = new() { handleStats = "professional", gripStats = "plain", treatmentStats = "none" },
            ["carpentedhandle-none-finished-twine"] = new() { handleStats = "professional", gripStats = "twine", treatmentStats = "none" },
            ["carpentedhandle-none-finished-cloth"] = new() { handleStats = "professional", gripStats = "cloth", treatmentStats = "none" },
            ["carpentedhandle-none-finished-leather"] = new() { handleStats = "professional", gripStats = "leather", treatmentStats = "none" },
            ["carpentedhandle-none-finished-sturdy"] = new() { handleStats = "professional", gripStats = "sturdy", treatmentStats = "none" },
            ["carpentedhandle-fat-finished-plain"] = new() { handleStats = "professional", gripStats = "plain", treatmentStats = "fat" },
            ["carpentedhandle-fat-finished-twine"] = new() { handleStats = "professional", gripStats = "twine", treatmentStats = "fat" },
            ["carpentedhandle-fat-finished-cloth"] = new() { handleStats = "professional", gripStats = "cloth", treatmentStats = "fat" },
            ["carpentedhandle-fat-finished-leather"] = new() { handleStats = "professional", gripStats = "leather", treatmentStats = "fat" },
            ["carpentedhandle-fat-finished-sturdy"] = new() { handleStats = "professional", gripStats = "sturdy", treatmentStats = "fat" },
            ["carpentedhandle-wax-finished-plain"] = new() { handleStats = "professional", gripStats = "plain", treatmentStats = "wax" },
            ["carpentedhandle-wax-finished-twine"] = new() { handleStats = "professional", gripStats = "twine", treatmentStats = "wax" },
            ["carpentedhandle-wax-finished-cloth"] = new() { handleStats = "professional", gripStats = "cloth", treatmentStats = "wax" },
            ["carpentedhandle-wax-finished-leather"] = new() { handleStats = "professional", gripStats = "leather", treatmentStats = "wax" },
            ["carpentedhandle-wax-finished-sturdy"] = new() { handleStats = "professional", gripStats = "sturdy", treatmentStats = "wax" },
            ["carpentedhandle-oil-finished-plain"] = new() { handleStats = "professional", gripStats = "plain", treatmentStats = "oil" },
            ["carpentedhandle-oil-finished-twine"] = new() { handleStats = "professional", gripStats = "twine", treatmentStats = "oil" },
            ["carpentedhandle-oil-finished-cloth"] = new() { handleStats = "professional", gripStats = "cloth", treatmentStats = "oil" },
            ["carpentedhandle-oil-finished-leather"] = new() { handleStats = "professional", gripStats = "leather", treatmentStats = "oil" },
            ["carpentedhandle-oil-finished-sturdy"] = new() { handleStats = "professional", gripStats = "sturdy", treatmentStats = "oil" }
        };

        public Dictionary<string, BindingWithStats> BindingsWithStats = new() {
            ["flaxtwine"] = new() { bindingStats = "twine" },
            ["rope"] = new() { bindingStats = "rope" },
            ["leather-normal-plain"] = new() { bindingStats = "leather" },
            ["leather-normal-orange"] = new() { bindingStats = "leather" },
            ["leather-normal-black"] = new() { bindingStats = "leather" },
            ["leather-normal-red"] = new() { bindingStats = "leather" },
            ["leather-normal-blue"] = new() { bindingStats = "leather" },
            ["leather-normal-purple"] = new() { bindingStats = "leather" },
            ["leather-normal-pink"] = new() { bindingStats = "leather" },
            ["leather-normal-white"] = new() { bindingStats = "leather" },
            ["leather-normal-yellow"] = new() { bindingStats = "leather" },
            ["leather-normal-gray"] = new() { bindingStats = "leather" },
            ["leather-normal-green"] = new() { bindingStats = "leather" },
            ["glueportion-pitch-hot"] = new() { bindingStats = "glue" }, //Okay Pitch Glue is just weird as hell. It'll probably be changed somewhere down the line but, right now it's just weird.
            ["metalnailsandstrips-tinbronze"] = new() { bindingStats = "tinbronzenails" },
            ["metalnailsandstrips-bismuthbronze"] = new() { bindingStats = "bismuthbronzenails" },
            ["metalnailsandstrips-blackbronze"] = new() { bindingStats = "blackbronzenails" },
            ["metalnailsandstrips-iron"] = new() { bindingStats = "ironnails" },
            ["metalnailsandstrips-cupronickel"] = new() { bindingStats = "cupronickelnails" },
            ["metalnailsandstrips-meteoriciron"] = new() { bindingStats = "meteoricironnails" },
            ["metalnailsandstrips-steel"] = new() { bindingStats = "steelnails" },
            ["metal-parts"] = new() { bindingStats = "cupronickelnails" },
            ["cordage"] = new() { bindingStats = "rope" },
            ["glueportion-sinew-cold"] = new() { bindingStats = "glue" },
            ["glueportion-hide-hot"] = new() { bindingStats = "glue" },
            ["leatherstrips-plain"] = new() { bindingStats = "leather" },
            ["twine-wool-plain"] = new() { bindingStats = "twine" },
            ["twine-wool-black"] = new() { bindingStats = "twine" },
            ["twine-wool-brown"] = new() { bindingStats = "twine" },
            ["twine-wool-gray"] = new() { bindingStats = "twine" },
            ["twine-wool-white"] = new() { bindingStats = "twine" },
            ["twine-wool-yellow"] = new() { bindingStats = "twine" }
        };
    }

    public class HandleWithStats {
        public string handleStats;
        public string gripStats;
        public string treatmentStats;
    }

    public class BindingWithStats {
        public string bindingStats;
    }
}