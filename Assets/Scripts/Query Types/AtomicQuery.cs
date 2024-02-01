using IATK;
using Microsoft.Maps.Unity;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Rendering;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class AtomicQuery : Query
{
    [Header("Query Components")]

    public IATKViewFilter queryIATKDataVertexIntersectionComputer;
    public GameObject queryPrism, querySpatialPolygon, querySpatialLine, queryLineAndDotsAnchor, queryWallProjsAnchor;
    public List<QueryWallProjection> queryWallProjs;
    public List<GameObject> queryListOfSpatialDots; // this should be moved to QueryMapProjection
    public Microsoft.Geospatial.LatLonAlt[] dotsLatLons = null; // this should be moved to QueryMapProjection

    [Header("Query containers")]
    public bool isPartOfARecurrentQuery = false;
    public RecurrentQuery associatedRecurrentQuery = null;


    [Header("Query internal variables")]

    public float queryHeight;
    public float minY;
    public float maxY;

    public DateTime queryInitialTime;
    public TimeSpan queryTimeSpan;

    private Vector3 previousPrismPosition = Vector3.zero;
    private Vector3 previousPrismScale = Vector3.one;

    public bool prismChanged = true;



    public override void RecomputeFilterTexture()
    {
        queryIATKDataVertexIntersectionComputer.Refilter();

        if (filterTextureAsTex2D == null)
            filterTextureAsTex2D = new Texture2D(queryIATKDataVertexIntersectionComputer.texSize, queryIATKDataVertexIntersectionComputer.texSize);

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


    // Start is called before the first frame update
    void Start()
    {
        Time.fixedDeltaTime = 0.1f;
        previousSTCPos = qm.sm.stc.transform.position;
        previousSTCScale = qm.sm.stc.transform.localScale;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (queryPrism.transform.position != previousPrismPosition || queryPrism.transform.localScale != previousPrismScale)
        {
            UpdateQueryAfterPrismInteraction();
            prismChanged = true; 
        }
        previousPrismPosition = queryPrism.transform.position;
        previousPrismScale = queryPrism.transform.localScale; 
    }

    public override void UpdateStatsTooltipPosition()
    {
        if (queryStatsTooltip != null && queryStatsTooltip.gameObject.activeSelf)
        {
            queryStatsTooltip.AnchorPosition = querySpatialPolygon.transform.position;
            queryStatsTooltip.PivotPosition = querySpatialPolygon.transform.position + new Vector3(0.25f, 0.25f, 0f);
        }
    }


    private Vector3 previousSTCScale = Vector3.one, previousSTCPos = Vector3.zero;

    public override void UpdateQueryAfterSTCInteraction()
    {
        /*
        //queryPrism.transform.position += new Vector3(0, qm.sm.stc.baseHeight - minY, 0);
        //queryPrism.transform.position += new Vector3(qm.sm.stc.transform.position.x - previousSTCPos.x, qm.sm.stc.transform.position.y - previousSTCPos.y, qm.sm.stc.transform.position.z - previousSTCPos.z);
        queryPrism.transform.position += new Vector3(0, qm.sm.stc.transform.position.y - previousSTCPos.y, 0);
        //queryPrism.transform.localScale = new Vector3(queryPrism.transform.localScale.x, queryPrism.transform.localScale.y * qm.sm.stc.transform.localScale.y / previousSTCScale.y, queryPrism.transform.localScale.z);
        queryPrism.transform.localScale = new Vector3(queryPrism.transform.localScale.x, queryPrism.transform.localScale.y * qm.sm.stc.transform.localScale.y / previousSTCScale.y, queryPrism.transform.localScale.z);
        //queryPrism.transform.localScale = new Vector3(queryPrism.transform.localScale.x, , queryPrism.transform.localScale.z);
        //queryPrism.transform.localScale = new Vector3(queryPrism.transform.localScale.x * qm.sm.stc.transform.localScale.x / previousSTCScale.x, queryPrism.transform.localScale.y * qm.sm.stc.transform.localScale.y / previousSTCScale.y, queryPrism.transform.localScale.z * qm.sm.stc.transform.localScale.z / previousSTCScale.z);

        querySpatialPolygon.transform.position = new Vector3(querySpatialPolygon.transform.position.x, qm.sm.bingMap.transform.position.y + qm.sm.bingMap.transform.localScale.y * 0.055f + (qm.sm.bingMap.mapRenderer.IsClippingVolumeWallEnabled ? 0.039f : 0f), querySpatialPolygon.transform.position.z);
        queryLineAndDotsAnchor.transform.position = new Vector3(querySpatialPolygon.transform.position.x, qm.sm.bingMap.transform.position.y + qm.sm.bingMap.transform.localScale.y * 0.055f + (qm.sm.bingMap.mapRenderer.IsClippingVolumeWallEnabled ? 0.039f : 0f), querySpatialPolygon.transform.position.z);

        //AdjustWallProjectionsVertically();

        //minY = qm.sm.stc.baseHeight;
        //maxY = minY + queryPrism.transform.localScale.y; //qm.sm.stc.transform.localScale.y; 
        //queryHeight = maxY - minY;

        minY = queryPrism.transform.position.y - queryPrism.transform.localScale.y / 2f;
        maxY = queryPrism.transform.position.y + queryPrism.transform.localScale.y / 2f;
        queryHeight = maxY - minY;
        */

        querySpatialPolygon.transform.position = new Vector3(querySpatialPolygon.transform.position.x, qm.sm.bingMap.transform.position.y + qm.sm.bingMap.transform.localScale.y * 0.055f + (qm.sm.bingMap.mapRenderer.IsClippingVolumeWallEnabled ? 0.039f : 0f), querySpatialPolygon.transform.position.z);
        queryLineAndDotsAnchor.transform.position = new Vector3(querySpatialPolygon.transform.position.x, qm.sm.bingMap.transform.position.y + qm.sm.bingMap.transform.localScale.y * 0.055f + (qm.sm.bingMap.mapRenderer.IsClippingVolumeWallEnabled ? 0.039f : 0f), querySpatialPolygon.transform.position.z);

        if(qm.sm.stc.timeDirection == STCManager.Direction.LatestOnTop)
        {
            SetQueryTimeSpanFromHeights(qm.sm.stc.mapTimeToY(queryInitialTime), qm.sm.stc.mapTimeToY(queryInitialTime + queryTimeSpan));
        }
        else
        {
            SetQueryTimeSpanFromHeights(qm.sm.stc.mapTimeToY(queryInitialTime + queryTimeSpan), qm.sm.stc.mapTimeToY(queryInitialTime));
        }


        if (qm.sm.stc.transform.position.y == previousSTCPos.y || qm.sm.stc.transform.localScale.x != previousSTCScale.x) // means interaction was map interaction and not time interaction, or that a map ref transform switch happened
        {
            RegenPrismsAfterMapUpdate();
        }
        else // means it was a time interaction
        {
            AdjustWallProjectionsVertically();
            //AdjustWallProjectionsAfterPrismInteractions();
            //AdjustMapProjectionAfterPrismInteractions();
        }

        UpdateStatsTooltipPosition();

        //UpdatePolygonAndLineAfterSTCInteraction();
        // Note: 2D Polygon, Line and Dots are updated somewhere else (where?)

        previousSTCPos = qm.sm.stc.transform.position;
        previousSTCScale = qm.sm.stc.transform.localScale;
    }

    private void UpdateQueryAfterPrismInteraction()
    {
        minY = queryPrism.transform.position.y - queryPrism.transform.localScale.y / 2f;
        maxY = queryPrism.transform.position.y + queryPrism.transform.localScale.y / 2f;
        queryHeight = maxY - minY;

        AdjustWallProjectionsAfterPrismInteractions();
        AdjustMapProjectionAfterPrismInteractions();

        if(dotsLatLons != null)
            UpdateVerticesLatLonList();
    }

    public void SetQueryTimeSpanFromDates(DateTime time, TimeSpan timeSpan)
    {
        queryInitialTime = time;
        queryTimeSpan = timeSpan;

        if(qm.sm.stc.timeDirection == STCManager.Direction.LatestOnBottom)
        { 
            this.minY = qm.sm.stc.mapTimeToY(time + timeSpan);
            this.maxY = qm.sm.stc.mapTimeToY(time);
        }
        else
        {
            this.minY = qm.sm.stc.mapTimeToY(time);
            this.maxY = qm.sm.stc.mapTimeToY(time + timeSpan);
        }

        queryHeight = maxY - minY;

        queryPrism.transform.localScale = new Vector3(queryPrism.transform.localScale.x, queryHeight, queryPrism.transform.localScale.z);
        queryPrism.transform.position = new Vector3(queryPrism.transform.position.x, minY + queryHeight / 2, queryPrism.transform.position.z);

        //AdjustWallProjectionsVertically();

        //AdjustWallProjectionsAfterPrismInteractions();
        //AdjustMapProjectionAfterPrismInteractions();
    }

    public void SetQueryTimeSpanFromHeights(float minY, float maxY)
    {
        if (qm.sm.stc.timeDirection == STCManager.Direction.LatestOnBottom)
        {
            queryInitialTime = qm.sm.stc.mapYToTime(maxY);
            queryTimeSpan = qm.sm.stc.mapYToTime(minY) - qm.sm.stc.mapYToTime(maxY);
        }
        else
        {
            queryInitialTime = qm.sm.stc.mapYToTime(minY);
            queryTimeSpan = qm.sm.stc.mapYToTime(maxY) - qm.sm.stc.mapYToTime(minY);
        }

        this.minY = minY;
        this.maxY = maxY;
       
        queryHeight = maxY - minY;

        queryPrism.transform.localScale = new Vector3(queryPrism.transform.localScale.x, queryHeight, queryPrism.transform.localScale.z);
        queryPrism.transform.position = new Vector3(queryPrism.transform.position.x, minY + queryHeight / 2, queryPrism.transform.position.z);

        //AdjustWallProjectionsVertically();

        //AdjustWallProjectionsAfterPrismInteractions();
        //AdjustMapProjectionAfterPrismInteractions();
    }

    public void UpdateQueryAfterWallProjectionInteraction(float minY, float maxY)
    {
        this.minY = minY;
        this.maxY = maxY;
        queryHeight = maxY - minY;

        queryPrism.transform.localScale = new Vector3(queryPrism.transform.localScale.x, queryHeight, queryPrism.transform.localScale.z);
        queryPrism.transform.position = new Vector3(queryPrism.transform.position.x, minY + queryHeight / 2, queryPrism.transform.position.z);

        AdjustWallProjectionsVertically();
        
        //AdjustWallProjectionsAfterPrismInteractions();
        //AdjustMapProjectionAfterPrismInteractions();
    }

    public void UpdateQueryAfterMapProjectionInteraction()
    {
        queryPrism.transform.localScale = new Vector3(querySpatialPolygon.transform.localScale.x, queryPrism.transform.localScale.y, querySpatialPolygon.transform.localScale.z);
        queryPrism.transform.position = new Vector3(querySpatialPolygon.transform.position.x, queryPrism.transform.position.y, querySpatialPolygon.transform.position.z);

        //AdjustWallProjectionsAfterPrismInteractions();
        //AdjustMapProjectionAfterPrismInteractions();
    }


    public void AdjustWallProjectionsVertically()
    {
        // BACK
        queryWallProjs[0].SetProjectionScale(new Vector3(queryWallProjs[0].GetProjectionScale().x, queryPrism.transform.localScale.y, queryWallProjs[0].GetProjectionScale().z));
        queryWallProjs[0].SetProjectionPosition(new Vector3(queryWallProjs[0].GetProjectionPosition().x, queryPrism.transform.position.y, queryWallProjs[0].GetProjectionPosition().z));

        // LEFT
        queryWallProjs[1].SetProjectionScale(new Vector3(queryWallProjs[1].GetProjectionScale().x, queryPrism.transform.localScale.y, queryWallProjs[1].GetProjectionScale().z));
        queryWallProjs[1].SetProjectionPosition(new Vector3(queryWallProjs[1].GetProjectionPosition().x, queryPrism.transform.position.y, queryWallProjs[1].GetProjectionPosition().z));

        // RIGHT
        queryWallProjs[2].SetProjectionScale(new Vector3(queryWallProjs[2].GetProjectionScale().x, queryPrism.transform.localScale.y, queryWallProjs[2].GetProjectionScale().z));
        queryWallProjs[2].SetProjectionPosition(new Vector3(queryWallProjs[2].GetProjectionPosition().x, queryPrism.transform.position.y, queryWallProjs[2].GetProjectionPosition().z));

        //FRONT
        queryWallProjs[3].SetProjectionScale(new Vector3(queryWallProjs[3].GetProjectionScale().x, queryPrism.transform.localScale.y, queryWallProjs[3].GetProjectionScale().z));
        queryWallProjs[3].SetProjectionPosition(new Vector3(queryWallProjs[3].GetProjectionPosition().x, queryPrism.transform.position.y, queryWallProjs[3].GetProjectionPosition().z));

    }
    

    private void AdjustWallProjectionsAfterPrismInteractions()
    {
        // BACK
        queryWallProjs[0].SetProjectionScale(new Vector3(queryWallProjs[0].GetProjectionScale().x * (queryPrism.transform.localScale.x / previousPrismScale.x), queryPrism.transform.localScale.y, queryWallProjs[0].GetProjectionScale().z));
        queryWallProjs[0].SetProjectionPosition(new Vector3(queryPrism.transform.position.x, queryPrism.transform.position.y, queryWallProjs[0].GetProjectionPosition().z));

        // LEFT
        queryWallProjs[1].SetProjectionScale(new Vector3(queryWallProjs[1].GetProjectionScale().x * (queryPrism.transform.localScale.z / previousPrismScale.z), queryPrism.transform.localScale.y, queryWallProjs[1].GetProjectionScale().z));
        queryWallProjs[1].SetProjectionPosition(new Vector3(queryWallProjs[1].GetProjectionPosition().x, queryPrism.transform.position.y, queryPrism.transform.position.z));

        // RIGHT
        queryWallProjs[2].SetProjectionScale(new Vector3(queryWallProjs[2].GetProjectionScale().x * (queryPrism.transform.localScale.z / previousPrismScale.z), queryPrism.transform.localScale.y, queryWallProjs[2].GetProjectionScale().z));
        queryWallProjs[2].SetProjectionPosition(new Vector3(queryWallProjs[2].GetProjectionPosition().x, queryPrism.transform.position.y, queryPrism.transform.position.z));

        //FRONT
        queryWallProjs[3].SetProjectionScale(new Vector3(queryWallProjs[3].GetProjectionScale().x * (queryPrism.transform.localScale.x / previousPrismScale.x), queryPrism.transform.localScale.y, queryWallProjs[3].GetProjectionScale().z));
        queryWallProjs[3].SetProjectionPosition(new Vector3(queryPrism.transform.position.x, queryPrism.transform.position.y, queryWallProjs[3].GetProjectionPosition().z));

    }

    private void AdjustMapProjectionAfterPrismInteractions()
    {
        querySpatialPolygon.transform.position = new Vector3(queryPrism.transform.position.x, querySpatialPolygon.transform.position.y, queryPrism.transform.position.z);
        queryLineAndDotsAnchor.transform.position = new Vector3(queryPrism.transform.position.x, queryLineAndDotsAnchor.transform.position.y, queryPrism.transform.position.z);

        querySpatialPolygon.transform.localScale = new Vector3(queryPrism.transform.lossyScale.x, querySpatialPolygon.transform.localScale.y, queryPrism.transform.lossyScale.z);
        queryLineAndDotsAnchor.transform.localScale = new Vector3(queryPrism.transform.lossyScale.x, queryLineAndDotsAnchor.transform.localScale.y, queryPrism.transform.lossyScale.z);
    }


    //private void UpdatePolygonAndLineAfterSTCInteraction()
    //{
    //querySpatialPolygon.transform.localScale = new Vector3(queryPrism.transform.localScale.x, querySpatialPolygon.transform.localScale.y, queryPrism.transform.localScale.z);
    //.transform.localScale = new Vector3(queryPrism.transform.localScale.x, querySpatialPolygon.transform.localScale.y, queryPrism.transform.localScale.z);

    //}

    public void GenerateVerticesLatLonList()
    {
        //dotsLatLons = new List<Microsoft.Geospatial.LatLonAlt>(queryListOfSpatialDots.Count);
        dotsLatLons = new Microsoft.Geospatial.LatLonAlt[queryListOfSpatialDots.Count];
        //Debug.Log("Created latlonlist: " + queryListOfSpatialDots.Count + " " + dotsLatLons.Length);

        for (int i = 0; i < queryListOfSpatialDots.Count; i++)
        {
            dotsLatLons[i] = qm.sm.bingMap.mapRenderer.TransformWorldPointToLatLonAlt(queryListOfSpatialDots[i].transform.position);
            //Debug.Log(dotsLatLons[i].ToString());
        }
    }

    public void UpdateVerticesLatLonList()
    {
        //Debug.Log("latlons: " + dotsLatLons.Length + " / dots: " + queryListOfSpatialDots.Count);

        //if(qm.sm.bingMap.mapRenderer.isActiveAndEnabled && dotsLatLons.Length == queryListOfSpatialDots.Count)
        //{
            for (int i = 0; i < dotsLatLons.Length; i++)
            {
                dotsLatLons[i] = qm.sm.bingMap.mapRenderer.TransformWorldPointToLatLonAlt(queryListOfSpatialDots[i].transform.position);
            }
        //}
    }

    private void RegenPrismsAfterMapUpdate()
    {
        if (dotsLatLons == null)
            return;

        // Adjust dots

        //queryLineAndDotsAnchor.transform.position = Vector3.zero;
        for (int i = 0; i < dotsLatLons.Length; i++) 
        {
            Vector3 newPos = qm.sm.bingMap.mapRenderer.TransformLatLonAltToWorldPoint(dotsLatLons[i]);
            queryListOfSpatialDots[i].transform.parent = this.transform;
            //queryListOfSpatialDots[i].transform.position = new Vector3(newPos.x, qm.sm.bingMap.transform.position.y + qm.sm.bingMap.transform.localScale.y * 0.055f + (qm.sm.bingMap.mapRenderer.IsClippingVolumeWallEnabled ? 0.039f : 0f), newPos.z);
            queryListOfSpatialDots[i].transform.position = new Vector3(newPos.x, qm.sm.bingMap.transform.position.y + qm.sm.bingMap.transform.localScale.y * 0.055f, newPos.z);
        }
        Vector3[] newPositions = GetPointsAsVectorsXYZ();

        // Adjust line

        //queryLineAndDotsAnchor.transform.position = new Vector3(querySpatialPolygon.transform.position.x, qm.sm.bingMap.transform.position.y + qm.sm.bingMap.transform.localScale.y * 0.055f + (qm.sm.bingMap.mapRenderer.IsClippingVolumeWallEnabled ? 0.039f : 0f), querySpatialPolygon.transform.position.z);
        LineRenderer lr = querySpatialLine.GetComponent<LineRenderer>();
        querySpatialLine.transform.parent = this.transform;
        //querySpatialLine.transform.localScale = Vector3.one;
        for (int i = 0; i < queryListOfSpatialDots.Count; i++)
        {
            lr.SetPosition(i, querySpatialLine.transform.InverseTransformPoint(queryListOfSpatialDots[i].transform.position));
        }

        // Adjust base polygon

        ProBuilderMesh polygonMesh = querySpatialPolygon.GetComponent<ProBuilderMesh>();
        querySpatialPolygon.transform.position = Vector3.zero;
        querySpatialPolygon.transform.localScale = Vector3.one;
        polygonMesh.CreateShapeFromPolygon(newPositions, 0.002f, false);
        polygonMesh.CenterPivot(polygonMesh.selectedFaceIndexes.ToArray());
        //querySpatialPolygon.transform.position = new Vector3(querySpatialPolygon.transform.position.x, qm.sm.bingMap.transform.position.y + qm.sm.bingMap.transform.localScale.y * 0.055f + (qm.sm.bingMap.mapRenderer.IsClippingVolumeWallEnabled ? 0.039f : 0f), querySpatialPolygon.transform.position.z);
        querySpatialPolygon.transform.position = new Vector3(querySpatialPolygon.transform.position.x, qm.sm.bingMap.transform.position.y + qm.sm.bingMap.transform.localScale.y * 0.055f, querySpatialPolygon.transform.position.z);

        // Adjust query prism 

        float prism_y_pos = queryPrism.transform.position.y;
        float prism_y_sca = queryPrism.transform.localScale.y;
        ProBuilderMesh prismMesh = queryPrism.GetComponent<ProBuilderMesh>();
        queryPrism.transform.position = Vector3.zero;
        queryPrism.transform.localScale = Vector3.one;
        prismMesh.CreateShapeFromPolygon(newPositions, 1f, false);
        prismMesh.CenterPivot(prismMesh.selectedFaceIndexes.ToArray());
        queryPrism.transform.position = new Vector3(queryPrism.transform.position.x, prism_y_pos, queryPrism.transform.position.z);
        queryPrism.transform.localScale = new Vector3(queryPrism.transform.localScale.x, prism_y_sca, queryPrism.transform.localScale.z);

        // Adjust dots and line anchors 
        queryLineAndDotsAnchor.transform.position = querySpatialPolygon.transform.position;
        queryLineAndDotsAnchor.transform.localScale = Vector3.one;
        foreach (GameObject dot in queryListOfSpatialDots)
        {
            dot.transform.parent = queryLineAndDotsAnchor.transform;
            //dot.transform.localPosition = new Vector3(dot.transform.localPosition.x, 0, dot.transform.localPosition.z);
        }
        querySpatialLine.transform.parent = queryLineAndDotsAnchor.transform;
        //querySpatialLine.transform.localPosition = new Vector3(querySpatialLine.transform.localPosition.x, 0, querySpatialLine.transform.localPosition.z);



        // Adjust wall projections 

        float minx = 1000, maxx = -1000, minz = 1000, maxz = -1000;
        for (int i = 0; i < newPositions.Length; i++)
        {
            if (newPositions[i].x < minx)
                minx = newPositions[i].x;
            if (newPositions[i].x > maxx)
                maxx = newPositions[i].x;
            if (newPositions[i].z < minz)
                minz = newPositions[i].z;
            if (newPositions[i].z > maxz)
                maxz = newPositions[i].z;
        }

        // BACK
        queryWallProjs[0].SetProjectionScale(new Vector3(maxx - minx, queryPrism.transform.localScale.y, queryWallProjs[0].GetProjectionScale().z));
        queryWallProjs[0].SetProjectionPosition(new Vector3((maxx + minx) / 2f, queryPrism.transform.position.y, qm.sm.bingMap.transform.position.z + qm.sm.stc.transform.localScale.z / 2 - 0.001f));

        // LEFT
        queryWallProjs[1].SetProjectionScale(new Vector3(maxz - minz, queryPrism.transform.localScale.y, queryWallProjs[1].GetProjectionScale().z));
        queryWallProjs[1].SetProjectionPosition(new Vector3(qm.sm.bingMap.transform.position.x - qm.sm.stc.transform.localScale.x / 2 + 0.001f, queryPrism.transform.position.y, (maxz + minz) / 2f));

        // RIGHT
        queryWallProjs[2].SetProjectionScale(new Vector3(maxz - minz, queryPrism.transform.localScale.y, queryWallProjs[2].GetProjectionScale().z));
        queryWallProjs[2].SetProjectionPosition(new Vector3(qm.sm.bingMap.transform.position.x + qm.sm.stc.transform.localScale.x / 2 - 0.001f, queryPrism.transform.position.y, (maxz + minz) / 2f));

        // FRONT
        if (qm.sm.ClippedEgoRoom && qm.sm.EgoRoomIsFourWalled)
        {
            queryWallProjs[3].gameObject.SetActive(true);
            queryWallProjs[3].SetProjectionScale(new Vector3(maxx - minx, queryPrism.transform.localScale.y, queryWallProjs[3].GetProjectionScale().z));
            queryWallProjs[3].SetProjectionPosition(new Vector3((maxx + minx) / 2f, queryPrism.transform.position.y, qm.sm.bingMap.transform.position.z - qm.sm.stc.transform.localScale.z / 2 + 0.001f));
        }
        else
        {
            queryWallProjs[3].gameObject.SetActive(false);
        }


        // Adjust trackers to avoid mistaken prism interaction detection 
        previousPrismScale = queryPrism.transform.localScale;
        previousPrismPosition = queryPrism.transform.position;

    }

    private Vector3[] GetPointsAsVectorsXYZ()
    {
        Vector3[] array = new Vector3[queryListOfSpatialDots.Count];
        for (int i = 0; i < queryListOfSpatialDots.Count; i++)
        {
            array[i] = queryListOfSpatialDots[i].transform.position;
            //position += new Vector3(0, 0, 5);
        }
        return array;
    }


    public override void SetTransparency(float a)
    {
        Color c = queryPrism.GetComponent<Renderer>().material.color;
        c.a = a;
        //queryPrism.GetComponent<Renderer>().material.color = c;
        queryPrism.EnsureComponent<MaterialInstance>().Material.color = c;
        // https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/rendering/material-instance?view=mrtkunity-2022-05

    }

    public override void DisableColliders()
    {
        queryPrism.GetComponent<MeshCollider>().enabled = false;

        querySpatialPolygon.GetComponent<MeshCollider>().enabled = false;

        queryWallProjs[0].DisableColliders(); 
        queryWallProjs[1].DisableColliders();
        queryWallProjs[2].DisableColliders();
        queryWallProjs[3].DisableColliders();

    }

    public override void EnableColliders()
    {
        queryPrism.GetComponent<MeshCollider>().enabled = true;

        querySpatialPolygon.GetComponent<MeshCollider>().enabled = true;
        queryWallProjs[0].EnableColliders();
        queryWallProjs[1].EnableColliders();
        queryWallProjs[2].EnableColliders();
        queryWallProjs[3].EnableColliders();
    }


    public void SetQueryModeToPickupsOrDropoffs()
    {
        //qm.EditCountsAfterQueryChange(type, QueryType.Either);
        type = QueryType.Either;
        RefreshMaterial();


        // ONLY FOR NOW:
        //ViewBrusherAndLinker vbl = queryPrism.gameObject.GetComponent<ViewBrusherAndLinker>();
        queryIATKDataVertexIntersectionComputer.brushedViews = new List<View>();
        queryIATKDataVertexIntersectionComputer.brushedViews.Add(GameObject.Find("STC-Pickups").GetComponent<View>());
        queryIATKDataVertexIntersectionComputer.brushedViews.Add(GameObject.Find("STC-Dropoffs").GetComponent<View>());
    }

    public void SetQueryModeToOnlyPickups()
    {
        //qm.EditCountsAfterQueryChange(type, QueryType.Origin);
        type = QueryType.Origin;
        RefreshMaterial();

        // ONLY FOR NOW:
        queryIATKDataVertexIntersectionComputer.brushedViews = new List<View>();
        queryIATKDataVertexIntersectionComputer.brushedViews.Add(GameObject.Find("STC-Pickups").GetComponent<View>());
    }

    public void SetQueryModeToOnlyDropoffs()
    {
        //qm.EditCountsAfterQueryChange(type, QueryType.Destination);
        type = QueryType.Destination;
        RefreshMaterial();

        // ONLY FOR NOW:
        queryIATKDataVertexIntersectionComputer.brushedViews = new List<View>();
        queryIATKDataVertexIntersectionComputer.brushedViews.Add(GameObject.Find("STC-Dropoffs").GetComponent<View>());
    }

    public override void RemoveQuery()
    {
        qm.RemoveAtomicQuery(this);

        qm.availableColors.Enqueue(queryColor);

        Destroy(this.gameObject);
    }


    void RefreshMaterial()
    {
        //float currentPrismAlpha = queryPrism.GetComponent<Renderer>().material.color.a;

        switch (type)
        {
            case QueryType.Origin:
                queryPrism.GetComponent<MeshRenderer>().material = qm.pickupQueryMaterial;
                querySpatialPolygon.GetComponent<MeshRenderer>().material = qm.pickupQueryProjectionMaterial;
                queryWallProjs[0].SetProjectionMaterial(qm.pickupQueryProjectionMaterial);
                queryWallProjs[1].SetProjectionMaterial(qm.pickupQueryProjectionMaterial);
                queryWallProjs[2].SetProjectionMaterial(qm.pickupQueryProjectionMaterial);
                queryWallProjs[3].SetProjectionMaterial(qm.pickupQueryProjectionMaterial);
                break;
            case QueryType.Destination:
                queryPrism.GetComponent<MeshRenderer>().material = qm.dropoffQueryMaterial;
                querySpatialPolygon.GetComponent<MeshRenderer>().material = qm.dropoffQueryProjectionMaterial;
                queryWallProjs[0].SetProjectionMaterial(qm.dropoffQueryProjectionMaterial);
                queryWallProjs[1].SetProjectionMaterial(qm.dropoffQueryProjectionMaterial);
                queryWallProjs[2].SetProjectionMaterial(qm.dropoffQueryProjectionMaterial);
                queryWallProjs[3].SetProjectionMaterial(qm.dropoffQueryProjectionMaterial);
                break;
            case QueryType.Either:
                queryPrism.GetComponent<MeshRenderer>().material = qm.doubleQueryMaterial;
                querySpatialPolygon.GetComponent<MeshRenderer>().material = qm.doubleQueryProjectionMaterial;
                queryWallProjs[0].SetProjectionMaterial(qm.doubleQueryProjectionMaterial);
                queryWallProjs[1].SetProjectionMaterial(qm.doubleQueryProjectionMaterial);
                queryWallProjs[2].SetProjectionMaterial(qm.doubleQueryProjectionMaterial);
                queryWallProjs[3].SetProjectionMaterial(qm.doubleQueryProjectionMaterial);
                break;
        }

        prismChanged = true;
    }

    public override void RefreshColor()
    {
        querySpatialLine.GetComponent<LineRenderer>().startColor = queryColor;
        querySpatialLine.GetComponent<LineRenderer>().endColor = queryColor;

        foreach(GameObject dot in queryListOfSpatialDots)
        {
            dot.GetComponent<Renderer>().material.color = queryColor;
        }

        foreach (QueryWallProjection proj in queryWallProjs)
        { 
            proj.SetColor(queryColor);
        }

    }

    public override Vector3 GetCentralPosition2D()
    {
        return querySpatialPolygon.transform.position;
    }

    public override Vector3 GetCentralPosition3D()
    {
        return queryPrism.transform.position;
    }

    public override bool CheckChanges()
    {
        return prismChanged;
    }

    //private float timeLastRecomputeSignal = 0;

    public override bool CheckChangesRecomputeAndResetFlag()
    {
        if (CheckChanges() && queryIATKDataVertexIntersectionComputer.hasBrushed)
        {
            //if(Time.time - timeLastRecomputeSignal > qm.MinInactiveIntervalBeforeUpdating)
            //{
            //    timeLastRecomputeSignal = Time.time;
                RecomputeQueryResults();
                prismChanged = false;
                return true;
            //}
            //else
            //{
            //    return false;
            //}
        }
        else
        {
            //timeLastRecomputeSignal = Time.time;
            return false; 
        }
    }

    public override bool CheckChangesAndResetFlag()
    {
        if (CheckChanges() && queryIATKDataVertexIntersectionComputer.hasBrushed)
        {
            prismChanged = false;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void HideMapProjections()
    {
        querySpatialPolygon.gameObject.SetActive(false);
        querySpatialLine.gameObject.SetActive(false);
        foreach(GameObject dot in queryListOfSpatialDots)
        {
            dot.SetActive(false);
        }
    }

    public void RevealMapProjections()
    {
        querySpatialPolygon.gameObject.SetActive(true);
        querySpatialLine.gameObject.SetActive(true);
        foreach (GameObject dot in queryListOfSpatialDots)
        {
            dot.SetActive(true);
        }
    }

    public Query Duplicate()
    {
        AtomicQuery newQuery = (AtomicQuery)this.Clone();
        if (qm.availableColors.Count >= 1)
            newQuery.queryColor = qm.availableColors.Dequeue();
        else
            newQuery.queryColor = Color.black;
        newQuery.RefreshColor();
        newQuery.queryPrism.transform.position += new Vector3(0.2f, 0.2f, 0.2f);

        qm.AddAtomicQuery(newQuery);

        return newQuery;
    }

    public Query Clone()
    {
        GameObject newGameObject = new GameObject();
        newGameObject.transform.parent = qm.transform;
        newGameObject.name = "Query";

        AtomicQuery q = newGameObject.AddComponent<AtomicQuery>();
        q.qm = qm;
        q.type = type;
        q.queryColor = queryColor; // to be changed later if necessary

        q.queryPrism = Instantiate(queryPrism);
        q.queryPrism.gameObject.transform.parent = newGameObject.transform;

        q.querySpatialPolygon = Instantiate(querySpatialPolygon);
        q.querySpatialPolygon.transform.parent = newGameObject.transform;

        GameObject dotsAndLineDad = new GameObject();
        dotsAndLineDad.transform.parent = newGameObject.transform;
        dotsAndLineDad.transform.position = q.querySpatialPolygon.transform.position;
        dotsAndLineDad.name = "Query Spatial Vertices Dots and Line";
        q.queryLineAndDotsAnchor = dotsAndLineDad;

        q.querySpatialLine = Instantiate(querySpatialLine);
        q.querySpatialLine.transform.parent = dotsAndLineDad.transform;
        Destroy(q.querySpatialLine.GetComponent<LineController>());
        q.queryListOfSpatialDots = new List<GameObject>();

        QueryMapProjection qmp = q.querySpatialPolygon.gameObject.GetComponent<QueryMapProjection>();
        qmp.myQuery = q;
        qmp.projectionVisual = q.querySpatialPolygon.gameObject;
        qmp.edgeAndDotsAnchor = dotsAndLineDad.gameObject;

        foreach (GameObject dot in queryListOfSpatialDots)
        {
            GameObject newDot = Instantiate(dot);
            Destroy(newDot.GetComponent<DotController>());
            newDot.transform.position = dot.transform.position;
            newDot.transform.parent = dotsAndLineDad.transform;
            q.queryListOfSpatialDots.Add(newDot);
        }

        LineRenderer lr = q.querySpatialLine.GetComponent<LineRenderer>();
        lr.positionCount = queryListOfSpatialDots.Count;
        int i = 0;
        foreach (GameObject dot in queryListOfSpatialDots)
        {
            lr.SetPosition(i++, lr.transform.InverseTransformPoint(dot.transform.position));
        }

        q.minY = minY;
        q.maxY = maxY;
        q.queryHeight = queryHeight;

        q.GenerateVerticesLatLonList();

        // WALL PROJS

        GameObject projsDad = new GameObject("Query Wall Projections");
        projsDad.gameObject.transform.parent = newGameObject.transform;
        List<QueryWallProjection> wallProjs = new List<QueryWallProjection>();
        foreach(QueryWallProjection qwp in queryWallProjs)
        {
            QueryWallProjection newWP = Instantiate(qwp);
            newWP.gameObject.transform.parent = projsDad.transform;
            newWP.myQuery = q;
            newWP.RevealWidgetsAndLabels();
            wallProjs.Add(newWP);
        }
        q.queryWallProjsAnchor = projsDad;
        q.queryWallProjs = wallProjs;

        // QUERY BUTTONS

        AtomicQueryButtonsController queryButtons = Instantiate(myButtons, q.queryPrism.transform.position - new Vector3(0, 0, 0.1f), Quaternion.identity, transform).GetComponent<AtomicQueryButtonsController>();
        queryButtons.name = "Query Buttons";
        q.myButtons = queryButtons.gameObject;
        queryButtons.myQuery = q;
        queryButtons.transform.parent = newGameObject.transform;

        // QUERY TOOLTIP

        q.queryStatsTooltip = GameObject.Instantiate(queryStatsTooltip).GetComponent<ToolTip>();
        q.queryStatsTooltip.transform.parent = newGameObject.transform;

        // QUERY IATK DATA VERTEX INTERSECTION COMPUTER / VIEW FILTER

        Destroy(q.queryPrism.GetComponent<IATKViewFilter>());
        IATKViewFilter vf = q.queryPrism.gameObject.AddComponent<IATKViewFilter>();
        q.queryIATKDataVertexIntersectionComputer = vf;
        vf.myQuery = q;
        vf.BRUSH_MODE = IATKViewFilter.BrushMode.SELECT;
        vf.BRUSH_SHAPE = IATKViewFilter.BrushShape.PRISM;
        vf.BRUSH_TYPE = IATKViewFilter.BrushType.FREE;
        vf.refTransform = q.queryPrism.transform;
        vf.isActive = true;
        vf.computeShader = Instantiate(queryIATKDataVertexIntersectionComputer.computeShader);
        vf.myRenderMaterial = Instantiate(queryIATKDataVertexIntersectionComputer.myRenderMaterial);
        vf.brushedViews = new List<View>();
        vf.brushedViews.Add(GameObject.Find("STC-Pickups").GetComponent<View>());
        vf.brushedViews.Add(GameObject.Find("STC-Dropoffs").GetComponent<View>());
        vf.brushedLinkingViews = new List<LinkingViews>();
        vf.brushedLinkingViews.Add(GameObject.Find("TaxiSTCManager").GetComponent<LinkingViews>());

        return q; 
    }


}
