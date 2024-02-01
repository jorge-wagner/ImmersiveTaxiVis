using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionalQuery : Query
{
    public Query originQuery, destinationQuery;
    public LineRenderer arrow;
    private bool justInitialized = false; 

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (justInitialized)
        {
            RefreshArrow();
            UpdateStatsTooltipPosition();
            justInitialized = false;
        }

        if (originQuery.CheckChanges() || destinationQuery.CheckChanges())
        {
            if(originQuery.gameObject.activeSelf && destinationQuery.gameObject.activeSelf)
                RefreshArrow();
        }
    }

    public override void RecomputeFilterTexture()
    {
        if (filterTextureAsTex2D == null)
            filterTextureAsTex2D = new Texture2D(qm.texSize, qm.texSize);

        if (originQuery.filterTexture == null)
            originQuery.RecomputeFilterTexture();

        if (destinationQuery.filterTexture == null)
            destinationQuery.RecomputeFilterTexture();

        //filterTexture = FilterTextureCombiner.CombineTexturesWithAnd(originQuery.filterTexture, destinationQuery.filterTexture);
        filterTexture = qm.CombineTexturesWithAndUsingCS(originQuery.filterTexture, destinationQuery.filterTexture);
    }


    void RefreshArrow()
    {
        arrow.SetPosition(0, originQuery.GetCentralPosition2D() + new Vector3(0, 0.01f, 0));
        arrow.SetPosition(1, destinationQuery.GetCentralPosition2D() + new Vector3(0, 0.01f, 0));
        float arrowLength = Vector3.Distance(arrow.GetPosition(0), arrow.GetPosition(1));
        if (6 * arrowLength >= 1)
            arrow.textureScale = new Vector2(-6 * arrowLength, 1);
        else
            arrow.textureScale = new Vector2(-1, 1);
    }

    public void InitializeQuery()
    {
        qm = originQuery.qm;
        type = QueryType.Directional;
        RefreshArrow();

        originQuery.transform.parent = this.transform;
        destinationQuery.transform.parent = this.transform;
        this.transform.parent = qm.transform;

        // CREATE QUERY BUTTONS

        originQuery.HideButtons();
        destinationQuery.HideButtons();

        myButtons = Instantiate(qm.dqButtonsPrefab);//.GetComponent<DirectionalQueryButtonsController>();
        myButtons.name = "Query Buttons";
        myButtons.GetComponent<DirectionalQueryButtonsController>().myQuery = this;
        myButtons.transform.parent = this.transform;

        // CREATE QUERY TOOLTIP

        queryStatsTooltip = Instantiate(originQuery.queryStatsTooltip).GetComponent<ToolTip>();
        queryStatsTooltip.name = "Query Summary Tooltip";
        queryStatsTooltip.transform.parent = this.transform;

        originQuery.HideTooltip();
        destinationQuery.HideTooltip();

        // DEAL WITH QUERY COLORS

        qm.availableColors.Enqueue(destinationQuery.queryColor);
        queryColor = originQuery.queryColor;
        destinationQuery.queryColor = queryColor;
        destinationQuery.RefreshColor();
        arrow.startColor = queryColor;
        arrow.endColor = queryColor;

        AudioSource.PlayClipAtPoint(qm.sm.goodSoundClip, arrow.transform.position);

        justInitialized = true;
    }

    /*
    public void FinalizeQuery()
    {
        originQuery.RevealButtons();
        destinationQuery.RevealButtons();

        originQuery.RevealTooltip();
        destinationQuery.RevealTooltip();

        originQuery.transform.parent = qm.transform;
        destinationQuery.transform.parent = qm.transform;

        // DEAL WITH QUERY COLORS

        if (qm.availableColors.Count >= 1)
            destinationQuery.queryColor = qm.availableColors.Dequeue();
        else
            destinationQuery.queryColor = Color.black;

        destinationQuery.RefreshColor();

        myButtons.SetActive(false);
        GameObject.Destroy(myButtons);
        GameObject.Destroy(queryStatsTooltip.gameObject);
        arrow.enabled = false;
        GameObject.Destroy(this.gameObject);
    }*/


    public void UnlinkQuery()
    {
        qm.UnmergeQueriesFromDirectionalQuery(this);
    }









    public override void DisableColliders()
    {
        originQuery.DisableColliders();
        destinationQuery.DisableColliders();
    }

    public override void EnableColliders()
    {
        originQuery.EnableColliders();
        destinationQuery.EnableColliders();
    }

    public override void SetTransparency(float a)
    {
        originQuery.SetTransparency(a);
        destinationQuery.SetTransparency(a);
    }

    public override void UpdateQueryAfterSTCInteraction()
    {
        originQuery.UpdateQueryAfterSTCInteraction();
        destinationQuery.UpdateQueryAfterSTCInteraction();
        RefreshArrow();
        UpdateStatsTooltipPosition();
    }

    public override void RemoveQuery()
    {
        qm.RemoveDirectionalQuery(this);

        qm.availableColors.Enqueue(queryColor);

        // If one of our subqueries is also part of other directional queries, we should not destroy it

        if(originQuery.associatedDirectionalQueries.Count > 1)
        {
            originQuery.associatedDirectionalQueries.Remove(this);
            originQuery.transform.parent = originQuery.associatedDirectionalQueries[0].transform;
            originQuery.queryColor = originQuery.associatedDirectionalQueries[0].queryColor;
            originQuery.RefreshColor(); // just in case it was colored in our color
        }

        if (destinationQuery.associatedDirectionalQueries.Count > 1)
        {
            destinationQuery.associatedDirectionalQueries.Remove(this);
            destinationQuery.transform.parent = destinationQuery.associatedDirectionalQueries[0].transform;
            destinationQuery.queryColor = destinationQuery.associatedDirectionalQueries[0].queryColor;
            destinationQuery.RefreshColor(); // just in case it was colored in our color
        }

        Destroy(this.gameObject);
    }


    public override void UpdateStatsTooltipPosition()
    {
        if (queryStatsTooltip != null)
        {
            queryStatsTooltip.AnchorPosition = GetCentralPosition2D(); 
            queryStatsTooltip.PivotPosition = queryStatsTooltip.AnchorPosition + new Vector3(0.25f, 0.25f, 0f);
        }
    }

    public override bool CheckChanges()
    {
        if (originQuery.CheckChanges() || destinationQuery.CheckChanges())
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public override bool CheckChangesRecomputeAndResetFlag()
    {
        if(originQuery.CheckChangesRecomputeAndResetFlag() || destinationQuery.CheckChangesRecomputeAndResetFlag())
        {
            RecomputeQueryResults();
            return true;
        }
        else
        {
            return false;
        }
    }

    public override bool CheckChangesAndResetFlag()
    {
        bool o = originQuery.CheckChangesAndResetFlag();
        bool d = destinationQuery.CheckChangesAndResetFlag();

        if (o || d)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public override void RefreshColor()
    {
        arrow.startColor = queryColor;
        arrow.endColor = queryColor;
        originQuery.queryColor = this.queryColor;
        originQuery.RefreshColor();
        destinationQuery.queryColor = this.queryColor;
        destinationQuery.RefreshColor();
    }

    public override Vector3 GetCentralPosition2D()
    {
        return (arrow.GetPosition(0) + arrow.GetPosition(1)) / 2f;
    }

    public override Vector3 GetCentralPosition3D()
    {
        return (originQuery.GetCentralPosition3D() + destinationQuery.GetCentralPosition3D()) / 2f;
    }
}
