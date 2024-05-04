using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LmpPalette : ScriptableObject
{
    public List<Color> colors = new List<Color>();
    public List<Color> brightColors = new List<Color>();
    public Color transparentColor;

    public bool isTransparent(Color32 c) { return c == transparentColor; }
    public bool isBrightColor(Color32 c)
    {
        foreach (var bc in brightColors)
        {
            if (c == bc) return true;
        }
        return false;
    }
}
