// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace DotRun.Runtime
{

    public static class DirectoryHelper
    {

        public static string GetAbsoluteLocalPath(string path)
        {
            if (string.IsNullOrEmpty(path) || path == "." || path == "./" || path == "./")
                return Environment.CurrentDirectory;

            if (path.StartsWith("~/") || path.StartsWith("~\\"))
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path.Substring(2));

            if (Directory.Exists(path))
                return new DirectoryInfo(path).FullName;

            if (File.Exists(path))
                return new FileInfo(path).FullName;

            return path;
        }

    }
}
