using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using static Query;
using IATK;
using Microsoft.Geospatial;
using System.IO;
using System;
using Microsoft.Maps.Unity;
using UnityEditor;
using System.Collections;

/*
 * The implementation of the line drawing feature was inspired by BLANKdev's Pen Tool System, including some heavily-adapted code in this class
 * Source: https://theblankdev.itch.io/linerenderseries, https://www.youtube.com/watch?v=pcLn2ze9JQA
 */

public class QueryCreator : MonoBehaviour
{
    [Header("Scenario manager")]
    [SerializeField] public ScenarioManager sm;

    [Header("Pen Canvas")]
    [SerializeField] private QueryCreationCanvas penCanvas;

    [Header("Query Manager")]
    [SerializeField] private QueryManager queryManager;

    [Header("Prefabs")]
    public GameObject dotPrefab;

    [SerializeField] private GameObject linePrefab;
    public QueryWallProjection wallProjPrefab;
    public AtomicQueryButtonsController queryButtonsPrefab;
    public ToolTip tooltipPrefab;
    public Material neighborhoodMaterial;
    public TextMesh neighborhoodLabelPrefab;

    private LineController currentLine;


    [Header("Settings")]

    public QueryType queryType = QueryType.Either; // the default type is either (green)
    public bool previewNextPoint = true;
    public bool previewPrism = true;
    public bool previewPolygon = true;
    public bool previewWallProj = true;
    public bool useProbuilder = true;
    public bool useClippingBox = false;
    public int maxVertices = 100;
    public float thresholdToClosePolygon = 0.05f;
    public Color lineColor = Color.blue;

    public ComputeShader cs;
    public Material mrm;


    [Header("Status")]

    public bool isDrawingLine = false;
    public bool isSelectingNeighborhoods = false;
    public bool isPenModeActive = false;
    public bool isNeighborhoodModeActive = false;

    



    private void Start() {
        penCanvas.OnPenCanvasClickEvent += AddDot;

        LoadNeighborhoodsFile();
    }

    public void Update()
    {
        if(!queryManager.InLinkingMode)
        {
            queryType = QueryType.Either; // the default new query type is either (green)
        }
        else // but, exceptionally, if we are creating a new query while in linking mode (creating a new directional constraint), we can change to the adequate type
        {
            if (queryManager.linkingModeFirstQuery.type == QueryType.Destination)
                queryType = QueryType.Origin;
            else
                queryType = QueryType.Destination;
        }


        if (isDrawingLine && currentLine.Count() > 2)
        {
            float distToOrigin = Vector3.Distance(currentLine.GetOrigin(), penCanvas.cursorPosition);
            if (distToOrigin <= thresholdToClosePolygon || currentLine.Count() == maxVertices)
            {
                currentLine.KillDotPreviewIfThereIsOne(); 
                currentLine.SetToLoop();
            }
            else
            {
                currentLine.SetNotToLoop();
            }
        }
            
        //if (isDrawingLine && previewNextPoint && !currentLine.isLooped())
        if (previewNextPoint && (currentLine == null || !currentLine.isLooped()) && penCanvas.onFocus) 
        {
            PreviewDot();
        }
        else if(currentLine != null)
        {
            currentLine.KillDotPreviewIfThereIsOne();
        }

        if (isDrawingLine && previewPrism && currentLine.Count() > 2)
        {
            if (!PreviewPrism())
            {
                KillProjPreviews();
                return;
            }
        }

        if (isDrawingLine && previewPolygon && currentLine.Count() > 2)
        {
            PreviewPolygon();
        }

        if (isDrawingLine && previewWallProj && currentLine.Count() > 2)
        {
            PreviewWallProjs();
        }

    }



    public void SwitchPenModeStatus()
    {
        if (isPenModeActive)
            DisablePenMode();
        else
            EnablePenMode();
          
    }

    public void SwitchNeighborhoodModeStatus()
    {
        if (isNeighborhoodModeActive)
        {
            DisableNeighborhoodMode();
        }
        else
        {
            EnableNeighborhoodMode();
        }
    }

    public void EnablePenMode()
    {
        if(isNeighborhoodModeActive)
            DisableNeighborhoodMode();

        penCanvas.gameObject.GetComponent<MixedRealityMapInteractionHandler>().enabled = false;
        sm.stc.walls.gameObject.GetComponent<STCWallsInteractionHandler>().enabled = false;

        penCanvas.enabled = true;

        queryManager.SetQueriesToEditMode();

        isPenModeActive = true;
    }

    public void EnableNeighborhoodMode()
    {
        if (isPenModeActive)
            DisablePenMode();

        penCanvas.gameObject.GetComponent<MixedRealityMapInteractionHandler>().enabled = false;
        sm.stc.walls.gameObject.GetComponent<STCWallsInteractionHandler>().enabled = false;

        StopAllCoroutines();
        StartCoroutine(CreateNeighborhoodPolygonsForCurrentSelection());

        queryManager.SetQueriesToEditMode();

        isNeighborhoodModeActive = true;
    }

    public void DisableNeighborhoodMode()
    {
        penCanvas.gameObject.GetComponent<MixedRealityMapInteractionHandler>().enabled = true;
        sm.stc.walls.gameObject.GetComponent<STCWallsInteractionHandler>().enabled = true;
        //penCanvas.enabled = false;

        DestroyNeighborhoodPolygons();

        ClearPreviews();

        queryManager.SetQueriesToFadedMode();


        prismMesh = null;
        prismExtruder = null;
        polygonMesh = null;
        wallProjs = null;

        currentLine = null;
        isDrawingLine = false;

        isNeighborhoodModeActive = false;
    }

    public void DisablePenMode()
    {
        penCanvas.gameObject.GetComponent<MixedRealityMapInteractionHandler>().enabled = true;
        sm.stc.walls.gameObject.GetComponent<STCWallsInteractionHandler>().enabled = true;
        penCanvas.enabled = false;

        if (prismExtruder != null)
        {
            Destroy(prismExtruder.gameObject);
        }
        if (prismMesh != null)
        {
            Destroy(prismMesh.gameObject);
        }
        if (polygonMesh != null)
        {
            Destroy(polygonMesh.gameObject);
        }
        if (wallProjs != null)
        {
            foreach(QueryWallProjection p in wallProjs)
                Destroy(p.gameObject);
        }
        if (currentLine != null)
        {
            currentLine.DestroyDots();
            Destroy(currentLine.gameObject);
        }

        queryManager.SetQueriesToFadedMode();


        prismMesh = null;
        prismExtruder = null;
        polygonMesh = null;
        wallProjs = null;

        currentLine = null;
        isDrawingLine = false;

        isPenModeActive = false;
    }

    private void KillProjPreviews()
    {
        if (polygonMesh != null)
        {
            Destroy(polygonMesh.gameObject);
        }
        if (wallProjs != null)
        {
            foreach (QueryWallProjection p in wallProjs)
                Destroy(p.gameObject);
        }
        polygonMesh = null;
        wallProjs = null;
    }



    private void AddDot() {

        if (currentLine == null)
        { // means the user adding a new dot to a new line

            LineController lineController = Instantiate(linePrefab, transform).GetComponent<LineController>();
            SetCurrentLine(lineController);

            if (lineColor == Color.blue) //default color
            {
                if (queryManager.availableColors.Count >= 1)
                    lineColor = queryManager.availableColors.Dequeue();
                else
                    lineColor = Color.black;
            }

            lineController.SetColor(lineColor);
            isDrawingLine = true;

            DotController dot = Instantiate(dotPrefab, penCanvas.lastClickPosition + new Vector3(0, 0.0055f, 0), Quaternion.identity, transform).GetComponent<DotController>();
            dot.GetComponent<Renderer>().material.color = lineColor;
            currentLine.AddDot(dot);
        }
        else if (currentLine.isLooped() && currentLine.Count() > 2) // means the user is closing the polygon 
        {
            EndCurrentLine();
        }
        else // means the user adding a new dot to an existing line 
        {
            if (currentLine.Count() > 2 && previewPrism && prismMesh == null) // means generating the last prism preview failed because the position is invalid (line crossings)
            {
                AudioSource.PlayClipAtPoint(sm.badSoundClip, penCanvas.lastClickPosition);

                return;
            }

            DotController dot = Instantiate(dotPrefab, penCanvas.lastClickPosition + new Vector3(0, 0.005f, 0), Quaternion.identity, transform).GetComponent<DotController>();
            dot.GetComponent<Renderer>().material.color = lineColor;
            currentLine.AddDot(dot);

            AudioSource.PlayClipAtPoint(sm.miniGoodSoundClip, penCanvas.lastClickPosition);

        }
    }


