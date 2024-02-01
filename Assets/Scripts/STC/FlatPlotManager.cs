using IATK;
using Microsoft.Maps.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ODSTCManager;

public class FlatPlotManager : MonoBehaviour
{
    // input data
    public TextAsset dataSource;
    public ScenarioManager sm;
    public ComputeShader brushShader;
    public Material brushMat;

    //IATK elements
    CSVDataSource csvdata;
    FlatPlot pickups = null;
    FlatPlot dropoffs = null;
    LinkingViews link;



    public class FlatPlot
    {
        public Visualisation visualization;
        public View view;
        public ViewBuilder viewBuilder;
        public float maxlongitude, maxlatitude, minlongitude, minlatitude;
        public float bestZoomLevel, meanlongitude, meanlatitude;
        public float mapLatToZ(float lat)
        {
                return (lat - minlatitude) / (maxlatitude - minlatitude);
        }

        public float mapLonToX(float lon)
        {
                return (lon - minlongitude) / (maxlongitude - minlongitude);
        }

        public float mapLatToZClamp(float lat)
        {
            if (lat >= maxlatitude)
                return 1;
            else if (lat <= minlatitude)
                return 0;
            else
                return (lat - minlatitude) / (maxlatitude - minlatitude);
        }

        public float mapLonToXClamp(float lon)
        {
            if (lon >= maxlongitude)
                return 1;
            else if (lon <= minlongitude)
                return 0;
            else
                return (lon - minlongitude) / (maxlongitude - minlongitude);
        }


    }


    [Space(10)]
    [Header("Data info")]
    public float maxlongitude;
    public float maxlatitude;
    public float minlongitude;
    public float minlatitude;
    public float meanlongitude;
    public float meanlatitude;
    public float bestZoomLevel;




    [Space(10)]
    [Header("Settings")]

    public bool ShowLinks = false;
    public bool diffPlanes = false;
    public bool UseBrushes = false;
    public bool UseBrushesOrigin = false;
    public bool UseBrushesDest = false;

    [Space(10)]
    [Header("Assets")]
    public Material IATKpointsMat;
    public Material IATKpointsMat2; 



    private string dateFormat;

    public void Load(TextAsset dataSource, string dateFormat)
    {
        this.dateFormat = dateFormat;
        Load(dataSource);
    }

    public void Load(ODSTCManager stc, string dateFormat)
    {
        this.dateFormat = dateFormat;
        Load(stc);
    }


