using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valheim_Mod_Sync
{
    public class BepInExVcontrol
    {
        public BepInExVcontrol() { }

        public bool is_bepinex_installed()
        {
            bool is_installed = false;

            if (Directory.Exists("BepInEx/") && Directory.Exists("BepInEx/core/") && Directory.Exists("unstripped_corlib/") && File.Exists("winhttp.dll"))
            {
                is_installed = true;
            }

            return is_installed;
        }
    }
}
