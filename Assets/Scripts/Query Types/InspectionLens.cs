using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IATK;
using Microsoft.MixedReality.Toolkit.UI;
using System;

public class InspectionLens : Query
{
    public IATKViewFilter brush;

    [Header("Visuals")]
    public Material inspectorStandardMaterial;
    public Material inspectorHighlightedMaterial;
    public MeshRenderer visualRenderer;
    public ToolTip tooltipPrefab;
    public bool renderSelectedTripLines = true; // true
    public List<LineRenderer> tripLines = new List<LineRenderer>();

    bool inspecting = false;
   
    //ToolTip myTooltip = null;

    // Start is called before the first frame update
    void Start()
    {
        Time.fixedDeltaTime = 0.5f;

        if (renderSelectedTripLines)
            RetrievePositions = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(qm == null)
            if (GameObject.Find("QueryManager"))
                qm = GameObject.Find("QueryManager").GetComponent<QueryManager>();

        if (brush.brushedViews.Count == 0)
        {
            if (GameObject.Find("STC-Pickups"))
                brush.brushedViews.Add(GameObject.Find("STC-Pickups").GetComponent<View>());

            if (GameObject.Find("STC-Dropoffs"))
                brush.brushedViews.Add(GameObject.Find("STC-Dropoffs").GetComponent<View>());
        }

        if (brush.brushedLinkingViews.Count == 0)
        {
            if (GameObject.Find("TaxiSTCManager"))
            {
                brush.brushedLinkingViews = new List<LinkingViews>();
                brush.brushedLinkingViews.Add(GameObject.Find("TaxiSTCManager").GetComponent<LinkingViews>());
            }
        }


        if (brush.brushedViews.Count != 0 && qm.FingerInspectionLensIsActive)
        {
            RecomputeQueryResults();

            if(numberOfFilteredPoints == 0 && inspecting)
            {
                visualRenderer.material = inspectorStandardMaterial;
                if(queryStatsTooltip != null)
                    queryStatsTooltip.gameObject.SetActive(false);
                foreach(LineRenderer tripLine in tripLines)
                {
                    tripLine.enabled = false;
                    Destroy(tripLine.gameObject);
                }
                tripLines = new List<LineRenderer>();
                inspecting = false; 
            }
            else if (numberOfFilteredPoints > 0)
            {
                if(!inspecting)
                {
                    AudioSource.PlayClipAtPoint(qm.sm.miniGoodSoundClip, this.transform.position);

                    visualRenderer.material = inspectorHighlightedMaterial;
                    if(queryStatsTooltip == null)
                    {
                        queryStatsTooltip = GameObject.Instantiate(tooltipPrefab).GetComponent<ToolTip>();
                        queryStatsTooltip.transform.parent = this.transform;
                    }
                    queryStatsTooltip.gameObject.SetActive(true);

                    inspecting = true;
                }

                UpdateStatsTooltip();

                if(renderSelectedTripLines)
                {
                    foreach (LineRenderer tripLine in tripLines)
                    {
                        tripLine.enabled = false;
                        Destroy(tripLine.gameObject);
                    }
                    tripLines = new List<LineRenderer>();

                    for (int i=0; i<tripOriginsWorldScale.Count; i++)
                    {
                        GameObject newLine = new GameObject();
                        newLine.transform.parent = this.transform;
                        LineRenderer tripLine = newLine.AddComponent<LineRenderer>();
                        tripLines.Add(tripLine);
                        tripLine.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
                        tripLine.useWorldSpace = true;
                        tripLine.positionCount = 2;
                        tripLine.SetPosition(0, tripOriginsWorldScale[i]);
                        tripLine.SetPosition(1, tripDestinationsWorldScale[i]);
                        tripLine.startColor = Color.blue;
                        tripLine.endColor = Color.red;
                        //tripLine.startColor = Color.green;
                        //tripLine.endColor = Color.green;
                        tripLine.startWidth = 0.0015f;
                        tripLine.endWidth = 0.0015f;
                    }
                }

            }
        }

    }

    public override void RecomputeQueryResults()
    {
        RecomputeFilterTexture();
        DeductAttributeAndTemporalConstraints();
        RecomputeQueryStats();
    }

    public override void RecomputeFilterTexture()
    {
        brush.Refilter();

        if (filterTextureAsTex2D == null)
            filterTextureAsTex2D = new Texture2D(brush.texSize, brush.texSize);

        if(filterTexture == null)
        {
            filterTexture = new RenderTexture(qm.texSize, qm.texSize, 24);
            filterTexture.enableRandomWrite = true;
            filterTexture.filterMode = FilterMode.Point;
            filterTexture.Create();
        }

        RenderTexture rt = RenderTexture.active;

        RenderTexture.active = brush.brushedIndicesTexture;
        filterTextureAsTex2D.ReadPixels(new Rect(0, 0, filterTextureAsTex2D.width, filterTextureAsTex2D.height), 0, 0, false);
        filterTextureAsTex2D.Apply();

        if (qm.allQueries.Count > 0) // IF WE HAVE ACTIVE QUERIES, LET'S TAKE THAT INTO ACCOUNT SO THAT WE DO NOT INSPECT FILTERED OUT POINTS
        {
            
                RenderTexture comb = FilterTextureCombiner.CombineTexturesWithAnd(qm.linkedViewFilterTexture, filterTextureAsTex2D);
                RenderTexture.active = comb;
                filterTextureAsTex2D.ReadPixels(new Rect(0, 0, comb.width, comb.height), 0, 0, false);
                filterTextureAsTex2D.Apply();
            
        }

        RenderTexture.active = filterTexture;
        Graphics.Blit(filterTextureAsTex2D, filterTexture);

        RenderTexture.active = rt;
    }


    public override void UpdateStatsTooltipPosition()
    {
        queryStatsTooltip.AnchorPosition = this.transform.position;
        queryStatsTooltip.PivotPosition = this.transform.position + new Vector3(0f, 0.1f, 0.1f);
    }


    public override bool CheckChangesRecomputeAndResetFlag()
    {
        throw new NotImplementedException();
    }

    public override bool CheckChangesAndResetFlag()
    {
        throw new NotImplementedException();
    }

    public override void DisableColliders()
    {
        throw new NotImplementedException();
    }

    public override void EnableColliders()
    {
        throw new NotImplementedException();
    }

    public override void SetTransparency(float a)
    {
        throw new NotImplementedException();
    }

    public override void UpdateQueryAfterSTCInteraction()
    {
        throw new NotImplementedException();
    }

    public override void RemoveQuery()
    {
        throw new NotImplementedException();
    }

    public override bool CheckChanges()
    {
        throw new NotImplementedException();
    }

    public override Vector3 GetCentralPosition2D()
    {
        return this.transform.position;
    }

    public override void RefreshColor()
    {
        throw new NotImplementedException();
    }

    public override Vector3 GetCentralPosition3D()
    {
        return this.transform.position;
    }
}
