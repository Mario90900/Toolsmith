using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolsmith.Utils {
    public static class ToolsmithConstants {
        public const string FallbackHeadCode = "game:candle";
        public const string DefaultHandleCode = "game:stick";
        public const float StartingSharpnessMult = 0.85f;
        public const float NonMetalStartingSharpnessMult = 0.66f;
        public const float HighSharpnessSpeedBonusMult = 0.05f;
        public const float LowSharpnessSpeedMalusMult = -0.1f;

        //The keys for accessing the default part entries themselves, to recieve their stat key blocks
        public const string DefaultHandlePartKey = "stick";
        public const string DefaultBindingPartKey = "none";

        //The keys for accessing the default stat blocks for the different parts
        public const string DefaultBindingStatKey = "none";
    }
}
