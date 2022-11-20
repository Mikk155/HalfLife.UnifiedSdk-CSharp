﻿using HalfLife.UnifiedSdk.Utilities.Tools;
using JeremyAnsel.Media.WavefrontObj;
using Serilog;
using Sledge.Formats.Bsp;
using Sledge.Formats.Bsp.Objects;
using Sledge.Formats.Id;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;

namespace HalfLife.UnifiedSdk.Bsp2Obj
{
    internal sealed class BspToObjConverter
    {
        /// <summary>
        /// Faces with these textures should not be included in the OBJ file since they are not visible faces.
        /// </summary>
        private static readonly ImmutableHashSet<string> TexturesToIgnore = ImmutableHashSet.Create(
            StringComparer.InvariantCultureIgnoreCase,
            "ORIGIN",
            "CLIP");

        private readonly ILogger _logger;

        private readonly string _destinationDirectory;

        private readonly string _bspFileName;

        private readonly BspFile _bspFile;

        private readonly ObjFile _objFile = new();

        private readonly ObjMaterialFile _objMaterialFile = new();

        private ObjMaterial? _dummyMaterial;

        private ImmutableDictionary<int, ObjMaterial> _materialsMap = ImmutableDictionary<int, ObjMaterial>.Empty;

        private BspToObjConverter(ILogger logger, string destinationDirectory, string bspFileName, BspFile bspFile)
        {
            _logger = logger;
            _destinationDirectory = destinationDirectory;
            _bspFileName = bspFileName;
            _bspFile = bspFile;
        }

        public static void Convert(ILogger logger, string destinationDirectory, string bspFileName, BspFile bspFile)
        {
            BspToObjConverter converter = new(logger, destinationDirectory, bspFileName, bspFile);

            converter.ConvertCore();
        }

        private void ConvertCore()
        {
            WriteMaterials();

            // Sledge checks for this so make sure to add it at the start. The exact format matters!
            _objFile.HeaderText = " Scale: 1";

            foreach (var entity in _bspFile.Entities.Select((e, i) => new { Entity = e, Index = i }))
            {
                int? modelNumber = TryGetModelNumber(entity.Entity, entity.Index);

                if (modelNumber is null)
                {
                    continue;
                }

                var origin = Vector3.Zero;

                if (entity.Entity.KeyValues.TryGetValue("origin", out var originString))
                {
                    origin = ParsingUtilities.ParseVector3(originString);
                }

                ConvertModel(modelNumber.Value, _bspFile.Models[modelNumber.Value], origin);
            }

            WriteObjFiles();
        }

        private void WriteObjFiles()
        {
            var baseName = Path.GetFileNameWithoutExtension(_bspFileName);
            var materialBaseName = baseName + ".mtl";
            var objDestinationFileName = Path.Combine(_destinationDirectory, baseName + ".obj");
            var objMaterialDestinationFileName = Path.Combine(_destinationDirectory, materialBaseName);

            var version = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).FileVersion ?? "Unknown version";

            var header = $" Generated by Half-Life Unified SDK Bsp2Obj {version}\n";

            _objFile.HeaderText = header + _objFile.HeaderText;

            _objFile.MaterialLibraries.Add(materialBaseName);

            _logger.Information("Writing OBJ file {FileName}", objDestinationFileName);
            _objFile.WriteTo(objDestinationFileName);

            _objMaterialFile.HeaderText = header + _objMaterialFile.HeaderText;

            _logger.Information("Writing OBJ material file {FileName}", objMaterialDestinationFileName);
            _objMaterialFile.WriteTo(objMaterialDestinationFileName);
        }

        private void WriteMaterials()
        {
            ObjMaterial CreateMaterial()
            {
                ObjMaterial material = new()
                {
                    Name = $"material_{_objMaterialFile.Materials.Count}",
                    IlluminationModel = 0,
                    DiffuseColor = new(1, 1, 1),
                };

                _objMaterialFile.Materials.Add(material);

                return material;
            }

            // All textures embedded in the map can be added, others use a dummy material.
            if (_bspFile.Textures.Any(t => t.NumMips == 0))
            {
                _dummyMaterial = CreateMaterial();
            }

            var mapBuilder = ImmutableDictionary.CreateBuilder<int, ObjMaterial>();

            var baseName = Path.GetFileNameWithoutExtension(_bspFileName);

            var writer = new TextureWriter(_logger, _destinationDirectory);

            for (var i = 0; i < _bspFile.Textures.Count; ++i)
            {
                var texture = _bspFile.Textures[i];

                if (TexturesToIgnore.Contains(texture.Name))
                {
                    continue;
                }

                if (texture.NumMips == 0)
                {
                    mapBuilder.Add(i, _dummyMaterial!);
                }
                else
                {
                    var material = CreateMaterial();

                    material.DiffuseMap = new()
                    {
                        FileName = $"{baseName}_{material.Name}.tga"
                    };

                    mapBuilder.Add(i, material);

                    writer.Write(material.DiffuseMap.FileName, texture);
                }
            }

            _materialsMap = mapBuilder.ToImmutable();
        }

