using IATK;
using Microsoft.Maps.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using static STCManager;


public class STCWallsManager : MonoBehaviour {


    public ScenarioManager sm;
    public STCManager stc;


    public CultureInfo culture = new CultureInfo("en-US");
    public string dateFormat = "f";

    public Collider fourthCollider;


    [Header("Components for walls v1")]

    //public STCManager stc;
    public TextMesh tripleLinePrefab;
    public TextMesh quadrupleLinePrefab;
    //[SerializeField] public List<TextMesh> lines;
    public List<TextMesh> lines = new List<TextMesh>();
    public TextMesh leftInspectionLine, rightInspectionLine;
    //private bool rightInspectionLineActive;


    public float spacing = .1f;
    public int numLines = 11;


    public bool viewLeftInspectionPlane = false;
    public bool viewRightInspectionPlane = false;
    public MapRenderer leftMap, rightMap;



    [Header("Components for walls v2")]

    public bool wallsV2 = true;

    public enum TimeSlicesGranularity
    {
        TenMinutes, HalfHour, Hour, SixHours, Day, Week, Month, Year
    };

    public TimeSlicesGranularity minGranularity = TimeSlicesGranularity.TenMinutes;
    public TimeSlicesGranularity maxGranularity = TimeSlicesGranularity.Year;
    public TimeSlicesGranularity currentGranularity;
    public GameObject timeSlicePrefab;
    public LineRenderer timeSliceLinePrefab;
    public TextMesh timeSliceLabelPrefab;
    public Material greyMaterial, whiteMaterial;
    public float regularTransparency = 0.4f, extraTransparency = 0.2f;
    public STCTimeInspector leftTimeInspector, rightTimeInspector;

    public bool allowTimeMinMaxConstraining = true;
    public bool isTimeMinMaxConstraining = false;
    public GameObject timeMinRegularSelectorWidget, timeMaxRegularSelectorWidget;
    public DateTime userDefinedMinTime, userDefinedMaxTime;
    public TextMesh timeMinRegularSelectorLabel, timeMaxRegularSelectorLabel;
    public float timeWidgetDiameter = 0.05f;
    public int deskScaleFontSize = 45, roomScaleFontSize = 80;
    Vector3 previousTimeMinRegSelectorWidgetPos, previousTimeMaxRegSelectorWidgetPos;
    public bool highlightPointedAtSlices = true;

    // Use this for initialization
    new void Start () {
        //DateTimeFormatInfo myDTFI = new CultureInfo("pt-BR", false).DateTimeFormat;
        //Debug.Log(myDTFI.LongDatePattern);
        //Debug.Log(myDTFI.ShortTimePattern);

        Time.fixedDeltaTime = 0.5f;


        Initialize();

    }


    private float previousSTCheight = 0;
    private bool inspectingSlices = true;

    void FixedUpdate()
    {
        if (wallsV2)// && stc.sm.qm.texSize > 0) // stc.sm.qm.texSize > 0 indicates that the dataset has been loaded
        {
            // otimizar aqui testando se altura do stc mudou 
            if (stc.transform.localScale.y != previousSTCheight)
            {
                RecomputeTimeGranularity();
                previousSTCheight = stc.transform.localScale.y;
            }

            //if (mustRefreshTimeSlices)
            //{
            //    RefreshTimeSlices();
            //    mustRefreshTimeSlices = false; 
            //}

            if(highlightPointedAtSlices && !sm.qm.InODBrushingMode)
            {
                if (leftTimeInspector.invisibleSlice.brushedViews.Count < 2)
                {
                    if (GameObject.Find("STC-Pickups"))
                        leftTimeInspector.invisibleSlice.brushedViews.Add(GameObject.Find("STC-Pickups").GetComponent<View>());
                    if (GameObject.Find("STC-Dropoffs"))
                        leftTimeInspector.invisibleSlice.brushedViews.Add(GameObject.Find("STC-Dropoffs").GetComponent<View>());
                    return;
                }
                if (rightTimeInspector.invisibleSlice.brushedViews.Count < 2)
                {
                    if (GameObject.Find("STC-Pickups"))
                        rightTimeInspector.invisibleSlice.brushedViews.Add(GameObject.Find("STC-Pickups").GetComponent<View>());
                    if (GameObject.Find("STC-Dropoffs"))
                        rightTimeInspector.invisibleSlice.brushedViews.Add(GameObject.Find("STC-Dropoffs").GetComponent<View>());
                    return;
                }



                if (!leftTimeInspector.gameObject.activeSelf && rightTimeInspector.gameObject.activeSelf)
                {
                    inspectingSlices = true;
                    rightTimeInspector.invisibleSlice.Refilter();

                    stc.sm.qm.pickupsView.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", rightTimeInspector.invisibleSlice.brushedIndicesTexture);
                    stc.sm.qm.dropoffsView.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", rightTimeInspector.invisibleSlice.brushedIndicesTexture);
                    stc.sm.qm.pickupsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", rightTimeInspector.invisibleSlice.brushedIndicesTexture);
                    stc.sm.qm.dropoffsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", rightTimeInspector.invisibleSlice.brushedIndicesTexture);
                    stc.sm.qm.pickupsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", rightTimeInspector.invisibleSlice.brushedIndicesTexture);
                    stc.sm.qm.dropoffsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", rightTimeInspector.invisibleSlice.brushedIndicesTexture);
                }
                else if (leftTimeInspector.gameObject.activeSelf && !rightTimeInspector.gameObject.activeSelf)
                {
                    inspectingSlices = true;
                    leftTimeInspector.invisibleSlice.Refilter();

                    stc.sm.qm.pickupsView.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", leftTimeInspector.invisibleSlice.brushedIndicesTexture);
                    stc.sm.qm.dropoffsView.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", leftTimeInspector.invisibleSlice.brushedIndicesTexture);
                    stc.sm.qm.pickupsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", leftTimeInspector.invisibleSlice.brushedIndicesTexture);
                    stc.sm.qm.dropoffsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", leftTimeInspector.invisibleSlice.brushedIndicesTexture);
                    stc.sm.qm.pickupsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", leftTimeInspector.invisibleSlice.brushedIndicesTexture);
                    stc.sm.qm.dropoffsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", leftTimeInspector.invisibleSlice.brushedIndicesTexture);
                }
                else if (leftTimeInspector.gameObject.activeSelf && rightTimeInspector.gameObject.activeSelf)
                {
                    inspectingSlices = true;
                    leftTimeInspector.invisibleSlice.Refilter();
                    rightTimeInspector.invisibleSlice.Refilter();

                    RenderTexture leftPlusRight = stc.sm.qm.CombineTexturesWithOrUsingCS(leftTimeInspector.invisibleSlice.brushedIndicesTexture, rightTimeInspector.invisibleSlice.brushedIndicesTexture);

                    stc.sm.qm.pickupsView.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", leftPlusRight);
                    stc.sm.qm.dropoffsView.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", leftPlusRight);
                    stc.sm.qm.pickupsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", leftPlusRight);
                    stc.sm.qm.dropoffsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", leftPlusRight);
                    stc.sm.qm.pickupsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", leftPlusRight);
                    stc.sm.qm.dropoffsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", leftPlusRight);
                }
                else if (!leftTimeInspector.gameObject.activeSelf && !rightTimeInspector.gameObject.activeSelf && inspectingSlices) // must clear highlights
                {
                    RenderTexture clearTexture = new RenderTexture(stc.sm.qm.texSize, stc.sm.qm.texSize, 24);
                    clearTexture.enableRandomWrite = true;
                    clearTexture.filterMode = FilterMode.Point;
                    clearTexture.Create();
                    ClearFilterTexture(clearTexture);

                    stc.sm.qm.pickupsView.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", clearTexture);
                    stc.sm.qm.dropoffsView.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", clearTexture);
                    stc.sm.qm.pickupsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", clearTexture);
                    stc.sm.qm.dropoffsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", clearTexture);
                    stc.sm.qm.pickupsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", clearTexture);
                    stc.sm.qm.dropoffsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_HighlightTexture", clearTexture);

                    inspectingSlices = false;
                }




            }




            // enables time "regular selection" through widgets
            ApplyTimeConstraints();

        }
    }