    // Use this for initialization
    public void Load(TextAsset dataSource)
    {
        this.dataSource = dataSource;
            csvdata = createCSVDataSource(dataSource.text);

        if (sm.stc.timeDirection == STCManager.Direction.LatestOnBottom)
            csvdata.inverseTime = true;
        else
            csvdata.inverseTime = false;

        ExtractSpaceLimits(csvdata, "pickup_longitude", "pickup_latitude", "dropoff_longitude", "dropoff_latitude");

        sm.bingMap.mapRenderer.Center = new Microsoft.Geospatial.LatLon(meanlatitude, meanlongitude);
            sm.bingMap.mapRenderer.ZoomLevel = bestZoomLevel;


        if (pickups != null)
        {
            pickups.view.gameObject.SetActive(false);
            Destroy(pickups.view.gameObject);
            pickups.view = null;
        }
        if (dropoffs != null)
        {
            dropoffs.view.gameObject.SetActive(false);
            Destroy(dropoffs.view.gameObject);
            dropoffs.view = null;
        }
        if (link != null)
        {
            link.view.gameObject.SetActive(false);
            Destroy(link.view.gameObject);
            Destroy(link.view);
            link = null;
        }

        pickups = PlotLatLonData(csvdata, "Pickups-Space", "pickup_longitude", "pickup_latitude", Color.blue);
        dropoffs = PlotLatLonData(csvdata, "Dropoffs-Space", "dropoff_longitude", "dropoff_latitude", Color.red);

        if (diffPlanes)
                    pickups.view.transform.position += new Vector3(0f, .5f, 0f);

                // ADD LINK
                link = gameObject.AddComponent<LinkingViews>();
                link.visualisationSource = pickups.view;
               link.visualisationTarget = dropoffs.view;
                link.showLinks = ShowLinks;
                link.linkTransparency = .8f;
        link.LinkViews();

        if (ShowLinks)
        {
            link.view.SetMinNormX(dropoffs.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LongitudeInDegrees));
            link.view.SetMinNormZ(dropoffs.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LatitudeInDegrees));
            link.view.SetMaxNormX(dropoffs.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.TopRight.LongitudeInDegrees));
            link.view.SetMaxNormZ(dropoffs.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.TopRight.LatitudeInDegrees));
        }

        
    }




    public void Load(ODSTCManager stc)
    {
        this.dataSource = stc.dataSource;
        csvdata = stc.csvdata;

        minlatitude = stc.minlatitude;
        minlongitude = stc.minlongitude;
        maxlatitude = stc.maxlatitude;
        maxlongitude = stc.maxlongitude;
        meanlatitude = stc.meanlatitude;
        meanlongitude = stc.meanlongitude;
        bestZoomLevel = stc.bestZoomLevel;


        if (pickups != null)
        {
            pickups.view.gameObject.SetActive(false);
            Destroy(pickups.view.gameObject);
            pickups.view = null;
        }
        if (dropoffs != null)
        {
            dropoffs.view.gameObject.SetActive(false);
            Destroy(dropoffs.view.gameObject);
            dropoffs.view = null;
        }
        if (link != null)
        {
            link.view.gameObject.SetActive(false);
            Destroy(link.view.gameObject);
            Destroy(link.view);
            link = null;
        }

        pickups = ProjLatLonData(stc, stc.pickups, "Pickups-Space", "pickup_longitude", "pickup_latitude", Color.blue);
        dropoffs = ProjLatLonData(stc, stc.dropoffs, "Dropoffs-Space", "dropoff_longitude", "dropoff_latitude", Color.red);

        // ADD LINK
        link = gameObject.AddComponent<LinkingViews>();
        link.visualisationSource = pickups.view;
        link.visualisationTarget = dropoffs.view;
        link.showLinks = ShowLinks;
        link.linkTransparency = .8f;
        link.LinkViews();

        if (ShowLinks)
        {
            link.view.SetMinNormX(dropoffs.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LongitudeInDegrees));
            link.view.SetMinNormZ(dropoffs.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LatitudeInDegrees));
            link.view.SetMaxNormX(dropoffs.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.TopRight.LongitudeInDegrees));
            link.view.SetMaxNormZ(dropoffs.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.TopRight.LatitudeInDegrees));
        }
    }






    CSVDataSource createCSVDataSource(string data)
    {
        CSVDataSource dataSource;
        dataSource = gameObject.AddComponent<CSVDataSource>();
        dataSource.setDateFormat(dateFormat);
        dataSource.load(data, null);
        return dataSource;
    }

    public Vector3 Spherical(float r, float theta, float phi)
    {
        Vector3 pt = new Vector3();
        float snt = (float)Mathf.Sin(theta * Mathf.PI / 180);
        float cnt = (float)Mathf.Cos(theta * Mathf.PI / 180);
        float snp = (float)Mathf.Sin(phi * Mathf.PI / 180);
        float cnp = (float)Mathf.Cos(phi * Mathf.PI / 180);
        pt.x = r * snt * cnp;
        pt.y = r * cnt;
        pt.z = -r * snt * snp;
        return pt;
    }

    void ExtractSpaceLimits(CSVDataSource csvds, string lon_dim1 = "Lon", string lat_dim1 = "Lat", string? lon_dim2 = null, string? lat_dim2 = null)
    {
        float min_lon1 = float.Parse(csvds.getOriginalValue(csvds[lon_dim1].Data.Min(), lon_dim1) + "");
        float max_lon1 = float.Parse(csvds.getOriginalValue(csvds[lon_dim1].Data.Max(), lon_dim1) + "");
        float min_lat1 = float.Parse(csvds.getOriginalValue(csvds[lat_dim1].Data.Min(), lat_dim1) + "");
        float max_lat1 = float.Parse(csvds.getOriginalValue(csvds[lat_dim1].Data.Max(), lat_dim1) + "");

        if (lon_dim2 == null)
        {
            minlongitude = min_lon1;
            maxlongitude = max_lon1;
        }
        else
        {
            float min_lon2 = float.Parse(csvds.getOriginalValue(csvds[lon_dim2].Data.Min(), lon_dim2) + "");
            float max_lon2 = float.Parse(csvds.getOriginalValue(csvds[lon_dim2].Data.Max(), lon_dim2) + "");

            if (min_lon1 < min_lon2)
                minlongitude = min_lon1;
            else
                minlongitude = min_lon2;

            if (max_lon1 > max_lon2)
                maxlongitude = max_lon1;
            else
                maxlongitude = max_lon2;
        }

        if (lat_dim2 == null)
        {
            minlatitude = min_lat1;
            maxlatitude = max_lat1;
        }
        else
        {
            float min_lat2 = float.Parse(csvds.getOriginalValue(csvds[lat_dim2].Data.Min(), lat_dim2) + "");
            float max_lat2 = float.Parse(csvds.getOriginalValue(csvds[lat_dim2].Data.Max(), lat_dim2) + "");

            if (min_lat1 < min_lat2)
                minlatitude = min_lat1;
            else
                minlatitude = min_lat2;

            if (max_lat1 > max_lat2)
                maxlatitude = max_lat1;
            else
                maxlatitude = max_lat2;
        }


        meanlatitude = minlatitude + (maxlatitude - minlatitude) / 2f;
        meanlongitude = minlongitude + (maxlongitude - minlongitude) / 2f;

        bestZoomLevel = ComputeBestZoomLevel(minlatitude, maxlatitude, minlongitude, maxlongitude);

    }



    FlatPlot ProjLatLonData(ODSTCManager stc, ODSTCSubplot stcSP, string viewName = "View", string lon_dim = "Lon", string lat_dim = "Lat", Color? data_color = null)
    {

        FlatPlot fp = new FlatPlot();

        fp.maxlongitude = stcSP.maxlongitude; //float.Parse(csvds.getOriginalValue(csvds[lon_dim].Data.Max(), lon_dim) + "");
        fp.maxlatitude = stcSP.maxlatitude; //float.Parse(csvds.getOriginalValue(csvds[lat_dim].Data.Max(), lat_dim) + "");
        fp.minlongitude = stcSP.minlongitude;//float.Parse(csvds.getOriginalValue(csvds[lon_dim].Data.Min(), lon_dim) + "");
        fp.minlatitude = stcSP.minlatitude;//float.Parse(csvds.getOriginalValue(csvds[lat_dim].Data.Min(), lat_dim) + "");

       
        // TO DO : CONSIDER EASTERN HEMISPHERE

        // create a view builder with the point topology
        fp.viewBuilder = new ViewBuilder(MeshTopology.Points, "FlatPlotManager").
             initialiseDataView(stc.csvdata.DataCount).
             setDataDimension(stc.csvdata[lon_dim].Data, ViewBuilder.VIEW_DIMENSION.X).
             setDataDimension(stc.csvdata[lat_dim].Data, ViewBuilder.VIEW_DIMENSION.Z).
             setSingleColor(data_color ?? Color.cyan); 
        fp.viewBuilder = fp.viewBuilder.createIndicesPointTopology();

        // initialise the view builder wiith thhe number of data points and parent GameOBject

        Material mt;
        mt = Instantiate(IATKpointsMat);

        mt.SetFloat("Size", 0.1f);
        mt.SetFloat("_MinSize", 0f);
        mt.SetFloat("_MaxSize", 1f);

        fp.view = fp.viewBuilder.updateView().apply(gameObject, mt, viewName);

        fp.view.SetSize(.1f);




        // TO DO: MOVE TO SCENARIOMANAGER
        if (sm.bingMap.transform.localScale.y != 1f)
            transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.bingMap.transform.position.y + 0.085f, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        else
            transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.bingMap.transform.position.y + 0.05f, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        transform.localScale = new Vector3(sm.bingMap.mapRenderer.MapDimension.x, transform.localScale.y, sm.bingMap.mapRenderer.MapDimension.y);
        //

        fp.view.SetMinNormX(fp.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LongitudeInDegrees));
        fp.view.SetMinNormZ(fp.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LatitudeInDegrees));
        fp.view.SetMaxNormX(fp.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.TopRight.LongitudeInDegrees));
        fp.view.SetMaxNormZ(fp.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.TopRight.LatitudeInDegrees));




        return fp;
    }




    FlatPlot PlotLatLonData(CSVDataSource csvds, string viewName = "View", string lon_dim = "Lon", string lat_dim = "Lat", Color? data_color = null)
    {
        // header
        // Date,Time,Lat,Lon,Base
        //Gradient g = new Gradient();
        //GradientColorKey[] gck = new GradientColorKey[2];
        //gck[0] = new GradientColorKey(Color.blue, 0);
        //gck[1] = new GradientColorKey(Color.red, 1);
        //g.colorKeys = gck;


        FlatPlot fp = new FlatPlot();
        
            fp.maxlongitude = float.Parse(csvds.getOriginalValue(csvds[lon_dim].Data.Max(), lon_dim) + "");
            fp.maxlatitude = float.Parse(csvds.getOriginalValue(csvds[lat_dim].Data.Max(), lat_dim) + "");
            fp.minlongitude = float.Parse(csvds.getOriginalValue(csvds[lon_dim].Data.Min(), lon_dim) + "");
            fp.minlatitude = float.Parse(csvds.getOriginalValue(csvds[lat_dim].Data.Min(), lat_dim) + "");

            Vector2 centerMap = new Vector2(fp.minlatitude + (fp.maxlatitude - fp.minlatitude) / 2.0f, fp.minlongitude + (fp.maxlongitude - fp.minlongitude) / 2.0f);

            Debug.Log("Min lat: " + fp.minlatitude.ToString() + " / Max lat: " + fp.maxlatitude.ToString() + " / Min lon: " + fp.minlongitude.ToString() + " / Max lon: " + fp.maxlongitude.ToString() + " / Center lat: " + centerMap.y.ToString() + " / Center lon: " + centerMap.x.ToString());

            //if(sm.UseBingMaps)
            // {

            //if (predefinedZoom == null)
            //    sm.bingMap.Center = new Microsoft.Geospatial.LatLon(centerMap.x, centerMap.y);
            //sm.bingMap.ZoomLevel = predefinedZoom ?? ComputeBestZoomLevel(fp.minlatitude, fp.maxlatitude, fp.minlongitude, fp.maxlongitude);



        //Vector3 topLeft = sm.bingMap.TransformLatLonAltToWorldPoint(new Microsoft.Geospatial.LatLonAlt(maxlatitude, minlongitude, 0f));
        //Vector3 bottomRight = sm.bingMap.TransformLatLonAltToWorldPoint(new Microsoft.Geospatial.LatLonAlt(minlatitude, maxlongitude, 0f));
        //Vector3 bottomLeft = sm.bingMap.TransformLatLonAltToWorldPoint(new Microsoft.Geospatial.LatLonAlt(minlatitude, minlongitude, 0f));
        // TO DO : CONSIDER EASTERN HEMISPHERE

        // create a view builder with the point topology
               fp.viewBuilder = new ViewBuilder(MeshTopology.Points, "FlatPlotManager").
                    initialiseDataView(csvds.DataCount).
                    setDataDimension(csvds[lon_dim].Data, ViewBuilder.VIEW_DIMENSION.X).
                    setDataDimension(csvds[lat_dim].Data, ViewBuilder.VIEW_DIMENSION.Z).setSingleColor(data_color ?? Color.cyan); //.setColors(colours.ToArray()); //.setSingleColor(Color.cyan);//.
                                                                                                                                  //setSize(csvds["Base"].Data).
        fp.viewBuilder = fp.viewBuilder.createIndicesPointTopology();
        //setColors(csvds["Time"].Data.Select(x => g.Evaluate(x)).ToArray());

        // initialise the view builder wiith thhe number of data points and parent GameOBject

        //Enumerable.Repeat(1f, dataSource[0].Data.Length).ToArray()
        Material mt;

        //if (viewName == "Pickups-Space")
        //    mt = IATKpointsMat; //IATKUtil.GetMaterialFromTopology(AbstractVisualisation.GeometryType.Points2D);
        //else
        //    mt = IATKpointsMat2;

        mt = Instantiate(IATKpointsMat);

        //mt.SetColor(Shader.PropertyToID("_Color"), Color.cyan);
        mt.SetFloat("Size", 0.1f);
        //mt.SetFloat("_MinSize", 0.1f);
        //mt.SetFloat("_MaxSize", 0.1f);
        mt.SetFloat("_MinSize", 0f);
        mt.SetFloat("_MaxSize", 1f);

        fp.view = fp.viewBuilder.updateView().apply(gameObject, mt, viewName);

                fp.view.SetSize(.1f);
        //fp.view.SetBlendingSourceMode(1f);
        //fp.view.SetBlendindDestinationMode(1f);


        //v.transform.position = new Vector3(bottomLeft.x, this.transform.position.y, bottomLeft.z);
        //v.transform.localScale = new Vector3(width, 1f, depth);


        ///Visualisation vis = new Visualisation();
        ///vis.dataSource = csvds;






        // TO DO: MOVE TO SCENARIOMANAGER
        if (sm.bingMap.transform.localScale.y != 1f)
            transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.bingMap.transform.position.y + 0.085f, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        else
            transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.bingMap.transform.position.y + 0.05f, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        transform.localScale = new Vector3(sm.bingMap.mapRenderer.MapDimension.x, transform.localScale.y, sm.bingMap.mapRenderer.MapDimension.y);
        //

        fp.view.SetMinNormX(fp.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LongitudeInDegrees));
        fp.view.SetMinNormZ(fp.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LatitudeInDegrees));
        fp.view.SetMaxNormX(fp.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.TopRight.LongitudeInDegrees));
        fp.view.SetMaxNormZ(fp.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.TopRight.LatitudeInDegrees));




        return fp;
    }



    private float distance(float x1, float y1, float x2, float y2)
    {
        return Mathf.Sqrt(Mathf.Pow(x2 - x1, 2) + Mathf.Pow(y2 - y1, 2));
    }

    private float ComputeBestZoomLevel(float minlatitude, float maxlatitude, float minlongitude, float maxlongitude)
    {
        Mercator mProj = new Mercator();

        float[] topLeft = mProj.latLonToMeters(minlatitude, minlongitude);
        float[] topright = mProj.latLonToMeters(minlatitude, maxlongitude);
        float[] bottomLeft = mProj.latLonToMeters(maxlatitude, minlongitude);
        float[] bottomRight = mProj.latLonToMeters(maxlatitude, maxlongitude);


        float leftRightDistance = this.distance(topLeft[0], topLeft[1], topright[0], topright[1]);
        float topBottomDistance = this.distance(topLeft[0], topLeft[1], bottomLeft[0], bottomLeft[1]);

        float maxdist = Mathf.Max(leftRightDistance, topBottomDistance);

        float pixelDist = 3 * 256;

        int lastgoodZoom = 0;

        for (int i = 0; i < 17; i++)
        {
            float realSize = 256 * (maxdist / 40000000) * Mathf.Pow(2, i);
            if (realSize < pixelDist)
            {
                lastgoodZoom = i;
            }
        }
        //Debug.Log("Appropriate Zoom level: " + lastgoodZoom);

        return lastgoodZoom;
    }



    // Update is called once per frame
    void Update()
    {

            if (link != null && link.showLinks != ShowLinks)
                link.showLinks = ShowLinks;
            
    }


    public void UpdatePlotsXZNormalizedRanges()
    {
        // SHOULD ONLY REFRESH ON CHANGES - TRANSFER TO SCENARIOMANAGER
        if (sm.bingMap.transform.localScale.y != 1f)
            transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.bingMap.transform.position.y + 0.085f, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        else
            transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.bingMap.transform.position.y + 0.05f, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        transform.localScale = new Vector3(sm.bingMap.mapRenderer.MapDimension.x, transform.localScale.y, sm.bingMap.mapRenderer.MapDimension.y);
        //

        if(pickups != null)
        {
            pickups.view.SetMinNormX(pickups.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LongitudeInDegrees));
            pickups.view.SetMinNormZ(pickups.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LatitudeInDegrees));
            pickups.view.SetMaxNormX(pickups.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.TopRight.LongitudeInDegrees));
            pickups.view.SetMaxNormZ(pickups.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.TopRight.LatitudeInDegrees));
        }

        if (dropoffs != null)
        {
            dropoffs.view.SetMinNormX(dropoffs.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LongitudeInDegrees));
            dropoffs.view.SetMinNormZ(dropoffs.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LatitudeInDegrees));
            dropoffs.view.SetMaxNormX(dropoffs.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.TopRight.LongitudeInDegrees));
            dropoffs.view.SetMaxNormZ(dropoffs.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.TopRight.LatitudeInDegrees));
        }

        if (link != null && link.view != null && link.showLinks)
        {
            link.view.SetMinNormX(dropoffs.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LongitudeInDegrees));
            link.view.SetMinNormZ(dropoffs.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LatitudeInDegrees));
            link.view.SetMaxNormX(dropoffs.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.TopRight.LongitudeInDegrees));
            link.view.SetMaxNormZ(dropoffs.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.TopRight.LatitudeInDegrees));
        }

    }


    public void UpdateGlyphSizeOrTrajWidth()
    {
        

        float zoom = sm.bingMap.mapRenderer.ZoomLevel;

        float newSize;

        //if (zoom < 9)
        //    newSize = sm.pointSizeMultiplier * 0.01f;
        //else if (zoom < 10)
        //    newSize = sm.pointSizeMultiplier * 0.02f;
        //else if (zoom < 11)
        //    newSize = sm.pointSizeMultiplier * 0.025f;

        if (zoom < 11)
            newSize = sm.pointSizeMultiplier * 0.05f;// 0.025f;
        else
            newSize = sm.pointSizeMultiplier * Mathf.Max(0.01f, 0.00132576f * Mathf.Pow(zoom, 4) - 0.0721801f * Mathf.Pow(zoom, 3) + 1.45966f * Mathf.Pow(zoom, 2) - 12.9653f * zoom + 42.7154f);
        //https://www.wolframalpha.com/input?i=polynomial+fit+%7B%7B11%2C+0.05%7D%2C+%7B12%2C+0.1%7D%2C+%7B13%2C+0.125%7D%2C+%7B14%2C+0.15%7D%2C+%7B15%2C+0.175%7D%2C+%7B16%2C+0.2%7D%2C+%7B17%2C+0.225%7D%2C+%7B18%2C+0.5%7D%2C+%7B19%2C+1%7D%7D

        if (pickups != null)
        {
            pickups.view.SetSize(newSize);
        }

        if (dropoffs != null)
        {
            dropoffs.view.SetSize(newSize);
        }

        /*
        float newSize = 0.01f;

        if (zoom >= 19)
            newSize = 1f;
        else if (zoom >= 18)
            newSize = 0.5f;
        else if (zoom >= 17)
            newSize = 0.225f;
        else if (zoom >= 16)
            newSize = 0.2f;
        else if (zoom >= 15)
            newSize = 0.175f;
        else if (zoom >= 14)
            newSize = 0.15f;
        else if (zoom >= 13)
            newSize = 0.125f;
        else if (zoom >= 12)
            newSize = 0.1f;
        else if (zoom >= 11)
            newSize = 0.05f;
        else if (zoom >= 10)
            newSize = 0.025f;
        else if (zoom >= 9)
            newSize = 0.02f;*/



    }
    


}
