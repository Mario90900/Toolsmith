using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Toolsmith.Client { }
    /*internal class OverlayedTextureSource : ITexPositionSource {

        private ITextureAtlasAPI targetAtlas;
        private ICoreClientAPI capi;
        public Dictionary<string, CompositeTexture> Textures = new Dictionary<string, CompositeTexture>();

        public Size2i AtlasSize => targetAtlas.Size;

        public TextureAtlasPosition this[string textureCode] => getOrCreateTexPos(Textures[textureCode]);

        public OverlayedTextureSource(ICoreClientAPI capi, ITextureAtlasAPI targetAtlas, Dictionary<string, CompositeTexture> textures) {
            this.capi = capi;
            this.targetAtlas = targetAtlas;
            Textures = textures;
        }

        protected TextureAtlasPosition getOrCreateTexPos(CompositeTexture compTexture) {
            AssetLocation path;
            if (compTexture.BlendedOverlays == null) {
                path = compTexture.Base;
            } else {
                if (compTexture.Baked == null) {
                    compTexture.RuntimeBake(capi, targetAtlas);
                }
                path = compTexture.Baked.BakedName;
            }
            TextureAtlasPosition texPos = targetAtlas[path];
            if (texPos == null) {
                AssetLocation texturePath;
                if (compTexture.BlendedOverlays == null) {
                    texturePath = compTexture.Base;
                } else {
                    texturePath = compTexture.Baked.BakedName;
                }

                IAsset texAsset = capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
                if (texAsset != null) {
                    targetAtlas.GetOrInsertTexture(texturePath, out var _, out texPos, () => texAsset.ToBitmap(capi));
                    if (texPos == null) {
                        ToolsmithModSystem.Logger.Error("OverlayedTextureSource ran into a problem, require texture {0} which exists, but unable to upload it or allocate space", texturePath);
                        texPos = targetAtlas.UnknownTexturePosition;
                    }
                } else {
                    ToolsmithModSystem.Logger.Error("OverlayedTextureSource ran into a problem, require texture {0}, but no such texture found.", texturePath);
                    texPos = targetAtlas.UnknownTexturePosition;
                }
            }

            return texPos;
        }
    }
}*/
