using System;
using IATK;
using UnityEngine;

public abstract class STCManager : MonoBehaviour
{
    // input data
    public TextAsset dataSource;
    public ScenarioManager sm;
    //public ComputeShader brushShader;
    //public Material brushMat;
    public STCWallsManager walls;

    //IATK elements

    public CSVDataSource csvdata;

    public LinkingViews link;
    //public ViewBrusherAndLinker blv_origin;
    //public ViewBrusherAndLinker blv_dest;
    //public XYZFilter origin_filter;
    //public XYZFilter destination_filter;

    public enum Direction { LatestOnBottom, LatestOnTop };
    public enum ColorBy { OD, Identifier, TimeOfDay, Activity, Mode, ActivityAndMode };


    [Space(10)]
    [Header("Data info")]

    public DateTime minTime, maxTime, onTop;
    public float secondsPerMeter, baseHeight;
    public float maxlongitude, maxlatitude, minlongitude, minlatitude, meanlongitude, meanlatitude, bestZoomLevel;

    public float minAllowedSecondsPerMeter = 600f; // 10 minutes
    public float maxAllowedSecondsPerMeter = 157680000f; // 5 years



    [Space(10)]
    [Header("Settings")]

    public bool ShowLinks = false;
    //public bool UseBrushes = false;
    //public bool UseBrushesOrigin = false;
    //public bool UseBrushesDest = false;
    public Direction timeDirection = Direction.LatestOnBottom;
    public ColorBy colorBy = ColorBy.Identifier;


    // Use this for initialization
    abstract public void Load(TextAsset dataSource);
    abstract public void Load(TextAsset dataSource, string dateFormat);

    /* void FacetBy(string attribute)
     {
         // categories
         // "B02598";
         // "B02512"

         float[] uniqueValues = csvdata[attribute].MetaData.categories;
         accordion = new View[uniqueValues.Length];

         for (int i = 0; i < uniqueValues.Length; i++)
         {
             View view = Facet(csvdata,
             csvdata.getOriginalValue(uniqueValues[i], attribute).ToString(), "Base", Random.ColorHSV());
             view.transform.position = new Vector3(i, 0, 0);
             accordion[i] = view;
         }

         //View v1 = Faceting(csvdata, "B02598");
         //v1.transform.position = Vector3.zero;

         //View v2 = Faceting(csvdata, "B02512");
         //v2.transform.position = Vector3.right;


     }*/

    abstract public CSVDataSource createCSVDataSource(string data);


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

    public float mapTimeToY(DateTime time)
    {
        //return ((time.Ticks - this.minTime.Ticks) / (this.maxTime.Ticks - this.minTime.Ticks)) * this.transform.localScale.y + this.transform.position.y - this.transform.localScale.y/2f;

        float height;
        //float baseHeight = this.transform.position.y;// - this.transform.localScale.y / 2f;

        if (timeDirection == Direction.LatestOnTop)
        {
            height = baseHeight + (float)time.Subtract(minTime).TotalSeconds / secondsPerMeter;
        }
        else
        {
            height = baseHeight + (float)maxTime.Subtract(time).TotalSeconds / secondsPerMeter;
        }

        return height;
    }


    public DateTime mapYToTime(float y)
    {
        //return (time.Ticks - this.minTime.Ticks) / (this.maxTime.Ticks - this.minTime.Ticks);

        //float baseHeight = this.transform.position.y;// - this.transform.localScale.y / 2f;

        float totalseconds = (y - baseHeight) * secondsPerMeter;


        //Debug.Log("base: " + baseHeight + " target: " + y + " totalseconds: " + totalseconds);

        //if (totalseconds > 5 * (maxTime - minTime).TotalSeconds)
        //    return minTime; // ERROR 

        DateTime dt;
        try
        {
            if (timeDirection == Direction.LatestOnTop)
            {
                dt = minTime.AddSeconds(totalseconds);
            }
            else
            {
                dt = maxTime.AddSeconds(-totalseconds);
            }
        }
        catch (ArgumentOutOfRangeException e)
        {
            dt = minTime;
        }

        return dt; 

    }

