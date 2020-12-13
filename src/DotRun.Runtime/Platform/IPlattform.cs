// This file is part of DotRun. Web: https://github.com/Arakis/DotRun
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace DotRun.Runtime
{

    public enum PlatformType
    {
        Unix,
        Windows,
    }

    public interface IPlatform
    {
        PlatformType PlatformType { get; }
        Task<string> GetHomeDir();
        Task<string> FindExecutablePath(string executable);
        Task<string> GetUsername();
    }

}
