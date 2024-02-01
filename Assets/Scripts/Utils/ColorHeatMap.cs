//using System.Windows.Media;// for WPF
// for WindowsForms using System.Drawing
using System;
using System.Collections.Generic;
using UnityEngine;



// SOURCE: Adapted from https://stackoverflow.com/a/37911674

public class ColorHeatMap
{
    public float Alpha = 255;// 0xff;
    public List<Color32> ColorsOfMap = new List<Color32>();

    public ColorHeatMap()
    {
        initColorsBlocks();
    }
    public ColorHeatMap(float alpha)
    {
        this.Alpha = alpha;
        initColorsBlocks();
    }
    private void initColorsBlocks()
    {
        /*ColorsOfMap.AddRange(new Color[]{
            Color.FromArgb(Alpha, 0, 0, 0) ,//Black
            Color.FromArgb(Alpha, 0, 0, 0xFF) ,//Blue
            Color.FromArgb(Alpha, 0, 0xFF, 0xFF) ,//Cyan
            Color.FromArgb(Alpha, 0, 0xFF, 0) ,//Green
            Color.FromArgb(Alpha, 0xFF, 0xFF, 0) ,//Yellow
            Color.FromArgb(Alpha, 0xFF, 0, 0) ,//Red
            Color.FromArgb(Alpha, 0xFF, 0xFF, 0xFF) // White
        });*/
        ColorsOfMap.AddRange(new Color32[]{
            new Color32(255, 245, 240, 255),
            new Color32(254, 224, 210, 255),
            new Color32(252, 187, 161, 255),
            new Color32(252, 146, 114, 255),
            new Color32(251, 106,  74, 255),
            new Color32(239,  59,  44, 255),
            new Color32(203,  24,  29, 255),
            new Color32(165,  15,  21, 255),
            new Color32(103,   0,  13, 255)
        });
    }
    public Color32 GetColorForValue(float val, float maxVal)
    {
        float valPerc = val / (maxVal + 1);// value%
        float colorPerc = 1f / (ColorsOfMap.Count - 1);// % of each block of color. the last is the "100% Color"
        float blockOfColor = valPerc / colorPerc;// the integer part repersents how many block to skip
        int blockIdx = (int)Math.Truncate(blockOfColor);// Idx of 
        float valPercResidual = valPerc - (blockIdx * colorPerc);//remove the part represented of block 
        float percOfColor = valPercResidual / colorPerc;// % of color of this block that will be filled

        Color32 cTarget = ColorsOfMap[blockIdx];
        Color32 cNext = cNext = ColorsOfMap[blockIdx + 1];

        float deltaR = cNext.r - cTarget.r;
        float deltaG = cNext.g - cTarget.g;
        float deltaB = cNext.b - cTarget.b;

        float R = cTarget.r + (deltaR * percOfColor);
        float G = cTarget.g + (deltaG * percOfColor);
        float B = cTarget.b + (deltaB * percOfColor);

        Color32 c = ColorsOfMap[0];
        try
        {
            c = new Color32((byte)R, (byte)G, (byte)B, (byte)Alpha);
        }
        catch (Exception ex)
        {
        }
        return c;
    }

}