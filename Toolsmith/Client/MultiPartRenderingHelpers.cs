using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.ToolTinkering.Behaviors;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Toolsmith.Client {
    public static class MultiPartRenderingHelpers {

        //
        //  ("modularMultiPartRenderData") <- The Tool EnumTool (and include specific overrides for specific tool types, which get grabbed OVER this) can modify the shapes to be a prefix for the .json file name. Resulting in the structure of {tool}-{partName}(-{override's tag})
        //      |
        //      |-- (PartAndTransform Tree)
        //      |
        //      |-- (PartAndTransform Tree) - Each one is named after their tag for their part, IE: handle, head, binding...
        //      |
        //      |-- (PartAndTransform Tree)
        //              |
        //              |-- (Rotation X-Y-Z) - In separate entries if they exist, otherwise will return 0.
        //              |
        //              |-- (Offset X-Y-Z) - Also in separate entries, and will return 0 otherwise.
        //              |
        //              |-- (Part Tree)  <- Named "modularPartRenderData" only on part items themselves! Might need to also include rotation and transform information later on?
        //                      |
        //                      |-- ("partShape") = Shape AssetLoc String
        //                      |
        //                      |-- (ShapeOverrideAppendTag) - This is set only by the tool or whatever Multi-Part render data set it up, and the renderer will automatically append it to the end of the shape it is a part of.
        //                      |
        //                      |-- ("partTextures") <- This can technically have multiple textures since a shape can have multiple textures
        //                              |
        //                              |-- (Texture Code) = Texture AssetLoc String
        //

        public static ITreeAttribute GetMultiPartRenderTree(this ItemStack item) { //Can be iterated through with a foreach loop to process all the parts!
            if (!item.Attributes.HasAttribute(ToolsmithAttributes.ModularMultiPartDataTree)) {
                item.Attributes.GetOrAddTreeAttribute(ToolsmithAttributes.ModularMultiPartDataTree);
            }
            return item.Attributes.GetTreeAttribute(ToolsmithAttributes.ModularMultiPartDataTree);
        }

        public static void SetMultiPartRenderTree(this ItemStack item, ITreeAttribute tree) {
            item.Attributes[ToolsmithAttributes.ModularMultiPartDataTree] = tree;
        }

        public static bool HasMultiPartRenderTree(this ItemStack item) {
            return item.Attributes.HasAttribute(ToolsmithAttributes.ModularMultiPartDataTree);
        }

        public static void RemoveMultiPartRenderTree(this ItemStack item) {
            item.Attributes.RemoveAttribute(ToolsmithAttributes.ModularMultiPartDataTree);
        }

        public static ITreeAttribute GetPartAndTransformRenderTree(this ITreeAttribute tree, string key) {
            if (!tree.HasAttribute(key)) {
                tree.GetOrAddTreeAttribute(key);
            }
            return tree.GetTreeAttribute(key);
        }

        public static void SetPartAndTransformRenderTree(this ITreeAttribute tree, string key, ITreeAttribute setTree) {
            tree[key] = setTree;
        }

        public static float GetPartRotationX(this ITreeAttribute tree) {
            return tree.GetFloat(ToolsmithAttributes.ModularPartRotationX);
        }

        public static void SetPartRotationX(this ITreeAttribute tree, float rotX) {
            tree.SetFloat(ToolsmithAttributes.ModularPartRotationX, rotX);
        }

        public static float GetPartRotationY(this ITreeAttribute tree) {
            return tree.GetFloat(ToolsmithAttributes.ModularPartRotationY);
        }

        public static void SetPartRotationY(this ITreeAttribute tree, float rotY) {
            tree.SetFloat(ToolsmithAttributes.ModularPartRotationY, rotY);
        }

        public static float GetPartRotationZ(this ITreeAttribute tree) {
            return tree.GetFloat(ToolsmithAttributes.ModularPartRotationZ);
        }

        public static void SetPartRotationZ(this ITreeAttribute tree, float rotZ) {
            tree.SetFloat(ToolsmithAttributes.ModularPartRotationZ, rotZ);
        }

        public static float GetPartOffsetX(this ITreeAttribute tree) {
            return tree.GetFloat(ToolsmithAttributes.ModularPartOffsetX);
        }

        public static void SetPartOffsetX(this ITreeAttribute tree, float offX) {
            tree.SetFloat(ToolsmithAttributes.ModularPartOffsetX, offX);
        }

        public static float GetPartOffsetY(this ITreeAttribute tree) {
            return tree.GetFloat(ToolsmithAttributes.ModularPartOffsetY);
        }

        public static void SetPartOffsetY(this ITreeAttribute tree, float offY) {
            tree.SetFloat(ToolsmithAttributes.ModularPartOffsetY, offY);
        }

        public static float GetPartOffsetZ(this ITreeAttribute tree) {
            return tree.GetFloat(ToolsmithAttributes.ModularPartOffsetZ);
        }

        public static void SetPartOffsetZ(this ITreeAttribute tree, float offZ) {
            tree.SetFloat(ToolsmithAttributes.ModularPartOffsetZ, offZ);
        }

        public static ITreeAttribute GetPartRenderTree(this ItemStack item) { //Will only contain a specific set of entries! Only set to this on the individual parts themselves! When part of a tool, will just be a tag respective and handled by that tool's Behavior.
            if (!item.Attributes.HasAttribute(ToolsmithAttributes.ModularPartDataTree)) {
                item.Attributes.GetOrAddTreeAttribute(ToolsmithAttributes.ModularPartDataTree);
            }
            return item.Attributes.GetTreeAttribute(ToolsmithAttributes.ModularPartDataTree);
        }

        public static ITreeAttribute GetPartRenderTree(this ITreeAttribute tree) {
            if (!tree.HasAttribute(ToolsmithAttributes.ModularPartDataTree)) {
                tree.GetOrAddTreeAttribute(ToolsmithAttributes.ModularPartDataTree);
            }
            return tree.GetTreeAttribute(ToolsmithAttributes.ModularPartDataTree);
        }

        public static void SetPartRenderTree(this ItemStack item, ITreeAttribute tree) {
            item.Attributes[ToolsmithAttributes.ModularPartDataTree] = tree;
        }

        public static void SetPartRenderTree(this ITreeAttribute main, ITreeAttribute toAdd) {
            main[ToolsmithAttributes.ModularPartDataTree] = toAdd;
        }

        public static bool HasPartRenderTree(this ItemStack item) {
            return item.Attributes.HasAttribute(ToolsmithAttributes.ModularPartDataTree);
        }

        public static bool HasPartRenderTree(this ITreeAttribute tree) {
            return tree.HasAttribute(ToolsmithAttributes.ModularPartDataTree);
        }

        public static void RemovePartRenderTree(this ItemStack item) {
            item.Attributes.RemoveAttribute(ToolsmithAttributes.ModularPartDataTree);
        }

        public static string GetPartShapePath(this ITreeAttribute tree) {
            var shapeString = tree.GetString(ToolsmithAttributes.ModularPartShapeIndex);
            if (shapeString == "toolsmith:shapes/item/gripfabric") {
                shapeString = "toolsmith:shapes/item/parts/handles/grips/gripfabric";
                tree.SetString(ToolsmithAttributes.ModularPartShapeIndex, shapeString);
            } else if (shapeString == "toolsmith:shapes/item/crudehandle") {
                shapeString = "toolsmith:shapes/item/parts/handles/crudehandle";
                tree.SetString(ToolsmithAttributes.ModularPartShapeIndex, shapeString);
            } else if (shapeString == "toolsmith:shapes/item/handle") {
                shapeString = "toolsmith:shapes/item/parts/handles/handle";
                tree.SetString(ToolsmithAttributes.ModularPartShapeIndex, shapeString);
            } else if (shapeString == "toolsmith:shapes/item/carpentedhandle") {
                shapeString = "toolsmith:shapes/item/parts/handles/carpentedhandle";
                tree.SetString(ToolsmithAttributes.ModularPartShapeIndex, shapeString);
            }
            return shapeString;
        }

        public static void SetPartShapePath(this ITreeAttribute tree, string path) { //This is only set when the part has an alternate shape, otherwise just use the Item Default.
            tree.SetString(ToolsmithAttributes.ModularPartShapeIndex, path);
        }

        public static bool HasPartShapePath(this ITreeAttribute tree) {
            return tree.HasAttribute(ToolsmithAttributes.ModularPartShapeIndex);
        }

        public static string GetShapeOverrideTag(this ITreeAttribute tree) {
            return tree.GetString(ToolsmithAttributes.ShapeOverrideAppendTag);
        }

        public static void SetShapeOverrideTag(this ITreeAttribute tree, string append) {
            tree.SetString(ToolsmithAttributes.ShapeOverrideAppendTag, append);
        }

        public static bool HasShapeOverrideTag(this ITreeAttribute tree) {
            return tree.HasAttribute(ToolsmithAttributes.ShapeOverrideAppendTag);
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

        public static string GetPartTexturePathFromKey(this ITreeAttribute tree, string key) {
            return tree.GetString(key);
        }

        public static void SetPartTexturePathFromKey(this ITreeAttribute tree, string key, string path) {
            tree.SetString(key, path);
        }

        public static void SetOverlay(this ITreeAttribute tree, string key, string overlayPath) {
            var baseTex = tree.GetString(key);
            var woodType = baseTex.Split('/').Last();
            tree.SetString(key, overlayPath + woodType);
        }
    }

    public class ToolHeadTextureData {
        public List<string> Tags = new List<string>();
        public List<string> Paths = new List<string>();
    }
}