    private void PreviewDot()
    {
        if (currentLine == null)
        { // means the user adding a new dot to a new line
            LineController lineController = Instantiate(linePrefab, transform).GetComponent<LineController>();
            SetCurrentLine(lineController);

            if (lineColor == Color.blue) //default color
            {
                if (queryManager.availableColors.Count >= 1)
                    lineColor = queryManager.availableColors.Dequeue();
                else
                    lineColor = Color.black;
            }

            lineController.SetColor(lineColor);
            isDrawingLine = true;

            DotController dot = Instantiate(dotPrefab, penCanvas.cursorPosition + new Vector3(0, 0.005f, 0), Quaternion.identity, transform).GetComponent<DotController>();
            dot.GetComponent<Renderer>().material.color = lineColor;
            currentLine.PreviewDot(dot);
        }
        else
        {
            DotController dot = Instantiate(dotPrefab, penCanvas.cursorPosition + new Vector3(0, 0.005f, 0), Quaternion.identity, transform).GetComponent<DotController>();
            dot.GetComponent<Renderer>().material.color = lineColor;
            currentLine.PreviewDot(dot);
        }
    }

   

    private void EndCurrentLine() {
        if (currentLine != null) {
          

            AudioSource.PlayClipAtPoint(sm.goodSoundClip, penCanvas.lastClickPosition);


            if (!prismMesh && !prismExtruder) // if we still don't have a prism, create it 
                PreviewPrism();

            if (!polygonMesh) // if we still don't have a prism, create it 
                PreviewPolygon();

            // final settings for our prism before saying goodbye

            GameObject newGameObject = new GameObject();
            newGameObject.transform.parent = queryManager.transform;
            newGameObject.name = "Query";

            AtomicQuery q = newGameObject.AddComponent<AtomicQuery>();
            q.qm = queryManager;
            q.type = queryType;
            q.queryColor = lineColor;
            q.querySpatialLine = currentLine.gameObject;
            q.queryPrism = prismMesh.gameObject;
            q.querySpatialPolygon = polygonMesh.gameObject;
            q.queryListOfSpatialDots = currentLine.GetDotsObjects();

            q.SetQueryTimeSpanFromHeights(sm.stc.baseHeight, sm.stc.baseHeight + sm.stc.transform.localScale.y);
           


            if (useProbuilder) { 
                prismMesh.gameObject.AddComponent<MeshCollider>();

                Microsoft.MixedReality.Toolkit.UI.ObjectManipulator om = prismMesh.gameObject.AddComponent<Microsoft.MixedReality.Toolkit.UI.ObjectManipulator>();
                //Microsoft.MixedReality.Toolkit.UI.ObjectManipulator om = newGameObject.AddComponent<Microsoft.MixedReality.Toolkit.UI.ObjectManipulator>();
                om.SmoothingFar = false;
                om.SmoothingNear = false;
                RotationAxisConstraint rc = prismMesh.gameObject.AddComponent<RotationAxisConstraint>();
                //RotationAxisConstraint rc = newGameObject.AddComponent<RotationAxisConstraint>();
                rc.ConstraintOnRotation = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.XAxis | Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.YAxis | Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.ZAxis;

                //Microsoft.MixedReality.Toolkit.Input.NearInteractionGrabbable nig = prismMesh.gameObject.AddComponent<Microsoft.MixedReality.Toolkit.Input.NearInteractionGrabbable>();


                Microsoft.MixedReality.Toolkit.UI.Interactable interac = prismMesh.gameObject.AddComponent<Interactable>();
                //interac.OnClick.AddListener(delegate { queryManager.HandleQueryPrismClick(interac.transform.parent.GetComponent<AtomicQuery>()); });
                interac.OnClick.AddListener(delegate { queryManager.HandleQueryPrismClick(q); });

                //om.OnHoverEntered.AddListener(delegate { queryManager.HandleQueryPrismHover(interac.transform.parent.GetComponent<AtomicQuery>()); });
                om.OnHoverEntered.AddListener(delegate { queryManager.HandleQueryPrismHover(q); });



                if (useClippingBox)
                { 
                    GameObject.Find("ClippingBox").GetComponent<ClippingBox>().AddRenderer(prismMesh.gameObject.GetComponent<Renderer>());
                }

                prismMesh.gameObject.name = "Query Prism Mesh";
                prismMesh.gameObject.transform.parent = newGameObject.transform;
            }
            else
            {
                //prismExtruder.gameObject.AddComponent<MeshCollider>();

                Microsoft.MixedReality.Toolkit.UI.ObjectManipulator om = prismExtruder.gameObject.AddComponent<Microsoft.MixedReality.Toolkit.UI.ObjectManipulator>();
                om.SmoothingFar = false;
                om.SmoothingNear = false;

                Microsoft.MixedReality.Toolkit.UI.Interactable interac = prismExtruder.gameObject.AddComponent<Interactable>();
                interac.OnClick.AddListener(delegate { queryManager.HandleQueryPrismClick(interac.transform.parent.GetComponent<AtomicQuery>()); });

                RotationAxisConstraint rc = prismExtruder.gameObject.AddComponent<RotationAxisConstraint>();
                rc.ConstraintOnRotation = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.XAxis | Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.YAxis | Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.ZAxis;

                prismExtruder.gameObject.name = "Query Prism Mesh";
                prismExtruder.gameObject.transform.parent = newGameObject.transform;
            }



            GameObject dotsAndLineDad = new GameObject();
            dotsAndLineDad.transform.parent = newGameObject.transform;
            if(polygonMesh)
               dotsAndLineDad.transform.position = polygonMesh.transform.position;
            dotsAndLineDad.name = "Query Spatial Vertices Dots and Line";

            currentLine.DonateDots(dotsAndLineDad.transform);

            q.queryLineAndDotsAnchor = dotsAndLineDad;

            currentLine.gameObject.transform.parent = dotsAndLineDad.transform;

            currentLine.gameObject.name = "Query Spatial Border Line";





            if (previewPolygon)
            { 
                polygonMesh.gameObject.name = "Query Polygon Mesh";
                polygonMesh.gameObject.transform.parent = newGameObject.transform;

                
                polygonMesh.gameObject.AddComponent<MeshCollider>();

                QueryMapProjection qmp = polygonMesh.gameObject.AddComponent<QueryMapProjection>();
                qmp.myQuery = q;
                qmp.projectionVisual = polygonMesh.gameObject;
                qmp.edgeAndDotsAnchor = dotsAndLineDad.gameObject;

                Microsoft.MixedReality.Toolkit.UI.ObjectManipulator om2 = polygonMesh.gameObject.AddComponent<Microsoft.MixedReality.Toolkit.UI.ObjectManipulator>();
                om2.SmoothingFar = false;
                om2.SmoothingNear = false;
                RotationAxisConstraint rc2 = polygonMesh.gameObject.AddComponent<RotationAxisConstraint>();
                rc2.ConstraintOnRotation = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.XAxis | Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.YAxis | Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.ZAxis;
                MoveAxisConstraint mc2 = polygonMesh.gameObject.AddComponent<MoveAxisConstraint>();
                mc2.ConstraintOnMovement = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.YAxis;


            }


            q.GenerateVerticesLatLonList();


            // WALL PROJS

            if (wallProjs == null)
                PreviewWallProjs();

            GameObject projsDad = new GameObject();
            projsDad.gameObject.name = "Query Wall Projections";
            projsDad.gameObject.transform.parent = newGameObject.transform;
            wallProjB.gameObject.transform.parent = projsDad.transform;
            wallProjB.myQuery = q;
            wallProjB.RevealWidgetsAndLabels();
            wallProjL.gameObject.transform.parent = projsDad.transform;
            wallProjL.myQuery = q;
            wallProjL.RevealWidgetsAndLabels();
            wallProjR.gameObject.transform.parent = projsDad.transform;
            wallProjR.myQuery = q;
            wallProjR.RevealWidgetsAndLabels();
            wallProjF.gameObject.transform.parent = projsDad.transform;
            wallProjF.myQuery = q;
            wallProjF.RevealWidgetsAndLabels();
            q.queryWallProjsAnchor = projsDad;
            q.queryWallProjs = wallProjs;



            // QUERY BUTTONS

            AtomicQueryButtonsController queryButtons = Instantiate(queryButtonsPrefab, prismMesh.transform.position - new Vector3(0,0,0.1f), Quaternion.identity, transform).GetComponent<AtomicQueryButtonsController>();
            queryButtons.name = "Query Buttons";
            q.myButtons = queryButtons.gameObject;
            queryButtons.myQuery = q;
            queryButtons.transform.parent = newGameObject.transform;


            // QUERY TOOLTIP

            q.queryStatsTooltip = GameObject.Instantiate(tooltipPrefab).GetComponent<ToolTip>();
            q.queryStatsTooltip.transform.parent = newGameObject.transform;
            q.queryStatsTooltip.GetComponent<MixedRealityLineRenderer>().LineMaterial.color = q.queryColor;
            //q.queryStatsTooltip.GetComponent<ToolTipBackgroundMesh>().BackgroundRenderer.material.color = q.queryColor;



            // QUERY IATK DATA VERTEX INTERSECTION COMPUTER / VIEW FILTER

            IATKViewFilter vf = prismMesh.gameObject.AddComponent<IATKViewFilter>();
            q.queryIATKDataVertexIntersectionComputer = vf;
            vf.myQuery = q;
            vf.BRUSH_MODE = IATKViewFilter.BrushMode.SELECT;
            vf.BRUSH_SHAPE = IATKViewFilter.BrushShape.PRISM;
            vf.BRUSH_TYPE = IATKViewFilter.BrushType.FREE;
            vf.refTransform = prismMesh.transform;
            vf.isActive = true;
            vf.computeShader = cs;
            vf.myRenderMaterial = mrm;
            vf.brushedViews = new List<View>();

                vf.brushedViews.Add(GameObject.Find("STC-Pickups").GetComponent<View>());
                vf.brushedViews.Add(GameObject.Find("STC-Dropoffs").GetComponent<View>());
                vf.brushedLinkingViews = new List<LinkingViews>();
                vf.brushedLinkingViews.Add(GameObject.Find("TaxiSTCManager").GetComponent<LinkingViews>());
            

            queryManager.AddAtomicQuery(q);



            // if, exceptionally, we created this new query while in linking mode (creating a new directional query), 
            // we probably wanted it to be the origin/destination of said directional query. let's check. 
            if (queryManager.InLinkingMode)
                queryManager.TryToLinkQueries(q);

            if (queryManager.InMergingMode)
                queryManager.TryToMergeQueries(q);


            prismMesh = null;
            prismExtruder = null;
            polygonMesh = null;
            wallProjs = null;
            newGameObject = null;

            currentLine = null;
            isDrawingLine = false;

            if (queryManager.availableColors.Count >= 1)
                lineColor = queryManager.availableColors.Dequeue();
            else
                lineColor = Color.black;
        }
    }

