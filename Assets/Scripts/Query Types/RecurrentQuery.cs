using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecurrentQuery : Query
{
    public List<AtomicQuery> slices = new List<AtomicQuery>();

    public GameObject querySpatialPolygon, querySpatialLine, queryLineAndDotsAnchor;
    public List<GameObject> queryListOfSpatialDots;

    private Vector3 querySliceScale, queryPolygonPosition;

    public bool SystemUpdatingQueries = false;


    [Header("Recurrent Query Selections")]

    public List<int> years;
    public List<int> months;
    public List<DayOfWeek> daysOfTheWeek;
    public List<int> hours;




    void Start()
    {
        Time.fixedDeltaTime = 0.5f;
    }

    void FixedUpdate()
    {
        
        bool aSliceWasMovedOrScaled = false;

        if(!SystemUpdatingQueries)
        { 
            if (querySpatialPolygon.transform.position.x != queryPolygonPosition.x ||
                querySpatialPolygon.transform.position.z != queryPolygonPosition.z) // Indicates the query map projection was moved in space and all slices should follow
            {
                aSliceWasMovedOrScaled = true;
                queryPolygonPosition = new Vector3(querySpatialPolygon.transform.position.x,
                    queryPolygonPosition.y,
                    querySpatialPolygon.transform.position.z);
            }
            else if (querySpatialPolygon.transform.localScale.x != querySliceScale.x ||
                querySpatialPolygon.transform.localScale.z != querySliceScale.z) // Indicates the query map projection was moved in space and all slices should follow
            {
                aSliceWasMovedOrScaled = true;
                querySliceScale = new Vector3(querySpatialPolygon.transform.localScale.x,
                    querySliceScale.y,
                    querySpatialPolygon.transform.localScale.z);
            }
            else // otherwise let's check if any of the slices were moved
            {
                foreach (AtomicQuery slice in slices)
                {
                    if (slice.queryPrism.transform.position.x != queryPolygonPosition.x ||
                        slice.queryPrism.transform.position.z != queryPolygonPosition.z) // Indicates one slice was moved in space and all should follow
                    {
                        aSliceWasMovedOrScaled = true;
                        queryPolygonPosition = new Vector3(slice.queryPrism.transform.position.x,
                            queryPolygonPosition.y,
                            slice.queryPrism.transform.position.z);
                    }
                    if (slice.queryPrism.transform.localScale.x != querySliceScale.x ||
                        //slice.queryPrism.transform.localScale.y != querySliceScale.y ||
                        slice.queryPrism.transform.localScale.z != querySliceScale.z) // Indicates one slice was scaled in space and all should follow
                    {
                        aSliceWasMovedOrScaled = true;
                        querySliceScale = new Vector3(slice.queryPrism.transform.localScale.x,
                            querySliceScale.y, // blocking y scaling (see bellow)
                            slice.queryPrism.transform.localScale.z);
                    }
                    if (aSliceWasMovedOrScaled)
                        break;
                }
            }

            if(aSliceWasMovedOrScaled)
            {
                querySpatialPolygon.transform.position = new Vector3(queryPolygonPosition.x, querySpatialPolygon.transform.position.y, queryPolygonPosition.z);

                foreach (AtomicQuery slice in slices)
                {
                    slice.queryPrism.transform.position = new Vector3(queryPolygonPosition.x, slice.queryPrism.transform.position.y, queryPolygonPosition.z);
                    slice.queryPrism.transform.localScale = new Vector3(querySliceScale.x, slice.queryPrism.transform.localScale.y, querySliceScale.z);
                }
                aSliceWasMovedOrScaled = false;
            }
        }      
    }





    public override void RecomputeFilterTexture()
    {
        foreach (AtomicQuery slice in slices)
        {
            //if (slice.filterTexture == null)
                slice.RecomputeFilterTexture();
        }

        if (filterTextureAsTex2D == null)
        {
            filterTextureAsTex2D = new Texture2D(slices[0].filterTextureAsTex2D.width, slices[0].filterTextureAsTex2D.height);
        }
        if (filterTexture == null)
        {
            filterTexture = new RenderTexture(qm.texSize, qm.texSize, 24);
            filterTexture.enableRandomWrite = true;
            filterTexture.filterMode = FilterMode.Point;
            filterTexture.Create();
        }

        ClearFilterTexture(filterTexture);

        foreach (AtomicQuery slice in slices)
        {
            //filterTexture = FilterTextureCombiner.CombineTexturesWithOr(filterTexture, slice.filterTexture);
            filterTexture = qm.CombineTexturesWithOrUsingCS(filterTexture, slice.filterTexture);
        }
    }

    public override bool CheckChanges()
    {
        foreach (AtomicQuery slice in slices)
        {
            if (slice.CheckChanges())
                return true;
        }

        return false;
    }

    public override bool CheckChangesRecomputeAndResetFlag()
    {
        bool foundChanges = false; 

        foreach (AtomicQuery slice in slices)
        {
            if (slice.CheckChangesRecomputeAndResetFlag())
                foundChanges = true;
        }

        if(foundChanges)
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
        bool foundChanges = false;

        foreach (AtomicQuery slice in slices)
        {
            if (slice.CheckChangesAndResetFlag())
                foundChanges = true;
        }

        return foundChanges;
    }

    public void InitializeQuery()
    {
        qm = slices[0].qm;
        type = slices[0].type;
        this.transform.parent = qm.transform;
        querySliceScale = slices[0].queryPrism.transform.localScale;
        queryPolygonPosition = slices[0].querySpatialPolygon.transform.position;

        // Let's adopt the map projection references from the first slice as the unified ones for this RQ

        querySpatialPolygon = slices[0].querySpatialPolygon;
        querySpatialPolygon.transform.parent = this.transform;
        queryLineAndDotsAnchor = slices[0].queryLineAndDotsAnchor;
        queryLineAndDotsAnchor.transform.parent = this.transform;
        querySpatialLine = slices[0].querySpatialLine;
        querySpatialLine.transform.parent = queryLineAndDotsAnchor.transform;
        queryListOfSpatialDots = slices[0].queryListOfSpatialDots;

        // Let's replace map projection references to the unified ones for each slice except the first one

        for (int i = 1; i < slices.Count; i++) 
        {
            slices[i].HideMapProjections();

            Destroy(slices[i].querySpatialPolygon);
            slices[i].querySpatialPolygon = querySpatialPolygon;
            Destroy(slices[i].querySpatialLine);
            slices[i].querySpatialLine = querySpatialLine;
            Destroy(slices[i].queryLineAndDotsAnchor);
            slices[i].queryLineAndDotsAnchor = queryLineAndDotsAnchor;
            foreach (GameObject originalDot in slices[i].queryListOfSpatialDots)
            {
                Destroy(originalDot);
            }
            slices[i].queryListOfSpatialDots = queryListOfSpatialDots;

            if (type == QueryType.Origin)
                slices[i].SetQueryModeToOnlyPickups();
            else if (type == QueryType.Destination)
                slices[i].SetQueryModeToOnlyDropoffs();
            if (type == QueryType.Either)
                slices[i].SetQueryModeToPickupsOrDropoffs();
        }

        // Let's unify the query buttons and tooltips

        foreach (AtomicQuery slice in slices)
        {
            slice.transform.name = "Query Slice";
            slice.transform.parent = this.transform;
            slice.isPartOfARecurrentQuery = true;
            slice.associatedRecurrentQuery = this;
            slice.HideButtons();
            slice.HideTooltip();

            // As an implementation choice, let's block temporal (y) interactions on the slices to preserve the user's recurrent time selections

            foreach (QueryWallProjection qwp in slice.queryWallProjs)
            {
                qwp.DisableColliders();
                qwp.HideWidgets();
            }
            MoveAxisConstraint mac = slice.queryPrism.gameObject.AddComponent<MoveAxisConstraint>();
            mac.ConstraintOnMovement = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.YAxis;

            MinMaxScaleConstraint mmsc = slice.queryPrism.gameObject.AddComponent<MinMaxScaleConstraint>();
            mmsc.ScaleMinimum = 1f;
            mmsc.ScaleMaximum = 1f;

            //Destroy(slice.queryPrism.GetComponent<Interactable>());
            Microsoft.MixedReality.Toolkit.UI.Interactable interac = slice.queryPrism.gameObject.GetComponent<Interactable>();
            interac.OnClick.RemoveAllListeners();
            interac.OnClick.AddListener(delegate { qm.HandleQueryPrismClick(this); });
            Microsoft.MixedReality.Toolkit.UI.ObjectManipulator om = slice.queryPrism.gameObject.GetComponent<Microsoft.MixedReality.Toolkit.UI.ObjectManipulator>();
            om.OnHoverEntered.RemoveAllListeners();
            om.OnHoverEntered.AddListener(delegate { qm.HandleQueryPrismHover(this); });


        }

        // CREATE NEW QUERY BUTTONS

        myButtons = Instantiate(qm.rqButtonsPrefab);
        myButtons.SetActive(true);
        myButtons.name = "Query Buttons";
        myButtons.GetComponent<RecurrentQueryButtonsController>().myQuery = this;
        myButtons.transform.parent = this.transform;

        // CREATE NEW QUERY TOOLTIP

        queryStatsTooltip = Instantiate(slices[0].queryStatsTooltip).GetComponent<ToolTip>();
        queryStatsTooltip.gameObject.SetActive(true);
        queryStatsTooltip.name = "Query Summary Tooltip";
        queryStatsTooltip.transform.parent = this.transform;
        UpdateStatsTooltipPosition();

        // DEAL WITH QUERY COLORS

        queryColor = slices[0].queryColor;


        SystemUpdatingQueries = false;

        AudioSource.PlayClipAtPoint(qm.sm.goodSoundClip, querySpatialPolygon.transform.position);
    }


    public override void DisableColliders()
    {
        foreach (AtomicQuery slice in slices)
        {
            slice.DisableColliders();
        }
    }

    public override void EnableColliders()
    {
        foreach (AtomicQuery slice in slices)
        {
            slice.EnableColliders();
        }
    }

    public override void RemoveQuery()
    {
        qm.RemoveRecurrentQuery(this);

        qm.availableColors.Enqueue(queryColor);

        Destroy(this.gameObject);
    }


    public override void SetTransparency(float a)
    {
        foreach (AtomicQuery slice in slices)
        {
            slice.SetTransparency(a);
        }
    }

    public override void UpdateQueryAfterSTCInteraction()
    {
        SystemUpdatingQueries = true;
        foreach (AtomicQuery slice in slices)
        {
            slice.UpdateQueryAfterSTCInteraction();
        }
        SystemUpdatingQueries = false; 
        UpdateStatsTooltipPosition();
    }

    public override void UpdateStatsTooltipPosition()
    {
        if (queryStatsTooltip != null)
        {
            queryStatsTooltip.AnchorPosition = querySpatialPolygon.transform.position;
            queryStatsTooltip.PivotPosition = querySpatialPolygon.transform.position + new Vector3(0.25f, 0.25f, 0f);
        }
    }

    public void SetQueryModeToPickupsOrDropoffs()
    {
        foreach (AtomicQuery slice in slices)
        {
            slice.SetQueryModeToPickupsOrDropoffs();
        }

        type = QueryType.Either;
    }

    public void SetQueryModeToOnlyPickups()
    {
        foreach (AtomicQuery slice in slices)
        {
            slice.SetQueryModeToOnlyPickups();
        }

        type = QueryType.Origin;
    }

    public void SetQueryModeToOnlyDropoffs()
    {
        foreach (AtomicQuery slice in slices)
        {
            slice.SetQueryModeToOnlyDropoffs();
        }

        type = QueryType.Destination;
    }

    public void AdoptTimeSelector(RecurrentQueryTimeSelectorMenu timeSelector)
    {
        timeSelector.gameObject.SetActive(false);
        timeSelector.transform.parent = myButtons.transform;
        timeSelector.qbc = myButtons.GetComponent<RecurrentQueryButtonsController>();
        timeSelector.LoadPreselections(years, months, daysOfTheWeek, hours);
        myButtons.GetComponent<RecurrentQueryButtonsController>().timeSelector = timeSelector; 
    }

    public override Vector3 GetCentralPosition2D()
    {
        return querySpatialPolygon.transform.position;
    }

    public override void RefreshColor()
    {
        foreach (AtomicQuery slice in slices)
        {
            slice.queryColor = this.queryColor;
            slice.RefreshColor();
        }
    }

    public override Vector3 GetCentralPosition3D()
    {
        Vector3 medium = Vector3.zero;
        foreach (AtomicQuery slice in slices)
        {
            medium += slice.GetCentralPosition3D();
        }

        medium /= slices.Count;

        if (Vector3.Distance(medium, GetCentralPosition2D()) < 2f) 
            return medium;
        else // if the slices are very far from the map the medium position can be impossible to interact with
            return GetCentralPosition2D();
    }
}