        private int? TryGetModelNumber(Entity entity, int index)
        {
            if (entity.ClassName != "worldspawn")
            {
                if (!entity.KeyValues.TryGetValue("model", out var model) || !model.StartsWith("*"))
                {
                    return null;
                }

                if (!int.TryParse(model.AsSpan()[1..], out var modelNumber))
                {
                    return null;
                }

                // Don't allow the world to be used by other entities.
                if (modelNumber <= 0 || modelNumber >= _bspFile.Models.Count)
                {
                    return null;
                }

                return modelNumber;
            }
            else if (index != 0)
            {
                // Ignore redundant worldspawn entities.
                return null;
            }

            return 0;
        }

        private void ConvertModel(int modelNumber, Model model, Vector3 origin)
        {
            ObjGroup group = new()
            {
                Name = $"BSP_Object.model_{modelNumber}"
            };

            foreach (var face in Enumerable
                .Range(model.FirstFace, model.NumFaces)
                .Select(i => _bspFile.Faces[i]))
            {
                var textureInfo = _bspFile.Texinfo[face.TextureInfo];
                var mipTextureIndex = textureInfo.MipTexture;
                var mipTexture = _bspFile.Textures[mipTextureIndex];

                if (TexturesToIgnore.Contains(mipTexture.Name))
                {
                    continue;
                }

                ObjFace objFace = new()
                {
                    MaterialName = _materialsMap[mipTextureIndex].Name
                };

                foreach (var vertex in GetEdgeVertices(face, textureInfo, mipTexture, origin))
                {
                    objFace.Vertices.Add(vertex);
                }

                _objFile.Faces.Add(objFace);
                group.Faces.Add(objFace);
            }

            _objFile.Groups.Add(group);
        }

        private IEnumerable<ObjTriplet> GetEdgeVertices(Face face, TextureInfo textureInfo, MipTexture texture, Vector3 origin)
        {
            // Vertices need to be reversed to match the OBJ format.
            foreach (var vertex in Enumerable
                .Range(face.FirstEdge, face.NumEdges)
                .Reverse()
                .Select(GetVertex))
            {
                // Indices are 1-based.
                yield return new ObjTriplet(
                    GetVertexIndex(vertex + origin),
                    GetTextureCoordinatesIndex(vertex, textureInfo, texture),
                    0);
            }
        }

        private Vector3 GetVertex(int surfEdgeIndex)
        {
            var edgeIndex = _bspFile.Surfedges[surfEdgeIndex];
            var edge = _bspFile.Edges[Math.Abs(edgeIndex)];
            return _bspFile.Vertices[edgeIndex > 0 ? edge.Start : edge.End];
        }

        private int GetVertexIndex(Vector3 vertex)
        {
            var objVertex = new ObjVertex(vertex.X, vertex.Y, vertex.Z);

            var index = _objFile.Vertices.IndexOf(objVertex);

            if (index == -1)
            {
                _objFile.Vertices.Add(objVertex);
                index = _objFile.Vertices.Count - 1;
            }

            return index + 1;
        }

        private int GetTextureCoordinatesIndex(Vector3 vertex, TextureInfo textureInfo, MipTexture texture)
        {
            var sOffset = new Vector3(textureInfo.S.X, textureInfo.S.Y, textureInfo.S.Z);
            var tOffset = new Vector3(textureInfo.T.X, textureInfo.T.Y, textureInfo.T.Z);

            var s = Vector3.Dot(vertex, sOffset) + textureInfo.S.W;
            s /= texture.Width;

            var t = Vector3.Dot(vertex, tOffset) + textureInfo.T.W;
            t /= texture.Height;

            // T is inverted for the OBJ format.
            t = -t;

            var objVertex = new ObjVector3(s, t);

            var index = _objFile.TextureVertices.IndexOf(objVertex);

            if (index == -1)
            {
                _objFile.TextureVertices.Add(objVertex);
                index = _objFile.TextureVertices.Count - 1;
            }

            return index + 1;
        }
    }
}