    private void SetCurrentLine(LineController newLine) {
        EndCurrentLine();

        currentLine = newLine;
        //currentLine.SetColor(activeColor);

        //loopToggle.enabled = true;
        //loopToggle.sprite = (currentLine.isLooped()) ? unloopSprite : loopSprite;
    }


    private bool PreviewPrism()
    {
        if (useProbuilder)
            return PreviewPrismWithProbuilder();
        else
            return PreviewPrismWithExtruder();
    }



    private PolyExtruder prismExtruder = null;
   
    private bool PreviewPrismWithExtruder()
    {
        if (prismExtruder != null)
        {
            Destroy(prismExtruder.gameObject);
        }
        GameObject polyExtruderGO = new GameObject();
        polyExtruderGO.transform.parent = this.transform;
        prismExtruder = polyExtruderGO.AddComponent<PolyExtruder>();
        //polyExtruder.isOutlineRendered = false;
        //prismExtruder.transform.position = currentLine.GetAveragePosition();
        prismExtruder.createPrism(polyExtruderGO.name, sm.stc.transform.localScale.y, sm.stc.baseHeight, currentLine.GetPointsAsVectorsXY(), Color.blue, true, true, true);

        return true;
    }

    
    private ProBuilderMesh prismMesh = null;

    // Adapted from https://github.com/Unity-Technologies/ProBuilder-API-Examples/blob/master/Runtime/Misc/CreatePolyShape.cs
    private bool PreviewPrismWithProbuilder()
    {
        if (prismMesh != null)
        {
            Destroy(prismMesh.gameObject);
        }

        var go = new GameObject();
        go.transform.parent = this.transform;

        prismMesh = go.gameObject.AddComponent<ProBuilderMesh>();

        ActionResult result = prismMesh.CreateShapeFromPolygon(currentLine.GetPointsAsVectorsXYZ(), 1f, false);

        if(result.status == ActionResult.Status.Success)
        { 
            //prismMesh.SetPivot(currentLine.GetAveragePosition());

            prismMesh.CenterPivot(prismMesh.selectedFaceIndexes.ToArray());

            prismMesh.transform.localPosition -= new Vector3(0, currentLine.GetOrigin().y - sm.stc.baseHeight, 0);

            prismMesh.transform.localScale = new Vector3(prismMesh.transform.localScale.x, sm.stc.transform.localScale.y, prismMesh.transform.localScale.z);

            switch (queryType)
            {
                case QueryType.Origin: go.GetComponent<MeshRenderer>().material = queryManager.pickupQueryMaterial; break;
                case QueryType.Destination: go.GetComponent<MeshRenderer>().material = queryManager.dropoffQueryMaterial; break;
                case QueryType.Either: go.GetComponent<MeshRenderer>().material = queryManager.doubleQueryMaterial; break;
            }
            Color c = go.GetComponent<MeshRenderer>().material.color;
            c.a = queryManager.editModeQueryTransparency;
            go.GetComponent<MeshRenderer>().material.color = c;

            return true;
        }
        else
        {
            Destroy(go);
            prismMesh = null;
            return false;
        }
    }


    private ProBuilderMesh polygonMesh = null;

    // Adapted from https://github.com/Unity-Technologies/ProBuilder-API-Examples/blob/master/Runtime/Misc/CreatePolyShape.cs
    private void PreviewPolygon()
    {
        if (polygonMesh != null)
        {
            Destroy(polygonMesh.gameObject);
        }

        var go = new GameObject();
        go.transform.parent = this.transform;

        polygonMesh = go.gameObject.AddComponent<ProBuilderMesh>();

        polygonMesh.CreateShapeFromPolygon(currentLine.GetPointsAsVectorsXYZ(), 0.002f, false);

        polygonMesh.CenterPivot(polygonMesh.selectedFaceIndexes.ToArray());


        //polygonMesh.CreatePolygon(, false);


        //prismMesh.transform.localPosition -= new Vector3(0, currentLine.GetOrigin().y - sm.stc.baseHeight, 0);

        switch (queryType)
        {
            //case QueryType.Origin: go.GetComponent<MeshRenderer>().material = queryManager.pickupQueryProjectionMaterial; break;
            //case QueryType.Destination: go.GetComponent<MeshRenderer>().material = queryManager.dropoffQueryProjectionMaterial; break;
            //case QueryType.Either: go.GetComponent<MeshRenderer>().material = queryManager.doubleQueryProjectionMaterial; break;
            case QueryType.Origin: go.GetComponent<MeshRenderer>().material = queryManager.pickupQueryMaterial; break;
            case QueryType.Destination: go.GetComponent<MeshRenderer>().material = queryManager.dropoffQueryMaterial; break;
            case QueryType.Either: go.GetComponent<MeshRenderer>().material = queryManager.doubleQueryMaterial; break;
        }
        Color c = go.GetComponent<MeshRenderer>().material.color;
        //c.a = queryManager.editModeQueryTransparency;
        c.a = queryManager.useModeQueryTransparency;
        go.GetComponent<MeshRenderer>().material.color = c;

    }


