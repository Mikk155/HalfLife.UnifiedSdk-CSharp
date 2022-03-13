﻿using HalfLife.UnifiedSdk.Utilities.Maps;
using Semver;
using System;

namespace HalfLife.UnifiedSdk.Utilities.Tools.UpgradeTool
{
    /// <summary>A map upgrade command.</summary>
    public sealed class MapUpgrade
    {
        /// <summary>Map being upgraded.</summary>
        public Map Map { get; }

        /// <summary>
        /// Version to upgrade from. If left as <see langword="null"/>, the map will be upgraded from its current version.
        /// If no current version key can be found in the map, the map will be upgraded from the first known version.
        /// </summary>
        public SemVersion? From { get; init; }

        /// <summary>
        /// Version to upgrade to. If left as <see langword="null"/>, the map will be upgraded to the latest version.
        /// <see cref="MapUpgradeTool.LatestVersion"/>
        /// </summary>
        public SemVersion? To { get; init; }

        /// <summary>
        /// If <see cref="From"/> is older than the version set by the map, throw an exception.
        /// Default true. This protects against upgrading maps that are already upgraded, which could break entity setups.
        /// </summary>
        public bool ThrowOnTooOldVersion { get; init; } = true;

        /// <summary>Creates a new map upgrde command.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is null.</exception>
        public MapUpgrade(Map map)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
        }
    }
}
