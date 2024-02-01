using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System.Collections.Generic;
using UnityEngine;

public class PointCounter : Query
{
    public IATKViewFilter queryIATKDataVertexIntersectionComputer;
    public List<GameObject> queryListOfSpatialDots;
    public float queryHeight;
    public float minY;
    public float maxY;

    public override void RecomputeQueryResults()
    {
        RecomputeFilterTexture();

        if(qm.allQueries.Count > 0 || qm.acm.isConstraining)
            DeductFilteredOutPoints();
        //DeductAttributeAndTemporalConstraints();
        //RecomputeQueryStats();
    }

    void DeductFilteredOutPoints()
    {
        filterTexture = qm.CombineTexturesWithAndUsingCS(filterTexture, qm.linkedViewFilterTexture);
    }

    public override void RecomputeFilterTexture()
    {
        queryIATKDataVertexIntersectionComputer.Refilter();

        if (filterTextureAsTex2D == null)
            filterTextureAsTex2D = new Texture2D(qm.texSize, qm.texSize);

        if (filterTexture == null)
        {
            filterTexture = new RenderTexture(qm.texSize, qm.texSize, 24);
            filterTexture.enableRandomWrite = true;
            filterTexture.filterMode = FilterMode.Point;
            filterTexture.Create();
        }

        RenderTexture rt = RenderTexture.active;

        RenderTexture.active = queryIATKDataVertexIntersectionComputer.brushedIndicesTexture;
        filterTextureAsTex2D.ReadPixels(new Rect(0, 0, filterTextureAsTex2D.width, filterTextureAsTex2D.height), 0, 0, false);
        filterTextureAsTex2D.Apply();

        RenderTexture.active = filterTexture;
        Graphics.Blit(filterTextureAsTex2D, filterTexture);

        RenderTexture.active = rt;
    }

    public int lastKnownNumberOfFilteredPoints = -1;

    public int RecomputeNumberOfFilteredPoints()
    {
        RecomputeQueryResults();

        RenderTexture.active = filterTexture;
        filterTextureAsTex2D.ReadPixels(new Rect(0, 0, filterTextureAsTex2D.width, filterTextureAsTex2D.height), 0, 0, false);
        filterTextureAsTex2D.Apply();


        int numberOfFilteredPoints = 0;

        for (int x = 0; x < filterTextureAsTex2D.width; x++)
        {
            for (int y = 0; y < filterTextureAsTex2D.height; y++)
            {
                if (filterTextureAsTex2D.GetPixel(x, y).r == 1f)
                {
                    numberOfFilteredPoints++;
                }
            }
        }

        lastKnownNumberOfFilteredPoints = numberOfFilteredPoints; 

        return numberOfFilteredPoints;
    }



    public void Start()
    {
    }


    public void Update()
    {
    }


    public override bool CheckChangesRecomputeAndResetFlag()
    {
        throw new System.NotImplementedException();
    }

    public override bool CheckChangesAndResetFlag()
    {
        throw new System.NotImplementedException();
    }

    public override void DisableColliders()
    {
        throw new System.NotImplementedException();
    }

    public override void EnableColliders()
    {
        throw new System.NotImplementedException();
    }

    public override void SetTransparency(float a)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateQueryAfterSTCInteraction()
    {
        throw new System.NotImplementedException();
    }

    public override void RemoveQuery()
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateStatsTooltipPosition()
    {
        throw new System.NotImplementedException();
    }

    public override bool CheckChanges()
    {
        throw new System.NotImplementedException();
    }

    public override Vector3 GetCentralPosition2D()
    {
        throw new System.NotImplementedException();
    }

    public override void RefreshColor()
    {
        throw new System.NotImplementedException();
    }

    public override Vector3 GetCentralPosition3D()
    {
        throw new System.NotImplementedException();
    }
}