    private List<QueryWallProjection> wallProjs = null;
    private QueryWallProjection wallProjB, wallProjL, wallProjR, wallProjF;

    private void PreviewWallProjs()
    {
        if (wallProjs == null)
        {
            wallProjs = new List<QueryWallProjection>();
            wallProjB = GameObject.Instantiate(wallProjPrefab);
            wallProjB.transform.parent = this.transform;
            wallProjs.Add(wallProjB);

            wallProjL = GameObject.Instantiate(wallProjPrefab);
            wallProjL.Rotate(new Vector3(0, -90, 0));
            wallProjL.transform.parent = this.transform;
            wallProjs.Add(wallProjL);

            wallProjR = GameObject.Instantiate(wallProjPrefab);
            wallProjR.Rotate(new Vector3(0, 90, 0));
            wallProjR.transform.parent = this.transform;
            wallProjs.Add(wallProjR);

            wallProjF = GameObject.Instantiate(wallProjPrefab);
            wallProjF.transform.parent = this.transform;
            wallProjF.gameObject.SetActive(false);
            wallProjs.Add(wallProjF);

            foreach (QueryWallProjection proj in wallProjs)
            {
                switch (queryType)
                {
                    case QueryType.Origin: proj.SetProjectionMaterial(queryManager.pickupQueryProjectionMaterial); break;
                    case QueryType.Destination: proj.SetProjectionMaterial(queryManager.dropoffQueryProjectionMaterial); break;
                    case QueryType.Either: proj.SetProjectionMaterial(queryManager.doubleQueryProjectionMaterial); break;
                }
                //Color c = proj.GetComponent<MeshRenderer>().material.color;
                //c.a = queryManager.editModeQueryTransparency;
                //proj.GetComponent<MeshRenderer>().material.color = c;
                proj.SetColor(lineColor);
            }
        }

        float minx = 1000, maxx = -1000, minz = 1000, maxz = -1000;
        Vector3[] positions = currentLine.GetPointsAsVectorsXYZ();
        for(int i = 0; i < positions.Length; i++)
        {
            if (positions[i].x < minx)
                minx = positions[i].x;
            if (positions[i].x > maxx)
                maxx = positions[i].x;
            if (positions[i].z < minz)
                minz = positions[i].z;
            if (positions[i].z > maxz)
                maxz = positions[i].z;
        }

        // BACK
        //wallProjB.transform.localScale = new Vector3(maxx - minx, prismMesh.transform.localScale.y, wallProjB.transform.localScale.z);
        //wallProjB.transform.position = new Vector3((maxx + minx) / 2f, prismMesh.transform.position.y, sm.bingMap.transform.position.z + sm.stc.transform.localScale.z / 2 - 0.001f);
        wallProjB.SetProjectionScale(new Vector3(maxx - minx, prismMesh.transform.localScale.y, wallProjB.GetProjectionScale().z));
        wallProjB.SetProjectionPosition(new Vector3((maxx + minx) / 2f, prismMesh.transform.position.y, sm.bingMap.transform.position.z + sm.stc.transform.localScale.z / 2 - 0.005f));


        // LEFT
        //wallProjL.transform.localScale = new Vector3(maxz - minz, prismMesh.transform.localScale.y, wallProjL.transform.localScale.z);
        //wallProjL.transform.position = new Vector3(sm.bingMap.transform.position.x - sm.stc.transform.localScale.x / 2 + 0.001f, prismMesh.transform.position.y, (maxz + minz) / 2f);
        wallProjL.SetProjectionScale(new Vector3(maxz - minz, prismMesh.transform.localScale.y, wallProjL.GetProjectionScale().z));
        wallProjL.SetProjectionPosition(new Vector3(sm.bingMap.transform.position.x - sm.stc.transform.localScale.x / 2 + 0.005f, prismMesh.transform.position.y, (maxz + minz) / 2f));


        // RIGHT
        //wallProjR.transform.localScale = new Vector3(maxz - minz, prismMesh.transform.localScale.y, wallProjR.transform.localScale.z);
        //wallProjR.transform.position = new Vector3(sm.bingMap.transform.position.x + sm.stc.transform.localScale.x / 2 - 0.001f, prismMesh.transform.position.y, (maxz + minz) / 2f);
        wallProjR.SetProjectionScale(new Vector3(maxz - minz, prismMesh.transform.localScale.y, wallProjR.GetProjectionScale().z));
        wallProjR.SetProjectionPosition(new Vector3(sm.bingMap.transform.position.x + sm.stc.transform.localScale.x / 2 - 0.005f, prismMesh.transform.position.y, (maxz + minz) / 2f));


        // FRONT
        if (sm.ClippedEgoRoom)
        {
            wallProjF.gameObject.SetActive(true);
            //wallProjF.transform.localScale = new Vector3(maxx - minx, prismMesh.transform.localScale.y, wallProjF.transform.localScale.z);
            //wallProjF.transform.position = new Vector3((maxx + minx) / 2f, prismMesh.transform.position.y, sm.bingMap.transform.position.z - sm.stc.transform.localScale.z / 2 + 0.001f);
            wallProjF.SetProjectionScale(new Vector3(maxx - minx, prismMesh.transform.localScale.y, wallProjF.GetProjectionScale().z));
            wallProjF.SetProjectionPosition(new Vector3((maxx + minx) / 2f, prismMesh.transform.position.y, sm.bingMap.transform.position.z - sm.stc.transform.localScale.z / 2 + 0.005f));

        }
        else
        {
            wallProjF.gameObject.SetActive(false);
        }


    }


    public Dictionary<string, List<LatLonAlt>> neighborhoods = new Dictionary<string, List<LatLonAlt>>();

    public void LoadNeighborhoodsFile()
    {
        ///TextAsset file = Resources.Load("neighborhoodsGeometryManhattan") as TextAsset;
        TextAsset file = Resources.Load("neighborhoodsGeometryFull") as TextAsset;

        IEnumerable<string> lines = file.text.Split('\n'); // File.ReadLines("neighborhoodsGeometry.txt");
        string name = "";
        List<LatLonAlt> list = new List<LatLonAlt>();
        foreach (string line in lines)
        {
            if (line[0] != '-')
            {
                if(name != "")
                {
                    neighborhoods.Add(name, list);
                }
                name = line.Split(';')[0];
                list = new List<LatLonAlt>();
            }
            else
            {
                list.Add(new LatLonAlt(new LatLon(Double.Parse(line.Split(' ')[1]), Double.Parse(line.Split(' ')[0])), 0));
            }
        }
        if (name != "") // LAST ENTRY
        {
            neighborhoods.Add(name, list);
        }
    }

    List<GameObject> neighborhoodObjects = new List<GameObject>();

    public void DestroyNeighborhoodPolygons()
    {
        foreach (GameObject go in neighborhoodObjects)
            Destroy(go);
        neighborhoodObjects = new List<GameObject>();
    }

