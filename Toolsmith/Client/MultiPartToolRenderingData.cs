using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Client.Behaviors;
using Vintagestory.API.Common;

namespace Toolsmith.Client {

    //Generally referenced and used for rendering the tool. The itemstack itself will hold the actual functional bits of the tool's attributes.
    //This is all mainly static render information that shouldn't change unless the tool or part does, so it can be cached and referenced back to for hopefully easier access.
    public class MultiPartToolRenderingData {
        public PartData[] parts = Array.Empty<PartData>();
        public EnumTool? toolType;
    }

    public class PartData {
        public TextureData[] textures { get; set; } = Array.Empty<TextureData>();
        public string[] creativeTabs { get; set; } = Array.Empty<string>(); //What tabs should these items show up in?
    }

    public class TextureData {
        public string code { get; set; } = ""; //Code for the texture entry of the shape.
        public string Default { get; set; } = ""; //Default texture fallback.
        public string[] values { get; set; } = Array.Empty<string>(); //The different possible textures available.
    }
}
