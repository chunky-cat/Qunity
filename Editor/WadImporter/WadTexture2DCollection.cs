using System;
using System.Collections.Generic;
using UnityEngine;


public class WadTexture2DCollection : ScriptableObject
{
    public List<Texture2D> textures = new List<Texture2D>();
    public string wadName;

    public Texture2D FindTexture(string name)
    {
        foreach (var tex in textures)
        {
            if (tex.name == name)
            {
                return tex;
            }
        }

        return null;
    }
}
