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
        public const string ToolHeadCurrentDur = "tinkeredToolHeadDurability";
        public const string ToolHeadMaxDur = "tinkeredToolHeadMaxDurability";
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
        public const string BypassMaxCall = "toolsmithSkipMaxDurPatch";
        public const string BrokeWhileSharpening = "toolsmithBrokeToolWhileSharpening";

        //The Attributes for the Part items themselves, used for Tool Heads and Handles currently. Try not to set these on a completed tool by mistake, use the specific above ones!
        public const string ToolPartCurrentDur = "toolPartCurrentDurability";
        public const string ToolPartMaxDur = "toolPartMaxDurability";

        //Vanilla Attribute Consts
        //While these are not attributes created by the mod, I figure it might be beneficial to give them the same treatment. Just make sure they stay updated with the base game!
        public const string Durability = "durability";

        //This just helps to organize it in this file, and pile them into one easy constant to call. Generally for Smithing Plus's Compat and the forgettable attributes there when a Workpiece is made.
        public const string ToolsmithForgettableAttributes = "," + ToolHead + "," + ToolHeadCurrentDur + "," + ToolHeadMaxDur + "," + ToolSharpnessCurrent + "," + ToolSharpnessMax + "," + ToolHandle + "," + ToolHandleCurrentDur + "," + ToolHandleMaxDur + "," + ToolBinding + "," + ToolBindingCurrentDur + "," + ToolBindingMaxDur + "," + GripChanceToDamage + "," + SpeedBonus + "," + Drawback + "," + BypassMaxCall + "," + BrokeWhileSharpening;
    }
}
