using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LmpPalette : ScriptableObject
{
    public List<Color> colors = new List<Color>();
    public List<Color> brightColors = new List<Color>();
    public Color transparentColor;

    public bool isTransparent(Color c) { return c == transparentColor; }
    public bool isBrightColor(Color c)
    {
        foreach (var bc in brightColors)
        {
            if (bc == c) return true;
        }
        return false;
    }
}
