using System;
using System.IO;

namespace DotRun.Runtime
{

    public static class DirectoryHelper
    {

        public static string ConfigRootDirectory
        {
            get
            {
                return "/tmp/dotrun.config"; //TODO: Temp
            }
        }

        public static string ConfigFile
            => Path.Combine(ConfigRootDirectory, "config.yaml");

    }
}
