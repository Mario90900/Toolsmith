using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolsmith.Utils {
    public static class ToolsmithConstants {
        public const string FallbackHeadCode = "game:candle";
        public const string DefaultHandleCode = "game:stick";
        public const string DefaultGripTag = "plain";
        public const string DefaultTreatmentTag = "none";
        public const string ToolBundleCode = "toolsmith:tinkertoolparts";
        public const string HandleBlankCode = "supportbeam"; //As in, the 'blank' that is crafted into a handle! If that recipe of Knife + this changes, make sure to change this!
        public const string SandpaperCode = "toolsmith:sandpaper";
        public const string FirewoodCode = "game:firewood";
        public const string DebarkedWoodPathMinusType = "game:block/wood/debarked/";
        public const string DefaultGripFallbackTexture = "game:block/cloth/reedrope";
        public const string LightTreatementOverlayPath = "toolsmith:textures/item/woodtints/light";
        public const string DarkTreatementOverlayPath = "toolsmith:textures/item/woodtints/dark";

        public const float TimeToCraftTinkerTool = 2.5f;
        public const float StartingSharpnessMult = 0.85f;
        public const float NonMetalStartingSharpnessMult = 0.66f;
        public const float HighSharpnessSpeedBonusMult = 0.05f;
        public const float LowSharpnessSpeedMalusMult = -0.1f;
        public const float sharpenInterval = 0.4f;
        public const int NumBitsReturnMinimum = 2;

        public const string ModularPartRenderingFromAttributesMeshRefs = "ToolsmithModularPartRenderingMeshRefs";
        public const string HandleRenderingDataRef = "ToolsmithHandleRenderingDataRef";
        public const string MultiPartToolMeshRefs = "ToolsmithMultiPartToolMeshRefs";

        //The keys for accessing the default part entries themselves, to recieve their stat key blocks
        public const string DefaultHandlePartKey = "stick";
        public const string DefaultBindingPartKey = "none";

        //The keys for accessing the default stat blocks for the different parts
        public const string DefaultBindingStatKey = "none";
    }
}