    public IEnumerator CreateNeighborhoodPolygonsForCurrentSelection()
    {
        if(neighborhoodObjects.Count > 0)
        {
            DestroyNeighborhoodPolygons();
        }

        foreach (string neighborhoodName in neighborhoods.Keys)
        {
            Vector3[] pointsAsVectorsXYZ = GetNeighborhoodXYZPoints(neighborhoodName);
            //Vector2[] pointsAsVectorsXY = new Vector2[neighborhoods[name].Count];
            
            if (IsAtLeastOneNeighborhoodVerticeOutsideMapBounds(neighborhoodName))
                continue;


            // PROBUILDER VERSION

            var go = new GameObject();
            go.transform.parent = this.transform;
            go.name = neighborhoodName;
            ProBuilderMesh neighborhoodPolygonMesh = go.gameObject.AddComponent<ProBuilderMesh>();

            ActionResult a = neighborhoodPolygonMesh.CreateShapeFromPolygon(pointsAsVectorsXYZ, 0.001f, false);

            if(a.status == ActionResult.Status.Failure) // IF PROBUILDER HAS TROUBLE BUILDING A MESH, SOMETIMES INVERTING THE ORDER OF THE POINTS HELPS
            {
                //Debug.Log("TRY 1: " + neighborhoodName + " " + pointsAsVectorsXYZ.Length.ToString() + " " + a.status.ToString() + " " + a.notification);

                neighborhoods[neighborhoodName].Reverse();
                pointsAsVectorsXYZ = GetNeighborhoodXYZPoints(neighborhoodName);
                a = neighborhoodPolygonMesh.CreateShapeFromPolygon(pointsAsVectorsXYZ, 0.001f, false); // let's try again
            }


            if (a.status == ActionResult.Status.Failure)
                Debug.Log("TRY 2: " + neighborhoodName + " " + pointsAsVectorsXYZ.Length.ToString() + " " + a.status.ToString() + " " + a.notification);

            neighborhoodPolygonMesh.transform.position -= new Vector3(0, 0.001f, 0);
            neighborhoodPolygonMesh.CenterPivot(neighborhoodPolygonMesh.selectedFaceIndexes.ToArray());

            go.GetComponent<MeshRenderer>().material = neighborhoodMaterial; //queryManager.dropoffQueryMaterial;
            Color c = go.GetComponent<MeshRenderer>().material.color;
            //c.a = queryManager.editModeQueryTransparency;
            c.a = 0.4f;// queryManager.useModeQueryTransparency;
            go.GetComponent<MeshRenderer>().material.color = c;

            //LineRendererz lr = neighborhoodPolygonMesh.gameObject.AddComponent<LineRenderer>();
            LineRenderer lr = GameObject.Instantiate(linePrefab).GetComponent<LineRenderer>();
            lr.gameObject.transform.parent = neighborhoodPolygonMesh.gameObject.transform;

            neighborhoodObjects.Add(neighborhoodPolygonMesh.gameObject);


            TextMesh label = GameObject.Instantiate(neighborhoodLabelPrefab).GetComponent<TextMesh>();
            label.gameObject.transform.parent = neighborhoodPolygonMesh.gameObject.transform;
            label.gameObject.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            label.text = neighborhoodName;


            neighborhoodPolygonMesh.gameObject.AddComponent<MeshCollider>();
            Microsoft.MixedReality.Toolkit.UI.ObjectManipulator om = neighborhoodPolygonMesh.gameObject.AddComponent<Microsoft.MixedReality.Toolkit.UI.ObjectManipulator>();
            om.SmoothingFar = false;
            om.SmoothingNear = false;
            om.TwoHandedManipulationType = TransformFlags.Move;
            MoveAxisConstraint mc = neighborhoodPolygonMesh.gameObject.AddComponent<MoveAxisConstraint>();
            mc.ConstraintOnMovement = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.XAxis | Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.YAxis | Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.ZAxis;
            RotationAxisConstraint rc = neighborhoodPolygonMesh.gameObject.AddComponent<RotationAxisConstraint>();
            rc.ConstraintOnRotation = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.XAxis | Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.YAxis | Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.ZAxis;
            Microsoft.MixedReality.Toolkit.UI.Interactable interac = neighborhoodPolygonMesh.gameObject.AddComponent<Interactable>();
            interac.OnClick.AddListener(delegate { HandleNeighborhoodClick(neighborhoodName); });
            om.OnHoverEntered.AddListener(delegate { HandleNeighborhoodHover(neighborhoodName); });
            om.OnHoverExited.AddListener(delegate { HandleNeighborhoodHoverExited(); });




            // PRISM EXTRUDER VERSION

            ///GameObject polyExtruderGO = new GameObject();
            ///polyExtruderGO.name = name;
            ///polyExtruderGO.transform.parent = this.transform;
            ///prismExtruder = polyExtruderGO.AddComponent<PolyExtruder>();

            ///prismExtruder.isOutlineRendered = true;
            ///prismExtruder.outlineWidth = 0.001f;
            ///prismExtruder.outlineColor = Color.black;
            ///prismExtruder.createPrism(polyExtruderGO.name, 0.002f, sm.stc.baseHeight, pointsAsVectorsXY, Color.blue, true, true, true);
            ///
            ///LineRenderer lr = prismExtruder.gameObject.AddComponent<LineRenderer>();

            ///neighborhoodObjects.Add(prismExtruder.gameObject);





            // CONTOUR

            //lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            lr.useWorldSpace = true;
            //lr.alignment = LineAlignment.TransformZ;
            lr.startWidth = 0.005f;
            lr.endWidth = 0.005f;
            lr.startColor = Color.black;
            lr.endColor = Color.black;
            lr.positionCount = pointsAsVectorsXYZ.Length;
            for (int j = 0; j < lr.positionCount; j++)
                lr.SetPosition(j, pointsAsVectorsXYZ[j] + new Vector3(0, 0.001f, 0));


            yield return null;
        }
    }


    public void HandleNeighborhoodHover(string name)
    {
        PreviewNeighborhood(name);

        if (queryManager.InLinkingMode)
        {
            queryManager.newArrowPreview.SetPosition(1, polygonMesh.transform.position);
        }
        if (queryManager.InMergingMode)
        {
            queryManager.newMergeLinePreview.SetPosition(1, polygonMesh.transform.position);
        }
    }

    public void HandleNeighborhoodHoverExited()
    {
        ClearPreviews();
    }

    public void HandleNeighborhoodClick(string name)
    {
        CreateNeighborhoodQuery(name);
    }

    public void ClearPreviews()
    {
        if (prismExtruder != null)
        {
            Destroy(prismExtruder.gameObject);
            prismExtruder = null;
        }
        if (prismMesh != null)
        {
            Destroy(prismMesh.gameObject);
            prismMesh = null;
        }
        if (polygonMesh != null)
        {
            Destroy(polygonMesh.gameObject);
            polygonMesh = null;
        }
        if (wallProjs != null)
        {
            foreach (QueryWallProjection p in wallProjs)
                Destroy(p.gameObject);
            wallProjs = null;
        }
        if (currentLine != null)
        {
            currentLine.DestroyDots();
            Destroy(currentLine.gameObject);
            currentLine = null;
        }

        isDrawingLine = false;
        isSelectingNeighborhoods = false;
    }


