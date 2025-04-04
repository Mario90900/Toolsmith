﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Config;
using Vintagestory.API.Util;

namespace Toolsmith.Utils {
    public static class ConfigUtility {

        public static string ConfigFilename = "Toolsmith.json";
        public static string StatsFilename = "ToolsmithPartsStats.json";

        //Send a CollectibleObject's Code.toString() these to check if they are contained in the respective config. Used to assign the Behaviors at loadtime, probably better to look for the Behaviors themselves during runtime.
        //Generally they are all doing the same thing but on a different filter from the config, unless otherwise noted!
        public static bool IsToolHead(string toolHead) {
            if (toolHead == null) return false;

            var match = WildcardUtil.Match(ToolsmithModSystem.Config.ToolHeads, toolHead);
            return match;
        }

        public static bool IsToolHandle(string toolHandle, Dictionary<string, HandleWithStats>.KeyCollection keys) {
            if (toolHandle == null) return false;

            var match = keys.Contains(toolHandle);
            return match;
        }

        public static bool IsToolBinding(string toolBinding, Dictionary<string, BindingWithStats>.KeyCollection keys) {
            if (toolBinding == null) return false;

            var match = keys.Contains(toolBinding);
            return match;
        }

        public static bool IsTinkerableTool(string tool) {
            if (tool == null) return false;

            var match = WildcardUtil.Match(ToolsmithModSystem.Config.TinkerableTools, tool);
            return match;
        }

        public static bool IsSinglePartTool(string tool) {
            if (tool == null) return false;

            var match = WildcardUtil.Match(ToolsmithModSystem.Config.SinglePartTools, tool);
            return match;
        }

        public static bool IsBluntTool(string tool) {
            if (tool == null) return false;

            var match = WildcardUtil.Match(ToolsmithModSystem.Config.BluntHeadedTools, tool);
            return match;
        }

        //Similar to the ones before, but this one is checking the blacklist string instead!
        public static bool IsOnBlacklist(string tool) {
            if (tool == null) return false;

            var match = WildcardUtil.Match(ToolsmithModSystem.Config.PartBlacklist, tool);
            return match;
        }
    }
}
