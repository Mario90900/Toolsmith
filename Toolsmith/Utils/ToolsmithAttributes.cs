using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolsmith.Utils {
    public static class ToolsmithAttributes { //Try to keep all attributes Camel Case! Don't want to make that mistake again and actually push it to people's saves...
        //Going to properly define constants for the Attributes. Just like the hardcoded x5 situation for toolheads before, lets get ontop of the bad practices now and clean them up :P
        //All the Attributes for a Tinkered Tool, it's various parts, durabilities and max durabilities, plus the two important stats they can get from higher quality parts.
        public const string ToolHead = "tinkeredToolHead";
        public const string ToolSharpnessCurrent = "toolSharpnessCurrent"; //For both the tool and the head alone
        public const string ToolSharpnessMax = "toolSharpnessMax"; //For both the tool and the head alone
        public const string ToolHandle = "tinkeredToolHandle";
        public const string ToolHandleCurrentDur = "tinkeredToolHandleDurability";
        public const string ToolHandleMaxDur = "tinkeredToolHandleMaxDurability";
        public const string ToolBinding = "tinkeredToolBinding";
        public const string ToolBindingCurrentDur = "tinkeredToolBindingDurability";
        public const string ToolBindingMaxDur = "tinkeredToolBindingMaxDurability";

        public const string GripChanceToDamage = "gripChanceToDamage";
        public const string SpeedBonus = "speedBonus";
        public const string Drawback = "toolsmithDrawback";

        //Using attributes as flags, if they exist on a tool, it means that flag is set
        public const string BrokeWhileSharpening = "toolsmithBrokeToolWhileSharpening";

        //The Attributes for the Part items themselves, used for Tool Heads and Handles currently. Try not to set these on a completed tool by mistake, use the specific above ones!
        public const string ToolPartCurrentDur = "toolPartCurrentDurability";
        public const string ToolPartMaxDur = "toolPartMaxDurability";

        //Attributes to control the addons to a handle, and the tool as a whole itself. Will be stored on the handles, and referenced for generating the renderer as well as stats. Important these are saved.
        public const string HandleGripTag = "toolHandleGripTag";
        public const string HandleTreatmentTag = "toolHandleTreatmentTag"; //Tags stay on the base item that have them and don't need to be moved to the crafted tool. They will have their stats transferred instead upwards.
        public const string PartWetTreatment = "partHasWetTreatment"; //Both a flag and holds the full time the treatment goes for.
        public const string PartAfterTreatmentStack = "partAfterTreatmentStack"; //Set the copy stack in the first Transition tick to this tag, to make retreiving it easier. It should regen this if it's somehow lost as well!

        // -- Render Data AttributeTree stuffs! --
        public const string ModularToolDataTree = "modularToolRenderData"; //This is a TreeAttribute that will contain more Trees of the respective parts. When added to a tool, the string tag for each part is that part's name. IE: Head, Handle or Binding, this will be set by the tool's behavior during OnCrafting.
        public const string ModularPartDataTree = "modularPartRenderData"; //This TreeAttribute is solely on individual parts to make retreieving them easier and consistant! This simply contains the Data entries organized below, and is also set and updated during OnCrafting!

        public const string ModularPartShapeIndex = "partShapeIndex"; //This will just contain a string for the dictionary entry holding the part in the cache.
        public const string ModularPartTextureTree = "partTextures"; //This is another TreeAttribute that contains entries of the respective Shape's codes for the various textures in it, and the texture entries.
                                                                     //To help handle 'overlay' textures, find the intended entry to be overlayed, and then append a ++ to the end of the texture path, and afterwards add the overlay path. This might be what that one Texture handling class was looking for?

        //Temp Attributes! Ones not intended to be saved to the item forever, and instead are used in the TempAttributes tree on the itemstack. It seems like the Temp Attributes get cleaned every time a slot is marked dirty.
        public const string ToolsmithMeshID = "toolsmithMeshrefID";

        // -- Vanilla Attribute Consts --
        //While these are not attributes created by the mod, I figure it might be beneficial to give them the same treatment. Just make sure they stay updated with the base game!
        public const string Durability = "durability";
        public const string TransitionState = "transitionstate";

        // -- Slated for Removal later down the line! Only kept around for the purposes of checking if they still exist and fixing them! Do not use these anymore!
        public const string ToolHeadCurrentDur = "tinkeredToolHeadDurability";
        public const string ToolHeadMaxDur = "tinkeredToolHeadMaxDurability";

        // -- This just helps to organize it in this file, and pile them into one easy constant to call. Generally for Smithing Plus's Compat and the forgettable attributes there when a Workpiece is made.
        public const string ToolsmithForgettableAttributes = "," + ToolHead + "," + ToolSharpnessCurrent + "," + ToolSharpnessMax + "," + ToolHandle + "," + ToolHandleCurrentDur + "," + ToolHandleMaxDur + "," + ToolBinding + "," + ToolBindingCurrentDur + "," + ToolBindingMaxDur + "," + GripChanceToDamage + "," + SpeedBonus + "," + Drawback + "," + BrokeWhileSharpening;
        public static readonly string[] ToolsmithIgnoreAttributesArray = new string[13] { ToolHead, ToolSharpnessCurrent, ToolSharpnessMax, ToolHandle, ToolHandleCurrentDur, ToolHandleMaxDur, ToolBinding, ToolBindingCurrentDur, ToolBindingMaxDur, GripChanceToDamage, SpeedBonus, Drawback, BrokeWhileSharpening };
    }
}
