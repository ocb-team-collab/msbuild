using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Build.Tasks
{
    internal static class GlobalEnvVars
    {
        public static bool GlobalIsStatic = Environment.GetEnvironmentVariable("MSBUILDSTATIC") == "1";
    }
}
