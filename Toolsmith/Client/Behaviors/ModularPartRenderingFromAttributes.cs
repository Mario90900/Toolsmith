using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Toolsmith.Client.Behaviors {

    //Modeled after the Code Maltiez sent my way! And Combat Overhaul + Armory code in general for use examples.
    public class ModularPartRenderingFromAttributes : CollectibleBehavior, IContainedMeshSource {

        private Dictionary<int, MultiTextureMeshRef> meshrefs => ObjectCacheUtil.GetOrCreate(api, ToolsmithConstants.ModularPartRenderingFromAttributesMeshRefs, () => new Dictionary<int, MultiTextureMeshRef>());
        private ICoreClientAPI capi;
        private ICoreAPI api;
        private readonly Item item;

        private PartData properties;

        public ModularPartRenderingFromAttributes(CollectibleObject collObj) : base(collObj) {
            item = collObj as Item ?? throw new Exception("ModularPartRenderingFromAttributes only works on Items.");
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
            if (meshrefID == 0 || !meshrefs.TryGetValue(meshrefID, out renderinfo.ModelRef)) { //This checks if it has already been rendered and cached, and if so, send that again - otherwise generate one.
                int id = meshrefs.Count + 1;
                MultiTextureMeshRef modelref = capi.Render.UploadMultiTextureMesh(GenMesh(itemstack, capi.ItemTextureAtlas));
                renderinfo.ModelRef = meshrefs[id] = modelref;

                itemstack.TempAttributes.SetInt(ToolsmithAttributes.ToolsmithMeshID, id);
            }
        }

        public MultiTextureMeshRef GetMeshRef(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo, PartData part) {
            int meshrefID = itemstack.TempAttributes.GetInt(ToolsmithAttributes.ToolsmithMeshID);
            if (meshrefID == 0 || !meshrefs.TryGetValue(meshrefID, out renderinfo.ModelRef)) { //This checks if it has already been rendered and cached, and if so, send that again - otherwise generate one.
                int id = meshrefs.Count + 1;
                MultiTextureMeshRef modelref = capi.Render.UploadMultiTextureMesh(GenMesh(itemstack, capi.ItemTextureAtlas, part));
                renderinfo.ModelRef = meshrefs[id] = modelref;

                itemstack.TempAttributes.SetInt(ToolsmithAttributes.ToolsmithMeshID, id);
            }

            return renderinfo.ModelRef;
        }

        public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos) {
            return GenMesh(itemstack, targetAtlas);
        }

        public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, PartData partOverride = null) {
            ContainedTextureSource texSource = new(api as ICoreClientAPI, targetAtlas, new Dictionary<string, AssetLocation>(), $"For rendering texture variants of {item.Code}");
            texSource.Textures.Clear();

            if (capi == null || properties == null) { //If not the client or there are no properties defined, just return nothing.
                return new MeshData();
            }

            Shape shape;
            if (itemstack.HasModularPartShape()) {
                shape = ToolsmithModSystem.AlternatePartShapes.TryGetValue(itemstack.GetModularPartShape());
            } else {
                shape = capi.TesselatorManager.GetCachedShape(item.Shape.Base);
            }
            
            if (shape == null) { //If shape cannot be found no matter what, just return nothing.
                return new MeshData();
            }//Going to have to completely rewrite this. It's far FAR too static the original way it was implemented...
            //Can leave in the basic woodtyping to allow for wood variants easily probably but... Can you even actively change a shape mid-runtime?
            //Need to make a few garenteed assumptions:
            //Has to be able to have access to all the data for rendering purposes in the original GenMesh call.
            // -- This means the shape, textures, everything, needs to be saved TO THE ITEMSTACK.
            //Likely have to revisit that idea of building a Render Data AttributeTree. ... Yep that's what I gotta do.

            foreach ((string texCode, AssetLocation assetLoc) in shape.Textures) { //Go through the shape's textures and populate the texSource with any that the item already has defined
                if (item.Textures.TryGetValue(texCode, out CompositeTexture texture)) {
                    texSource.Textures[texCode] = texture.Base;
                } else { //If the item doesn't have any texture overrides defined, then use the shape's default.
                    texSource.Textures[texCode] = assetLoc;
                }
            }

            foreach (TextureData texConfig in properties.textures) { //Then go through each texture entry in the properties, see if the itemstack in question has that attribute set and retrieve it if so
                string texPath = itemstack.Attributes.GetString(texConfig.attribute) ?? texConfig.Default;
                if (!texConfig.overlay) {
                    texSource.Textures[texConfig.code] = new AssetLocation(texPath + ".png");
                } else {
                    var baseTexture = properties.textures[texConfig.overlayTargetIndex].values[texConfig.overlayTargetIndex]; //There has to be a better way to get this base wood texture but uhhh.
                    CompositeTexture overlayedTexture = new CompositeTexture(new AssetLocation(baseTexture + ".png"));
                    BlendedOverlayTexture overlay = new BlendedOverlayTexture();
                    overlay.Base = new AssetLocation(texPath + ".png");
                    overlayedTexture.BlendedOverlays = new BlendedOverlayTexture[] { overlay };
                    overlayedTexture.RuntimeBake(capi, targetAtlas);
                    texSource.Textures[texConfig.code] = overlayedTexture.Baked.BakedName;
                }
            }

            capi.Tesselator.TesselateItem(item, out MeshData mesh, texSource);
            return mesh;
        }

        public string GetMeshCacheKey(ItemStack itemstack) {
            string cacheKey = item.Code.ToShortString();

            foreach (TextureData textures in properties.textures) {
                cacheKey += "-" + itemstack.Attributes.GetString(textures.attribute)?.Replace('/', '-') ?? "default";
            }
            return cacheKey;
        }

        private void AddAllToCreativeInventory() {
            if (properties == null) {
                return;
            }

            List<JsonItemStack> stacks = new();
            ConstructStacksWithRecursion(stacks, "{", 0);
            
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
        private void ConstructStacksWithRecursion(List<JsonItemStack> stacks, string json, int index) {
            if (properties == null) { //If it's null, then we just break out. Probably something went wrong and we shoudn't have gotten here anyway.
                return;
            }

            if (properties.textures.Length <= index) { //If the current step is beyond the length of the Textures array, then we can cap it off and collapse this line of calls here.
                json += "}";
                stacks.Add(GenJsonStack(json)); //Add it to the list of itemstacks, and then collapse
                return;
            }

            TextureData textureProp = properties.textures[index];
            if (json != "{") { //As long as it is not the initial call, add a comma between each entry.
                json += ", ";
            }

            foreach (string path in textureProp.values) { //Go through the whole values array, and keep adding the attributes it finds to the json string. One itemstack per each combo of attributes in the full properties.
                string jsonCopy = (string)json.Clone();
                jsonCopy += $"{textureProp.attribute}: \"{path}\"";
                ConstructStacksWithRecursion(stacks, jsonCopy, index + 1);
            }
        }

        //Actually turn all that parsed json into the attributes, and add in the item's information to create the different textured variant!
        private JsonItemStack GenJsonStack(string json) {
            JsonItemStack stack = new() {
                Code = item.Code,
                Type = EnumItemClass.Item,
                Attributes = new JsonObject(JToken.Parse(json))
            };

            stack.Resolve(api?.World, "Generated Texture Variant of " + item.Code);
            return stack;
        }
    }
}
