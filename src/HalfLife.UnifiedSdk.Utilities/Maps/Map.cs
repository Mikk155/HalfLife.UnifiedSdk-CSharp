﻿using HalfLife.UnifiedSdk.Utilities.Entities;
using Serilog;
using System;
using System.IO;

namespace HalfLife.UnifiedSdk.Utilities.Maps
{
    /// <summary>Provides access to map data.</summary>
    public abstract class Map
    {
        /// <summary>File name of this map.</summary>
        public string FileName { get; }

        /// <summary>
        /// Base name of the map, used in trigger_changelevel and the changelevel command among other things.
        /// </summary>
        public string BaseName => Path.GetFileNameWithoutExtension(FileName);

        /// <summary>Which content type the map data is in.</summary>
        /// <seealso cref="MapContentType"/>
        public MapContentType ContentType { get; }

        /// <summary>Whether this map is a compiled or in a source content type.</summary>
        /// <seealso cref="MapContentType"/>
        public bool IsCompiled => ContentType == MapContentType.Compiled;

        /// <summary>List of entities in the map.</summary>
        public abstract EntityList Entities { get; }

        /// <summary>Creates a new map with the given file name and content type.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <see langword="null"/>.</exception>
        protected Map(string fileName, MapContentType contentType)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            ContentType = contentType;
        }

        internal abstract IMapEntity CreateNewEntity(string className);

        internal abstract int IndexOf(IMapEntity entity);

        internal abstract void Add(IMapEntity entity);

        internal abstract void Remove(IMapEntity entity);

        /// <summary>Serializes this map to the given stream.</summary>
        public abstract void Serialize(Stream stream);

        /// <summary>
        /// Wraps this map instance with a map that logs all changes to the given logger.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <exception cref="InvalidOperationException">If this map is already wrapped for logging.</exception>
        public Map WithLogger(ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            if (this is LoggingMap)
            {
                throw new InvalidOperationException("Don't wrap maps that are already wrapped");
            }

            return new LoggingMap(this, logger);
        }
    }
}
