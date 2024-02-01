using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FilterTextureCombiner : object
{
    public static RenderTexture CombineTexturesWithOr(RenderTexture tex1, RenderTexture tex2)
    {
        Texture2D t1_as_tex2d = new Texture2D(tex1.width, tex1.height);
        Texture2D t2_as_tex2d = new Texture2D(tex2.width, tex2.height);

        RenderTexture rt = RenderTexture.active;

        RenderTexture.active = tex1;
        t1_as_tex2d.ReadPixels(new Rect(0, 0, tex1.width, tex1.height), 0, 0, false);
        t1_as_tex2d.Apply();

        RenderTexture.active = tex2;
        t2_as_tex2d.ReadPixels(new Rect(0, 0, tex2.width, tex2.height), 0, 0, false);
        t2_as_tex2d.Apply();

        RenderTexture.active = rt;

        return CombineTextures(true, t1_as_tex2d, t2_as_tex2d);
    }

    public static void CombineTexturesWithOr(RenderTexture tex1, RenderTexture tex2, out RenderTexture result)
    {
        Texture2D t1_as_tex2d = new Texture2D(tex1.width, tex1.height);
        Texture2D t2_as_tex2d = new Texture2D(tex2.width, tex2.height);

        RenderTexture rt = RenderTexture.active;

        RenderTexture.active = tex1;
        t1_as_tex2d.ReadPixels(new Rect(0, 0, tex1.width, tex1.height), 0, 0, false);
        t1_as_tex2d.Apply();

        RenderTexture.active = tex2;
        t2_as_tex2d.ReadPixels(new Rect(0, 0, tex2.width, tex2.height), 0, 0, false);
        t2_as_tex2d.Apply();

        RenderTexture.active = rt;

        CombineTextures(true, t1_as_tex2d, t2_as_tex2d, out result);
    }

    public static RenderTexture CombineTexturesWithAnd(RenderTexture tex1, RenderTexture tex2)
    {
        Texture2D t1_as_tex2d = new Texture2D(tex1.width, tex1.height);
        Texture2D t2_as_tex2d = new Texture2D(tex2.width, tex2.height);

        RenderTexture rt = RenderTexture.active;

        RenderTexture.active = tex1;
        t1_as_tex2d.ReadPixels(new Rect(0, 0, tex1.width, tex1.height), 0, 0, false);
        t1_as_tex2d.Apply();

        RenderTexture.active = tex2;
        t2_as_tex2d.ReadPixels(new Rect(0, 0, tex2.width, tex2.height), 0, 0, false);
        t2_as_tex2d.Apply();

        RenderTexture.active = rt;

        return CombineTextures(false, t1_as_tex2d, t2_as_tex2d);
    }

    public static RenderTexture CombineTexturesWithOr(RenderTexture tex1, Texture2D tex2)
    {
        Texture2D t1_as_tex2d = new Texture2D(tex1.width, tex1.height);

        RenderTexture rt = RenderTexture.active;

        RenderTexture.active = tex1;
        t1_as_tex2d.ReadPixels(new Rect(0, 0, tex1.width, tex1.height), 0, 0, false);
        t1_as_tex2d.Apply();

        return CombineTextures(true, t1_as_tex2d, tex2);
    }

    public static RenderTexture CombineTexturesWithAnd(RenderTexture tex1, Texture2D tex2)
    {
        Texture2D t1_as_tex2d = new Texture2D(tex1.width, tex1.height);

        RenderTexture rt = RenderTexture.active;

        RenderTexture.active = tex1;
        t1_as_tex2d.ReadPixels(new Rect(0, 0, tex1.width, tex1.height), 0, 0, false);
        t1_as_tex2d.Apply();

        RenderTexture.active = rt;

        return CombineTextures(false, t1_as_tex2d, tex2);
    }

    public static RenderTexture CombineTexturesWithOr(Texture2D tex1, Texture2D tex2)
    {
        return CombineTextures(true, tex1, tex2);
    }

    public static RenderTexture CombineTexturesWithAnd(Texture2D tex1, Texture2D tex2)
    {
        return CombineTextures(false, tex1, tex2);
    }

    public static Texture2D CombineTextures2DWithAnd(Texture2D tex1, Texture2D tex2)
    {
        return CombineTextures2D(false, tex1, tex2);
    }

    public static Texture2D CombineTextures2DWithOr(Texture2D tex1, Texture2D tex2)
    {
        return CombineTextures2D(true, tex1, tex2);
    }



    public static RenderTexture CombineTextures(bool combineUsingOrLogic, Texture2D tex1, Texture2D tex2) // IN THE FUTURE THIS SHOULD LIKELY BE IMPROVED BY USING A SHADER TO COMBINE TEXTURES
    {
        RenderTexture result = new RenderTexture(tex1.width, tex1.height, 24);
        result.enableRandomWrite = true;
        result.filterMode = FilterMode.Point;
        result.Create();
        ClearFilterTexture(result);
        RenderTexture.active = result;

        Texture2D result_as_tex2d = new Texture2D(tex1.width, tex1.height);

        RenderTexture rt = RenderTexture.active;

        for (int x = 0; x < tex1.width; x++)
        {
            for (int y = 0; y < tex1.height; y++)
            {
                if (combineUsingOrLogic)
                {
                    if (tex1.GetPixel(x, y).r == 1f || tex2.GetPixel(x, y).r == 1f)
                        //if (t1_as_tex2d.GetPixel(x, y) == Color.red || t2_as_tex2d.GetPixel(x, y) == Color.red)
                        result_as_tex2d.SetPixel(x, y, Color.red);
                    else
                        result_as_tex2d.SetPixel(x, y, Color.black);
                }
                else
                {
                    if (tex1.GetPixel(x, y).r == 1f && tex2.GetPixel(x, y).r == 1f)
                        //if (t1_as_tex2d.GetPixel(x, y) == Color.red || t2_as_tex2d.GetPixel(x, y) == Color.red)
                        result_as_tex2d.SetPixel(x, y, Color.red);
                    else
                        result_as_tex2d.SetPixel(x, y, Color.black);
                }
            }
        }
        result_as_tex2d.Apply();

        Graphics.Blit(result_as_tex2d, result);

        //RenderTexture.active = rt;

        return result;
    }

    public static void CombineTextures(bool combineUsingOrLogic, Texture2D tex1, Texture2D tex2, out RenderTexture result) // IN THE FUTURE THIS SHOULD LIKELY BE IMPROVED BY USING A SHADER TO COMBINE TEXTURES
    {
        Texture2D result_as_tex2d = new Texture2D(tex1.width, tex1.height);

        RenderTexture rt = RenderTexture.active;

        for (int x = 0; x < tex1.width; x++)
        {
            for (int y = 0; y < tex1.height; y++)
            {
                if (combineUsingOrLogic)
                {
                    if (tex1.GetPixel(x, y).r == 1f || tex2.GetPixel(x, y).r == 1f)
                        //if (t1_as_tex2d.GetPixel(x, y) == Color.red || t2_as_tex2d.GetPixel(x, y) == Color.red)
                        result_as_tex2d.SetPixel(x, y, Color.red);
                    else
                        result_as_tex2d.SetPixel(x, y, Color.black);
                }
                else
                {
                    if (tex1.GetPixel(x, y).r == 1f && tex2.GetPixel(x, y).r == 1f)
                        //if (t1_as_tex2d.GetPixel(x, y) == Color.red || t2_as_tex2d.GetPixel(x, y) == Color.red)
                        result_as_tex2d.SetPixel(x, y, Color.red);
                    else
                        result_as_tex2d.SetPixel(x, y, Color.black);
                }
            }
        }
        result_as_tex2d.Apply();


        result = new RenderTexture(tex1.width, tex1.height, 24);
        result.enableRandomWrite = true;

        RenderTexture.active = result;

        Graphics.Blit(result_as_tex2d, result);

        RenderTexture.active = rt;

        //return result;
    }




    public static Texture2D CombineTextures2D(bool combineUsingOrLogic, Texture2D tex1, Texture2D tex2) // IN THE FUTURE THIS SHOULD LIKELY BE IMPROVED BY USING A SHADER TO COMBINE TEXTURES
    {
        Texture2D result = new Texture2D(tex1.width, tex1.height);

        for (int x = 0; x < tex1.width; x++)
        {
            for (int y = 0; y < tex1.height; y++)
            {
                if (combineUsingOrLogic)
                {
                    if (tex1.GetPixel(x, y).r == 1f || tex2.GetPixel(x, y).r == 1f)
                        result.SetPixel(x, y, Color.red);
                    else
                        result.SetPixel(x, y, Color.black);
                }
                else
                {
                    if (tex1.GetPixel(x, y).r == 1f && tex2.GetPixel(x, y).r == 1f)
                        result.SetPixel(x, y, Color.red);
                    else
                        result.SetPixel(x, y, Color.black);
                }
            }
        }
        result.Apply();

        return result;
    }

    public static RenderTexture ClearFilterTexture(RenderTexture renderTexture)
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = rt;
        return renderTexture;
    }

    public static RenderTexture FillFilterTexture(RenderTexture renderTexture)
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.red);
        RenderTexture.active = rt;
        return renderTexture;
    }

    public static void ClearFilterTexture(Texture2D texture)
    {
        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                texture.SetPixel(x, y, Color.black);
            }
        }
        texture.Apply();
    }

    public static void FillFilterTexture(Texture2D texture)
    {
        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                texture.SetPixel(x, y, Color.red);
            }
        }
        texture.Apply();
    }


}
