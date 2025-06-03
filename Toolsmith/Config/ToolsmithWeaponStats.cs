using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolsmith.Config {

    public class ToolsmithWeaponStats {
        //public Dictionary<string, >

        // Heads are captured by the global 'head' tag, then filtered by what their recipe result creates
        // Bindings can also be the same as Tools

        //Parts for "Simple Weapons", IE: Maces, similar 'normal' shaft length weapons that take a Head, Binding, Handle, and optional Endcap/Adornment
        // Handles for these are the same as handles for Tools
        // Are Adornments going to be universal for both Simple and Long weapons? Maybe. Hmm.

        //Parts for "Martial Weapons", IE: Swords of most types, Blade (head), Guard, Hilt (shorter handle effectively), (optional?) Pommel
        // Guards
        // Hilts
        // Pommels

        //Parts for "Long Weapons", IE: Spears, Halberd, Long Axe, take a Head, Long Handle, Binding and optional Endcap/Adornment
        // Long Handles
        // Probably best to make 1 set of Endcaps and Adornments, and use them both for Simple and Long Weapons.
    }
}