    // Update is called once per frame
    void Update () {

        if(TimeSelectorWidgetsMoved())
            UpdateTimeSelectorLabels();


        if (wallsV2)// && stc.sm.qm.texSize > 0) // stc.sm.qm.texSize > 0 indicates that the dataset has been loaded
        {
            if (mustRefreshTimeSlices)
            {
                RefreshTimeSlices();
                mustRefreshTimeSlices = false;
            }
        }


    }


    public void ApplyTimeConstraints()
    {
        //if (stc.mapYToTime(timeMinRegularSelectorWidget.transform.position.y).CompareTo(userDefinedMinTime) != 0 ||
        //        stc.mapYToTime(timeMaxRegularSelectorWidget.transform.position.y).CompareTo(userDefinedMaxTime) != 0)
        if (stc.mapYToTime(timeMinRegularSelectorWidget.transform.position.y).Subtract(userDefinedMinTime).Duration().TotalMinutes > 1 ||
            stc.mapYToTime(timeMaxRegularSelectorWidget.transform.position.y).Subtract(userDefinedMaxTime).Duration().TotalMinutes > 1)
            {
            userDefinedMinTime = stc.mapYToTime(timeMinRegularSelectorWidget.transform.position.y);
            userDefinedMaxTime = stc.mapYToTime(timeMaxRegularSelectorWidget.transform.position.y);

            if (userDefinedMinTime.CompareTo(stc.minTime) < 0)
                userDefinedMinTime = stc.minTime;
            if (userDefinedMaxTime.CompareTo(stc.maxTime) > 0)
                userDefinedMaxTime = stc.maxTime;

            if (userDefinedMinTime.Subtract(stc.minTime).Duration().TotalMinutes > 5 || userDefinedMaxTime.Subtract(stc.maxTime).Duration().TotalMinutes > 5)
                isTimeMinMaxConstraining = true;
            else
                isTimeMinMaxConstraining = false;

            UpdateTimeSelectorWidgetsAndLabels();

            stc.sm.qm.acm.SignalNeedForTextureUpdate();

            //stc.UpdatePlotsYRange(userDefinedMinTime, userDefinedMaxTime); //not necessary anymore as time constraint is now applied by the AttributeConstraintManager through its filter texture
        }
    }

    public void Initialize()
    {
       

        

        this.transform.position = sm.bingMap.transform.position + new Vector3(0f, sm.bingMap.mapRenderer.MapBaseHeight, 0f);

            this.transform.localScale = new Vector3(sm.bingMap.mapRenderer.MapDimension.x, stc.transform.localScale.y, sm.bingMap.mapRenderer.MapDimension.y);


        stc.baseHeight = this.transform.position.y;



        userDefinedMinTime = stc.minTime;
        userDefinedMaxTime = stc.maxTime;
        UpdateTimeSelectorWidgetsAndLabels();

        this.Initialize(numLines, spacing);

       
    }
    

