using IATK;
using Microsoft.Geospatial;
using Microsoft.Maps.Unity;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;


public class ChoroplethManager : MonoBehaviour
{
    [Header("Assets")]

    public ScenarioManager sm;
    public Material neighborhoodTransparentMaterial;
    public Material neighborhoodSolidMaterial;
    public TextMesh neighborhoodLabelPrefab;
    public GameObject linePrefab;

    [Header("Settings")]

    public bool showNameLabels;
    public float alpha = 250f;

    [Header("Stats")]

    public bool choroplethModeOn = false;

    Dictionary<string, List<LatLonAlt>> neighborhoods = new Dictionary<string, List<LatLonAlt>>();
    Dictionary<string, GameObject> neighborhoodObjects = new Dictionary<string, GameObject>();
    Dictionary<string, PointCounter> neighborhoodPointCounters = new Dictionary<string, PointCounter>();
    Dictionary<string, TextMesh> neighborhoodLabels = new Dictionary<string, TextMesh>();
    Dictionary<string, int> neighborhoodCounts = new Dictionary<string, int>();
    ColorHeatMap sequentialRedColorScale;
    Dictionary<string, List<PointCounter>> neighborhoodStacks = new Dictionary<string, List<PointCounter>>();

    // Start is called before the first frame update
    void Start()
    {
        LoadNeighborhoodsFile();
        sequentialRedColorScale = new ColorHeatMap(alpha);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToggleMode()
    {
        if (choroplethModeOn)
            DeactivateCloroplethMode();
        else
        {
            StopAllCoroutines();
            StartCoroutine(ActivateCloroplethMode());
        }
    }

    public IEnumerator ActivateCloroplethMode()
    {
        sm.bingMap.gameObject.GetComponent<MixedRealityMapInteractionHandler>().enabled = false;
        sm.stc.walls.gameObject.GetComponent<STCWallsInteractionHandler>().enabled = false;


        yield return StartCoroutine(CreateNeighborhoodPolygonsForCurrentSelection());
        choroplethModeOn = true;

        yield return StartCoroutine(ColorNeighborhoods()); 
    }




    public IEnumerator ColorNeighborhoods()
    {
        int minCount = 100000;
        int maxCount = 0;

        string minRegion = "", maxRegion = "";

        foreach(string neighborhoodName in neighborhoodPointCounters.Keys)
        {
            int count = neighborhoodPointCounters[neighborhoodName].RecomputeNumberOfFilteredPoints();
            if(count < minCount)
            {
                minCount = count;
                minRegion = neighborhoodName;
            }
            if(count > maxCount)
            {
                maxCount = count;
                maxRegion = neighborhoodName;
            }

            neighborhoodCounts[neighborhoodName] = count;

            if(neighborhoodLabels.ContainsKey(neighborhoodName))
                neighborhoodLabels[neighborhoodName].text = neighborhoodName + "\n(" + count.ToString() + ")";

            yield return null;
        }

        foreach (string neighborhoodName in neighborhoodObjects.Keys)
        {
            neighborhoodObjects[neighborhoodName].GetComponent<Renderer>().material.color = sequentialRedColorScale.GetColorForValue((float)neighborhoodCounts[neighborhoodName], (float)maxCount);
            //neighborhoodObjects[neighborhoodName].GetComponent<Renderer>().material.SetColor("_Color", sequentialRedColorScale.GetColorForValue(neighborhoodCounts[neighborhoodName], maxCount));

            //neighborhoodLabels[neighborhoodName].color = sequentialRedColorScale.GetColorForValue((float)neighborhoodCounts[neighborhoodName], (float)maxCount); 

            //Debug.Log("Color for " + neighborhoodName + " is " + sequentialRedColorScale.GetColorForValue(neighborhoodCounts[neighborhoodName], maxCount).ToString());

            yield return null;
        }


        //Debug.Log("Min: " + minCount.ToString() + " trips in " + minRegion + ". Max: " + maxCount.ToString() + " trips in " + maxRegion + ".");
    }


    public void DeactivateCloroplethMode()
    {
        sm.bingMap.gameObject.GetComponent<MixedRealityMapInteractionHandler>().enabled = true;
        sm.stc.walls.gameObject.GetComponent<STCWallsInteractionHandler>().enabled = true;

        DestroyNeighborhoodPolygons();
        DestroyAllNeighborhoodStacks();
        choroplethModeOn = false;
        //ClearPreviews();
    }


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
                if (name != "")
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

    public void DestroyNeighborhoodPolygons()
    {
        foreach (string neighborhood in neighborhoodPointCounters.Keys)
            Destroy(neighborhoodPointCounters[neighborhood].gameObject);

        foreach (string neighborhood in neighborhoodObjects.Keys)
            Destroy(neighborhoodObjects[neighborhood].transform.parent.gameObject);

        neighborhoodObjects = new Dictionary<string, GameObject>();
        neighborhoodLabels = new Dictionary<string, TextMesh>();
        neighborhoodCounts = new Dictionary<string, int>();
    }

    public IEnumerator CreateNeighborhoodPolygonsForCurrentSelection()
    {
        if (neighborhoodObjects.Count > 0)
        {
            DestroyNeighborhoodPolygons();
        }

        foreach (string neighborhoodName in neighborhoods.Keys)
        {
            Vector3[] pointsAsVectorsXYZ = GetNeighborhoodXYZPoints(neighborhoodName);
            //Vector2[] pointsAsVectorsXY = new Vector2[neighborhoods[name].Count];

            if (IsAtLeastOneNeighborhoodVerticeOutsideMapBounds(neighborhoodName))
            {
                neighborhoodPointCounters[neighborhoodName] = CreateNeighborhoodViewFilter(neighborhoodName);
                continue;
            }


            // PROBUILDER VERSION

            var go = new GameObject();
            go.transform.parent = this.transform;
            go.name = neighborhoodName;


            var poly = new GameObject();
            poly.transform.parent = go.transform;
            poly.name = neighborhoodName + " Polygon";


            
            ProBuilderMesh neighborhoodPolygonMesh = poly.gameObject.AddComponent<ProBuilderMesh>();

            ActionResult a = neighborhoodPolygonMesh.CreateShapeFromPolygon(pointsAsVectorsXYZ, 0.001f, false);

            if (a.status == ActionResult.Status.Failure) // IF PROBUILDER HAS TROUBLE BUILDING A MESH, SOMETIMES INVERTING THE ORDER OF THE POINTS HELPS
            {
                //Debug.Log("TRY 1: " + neighborhoodName + " " + pointsAsVectorsXYZ.Length.ToString() + " " + a.status.ToString() + " " + a.notification);

                neighborhoods[neighborhoodName].Reverse();
                pointsAsVectorsXYZ = GetNeighborhoodXYZPoints(neighborhoodName);
                a = neighborhoodPolygonMesh.CreateShapeFromPolygon(pointsAsVectorsXYZ, 0.001f, false); // let's try again
            }
            
            /*if (a.status == ActionResult.Status.Failure) // IF PROBUILDER HAS TROUBLE BUILDING A MESH, SOMETIMES INVERTING THE ORDER OF THE POINTS HELPS
            {
                Debug.Log("TRY 2: " + neighborhoodName + " " + pointsAsVectorsXYZ.Length.ToString() + " " + a.status.ToString() + " " + a.notification);

                double latAcc = 0, lonAcc = 0;
                foreach(LatLonAlt p in neighborhoods[neighborhoodName])
                {
                    latAcc += p.LatitudeInDegrees;
                    lonAcc += p.LongitudeInDegrees;
                }
                latAcc /= neighborhoods[neighborhoodName].Count;
                lonAcc /= neighborhoods[neighborhoodName].Count;

                neighborhoods[neighborhoodName] = neighborhoods[neighborhoodName].OrderByDescending(x => System.Math.Atan2(-1* x.LongitudeInDegrees + lonAcc, x.LatitudeInDegrees - latAcc)).ToList();

                pointsAsVectorsXYZ = GetNeighborhoodXYZPoints(neighborhoodName);
                a = neighborhoodPolygonMesh.CreateShapeFromPolygon(pointsAsVectorsXYZ, 0.001f, false); // let's try again
            }*/
            
            if (a.status == ActionResult.Status.Failure)
            {
                Debug.Log("TRY 2: " + neighborhoodName + " " + pointsAsVectorsXYZ.Length.ToString() + " " + a.status.ToString() + " " + a.notification);
                Destroy(go);
                continue;
            }


            neighborhoodPolygonMesh.transform.position -= new Vector3(0, 0.001f, 0);
            neighborhoodPolygonMesh.CenterPivot(neighborhoodPolygonMesh.selectedFaceIndexes.ToArray());
            



            /*
            
            // PRISM EXTRUDER VERSION

            Vector2[] pointsAsVectorsXY = GetNeighborhoodXYPoints(neighborhoodName);

            GameObject polyExtruderGO = new GameObject();
            polyExtruderGO.name = name;
            polyExtruderGO.transform.parent = this.transform;
            PolyExtruder neighborhoodPolygonMesh = polyExtruderGO.AddComponent<PolyExtruder>();

            neighborhoodPolygonMesh.isOutlineRendered = false ;
            //prismExtruder.outlineWidth = 0.001f;
            ///prismExtruder.outlineColor = Color.black;
            neighborhoodPolygonMesh.createPrism(polyExtruderGO.name, 0.002f, sm.stc.baseHeight, pointsAsVectorsXY, Color.blue, true, true, true);
            ///
            ///LineRenderer lr = prismExtruder.gameObject.AddComponent<LineRenderer>();

            ///neighborhoodObjects.Add(prismExtruder.gameObject);
            */
            



            
            poly.GetComponent<MeshRenderer>().material = neighborhoodTransparentMaterial; //queryManager.dropoffQueryMaterial;
            UnityEngine.Color c = poly.GetComponent<MeshRenderer>().material.color;
            //c.a = queryManager.editModeQueryTransparency;
            c.a = 0.4f;// queryManager.useModeQueryTransparency;
            poly.GetComponent<MeshRenderer>().material.color = c;

            //LineRenderer lr = neighborhoodPolygonMesh.gameObject.AddComponent<LineRenderer>();
            LineRenderer lr = GameObject.Instantiate(linePrefab).GetComponent<LineRenderer>();
            lr.gameObject.transform.parent = go.gameObject.transform;

            neighborhoodObjects[neighborhoodName] = (neighborhoodPolygonMesh.gameObject);

            if(showNameLabels)
            { 
                TextMesh label = GameObject.Instantiate(neighborhoodLabelPrefab).GetComponent<TextMesh>();
                label.gameObject.transform.parent = go.gameObject.transform;
                label.gameObject.transform.localPosition = poly.gameObject.transform.localPosition + new Vector3(0f, 0.02f, 0f);
                label.text = neighborhoodName;
                neighborhoodLabels[neighborhoodName] = label;
            }

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
            om.OnHoverExited.AddListener(delegate { HandleNeighborhoodHoverExited(neighborhoodName); });





            // CONTOUR

            //lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            lr.useWorldSpace = true;
            //lr.alignment = LineAlignment.TransformZ;
            lr.startWidth = 0.005f;
            lr.endWidth = 0.005f;
            lr.startColor = UnityEngine.Color.black;
            lr.endColor = UnityEngine.Color.black;
            lr.positionCount = pointsAsVectorsXYZ.Length;
            for (int j = 0; j < lr.positionCount; j++)
                lr.SetPosition(j, pointsAsVectorsXYZ[j]);


            // CREATING VIEW FILTER

            neighborhoodPointCounters[neighborhoodName] = CreateNeighborhoodViewFilter(neighborhoodName);


            yield return null;



        }
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

    Vector2[] GetNeighborhoodXYPoints(string neighborhoodName)
    {
        Vector3[] pointsAsVectorsXYZ = new Vector3[neighborhoods[neighborhoodName].Count];
        Vector2[] pointsAsVectorsXY = new Vector2[neighborhoods[neighborhoodName].Count];
        int i = 0;
        foreach (LatLonAlt latlon in neighborhoods[neighborhoodName])
        {
            pointsAsVectorsXYZ[i] = MapRendererTransformExtensions.TransformLatLonAltToWorldPoint(sm.bingMap.mapRenderer, latlon);
            pointsAsVectorsXYZ[i] += new Vector3(0, 0.005f, 0);
            pointsAsVectorsXY[i] = new Vector2(pointsAsVectorsXYZ[i].x, pointsAsVectorsXYZ[i].z);
            i++;
        }
        return pointsAsVectorsXY;
    }


    private PointCounter CreateNeighborhoodViewFilter(string neighborhoodName)
    {
        var go = new GameObject();

        if (neighborhoodObjects.ContainsKey(neighborhoodName))
            go.transform.parent = neighborhoodObjects[neighborhoodName].transform.parent;
        else
            go.transform.parent = this.transform;

        go.name = neighborhoodName + " PointCounter";

        //ProBuilderMesh prismMesh = go.gameObject.AddComponent<ProBuilderMesh>();

        Vector3[] pointsAsVectorsXYZ = GetNeighborhoodXYZPoints(neighborhoodName);

        //ActionResult result = prismMesh.CreateShapeFromPolygon(pointsAsVectorsXYZ, 1f, false);

        //if (result.status == ActionResult.Status.Success)
       // {
         //   //prismMesh.SetPivot(currentLine.GetAveragePosition());

           // prismMesh.CenterPivot(prismMesh.selectedFaceIndexes.ToArray());

           // //prismMesh.transform.localPosition -= new Vector3(0, currentLine.GetOrigin().y - sm.stc.baseHeight, 0);
           // prismMesh.transform.localPosition -= new Vector3(0, pointsAsVectorsXYZ[0].y - sm.stc.baseHeight, 0);

           // prismMesh.transform.localScale = new Vector3(prismMesh.transform.localScale.x, sm.stc.transform.localScale.y, prismMesh.transform.localScale.z);

           // go.GetComponent<MeshRenderer>().enabled = false;



            // QUERY IATK DATA VERTEX INTERSECTION COMPUTER / VIEW FILTER

            IATKViewFilter vf = go.AddComponent<IATKViewFilter>();
            vf.BRUSH_MODE = IATKViewFilter.BrushMode.SELECT;
            vf.BRUSH_SHAPE = IATKViewFilter.BrushShape.PRISM;
            vf.BRUSH_TYPE = IATKViewFilter.BrushType.FREE;
            vf.OnlyGenerateTexturesAndWaitForQueryManager = true;
            vf.refTransform = go.transform;
            vf.isActive = true;
            vf.computeShader = sm.qc.cs;
            vf.myRenderMaterial = sm.qc.mrm;
            vf.brushedViews = new List<View>();

            if (sm.stc is ODSTCManager)
            {
                vf.brushedViews.Add(GameObject.Find("STC-Pickups").GetComponent<View>());
                vf.brushedViews.Add(GameObject.Find("STC-Dropoffs").GetComponent<View>());
                vf.brushedLinkingViews = new List<LinkingViews>();
                vf.brushedLinkingViews.Add(GameObject.Find("TaxiSTCManager").GetComponent<LinkingViews>());
            }


            LineController lineController = Instantiate(linePrefab, go.transform).GetComponent<LineController>();
            lineController.GetComponent<LineRenderer>().enabled = false;
            for (int i = 0; i < pointsAsVectorsXYZ.Length; i++)
            {
                DotController dot = Instantiate(sm.qc.dotPrefab, pointsAsVectorsXYZ[i] + new Vector3(0, 0.0055f, 0), Quaternion.identity, go.transform).GetComponent<DotController>();
                dot.GetComponent<Renderer>().enabled = false;
                lineController.AddDot(dot);
            }

            PointCounter q = go.AddComponent<PointCounter>();
            q.qm = sm.qm;
            q.queryIATKDataVertexIntersectionComputer = vf;
            q.type = Query.QueryType.Either;
            q.queryListOfSpatialDots = lineController.GetDotsObjects();

            q.minY = sm.stc.baseHeight; //
            q.maxY = q.minY + sm.stc.transform.localScale.y; //
            q.queryHeight = q.maxY - q.minY; //

            vf.myQuery = q;



            return q;
        //}
       // else
       // {
       //     go.Destroy();
       //     return null;
       // }
    }


    private IEnumerator CreateNeighborhoodStack(string neighborhoodName)
    {
        List<PointCounter> newStack = new List<PointCounter>();

        /*Vector3[] pointsAsVectorsXYZ = GetNeighborhoodXYZPoints(neighborhoodName);

        LineController lineController = Instantiate(linePrefab, this.transform).GetComponent<LineController>();
        lineController.GetComponent<LineRenderer>().enabled = false;
        for (int i = 0; i < pointsAsVectorsXYZ.Length; i++)
        {
            DotController dot = Instantiate(sm.qc.dotPrefab, pointsAsVectorsXYZ[i] + new Vector3(0, 0.0055f, 0), Quaternion.identity, lineController.transform).GetComponent<DotController>();
            dot.GetComponent<Renderer>().enabled = false;
            lineController.AddDot(dot);
        }*/

        for (int i =0; i< sm.stc.walls.num_slices; i++)
        {
            GameObject go;

            if (neighborhoodObjects.ContainsKey(neighborhoodName))
            {
                go = GameObject.Instantiate(neighborhoodObjects[neighborhoodName], neighborhoodObjects[neighborhoodName].transform.parent);

                Destroy(go.GetComponent<Microsoft.MixedReality.Toolkit.UI.ObjectManipulator>());
                Destroy(go.GetComponent<Interactable>());
                Destroy(go.GetComponent<Collider>());

                go.name = neighborhoodName + " Stack PointCounter " + i;

                go.GetComponent<Renderer>().material = neighborhoodSolidMaterial;


            }
            else
            {
                continue; 
            }


            /*if (neighborhoodObjects.ContainsKey(neighborhoodName))
                go.transform.parent = neighborhoodObjects[neighborhoodName].transform;
            else
                go.transform.parent = this.transform;

            go.name = neighborhoodName + " Stack PointCounter " + i;

            ProBuilderMesh prismMesh = go.gameObject.AddComponent<ProBuilderMesh>();


            ActionResult result = prismMesh.CreateShapeFromPolygon(pointsAsVectorsXYZ, 1f, false);

            if (result.status == ActionResult.Status.Success)
            {
                //prismMesh.SetPivot(currentLine.GetAveragePosition());

                prismMesh.CenterPivot(prismMesh.selectedFaceIndexes.ToArray());

                // //prismMesh.transform.localPosition -= new Vector3(0, currentLine.GetOrigin().y - sm.stc.baseHeight, 0);

                prismMesh.GetComponent<Renderer>().material = neighborhoodSolidMaterial;

            }
            else
            {
                continue;
            }
            // go.GetComponent<MeshRenderer>().enabled = false;
            */


            // QUERY IATK DATA VERTEX INTERSECTION COMPUTER / VIEW FILTER

            IATKViewFilter vf = go.AddComponent<IATKViewFilter>();
            vf.BRUSH_MODE = IATKViewFilter.BrushMode.SELECT;
            vf.BRUSH_SHAPE = IATKViewFilter.BrushShape.PRISM;
            vf.BRUSH_TYPE = IATKViewFilter.BrushType.FREE;
            vf.OnlyGenerateTexturesAndWaitForQueryManager = true;
            vf.refTransform = go.transform;
            vf.isActive = true;
            vf.computeShader = sm.qc.cs;
            vf.myRenderMaterial = sm.qc.mrm;
            vf.brushedViews = new List<View>();

            if (sm.stc is ODSTCManager)
            {
                vf.brushedViews.Add(GameObject.Find("STC-Pickups").GetComponent<View>());
                vf.brushedViews.Add(GameObject.Find("STC-Dropoffs").GetComponent<View>());
                vf.brushedLinkingViews = new List<LinkingViews>();
                vf.brushedLinkingViews.Add(GameObject.Find("TaxiSTCManager").GetComponent<LinkingViews>());
            }

            PointCounter q = go.AddComponent<PointCounter>();
            q.qm = sm.qm;
            q.queryIATKDataVertexIntersectionComputer = vf;
            q.type = Query.QueryType.Either;
            q.queryListOfSpatialDots = neighborhoodPointCounters[neighborhoodName].queryListOfSpatialDots; // lineController.GetDotsObjects();

            q.minY = sm.stc.baseHeight + i * (sm.stc.transform.localScale.y / sm.stc.walls.num_slices); //
            q.maxY = q.minY + (sm.stc.transform.localScale.y / sm.stc.walls.num_slices); //
            q.queryHeight = q.maxY - q.minY; //

            //prismMesh.transform.localPosition -= new Vector3(0, pointsAsVectorsXYZ[0].y - q.minY, 0);
            go.transform.position = new Vector3(neighborhoodObjects[neighborhoodName].transform.position.x, (q.minY + q.maxY) / 2, neighborhoodObjects[neighborhoodName].transform.position.z);

            go.transform.localScale = new Vector3(neighborhoodObjects[neighborhoodName].transform.localScale.x, (sm.stc.transform.localScale.y / sm.stc.walls.num_slices) / 0.001f, neighborhoodObjects[neighborhoodName].transform.localScale.z);
            //go.transform.localScale = new Vector3(neighborhoodObjects[neighborhoodName].transform.localScale.x, (sm.stc.transform.localScale.y / sm.stc.walls.num_slices), neighborhoodObjects[neighborhoodName].transform.localScale.z);


            vf.myQuery = q;



            newStack.Add(q);

            yield return null;
        }

        //lineController.gameObject.Destroy();


        neighborhoodStacks[neighborhoodName] = newStack;

        if (visibleStacks.Contains(neighborhoodName))
            ShowStack(neighborhoodName);
        else
            HideStack(neighborhoodName);

        RecolorVisibleStacks();

    }


    public void RecolorVisibleStacks()
    {
        int minCount = 100000;
        int maxCount = 0;

        foreach (string neighborhoodName in neighborhoodStacks.Keys)
        {
            if (visibleStacks.Contains(neighborhoodName))
            { 
                foreach(PointCounter pc in neighborhoodStacks[neighborhoodName])
                {
                    int count;
                    if (pc.lastKnownNumberOfFilteredPoints >= 0)
                        count = pc.lastKnownNumberOfFilteredPoints;
                    else 
                        count =  pc.RecomputeNumberOfFilteredPoints();

                    if (count < minCount)
                    {
                        minCount = count;
                    }
                    if (count > maxCount)
                    {
                        maxCount = count;
                    }
                }
            }
        }

        foreach (string neighborhoodName in neighborhoodStacks.Keys)
        {
            if (visibleStacks.Contains(neighborhoodName))
            {
                foreach (PointCounter pc in neighborhoodStacks[neighborhoodName])
                {
                    pc.gameObject.GetComponent<Renderer>().material.color = sequentialRedColorScale.GetColorForValue(pc.lastKnownNumberOfFilteredPoints, (float)maxCount);
                }
            }
        }
    }
    

    public void UpdateChoroplethsAfterSTCInteractions()
    {
        if(choroplethModeOn)
        {
            DestroyNeighborhoodPolygons();
            CreateNeighborhoodPolygonsForCurrentSelection();
        }
    }

    public void DestroyNeighborhoodStack(string name)
    {
        foreach(PointCounter p in neighborhoodStacks[name])
        {
            Destroy(p.gameObject);
        }
    }

    public void DestroyAllNeighborhoodStacks()
    {
        foreach (string key in neighborhoodStacks.Keys)
        {
            DestroyNeighborhoodStack(key);
        }
        neighborhoodStacks = new Dictionary<string, List<PointCounter>>();
        //visibleStacks = new HashSet<string>();
    }



    public void HandleNeighborhoodHover(string name)
    {
        //PreviewNeighborhood(name);

        visibleStacks.Add(name);

        if (!neighborhoodStacks.ContainsKey(name))
        {
            //neighborhoodStacks[name] = null;
            StartCoroutine(CreateNeighborhoodStack(name));
        }
        else 
        {
            //  if(neighborhoodStacks[name] != null)
            ShowStack(name);
            RecolorVisibleStacks();
        }




        //Debug.Log(visibleStacks.Count);

    }

    public void HideStack(string name)
    {
        //if (neighborhoodStacks.ContainsKey(name) && neighborhoodStacks[name][0].gameObject.activeSelf)
        //{
        foreach (PointCounter pc in neighborhoodStacks[name])
            pc.GetComponent<MeshRenderer>().enabled = false; // gameObject.SetActive(false);

            //RecolorStacks();
        //}
    }

    public void ShowStack(string name)
    {
        //if (neighborhoodStacks.ContainsKey(name) && !neighborhoodStacks[name][0].gameObject.activeSelf)
        //{
        foreach (PointCounter pc in neighborhoodStacks[name])
            pc.GetComponent<MeshRenderer>().enabled = true; // SetActive(true);

            //RecolorStacks();
        //}
    }

    public void HandleNeighborhoodHoverExited(string name)
    {
        //ClearPreviews();

        //Debug.Log("Exited " + name);

        visibleStacks.Remove(name);

        if (neighborhoodStacks.ContainsKey(name))
        {
            //DestroyNeighborhoodStack(name);
            //neighborhoodStacks.Remove(name);

            HideStack(name);
            RecolorVisibleStacks();
        }

    }

    HashSet<string> visibleStacks = new HashSet<string>();

    public void HandleNeighborhoodClick(string name)
    {
        //CreateNeighborhoodQuery(name);

        // do nothing
    }


}
