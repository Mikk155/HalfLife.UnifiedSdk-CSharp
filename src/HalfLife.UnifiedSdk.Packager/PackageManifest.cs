﻿using HalfLife.UnifiedSdk.Utilities.Configuration;
using Newtonsoft.Json;

namespace HalfLife.UnifiedSdk.Packager
{
    /// <summary>
    /// See the MSDN documentation on the Matcher class for more information on what kind of patterns are supported:
    /// https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.filesystemglobbing.matcher
    /// </summary>
    internal class PackageManifest
    {
        /// <summary>
        /// List of files and directories to package.
        /// Filenames ending with ".install" will be renamed to remove this extension after being added to the archive.
        /// </summary>
        [JsonProperty(ItemConverterType = typeof(PathConverter))]
        public List<string> IncludePatterns { get; set; } = new();

        /// <summary>
        /// Files and directories to exclude from the archive.
        /// </summary>
        [JsonProperty(ItemConverterType = typeof(PathConverter))]
        public List<string> ExcludePatterns { get; set; } = new();
    }
}