    internal void RefreshInspectionLine(int which, float height)
    {
        if (stc == null)
            return;

        if(!wallsV2)
        { 

            if(which == 1)
            {
                this.rightInspectionLine.gameObject.SetActive(true);
                //rightInspectionLineActive = true;
                rightInspectionLine.gameObject.transform.position = new Vector3(rightInspectionLine.gameObject.transform.position.x, height, rightInspectionLine.gameObject.transform.position.z);
                ///rightInspectionLine.text = stc.trajHolder.heightToTime(height).ToString(stc.dateFormat, stc.culture);
                rightInspectionLine.text = stc.mapYToTime(height).ToString(dateFormat, culture);
                //rightInspectionLine.text = "right :)";


                foreach (Transform child in rightInspectionLine.transform)
                {
                    child.gameObject.SetActive(true);
                    child.GetComponent<TextMesh>().text = rightInspectionLine.text;
                }

                if (viewRightInspectionPlane)
                    UpdateInspectionMap(rightMap, height);

           


            }
            else if(which == 0)
            {
                this.leftInspectionLine.gameObject.SetActive(true);
                //leftInspectionLineActive = true;
                leftInspectionLine.gameObject.transform.position = new Vector3(leftInspectionLine.gameObject.transform.position.x, height, leftInspectionLine.gameObject.transform.position.z);
                ///leftInspectionLine.text = stc.trajHolder.heightToTime(height).ToString(stc.dateFormat, stc.culture);
                leftInspectionLine.text = stc.mapYToTime(height).ToString(dateFormat, culture); 
                //leftInspectionLine.text = "left :)";

                foreach (Transform child in leftInspectionLine.transform)
                {
                    child.gameObject.SetActive(true);
                    child.GetComponent<TextMesh>().text = leftInspectionLine.text;
                }

                if (viewLeftInspectionPlane)
                    UpdateInspectionMap(leftMap, height);
                //else
                //    leftMap.gameObject.SetActive(false);
            }
        }

        else // V2
        {
            if (which == 1)
            {
                rightTimeInspector.gameObject.SetActive(true);
                rightTimeInspector.SetPositionAndText(height);
               
            }
            else if (which == 0)
            {
                leftTimeInspector.gameObject.SetActive(true);
                leftTimeInspector.SetPositionAndText(height);
            }
        }

    }

    private void UpdateInspectionMap(MapRenderer map, float height)
    {
        map.gameObject.SetActive(true);
        map.transform.position = new Vector3(rightInspectionLine.gameObject.transform.position.x, height - map.MapBaseHeight, rightInspectionLine.gameObject.transform.position.z);

        if(map.Center != sm.bingMap.mapRenderer.Center)
            map.Center = sm.bingMap.mapRenderer.Center;
        if(map.LocalMapDimension != sm.bingMap.mapRenderer.LocalMapDimension)
            map.LocalMapDimension = sm.bingMap.mapRenderer.LocalMapDimension;
        if(map.gameObject.transform.localScale != sm.bingMap.gameObject.transform.localScale)
            map.gameObject.transform.localScale = sm.bingMap.gameObject.transform.localScale;
        if(map.ZoomLevel != sm.bingMap.mapRenderer.ZoomLevel)
            map.ZoomLevel = sm.bingMap.mapRenderer.ZoomLevel;

    }

    public void deactivateInspectionLine(int which)
    {
        if (wallsV2)
        {
            if (which == 0)
            {
                leftTimeInspector.gameObject.SetActive(false);
            }
            else if (which == 1)
            {
                rightTimeInspector.gameObject.SetActive(false);
            }
        }
        else
        {
            if (which == 0)
            {
                this.leftInspectionLine.gameObject.SetActive(false);
                //leftInspectionLineActive = false;

                if (viewLeftInspectionPlane)
                    //leftMap.gameObject.SetActive(false);
                    UpdateInspectionMap(leftMap, -1000f);
                //else
                //    leftMap.gameObject.SetActive(false);

            }
            else if (which == 1)
            {
                this.rightInspectionLine.gameObject.SetActive(false);
                //rightInspectionLineActive = false;

                if (viewRightInspectionPlane)
                    //rightMap.gameObject.SetActive(false);
                    UpdateInspectionMap(rightMap, -1000f);
                //else
                //    rightMap.gameObject.SetActive(false);
            }
        }
    }

    public void activateInspectionLine(int which)
    {
        if(wallsV2)
        {
            if (which == 0)
            {
                leftTimeInspector.gameObject.SetActive(true);
            }
            else if (which == 1)
            {
                rightTimeInspector.gameObject.SetActive(true);
            }
        }
        else
        { 
            if (which == 0)
            {
                this.leftInspectionLine.gameObject.SetActive(true);
                //leftInspectionLineActive = false;

                //if (viewLeftInspectionPlane)
                 //   leftMap.gameObject.SetActive(true);
            }
            else if (which == 1)
            {
                this.rightInspectionLine.gameObject.SetActive(true);
                //rightInspectionLineActive = false;

                //if (viewRightInspectionPlane)
                //    rightMap.gameObject.SetActive(true);
            }
        }

    }

    internal void Initialize(int numLines, float spacing)
    {

        if (!wallsV2)
        { // WALLS V1


#pragma warning disable
            for (int i = 0; i < numLines; i++)
            {
                //TextMesh newLine = Instantiate(linePrefab).GetComponent<TextMesh>();
                GameObject newLine;
                if (sm.VirtualDesk)
                    newLine = Instantiate(tripleLinePrefab).gameObject;
                else
                    newLine = Instantiate(quadrupleLinePrefab).gameObject;

                newLine.name = newLine.name + "(" + i + ")";

                newLine.transform.SetParent(this.transform);

                //newLine.GetComponent<LineRenderer>().alignment = LineAlignment.Local;
                // if (stc.VirtualDesk)
                newLine.GetComponent<TextMesh>().transform.localScale = new Vector3(-1, 3, 1);  

                newLine.transform.localPosition = new Vector3(0f, 0f + i * spacing, 0f);
                //newLine.GetComponent<TextMesh>().fontSize = 15;
                lines.Add(newLine.GetComponent<TextMesh>()); //lines.Add(newLine);
            }

        }
        
#pragma warning restore


        if (sm.VirtualDesk)
        {
            fourthCollider.enabled = false;
        }
        else if (sm.ClippedEgoRoom)
        {
            if (sm.EgoRoomIsFourWalled)
                fourthCollider.enabled = true;
            else
                fourthCollider.enabled = false;
        }

        





        this.Refresh();
    }

