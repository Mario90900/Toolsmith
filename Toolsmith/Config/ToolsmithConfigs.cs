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
        public bool EnableOptionalGridCraftingForTools = false;
        public float ExtraBitVoxelChance = 0.1f;

        public string ToolHeads = "@.*(head|blade|thorn|finishingchiselhead|wedgechiselhead|toolhead).*"; //This will catch Weapon Heads as well, so, filter them by the result of the recipe that crafts them.
        public string TinkerableTools = "@.*:(axe-felling|hammer|hoe|knife|pickaxe|prospectingpick|saw|scythe|shovel|adze|mallet|awl|chisel-finishing|chisel-wedge|rubblehammer|forestaxe|grubaxe|maul|hayfork|bonepickaxe|huntingknife|paxel|chiselpick).*";
        public string SinglePartTools = "@.*:(chisel|cleaver|shears|wrench|wedge|truechisel|rollingpin|handplaner|handwedge|laddermaker|paintbrush|paintscraper|pantograph|pathmaker|spyglass|creaser|flail|cangemchisel).*";
        public string BluntHeadedTools = "@.*:(hammer|wrench|mallet|rubblehammer|rollingpin|handwedge|laddermaker|paintbrush|pantograph|pathmaker|spyglass|creaser|flail).*";

        public string SimpleWeapons = "@.*:(mace).*"; //Warhammer is the Steel Mace, but Polehammer is the Steel Club, both use the same head? Maybe just disable the Polehammer.
        public string MartialWeapons = "@.*:(blade|sabre|sword).*";
        public string LongWeapons = "@.*:(spear|axe-long|javelin|pike|halberd).*";
        public string ImprovisedWeapons = "@.*:(club|quarterstaff).*";

        public string PartBlacklist = "@.*(helve|-wet-|chiseledblock|stickslayer|gold|silver|scrap|ruined|wfradmin|chiseled|chiselmold|wrenchmold|knifemold|awl-bone|awl-horn|awl-flint|awl-obsidian|sawmill|sawbuck|sawhorse|sawdust|wooden).*";

        public string ModVersionNumber = "1.0.0"; //To force a reload if an old config that doesn't have this segment in it yet gains it.
    }
}
