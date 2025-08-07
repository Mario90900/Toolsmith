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
        public bool RunFullJsonVerifying = false;
        public bool EnableGridRecipesForToolCrafting = false;
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

        public bool EnableEditsForRegex = false;
        public string ToolHeads = ""; /*"@.*(head|blade|thorn|finishingchiselhead|wedgechiselhead|toolhead).*";*/
        public string TinkerableTools = ""; /*"@.*:(axe|hammer|hoe|knife|pickaxe|prospectingpick|saw|scythe|shovel|adze|mallet|awl|chisel-finishing|chisel-wedge|rubblehammer|forestaxe|grubaxe|maul|hayfork|bonepickaxe|huntingknife|paxel|chiselpick).*";*/
        public string SinglePartTools = ""; /*"@.*:(chisel|cleaver|shears|wrench|wedge|rollingpin|truechisel|handplaner|handwedge|laddermaker|paintbrush|paintscraper|pantograph|pathmaker|spyglass|creaser|flail|cangemchisel).*";*/
        public string BluntHeadedTools = ""; /*"@.*:(hammer|wrench|mallet|rubblehammer|rollingpin|handwedge|laddermaker|paintbrush|pantograph|pathmaker|spyglass|creaser|flail).*";*/

        public string PartBlacklist = ""; /*"@.*(helve|-wet-|chiseledblock|stickslayer|scrap|ruined|wfradmin|chiseled|chiselmold|wrenchmold|knifemold|armory|awl-bone|awl-horn|awl-flint|awl-obsidian|sawmill|sawbuck|sawhorse|sawdust|wooden).*";*/

        public string ModVersionNumber = "1.0.0"; //To force a reload if an old config that doesn't have this segment in it yet gains it.
    }

    public static class ToolsmithConfigsHelpers {

        public static void ResetRegexStrings(ref ToolsmithConfigs config) {
            config.ToolHeads = "";
            config.TinkerableTools = "";
            config.SinglePartTools = "";
            config.BluntHeadedTools = "";
            config.PartBlacklist = "";
        }

        public static void AddToRegexString(List<string> entries, ref string regexString) {
            for (int i = 0; i < entries.Count; i++) {
                regexString += entries[i] + "|";
            }
        }
    }
}