    //private List<int> dataYears = new List<int>();
    //private 


    public void Refresh()
    {
        // if v1

        if(!wallsV2)
        { 
            foreach (TextMesh line in lines)
            {
                //DateTime lineTime = stc.trajHolder.heightToTime(line.gameObject.transform.position.y);
               DateTime lineTime = stc.mapYToTime(line.gameObject.transform.position.y - .005f);

               if (interactibleAreaContainsTime(lineTime))
               {
                   line.gameObject.SetActive(true);
                   line.text = stc.mapYToTime(line.gameObject.transform.position.y).ToString(dateFormat, culture);
           
                    foreach(Transform child in line.transform)
                    {
                        child.gameObject.SetActive(true);
                        child.GetComponent<TextMesh>().text = line.text;
                    }
               }
               else
               {
                    line.gameObject.SetActive(false);
               }
            }
        }

        if (wallsV2)
        {
            mustRefreshTimeSlices = true; 
            
        }


    }


    public List<GameObject> timeSlicePool = new List<GameObject>();
    public List<LineRenderer> timeSliceLinePool = new List<LineRenderer>();
    public List<TextMesh> timeSliceLabelPool = new List<TextMesh>();
    public int num_slices; 

    bool mustRefreshTimeSlices = false;
    bool white = true; 

    void RefreshTimeSlices()
    {
        //DateTime bottom = stc.mapYToTime(sm.bingMap.transform.position.y);
        //DateTime top = stc.mapYToTime(sm.bingMap.transform.position.y + stc.transform.localScale.y);

        DateTime earliest, latest;
        float direction; 
        if (stc.timeDirection == Direction.LatestOnBottom)
        {
            //earliest = top;
            //latest = bottom;
            direction = -1f;
        }
        else
        {
            //earliest = bottom;
            //latest = top;
            direction = 1f;
            ////direction = -1f;
        }

        earliest = stc.minTime;
        latest = stc.maxTime;


        TimeSpan span = (latest - earliest).Duration();
        //int num_slices;
        DateTime sliceTime;

        //string debug;

        switch (currentGranularity)
        {
            case TimeSlicesGranularity.TenMinutes:

                num_slices = (int)Math.Ceiling(span.TotalHours * 6);
                UpdatePools(num_slices);

                DateTime earliestTenMinutesStart = new DateTime(earliest.Year, earliest.Month, earliest.Day, earliest.Hour, 0, 0);

                for (int i = 0; i < num_slices; i++)
                {
                    sliceTime = earliestTenMinutesStart.AddHours(i / 6f);
                    string sliceText;
                    if (sliceTime.Hour == 0)
                        sliceText = sliceTime.ToString("f", culture);
                    else
                        sliceText = sliceTime.ToString("t", culture);
                    SetupTimeSlice(i, 600f, sliceTime, direction, sliceText);
                }
                break;


            case TimeSlicesGranularity.HalfHour:

                num_slices = (int)Math.Ceiling(span.TotalHours * 2);
                UpdatePools(num_slices);

                DateTime earliestHalfHourStart = new DateTime(earliest.Year, earliest.Month, earliest.Day, earliest.Hour, 0, 0);

                for (int i = 0; i < num_slices; i++)
                {
                    sliceTime = earliestHalfHourStart.AddHours(0.5 * i);
                    string sliceText;
                    if (sliceTime.Hour == 0)
                        sliceText = sliceTime.ToString("f", culture);
                    else
                        sliceText = sliceTime.ToString("t", culture);
                    SetupTimeSlice(i, 1800f, sliceTime, direction, sliceText);
                }
                break;

            case TimeSlicesGranularity.Hour:

                num_slices = (int)Math.Ceiling(span.TotalHours);
                UpdatePools(num_slices);

                DateTime earliestHourStart = new DateTime(earliest.Year, earliest.Month, earliest.Day, earliest.Hour, 0, 0);

                //debug = "Number of slices: " + num_slices + " => ";
                for (int i = 0; i < num_slices; i++)
                {
                    //    debug += ((top.AddHours(i).Hour) + " ( " + (top.AddHours(i).DayOfWeek) + " " + (top.AddHours(i).Day) + "), ");
                    sliceTime = earliestHourStart.AddHours(i);
                    string sliceText;
                    if (sliceTime.Hour == 0)
                        sliceText = sliceTime.ToString("f", culture);
                    else
                        sliceText = sliceTime.ToString("t", culture); // sliceTime.Hour.ToString() + ":00"; //.ToString(dateFormat, culture)
                    SetupTimeSlice(i, 3600f, sliceTime, direction, sliceText);                 
                }
                //Debug.Log(debug);
                break;

            case TimeSlicesGranularity.SixHours:

                num_slices = (int)Math.Ceiling(span.TotalHours / 6);
                UpdatePools(num_slices);

                DateTime earliestSixHourStart = new DateTime(earliest.Year, earliest.Month, earliest.Day, 0, 0, 0);

                //debug = "Number of slices: " + num_slices + " => ";
                for (int i = 0; i < num_slices; i++)
                {
                    //    debug += ((top.AddHours(i).Hour) + " ( " + (top.AddHours(i).DayOfWeek) + " " + (top.AddHours(i).Day) + "), ");
                    sliceTime = earliestSixHourStart.AddHours(6 * i);
                    string sliceText;
                    if (sliceTime.Hour == 0)
                        sliceText = sliceTime.ToString("f", culture);
                    else
                        sliceText = sliceTime.ToString("t", culture); // sliceTime.Hour.ToString() + ":00"; //.ToString(dateFormat, culture)
                    SetupTimeSlice(i, 6 * 3600f, sliceTime, direction, sliceText);
                }
                //Debug.Log(debug);
                break;

            case TimeSlicesGranularity.Day:

                num_slices = (int)Math.Ceiling(span.TotalDays);
                UpdatePools(num_slices);

                DateTime earliestDayStart = new DateTime(earliest.Year, earliest.Month, earliest.Day, 0, 0, 0);

                //debug = "Number of slices: " + num_slices + " => ";
                for (int i = 0; i < num_slices; i++)
                {
                    //debug += (top.AddDays(i).Month + " " + top.AddDays(i).Day + ", ");
                    sliceTime = earliestDayStart.AddDays(i);
                    string sliceText = sliceTime.ToString("D", culture);// sliceTime.Day.ToString();
                    SetupTimeSlice(i, 24 * 3600f, sliceTime, direction, sliceText);


                }
                //Debug.Log(debug);



                break;

            case TimeSlicesGranularity.Week:
                
                num_slices = (int)Math.Ceiling(span.TotalDays / 7f);
                UpdatePools(num_slices);
                DateTime earliestWeekStart = new DateTime(earliest.Year, earliest.Month, earliest.Day, 0, 0, 0).AddDays(-(int)earliest.DayOfWeek);
                // DateTimeFormatInfo.CurrentInfo.Calendar.GetWeekOfYear(earliest, CalendarWeekRule.FirstDay, DayOfWeek.Monday)

                for (int i = 0; i < num_slices; i++)
                {
                    sliceTime = earliestWeekStart.AddDays(7 * i);
                    string sliceText = sliceTime.ToString("D", culture);   
                    SetupTimeSlice(i, 7 * 24 * 3600f, sliceTime, direction, sliceText);
                }

                break;

            case TimeSlicesGranularity.Month:

                num_slices = (int)Math.Ceiling(span.TotalDays / 30f);
                UpdatePools(num_slices);
                DateTime earliestMonthStart = new DateTime(earliest.Year, earliest.Month, 1, 0, 0, 0);

                //debug = "Number of slices: " + num_slices + " => ";
                for (int i = 0; i < num_slices; i++)
                {
                    //    debug += (top.AddMonths(i).Month + " " + top.AddMonths(i).Year + ", ");
                    sliceTime = earliestMonthStart.AddMonths(i);
                    string sliceText = sliceTime.ToString("y", culture);   //sliceTime.Month.ToString();
                    SetupTimeSlice(i, DateTime.DaysInMonth(sliceTime.Year, sliceTime.Month) * 24 * 3600f, sliceTime, direction, sliceText);
                }
                //Debug.Log(debug);

                break;

            case TimeSlicesGranularity.Year:

                num_slices = (int)Math.Ceiling(span.TotalDays / 365f);
                UpdatePools(num_slices);
                DateTime earliestYearStart = new DateTime(earliest.Year, 1, 1, 0, 0, 0);

                //debug = "Number of slices: " + num_slices + " => ";
                for (int i = 0; i < num_slices; i++)
                {
                    //debug += (top.AddYears(i).Year + ", ");
                    sliceTime = earliestYearStart.AddYears(i);
                    string sliceText = sliceTime.ToString("yyyy", culture);// sliceTime.Year.ToString();
                    SetupTimeSlice(i, 365 * 24 * 3600f, sliceTime, direction, sliceText);
                }
                //Debug.Log(debug);

                break;

            default:
                break;
        }
    }

