using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace Toolsmith.Client.Behaviors {

    //Modeled after the Code Maltiez sent my way! And Combat Overhaul + Armory code in general for use examples.
    public class ModularPartRenderingFromAttributes : CollectibleBehavior, IContainedMeshSource {

        private Dictionary<int, MultiTextureMeshRef> meshrefs => ObjectCacheUtil.GetOrCreate(api, ToolsmithConstants.ModularPartRenderingFromAttributesMeshRefs, () => new Dictionary<int, MultiTextureMeshRef>());
        private ICoreClientAPI capi;
        private ICoreAPI api;
        private readonly Item item;

        private PartData properties; //This holds the static defaults that might want to be defined for the base item in question, while any dynamic addons will be defined on the passed item's attribute and parsed out like that.
                                     //Perhaps this could be moved from the configs (for the additional part defines and such) to actual Json... But that only helps modders. Yet... at the same time, who's going to really be messing with shapes and not be familiar with Json?

        public ModularPartRenderingFromAttributes(CollectibleObject collObj) : base(collObj) {
            item = collObj as Item ?? throw new Exception("ModularPartRenderingFromAttributes only works on Items.");
            properties = new PartData();
        }

        public override void OnLoaded(ICoreAPI api) {
            base.OnLoaded(api);

            this.api = api;
            capi = api as ICoreClientAPI;

            AddAllToCreativeInventory();
        }

        public override void OnUnloaded(ICoreAPI api) {
            if (api.ObjectCache.ContainsKey(ToolsmithConstants.ModularPartRenderingFromAttributesMeshRefs) && meshrefs.Count > 0) { //If the Cache exists and has more then one entry, iterate through them all to dispose them and clean things up.
                foreach ((int _, MultiTextureMeshRef mesh) in meshrefs) {
                    mesh.Dispose();
                }
                ObjectCacheUtil.Delete(api, ToolsmithConstants.ModularPartRenderingFromAttributesMeshRefs); //Clean up the cache itself!
            }

            base.OnUnloaded(api);
        }

        public override void Initialize(JsonObject properties) {
            base.Initialize(properties);
            this.properties = properties.AsObject<PartData>();
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
            int meshrefID = itemstack.TempAttributes.GetInt(ToolsmithAttributes.ToolsmithMeshID);
            if (api != null && (meshrefID == 0 || !meshrefs.TryGetValue(meshrefID, out renderinfo.ModelRef))) { //This checks if it has already been rendered and cached, and if so, send that again - otherwise generate one.
                int id = meshrefs.Count + 1;

                var mesh = GenMesh(itemstack, capi.ItemTextureAtlas, null);
                if (mesh != null) {
                    MultiTextureMeshRef modelref = capi.Render.UploadMultiTextureMesh(mesh);
                    renderinfo.ModelRef = meshrefs[id] = modelref;
                } else {
                    meshrefs[id] = renderinfo.ModelRef;
                }

                itemstack.TempAttributes.SetInt(ToolsmithAttributes.ToolsmithMeshID, id);
            }
        }

        public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos) {
            if (itemstack == null) {
                return new MeshData();
            }

            var mesh = new MeshData(6, 4);
            if (itemstack.HasMultiPartRenderTree()) {
                ITreeAttribute partTransTree = itemstack.GetMultiPartRenderTree(); //The Multi-Part tree contains sub-trees of the PartRenderTrees paired with their render data like rotation and everything. So loop through them all and add them together on the Mesh.
                if (partTransTree.Count == 0) {
                    return GenMesh(null, targetAtlas);
                }

                bool failedPart = false;
                foreach (var part in partTransTree) { //This is the PartAndTransform Tree.
                    ITreeAttribute partTree = partTransTree.GetTreeAttribute(part.Key);
                    Vec3f rotation = new Vec3f(partTree.GetPartRotationX(), partTree.GetPartRotationY(), partTree.GetPartRotationZ());
                    MeshData partMesh = GenMesh(partTree.GetPartRenderTree(), targetAtlas, rotation);
                    if (partMesh == null) {
                        failedPart = true;
                        break;
                    } else {
                        mesh.AddMeshData(partMesh, partTree.GetPartOffsetX(), partTree.GetPartOffsetY(), partTree.GetPartOffsetZ());
                    }
                }

                if (failedPart) { //If any part fails to be found or render, it'll just default to using the item fallback. This ideally should make things cleaner in the end, and prevent invisible items.
                    return GenMesh(null, targetAtlas);
                }
            } else if (itemstack.HasPartRenderTree()) {
                ITreeAttribute renderTree = itemstack.GetPartRenderTree();
                MeshData partMesh = GenMesh(renderTree, targetAtlas);

                if (partMesh == null) {
                    return GenMesh(null, targetAtlas);
                }
                return partMesh;
            } else {
                return GenMesh(null, targetAtlas);
            }

            return mesh;
        }

        public MeshData GenMesh(ITreeAttribute renderTree, ITextureAtlasAPI targetAtlas, Vec3f rotationInfo = null) { //This is only to handle JUST the 'PartRenderTree's, it will either render the data provided, or if the tree sent is null, just fall back to item defaults.
            if (capi == null) { //If not the client or there are no properties defined, just return nothing.
                return new MeshData();
            }

            bool needFallback = (renderTree == null); //Fallback to the properties defaults if this is the case.

            Shape shape = null;
            if (!needFallback && renderTree.HasPartShapePath() && renderTree.GetPartShapePath() != "") {
                if (renderTree.HasShapeOverrideTag()) {
                    shape = api.Assets.TryGet(new AssetLocation(renderTree.GetPartShapePath() + renderTree.GetShapeOverrideTag() + ".json"))?.ToObject<Shape>();
                } else {
                    shape = api.Assets.TryGet(new AssetLocation(renderTree.GetPartShapePath() + ".json"))?.ToObject<Shape>();
                }

                if (shape == null) { //If something above fails, IE it probably has busted data, return null and handle above to send it back through with a null render tree to grab the fallback.
                    return null;
                }
            } else {
                if (item?.Shape != null) {
                    shape = capi.TesselatorManager.GetCachedShape(item.Shape.Base);
                }
            }

            if (shape == null) { //If shape cannot be found no matter what, just return nothing.
                if (item?.Shape == null) {
                    ToolsmithModSystem.Logger.Error("Item.Shape for " + item.Code + " was null. This item might appear invisible to prevent any direct code errors.");
                } else {
                    ToolsmithModSystem.Logger.Error("Could not find a fallback cached shape for " + item.Code + ". This item might appear invisible to prevent any direct code errors.");
                }
                
                return new MeshData();
            }

            ShapeTextureSource texSource = new(api as ICoreClientAPI, shape, "Compiling and Rendering Composite Shape for Modular Tool and Part rendering");
            texSource.textures.Clear();

            foreach ((string texCode, AssetLocation assetLoc) in shape.Textures) { //Go through the shape's textures and populate the texSource with any that the shape already has defined
                if (item.Textures.TryGetValue(texCode, out CompositeTexture texture)) {
                    texSource.textures[texCode] = texture;
                } else { //If the item doesn't have any texture overrides defined, then use the shape's default.
                    texSource.textures[texCode] = new CompositeTexture(assetLoc);
                } //This more or less initializes it to have something in each texture spot. Maybe good for safety? But might not be needed either, since I can expect that the attributes will be set properly? They need to be at least.
            }

            if (!needFallback) {
                ITreeAttribute textureTree = renderTree.GetPartTextureTree();
                foreach (var entry in textureTree) {
                    CompositeTexture tex;
                    if (entry.Key.Contains("-overlay")) { //This looks for any entries that have the "-overlay" appended to them! This means, if you assign a texture to the RenderDataTree with the key of {code}-overlay you will overlay that texture code! And it should (theoretically) function with any number of overlays!
                        var actualEntry = entry.Key.Split('-').First();
                        tex = texSource.textures[actualEntry];
                        if (tex.BlendedOverlays == null) {
                            tex.BlendedOverlays = Array.Empty<BlendedOverlayTexture>();
                        }
                        var overlay = new BlendedOverlayTexture();
                        overlay.Base = new AssetLocation(textureTree.GetAsString(entry.Key) + ".png");
                        tex.BlendedOverlays = tex.BlendedOverlays.Append(overlay);
                    } else {
                        tex = new CompositeTexture(new AssetLocation(textureTree.GetAsString(entry.Key) + ".png"));
                    }
                    texSource.textures[entry.Key] = tex;
                }
            } else { //Fallback to the default textures in the properties. Ideally shouldn't hit here but uhhh.
                foreach (var entry in item.Textures) {
                    texSource.textures[entry.Key] = entry.Value;
                }
                
                if (properties != null && properties.textures?.Length > 0) {
                    foreach (TextureData texConfig in properties.textures) { //Then go through each texture entry in the properties, see if the itemstack in question has that attribute set and retrieve it if so
                        texSource.textures[texConfig.code] = new CompositeTexture(new AssetLocation(texConfig.Default + ".png"));
                    }
                }
            }

            capi.Tesselator.TesselateShape("Modular Part rendering", shape, out MeshData mesh, texSource, rotationInfo);
            return mesh;
        }

        public string GetMeshCacheKey(ItemStack itemstack) {
            string cacheKey = item.Code.ToShortString();

            if (itemstack.HasPartRenderTree()) {
                var renderTree = itemstack.GetPartRenderTree();
                GetMeshCacheKeyFromSubTrees(ref cacheKey, renderTree);
            } else if (itemstack.HasMultiPartRenderTree()) {
                var renderTree = itemstack.GetMultiPartRenderTree();
                foreach (var part in renderTree) {
                    cacheKey += "-" + part.Key;
                    var partRenderAndTransformTree = renderTree.GetTreeAttribute(part.Key);
                    var partRenderTree = partRenderAndTransformTree.GetPartRenderTree();
                    GetMeshCacheKeyFromSubTrees(ref cacheKey, partRenderTree);
                }
            }
            return cacheKey;
        }

        private void GetMeshCacheKeyFromSubTrees(ref string cacheKey, ITreeAttribute renderTree) {
            if (renderTree.HasPartShapePath()) {
                cacheKey += "-" + renderTree.GetPartShapePath().Replace('/', '-');
            } else {
                cacheKey += "-" + "itemdefault";
            }
            var textureTree = renderTree.GetPartTextureTree();
            foreach (var texEntry in textureTree) {
                cacheKey += "-" + textureTree.GetString(texEntry.Key)?.Replace('/', '-') ?? "default";
            }
        }

        private void AddAllToCreativeInventory() {
            if (properties == null || properties.skipCreativeInventoryAdditions) { //If it's null, or it is set to skip the creative additions step, then we just break out.
                return;
            }

            List<JsonItemStack> stacks = new();
            ITreeAttribute tree = new TreeAttribute();
            if (item.CollectibleBehaviors?.Length > 0) {
                foreach (var behavior in item.CollectibleBehaviors.Where(b => (b as IModularPartRenderer) != null)) {
                    tree = (behavior as IModularPartRenderer).InitializeRenderTree(tree, item);
                }
            }
            ConstructStacksWithRecursion(stacks, tree, 0);

            JsonItemStack stackWithNoAttributes = new() {
                Code = item.Code,
                Type = EnumItemClass.Item
            };
            stackWithNoAttributes.Resolve(api?.World, "Fallback default for " + item.Code);

            if (item.CreativeInventoryStacks == null) {
                if (stacks.Count == 0) {
                    item.CreativeInventoryStacks = new CreativeTabAndStackList[] {
                        new() { Stacks = stacks.ToArray(), Tabs = properties.creativeTabs },
                        new() { Stacks = new JsonItemStack[] { stackWithNoAttributes }, Tabs = item.CreativeInventoryTabs }
                    };
                    item.CreativeInventoryTabs = null;
                } else {
                    item.CreativeInventoryStacks = new CreativeTabAndStackList[] {
                        new() { Stacks = stacks.ToArray(), Tabs = properties.creativeTabs },
                        new() { Stacks = new JsonItemStack[] { stacks[0] }, Tabs = item.CreativeInventoryTabs }
                    };
                    item.CreativeInventoryTabs = null;
                }
            }
        }

        //This will parse through the Behavior's properties and all the data that had been entered there, compiling all of the "variant"
        private void ConstructStacksWithRecursion(List<JsonItemStack> stacks, ITreeAttribute tree, int index) { //Will likely not be able to construct full modular tools.
            if (properties == null) { //If it's null, then we just break out. Probably something went wrong and we shoudn't have gotten here anyway.
                return;
            }

            if (properties.textures.Length <= index) { //If the current step is beyond the length of the Textures array, then we can cap it off and collapse this line of calls here.
                stacks.Add(GenJsonStack(tree)); //Add it to the list of itemstacks, and then collapse
                return;
            }

            TextureData textureProp = properties.textures[index];
            foreach (string path in textureProp.values) { //Go through the whole values array, and keep adding the attributes it finds to the json string. One itemstack per each combo of attributes in the full properties.
                tree.GetOrAddTreeAttribute(ToolsmithAttributes.ModularPartDataTree).GetPartTextureTree().SetPartTexturePathFromKey(textureProp.code, path);
                ConstructStacksWithRecursion(stacks, tree, index + 1);
            }
        }

        //Actually turn all that parsed json into the attributes, and add in the item's information to create the different textured variant!
        private JsonItemStack GenJsonStack(ITreeAttribute tree) {
            JsonItemStack stack = new() {
                Code = item.Code,
                Type = EnumItemClass.Item,
                Attributes = new JsonObject(JToken.Parse(TreeAttribute.ToJsonToken(tree)))
            };

            stack.Resolve(api?.World, "Generated Texture Variant of " + item.Code);
            return stack;
        }
    }
}