    public void ResetPositionAndScale()
    {
        sm.bingMap.mapRenderer.Center = new Microsoft.Geospatial.LatLon(meanlatitude, meanlongitude);
        sm.bingMap.mapRenderer.ZoomLevel = bestZoomLevel;

        walls.SuperRescaleTime(1f/ transform.localScale.y, baseHeight);
    }

    public bool containsTime(DateTime time)
    {
        if (DateTime.Compare(time, minTime) > 0 && DateTime.Compare(time, maxTime) < 0)
            return true;
        else
            return false;
    }


    // abstract public void ExtractTimeLimits(CSVDataSource csvds, string time_dim = "Time");

    //   abstract public void ExtractSpaceLimits(CSVDataSource csvds, string lon_dim = "Lon", string lat_dim = "Lat");









    public float distance(float x1, float y1, float x2, float y2)
    {
        return Mathf.Sqrt(Mathf.Pow(x2 - x1, 2) + Mathf.Pow(y2 - y1, 2));
    }

    public float ComputeBestZoomLevel(float minlatitude, float maxlatitude, float minlongitude, float maxlongitude)
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



    //delegate float[] Filter(float[] ar, CSVDataSource csvds, string fiteredValue, string filteringAttribute);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="csvds">CSV data source</param>
    /// <param name="filteringValue"> filtered value</param>
    /// <param name="filteringAttribute"> filtering attribute </param>
    /// <param name="color"></param>
    /// <returns></returns>
    /* View Facet(CSVDataSource csvds, string filteringValue, string filteringAttribute, Color color)
     {
         //B02598
         //B02512

         // filters the array on a particular value in another dimension
         Filter baseFilter = (array, datasource, value, dimension) =>
         {
             return array.Select((b, i) => new { index = i, _base = b })
             .Where(b => datasource.getOriginalValuePrecise(csvds[dimension].Data[b.index], dimension).ToString() == value)
             .Select(b => b._base).ToArray();
         };

         Filter identity = (ar, ds, fv, fa) => { return ar; };
         // baseFilter = identity;

         var xData = baseFilter(csvds["Lat"].Data, csvds, filteringValue, filteringAttribute);
         var yData = baseFilter(csvds["Lon"].Data, csvds, filteringValue, filteringAttribute);
         var zData = baseFilter(csvds["Base"].Data, csvds, filteringValue, filteringAttribute);

         ViewBuilder vb = new ViewBuilder(MeshTopology.Points, "Uber pick up point visualisation").
             initialiseDataView(xData.Length).
             setDataDimension(xData, ViewBuilder.VIEW_DIMENSION.X).
             setDataDimension(yData, ViewBuilder.VIEW_DIMENSION.Y).
             //setDataDimension(zData, ViewBuilder.VIEW_DIMENSION.Z).
             setSize(baseFilter(csvds["Date"].Data, csvds, filteringValue, filteringAttribute)).
             setColors(xData.Select(x => color).ToArray());

         Material mt = IATKUtil.GetMaterialFromTopology(AbstractVisualisation.GeometryType.Points);
         mt.SetFloat("_MinSize", 0.01f);
         mt.SetFloat("_MaxSize", 0.05f);

         return vb.updateView().apply(gameObject, mt);
     }
    */

    //void accordionPosition(ref Transform toAccordion, Transform left, Transform right, float pos, float nbOfViews)
    // {
    //    toAccordion.position = Vector3.Lerp(left.position, right.position, pos / nbOfViews);
    //    toAccordion.rotation = Quaternion.Lerp(left.rotation, right.rotation, pos / nbOfViews);
    // }

    // Update is called once per frame
    abstract public void Update();



    abstract public void UpdatePlotsXZNormalizedRanges();

    abstract public void UpdatePlotsYRange(DateTime minY, DateTime maxY);


    public virtual void UpdateGlyphSizeOrTrajWidth()
    {

    }

    public virtual void ReloadViewsWithUpdatedColors()
    {

    }

}