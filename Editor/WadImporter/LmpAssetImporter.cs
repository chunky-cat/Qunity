using UnityEditor.AssetImporters;
using UnityEngine;
using System;
using System.IO;
using Unity.VisualScripting;

namespace Qunity
{
    /// <summary>
    /// Import any files with the .map extension
    /// </summary>
    [ScriptedImporter(1, "lmp", AllowCaching = true)]
    public sealed class LmpAssetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Stream s = new FileStream(ctx.assetPath, FileMode.Open);
            try
            {
                BinaryReader br = new BinaryReader(s);
                var pal = ScriptableObject.CreateInstance<LmpPalette>();
                int i = 0;
                while (br.BaseStream.Position != br.BaseStream.Length)
                {
                    var r = br.ReadByte() / 255.0f;
                    var g = br.ReadByte() / 255.0f;
                    var b = br.ReadByte() / 255.0f;
                    var color = new Color(r, g, b);
                    pal.colors.Add(color);
                    if (i == 255) { pal.transparentColor = color; }
                    if (i >= 240 && i < 255) { pal.brightColors.Add(color); }
                    i++;
                }
                br.Close();
                ctx.AddObjectToAsset("Palette", pal);
                ctx.SetMainObject(pal);
            }
            catch (Exception e)
            {
                s.Close();
                throw e;
            }
        }
    }
}