    private void CreateNeighborhoodQuery(string neighborhoodName)
    {

        AudioSource.PlayClipAtPoint(sm.goodSoundClip, penCanvas.lastClickPosition);

        if (!prismMesh && !prismExtruder) // if we still don't have a prism, create it 
            PreviewNeighborhoodPrismWithProbuilder(neighborhoodName);

        if (!polygonMesh) // if we still don't have a prism, create it 
            PreviewNeighborhoodPolygonWithProbuilder(neighborhoodName);

        // final settings for our prism before saying goodbye

        GameObject newGameObject = new GameObject();
        newGameObject.transform.parent = queryManager.transform;
        newGameObject.name = "Query";

        AtomicQuery q = newGameObject.AddComponent<AtomicQuery>();
        q.qm = queryManager;
        q.type = queryType;
        q.queryColor = lineColor;
        q.querySpatialLine = currentLine.gameObject; // polygonMesh.gameObject.GetComponentInChildren<LineRenderer>().gameObject;
        q.queryPrism = prismMesh.gameObject;
        q.querySpatialPolygon = polygonMesh.gameObject;
        q.queryListOfSpatialDots = currentLine.GetDotsObjects();

        q.SetQueryTimeSpanFromHeights(sm.stc.baseHeight, sm.stc.baseHeight + sm.stc.transform.localScale.y);
        //q.minY = sm.stc.baseHeight; //
        //q.maxY = q.minY + sm.stc.transform.localScale.y; //
        //q.queryHeight = q.maxY - q.minY; //



            //queryManager.queries.Add(q);

            if (useProbuilder)
            {
                prismMesh.gameObject.AddComponent<MeshCollider>();

                Microsoft.MixedReality.Toolkit.UI.ObjectManipulator om = prismMesh.gameObject.AddComponent<Microsoft.MixedReality.Toolkit.UI.ObjectManipulator>();
                //Microsoft.MixedReality.Toolkit.UI.ObjectManipulator om = newGameObject.AddComponent<Microsoft.MixedReality.Toolkit.UI.ObjectManipulator>();
                om.SmoothingFar = false;
                om.SmoothingNear = false;
                RotationAxisConstraint rc = prismMesh.gameObject.AddComponent<RotationAxisConstraint>();
                //RotationAxisConstraint rc = newGameObject.AddComponent<RotationAxisConstraint>();
                rc.ConstraintOnRotation = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.XAxis | Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.YAxis | Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.ZAxis;

                //Microsoft.MixedReality.Toolkit.Input.NearInteractionGrabbable nig = prismMesh.gameObject.AddComponent<Microsoft.MixedReality.Toolkit.Input.NearInteractionGrabbable>();


                Microsoft.MixedReality.Toolkit.UI.Interactable interac = prismMesh.gameObject.AddComponent<Interactable>();
                //interac.OnClick.AddListener(delegate { queryManager.HandleQueryPrismClick(interac.transform.parent.GetComponent<AtomicQuery>()); });
                interac.OnClick.AddListener(delegate { queryManager.HandleQueryPrismClick(q); });

                //om.OnHoverEntered.AddListener(delegate { queryManager.HandleQueryPrismHover(interac.transform.parent.GetComponent<AtomicQuery>()); });
                om.OnHoverEntered.AddListener(delegate { queryManager.HandleQueryPrismHover(q); });



                if (useClippingBox)
                {
                    GameObject.Find("ClippingBox").GetComponent<ClippingBox>().AddRenderer(prismMesh.gameObject.GetComponent<Renderer>());
                }

                prismMesh.gameObject.name = "Query Prism Mesh";
                prismMesh.gameObject.transform.parent = newGameObject.transform;
            }
            else
            {
                //prismExtruder.gameObject.AddComponent<MeshCollider>();

                Microsoft.MixedReality.Toolkit.UI.ObjectManipulator om = prismExtruder.gameObject.AddComponent<Microsoft.MixedReality.Toolkit.UI.ObjectManipulator>();
                om.SmoothingFar = false;
                om.SmoothingNear = false;

                Microsoft.MixedReality.Toolkit.UI.Interactable interac = prismExtruder.gameObject.AddComponent<Interactable>();
                interac.OnClick.AddListener(delegate { queryManager.HandleQueryPrismClick(interac.transform.parent.GetComponent<AtomicQuery>()); });

                RotationAxisConstraint rc = prismExtruder.gameObject.AddComponent<RotationAxisConstraint>();
                rc.ConstraintOnRotation = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.XAxis | Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.YAxis | Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.ZAxis;

                prismExtruder.gameObject.name = "Query Prism Mesh";
                prismExtruder.gameObject.transform.parent = newGameObject.transform;
            }



            GameObject dotsAndLineDad = new GameObject();
            dotsAndLineDad.transform.parent = newGameObject.transform;
            if (polygonMesh)
                dotsAndLineDad.transform.position = polygonMesh.transform.position;
            dotsAndLineDad.name = "Query Spatial Vertices Dots and Line";
            ///dotsAndLineDad.gameObject.AddComponent<SolverHandler>().TransformOverride = prismMesh.transform;
            ///dotsAndLineDad.gameObject.GetComponent<SolverHandler>().TrackedTargetType = TrackedObjectType.CustomOverride;
            ///dotsAndLineDad.gameObject.AddComponent<FollowPrismOnMap>();
            currentLine.DonateDots(dotsAndLineDad.transform);

            q.queryLineAndDotsAnchor = dotsAndLineDad;


            //currentLine.gameObject.transform.parent = newGameObject.transform;
            currentLine.gameObject.transform.parent = dotsAndLineDad.transform;
            //if (polygonMesh)
            //    currentLine.transform.position = polygonMesh.transform.position;
            currentLine.gameObject.name = "Query Spatial Border Line";
            //currentLine.gameObject.AddComponent<SolverHandler>().TransformOverride = prismMesh.transform;
            //currentLine.gameObject.GetComponent<SolverHandler>().TrackedTargetType = TrackedObjectType.CustomOverride;
            //currentLine.gameObject.AddComponent<FollowPrismOnMap>();




            if (previewPolygon)
            {
                polygonMesh.gameObject.name = "Query Polygon Mesh";
                polygonMesh.gameObject.transform.parent = newGameObject.transform;
                ///polygonMesh.gameObject.AddComponent<SolverHandler>().TransformOverride = prismMesh.transform;
                ///polygonMesh.gameObject.GetComponent<SolverHandler>().TrackedTargetType = TrackedObjectType.CustomOverride;
                ///polygonMesh.gameObject.AddComponent<FollowPrismOnMap>();


                polygonMesh.gameObject.AddComponent<MeshCollider>();

                QueryMapProjection qmp = polygonMesh.gameObject.AddComponent<QueryMapProjection>();
                qmp.myQuery = q;
                qmp.projectionVisual = polygonMesh.gameObject;
                qmp.edgeAndDotsAnchor = dotsAndLineDad.gameObject;

                Microsoft.MixedReality.Toolkit.UI.ObjectManipulator om2 = polygonMesh.gameObject.AddComponent<Microsoft.MixedReality.Toolkit.UI.ObjectManipulator>();
                om2.SmoothingFar = false;
                om2.SmoothingNear = false;
                RotationAxisConstraint rc2 = polygonMesh.gameObject.AddComponent<RotationAxisConstraint>();
                rc2.ConstraintOnRotation = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.XAxis | Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.YAxis | Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.ZAxis;
                MoveAxisConstraint mc2 = polygonMesh.gameObject.AddComponent<MoveAxisConstraint>();
                mc2.ConstraintOnMovement = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.YAxis;

                /*
                prismMesh.gameObject.AddComponent<SolverHandler>().TransformOverride = polygonMesh.transform;
                prismMesh.gameObject.GetComponent<SolverHandler>().TrackedTargetType = TrackedObjectType.CustomOverride;
                prismMesh.gameObject.AddComponent<FollowPrismOnMap>();
                */

            }


            q.GenerateVerticesLatLonList();


            // WALL PROJS

            if (wallProjs == null)
                PreviewWallProjs();

            GameObject projsDad = new GameObject();
            projsDad.gameObject.name = "Query Wall Projections";
            projsDad.gameObject.transform.parent = newGameObject.transform;
            wallProjB.gameObject.transform.parent = projsDad.transform;
            wallProjB.myQuery = q;
            wallProjB.RevealWidgetsAndLabels();
            wallProjL.gameObject.transform.parent = projsDad.transform;
            wallProjL.myQuery = q;
            wallProjL.RevealWidgetsAndLabels();
            wallProjR.gameObject.transform.parent = projsDad.transform;
            wallProjR.myQuery = q;
            wallProjR.RevealWidgetsAndLabels();
            wallProjF.gameObject.transform.parent = projsDad.transform;
            wallProjF.myQuery = q;
            wallProjF.RevealWidgetsAndLabels();
            q.queryWallProjsAnchor = projsDad;
            q.queryWallProjs = wallProjs;



            // QUERY BUTTONS

            AtomicQueryButtonsController queryButtons = Instantiate(queryButtonsPrefab, prismMesh.transform.position - new Vector3(0, 0, 0.1f), Quaternion.identity, transform).GetComponent<AtomicQueryButtonsController>();
            queryButtons.name = "Query Buttons";
            q.myButtons = queryButtons.gameObject;
            queryButtons.myQuery = q;
            queryButtons.transform.parent = newGameObject.transform;


            // QUERY TOOLTIP

            q.queryStatsTooltip = GameObject.Instantiate(tooltipPrefab).GetComponent<ToolTip>();
            q.queryStatsTooltip.transform.parent = newGameObject.transform;
            q.queryStatsTooltip.GetComponent<MixedRealityLineRenderer>().LineMaterial.color = q.queryColor;
            //q.queryStatsTooltip.GetComponent<ToolTipBackgroundMesh>().BackgroundRenderer.material.color = q.queryColor;



            // QUERY IATK DATA VERTEX INTERSECTION COMPUTER / VIEW FILTER

            IATKViewFilter vf = prismMesh.gameObject.AddComponent<IATKViewFilter>();
            q.queryIATKDataVertexIntersectionComputer = vf;
            vf.myQuery = q;
            vf.BRUSH_MODE = IATKViewFilter.BrushMode.SELECT;
            vf.BRUSH_SHAPE = IATKViewFilter.BrushShape.PRISM;
            vf.BRUSH_TYPE = IATKViewFilter.BrushType.FREE;
            vf.refTransform = prismMesh.transform;
            vf.isActive = true;
            vf.computeShader = cs;
            vf.myRenderMaterial = mrm;
            vf.brushedViews = new List<View>();

                vf.brushedViews.Add(GameObject.Find("STC-Pickups").GetComponent<View>());
                vf.brushedViews.Add(GameObject.Find("STC-Dropoffs").GetComponent<View>());
                vf.brushedLinkingViews = new List<LinkingViews>();
                vf.brushedLinkingViews.Add(GameObject.Find("TaxiSTCManager").GetComponent<LinkingViews>());
            

            queryManager.AddAtomicQuery(q);



            // if, exceptionally, we created this new query while in linking mode (creating a new directional query), 
            // we probably wanted it to be the origin/destination of said directional query. let's check. 
            if (queryManager.InLinkingMode)
                queryManager.TryToLinkQueries(q);

            if (queryManager.InMergingMode)
                queryManager.TryToMergeQueries(q);


            prismMesh = null;
            prismExtruder = null;
            polygonMesh = null;
            wallProjs = null;
            newGameObject = null;

            currentLine = null;
            isDrawingLine = false;
        isSelectingNeighborhoods = false;


            if (queryManager.availableColors.Count >= 1)
                lineColor = queryManager.availableColors.Dequeue();
            else
                lineColor = Color.black;
        

    }

