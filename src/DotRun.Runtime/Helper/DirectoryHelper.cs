// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace DotRun.Runtime
{

    public static class DirectoryHelper
    {

        internal static async Task<TempFile> WriteStreamToTempFile(Stream source)
        {
            var randomPath = Path.GetRandomFileName();

            if (File.Exists(randomPath))
                File.Delete(randomPath);

            using var fileStream = File.Create(randomPath);
            source.Seek(0, SeekOrigin.Begin);
            await source.CopyToAsync(fileStream);

            return new TempFile { FilePath = randomPath };
        }

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
