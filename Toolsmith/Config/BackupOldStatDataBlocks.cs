using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolsmith.Config {
    public class BackupOldStatDataBlocks {

        //This is leftover here for the purposes of transferring old worlds and items into the new system. I think it's better to try and move over to more attributes on items for everything.
        //Less item registration overall, which will work nicer with the variations of wood. Plus, it will be easier to be more modular and add on. I knew this was coming when I made the first handles but I just didn't have the systems in place to really do this yet, now I think I can.
        public Dictionary<string, BackupHandleStats> OldBaseHandleRegistry = new() { //Both for the Handles and Bindings, it should be simple enough to just find each
            ["stick"] = new() { handleStatTag = "stick", gripStats = "plain", treatmentStats = "none" },
            ["bone"] = new() { handleStatTag = "bone", gripStats = "plain", treatmentStats = "none" },
            ["crudehandle-plain"] = new() { handleStatTag = "crude", gripStats = "plain", treatmentStats = "none" },
            ["crudehandle-twine"] = new() { handleStatTag = "crude", gripStats = "twine", treatmentStats = "none" },
            ["crudehandle-cloth"] = new() { handleStatTag = "crude", gripStats = "cloth", treatmentStats = "none" },
            ["crudehandle-leather"] = new() { handleStatTag = "crude", gripStats = "leather", treatmentStats = "none" },
            ["handle-none-finished-plain"] = new() { handleStatTag = "handle", gripStats = "plain", treatmentStats = "none" },
            ["handle-none-finished-twine"] = new() { handleStatTag = "handle", gripStats = "twine", treatmentStats = "none" },
            ["handle-none-finished-cloth"] = new() { handleStatTag = "handle", gripStats = "cloth", treatmentStats = "none" },
            ["handle-none-finished-leather"] = new() { handleStatTag = "handle", gripStats = "leather", treatmentStats = "none" },
            ["handle-none-finished-sturdy"] = new() { handleStatTag = "handle", gripStats = "sturdy", treatmentStats = "none" },
            ["handle-fat-finished-plain"] = new() { handleStatTag = "handle", gripStats = "plain", treatmentStats = "fat" },
            ["handle-fat-finished-twine"] = new() { handleStatTag = "handle", gripStats = "twine", treatmentStats = "fat" },
            ["handle-fat-finished-cloth"] = new() { handleStatTag = "handle", gripStats = "cloth", treatmentStats = "fat" },
            ["handle-fat-finished-leather"] = new() { handleStatTag = "handle", gripStats = "leather", treatmentStats = "fat" },
            ["handle-fat-finished-sturdy"] = new() { handleStatTag = "handle", gripStats = "sturdy", treatmentStats = "fat" },
            ["handle-wax-finished-plain"] = new() { handleStatTag = "handle", gripStats = "plain", treatmentStats = "wax" },
            ["handle-wax-finished-twine"] = new() { handleStatTag = "handle", gripStats = "twine", treatmentStats = "wax" },
            ["handle-wax-finished-cloth"] = new() { handleStatTag = "handle", gripStats = "cloth", treatmentStats = "wax" },
            ["handle-wax-finished-leather"] = new() { handleStatTag = "handle", gripStats = "leather", treatmentStats = "wax" },
            ["handle-wax-finished-sturdy"] = new() { handleStatTag = "handle", gripStats = "sturdy", treatmentStats = "wax" },
            ["handle-oil-finished-plain"] = new() { handleStatTag = "handle", gripStats = "plain", treatmentStats = "oil" },
            ["handle-oil-finished-twine"] = new() { handleStatTag = "handle", gripStats = "twine", treatmentStats = "oil" },
            ["handle-oil-finished-cloth"] = new() { handleStatTag = "handle", gripStats = "cloth", treatmentStats = "oil" },
            ["handle-oil-finished-leather"] = new() { handleStatTag = "handle", gripStats = "leather", treatmentStats = "oil" },
            ["handle-oil-finished-sturdy"] = new() { handleStatTag = "handle", gripStats = "sturdy", treatmentStats = "oil" },
            ["carpentedhandle-none-finished-plain"] = new() { handleStatTag = "professional", gripStats = "plain", treatmentStats = "none" },
            ["carpentedhandle-none-finished-twine"] = new() { handleStatTag = "professional", gripStats = "twine", treatmentStats = "none" },
            ["carpentedhandle-none-finished-cloth"] = new() { handleStatTag = "professional", gripStats = "cloth", treatmentStats = "none" },
            ["carpentedhandle-none-finished-leather"] = new() { handleStatTag = "professional", gripStats = "leather", treatmentStats = "none" },
            ["carpentedhandle-none-finished-sturdy"] = new() { handleStatTag = "professional", gripStats = "sturdy", treatmentStats = "none" },
            ["carpentedhandle-fat-finished-plain"] = new() { handleStatTag = "professional", gripStats = "plain", treatmentStats = "fat" },
            ["carpentedhandle-fat-finished-twine"] = new() { handleStatTag = "professional", gripStats = "twine", treatmentStats = "fat" },
            ["carpentedhandle-fat-finished-cloth"] = new() { handleStatTag = "professional", gripStats = "cloth", treatmentStats = "fat" },
            ["carpentedhandle-fat-finished-leather"] = new() { handleStatTag = "professional", gripStats = "leather", treatmentStats = "fat" },
            ["carpentedhandle-fat-finished-sturdy"] = new() { handleStatTag = "professional", gripStats = "sturdy", treatmentStats = "fat" },
            ["carpentedhandle-wax-finished-plain"] = new() { handleStatTag = "professional", gripStats = "plain", treatmentStats = "wax" },
            ["carpentedhandle-wax-finished-twine"] = new() { handleStatTag = "professional", gripStats = "twine", treatmentStats = "wax" },
            ["carpentedhandle-wax-finished-cloth"] = new() { handleStatTag = "professional", gripStats = "cloth", treatmentStats = "wax" },
            ["carpentedhandle-wax-finished-leather"] = new() { handleStatTag = "professional", gripStats = "leather", treatmentStats = "wax" },
            ["carpentedhandle-wax-finished-sturdy"] = new() { handleStatTag = "professional", gripStats = "sturdy", treatmentStats = "wax" },
            ["carpentedhandle-oil-finished-plain"] = new() { handleStatTag = "professional", gripStats = "plain", treatmentStats = "oil" },
            ["carpentedhandle-oil-finished-twine"] = new() { handleStatTag = "professional", gripStats = "twine", treatmentStats = "oil" },
            ["carpentedhandle-oil-finished-cloth"] = new() { handleStatTag = "professional", gripStats = "cloth", treatmentStats = "oil" },
            ["carpentedhandle-oil-finished-leather"] = new() { handleStatTag = "professional", gripStats = "leather", treatmentStats = "oil" },
            ["carpentedhandle-oil-finished-sturdy"] = new() { handleStatTag = "professional", gripStats = "sturdy", treatmentStats = "oil" }
        };

        //This one is also a copy of the old Binding Stats, but since it is unlikely to change exactly, only copying it over here _just_ in case. Mostly as a backup for before the overall changes. But since Bindings were already 1 for 1, item to stat block, it'll likely stay that way.
        public Dictionary<string, BindingStatPair> OldBindingRegistry = new() {
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

        public class BackupHandleStats {
            public string handleStatTag;
            public string gripStats;
            public string treatmentStats;
        }
    }
}