    void PreviewNeighborhood(string name)
    {
        if(!isSelectingNeighborhoods)
        {
            if (lineColor == Color.blue) //default color
            {
                if (queryManager.availableColors.Count >= 1)
                    lineColor = queryManager.availableColors.Dequeue();
                else
                    lineColor = Color.black;
            }
            isSelectingNeighborhoods = true;
        }

        PreviewNeighborhoodPolygonWithProbuilder(name);
        PreviewNeighborhoodPrismWithProbuilder(name);
        PreviewNeighborhoodWallProjs(name);
        PreviewNeighborhoodLine(name);
    }

    bool IsAtLeastOneNeighborhoodVerticeOutsideMapBounds(string neighborhoodName)
    {
        int i = 0;
        bool atLeastOnePointOutside = false;
        foreach (LatLonAlt latlon in neighborhoods[neighborhoodName])
        {
            if (!sm.bingMap.mapRenderer.Bounds.Intersects(latlon.LatLon))
                atLeastOnePointOutside = true;
            i++;
        }
        return atLeastOnePointOutside;
    }

    bool IsAtLeastOneNeighborhoodVerticeInsideMapBounds(string neighborhoodName)
    {
        int i = 0;
        bool atLeastOnePointInside = false;
        foreach (LatLonAlt latlon in neighborhoods[neighborhoodName])
        {
            if (sm.bingMap.mapRenderer.Bounds.Intersects(latlon.LatLon))
                atLeastOnePointInside = true;
            i++;
        }
        return atLeastOnePointInside;
    }

    Vector3[] GetNeighborhoodXYZPoints(string neighborhoodName)
    {
        Vector3[] pointsAsVectorsXYZ = new Vector3[neighborhoods[neighborhoodName].Count];
        //Vector2[] pointsAsVectorsXY = new Vector2[neighborhoods[neighborhoodName].Count];
        int i = 0;
        foreach (LatLonAlt latlon in neighborhoods[neighborhoodName])
        {
            pointsAsVectorsXYZ[i] = MapRendererTransformExtensions.TransformLatLonAltToWorldPoint(sm.bingMap.mapRenderer, latlon);
            pointsAsVectorsXYZ[i] += new Vector3(0, 0.005f, 0);
            //pointsAsVectorsXY[i] = new Vector2(pointsAsVectorsXYZ[i].x, pointsAsVectorsXYZ[i].z);
            i++;
        }
        return pointsAsVectorsXYZ;
    }

    // Adapted from https://github.com/Unity-Technologies/ProBuilder-API-Examples/blob/master/Runtime/Misc/CreatePolyShape.cs
    private bool PreviewNeighborhoodPrismWithProbuilder(string neighborhoodName)
    {
        if (prismMesh != null)
        {
            Destroy(prismMesh.gameObject);
        }

        var go = new GameObject();
        go.transform.parent = this.transform;

        prismMesh = go.gameObject.AddComponent<ProBuilderMesh>();

        Vector3[] pointsAsVectorsXYZ = GetNeighborhoodXYZPoints(neighborhoodName);

        ActionResult result = prismMesh.CreateShapeFromPolygon(pointsAsVectorsXYZ, 1f, false);

        if (result.status == ActionResult.Status.Success)
        {
            //prismMesh.SetPivot(currentLine.GetAveragePosition());

            prismMesh.CenterPivot(prismMesh.selectedFaceIndexes.ToArray());

            //prismMesh.transform.localPosition -= new Vector3(0, currentLine.GetOrigin().y - sm.stc.baseHeight, 0);
            prismMesh.transform.localPosition -= new Vector3(0, pointsAsVectorsXYZ[0].y - sm.stc.baseHeight, 0);

            prismMesh.transform.localScale = new Vector3(prismMesh.transform.localScale.x, sm.stc.transform.localScale.y, prismMesh.transform.localScale.z);

            switch (queryType)
            {
                case QueryType.Origin: go.GetComponent<MeshRenderer>().material = queryManager.pickupQueryMaterial; break;
                case QueryType.Destination: go.GetComponent<MeshRenderer>().material = queryManager.dropoffQueryMaterial; break;
                case QueryType.Either: go.GetComponent<MeshRenderer>().material = queryManager.doubleQueryMaterial; break;
            }
            Color c = go.GetComponent<MeshRenderer>().material.color;
            c.a = queryManager.editModeQueryTransparency;
            go.GetComponent<MeshRenderer>().material.color = c;

            return true;
        }
        else
        {
            Destroy(go);
            prismMesh = null;
            return false;
        }
    }