    void SetupTimeSlice(int i, float spanInSeconds, DateTime sliceTime, float direction, string text)
    {
        timeSlicePool[i].transform.localScale = new Vector3(stc.transform.localScale.x / this.transform.localScale.x, spanInSeconds / stc.secondsPerMeter / this.transform.localScale.y, stc.transform.localScale.z / this.transform.localScale.z);
        timeSlicePool[i].transform.position = new Vector3(sm.bingMap.transform.position.x, stc.mapTimeToY(sliceTime) + direction * timeSlicePool[i].transform.localScale.y / 2f, sm.bingMap.transform.position.z); // incorrect y if direction is inverted

        timeSliceLinePool[i].transform.localScale = new Vector3(timeSlicePool[i].transform.localScale.x, timeSliceLinePool[i].transform.localScale.y, timeSlicePool[i].transform.localScale.z);
        timeSliceLinePool[i].transform.position = new Vector3(sm.bingMap.transform.position.x, stc.mapTimeToY(sliceTime), sm.bingMap.transform.position.z);

        timeSliceLabelPool[i].transform.localScale = new Vector3(0.005f / this.transform.localScale.z, timeSliceLabelPool[i].transform.localScale.y, timeSliceLabelPool[i].transform.localScale.z);
        //timeSliceLabelPool[i].transform.position = new Vector3(sm.bingMap.transform.position.x - stc.transform.localScale.x / 2, stc.mapTimeToY(sliceTime) + direction * 0.015f, sm.bingMap.transform.position.z - stc.transform.localScale.z * 0.49f);
        timeSliceLabelPool[i].transform.position = new Vector3(sm.bingMap.transform.position.x - stc.transform.localScale.x / 2, stc.mapTimeToY(sliceTime) + direction * 0.005f, sm.bingMap.transform.position.z - stc.transform.localScale.z * 0.49f);
        timeSliceLabelPool[i].text = text;

        //if(stc.containsTime(sliceTime))
        //{
            timeSliceLabelPool[i].color = Color.black;
        //Color c = queryPrism.GetComponent<Renderer>().material.color;
        //c.a = a;
        ////queryPrism.GetComponent<Renderer>().material.color = c;
        //queryPrism.EnsureComponent<MaterialInstance>().Material.color = c;
        //// https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/rendering/material-instance?view=mrtkunity-2022-05
        //}
        //else
        //{
        //    timeSliceLabelPool[i].color = Color.grey;
        //}

        if (sm.VirtualDesk)
            timeSliceLabelPool[i].fontSize = deskScaleFontSize;
        else if (sm.ClippedEgoRoom)
            timeSliceLabelPool[i].fontSize = roomScaleFontSize;

    }

