﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Toolsmith.Config;
using Toolsmith.ToolTinkering;
using Toolsmith.ToolTinkering.Behaviors;
using Toolsmith.ToolTinkering.Blocks;
using Toolsmith.ToolTinkering.Items;
using Toolsmith.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.Server;
using Vintagestory.ServerMods;

namespace Toolsmith {
    public class ToolsmithModSystem : ModSystem //TODO for Toolsmith, Grinding Stone and that functionality. Also can work on making the Handle models a bit more distinctive, too.
    {

        public static ILogger Logger;
        public static string ModId;
        public static ICoreAPI Api;
        public static Harmony HarmonyInstance;
        public static ToolsmithConfigs Config;
        public static ToolsmithPartStats Stats;
        public static int GradientSelection = 0;

        public const string ToolTinkeringPatchCategory = "toolTinkering";

        public static List<CollectibleObject> TinkerableToolsList; //This will still get populated because it's used to search the recipes later, and it saves having to re-iterate over every single object a second time.
                                                                   //I feel it's best to keep the recipe handling after the game expects them all to be loaded. Let me know if it might be better in the long run to just
                                                                   //do it immediately on first discovery - or push it all later? Wait that might not be a good idea either, cause Behaviors get assigned here-ish.
        public static List<CollectibleObject> HandleList; //Will not be populated unless PrintAllParsedToolsAndParts is true
        public static List<CollectibleObject> BindingList; //Same as HandleList above

        public static List<string> IgnoreCodes;

        public override void StartPre(ICoreAPI api) {
            Logger = Mod.Logger;
            ModId = Mod.Info.ModID;
            Api = api;
            TryToLoadConfig(api);
            TryToLoadStats(api);
            TinkerableToolsList = new List<CollectibleObject>();
            HandleList = new List<CollectibleObject>();
            BindingList = new List<CollectibleObject>();
            IgnoreCodes = new List<string>();
        }

        public override void Start(ICoreAPI api) {
            if (api.ModLoader.IsModEnabled("smithingplus")) {
                Logger.VerboseDebug("Smithing Plus found, trying to patch in attributes to the forgettable config!");
                HandleSmithingPlusStartCompat(api);
            }

            //This is important to let the Treasure Hunter Trader accept a Toolsmith Pick to get the map for Story Content! Thank you Item Rarity for also having the issue and both leaving a comment and pushing the commit not too long before I had the same problem :P
            GlobalConstants.IgnoredStackAttributes = GlobalConstants.IgnoredStackAttributes.Append(ToolsmithAttributes.ToolsmithIgnoreAttributesArray);

            api.RegisterCollectibleBehaviorClass($"{ModId}:TinkeredTools", typeof(CollectibleBehaviorTinkeredTools));
            api.RegisterCollectibleBehaviorClass($"{ModId}:ToolPartWithHealth", typeof(CollectibleBehaviorToolPartWithHealth));
            api.RegisterCollectibleBehaviorClass($"{ModId}:ToolNoDamageWithUse", typeof(CollectibleBehaviorToolBlunt));
            api.RegisterCollectibleBehaviorClass($"{ModId}:SmithedTool", typeof(CollectibleBehaviorSmithedTools));
            api.RegisterCollectibleBehaviorClass($"{ModId}:ToolHead", typeof(CollectibleBehaviorToolHead));
            api.RegisterCollectibleBehaviorClass($"{ModId}:ToolHandle", typeof(CollectibleBehaviorToolHandle));
            api.RegisterCollectibleBehaviorClass($"{ModId}:ToolBinding", typeof(CollectibleBehaviorToolBinding));
            api.RegisterBlockEntityClass($"{ModId}:EntityGrindstone", typeof(BlockEntityGrindstone));
            api.RegisterBlockClass($"{ModId}:BlockGrindstone", typeof(BlockGrindstone));
            api.RegisterItemClass($"{ModId}:ItemWhetstone", typeof(ItemWhetstone));
            api.RegisterItemClass($"{ModId}:ItemTinkerToolParts", typeof(ItemTinkerToolParts));
            HarmonyPatch();
        }

        public override void StartServerSide(ICoreServerAPI api) {
            
        }

        public override void StartClientSide(ICoreClientAPI api) {
            GradientSelection = 0;
            TinkeringUtility.InitializeSharpnessColorGradient();
        }

