using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Client;
using Toolsmith.Client.Behaviors;
using Toolsmith.ToolTinkering.Behaviors;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Toolsmith.Server {
    public static class ServerCommands {

        public static List<string> ValidAttributeParts = new() { "head", "hand", "handle", "bind", "binding", "tool", "part", "sharp", "sharpness" };
        public static List<string> CurrentOrMax = new() { "cur", "current", "max" };

        //Some helpful debugging server commands to try and set the rotation and offset of various parts that compose a ModularPartRenderer.
        //Only usable on an item that has multiple 'part' shapes!
        public static void RegisterServerCommands(ICoreServerAPI sapi) {
            sapi.ChatCommands
                .Create("setMultiPartRenderingRotation")
                .WithAlias("smprr")
                .WithDescription("In-game tweaking of the Rendering on a held Multi-Part item. [Toolsmith]")
                .RequiresPrivilege("controlserver")
                .WithArgs(sapi.ChatCommands.Parsers.Word("partKey"), sapi.ChatCommands.Parsers.Float("rotateX"), sapi.ChatCommands.Parsers.Float("rotateY"), sapi.ChatCommands.Parsers.Float("rotateZ"))
                .HandleWith(args => OnSetMultiPartRenderingRotation(sapi, args));
            sapi.ChatCommands
                .Create("setMultiPartRenderingOffset")
                .WithAlias("smpro")
                .WithDescription("In-game tweaking of the Offset on a held Multi-Part item. [Toolsmith]")
                .RequiresPrivilege("controlserver")
                .WithArgs(sapi.ChatCommands.Parsers.Word("partKey"), sapi.ChatCommands.Parsers.Float("offsetX"), sapi.ChatCommands.Parsers.Float("offsetY"), sapi.ChatCommands.Parsers.Float("offsetZ"))
                .HandleWith(args => OnSetMultiPartRenderingOffset(sapi, args));
            sapi.ChatCommands
                .Create("changeToolsmithAttribute")
                .WithAlias("cta")
                .WithDescription("Easier command to handle the adjusting of important various durability attributes related to Toolsmith. [Toolsmith]")
                .RequiresPrivilege("controlserver")
                .WithArgs(sapi.ChatCommands.Parsers.Word("partTarget"), sapi.ChatCommands.Parsers.Word("currentOrMax"), sapi.ChatCommands.Parsers.Int("value"))
                .HandleWith(args => OnChangeToolsmithAttribute(sapi, args));
        }

        private static TextCommandResult OnSetMultiPartRenderingRotation(ICoreServerAPI sapi, TextCommandCallingArgs args) {
            var heldItem = args.Caller.Player.InventoryManager.ActiveHotbarSlot?.Itemstack;
            if (heldItem == null) {
                return TextCommandResult.Error("Could not find an active hotbar, or a held item for the player running the command!");
            }
            if (!heldItem.Collectible.HasBehavior<ModularPartRenderingFromAttributes>()) {
                return TextCommandResult.Error("Item was found, but does not have the ModularPartRenderingFromAttributes Behavior! This is only for items using that, otherwise it has no effect.");
            }
            if (!heldItem.HasMultiPartRenderTree()) {
                return TextCommandResult.Error("Item has the intended behavior, but does not have a set Multi-Part Rendering Tree Attribute. Was it instantiated properly?");
            }

            var partKey = args[0] as string;
            var rotateX = args[1] as float? ?? 0;
            var rotateY = args[2] as float? ?? 0;
            var rotateZ = args[3] as float? ?? 0;

            var multiPartTree = heldItem.GetMultiPartRenderTree();
            if (!multiPartTree.HasAttribute(partKey)) {
                return TextCommandResult.Error("Could not find part with key '" + partKey + "' on this item's multiPartTree.");
            }

            var partTree = multiPartTree.GetPartAndTransformRenderTree(partKey);
            partTree.SetPartRotationX(rotateX);
            partTree.SetPartRotationY(rotateY);
            partTree.SetPartRotationZ(rotateZ);

            args.Caller.Player.InventoryManager.ActiveHotbarSlot.MarkDirty();
            return TextCommandResult.Success("Successfully set part '" + partKey + "' rotation to X: " + rotateX + ", Y: " + rotateY + ", Z: " + rotateZ);
        }

        private static TextCommandResult OnSetMultiPartRenderingOffset(ICoreServerAPI sapi, TextCommandCallingArgs args) {
            var heldItem = args.Caller.Player.InventoryManager.ActiveHotbarSlot?.Itemstack;
            if (heldItem == null) {
                return TextCommandResult.Error("Could not find an active hotbar, or a held item for the player running the command!");
            }
            if (!heldItem.Collectible.HasBehavior<ModularPartRenderingFromAttributes>()) {
                return TextCommandResult.Error("Item was found, but does not have the ModularPartRenderingFromAttributes Behavior! This is only for items using that, otherwise it has no effect.");
            }
            if (!heldItem.HasMultiPartRenderTree()) {
                return TextCommandResult.Error("Item has the intended behavior, but does not have a set Multi-Part Rendering Tree Attribute. Was it instantiated properly?");
            }

            var partKey = args[0] as string;
            var offsetX = args[1] as float? ?? 0;
            var offsetY = args[2] as float? ?? 0;
            var offsetZ = args[3] as float? ?? 0;

            var multiPartTree = heldItem.GetMultiPartRenderTree();
            if (!multiPartTree.HasAttribute(partKey)) {
                return TextCommandResult.Error("Could not find part with key '" + partKey + "' on this item's multiPartTree.");
            }

            var partTree = multiPartTree.GetPartAndTransformRenderTree(partKey);
            partTree.SetPartOffsetX(offsetX);
            partTree.SetPartOffsetY(offsetY);
            partTree.SetPartOffsetZ(offsetZ);

            args.Caller.Player.InventoryManager.ActiveHotbarSlot.MarkDirty();
            return TextCommandResult.Success("Successfully set part '" + partKey + "' offset to X: " + offsetX + ", Y: " + ", Z: " + offsetZ);
        }

        private static TextCommandResult OnChangeToolsmithAttribute(ICoreServerAPI sapi, TextCommandCallingArgs args) {
            var heldItem = args.Caller.Player.InventoryManager.ActiveHotbarSlot?.Itemstack;
            if (heldItem == null) {
                return TextCommandResult.Error("Could not find an active hotbar, or a held item for the player running the command!");
            }
            if (!heldItem.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>() && !heldItem.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>() && !heldItem.Collectible.HasBehavior<CollectibleBehaviorToolHead>() && !heldItem.Collectible.HasBehavior<CollectibleBehaviorToolPartWithHealth>()) {
                return TextCommandResult.Error("Held Item is not a valid item with a Toolsmith Behavior. Skipping adding or changing any attributes to avoid stacking problems!");
            }

            var partTarget = args[0] as string;
            var currentOrMax = args[1] as string;
            int value = args[2] as int? ?? 1;

            if (!ValidAttributeParts.Contains(partTarget)) {
                return TextCommandResult.Error("Could not parse the part to target. Please use something like \"head\", \"handle\", \"binding\", \"tool\", \"part\", \"sharp\"...");
            }
            if (!CurrentOrMax.Contains(currentOrMax)) {
                return TextCommandResult.Error("Could not parse current or max. Please enter either \"current\" or \"max\".");
            }

            if (heldItem.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                switch (partTarget) {
                    case "head":
                        if (currentOrMax == "max") {
                            return TextCommandResult.Error("Cannot set the tool's max head durability! To edit this, a Json Patch is needed that changes the vanilla durability, or the Head Mult config will need adjusting.");
                        } else {
                            heldItem.SetToolheadCurrentDurability(value);
                        }
                        args.Caller.Player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                        return TextCommandResult.Success("Set held tool's " + currentOrMax + " " + partTarget + " durability to " + value);
                    case "sharp":
                    case "sharpness":
                        if (currentOrMax == "max") {
                            heldItem.SetToolMaxSharpness(value);
                        } else {
                            heldItem.SetToolCurrentSharpness(value);
                        }
                        args.Caller.Player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                        return TextCommandResult.Success("Set held tool's " + currentOrMax + " sharpness to " + value);
                    case "hand":
                    case "handle":
                        if (currentOrMax == "max") {
                            heldItem.SetToolhandleMaxDurability(value);
                        } else {
                            heldItem.SetToolhandleCurrentDurability(value);
                        }
                        args.Caller.Player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                        return TextCommandResult.Success("Set held tool's " + currentOrMax + " " + partTarget + " durability to " + value);
                    case "bind":
                    case "binding":
                        if (currentOrMax == "max") {
                            heldItem.SetToolbindingMaxDurability(value);
                        } else {
                            heldItem.SetToolbindingCurrentDurability(value);
                        }
                        args.Caller.Player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                        return TextCommandResult.Success("Set held tool's " + currentOrMax + " " + partTarget + " durability to " + value);
                    default:
                        return TextCommandResult.Error("That type of part is not valid for the held item, will avoid setting any attributes. Try using \"head\", \"sharp\", \"sharpness\", \"hand\", \"handle\", \"bind\" or \"binding\" instead!");
                }
            } else if (heldItem.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>()) {
                switch (partTarget) {
                    case "tool":
                        if (currentOrMax == "max") {
                            return TextCommandResult.Error("Cannot set the tool's max durability! To edit this, a Json Patch is needed that changes the vanilla durability, or the Head Mult config will need adjusting.");
                        } else {
                            heldItem.SetSmithedDurability(value);
                        }
                        args.Caller.Player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                        return TextCommandResult.Success("Set held tool's " + currentOrMax + " " + partTarget + " durability to " + value);
                    case "sharp":
                    case "sharpness":
                        if (currentOrMax == "max") {
                            heldItem.SetToolMaxSharpness(value);
                        } else {
                            heldItem.SetToolCurrentSharpness(value);
                        }
                        args.Caller.Player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                        return TextCommandResult.Success("Set held tool's " + currentOrMax + " sharpness to " + value);
                    default:
                        return TextCommandResult.Error("That type of part is not valid for the held item, will avoid setting any attributes. Try using \"tool\", \"sharp\" or \"sharpness\" instead!");
                }
            } else if (heldItem.Collectible.HasBehavior<CollectibleBehaviorToolHead>()) {
                switch (partTarget) {
                    case "head":
                    case "part":
                        if (currentOrMax == "max") {
                            heldItem.SetPartMaxDurability(value);
                        } else {
                            heldItem.SetPartCurrentDurability(value);
                        }
                        args.Caller.Player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                        return TextCommandResult.Success("Set held parts's " + currentOrMax + " durability to " + value);
                    case "sharp":
                    case "sharpness":
                        if (currentOrMax == "max") {
                            heldItem.SetPartMaxSharpness(value);
                        } else {
                            heldItem.SetPartCurrentSharpness(value);
                        }
                        args.Caller.Player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                        return TextCommandResult.Success("Set held parts's " + currentOrMax + " sharpness to " + value);
                    default:
                        return TextCommandResult.Error("That type of part is not valid for the held item, will avoid setting any attributes. Try using \"head\", \"part\", \"sharp\" or \"sharpness\" instead!");
                }
            } else { //This should catch all regular Tool Parts With Health, and not those with sharpness as well.
                switch (partTarget) {
                    case "part":
                        if (currentOrMax == "max") {
                            heldItem.SetPartMaxDurability(value);
                        } else {
                            heldItem.SetPartCurrentDurability(value);
                        }
                        args.Caller.Player.InventoryManager.ActiveHotbarSlot.MarkDirty();
                        return TextCommandResult.Success("Set held parts's " + currentOrMax + " durability to " + value);
                    default:
                        return TextCommandResult.Error("That type of part is not valid for the held item, will avoid setting any attributes in case of a mistake. The valid option for this part is simply \"part\".");
                }
            }
        }
    }
}