    void UpdatePools(int n)
    {
        GameObject newTimeSlice;
        LineRenderer newTimeSliceLine;
        TextMesh newTimeSliceLabel;

        for(int i = 0; i < n; i++)
        { 
            if (i < timeSlicePool.Count)
            {
                if (!timeSlicePool[i].activeSelf)
                    timeSlicePool[i].SetActive(true);
                if (!timeSliceLinePool[i].gameObject.activeSelf)
                    timeSliceLinePool[i].gameObject.SetActive(true);
                if (!timeSliceLabelPool[i].gameObject.activeSelf)
                    timeSliceLabelPool[i].gameObject.SetActive(true);
            }
            else
            {
                newTimeSlice = GameObject.Instantiate(timeSlicePrefab, this.transform);
                if (white)
                    newTimeSlice.GetComponent<Renderer>().material = whiteMaterial;
                else
                    newTimeSlice.GetComponent<Renderer>().material = greyMaterial;
                white = !white;

                timeSlicePool.Add(newTimeSlice);


                newTimeSliceLine = GameObject.Instantiate(timeSliceLinePrefab, this.transform);
                timeSliceLinePool.Add(newTimeSliceLine);

                newTimeSliceLabel = GameObject.Instantiate(timeSliceLabelPrefab, this.transform);
                timeSliceLabelPool.Add(newTimeSliceLabel);
            }
        }

        if (timeSlicePool.Count > n)
        {
            for(int i = n; i < timeSlicePool.Count; i++)
            {
                if (timeSlicePool[i].activeSelf)
                    timeSlicePool[i].SetActive(false);
                if (timeSliceLinePool[i].gameObject.activeSelf)
                    timeSliceLinePool[i].gameObject.SetActive(false);
                if (timeSliceLabelPool[i].gameObject.activeSelf)
                    timeSliceLabelPool[i].gameObject.SetActive(false);
            }
        }
    }


    public bool interactibleAreaContainsTime(DateTime t)
    {
        if (stc.timeDirection == Direction.LatestOnTop)
        {
            if (stc.onTop.CompareTo(t) >= 0) return true;
            else return false;
        }
        else
        {
            if (stc.onTop.CompareTo(t) <= 0) return true;
            else return false;
        }
    }


    public void PanTime(float delta)
    {
        stc.transform.position += new Vector3(0f, delta, 0f);
        stc.baseHeight += delta;

        UpdateTimeSelectorWidgetsAndLabels();
    }


     public void RescaleTime(float factor, float centerOfInspection)
     {
        if (!(factor > 1.001 || factor < 0.999)) return;
        if (factor < 0.8 || factor > 1.1) return;


        if (stc.secondsPerMeter / factor < stc.minAllowedSecondsPerMeter || stc.secondsPerMeter / factor > stc.maxAllowedSecondsPerMeter)
            return;

        //this.centerOfInspection = centerOfInspection;
        DateTime centerTimeOfInspection = stc.mapYToTime(centerOfInspection);



        stc.secondsPerMeter = stc.secondsPerMeter / factor;
        float delta_y;

            if (stc.timeDirection == Direction.LatestOnTop)
            {
                delta_y = (float)centerTimeOfInspection.Subtract(stc.mapYToTime(stc.baseHeight)).TotalSeconds / stc.secondsPerMeter;

                //if (((centerOfInspection - y) <= initialBaseHeight && centerTimeOfInspection.CompareTo(onTop) < 0) || !stc.glue)
                //{
                    stc.baseHeight = centerOfInspection - delta_y;
                //}
                //else
                //{
                //    baseHeight = initialBaseHeight;
                //}
            }
            else
            {
                delta_y = (float)stc.mapYToTime(stc.baseHeight).Subtract(centerTimeOfInspection).TotalSeconds / stc.secondsPerMeter;

                //if (((centerOfInspection - y) <= initialBaseHeight && centerTimeOfInspection.CompareTo(onTop) > 0) || !stc.glue)
                //{
                    stc.baseHeight = centerOfInspection - delta_y;
                //}
                //else
                //{
                //    baseHeight = initialBaseHeight;
                //}
            }



        //dm.transform.position -= new Vector3(0f, delta_y, 0f);
        stc.transform.position = new Vector3(stc.transform.position.x, stc.baseHeight, stc.transform.position.z);
        stc.transform.localScale = new Vector3(stc.transform.localScale.x, stc.transform.localScale.y * factor, stc.transform.localScale.z);


        UpdateTimeSelectorWidgetsAndLabels();


    }


    public void SuperRescaleTime(float factor, float centerOfInspection)
    {
        if (stc.secondsPerMeter / factor < stc.minAllowedSecondsPerMeter || stc.secondsPerMeter / factor > stc.maxAllowedSecondsPerMeter)
            return;

        DateTime centerTimeOfInspection = stc.mapYToTime(centerOfInspection);


        stc.secondsPerMeter = stc.secondsPerMeter / factor;
        float delta_y;

        if (stc.timeDirection == Direction.LatestOnTop)
        {
            delta_y = (float)centerTimeOfInspection.Subtract(stc.mapYToTime(stc.baseHeight)).TotalSeconds / stc.secondsPerMeter;

            //if (((centerOfInspection - y) <= initialBaseHeight && centerTimeOfInspection.CompareTo(onTop) < 0) || !stc.glue)
            //{
            stc.baseHeight = centerOfInspection - delta_y;
            //}
            //else
            //{
            //    baseHeight = initialBaseHeight;
            //}
        }
        else
        {
            delta_y = (float)stc.mapYToTime(stc.baseHeight).Subtract(centerTimeOfInspection).TotalSeconds / stc.secondsPerMeter;

            //if (((centerOfInspection - y) <= initialBaseHeight && centerTimeOfInspection.CompareTo(onTop) > 0) || !stc.glue)
            //{
            stc.baseHeight = centerOfInspection - delta_y;
            //}
            //else
            //{
            //    baseHeight = initialBaseHeight;
            //}
        }

        stc.transform.position = new Vector3(stc.transform.position.x, stc.baseHeight, stc.transform.position.z);
        stc.transform.localScale = new Vector3(stc.transform.localScale.x, stc.transform.localScale.y * factor, stc.transform.localScale.z);


        UpdateTimeSelectorWidgetsAndLabels();

        RecomputeTimeGranularity();
        previousSTCheight = stc.transform.localScale.y;
    }