        public override void AssetsFinalize(ICoreAPI api) {
            base.AssetsFinalize(api);
            if (api.Side.IsClient()) return;
            
            if (Config.PrintAllParsedToolsAndParts) {
                Logger.Debug("Single Part Tools:");
            }
            var handleKeys = Config.ToolHandlesWithStats.Keys;
            var bindingKeys = Config.BindingsWithStats.Keys;
            foreach (var t in api.World.Collectibles.Where(t => t?.Code != null)) { //A tool/part should likely be only one of these!
                if (ConfigUtility.IsTinkerableTool(t.Code.ToString()) && !(ConfigUtility.IsToolHead(t.Code.ToString())) && !(ConfigUtility.IsOnBlacklist(t.Code.ToString()))) { //Any tool that you actually craft from a Tool Head to create!
                    if (!t.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                        t.AddBehavior<CollectibleBehaviorTinkeredTools>();
                    }
                    if (ConfigUtility.IsBluntTool(t.Code.ToString())) { //Any tinkered tool can still be one that's 'blunt', IE a Hammer in this case
                        if (!t.HasBehavior<CollectibleBehaviorToolBlunt>()) {
                            t.AddBehavior<CollectibleBehaviorToolBlunt>();
                        }
                    }
                    TinkerableToolsList.Add(t);
                } else if (ConfigUtility.IsSinglePartTool(t.Code.ToString()) && !(ConfigUtility.IsOnBlacklist(t.Code.ToString()))) { //A 'Smithed' tool is one that once you finish the anvil smithing recipe, the tool is done. Shears, Wrench, or Chisel in vanilla! Add the 'Smithed' Tool Behavior so they can gain the Grinding interaction to maintain them.
                    if (!t.HasBehavior<CollectibleBehaviorSmithedTools>()) {
                        t.AddBehavior<CollectibleBehaviorSmithedTools>();
                    }
                    if (ConfigUtility.IsBluntTool(t.Code.ToString())) { //Any smithed tool can still be one that's 'blunt', IE a Wrench in this case
                        if (!t.HasBehavior<CollectibleBehaviorToolBlunt>()) {
                            t.AddBehavior<CollectibleBehaviorToolBlunt>();
                        }
                    }
                    if (Config.PrintAllParsedToolsAndParts) {
                        Logger.Debug(t.Code.ToString());
                    }
                } else if (ConfigUtility.IsToolHandle(t.Code.Path, handleKeys)) { //Probably don't need the blacklist anymore, since can assume the configs have the exact Path
                    if (!t.HasBehavior<CollectibleBehaviorToolHandle>()) {
                        t.AddBehavior<CollectibleBehaviorToolHandle>();
                    }
                    if (Config.PrintAllParsedToolsAndParts) { //Both Handles and Bindings don't really need to populate these lists anymore unless we are looking to actually print everything found. Should help to save some time and ram?
                        HandleList.Add(t);
                    }
                } else if (ConfigUtility.IsToolBinding(t.Code.Path, bindingKeys)) {
                    if (!t.HasBehavior<CollectibleBehaviorToolBinding>()) {
                        t.AddBehavior<CollectibleBehaviorToolBinding>();
                        t.StorageFlags += 0x100;
                    }
                    if (Config.PrintAllParsedToolsAndParts) {
                        BindingList.Add(t);
                    }
                }
            }

            if (Config.PrintAllParsedToolsAndParts) { //Mainly left in for debugging purposes since it's kinda useful to just let it run through everything and see what might be going wrong and where... Especially when adding other mods
                Logger.Debug("Tinkerable Tools:");
                foreach (var t in TinkerableToolsList) {
                    Logger.Debug(t.Code.ToString());
                }
                Logger.Debug("Tool Handles:");
                foreach (var t in HandleList) {
                    Logger.Debug(t.Code.ToString());
                }
                Logger.Debug("Tool Bindings:");
                foreach (var t in BindingList) {
                    Logger.Debug(t.Code.ToString());
                }
            }
        }

        private void TryToLoadConfig(ICoreAPI api) { //Created following the tutorial on the Wiki!
            try {
                Config = api.LoadModConfig<ToolsmithConfigs>(ConfigUtility.ConfigFilename);
                if (Config == null) {
                    Config = new ToolsmithConfigs();
                }
                api.StoreModConfig<ToolsmithConfigs>(Config, ConfigUtility.ConfigFilename);
            } catch (Exception e) {
                Mod.Logger.Error("Could not load config, using default settings instead!");
                Mod.Logger.Error(e);
                Config = new ToolsmithConfigs();
            }
        }

        private void TryToLoadStats(ICoreAPI api) {
            try {
                Stats = api.LoadModConfig<ToolsmithPartStats>(ConfigUtility.StatsFilename);
                if (Stats == null) {
                    Stats = new ToolsmithPartStats();
                }
                api.StoreModConfig<ToolsmithPartStats>(Stats, ConfigUtility.StatsFilename);
            } catch (Exception e) {
                Mod.Logger.Error("Could not load stats, using default settings instead!");
                Mod.Logger.Error(e);
                Stats = new ToolsmithPartStats();
            }
        }

        private void HandleSmithingPlusStartCompat(ICoreAPI api) {
            SmithingPlus.Core SPCore = api.ModLoader.GetModSystem<SmithingPlus.Core>();
            if (SPCore != null) {
                if (!SmithingPlus.Core.Config.GetToolRepairForgettableAttributes.Contains<string>("tinkeredToolHead")) {
                    SmithingPlus.Core.Config.ToolRepairForgettableAttributes = SmithingPlus.Core.Config.ToolRepairForgettableAttributes + ToolsmithAttributes.ToolsmithForgettableAttributes;
                    Logger.VerboseDebug("Added Toolsmith Attributes to Smithing Plus's Forgettable Attributes config!");
                } else {
                    Logger.VerboseDebug("Found possible presence of existing configs already in Smithing Plus for Toolsmith, forgoing the addition! If you have issues, please reset the ToolRepairForgettableAttributes line in the Smithing Plus config.");
                }
            } else {
                Logger.Error("Found Smithing Plus loaded, but could not retrieve the Core ModLoader for it! Auto compatability will not work.");
            }
        }

        private static void HarmonyPatch() {
            if (HarmonyInstance != null) {
                return;
            }
            HarmonyInstance = new Harmony(ModId);
            Logger.VerboseDebug("Harmony is starting Patches!");
            HarmonyInstance.PatchCategory(ToolTinkeringPatchCategory);
            Logger.VerboseDebug("Patched functions for Tool Tinkering purposes.");
        }

        private static void HarmonyUnpatch() {
            Logger?.VerboseDebug("Unpatching Harmony Patches.");
            HarmonyInstance?.UnpatchAll(ModId);
            HarmonyInstance = null;
        }

        public override void Dispose() {
            HarmonyUnpatch();
            Logger = null;
            ModId = null;
            Api = null;
            Config = null;
            Stats = null;
            TinkerableToolsList = null;
            HandleList = null;
            BindingList = null;
            IgnoreCodes = null;
            base.Dispose();
        }
    }
}
