using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Toolsmith.Client {
    public static class MultiPartRenderingHelpers {

        //
        //  ("modularToolRenderData")
        //      |
        //      |-- (Part Tree)
        //      |
        //      |-- (Part Tree)
        //      |
        //      |-- (Part Tree)  <- Named "modularPartRenderData" only on part items themselves! Might need to also include rotation and transform information later on?
        //              |
        //              |-- ("partShape") = Shape AssetLoc String
        //              |
        //              |-- ("partTextures")
        //                      |
        //                      |-- (Texture Code) = Texture AssetLoc String
        //

        public static ITreeAttribute GetToolRenderTree(this ItemStack item) { //Can be iterated through with a foreach loop to process all the parts!
            if (!item.Attributes.HasAttribute(ToolsmithAttributes.ModularToolDataTree)) {
                item.Attributes.GetOrAddTreeAttribute(ToolsmithAttributes.ModularToolDataTree);
            }
            return item.Attributes.GetTreeAttribute(ToolsmithAttributes.ModularToolDataTree);
        }

        public static void SetToolRenderTree(this ItemStack item, ITreeAttribute tree) {
            item.Attributes[ToolsmithAttributes.ModularToolDataTree] = tree;
        }

        public static bool HasToolRenderTree(this ItemStack item) {
            return item.Attributes.HasAttribute(ToolsmithAttributes.ModularToolDataTree);
        }

        public static ITreeAttribute GetPartRenderTree(this ItemStack item) { //Will only contain a specific set of entries! Only set to this on the individual parts themselves! When part of a tool, will just be a tag respective and handled by that tool's Behavior.
            if (!item.Attributes.HasAttribute(ToolsmithAttributes.ModularPartDataTree)) {
                item.Attributes.GetOrAddTreeAttribute(ToolsmithAttributes.ModularPartDataTree);
            }
            return item.Attributes.GetTreeAttribute(ToolsmithAttributes.ModularPartDataTree);
        }

        public static void SetPartRenderTree(this ItemStack item, ITreeAttribute tree) {
            item.Attributes[ToolsmithAttributes.ModularPartDataTree] = tree;
        }

        public static bool HasPartRenderTree(this ItemStack item) {
            return item.Attributes.HasAttribute(ToolsmithAttributes.ModularPartDataTree);
        }

        public static string GetPartShapeIndex(this ITreeAttribute tree) {
            return tree.GetString(ToolsmithAttributes.ModularPartShapeIndex);
        }

        public static void SetPartShapeIndex(this ITreeAttribute tree, string path) { //This is only set when the part has an alternate shape, otherwise just use the Item Default.
            tree.SetString(ToolsmithAttributes.ModularPartShapeIndex, path);
        }

        public static bool HasPartShapeIndex(this ITreeAttribute tree) {
            return tree.HasAttribute(ToolsmithAttributes.ModularPartShapeIndex);
        }

        public static ITreeAttribute GetPartTextureTree(this ITreeAttribute tree) {
            if (!tree.HasAttribute(ToolsmithAttributes.ModularPartTextureTree)) {
                tree.GetOrAddTreeAttribute(ToolsmithAttributes.ModularPartTextureTree);
            }
            return tree.GetTreeAttribute(ToolsmithAttributes.ModularPartTextureTree);
        }

        public static void SetPartTextureTree(this ITreeAttribute main, ITreeAttribute toAdd) {
            main[ToolsmithAttributes.ModularPartTextureTree] = toAdd;
        }

        public static void SetOverlay(this ITreeAttribute tree, string key, string overlayPath) {
            var baseTex = tree.GetString(key);
            var woodType = baseTex.Split('/').Last();
            tree.SetString(key, overlayPath + woodType);
        }
    }
}