    public bool TimeSelectorWidgetsMoved()
    {
        if (previousTimeMinRegSelectorWidgetPos != timeMinRegularSelectorWidget.transform.position ||
            previousTimeMaxRegSelectorWidgetPos != timeMaxRegularSelectorWidget.transform.position)
            return true;
        else
            return false;
    }

    public void UpdateTimeSelectorWidgetsAndLabels()
    {

        timeMinRegularSelectorWidget.transform.position = new Vector3(timeMinRegularSelectorWidget.transform.position.x, 
                                                                      stc.mapTimeToY(userDefinedMinTime), 
                                                                      timeMinRegularSelectorWidget.transform.position.z);
        timeMinRegularSelectorWidget.transform.localScale = new Vector3(timeWidgetDiameter / transform.localScale.x,
                                                                        timeWidgetDiameter / transform.localScale.y,
                                                                        timeWidgetDiameter / transform.localScale.z);

        timeMaxRegularSelectorWidget.transform.position = new Vector3(timeMaxRegularSelectorWidget.transform.position.x, 
                                                                      stc.mapTimeToY(userDefinedMaxTime), 
                                                                      timeMaxRegularSelectorWidget.transform.position.z);
        timeMaxRegularSelectorWidget.transform.localScale = timeMinRegularSelectorWidget.transform.localScale;

        UpdateTimeSelectorLabels();

        previousTimeMinRegSelectorWidgetPos = timeMinRegularSelectorWidget.transform.position;
        previousTimeMaxRegSelectorWidgetPos = timeMaxRegularSelectorWidget.transform.position;
    }

    public void UpdateTimeSelectorLabels()
    {
        timeMinRegularSelectorLabel.transform.position = timeMinRegularSelectorWidget.transform.position + new Vector3(0, 0, 0.03f);
        timeMinRegularSelectorLabel.transform.localScale = new Vector3(1f / transform.localScale.z,
                                                                        timeMinRegularSelectorLabel.transform.localScale.y,
                                                                        timeMinRegularSelectorLabel.transform.localScale.z);

        timeMaxRegularSelectorLabel.transform.position = timeMaxRegularSelectorWidget.transform.position + new Vector3(0, 0, 0.03f);
        timeMaxRegularSelectorLabel.transform.localScale = timeMinRegularSelectorLabel.transform.localScale;

        timeMinRegularSelectorLabel.text = "Start Time:\n" + stc.mapYToTime(timeMinRegularSelectorWidget.transform.position.y).ToString("f", culture);
        timeMaxRegularSelectorLabel.text = "End Time:\n" + stc.mapYToTime(timeMaxRegularSelectorWidget.transform.position.y).ToString("f", culture);

        if (sm.VirtualDesk)
        {
            timeMinRegularSelectorLabel.fontSize = deskScaleFontSize;
            timeMaxRegularSelectorLabel.fontSize = deskScaleFontSize;
        }
        else if (sm.ClippedEgoRoom)
        {
            timeMinRegularSelectorLabel.fontSize = roomScaleFontSize;
            timeMaxRegularSelectorLabel.fontSize = roomScaleFontSize;
        }

        previousTimeMinRegSelectorWidgetPos = timeMinRegularSelectorWidget.transform.position;
        previousTimeMaxRegSelectorWidgetPos = timeMaxRegularSelectorWidget.transform.position;
    }

    public void Reinitialize()
    {
        if(!wallsV2)
        { 
            int oldnumlines = lines.Count;

            foreach (TextMesh line in lines)
            {
                Destroy(line.gameObject);
            }

            lines.Clear();

            for (int i = 0; i < numLines; i++)
            {
                GameObject newLine;
                if (sm.VirtualDesk)
                    newLine = Instantiate(tripleLinePrefab).gameObject;
                else
                    newLine = Instantiate(quadrupleLinePrefab).gameObject;
                newLine.name = newLine.name + "(" + i + ")";
                newLine.transform.SetParent(this.transform);
                newLine.GetComponent<TextMesh>().transform.localScale = new Vector3(-1, 3, 1);   
                newLine.transform.localPosition = new Vector3(0f, 0f + i * spacing, 0f);
                lines.Add(newLine.GetComponent<TextMesh>());
            }





            
        }

        this.transform.position = sm.bingMap.transform.position + new Vector3(0f, sm.bingMap.mapRenderer.MapBaseHeight, 0f);

        this.transform.localScale = new Vector3(sm.bingMap.mapRenderer.LocalMapDimension.x, this.transform.localScale.y, sm.bingMap.mapRenderer.LocalMapDimension.y);



        stc.baseHeight = this.transform.position.y;

        stc.transform.position = new Vector3(stc.transform.position.x, stc.baseHeight, stc.transform.position.z);

        UpdateTimeSelectorWidgetsAndLabels();

        if (sm.VirtualDesk)
        {
            fourthCollider.enabled = false;
        }
        else if (sm.ClippedEgoRoom)
        {
            if (sm.EgoRoomIsFourWalled)
                fourthCollider.enabled = true;
            else
                fourthCollider.enabled = false;
        }


         this.Refresh();
    }



