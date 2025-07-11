﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Config;
using Toolsmith.ToolTinkering.Behaviors;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
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
        //              |-- (Part Tree)  <- Named "modularPartRenderData" only on part items themselves!
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

        public static bool HasPartAndTransformRenderTree(this ITreeAttribute tree, string key) {
            return tree.HasAttribute(key);
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

        public static string GetToolTypeFromHeadShapePath(string toolPath) {
            var splitShape = toolPath.Split('/');
            string toolType = null;
            for (int i = 1; i < splitShape.Length; i++) {
                if ((i + 1) < splitShape.Length && splitShape[i] == "parts") {
                    toolType = splitShape[i + 1];
                    break;
                }
            }
            if (toolType == "handles" || toolType == "grip") {
                return null;
            }

            return toolType;
        }

        public static string GetToolTypeFromHeadShapePathFast(string toolPath) {
            var splitShape = toolPath.Split('/');
            string toolType = null;
            if (splitShape.Length > 2 && splitShape[0] == "item" && splitShape[1] == "parts") {
                toolType = splitShape[2];
            }

            return toolType;
        }

        public static string GetHandleTypeFromShapePath(string handlePath) {
            var splitShape = handlePath.Split("/");
            string handleType = null;
            for (int i = 1; i < splitShape.Length; i++) {
                if ((i + 1) < splitShape.Length && splitShape[i] == "handles") {
                    handleType = splitShape[i + 1];
                    break;
                }
            }

            return handleType;
        }

        public static string GetGenericHandleTypeFromShapePathFast(string handlePath) { //This expects a generic handle item path. 
            var splitShape = handlePath.Split("/");
            string handleType = null;
            if (splitShape.Length > 4 && splitShape[1] == "parts" && splitShape[2] == "handles") {
                handleType = splitShape[3];
            }

            return handleType;
        }

        public static string GetTypedHandleTypeFromShapePathFast(string handlePath) { //This expects a typed handle path. 
            var splitShape = handlePath.Split("/");
            string handleType = null;
            if (splitShape.Length > 5 && splitShape[1] == "parts" && splitShape[3] == "handles") {
                handleType = splitShape[4];
            }

            return handleType;
        }

        public static string ConvertFromHandlePathToShapePath(string handlePath, string tool) {
            var splitHandle = handlePath.Split("/");
            var retVal = splitHandle[0];
            for (int i = 1; i < splitHandle.Length; i++) {
                if (i == 3) {
                    retVal = retVal + "/" + tool + "/" + splitHandle[i];
                } else {
                    retVal = retVal + "/" + splitHandle[i];
                }
            }

            if (!ToolsmithModSystem.Api.Assets.Exists(new AssetLocation(retVal + ".json"))) { //This IDEALLY should never happen, but just in case it's here as a backup.
                retVal = "toolsmith:shapes/item/parts/" + tool + "/handles/universal/handle";
            }

            return retVal;
        }

        public static string ConvertFromGenericHandlePathToGripShapePath(string handlePath, string grip) {
            var splitHandle = handlePath.Split("/");
            var retVal = splitHandle[0];
            for (int i = 1; i < splitHandle.Length - 1; i++) {
                retVal = retVal + "/" + splitHandle[i];
            }
            retVal = retVal + "/grip/" + grip;

            if (!ToolsmithModSystem.Api.Assets.Exists(new AssetLocation(retVal + ".json"))) {
                retVal = "toolsmith:shapes/item/parts/handles/universal/grip/" + grip;
            }

            return retVal;
        }

        public static string ConvertFromHandlePathToGripShapePath(string handlePath, string gripPath, string tool) {
            var splitHandle = handlePath.Split("/");
            var retVal = splitHandle[0];
            for (int i = 1; i < splitHandle.Length - 1; i++) {
                if (i == 3) {
                    retVal = retVal + "/" + tool + "/" + splitHandle[i];
                } else {
                    retVal = retVal + "/" + splitHandle[i];
                }
            }

            var splitGrip = gripPath.Split("/");
            var gripType = splitGrip[splitGrip.Length - 1];

            retVal = retVal + "/grip/" + gripType;

            if (!ToolsmithModSystem.Api.Assets.Exists(new AssetLocation(retVal + ".json"))) {
                retVal = "toolsmith:shapes/item/parts/" + tool + "/handles/universal/grip/" + gripType;
            }

            return retVal;
        }

        public static string ConvertFromGenericHandlePathToBindingShapePath(string handlePath, string binding, string tool, bool isMetalMat) {
            var splitHandle = handlePath.Split("/");
            var retVal = splitHandle[0];
            for (int i = 1; i < splitHandle.Length - 1; i++) {
                if (i == 3) {
                    retVal = retVal + "/" + tool + "/" + splitHandle[i];
                } else {
                    retVal = retVal + "/" + splitHandle[i];
                }
            }

            var bindingString = binding;
            if (isMetalMat) {
                bindingString = bindingString + "-metalhead";
            } else {
                bindingString = bindingString + "-stonehead";
            }
            retVal = retVal + "/binding/" + bindingString;

            if (!ToolsmithModSystem.Api.Assets.Exists(new AssetLocation(retVal + ".json"))) {
                retVal = "toolsmith:shapes/item/parts/" + tool + "/handles/universal/binding/" + bindingString;
            }

            return retVal;
        }

        public static string ConvertFromTypedHandlePathToBindingShapePath(string handlePath, string binding, bool isMetalMat) {
            var splitHandle = handlePath.Split("/");
            var retVal = splitHandle[0];
            for (int i = 1; i < splitHandle.Length - 1; i++) {
                retVal = retVal + "/" + splitHandle[i];
            }

            var bindingString = binding;
            if (isMetalMat) {
                bindingString = bindingString + "-metalhead";
            } else {
                bindingString = bindingString + "-stonehead";
            }
            retVal = retVal + "/binding/" + bindingString;

            if (!ToolsmithModSystem.Api.Assets.Exists(new AssetLocation(retVal + ".json"))) {
                retVal = "toolsmith:shapes/item/parts/" + splitHandle[3] + "/handles/universal/binding/" + bindingString;
            }

            return retVal;
        }

        public static void BuildToolRenderFromAllSeparateParts(ItemStack tool, ItemStack head, ItemStack handle, ItemStack binding = null) {
            var toolMultiPartTree = tool.GetMultiPartRenderTree(); //Time to assign the data for rendering the Bundle!
            var toolType = GetToolTypeFromHeadShapePath(head.Item.Shape.Base.Path);
            var headPartAndTransformTree = toolMultiPartTree.GetPartAndTransformRenderTree(ToolsmithAttributes.ModularPartHeadName);
            var handlePartAndTransformTree = toolMultiPartTree.GetPartAndTransformRenderTree(ToolsmithAttributes.ModularPartHandleName);

            headPartAndTransformTree.SetPartOffsetX(0.54f);
            headPartAndTransformTree.SetPartOffsetY(0.01f);
            headPartAndTransformTree.SetPartOffsetZ(-0.02f);
            headPartAndTransformTree.SetPartRotationX(0);
            headPartAndTransformTree.SetPartRotationY(-90);
            headPartAndTransformTree.SetPartRotationZ(0);

            var headPartTree = headPartAndTransformTree.GetPartRenderTree();
            headPartTree.SetPartShapePath(head.Item.Shape.Base.Domain + ":shapes/" + head.Item.Shape.Base.Path);
            var headTextureTree = headPartTree.GetPartTextureTree();
            if (ToolsmithModSystem.Api.Side.IsServer()) {
                ToolHeadTextureData textures;
                var success = RecipeRegisterModSystem.ToolHeadTexturesCache.TryGetValue(head.Item.Code, out textures);
                if (success) {
                    for (int i = 0; i < textures.Tags.Count; i++) {
                        headTextureTree.SetPartTexturePathFromKey(textures.Tags[i], textures.Paths[i]);
                    }
                } else {
                    ToolsmithModSystem.Logger.Error("Could not find the tool head's ToolHeadTextureData entry when crafting a Part Bundle. Might not render right.");
                }
            } else {
                foreach (var tex in head.Item.Textures) {
                    headTextureTree.SetPartTexturePathFromKey(tex.Key, tex.Value.Base);
                }
            }

            HandleStatPair handleStats = ToolsmithModSystem.Config.BaseHandleRegistry.TryGetValue(handle.Collectible.Code.Path);
            string toolSpecificHandleShape = null;
            if (toolType != null) {
                toolSpecificHandleShape = ConvertFromHandlePathToShapePath(handleStats.handleShapePath, toolType);
            }
            if (handle.HasMultiPartRenderTree()) {
                var handleMultiPartTree = handle.GetMultiPartRenderTree();
                foreach (var tree in handleMultiPartTree) {
                    var subPartAndTransformTree = handleMultiPartTree.GetPartAndTransformRenderTree(tree.Key);
                    if (tree.Key == ToolsmithAttributes.ModularPartHandleName && toolSpecificHandleShape != null) {
                        var handlePartTree = subPartAndTransformTree.GetPartRenderTree();
                        handlePartTree.SetPartShapePath(toolSpecificHandleShape);
                    } else if (tree.Key == ToolsmithAttributes.ModularPartGripName && toolType != null) {
                        var gripPartTree = subPartAndTransformTree.GetPartRenderTree();
                        var gripPath = ConvertFromHandlePathToGripShapePath(handleStats.handleShapePath, gripPartTree.GetPartShapePath(), toolType);
                        if (gripPath != null) {
                            gripPartTree.SetPartShapePath(gripPath);
                            var gripTextureTree = gripPartTree.GetPartTextureTree();
                        }
                    }

                    subPartAndTransformTree.SetPartOffsetX(0);
                    subPartAndTransformTree.SetPartOffsetY(0);
                    subPartAndTransformTree.SetPartOffsetZ(0);
                    subPartAndTransformTree.SetPartRotationX(0);
                    subPartAndTransformTree.SetPartRotationY(0);
                    subPartAndTransformTree.SetPartRotationZ(0);
                    toolMultiPartTree.SetPartAndTransformRenderTree(tree.Key, subPartAndTransformTree);
                }
            } else {
                handlePartAndTransformTree.SetPartOffsetX(0);
                handlePartAndTransformTree.SetPartOffsetY(0);
                handlePartAndTransformTree.SetPartOffsetZ(0);
                handlePartAndTransformTree.SetPartRotationX(0);
                handlePartAndTransformTree.SetPartRotationY(0);
                handlePartAndTransformTree.SetPartRotationZ(0);

                var handlePartTree = handlePartAndTransformTree.GetPartRenderTree();
                if (toolSpecificHandleShape == null && !handle.HasPartRenderTree()) {
                    handlePartTree.SetPartShapePath(handle.Item.Shape.Base.Domain + ":shapes/" + handle.Item.Shape.Base.Path);
                }
                if (handle.HasPartRenderTree()) {
                    handlePartAndTransformTree.SetPartRenderTree(handle.GetPartRenderTree());
                }
                if (toolSpecificHandleShape != null) {
                    handlePartTree.SetPartShapePath(toolSpecificHandleShape);
                }
            }

            if (binding != null) {
                var bindingTransformAndPartTree = toolMultiPartTree.GetPartAndTransformRenderTree(ToolsmithAttributes.ModularPartBindingName);
                bindingTransformAndPartTree.SetPartOffsetX(0);
                bindingTransformAndPartTree.SetPartOffsetY(0);
                bindingTransformAndPartTree.SetPartOffsetZ(0);
                bindingTransformAndPartTree.SetPartRotationX(0);
                bindingTransformAndPartTree.SetPartRotationY(0);
                bindingTransformAndPartTree.SetPartRotationZ(0);

                BindingStatPair bindingWithStats = ToolsmithModSystem.Config.BindingRegistry.TryGetValue(binding.Collectible.Code.Path);
                BindingStats bindingStats = ToolsmithModSystem.Stats.bindings.TryGetValue(bindingWithStats.bindingStatTag);
                var bindingPartTree = bindingTransformAndPartTree.GetPartRenderTree();
                if (toolSpecificHandleShape != null) {
                    var bindingPath = ConvertFromTypedHandlePathToBindingShapePath(toolSpecificHandleShape, bindingWithStats.bindingShapePath, tool.Collectible.IsCraftableMetal());
                    bindingPartTree.SetPartShapePath(bindingPath);
                }

                var bindingTextureTree = bindingPartTree.GetPartTextureTree();
                if (bindingWithStats.bindingTextureOverride != "") {
                    bindingTextureTree.SetPartTexturePathFromKey("material", bindingWithStats.bindingTextureOverride);
                } else {
                    bindingTextureTree.SetPartTexturePathFromKey("material", bindingStats.texturePath);
                }
            }
        }
    }

    public class ToolHeadTextureData {
        public List<string> Tags = new List<string>();
        public List<string> Paths = new List<string>();
    }
}