    // Adapted from https://github.com/Unity-Technologies/ProBuilder-API-Examples/blob/master/Runtime/Misc/CreatePolyShape.cs
    private void PreviewNeighborhoodPolygonWithProbuilder(string neighborhoodName)
    {
        if (polygonMesh != null)
        {
            Destroy(polygonMesh.gameObject);
        }

        var go = new GameObject();
        go.transform.parent = this.transform;

        polygonMesh = go.gameObject.AddComponent<ProBuilderMesh>();

        Vector3[] pointsAsVectorsXYZ = GetNeighborhoodXYZPoints(neighborhoodName);

        polygonMesh.CreateShapeFromPolygon(pointsAsVectorsXYZ, 0.002f, false);

        polygonMesh.CenterPivot(polygonMesh.selectedFaceIndexes.ToArray());

        //polygonMesh.CreatePolygon(, false);
        //prismMesh.transform.localPosition -= new Vector3(0, currentLine.GetOrigin().y - sm.stc.baseHeight, 0);

        switch (queryType)
        {
            //case QueryType.Origin: go.GetComponent<MeshRenderer>().material = queryManager.pickupQueryProjectionMaterial; break;
            //case QueryType.Destination: go.GetComponent<MeshRenderer>().material = queryManager.dropoffQueryProjectionMaterial; break;
            //case QueryType.Either: go.GetComponent<MeshRenderer>().material = queryManager.doubleQueryProjectionMaterial; break;
            case QueryType.Origin: go.GetComponent<MeshRenderer>().material = queryManager.pickupQueryMaterial; break;
            case QueryType.Destination: go.GetComponent<MeshRenderer>().material = queryManager.dropoffQueryMaterial; break;
            case QueryType.Either: go.GetComponent<MeshRenderer>().material = queryManager.doubleQueryMaterial; break;
        }
        Color c = go.GetComponent<MeshRenderer>().material.color;
        //c.a = queryManager.editModeQueryTransparency;
        c.a = queryManager.useModeQueryTransparency;
        go.GetComponent<MeshRenderer>().material.color = c;




    }

    private void PreviewNeighborhoodLine(string neighborhoodName)
    {
        if(currentLine != null)
        {
            Destroy(currentLine.gameObject);
        }

        LineController lineController = Instantiate(linePrefab, transform).GetComponent<LineController>();
        currentLine = lineController;

        /*LineRenderer line = GameObject.Instantiate(linePrefab).GetComponent<LineRenderer>();
        line.gameObject.transform.parent = go.transform;
        line.startColor = lineColor;
        line.endColor = lineColor;
        line.useWorldSpace = true;
        line.positionCount = pointsAsVectorsXYZ.Length;
        for (int j = 0; j < line.positionCount; j++)
            line.SetPosition(j, pointsAsVectorsXYZ[j]);*/


        lineController.SetColor(lineColor);
        //isDrawingLine = true;

        Vector3[] pointsAsVectorXYZ = GetNeighborhoodXYZPoints(neighborhoodName);
        for(int i =0; i<pointsAsVectorXYZ.Length;i++)
        {
            DotController dot = Instantiate(dotPrefab, pointsAsVectorXYZ[i] + new Vector3(0, 0.0055f, 0), Quaternion.identity, transform).GetComponent<DotController>();
            dot.GetComponent<Renderer>().material.color = lineColor;
            dot.GetComponent<Renderer>().enabled = false;
            currentLine.AddDot(dot);
        }
    }


    private void PreviewNeighborhoodWallProjs(string neighborhoodName)
    {
        if (wallProjs == null)
        {
            wallProjs = new List<QueryWallProjection>();
            wallProjB = GameObject.Instantiate(wallProjPrefab);
            wallProjB.transform.parent = this.transform;
            wallProjs.Add(wallProjB);

            wallProjL = GameObject.Instantiate(wallProjPrefab);
            wallProjL.Rotate(new Vector3(0, -90, 0));
            wallProjL.transform.parent = this.transform;
            wallProjs.Add(wallProjL);

            wallProjR = GameObject.Instantiate(wallProjPrefab);
            wallProjR.Rotate(new Vector3(0, 90, 0));
            wallProjR.transform.parent = this.transform;
            wallProjs.Add(wallProjR);

            wallProjF = GameObject.Instantiate(wallProjPrefab);
            wallProjF.transform.parent = this.transform;
            wallProjF.gameObject.SetActive(false);
            wallProjs.Add(wallProjF);

            foreach (QueryWallProjection proj in wallProjs)
            {
                switch (queryType)
                {
                    case QueryType.Origin: proj.SetProjectionMaterial(queryManager.pickupQueryProjectionMaterial); break;
                    case QueryType.Destination: proj.SetProjectionMaterial(queryManager.dropoffQueryProjectionMaterial); break;
                    case QueryType.Either: proj.SetProjectionMaterial(queryManager.doubleQueryProjectionMaterial); break;
                }
                //Color c = proj.GetComponent<MeshRenderer>().material.color;
                //c.a = queryManager.editModeQueryTransparency;
                //proj.GetComponent<MeshRenderer>().material.color = c;
                proj.SetColor(lineColor);
            }
        }

        float minx = 1000, maxx = -1000, minz = 1000, maxz = -1000;
        Vector3[] positions = GetNeighborhoodXYZPoints(neighborhoodName);
        for (int i = 0; i < positions.Length; i++)
        {
            if (positions[i].x < minx)
                minx = positions[i].x;
            if (positions[i].x > maxx)
                maxx = positions[i].x;
            if (positions[i].z < minz)
                minz = positions[i].z;
            if (positions[i].z > maxz)
                maxz = positions[i].z;
        }

        // BACK
        //wallProjB.transform.localScale = new Vector3(maxx - minx, prismMesh.transform.localScale.y, wallProjB.transform.localScale.z);
        //wallProjB.transform.position = new Vector3((maxx + minx) / 2f, prismMesh.transform.position.y, sm.bingMap.transform.position.z + sm.stc.transform.localScale.z / 2 - 0.001f);
        wallProjB.SetProjectionScale(new Vector3(maxx - minx, prismMesh.transform.localScale.y, wallProjB.GetProjectionScale().z));
        wallProjB.SetProjectionPosition(new Vector3((maxx + minx) / 2f, prismMesh.transform.position.y, sm.bingMap.transform.position.z + sm.stc.transform.localScale.z / 2 - 0.005f));


        // LEFT
        //wallProjL.transform.localScale = new Vector3(maxz - minz, prismMesh.transform.localScale.y, wallProjL.transform.localScale.z);
        //wallProjL.transform.position = new Vector3(sm.bingMap.transform.position.x - sm.stc.transform.localScale.x / 2 + 0.001f, prismMesh.transform.position.y, (maxz + minz) / 2f);
        wallProjL.SetProjectionScale(new Vector3(maxz - minz, prismMesh.transform.localScale.y, wallProjL.GetProjectionScale().z));
        wallProjL.SetProjectionPosition(new Vector3(sm.bingMap.transform.position.x - sm.stc.transform.localScale.x / 2 + 0.005f, prismMesh.transform.position.y, (maxz + minz) / 2f));


        // RIGHT
        //wallProjR.transform.localScale = new Vector3(maxz - minz, prismMesh.transform.localScale.y, wallProjR.transform.localScale.z);
        //wallProjR.transform.position = new Vector3(sm.bingMap.transform.position.x + sm.stc.transform.localScale.x / 2 - 0.001f, prismMesh.transform.position.y, (maxz + minz) / 2f);
        wallProjR.SetProjectionScale(new Vector3(maxz - minz, prismMesh.transform.localScale.y, wallProjR.GetProjectionScale().z));
        wallProjR.SetProjectionPosition(new Vector3(sm.bingMap.transform.position.x + sm.stc.transform.localScale.x / 2 - 0.005f, prismMesh.transform.position.y, (maxz + minz) / 2f));


        // FRONT
        if (sm.ClippedEgoRoom)
        {
            wallProjF.gameObject.SetActive(true);
            //wallProjF.transform.localScale = new Vector3(maxx - minx, prismMesh.transform.localScale.y, wallProjF.transform.localScale.z);
            //wallProjF.transform.position = new Vector3((maxx + minx) / 2f, prismMesh.transform.position.y, sm.bingMap.transform.position.z - sm.stc.transform.localScale.z / 2 + 0.001f);
            wallProjF.SetProjectionScale(new Vector3(maxx - minx, prismMesh.transform.localScale.y, wallProjF.GetProjectionScale().z));
            wallProjF.SetProjectionPosition(new Vector3((maxx + minx) / 2f, prismMesh.transform.position.y, sm.bingMap.transform.position.z - sm.stc.transform.localScale.z / 2 + 0.005f));

        }
        else
        {
            wallProjF.gameObject.SetActive(false);
        }


    }


}
