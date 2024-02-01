using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MergedQuery : Query
{
    public List<Query> subqueries;


    public override bool CheckChanges()
    {
        foreach (Query q in subqueries)
            if (q.CheckChanges())
                return true;
        return false;
    }

    internal void UnmergeQuery()
    {
        qm.UnmergeQueriesFromMergedQuery(this);
    }

    public override bool CheckChangesRecomputeAndResetFlag()
    {
        foreach (Query q in subqueries)
        { 
            if (q.CheckChangesRecomputeAndResetFlag())
            {
                RecomputeQueryResults();
                return true;
            }
        }
        return false;
    }

    public override bool CheckChangesAndResetFlag()
    {
        bool changes = false;

        foreach (Query q in subqueries)
        {
            if (q.CheckChangesAndResetFlag())
            {
                changes = true;
            }
        }
        return changes;
    }


    public override void DisableColliders()
    {
        foreach (Query q in subqueries)
            q.DisableColliders();
    }

    public override void EnableColliders()
    {
        foreach (Query q in subqueries)
            q.EnableColliders();
    }

    public override Vector3 GetCentralPosition2D()
    {
        return subqueries[0].GetCentralPosition2D();
        //throw new System.NotImplementedException();
    }

    public override Vector3 GetCentralPosition3D()
    {
        return subqueries[0].GetCentralPosition3D();
        //throw new System.NotImplementedException();
    }

    public override void RecomputeFilterTexture()
    {
        foreach (Query q in subqueries)
        {
            if (q.filterTextureAsTex2D == null)
                q.RecomputeFilterTexture();
        }

        if (filterTextureAsTex2D == null)
        {
            filterTextureAsTex2D = new Texture2D(subqueries[0].filterTextureAsTex2D.width, subqueries[0].filterTextureAsTex2D.height);
        }

        if (filterTexture == null)
        {
            filterTexture = new RenderTexture(qm.texSize, qm.texSize, 24);
            filterTexture.enableRandomWrite = true;
            filterTexture.filterMode = FilterMode.Point;
            filterTexture.Create();
        }

        FilterTextureCombiner.ClearFilterTexture(filterTextureAsTex2D);

        foreach (Query q in subqueries)
        {
            //filterTextureAsTex2D = FilterTextureCombiner.CombineTextures2DWithOr(filterTextureAsTex2D, q.filterTextureAsTex2D);
            filterTexture = FilterTextureCombiner.CombineTexturesWithOr(filterTexture, q.filterTexture);
        }
    }

    public override void RefreshColor()
    {
        foreach (Query q in subqueries)
        {
            q.queryColor = this.queryColor;
            q.RefreshColor();
        }
    }

    public override void RemoveQuery()
    {
        qm.RemoveMergedQuery(this);

        qm.availableColors.Enqueue(queryColor);

        Destroy(this.gameObject);
    }

    public override void SetTransparency(float a)
    {
        foreach (Query q in subqueries)
            q.SetTransparency(a);
    }

    public override void UpdateQueryAfterSTCInteraction()
    {
        foreach (Query q in subqueries)
        {
            q.UpdateQueryAfterSTCInteraction();
        }
        UpdateStatsTooltipPosition();
    }

    public override void UpdateStatsTooltipPosition()
    {
        if (queryStatsTooltip != null)
        {
            queryStatsTooltip.AnchorPosition = GetCentralPosition2D();
            queryStatsTooltip.PivotPosition = queryStatsTooltip.AnchorPosition + new Vector3(0.25f, 0.25f, 0f);
        }
    }

    public void InitializeQuery()
    {
        qm = subqueries[0].qm;
        type = QueryType.Merged;
        //RefreshArrow();

        foreach(Query q in subqueries)
            q.transform.parent = this.transform;
        this.transform.parent = qm.transform;

        foreach (Query q in subqueries)
        {
            if(q is DirectionalQuery)
            {
                ((DirectionalQuery)q).originQuery.associatedMergedQuery = this;
                ((DirectionalQuery)q).destinationQuery.associatedMergedQuery = this;
            }
            if (q is RecurrentQuery)
            {
                foreach (Query slice in ((RecurrentQuery)q).slices)
                    slice.associatedMergedQuery = this;
            }
        }

        // CREATE QUERY BUTTONS

        foreach (Query q in subqueries)
            q.HideButtons();

        myButtons = Instantiate(qm.mqButtonsPrefab);
        myButtons.name = "Query Buttons";
        myButtons.GetComponent<MergedQueryButtonsController>().myQuery = this;
        myButtons.transform.parent = this.transform;

        // CREATE QUERY TOOLTIP

        queryStatsTooltip = Instantiate(subqueries[0].queryStatsTooltip).GetComponent<ToolTip>();
        queryStatsTooltip.name = "Query Summary Tooltip";
        queryStatsTooltip.transform.parent = this.transform;

        foreach (Query q in subqueries)
            q.HideTooltip();

        // DEAL WITH QUERY COLORS

        queryColor = subqueries[0].queryColor;

        foreach(Query q in subqueries)
        {
            if(q.queryColor != queryColor)
            {
                qm.availableColors.Enqueue(q.queryColor);
                q.queryColor = queryColor;
                q.RefreshColor();
            }
        }
        //arrow.startColor = queryColor;
        //arrow.endColor = queryColor;

        AudioSource.PlayClipAtPoint(qm.sm.goodSoundClip, subqueries[0].GetCentralPosition3D());

    }

    public void ReinitializeQuery()
    {
        foreach (Query q in subqueries)
            q.transform.parent = this.transform;
        this.transform.parent = qm.transform;

        foreach (Query q in subqueries)
        {
            if (q is DirectionalQuery)
            {
                ((DirectionalQuery)q).originQuery.associatedMergedQuery = this;
                ((DirectionalQuery)q).destinationQuery.associatedMergedQuery = this;
            }
            if (q is RecurrentQuery)
            {
                foreach (Query slice in ((RecurrentQuery)q).slices)
                    slice.associatedMergedQuery = this;
            }
        }

        foreach (Query q in subqueries)
            q.HideButtons();

        foreach (Query q in subqueries)
            q.HideTooltip();

        // DEAL WITH QUERY COLORS

        queryColor = subqueries[0].queryColor;

        foreach (Query q in subqueries)
        {
            if (q.queryColor != queryColor)
            {
                qm.availableColors.Enqueue(q.queryColor);
                q.queryColor = queryColor;
                q.RefreshColor();
            }
        }
        //arrow.startColor = queryColor;
        //arrow.endColor = queryColor;

        AudioSource.PlayClipAtPoint(qm.sm.goodSoundClip, subqueries[0].GetCentralPosition3D());

    }
}