    void RecomputeTimeGranularity()
    {
        TimeSlicesGranularity previousGranularity = currentGranularity;
       
        if ((12 * 30f * 24f * 3600f / stc.secondsPerMeter) < 0.5f * stc.transform.localScale.y && TimeSlicesGranularity.Year <= maxGranularity && TimeSlicesGranularity.Year >= minGranularity)
        {
            currentGranularity = TimeSlicesGranularity.Year;
        }
        else if ((30f * 24f * 3600f / stc.secondsPerMeter) < 0.5f && TimeSlicesGranularity.Month <= maxGranularity && TimeSlicesGranularity.Month >= minGranularity)
        {
            currentGranularity = TimeSlicesGranularity.Month;
        }
        else if ((7f * 24f * 3600f / stc.secondsPerMeter) < 0.5f  && TimeSlicesGranularity.Week <= maxGranularity && TimeSlicesGranularity.Week >= minGranularity)
        {
            currentGranularity = TimeSlicesGranularity.Week;
        }
        else if ((24f * 3600f / stc.secondsPerMeter) < 0.5f  && TimeSlicesGranularity.Day <= maxGranularity && TimeSlicesGranularity.Day >= minGranularity)
        {
            currentGranularity = TimeSlicesGranularity.Day;
        }
        else if ((12f * 3600f / stc.secondsPerMeter) < 0.5  && TimeSlicesGranularity.SixHours <= maxGranularity && TimeSlicesGranularity.SixHours >= minGranularity)
        {
            currentGranularity = TimeSlicesGranularity.SixHours;
        }
        else if ((6f * 3600f / stc.secondsPerMeter) < 0.5  && TimeSlicesGranularity.Hour <= maxGranularity && TimeSlicesGranularity.Hour >= minGranularity)
        {
            currentGranularity = TimeSlicesGranularity.Hour;
        }
        else if ((1f * 3600f / stc.secondsPerMeter) < 0.5  && TimeSlicesGranularity.HalfHour <= maxGranularity && TimeSlicesGranularity.HalfHour >= minGranularity)
        {
            currentGranularity = TimeSlicesGranularity.HalfHour;
        }
        else if (TimeSlicesGranularity.TenMinutes <= maxGranularity && TimeSlicesGranularity.TenMinutes >= minGranularity)
        {
            currentGranularity = TimeSlicesGranularity.TenMinutes;
        }



    }





    public float DateToSliceHeightAtCurrentGranularity(DateTime d)
    {
        return GranularityToSeconds(currentGranularity, d) / stc.secondsPerMeter;
    }

    public float DateToSliceCenterAtCurrentGranularity(DateTime d)
    {
        float direction;
        if (stc.timeDirection == Direction.LatestOnBottom)
            direction = -1;
        else
            direction = 1;
        float sliceBeginning = 0;
        switch (currentGranularity)
        {
            case TimeSlicesGranularity.TenMinutes: sliceBeginning = stc.mapTimeToY(new DateTime(d.Year, d.Month, d.Day, d.Hour, RoundToLastTenMinutesMark(d.Minute), 0)); break;
            case TimeSlicesGranularity.HalfHour: sliceBeginning = stc.mapTimeToY(new DateTime(d.Year, d.Month, d.Day, d.Hour, RoundToLastHalfHoursMark(d.Minute), 0)); break;
            case TimeSlicesGranularity.Hour: sliceBeginning = stc.mapTimeToY(new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0)); break;
            case TimeSlicesGranularity.SixHours: sliceBeginning = stc.mapTimeToY(new DateTime(d.Year, d.Month, d.Day, RoundToLastSixHoursMark(d.Hour), 0, 0)); break;
            case TimeSlicesGranularity.Day: sliceBeginning = stc.mapTimeToY(new DateTime(d.Year, d.Month, d.Day, 0, 0, 0)); break;
            case TimeSlicesGranularity.Week:  sliceBeginning = stc.mapTimeToY(new DateTime(d.Year, d.Month, d.Day, 0, 0, 0).AddDays(-(int)d.DayOfWeek)); break;
            case TimeSlicesGranularity.Month: sliceBeginning = stc.mapTimeToY(new DateTime(d.Year, d.Month, 1, 0, 0, 0)); break;
            case TimeSlicesGranularity.Year: sliceBeginning = stc.mapTimeToY(new DateTime(d.Year, 1, 1, 0, 0, 0)); break;
        }

        return sliceBeginning + direction * DateToSliceHeightAtCurrentGranularity(d) / 2;
    }

    public float GranularityToSeconds(TimeSlicesGranularity g, DateTime d)
    {
        switch (g)
        {
            case TimeSlicesGranularity.TenMinutes: return 600;
            case TimeSlicesGranularity.HalfHour: return 1800;
            case TimeSlicesGranularity.Hour: return 3600;
            case TimeSlicesGranularity.SixHours: return 6 * 3600;
            case TimeSlicesGranularity.Day: return 24 * 3600;
            case TimeSlicesGranularity.Week: return 7 * 24 * 3600;
            case TimeSlicesGranularity.Month: return DateTime.DaysInMonth(d.Year, d.Month) * 24 * 3600;
            case TimeSlicesGranularity.Year: return 365 * 24 * 3600;  
            default: return 0;
        }
    }

    internal int RoundToLastTenMinutesMark(int minutes)
    {
        return (int)Mathf.Floor((float)minutes / 10) * 10;
    }

    internal int RoundToLastSixHoursMark(int hours)
    {
        return (int)Mathf.Floor((float)hours / 6) * 6;
    }

    internal int RoundToLastHalfHoursMark(int minutes)
    {
        if (minutes >= 30) return 30;
        else return 0;
    }


    

    public void SwitchTimeInspectorPlanes()
    {
        leftTimeInspector.showCuttingPlane = !leftTimeInspector.showCuttingPlane;
        rightTimeInspector.showCuttingPlane = !rightTimeInspector.showCuttingPlane;
    }



    public static RenderTexture ClearFilterTexture(RenderTexture renderTexture)
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = rt;
        return renderTexture;
    }
}
