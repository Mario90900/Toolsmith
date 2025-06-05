using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Toolsmith.Utils {
    public static class ConfigUtility {

        public static string ConfigFilename = "Toolsmith.json";
        public static string ClientConfigFilename = "ToolsmithClient.json";
        public static string StatsFilename = "ToolsmithPartsStats.json";
        public static string WeaponStatsFilename = "ToolsmithWeaponStats.json";

        public const string AnyWildcardStringPrefix = "@.*("; //For the configs that don't want just the first part of the code, matching the Heads or the Blacklist mainly.
        public const string AnyFirstCodeStartStringPrefix = "@.*:("; //The start of the configs that look for the first part of the code for comparison purposes. Mostly looking for the actual tool/weapon codes!
        public const string ConfigStringPostfix = ").*"; //All config strings end with this! Anything can come after the entered strings.
        public const string ConfigEntrySeparator = "|"; //All entries are separated by this between the Prefix and Postfix!

        public static List<string> SplitToolHeadsConfig;
        public static List<string> SplitTinkerableToolsConfig;
        public static List<string> SplitSmithedToolsConfig;
        public static List<string> SplitBluntToolsConfig;
        public static List<string> SplitBlacklistConfig;

        //Send a CollectibleObject's Code.toString() these to check if they are contained in the respective config. Used to assign the Behaviors at loadtime, probably better to look for the Behaviors themselves during runtime.
        //Generally they are all doing the same thing but on a different filter from the config, unless otherwise noted!
        public static bool IsToolHead(string toolHead) {
            if (toolHead == null) return false;

            var match = WildcardUtil.Match(ToolsmithModSystem.Config.ToolHeads, toolHead);
            return match;
        }

        public static bool IsToolHandle(string toolHandle, Dictionary<string, HandleStatPair>.KeyCollection keys) {
            if (toolHandle == null) return false;

            var match = keys.Contains(toolHandle);
            return match;
        }

        public static bool IsToolBinding(string toolBinding, Dictionary<string, BindingStatPair>.KeyCollection keys) {
            if (toolBinding == null) return false;

            var match = keys.Contains(toolBinding);
            return match;
        }

        public static bool IsValidGripMaterial(string gripMat, Dictionary<string, GripStatPair>.KeyCollection keys) {
            if (gripMat == null) return false;

            var match = keys.Contains(gripMat);
            return match;
        }

        public static bool IsValidTreatmentMaterial(string treatmentMat, Dictionary<string,  TreatmentStatPair>.KeyCollection keys) {
            if (treatmentMat == null) return false;

            var match = keys.Contains(treatmentMat);
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

        public static void PrepareAndSplitConfigStrings() {
            var dirtyHeadsSplit = ToolsmithModSystem.Config.ToolHeads.Split('|');
            var dirtyTinkerSplit = ToolsmithModSystem.Config.TinkerableTools.Split("|");
            var dirtySmithedSplit = ToolsmithModSystem.Config.SinglePartTools.Split("|");
            var dirtyBluntSplit = ToolsmithModSystem.Config.BluntHeadedTools.Split("|");
            var dirtyBlacklistSplit = ToolsmithModSystem.Config.PartBlacklist.Split("|");

            var workingEntry = dirtyHeadsSplit[0];
            dirtyHeadsSplit[0] = workingEntry.Split('(').Last();
            workingEntry = dirtyTinkerSplit[0];
            dirtyTinkerSplit[0] = workingEntry.Split('(').Last();
            workingEntry = dirtySmithedSplit[0];
            dirtySmithedSplit[0] = workingEntry.Split('(').Last();
            workingEntry = dirtyBluntSplit[0];
            dirtyBluntSplit[0] = workingEntry.Split('(').Last();
            workingEntry = dirtyBlacklistSplit[0];
            dirtyBlacklistSplit[0] = workingEntry.Split('(').Last();

            var count = dirtyHeadsSplit.Length;
            workingEntry = dirtyHeadsSplit[count - 1];
            dirtyHeadsSplit[count - 1] = workingEntry.Split(')').First();
            count = dirtyTinkerSplit.Length;
            workingEntry = dirtyTinkerSplit[count - 1];
            dirtyTinkerSplit[count - 1] = workingEntry.Split(')').First();
            count = dirtySmithedSplit.Length;
            workingEntry = dirtySmithedSplit[count - 1];
            dirtySmithedSplit[count - 1] = workingEntry.Split(')').First();
            count = dirtyBluntSplit.Length;
            workingEntry = dirtyBluntSplit[count - 1];
            dirtyBluntSplit[count - 1] = workingEntry.Split(')').First();
            count = dirtyBlacklistSplit.Length;
            workingEntry = dirtyBlacklistSplit[count - 1];
            dirtyBlacklistSplit[count - 1] = workingEntry.Split(')').First();

            SplitToolHeadsConfig = dirtyHeadsSplit.ToList();
            SplitTinkerableToolsConfig = dirtyTinkerSplit.ToList();
            SplitSmithedToolsConfig = dirtySmithedSplit.ToList();
            SplitBluntToolsConfig = dirtyBluntSplit.ToList();
            SplitBlacklistConfig = dirtyBlacklistSplit.ToList();
        }

        public static void MergeAndSetConfigStrings() {
            string completeHeads = AnyWildcardStringPrefix;
            string completeTinkerTools = AnyFirstCodeStartStringPrefix;
            string completeSmithedTools = AnyFirstCodeStartStringPrefix;
            string completeBluntTools = AnyFirstCodeStartStringPrefix;
            string completeBlacklist = AnyWildcardStringPrefix;

            for (int i = 0; i < SplitToolHeadsConfig.Count; i++) {
                completeHeads += SplitToolHeadsConfig[i];
                if (i < SplitToolHeadsConfig.Count - 1) {
                    completeHeads += ConfigEntrySeparator;
                }
            }
            completeHeads += ConfigStringPostfix;

            for (int i = 0; i < SplitTinkerableToolsConfig.Count; i++) {
                completeTinkerTools += SplitTinkerableToolsConfig[i];
                if (i < SplitTinkerableToolsConfig.Count - 1) {
                    completeTinkerTools += ConfigEntrySeparator;
                }
            }
            completeTinkerTools += ConfigStringPostfix;

            for (int i = 0; i < SplitSmithedToolsConfig.Count; i++) {
                completeSmithedTools += SplitSmithedToolsConfig[i];
                if (i < SplitSmithedToolsConfig.Count - 1) {
                    completeSmithedTools += ConfigEntrySeparator;
                }
            }
            completeSmithedTools += ConfigStringPostfix;

            for (int i = 0; i < SplitBluntToolsConfig.Count; i++) {
                completeBluntTools += SplitBluntToolsConfig[i];
                if (i < SplitBluntToolsConfig.Count - 1) {
                    completeBluntTools += ConfigEntrySeparator;
                }
            }
            completeBluntTools += ConfigStringPostfix;

            for (int i = 0; i < SplitBlacklistConfig.Count; i++) {
                completeBlacklist += SplitBlacklistConfig[i];
                if (i < SplitBlacklistConfig.Count - 1) {
                    completeBlacklist += ConfigEntrySeparator;
                }
            }
            completeBlacklist += ConfigStringPostfix;

            if (ToolsmithModSystem.Config.DebugMessages) {
                ToolsmithModSystem.Logger.Warning("The complete config strings before setting them are: ");
                ToolsmithModSystem.Logger.Warning("Heads: " + completeHeads);
                ToolsmithModSystem.Logger.Warning("Tinker Tools: " + completeTinkerTools);
                ToolsmithModSystem.Logger.Warning("Smithed Tools: " + completeSmithedTools);
                ToolsmithModSystem.Logger.Warning("Blunt Tools: " + completeBluntTools);
                ToolsmithModSystem.Logger.Warning("Blacklist: " + completeBlacklist);
            }

            ToolsmithModSystem.Config.ToolHeads = completeHeads;
            ToolsmithModSystem.Config.TinkerableTools = completeTinkerTools;
            ToolsmithModSystem.Config.SinglePartTools = completeSmithedTools;
            ToolsmithModSystem.Config.BluntHeadedTools = completeBluntTools;
            ToolsmithModSystem.Config.PartBlacklist = completeBlacklist;
        }

        public static void AddEntryToToolHeadsConfig(string entry) {
            SplitToolHeadsConfig.Add(entry);
        }

        public static void AddEntryToTinkeredToolsConfig(string entry) {
            SplitTinkerableToolsConfig.Add(entry);
        }

        public static void AddEntryToSmithedToolsConfig(string entry) {
            SplitSmithedToolsConfig.Add(entry);
        }

        public static void AddEntryToBluntToolsConfig(string entry) {
            SplitBluntToolsConfig.Add(entry);
        }

        public static void AddEntryToBlacklistConfig(string entry) {
            SplitBlacklistConfig.Add(entry);
        }
    }
}
