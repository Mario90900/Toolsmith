using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolsmith.Utils {
    public static class ToolsmithConstants {
        public const string FallbackHeadCode = "game:candle";
        public const string DefaultHandleCode = "game:stick";

        //The keys for accessing the default part entries themselves, to recieve their stat key blocks
        public const string DefaultHandlePartKey = "stick";
        public const string DefaultBindingPartKey = "none";

        //The keys for accessing the default stat blocks for the different parts
        public const string DefaultBindingStatKey = "none";
    }
}
