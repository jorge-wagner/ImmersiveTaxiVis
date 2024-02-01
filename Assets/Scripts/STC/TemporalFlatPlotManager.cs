using IATK;
using Microsoft.Maps.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ODSTCManager;

public class TemporalFlatPlotManager : MonoBehaviour
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
        public DateTime minTime, maxTime;
        public float minYValue, maxYValue;
        public Color viewODColor;

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

        public float mapTimeToY(DateTime time)
        {
            return minYValue + mapTimeToNormY(time) * (maxYValue - minYValue);
        }

        public float mapTimeToNormY(DateTime time)
        {
            return ((float)time.Ticks - (float)this.minTime.Ticks) / ((float)this.maxTime.Ticks - (float)this.minTime.Ticks);
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
    public DateTime minTime, maxTime, onTop;
    public float secondsPerMeter;



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
    public Material IATKpointsMat2; // should duplicate material in code



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

        ExtractSpaceLimits(csvdata, "pickup_longitude", "pickup_latitude", "dropoff_longitude", "dropoff_latitude");
        ExtractTimeLimits(csvdata, "pickup_datetime", "dropoff_datetime");

        //sm.bingMap.mapRenderer.Center = new Microsoft.Geospatial.LatLon(meanlatitude, meanlongitude);
        //sm.bingMap.mapRenderer.ZoomLevel = bestZoomLevel;

        pickups = PlotLatTimeData(csvdata, "Pickups-Time", "pickup_latitude", "pickup_datetime", Color.blue);
        dropoffs = PlotLatTimeData(csvdata, "Dropoffs-Time", "dropoff_latitude", "dropoff_datetime", Color.red);

        // ADD LINK
        link = gameObject.AddComponent<LinkingViews>();
        link.visualisationSource = pickups.view;
        link.visualisationTarget = dropoffs.view;
        link.showLinks = ShowLinks;
        link.linkTransparency = .8f;
        link.LinkViews();        
    }


    public void Load(ODSTCManager stc)
    {
        this.dataSource = stc.dataSource;
        csvdata = stc.csvdata; // createCSVDataSource(dataSource.text);

        minlatitude = stc.minlatitude;
        minlongitude = stc.minlongitude;
        maxlatitude = stc.maxlatitude;
        maxlongitude = stc.maxlongitude;
        meanlatitude = stc.meanlatitude;
        meanlongitude = stc.meanlongitude;
        bestZoomLevel = stc.bestZoomLevel;

        //ExtractSpaceLimits(csvdata, "pickup_longitude", "pickup_latitude", "dropoff_longitude", "dropoff_latitude");
        //ExtractTimeLimits(csvdata, "pickup_datetime", "dropoff_datetime");

        //sm.bingMap.mapRenderer.Center = new Microsoft.Geospatial.LatLon(meanlatitude, meanlongitude);
        //sm.bingMap.mapRenderer.ZoomLevel = bestZoomLevel;

        pickups = ProjLatTimeData(stc, stc.pickups, "Pickups-Time", "pickup_latitude", "pickup_datetime", Color.blue);
        dropoffs = ProjLatTimeData(stc, stc.dropoffs, "Dropoffs-Time", "dropoff_latitude", "dropoff_datetime", Color.red);

        // ADD LINK
        link = gameObject.AddComponent<LinkingViews>();
        link.visualisationSource = pickups.view;
        link.visualisationTarget = dropoffs.view;
        link.showLinks = ShowLinks;
        link.linkTransparency = .8f;
        link.LinkViews();
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

    private void ExtractTimeLimits(CSVDataSource csvds, string time_dim1 = "Time", string? time_dim2 = null)
    {
        DateTime min1, max1, min2, max2;

        if (sm.stc.timeDirection == STCManager.Direction.LatestOnTop)
        {
            min1 = (DateTime)csvds.getOriginalValue(csvds[time_dim1].Data.Min(), time_dim1);
            max1 = (DateTime)csvds.getOriginalValue(csvds[time_dim1].Data.Max(), time_dim1);
        }
        else
        {
            min1 = (DateTime)csvds.getOriginalValue(csvds[time_dim1].Data.Max(), time_dim1);
            max1 = (DateTime)csvds.getOriginalValue(csvds[time_dim1].Data.Min(), time_dim1);
        }

        if (time_dim2 == null)
        {
            minTime = min1;
            maxTime = max1;
        }
        else
        {
            if (sm.stc.timeDirection == STCManager.Direction.LatestOnTop)
            {
                min2 = (DateTime)csvds.getOriginalValue(csvds[time_dim2].Data.Min(), time_dim2);
                max2 = (DateTime)csvds.getOriginalValue(csvds[time_dim2].Data.Max(), time_dim2);
            }
            else
            {
                min2 = (DateTime)csvds.getOriginalValue(csvds[time_dim2].Data.Max(), time_dim2);
                max2 = (DateTime)csvds.getOriginalValue(csvds[time_dim2].Data.Min(), time_dim2);
            }

            if (min1.CompareTo(min2) < 0)
                minTime = min1;
            else
                minTime = min2;

            if (max1.CompareTo(max2) > 0)
                maxTime = max1;
            else
                maxTime = max2;
        }

        if (sm.stc.timeDirection == STCManager.Direction.LatestOnTop)
            onTop = maxTime;
        else
            onTop = minTime;

        secondsPerMeter = (float)maxTime.Subtract(minTime).TotalSeconds / this.transform.localScale.y;

        Debug.Log("MinTime = " + minTime.ToString() + " , MaxTime = " + maxTime.ToString() + " , sPm = " + secondsPerMeter);
    }


    FlatPlot PlotLatLonData(CSVDataSource csvds, string viewName = "View", string lon_dim = "Lon", string lat_dim = "Lat", Color? data_color = null)
    {
        FlatPlot fp = new FlatPlot();
        
            fp.maxlongitude = float.Parse(csvds.getOriginalValue(csvds[lon_dim].Data.Max(), lon_dim) + "");
            fp.maxlatitude = float.Parse(csvds.getOriginalValue(csvds[lat_dim].Data.Max(), lat_dim) + "");
            fp.minlongitude = float.Parse(csvds.getOriginalValue(csvds[lon_dim].Data.Min(), lon_dim) + "");
            fp.minlatitude = float.Parse(csvds.getOriginalValue(csvds[lat_dim].Data.Min(), lat_dim) + "");

            Vector2 centerMap = new Vector2(fp.minlatitude + (fp.maxlatitude - fp.minlatitude) / 2.0f, fp.minlongitude + (fp.maxlongitude - fp.minlongitude) / 2.0f);

            Debug.Log("Min lat: " + fp.minlatitude.ToString() + " / Max lat: " + fp.maxlatitude.ToString() + " / Min lon: " + fp.minlongitude.ToString() + " / Max lon: " + fp.maxlongitude.ToString() + " / Center lat: " + centerMap.y.ToString() + " / Center lon: " + centerMap.x.ToString());


        // create a view builder with the point topology
               fp.viewBuilder = new ViewBuilder(MeshTopology.Points, "TemporalFlatPlotManager").
                    initialiseDataView(csvds.DataCount).
                    setDataDimension(csvds[lon_dim].Data, ViewBuilder.VIEW_DIMENSION.X).
                    setDataDimension(csvds[lat_dim].Data, ViewBuilder.VIEW_DIMENSION.Z).setSingleColor(data_color ?? Color.cyan); //.setColors(colours.ToArray()); //.setSingleColor(Color.cyan);//.
                                                                                                                                  //setSize(csvds["Base"].Data).
        fp.viewBuilder = fp.viewBuilder.createIndicesPointTopology();

        Material mt;

        if (viewName == "Pickups")
            mt = IATKpointsMat;
        else
            mt = IATKpointsMat2;

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

    FlatPlot PlotLatTimeData(CSVDataSource csvds, string viewName = "View", string lat_dim = "Lat", string time_dim = "Time", Color? data_color = null)
    {
        FlatPlot fp = new FlatPlot();

        //fp.maxlongitude = float.Parse(csvds.getOriginalValue(csvds[lon_dim].Data.Max(), lon_dim) + "");
        fp.maxlatitude = float.Parse(csvds.getOriginalValue(csvds[lat_dim].Data.Max(), lat_dim) + "");
        //fp.minlongitude = float.Parse(csvds.getOriginalValue(csvds[lon_dim].Data.Min(), lon_dim) + "");
        fp.minlatitude = float.Parse(csvds.getOriginalValue(csvds[lat_dim].Data.Min(), lat_dim) + "");

        fp.minYValue = csvds[time_dim].MetaData.minValue;
        fp.maxYValue = csvds[time_dim].MetaData.maxValue;
        if (sm.stc.timeDirection == STCManager.Direction.LatestOnBottom)
        {
            fp.minTime = (DateTime)csvds.getOriginalValue(csvds[time_dim].Data.Max(), time_dim);
            fp.maxTime = (DateTime)csvds.getOriginalValue(csvds[time_dim].Data.Min(), time_dim);
        }
        else
        {
            fp.minTime = (DateTime)csvds.getOriginalValue(csvds[time_dim].Data.Min(), time_dim);
            fp.maxTime = (DateTime)csvds.getOriginalValue(csvds[time_dim].Data.Max(), time_dim);
        }


        //Vector2 centerMap = new Vector2(fp.minlatitude + (fp.maxlatitude - fp.minlatitude) / 2.0f, fp.minlongitude + (fp.maxlongitude - fp.minlongitude) / 2.0f);

        //Debug.Log("Min lat: " + fp.minlatitude.ToString() + " / Max lat: " + fp.maxlatitude.ToString() + " / Min lon: " + fp.minlongitude.ToString() + " / Max lon: " + fp.maxlongitude.ToString() + " / Center lat: " + centerMap.y.ToString() + " / Center lon: " + centerMap.x.ToString());


        // create a view builder with the point topology
        fp.viewBuilder = new ViewBuilder(MeshTopology.Points, "TemporalFlatPlotManager").
             initialiseDataView(csvds.DataCount).
             setDataDimension(csvds[time_dim].Data, ViewBuilder.VIEW_DIMENSION.Y).
             setDataDimension(csvds[lat_dim].Data, ViewBuilder.VIEW_DIMENSION.Z).setSingleColor(data_color ?? Color.cyan);
    
        fp.viewBuilder = fp.viewBuilder.createIndicesPointTopology();

        Material mt;

        if (viewName == "Pickups-Time")
            mt = IATKpointsMat;
        else
            mt = IATKpointsMat2;

        mt.SetFloat("Size", 0.1f);
        mt.SetFloat("_MinSize", 0f);
        mt.SetFloat("_MaxSize", 1f);

        fp.view = fp.viewBuilder.updateView().apply(gameObject, mt, viewName);

        fp.view.SetSize(.1f);






        // TO DO: MOVE TO SCENARIOMANAGER
        //if (sm.bingMap.transform.localScale.y != 1f)
        //    transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.bingMap.transform.position.y + 0.085f, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        //else
        //    transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.bingMap.transform.position.y + 0.05f, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        //transform.localScale = new Vector3(sm.bingMap.mapRenderer.MapDimension.x, transform.localScale.y, sm.bingMap.mapRenderer.MapDimension.y);
        //

        // SHOULD ONLY REFRESH ON CHANGES - TRANSFER TO SCENARIOMANAGER
        if (sm.bingMap.transform.localScale.y != 1f)
            transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.stc.transform.position.y, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        else
            transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.stc.transform.position.y, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        transform.localScale = new Vector3(transform.localScale.x, sm.stc.transform.localScale.y, sm.bingMap.mapRenderer.MapDimension.y);
        //


        //fp.view.SetMinNormX(fp.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LongitudeInDegrees));
        fp.view.SetMinNormZ(fp.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LatitudeInDegrees));
        //fp.view.SetMaxNormX(fp.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.TopRight.LongitudeInDegrees));
        fp.view.SetMaxNormZ(fp.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.TopRight.LatitudeInDegrees));

        if (sm.stc.timeDirection == STCManager.Direction.LatestOnTop)
        {
            fp.view.SetMinNormY(fp.mapTimeToNormY(minTime));
            fp.view.SetMaxNormY(fp.mapTimeToNormY(maxTime));
        }
        else
        {
            fp.view.SetMinNormY(fp.mapTimeToNormY(maxTime));
            fp.view.SetMaxNormY(fp.mapTimeToNormY(minTime));
        }


        return fp;
    }

    FlatPlot ProjLatTimeData(ODSTCManager stc, ODSTCSubplot stcSP, string viewName = "View", string lat_dim = "Lat", string time_dim = "Time", Color? data_color = null)
    {
        FlatPlot fp = new FlatPlot();

        //fp.maxlongitude = float.Parse(csvds.getOriginalValue(csvds[lon_dim].Data.Max(), lon_dim) + "");
        fp.maxlatitude = stcSP.maxlatitude; //float.Parse(csvds.getOriginalValue(csvds[lat_dim].Data.Max(), lat_dim) + "");
        //fp.minlongitude = float.Parse(csvds.getOriginalValue(csvds[lon_dim].Data.Min(), lon_dim) + "");
        fp.minlatitude = stcSP.minlatitude; //float.Parse(csvds.getOriginalValue(csvds[lat_dim].Data.Min(), lat_dim) + "");

        fp.minYValue = stcSP.minYValue; //csvds[time_dim].MetaData.minValue;
        fp.maxYValue = stcSP.maxYValue; //csvds[time_dim].MetaData.maxValue;
        //if (sm.stc.timeDirection == STCManager.Direction.LatestOnBottom)
        //{
            fp.minTime = stcSP.minTime;//(DateTime)csvds.getOriginalValue(csvds[time_dim].Data.Max(), time_dim);
        fp.maxTime = stcSP.maxTime;//(DateTime)csvds.getOriginalValue(csvds[time_dim].Data.Min(), time_dim);
                                        //}
                                        // else
                                        //{
                                        //   fp.minTime = (DateTime)csvds.getOriginalValue(csvds[time_dim].Data.Min(), time_dim);
                                        //   fp.maxTime = (DateTime)csvds.getOriginalValue(csvds[time_dim].Data.Max(), time_dim);
                                        // }


        //Vector2 centerMap = new Vector2(fp.minlatitude + (fp.maxlatitude - fp.minlatitude) / 2.0f, fp.minlongitude + (fp.maxlongitude - fp.minlongitude) / 2.0f);

        //Debug.Log("Min lat: " + fp.minlatitude.ToString() + " / Max lat: " + fp.maxlatitude.ToString() + " / Min lon: " + fp.minlongitude.ToString() + " / Max lon: " + fp.maxlongitude.ToString() + " / Center lat: " + centerMap.y.ToString() + " / Center lon: " + centerMap.x.ToString());


        // create a view builder with the point topology
        fp.viewBuilder = new ViewBuilder(MeshTopology.Points, "TemporalFlatPlotManager").
             initialiseDataView(csvdata.DataCount).
             setDataDimension(csvdata[time_dim].Data, ViewBuilder.VIEW_DIMENSION.Y).
             setDataDimension(csvdata[lat_dim].Data, ViewBuilder.VIEW_DIMENSION.Z).setSingleColor(data_color ?? Color.cyan);

        fp.viewBuilder = fp.viewBuilder.createIndicesPointTopology();

        Material mt;

        if (viewName == "Pickups-Time")
            mt = IATKpointsMat;
        else
            mt = IATKpointsMat2;

        mt.SetFloat("Size", 0.1f);
        mt.SetFloat("_MinSize", 0f);
        mt.SetFloat("_MaxSize", 1f);

        fp.view = fp.viewBuilder.updateView().apply(gameObject, mt, viewName);
        //fp.view = fp.viewBuilder.applyCopy(gameObject, stcSP.view, mt, viewName);

        fp.view.SetSize(.1f);






        // TO DO: MOVE TO SCENARIOMANAGER
        //if (sm.bingMap.transform.localScale.y != 1f)
        //    transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.bingMap.transform.position.y + 0.085f, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        //else
        //    transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.bingMap.transform.position.y + 0.05f, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        //transform.localScale = new Vector3(sm.bingMap.mapRenderer.MapDimension.x, transform.localScale.y, sm.bingMap.mapRenderer.MapDimension.y);
        //

        // SHOULD ONLY REFRESH ON CHANGES - TRANSFER TO SCENARIOMANAGER
        if (sm.bingMap.transform.localScale.y != 1f)
            transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.stc.transform.position.y, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        else
            transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.stc.transform.position.y, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        transform.localScale = new Vector3(transform.localScale.x, sm.stc.transform.localScale.y, sm.bingMap.mapRenderer.MapDimension.y);
        //


        //fp.view.SetMinNormX(fp.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LongitudeInDegrees));
        fp.view.SetMinNormZ(fp.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LatitudeInDegrees));
        //fp.view.SetMaxNormX(fp.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.TopRight.LongitudeInDegrees));
        fp.view.SetMaxNormZ(fp.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.TopRight.LatitudeInDegrees));

        if (sm.stc.timeDirection == STCManager.Direction.LatestOnTop)
        {
            fp.view.SetMinNormY(fp.mapTimeToNormY(minTime));
            fp.view.SetMaxNormY(fp.mapTimeToNormY(maxTime));
        }
        else
        {
            fp.view.SetMinNormY(fp.mapTimeToNormY(maxTime));
            fp.view.SetMaxNormY(fp.mapTimeToNormY(minTime));
        }


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


    public void UpdatePlotsYZNormalizedRanges()
    {
        // SHOULD ONLY REFRESH ON CHANGES - TRANSFER TO SCENARIOMANAGER
        if (sm.bingMap.transform.localScale.y != 1f)
            transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.stc.transform.position.y, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        else
            transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.stc.transform.position.y, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        transform.localScale = new Vector3(transform.localScale.x, sm.stc.transform.localScale.y, sm.bingMap.mapRenderer.MapDimension.y);
        //

        if(pickups != null)
        {
            //pickups.view.SetMinNormX(pickups.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LongitudeInDegrees));
            pickups.view.SetMinNormZ(pickups.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LatitudeInDegrees));
            //pickups.view.SetMaxNormX(pickups.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.TopRight.LongitudeInDegrees));
            pickups.view.SetMaxNormZ(pickups.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.TopRight.LatitudeInDegrees));
        }

        if (dropoffs != null)
        {
            //dropoffs.view.SetMinNormX(dropoffs.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LongitudeInDegrees));
            dropoffs.view.SetMinNormZ(dropoffs.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LatitudeInDegrees));
            //dropoffs.view.SetMaxNormX(dropoffs.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.TopRight.LongitudeInDegrees));
            dropoffs.view.SetMaxNormZ(dropoffs.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.TopRight.LatitudeInDegrees));
        }

        if (link != null && link.view != null && link.showLinks)
        {
            //link.view.SetMinNormX(dropoffs.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LongitudeInDegrees));
            link.view.SetMinNormZ(dropoffs.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LatitudeInDegrees));
            //link.view.SetMaxNormX(dropoffs.mapLonToX((float)sm.bingMap.mapRenderer.Bounds.TopRight.LongitudeInDegrees));
            link.view.SetMaxNormZ(dropoffs.mapLatToZ((float)sm.bingMap.mapRenderer.Bounds.TopRight.LatitudeInDegrees));
        }

        if (sm.stc.timeDirection == STCManager.Direction.LatestOnTop)
        {
            if (pickups != null)
            {
                pickups.view.SetMinY(dropoffs.mapTimeToNormY(sm.stc.minTime));
                pickups.view.SetMaxY(dropoffs.mapTimeToNormY(sm.stc.maxTime));
            }

            if (dropoffs != null)
            {
                dropoffs.view.SetMinY(dropoffs.mapTimeToNormY(sm.stc.minTime));
                dropoffs.view.SetMaxY(dropoffs.mapTimeToNormY(sm.stc.maxTime));
            }

            if (link != null && link.view != null && link.showLinks && dropoffs != null)
            {
                link.view.SetMinY(dropoffs.mapTimeToY(sm.stc.minTime));
                link.view.SetMaxY(dropoffs.mapTimeToY(sm.stc.maxTime));
            }
        }
        else
        {
            if (pickups != null)
            {
                pickups.view.SetMinY(pickups.mapTimeToY(sm.stc.maxTime));
                pickups.view.SetMaxY(pickups.mapTimeToY(sm.stc.minTime));
            }

            if (dropoffs != null)
            {
                dropoffs.view.SetMinY(dropoffs.mapTimeToY(sm.stc.maxTime));
                dropoffs.view.SetMaxY(dropoffs.mapTimeToY(sm.stc.minTime));
            }
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
        //   newSize = sm.pointSizeMultiplier * 0.025f;

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
    }



}
