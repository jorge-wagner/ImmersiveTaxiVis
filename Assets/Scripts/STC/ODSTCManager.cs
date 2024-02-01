using IATK;
using Microsoft.Maps.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;









public class ODSTCManager : STCManager
{

    public ODSTCSubplot pickups = null;
    public ODSTCSubplot dropoffs = null;
   
    public Material iatkSphereMat;
   

    public class ODSTCSubplot
    {
        public Visualisation visualization;
        public View view;
        public ViewBuilder viewBuilder;
        public float maxlongitude, maxlatitude, minlongitude, minlatitude;
        public DateTime minTime, maxTime;
        public float minYValue, maxYValue;
        public float bestZoomLevel, meanlongitude, meanlatitude;
        public Color viewODColor; 

        public float mapLatToNormZ(float lat)
        {
            return (lat - minlatitude) / (maxlatitude - minlatitude);
        }

        public float mapLonToNormX(float lon)
        {
            return (lon - minlongitude) / (maxlongitude - minlongitude);
        }

        public float mapTimeToNormY(DateTime time)
        {
            return ((float)time.Ticks - (float)this.minTime.Ticks) / ((float)this.maxTime.Ticks - (float)this.minTime.Ticks);
        }

        public float mapTimeToY(DateTime time)
        {
            return minYValue + mapTimeToNormY(time) * (maxYValue - minYValue);
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
    
    private string dateFormat;

    public override void Load(TextAsset dataSource, string dateFormat)
    {
        this.dateFormat = dateFormat;
        Load(dataSource);
    }

    // Use this for initialization
    public override void Load(TextAsset dataSource)
    {
        
        this.dataSource = dataSource;

        csvdata = createCSVDataSource(dataSource.text);

        if (timeDirection == Direction.LatestOnBottom)
            csvdata.inverseTime = true;
        else
            csvdata.inverseTime = false;

        sm.stc = this;


        GenerateAttributeDictionaires();

        ExtractTimeLimits(csvdata, "pickup_datetime", "dropoff_datetime");
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
        if(link != null)
        {
            link.view.gameObject.SetActive(false);
            Destroy(link.view.gameObject);
            Destroy(link.view);
            link = null;
        }

        //pickups = PlotLatLonTimeData(csvdata, "STC-Pickups", "Pickup_longitude", "Pickup_latitude", "lpep_pickup_datetime", Color.blue);
        //dropoffs = PlotLatLonTimeData(csvdata, "STC-Dropoffs", "Dropoff_longitude", "Dropoff_latitude", "Lpep_dropoff_datetime", Color.red);
        pickups = PlotLatLonTimeData(csvdata, "STC-Pickups", "pickup_longitude", "pickup_latitude", "pickup_datetime", Color.blue);
        dropoffs = PlotLatLonTimeData(csvdata, "STC-Dropoffs", "dropoff_longitude", "dropoff_latitude", "dropoff_datetime", Color.red);

        //ReloadViewsWithUpdatedColors();


        // ADD LINKING VIEW
        link = gameObject.AddComponent<LinkingViews>();
        link.visualisationSource = pickups.view;
        link.visualisationTarget = dropoffs.view;
        link.showLinks = ShowLinks;
        link.linkTransparency = .8f;
        link.LinkViews();

        if (ShowLinks)
        {
            link.view.SetMinNormX(dropoffs.mapLonToNormX((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LongitudeInDegrees));
            link.view.SetMinNormZ(dropoffs.mapLatToNormZ((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LatitudeInDegrees));
            link.view.SetMaxNormX(dropoffs.mapLonToNormX((float)sm.bingMap.mapRenderer.Bounds.TopRight.LongitudeInDegrees));
            link.view.SetMaxNormZ(dropoffs.mapLatToNormZ((float)sm.bingMap.mapRenderer.Bounds.TopRight.LatitudeInDegrees));
        }
    }
    

    public override CSVDataSource createCSVDataSource(string data)
    {
        CSVDataSource dataSource;
        dataSource = gameObject.AddComponent<CSVDataSource>();
        dataSource.setDateFormat(dateFormat);
        dataSource.load(data, null);
        return dataSource;
    }


    public Dictionary<int, float> tripDistances;
    public Dictionary<int, float> tripDurationsInMinutes;
    public Dictionary<int, float> tripDurationsInHours;
    public Dictionary<int, float> tripPassengerCounts;
    public Dictionary<int, float> tripTipAmounts;
    public Dictionary<int, float> tripFareAmounts;
    public Dictionary<int, float> tripTotalAmounts;
    public Dictionary<int, float> tripOriginLats;
    public Dictionary<int, float> tripOriginLons;
    public Dictionary<int, float> tripDestinationLats;
    public Dictionary<int, float> tripDestinationLons;
    public Dictionary<int, DateTime> tripOriginTimes;
    public Dictionary<int, DateTime> tripDestinationTimes;

    public void GenerateAttributeDictionaires()
    {
        tripDistances = new Dictionary<int, float>();
        tripDurationsInMinutes = new Dictionary<int, float>();
        tripDurationsInHours = new Dictionary<int, float>();
        tripPassengerCounts = new Dictionary<int, float>();
        tripTipAmounts = new Dictionary<int, float>();
        tripFareAmounts = new Dictionary<int, float>();
        tripTotalAmounts = new Dictionary<int, float>();
        tripOriginLats = new Dictionary<int, float>();
        tripOriginLons = new Dictionary<int, float>();
        tripDestinationLats = new Dictionary<int, float>();
        tripDestinationLons = new Dictionary<int, float>();
        tripOriginTimes = new Dictionary<int, DateTime>();
        tripDestinationTimes = new Dictionary<int, DateTime>();

        int texSize = NextPowerOf2((int)Mathf.Sqrt(csvdata.DataCount));

        for (int x = 0; x < texSize; x++)
        {
            for (int y = 0; y < texSize; y++)
            {
                int index = x + y * texSize;
                if (index < csvdata.DataCount)
                    AddPointAttributesToDicitionaries(index);                
            }
        }

    }

   
    public void AddPointAttributesToDicitionaries(int index)
    {
        tripDistances[index] = (float)(csvdata.getOriginalValuePrecise(csvdata["trip_distance"].Data[index], "trip_distance"));
        tripOriginTimes[index] = (DateTime)csvdata.getOriginalValue(csvdata["pickup_datetime"].Data[index], "pickup_datetime");
        tripDestinationTimes[index] = (DateTime)csvdata.getOriginalValue(csvdata["dropoff_datetime"].Data[index], "dropoff_datetime");

        TimeSpan tripDuration = (tripDestinationTimes[index] - tripOriginTimes[index]);
        tripDurationsInMinutes[index] = (float)tripDuration.TotalMinutes;
        tripDurationsInHours[index] = (float)tripDuration.TotalHours;

        tripPassengerCounts[index] = (float)(csvdata.getOriginalValuePrecise(csvdata["passenger_count"].Data[index], "passenger_count"));
        tripTipAmounts[index] = (float)(csvdata.getOriginalValuePrecise(csvdata["tip_amount"].Data[index], "tip_amount"));
        tripFareAmounts[index] = (float)(csvdata.getOriginalValuePrecise(csvdata["fare_amount"].Data[index], "fare_amount"));
        tripTotalAmounts[index] = (float)(csvdata.getOriginalValuePrecise(csvdata["total_amount"].Data[index], "total_amount"));

        tripOriginLats[index] = float.Parse(csvdata.getOriginalValue(csvdata["pickup_latitude"].Data[index], "pickup_latitude") + "");
        tripOriginLons[index] = float.Parse(csvdata.getOriginalValue(csvdata["pickup_longitude"].Data[index], "pickup_longitude") + "");
        tripDestinationLats[index] = float.Parse(csvdata.getOriginalValue(csvdata["dropoff_latitude"].Data[index], "dropoff_latitude") + "");
        tripDestinationLons[index] = float.Parse(csvdata.getOriginalValue(csvdata["dropoff_longitude"].Data[index], "dropoff_longitude") + "");
    }





    private void ExtractTimeLimits(CSVDataSource csvds, string time_dim1 = "Time", string? time_dim2 = null)
    {
        DateTime min1, max1, min2, max2;

        if (timeDirection == Direction.LatestOnTop)
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
            if(timeDirection == Direction.LatestOnTop)
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

        if (timeDirection == Direction.LatestOnTop)
            onTop = maxTime;
        else
            onTop = minTime;

        secondsPerMeter = (float)maxTime.Subtract(minTime).TotalSeconds / this.transform.localScale.y;


        Debug.Log("MinTime = " + minTime.ToString() + " , MaxTime = " + maxTime.ToString() + " , sPm = " + secondsPerMeter);



    }

    private void ExtractSpaceLimits(CSVDataSource csvds, string lon_dim1 = "Lon", string lat_dim1 = "Lat", string? lon_dim2 = null, string? lat_dim2 = null)
    {
        float min_lon1 = float.Parse(csvds.getOriginalValue(csvds[lon_dim1].Data.Min(), lon_dim1) + "");
        float max_lon1 = float.Parse(csvds.getOriginalValue(csvds[lon_dim1].Data.Max(), lon_dim1) + "");
        float min_lat1 = float.Parse(csvds.getOriginalValue(csvds[lat_dim1].Data.Min(), lat_dim1) + "");
        float max_lat1 = float.Parse(csvds.getOriginalValue(csvds[lat_dim1].Data.Max(), lat_dim1) + "");

        if(lon_dim2 == null)
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






    ODSTCSubplot PlotLatLonTimeData(CSVDataSource csvds, string viewName = "View", string lon_dim = "Lon", string lat_dim = "Lat", string time_dim = "Time", Color? data_color = null)
    {
       

        ODSTCSubplot sp = new ODSTCSubplot();

        sp.maxlongitude = float.Parse(csvds.getOriginalValue(csvds[lon_dim].Data.Max(), lon_dim) + "");
        sp.maxlatitude = float.Parse(csvds.getOriginalValue(csvds[lat_dim].Data.Max(), lat_dim) + "");
        sp.minlongitude = float.Parse(csvds.getOriginalValue(csvds[lon_dim].Data.Min(), lon_dim) + "");
        sp.minlatitude = float.Parse(csvds.getOriginalValue(csvds[lat_dim].Data.Min(), lat_dim) + "");
        sp.minYValue = csvds[time_dim].MetaData.minValue; 
        sp.maxYValue = csvds[time_dim].MetaData.maxValue;
        if (timeDirection == Direction.LatestOnBottom)
        {
            sp.minTime = (DateTime)csvds.getOriginalValue(csvds[time_dim].Data.Max(), time_dim);
            sp.maxTime = (DateTime)csvds.getOriginalValue(csvds[time_dim].Data.Min(), time_dim);
        }
        else
        {
            sp.minTime = (DateTime)csvds.getOriginalValue(csvds[time_dim].Data.Min(), time_dim);
            sp.maxTime = (DateTime)csvds.getOriginalValue(csvds[time_dim].Data.Max(), time_dim);
        }



        Vector2 centerMap = new Vector2(sp.minlatitude + (sp.maxlatitude - sp.minlatitude) / 2.0f, sp.minlongitude + (sp.maxlongitude - sp.minlongitude) / 2.0f);

        Debug.Log("SubPlot Min time: " + sp.minTime.ToString() + " / Max time: " + sp.maxTime.ToString() +  " / Min lat: " + sp.minlatitude.ToString() + " / Max lat: " + sp.maxlatitude.ToString() + " / Min lon: " + sp.minlongitude.ToString() + " / Max lon: " + sp.maxlongitude.ToString() + " / Center lat: " + centerMap.y.ToString() + " / Center lon: " + centerMap.x.ToString());
        
        // TO DO : CONSIDER EASTERN HEMISPHERE

        // create a view builder with the point topology
        sp.viewBuilder = new ViewBuilder(MeshTopology.Points, "TaxiSTCManager").
             initialiseDataView(csvds.DataCount).
             setDataDimension(csvds[lon_dim].Data, ViewBuilder.VIEW_DIMENSION.X).
             setDataDimension(csvds[time_dim].Data, ViewBuilder.VIEW_DIMENSION.Y).
             setDataDimension(csvds[lat_dim].Data, ViewBuilder.VIEW_DIMENSION.Z).
             setSingleColor(data_color ?? Color.cyan);

        sp.viewBuilder = sp.viewBuilder.createIndicesPointTopology();

        //
        listOfTracks = new Dictionary<string, Color>();
        listOfActivities = new Dictionary<string, Color>();
        listOfModes = new Dictionary<string, Color>();
        listOfActivitiesAndModes = new Dictionary<string, Color>();
        PreloadCustomColorMapping(); // ALINE'S COLOR MAPPING 
        //



        sp.viewODColor = data_color ?? Color.cyan;

        // initialise the view builder wiith thhe number of data points and parent GameOBject

        Material mt = Instantiate(iatkSphereMat);

        mt.mainTexture = Resources.Load("sphere-texture") as Texture2D;

        mt.SetFloat("Size", 0.1f);
        mt.SetFloat("_MinSize", 0f);
        mt.SetFloat("_MaxSize", 1f);

        sp.view = sp.viewBuilder.updateView().apply(gameObject, mt, viewName);

        sp.view.SetSize(.1f);
       


        // TO DO: MOVE TO SCENARIOMANAGER
        if (sm.bingMap.transform.localScale.y != 1f)
            transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.bingMap.transform.position.y + 0.085f, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        else
            transform.position = new Vector3(sm.bingMap.transform.position.x - sm.bingMap.mapRenderer.MapDimension.x / 2f, sm.bingMap.transform.position.y + 0.05f, sm.bingMap.transform.position.z - sm.bingMap.mapRenderer.MapDimension.y / 2f);
        transform.localScale = new Vector3(sm.bingMap.mapRenderer.MapDimension.x, transform.localScale.y, sm.bingMap.mapRenderer.MapDimension.y);
        //

        sp.view.SetMinNormX(sp.mapLonToNormX((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LongitudeInDegrees));
        sp.view.SetMinNormZ(sp.mapLatToNormZ((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LatitudeInDegrees));
        sp.view.SetMaxNormX(sp.mapLonToNormX((float)sm.bingMap.mapRenderer.Bounds.TopRight.LongitudeInDegrees));
        sp.view.SetMaxNormZ(sp.mapLatToNormZ((float)sm.bingMap.mapRenderer.Bounds.TopRight.LatitudeInDegrees));

        if(timeDirection == Direction.LatestOnTop)
        { 
            sp.view.SetMinNormY(sp.mapTimeToNormY(minTime));
            sp.view.SetMaxNormY(sp.mapTimeToNormY(maxTime));
        }
        else
        {
            sp.view.SetMinNormY(sp.mapTimeToNormY(maxTime));
            sp.view.SetMaxNormY(sp.mapTimeToNormY(minTime));
        }

        return sp;
    }




   

    // Update is called once per frame
    public override void Update()
    {

        if (link != null && link.showLinks != ShowLinks)
            link.showLinks = ShowLinks;
        
    }


    public override void UpdatePlotsXZNormalizedRanges()
    {

        if (pickups != null)
        {
            pickups.view.SetMinNormX(pickups.mapLonToNormX((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LongitudeInDegrees));
            pickups.view.SetMinNormZ(pickups.mapLatToNormZ((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LatitudeInDegrees));
            pickups.view.SetMaxNormX(pickups.mapLonToNormX((float)sm.bingMap.mapRenderer.Bounds.TopRight.LongitudeInDegrees));
            pickups.view.SetMaxNormZ(pickups.mapLatToNormZ((float)sm.bingMap.mapRenderer.Bounds.TopRight.LatitudeInDegrees));
        }

        if (dropoffs != null)
        {
            dropoffs.view.SetMinNormX(dropoffs.mapLonToNormX((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LongitudeInDegrees));
            dropoffs.view.SetMinNormZ(dropoffs.mapLatToNormZ((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LatitudeInDegrees));
            dropoffs.view.SetMaxNormX(dropoffs.mapLonToNormX((float)sm.bingMap.mapRenderer.Bounds.TopRight.LongitudeInDegrees));
            dropoffs.view.SetMaxNormZ(dropoffs.mapLatToNormZ((float)sm.bingMap.mapRenderer.Bounds.TopRight.LatitudeInDegrees));
        }

        if (link != null && link.view != null && link.showLinks && dropoffs != null)
        {
            link.view.SetMinNormX(dropoffs.mapLonToNormX((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LongitudeInDegrees));
            link.view.SetMinNormZ(dropoffs.mapLatToNormZ((float)sm.bingMap.mapRenderer.Bounds.BottomLeft.LatitudeInDegrees));
            link.view.SetMaxNormX(dropoffs.mapLonToNormX((float)sm.bingMap.mapRenderer.Bounds.TopRight.LongitudeInDegrees));
            link.view.SetMaxNormZ(dropoffs.mapLatToNormZ((float)sm.bingMap.mapRenderer.Bounds.TopRight.LatitudeInDegrees));
        }


    }

    public override void UpdatePlotsYRange(DateTime minY, DateTime maxY)
    {
        if(timeDirection == Direction.LatestOnTop)
        {
            if (pickups != null)
            {
                pickups.view.SetMinY(dropoffs.mapTimeToNormY(minY));
                pickups.view.SetMaxY(dropoffs.mapTimeToNormY(maxY));

            }

            if (dropoffs != null)
            {
                dropoffs.view.SetMinY(dropoffs.mapTimeToNormY(minY));
                dropoffs.view.SetMaxY(dropoffs.mapTimeToNormY(maxY));

            }

            if (link != null && link.view != null && link.showLinks && dropoffs != null)
            {
                link.view.SetMinY(dropoffs.mapTimeToY(minY));
                link.view.SetMaxY(dropoffs.mapTimeToY(maxY));
            }
        }
        else
        {
            if (pickups != null)
            {
                pickups.view.SetMinY(pickups.mapTimeToY(maxY));
                pickups.view.SetMaxY(pickups.mapTimeToY(minY));
            }

            if (dropoffs != null)
            {
                dropoffs.view.SetMinY(dropoffs.mapTimeToY(maxY));
                dropoffs.view.SetMaxY(dropoffs.mapTimeToY(minY));
            }
        }
        
    }


    public override void UpdateGlyphSizeOrTrajWidth()
    {

        float zoom = sm.bingMap.mapRenderer.ZoomLevel;

        float newSize;

        //if (zoom < 9)
        //    newSize = 0.02f; // 0.01f
        //else 
        //if (zoom < 10)
        //    newSize = 0.025f; //0.02f;
        //else 
        if (zoom < 11)
            newSize = sm.pointSizeMultiplier * 0.05f;// 0.025f;
        else
            newSize = sm.pointSizeMultiplier * Mathf.Max(0.05f, 0.00132576f * Mathf.Pow(zoom, 4) - 0.0721801f * Mathf.Pow(zoom, 3) + 1.45966f * Mathf.Pow(zoom, 2) - 12.9653f * zoom + 42.7154f);
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

    









    /* 
     * COLORS: 
     * (ALL THE FOLLOWING FUNCTIONS TO HANDLE "COLOR BY ..." IDEALLY SHOULD NOT BE HARD CODED LIKE THIS)
     */
    string time_dim = "Timestamp";

    public override void ReloadViewsWithUpdatedColors()
    {
        ComputeColorsAndUpdateLegends();

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

        Material mt = Instantiate(iatkSphereMat);
        mt.mainTexture = Resources.Load("sphere-texture") as Texture2D;
        mt.SetFloat("Size", 0.1f);
        mt.SetFloat("_MinSize", 0f);
        mt.SetFloat("_MaxSize", 1f);
        pickups.view = pickups.viewBuilder.updateView().apply(gameObject, mt, "STC-Pickups");
        pickups.view.SetSize(.1f);

        mt = Instantiate(iatkSphereMat);
        mt.mainTexture = Resources.Load("sphere-texture") as Texture2D;
        mt.SetFloat("Size", 0.1f);
        mt.SetFloat("_MinSize", 0f);
        mt.SetFloat("_MaxSize", 1f);
        dropoffs.view = dropoffs.viewBuilder.updateView().apply(gameObject, iatkSphereMat, "STC-Dropoffs");
        dropoffs.view.SetSize(.1f);


        link = gameObject.AddComponent<LinkingViews>();
        link.visualisationSource = pickups.view;
        link.visualisationTarget = dropoffs.view;
        link.showLinks = ShowLinks;
        link.linkTransparency = .8f;
        link.LinkViews();

        //UpdatePlotsXZNormalizedRanges();
    }


    private void ComputeColorsAndUpdateLegends()
    {
        CSVDataSource csvds = this.GetComponent<CSVDataSource>();

        if (colorBy == ColorBy.OD)
        {
            pickups.viewBuilder = pickups.viewBuilder.setSingleColor(Color.blue);
            dropoffs.viewBuilder = dropoffs.viewBuilder.setSingleColor(Color.red);
            //UpdateLegend(listOfTracks);
        }

        if (colorBy == ColorBy.Identifier && csvds["Track"] != null)
        {
            pickups.viewBuilder = pickups.viewBuilder.setColors(csvds["Track"].Data.Select(x => TrackToColor(csvds.getOriginalValue(x, "Track").ToString())).ToArray()); //.setSingleColor(Color.cyan);//.
            dropoffs.viewBuilder = dropoffs.viewBuilder.setColors(csvds["Track"].Data.Select(x => TrackToColor(csvds.getOriginalValue(x, "Track").ToString())).ToArray()); //.setSingleColor(Color.cyan);//.
            //UpdateLegend(listOfTracks);
        }
        else if (colorBy == ColorBy.TimeOfDay && csvds[time_dim] != null)
        {
            pickups.viewBuilder = pickups.viewBuilder.setColors(csvds[time_dim].Data.Select(x => TimeToColor((DateTime)csvds.getOriginalValue(x, time_dim))).ToArray());
            dropoffs.viewBuilder = dropoffs.viewBuilder.setColors(csvds[time_dim].Data.Select(x => TimeToColor((DateTime)csvds.getOriginalValue(x, time_dim))).ToArray());
            /*Dictionary<string, Color> ColorByTimeLegend = new Dictionary<string, Color>();
            ColorByTimeLegend.Add("0h-6h", new Color(0f, 0f, 0f, 1f));
            ColorByTimeLegend.Add("6h-12h", new Color(1f, 0.5f, 0f, 1f));
            ColorByTimeLegend.Add("12h-18h", new Color(1f, 0f, 0f, 1f));
            ColorByTimeLegend.Add("18h-0h", new Color(0f, 0f, 1f, 1f));*/
            //UpdateLegend(ColorByTimeLegend);
        }
        else if (colorBy == ColorBy.Activity && csvds["Activity"] != null)
        {
            pickups.viewBuilder = pickups.viewBuilder.setColors(csvds["Activity"].Data.Select(x => ActivityToColor(csvds.getOriginalValue(x, "Activity").ToString())).ToArray());
            dropoffs.viewBuilder = dropoffs.viewBuilder.setColors(csvds["Activity"].Data.Select(x => ActivityToColor(csvds.getOriginalValue(x, "Activity").ToString())).ToArray());
            //UpdateLegend(listOfActivities);
        }
        else if (colorBy == ColorBy.Mode && csvds["Mode"] != null)
        {
            pickups.viewBuilder = pickups.viewBuilder.setColors(csvds["Mode"].Data.Select(x => ModeToColor(csvds.getOriginalValue(x, "Mode").ToString())).ToArray());
            dropoffs.viewBuilder = dropoffs.viewBuilder.setColors(csvds["Mode"].Data.Select(x => ModeToColor(csvds.getOriginalValue(x, "Mode").ToString())).ToArray());
            //UpdateLegend(listOfModes);
        }
        else if (colorBy == ColorBy.ActivityAndMode && csvds["ActivityAndMode"] != null)
        {
            pickups.viewBuilder = pickups.viewBuilder.setColors(csvds["ActivityAndMode"].Data.Select(x => ActivityAndModeToColor(csvds.getOriginalValue(x, "ActivityAndMode").ToString())).ToArray());
            dropoffs.viewBuilder = dropoffs.viewBuilder.setColors(csvds["ActivityAndMode"].Data.Select(x => ActivityAndModeToColor(csvds.getOriginalValue(x, "ActivityAndMode").ToString())).ToArray());
            //UpdateLegend(listOfActivitiesAndModes);
        }
    }

    private void PreloadCustomColorMapping()
    {
        // ALINE'S COLOR MAPPING 

        listOfActivities.Add("home", new Color32(27, 158, 119, 255));
        listOfActivities.Add("leisure", new Color32(216, 93, 0, 255));
        listOfActivities.Add("shopping", new Color32(117, 112, 179, 255));
        listOfActivities.Add("education", new Color32(231, 41, 138, 255));
        listOfActivities.Add("business", new Color32(230, 171, 2, 255));
        listOfActivities.Add("personal business", new Color32(102, 166, 30, 255));
        listOfActivities.Add("escort trips", new Color32(166, 118, 29, 255));
        listOfActivities.Add("other", new Color32(229, 196, 148, 255));
        listOfActivities.Add("moving", new Color32(128, 128, 128, 255));

        listOfModes.Add("walking", new Color32(51, 160, 44, 255));
        listOfModes.Add("cycling", new Color32(178, 223, 138, 255));
        listOfModes.Add("car", new Color32(31, 120, 180, 255));
        listOfModes.Add("public transport", new Color32(166, 206, 227, 255));
        listOfModes.Add("stationary", new Color32(128, 128, 128, 255));

        listOfActivitiesAndModes.Add("home", new Color32(27, 158, 119, 255));
        listOfActivitiesAndModes.Add("leisure", new Color32(216, 93, 0, 255));
        listOfActivitiesAndModes.Add("shopping", new Color32(117, 112, 179, 255));
        listOfActivitiesAndModes.Add("education", new Color32(231, 41, 138, 255));
        listOfActivitiesAndModes.Add("business", new Color32(230, 171, 2, 255));
        listOfActivitiesAndModes.Add("personal business", new Color32(102, 166, 30, 255));
        listOfActivitiesAndModes.Add("escort trips", new Color32(166, 118, 29, 255));
        listOfActivitiesAndModes.Add("other", new Color32(229, 196, 148, 255));
        //listOfActivitiesAndModes.Add("moving", new Color(128, 128, 128));
        listOfActivitiesAndModes.Add("walking", new Color32(51, 160, 44, 255));
        listOfActivitiesAndModes.Add("cycling", new Color32(178, 223, 138, 255));
        listOfActivitiesAndModes.Add("car", new Color32(31, 120, 180, 255));
        listOfActivitiesAndModes.Add("public transport", new Color32(166, 206, 227, 255));
        //listOfActivitiesAndModes.Add("stationary", new Color(128, 128, 128));
    }



    List<Color> colorList = new List<Color>() {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.magenta,
        Color.cyan,
        Color.black,
        // Color.white,
        Color.gray,
        new Color32(102, 0, 204, 255),
        new Color32(255, 140, 0, 255),//new Color32(51, 0, 102, 255),//new Color32(51, 102, 0, 255),
        new Color32(255, 153, 153, 255),
        //new Color32(255, 0, 127, 255),
        new Color32(102, 51, 0, 255),
        new Color32(153, 0, 0, 255),
        new Color32(0, 51, 102, 255),
        new Color32(102, 51, 0, 255),
        new Color32(255, 255, 153, 255),
        new Color32(0, 102, 102, 255),
        new Color32(255, 128, 0, 255),
        new Color32(153, 153, 0, 255),
        new Color32(255, 204, 153, 255),
        new Color32(153, 0, 76, 255),
        //new Color32(0, 204, 204, 255),
        //new Color32(0, 51, 51, 255),
        new Color32(102, 178, 255, 255),
        new Color32(204, 204, 255, 255),
        new Color32(153, 0, 153, 255),
        new Color32(255, 0, 127, 255),
        Color.white
    };

    Dictionary<string, Color> listOfTracks;// = new List<object>();
    Dictionary<string, Color> listOfActivities;// = new List<object>();
    Dictionary<string, Color> listOfModes;// = new List<object>();
    Dictionary<string, Color> listOfActivitiesAndModes;// = new List<object>();


    Color TrackToColor(string ID)
    {
        if (!listOfTracks.ContainsKey(ID))
        {
            listOfTracks.Add(ID, colorList.ElementAt((listOfTracks.Count + 1) % colorList.Count));
        }
        return listOfTracks[ID];
    }

    Color TimeToColor(DateTime t)
    {
        Color c = Color.magenta;

        int h = Int32.Parse(t.ToString("HH"));

        if (h >= 0 && h < 6) c = new Color(0f, 0f, 0f, 1f);   // black
        else if (h >= 6 && h < 12) c = new Color(1f, 0.5f, 0f, 1f); // orange
        else if (h >= 12 && h < 18) c = new Color(1f, 0f, 0f, 1f);   // red
        else if (h >= 18 && h < 24) c = new Color(0f, 0f, 1f, 1f);   // blue

        //Debug.Log(h + " ---- " + c.ToString());

        return c;
    }

    Color ActivityToColor(string ID)
    {
        if (!listOfActivities.ContainsKey(ID))
        {
            listOfActivities.Add(ID, colorList.ElementAt((listOfActivities.Count + 1) % colorList.Count));
        }
        return listOfActivities[ID];
    }

    Color ModeToColor(string ID)
    {
        if (!listOfModes.ContainsKey(ID))
        {
            listOfModes.Add(ID, colorList.ElementAt((listOfModes.Count + 1) % colorList.Count));
        }
        return listOfModes[ID];
    }


    Color ActivityAndModeToColor(string ID)
    {
        if (!listOfActivitiesAndModes.ContainsKey(ID))
        {
            listOfActivitiesAndModes.Add(ID, colorList.ElementAt((listOfActivitiesAndModes.Count + 1) % colorList.Count));
        }
        return listOfActivitiesAndModes[ID];
    }




    /// <summary>
    /// Finds the next power of 2 for a given number.
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    private int NextPowerOf2(int number)
    {
        int pos = 0;

        while (number > 0)
        {
            pos++;
            number = number >> 1;
        }
        return (int)Mathf.Pow(2, pos);
    }








}
