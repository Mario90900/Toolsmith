using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Client;
using Toolsmith.Client.Behaviors;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Toolsmith.Server {
    public static class ServerCommands {

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

            return TextCommandResult.Success("Successfully set part '" + partKey + "' offset to X: " + offsetX + ", Y: " + ", Z: " + offsetZ);
        }
    }
}
