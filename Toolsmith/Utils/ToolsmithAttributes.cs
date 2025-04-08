using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolsmith.Utils {
    public static class ToolsmithAttributes {
        //Going to properly define constants for the Attributes. Just like the hardcoded x5 situation for toolheads before, lets get ontop of the bad practices now and clean them up :P
        //All the Attributes for a Tinkered Tool, it's various parts, durabilities and max durabilities, plus the two important stats they can get from higher quality parts.
        public const string ToolHead = "tinkeredToolHead";
        public const string ToolHeadCurrentDur = "tinkeredToolHeadDurability";
        public const string ToolHeadMaxDur = "tinkeredToolHeadMaxDurability";
        public const string ToolHandle = "tinkeredToolHandle";
        public const string ToolHandleCurrentDur = "tinkeredToolHandleDurability";
        public const string ToolHandleMaxDur = "tinkeredToolHandleMaxDurability";
        public const string ToolBinding = "tinkeredToolBinding";
        public const string ToolBindingCurrentDur = "tinkeredToolBindingDurability";
        public const string ToolBindingMaxDur = "tinkeredToolBindingMaxDurability";

        public const string GripChanceToDamage = "gripChanceToDamage";
        public const string SpeedBonus = "speedBonus";

        public const string BypassMaxCall = "toolsmithSkipMaxDurPatch";

        //The Attributes for the Part items themselves, used for Tool Heads and Handles currently. Try not to set these on a completed tool by mistake, use the specific above ones!
        public const string ToolPartCurrentDur = "toolPartCurrentDurability";
        public const string ToolPartMaxDur = "toolPartMaxDurability";

        //Vanilla Attribute Consts
        //While these are not attributes created by the mod, I figure it might be beneficial to give them the same treatment. Just make sure they stay updated with the base game!
        public const string Durability = "durability";
    }
}
