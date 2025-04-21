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

        /*public MultiTextureMeshRef GetMeshRef(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo, PartData part) {
            int meshrefID = itemstack.TempAttributes.GetInt(ToolsmithAttributes.ToolsmithMeshID);
            if (meshrefID == 0 || !meshrefs.TryGetValue(meshrefID, out renderinfo.ModelRef)) { //This checks if it has already been rendered and cached, and if so, send that again - otherwise generate one.
                int id = meshrefs.Count + 1;
                MultiTextureMeshRef modelref = capi.Render.UploadMultiTextureMesh(GenMesh(itemstack, capi.ItemTextureAtlas, part));
                renderinfo.ModelRef = meshrefs[id] = modelref;

                itemstack.TempAttributes.SetInt(ToolsmithAttributes.ToolsmithMeshID, id);
            }
            
            return renderinfo.ModelRef;
        }*/ //Might not need this either, I really don't see myself using the Part Override.

        public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos) {
            return GenMesh(itemstack, targetAtlas);
        }

        public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas) {
            if (capi == null || properties == null) { //If not the client or there are no properties defined, just return nothing.
                return new MeshData();
            }

            bool needFallback = (!itemstack.HasToolRenderTree() && !itemstack.HasPartRenderTree()); //Fallback to the properties defaults if this is the case.

            Shape shape;
            ITreeAttribute renderTree = itemstack.GetPartRenderTree(); //Only works for Parts currently. Will work on multi-part renders after.
            if (renderTree.HasPartShapeIndex()) {
                shape = ToolsmithModSystem.AlternatePartShapes.TryGetValue(renderTree.GetPartShapeIndex());
            } else {
                shape = capi.TesselatorManager.GetCachedShape(item.Shape.Base);
            }

            if (shape == null) { //If shape cannot be found no matter what, just return nothing.
                return new MeshData();
            }

            ContainedTextureSource texSource = new(api as ICoreClientAPI, targetAtlas, new Dictionary<string, AssetLocation>(), $"For rendering texture variants of {item.Code}");
            texSource.Textures.Clear();

            foreach ((string texCode, AssetLocation assetLoc) in shape.Textures) { //Go through the shape's textures and populate the texSource with any that the shape already has defined
                if (item.Textures.TryGetValue(texCode, out CompositeTexture texture)) {
                    texSource.Textures[texCode] = texture.Base;
                } else { //If the item doesn't have any texture overrides defined, then use the shape's default.
                    texSource.Textures[texCode] = assetLoc;
                } //This more or less initializes it to have something in each texture spot. Maybe good for safety? But might not be needed either, since I can expect that the attributes will be set properly? They need to be at least.
            }

            if (!needFallback) {
                ITreeAttribute textureTree = renderTree.GetPartTextureTree();
                foreach (var entry in textureTree) {
                    string path = textureTree.GetAsString(entry.Key);
                    texSource.Textures[entry.Key] = new AssetLocation(path + ".png");
                }
            } else { //Fallback to the default textures in the properties. Ideally shouldn't hit here but uhhh.
                foreach (TextureData texConfig in properties.textures) { //Then go through each texture entry in the properties, see if the itemstack in question has that attribute set and retrieve it if so
                    texSource.Textures[texConfig.code] = new AssetLocation(texConfig.Default + ".png");
                }
            }

            capi.Tesselator.TesselateShape("Rendering Modular part for " + item.Code, shape, out MeshData mesh, texSource); //item, out MeshData mesh, texSource
            return mesh;

            //Going to have to completely rewrite this. It's far FAR too static the original way it was implemented...
            //Can leave in the basic woodtyping to allow for wood variants easily probably but... Can you even actively change a shape mid-runtime? - God I wasn't even USING the new shape, was I...?
            //Need to make a few garenteed assumptions:
            //Has to be able to have access to all the data for rendering purposes in the original GenMesh call.
            // -- This means the shape, textures, everything, needs to be saved TO THE ITEMSTACK. Pull data from the ItemStack PRIMARILY. Use the Wood defaults as a fallback, since that's hardcoded in the Json.
            // Important Data for Handles so far:
            // - Shape if it's a Gripped handle or not.
            // -- Both the grip tag being saved and the presence of the saved shape can be a sign the handle has a grip, and use the tag to get the stat information.
            // - Treatment tag and then if it is currently still 'wet', once I get the transition working.
            // -- It is wet if it currently has the TreatmentOverlay set
            // - Wood typing is done and dusted, it's working thankfully without a hitch really.
            // --- This above information needs to be able to be abstracted and rendered generic enough so that all the Rendering can take place right here, no matter what the part.
            // - - The Paths can be tossed in the Render tree. For data purposes, things like the 'tags' can be left on the main part itself.
            // - - Ideally if all the render information can be stored in one render tree and all shapes (if multiple - can you even have multiple shapes in one...?)
            // - - Can be read from this tree and just rendered easy.
            //
            // Things to note from that damn testing: Shapes that are NOT part of any json file are NOT pre-cached in the TesselatorManager. They have to be grabbed with that
            //   bit of code in AssetsFinalize in ToolsmithModSystem, then added to a local cache. They should be saved using the StatTag for that handle. It's stupid to anything else.
            //   -- Limitation -- Requires unique part tags. But that's fine really, it was already assumed since Dictionaries were being used from the start.
            //   Make sure to update caching all possible part shapes any time an additional part is able to have a shape.
            //
            // Will actually have to compile this all into the shape and tesselate the shape itself with the textures and return that - this currently was only ever caring about the textures. That was silly.
            // If this works out though, that means having the parts fully rendered in a neat little package being shipped out from here? Might be easier to mash two parts together later down the line...
            // Make this my code now, not gifted code, it gave me a springboard, but there's no reason I can't change every little part of this code. REMEBER THAT. Whatever works for me will work, it's not like I'm making a libarary mod right now.
            //
            // Each part is composed of 1 shape object to keep things simple. This object can change though!
            //   Can start with a Shape tag on the first layer of the tree, a Default 'Fallback' texture, likely can be grabbed from the base shape of the item actually, followed by a Textures AttributesTree - Each entry on this tree is a key-value pair, where the Key can 
            // -- How are the trees organized? Can they be iterated through reliably? Will likely need to set up the reading and breakdown into info into an initial step, then run the rendering once all the information is sorted.
            // -- Does this effect the original tree? Will have to experiment.
            //
            //Likely have to revisit that idea of building a Render Data AttributeTree. ... Yep that's what I gotta do.
            //This data needs to be preserved between saving and loading of an area, and full reboots.
            //The base foundation of alternate wood textures can be enough to not care about the wood as long as it's already saved to attributes, which all of them should be.
            // - That much is solved at least.
            //
            // If I ever find myself floundering for a while and getting frustrated, stop just slamming my head on the wall. I can do this. It helped to stop slamming and run tests.
            // And writing all this down and actually _thinking_ it through.
            //
            //COMPOSITE SHAPE IS WHAT I NEED!!! ITS GOING TO MAKE THE TOOLS EASY TO RENDER! CompositeShape compShape = new CompositeShape();
            //Tesselator.TesselateItem also has MANY overloads with different options.
            //It looks like Texture Source will probably just hold the strings of the various texture entries of the shape as the key, then give the assets as the return. It likely just matches to the shapes like that.
            //Probably the same is true for a CompositeShape.

            /*foreach ((string texCode, AssetLocation assetLoc) in shape.Textures) { //Go through the shape's textures and populate the texSource with any that the item already has defined
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
            }*/
        }

        public string GetMeshCacheKey(ItemStack itemstack) {
            string cacheKey = item.Code.ToShortString();
            
            foreach (TextureData textures in properties.textures) {
                cacheKey += "-" + itemstack.Attributes.GetString(textures.code)?.Replace('/', '-') ?? "default";
            }
            return cacheKey;
        }

        private void AddAllToCreativeInventory() {
            if (properties == null) { //If it's null, then we just break out.
                return;
            }

            List<JsonItemStack> stacks = new();
            ITreeAttribute tree = new TreeAttribute();
            tree.GetOrAddTreeAttribute(ToolsmithAttributes.ModularPartDataTree);
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
                //json += "}";
                stacks.Add(GenJsonStack(tree)); //Add it to the list of itemstacks, and then collapse
                return;
            }

            //if (json != "{") { //As long as it is not the initial call, add a comma between each entry.
            //    json += ", ";
            //}

            TextureData textureProp = properties.textures[index];
            foreach (string path in textureProp.values) { //Go through the whole values array, and keep adding the attributes it finds to the json string. One itemstack per each combo of attributes in the full properties.
                //string jsonCopy = (string)json.Clone();
                //jsonCopy += $"{textureProp.attribute}: \"{path}\"";
                tree.GetOrAddTreeAttribute(ToolsmithAttributes.ModularPartDataTree).GetPartTextureTree().SetString(textureProp.code, path);
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
