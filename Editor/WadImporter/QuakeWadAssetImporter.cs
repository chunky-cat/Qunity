using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

namespace Qunity
{
    /// <summary>
    /// Import any files with the .map extension
    /// </summary>
    [ScriptedImporter(1, "wad", AllowCaching = true)]
    public sealed class QuakeWADAssetImporter : ScriptedImporter
    {
        private enum WadEntryType
        {
            Palette = 0x40,
            SBarPic = 0x42,
            MipsTexture = 0x44,
            ConsolePic = 0x45
        }

        private struct WadEntry
        {
            public uint offset;
            public uint inWadSize;
            public uint size;
            public byte type;
            public byte compression;
            public ushort unknown;
            public string nameStr;
        }

        private struct TextureData
        {
            public string name;
            public uint width;
            public uint height;
            public byte[] pixelData;

            public TextureData(string name, uint width, uint height, byte[] pixelData)
            {
                this.name = name;
                this.width = width;
                this.height = height;
                this.pixelData = pixelData;
            }
        }

        public LmpPalette palette;

        [Header("Export")]
        public string folder = "Assets/textures";
        [Space(10)]
        [InspectorButton("ExportTextures")]
        public bool exportTextures;

        private const int TEXTURE_NAME_LENGTH = 16;
        private const int MAX_MIP_LEVELS = 4;

        [HideInInspector]
        public WadTexture2DCollection collection;


        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (palette == null)
            {
                palette = (LmpPalette)AssetDatabase.LoadAssetAtPath("Packages/com.chunkycat.qunity/Assets/palette.lmp", typeof(LmpPalette));
            }
            Stream s = new FileStream(ctx.assetPath, FileMode.Open);
            BinaryReader br = new BinaryReader(s);
            var wadCollection = ScriptableObject.CreateInstance<WadTexture2DCollection>();
            wadCollection.wadName = ctx.assetPath;

            try
            {
                var magicStr = new string(br.ReadChars(4));
                if (magicStr != "WAD2")
                {
                    throw new Exception("invalid header: " + magicStr);
                }

                uint numEntries = br.ReadUInt32();
                uint dirOffset = br.ReadUInt32();

                br.BaseStream.Seek(dirOffset, SeekOrigin.Begin);



                var entries = new List<WadEntry>();
                for (int i = 0; i < numEntries; i++)
                {
                    WadEntry entry = new WadEntry();
                    entry.offset = br.ReadUInt32();
                    entry.inWadSize = br.ReadUInt32();
                    entry.size = br.ReadUInt32();
                    entry.type = br.ReadByte();
                    entry.compression = br.ReadByte();
                    entry.unknown = br.ReadUInt16();
                    entry.nameStr = new string(br.ReadChars(TEXTURE_NAME_LENGTH));
                    if (entry.type == (byte)WadEntryType.MipsTexture)
                    {
                        entries.Add(entry);
                    }
                }

                var textureDataArray = new TextureData[entries.Count];
                for (int i = 0; i < entries.Count; i++)
                {
                    br.BaseStream.Seek(entries[i].offset, SeekOrigin.Begin);

                    string nameStr = new string(br.ReadChars(TEXTURE_NAME_LENGTH));
                    uint width = br.ReadUInt32();
                    uint height = br.ReadUInt32();

                    uint[] mipOffsets = new uint[MAX_MIP_LEVELS];
                    for (int j = 0; j < MAX_MIP_LEVELS; j++)
                    {
                        mipOffsets[j] = br.ReadUInt32();
                    }

                    textureDataArray[i] = new TextureData(nameStr, width, height, br.ReadBytes((int)(width * height)));
                }
                br.Close();

                Span<TextureData> textureDataSpan = textureDataArray;
                for (int i = 0; i < textureDataSpan.Length; i++)
                {
                    ref TextureData tex = ref textureDataSpan[i];

                    var pixelsRgb = new Color32[tex.pixelData.Length];
                    int k = 0;
                    for (int h = (int)tex.height - 1; h > 0; h--)
                    {
                        for (int w = 0; w < tex.width; w++)
                        {
                            var rgbColor = palette.colors[tex.pixelData[w + (h * tex.height)]];
                            pixelsRgb[k] = rgbColor;
                            k++;
                        }
                    }

                    var texImage = new Texture2D((int)tex.width, (int)tex.height);
                    texImage.SetPixels32(pixelsRgb);
                    texImage.Apply(true);
                    texImage.filterMode = FilterMode.Point;
                    texImage.name = tex.name;

                    ctx.AddObjectToAsset(tex.name, texImage);
                    wadCollection.textures.Add(texImage);
                }

                ctx.AddObjectToAsset("collection", wadCollection);
                ctx.SetMainObject(wadCollection);
                collection = wadCollection;
            }
            catch (Exception e)
            {
                br.Close();
                Debug.LogException(e);
                throw e.InnerException;
            }
        }

        public void ExportTextures()
        {
            folder = folder.Trim('/').TrimEnd('\\');
            Directory.CreateDirectory(folder);
            foreach (var tex in collection.textures)
            {
                var fullPath = folder + "/" + tex.name + ".png";
                fullPath = fullPath.Replace("*", "").Replace("+", "");
                byte[] _bytes = tex.EncodeToPNG();
                File.WriteAllBytes(fullPath, _bytes);
            }
        }
    }
}
