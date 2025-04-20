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
        public string partAttribute { get; set; } = ""; //The attribute of the part in question, IE: "tinkeredToolHead" for a tool's head. Easy to check if it exists on a tool, and should it render?
        public string shapePath { get; set; } = "";
        public TextureData[] textures { get; set; } = Array.Empty<TextureData>();
        public string[] creativeTabs { get; set; } = Array.Empty<string>(); //What tabs should these items show up in?
    }

    public class TextureData {
        public string code { get; set; } = ""; //Code for the texture entry of the shape.
        public string attribute { get; set; } = ""; //The attribute that will store the texture information for said code above.
        public string Default { get; set; } = ""; //Default texture fallback.
        public bool overlay { get; set; } = false; //Is this texture to be treated as an overlay?
        public int overlayTargetIndex { get; set; } = 0;
        public string[] values { get; set; } = Array.Empty<string>(); //The different possible textures available.
    }
}
