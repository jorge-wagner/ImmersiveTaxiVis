using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static AttributeConstraintManager;
//using static UnityEngine.Rendering.DebugUI;

public class AttributeConstraintManager : MonoBehaviour
{
    public enum Attribute { TripFare, TripTip, TripDistance, TripDuration};

    public QueryManager qm;

    public int numberOfBins = 10;
    private float previousNumberOfBins = -1;

    //public Dictionary<Attribute, string> attributeDataColumns = new Dictionary<Attribute, string>();
    public Dictionary<Attribute, string> attributeTitles;
    public Dictionary<Attribute, string[]> attributeBinLabels;
    public Dictionary<Attribute, float[]> attributeTripsPerBin;
    public Dictionary<Attribute, float> attributeMinValue;
    public Dictionary<Attribute, float> attributeMaxValue;
    public Dictionary<Attribute, float> attributeUserDefinedMinValue;
    public Dictionary<Attribute, float> attributeUserDefinedMaxValue;
    public Dictionary<Attribute, BasicHistogram> attributeHistograms;
    public Dictionary<Attribute, Dictionary<int, float>> attributeDictionaries;


    public bool constraintsChanged = false;
    public bool isConstraining = false;
    public bool needsTextureUpdate = false;
    public RenderTexture filterTexture;
    public Texture2D filterTextureAsTex2D;

    // Start is called before the first frame update
    void Start()
    {
        Time.fixedDeltaTime = 0.5f;       
    }

    private void FixedUpdate()
    {
        if(numberOfBins != previousNumberOfBins && qm.texSize != 0)
        {
            if(!dictionariesReady)
                SetupDictionaries();
            else
            { 
                FindAttributesMinMaxValues();
                ResetUserDefinedMinMaxValues();
                ResetAttributeBins();
                PopulateAttributeBinsBasedOnFullData();
                UpdateBinLabels();
                PlotAttributeHistograms();

                filterTexture = new RenderTexture(qm.texSize, qm.texSize, 24);
                filterTexture.enableRandomWrite = true;
                filterTexture.filterMode = FilterMode.Point;
                filterTexture.Create();
                filterTextureAsTex2D = new Texture2D(qm.texSize, qm.texSize);

                previousNumberOfBins = numberOfBins;
            }
        }

        if(needsTextureUpdate && (Time.time - lastSignalForTextureUpdate) > qm.MinInactiveIntervalBeforeUpdating)
        {
            UpdateFilterTexture();
            needsTextureUpdate = false;
        }

        if (needsHistogramUpdate)// && (Time.time - lastSignalForHistogramUpdate) > qm.InactiveDelayBeforeUpdating)
        {
            if (qm.verbose) Debug.Log("Starting ACM histogram update. " + qm.LogTimeSincePreviousLog().ToString());

            ResetAttributeBins();

            if (qm.verbose) Debug.Log("Reset ACM attribute beans complete. " + qm.LogTimeSincePreviousLog().ToString());

            if (qm.allQueries.Count > 0 || isConstraining || qm.IsBrushing())
                PopulateAttributeBinsBasedOnCurrentQueries();
            else
                PopulateAttributeBinsBasedOnFullData();

            if (qm.verbose) Debug.Log("Completed populating ACM attribute histogram bins. " + qm.LogTimeSincePreviousLog().ToString());

            PlotAttributeHistograms();

            if (qm.verbose) Debug.Log("Completed plotting ACM attribute histograms. " + qm.LogTimeSincePreviousLog().ToString());

            needsHistogramUpdate = false;
        }
    }

    bool dictionariesReady = false;
    void SetupDictionaries()
    {
        if (((ODSTCManager)qm.sm.stc).tripDurationsInMinutes.Count != qm.sm.stc.csvdata.DataCount)
            return;

        //attributeDataColumns = new Dictionary<Attribute, string>();
        attributeTitles = new Dictionary<Attribute, string>();
        attributeDictionaries = new Dictionary<Attribute, Dictionary<int, float>>();
        attributeBinLabels = new Dictionary<Attribute, string[]>();
        attributeTripsPerBin = new Dictionary<Attribute, float[]>();
        attributeMinValue = new Dictionary<Attribute, float>();
        attributeMaxValue = new Dictionary<Attribute, float>();
        attributeUserDefinedMinValue = new Dictionary<Attribute, float>();
        attributeUserDefinedMaxValue = new Dictionary<Attribute, float>();
        attributeHistograms = new Dictionary<Attribute, BasicHistogram>();

        attributeDictionaries.Add(Attribute.TripTip, ((ODSTCManager)qm.sm.stc).tripTipAmounts);
        attributeDictionaries.Add(Attribute.TripFare, ((ODSTCManager)qm.sm.stc).tripFareAmounts);
        attributeDictionaries.Add(Attribute.TripDistance, ((ODSTCManager)qm.sm.stc).tripDistances);
        attributeDictionaries.Add(Attribute.TripDuration, ((ODSTCManager)qm.sm.stc).tripDurationsInMinutes);

        //attributeDataColumns.Add(Attribute.TripTip, "tip_amount");
        //attributeDataColumns.Add(Attribute.TripFare, "fare_amount");
        //attributeDataColumns.Add(Attribute.TripDistance, "trip_distance");
        //attributeDataColumns.Add(Attribute.TripDuration, ""); // for the time being, trip duration does not have a column in the dataset - it should be computed from start and end times

        attributeTitles.Add(Attribute.TripTip, "Tip amount");
        attributeTitles.Add(Attribute.TripFare, "Fare amount");
        attributeTitles.Add(Attribute.TripDistance, "Distance in miles");
        attributeTitles.Add(Attribute.TripDuration, "Duration in minutes");

        foreach (Attribute attribute in (Attribute[])Enum.GetValues(typeof(Attribute)))
        {
            attributeMinValue.Add(attribute, 0); // it should be a large number if we wanted to compute the real min, but let's fixate it always at 0
            attributeMaxValue.Add(attribute, 0);
            attributeTripsPerBin.Add(attribute, new float[numberOfBins]);
            attributeBinLabels.Add(attribute, new string[numberOfBins]);
            if (GameObject.Find(attribute.ToString() + " Histogram"))
                attributeHistograms.Add(attribute, GameObject.Find(attribute.ToString() + " Histogram").GetComponent<BasicHistogram>());
        }

        dictionariesReady = true;
    }



    float lastSignalForTextureUpdate = 0;
    public void SignalNeedForTextureUpdate()
    {
        needsTextureUpdate = true;
        lastSignalForTextureUpdate = Time.time;
    }

    bool needsHistogramUpdate = false;
    float lastSignalForHistogramUpdate = 0;
    public void SignalNeedForHistogramUpdatesBasedOnQueries()
    {
        needsHistogramUpdate = true;
        lastSignalForHistogramUpdate = Time.time;
    }
    public void SignalImmediateNeedForHistogramUpdatesBasedOnQueries()
    {
        needsHistogramUpdate = true;
        lastSignalForHistogramUpdate = Time.time - qm.MinInactiveIntervalBeforeUpdating;
    }

    public void UpdateFilterTexture()
    {
        bool atLeastOneFilteredOut = false;

        if (qm.verbose) Debug.Log("Starting ACM texture update. " + qm.LogTimeSincePreviousLog().ToString());

        //RenderTexture rt = RenderTexture.active;
        //RenderTexture.active = filterTexture;
        //filterTextureAsTex2D.ReadPixels(new Rect(0, 0, filterTexture.width, filterTexture.height), 0, 0, false);
        //filterTextureAsTex2D.Apply();
        //RenderTexture.active = rt;

        for (int x = 0; x < qm.texSize; x++)
        {
            for (int y = 0; y < qm.texSize; y++)
            {
                int index = x + y * qm.texSize;
                if (index >= qm.sm.stc.csvdata.DataCount)
                    continue;

                bool pointIsFilteredOut = false;

                foreach (Attribute attribute in (Attribute[])Enum.GetValues(typeof(Attribute)))
                {
                    float value;
                    //if (attributeDataColumns[attribute] != "")
                    //    value = (float)(qm.sm.stc.csvdata.getOriginalValuePrecise(qm.sm.stc.csvdata[attributeDataColumns[attribute]].Data[index], attributeDataColumns[attribute]));
                    //else // trip duration 
                    //{
                    //    TimeSpan tripDuration = (d_time - o_time);
                    //    value = (float)tripDuration.TotalMinutes;
                    //}
                    value = attributeDictionaries[attribute][index];

                    if (value > attributeUserDefinedMaxValue[attribute] || value < attributeUserDefinedMinValue[attribute])
                    {
                        pointIsFilteredOut = true;
                        break;
                    }
                }

                // HANDLE TIME MIN MAX CONSTRAINTS DEFINED THROUGH THE STC WALLS WIDGETS
                if(!pointIsFilteredOut && qm.sm.stc.walls.allowTimeMinMaxConstraining && qm.sm.stc.walls.isTimeMinMaxConstraining)
                {
                    if (((ODSTCManager)qm.sm.stc).tripDestinationTimes[index].CompareTo(qm.sm.stc.walls.userDefinedMinTime) < 0 || 
                        ((ODSTCManager)qm.sm.stc).tripOriginTimes[index].CompareTo(qm.sm.stc.walls.userDefinedMaxTime) > 0)
                        pointIsFilteredOut = true;
                }
                //

                if (pointIsFilteredOut)
                {
                    filterTextureAsTex2D.SetPixel(x, y, Color.black);
                    atLeastOneFilteredOut = true;
                }
                else
                {
                    filterTextureAsTex2D.SetPixel(x, y, Color.red);
                }           
            }
        }

        filterTextureAsTex2D.Apply();
        RenderTexture.active = filterTexture;
        Graphics.Blit(filterTextureAsTex2D, filterTexture);

        if (atLeastOneFilteredOut)
            isConstraining = true;
        else
            isConstraining = false;

        constraintsChanged = true;

        if (qm.verbose) Debug.Log("Completed ACM texture update. " + qm.LogTimeSincePreviousLog().ToString());
    }


    public int MapAttributeValueToBinNumber(Attribute attribute, float value)
    {
        float diffOverMinValue = value - attributeMinValue[Attribute.TripTip];
        float valuePerBin = ((attributeMaxValue[attribute] - attributeMinValue[attribute]) / numberOfBins);

        return Math.Min(numberOfBins - 1, Math.Max(0, Mathf.FloorToInt(diffOverMinValue / valuePerBin)));
    }

    public float MapBinNumberToStartValue(Attribute attribute, int binNumber)
    {
        return attributeMinValue[attribute] + binNumber * ((attributeMaxValue[attribute] - attributeMinValue[attribute]) / numberOfBins);
    }

    public void ResetAttributeBins()
    {
        foreach(Attribute attribute in (Attribute[])Enum.GetValues(typeof(Attribute)))
        {
            attributeTripsPerBin[attribute] = new float[numberOfBins];
        }
    }

    public void FindAttributesMinMaxValues()
    {
        for (int x = 0; x < qm.texSize; x++)
        {
            for (int y = 0; y < qm.texSize; y++)
            {
                int index = x + y * qm.texSize;
                if (index >= qm.sm.stc.csvdata.DataCount)
                    continue;

                foreach (Attribute attribute in (Attribute[])Enum.GetValues(typeof(Attribute)))
                {
                    float value = attributeDictionaries[attribute][index];
                    //if (attributeDataColumns[attribute] != "")
                    //    value = (float)(qm.sm.stc.csvdata.getOriginalValuePrecise(qm.sm.stc.csvdata[attributeDataColumns[attribute]].Data[index], attributeDataColumns[attribute]));
                    //else // trip duration 
                    //{
                    //    DateTime o_time = (DateTime)qm.sm.stc.csvdata.getOriginalValue(qm.sm.stc.csvdata["pickup_datetime"].Data[index], "pickup_datetime");
                    //    DateTime d_time = (DateTime)qm.sm.stc.csvdata.getOriginalValue(qm.sm.stc.csvdata["dropoff_datetime"].Data[index], "dropoff_datetime");
                    //    TimeSpan tripDuration = (d_time - o_time);
                    //    value = (float)tripDuration.TotalMinutes;
                    //}

                    if (value > attributeMaxValue[attribute])
                        attributeMaxValue[attribute] = value;
                    //if (value < attributeMinValue[attribute])
                    //    attributeMinValue[attribute] = value;

                }
            }
        }
    }

    public void ResetUserDefinedMinMaxValues()
    {
        foreach (Attribute attribute in (Attribute[])Enum.GetValues(typeof(Attribute)))
        {
            attributeUserDefinedMinValue[attribute] = attributeMinValue[attribute];
            attributeUserDefinedMaxValue[attribute] = attributeMaxValue[attribute];
        }
    }

    public void PopulateAttributeBinsBasedOnCurrentQueries()
    {
        for (int x = 0; x < qm.texSize; x++)
        {
            for (int y = 0; y < qm.texSize; y++)
            {
                int index = x + y * qm.texSize;
                if (index >= qm.sm.stc.csvdata.DataCount)
                    continue;

                //
                if(qm.linkedViewFilterTex2D.GetPixel(x, y).r != 1f)
                    continue;
                //

                foreach (Attribute attribute in (Attribute[])Enum.GetValues(typeof(Attribute)))
                {
                    float value = attributeDictionaries[attribute][index];
                    //if (attributeDataColumns[attribute] != "")
                    //    value = (float)(qm.sm.stc.csvdata.getOriginalValuePrecise(qm.sm.stc.csvdata[attributeDataColumns[attribute]].Data[index], attributeDataColumns[attribute]));
                    //else // trip duration 
                    //{
                    //    DateTime o_time = (DateTime)qm.sm.stc.csvdata.getOriginalValue(qm.sm.stc.csvdata["pickup_datetime"].Data[index], "pickup_datetime");
                    //    DateTime d_time = (DateTime)qm.sm.stc.csvdata.getOriginalValue(qm.sm.stc.csvdata["dropoff_datetime"].Data[index], "dropoff_datetime");
                    //    TimeSpan tripDuration = (d_time - o_time);
                    //    value = (float)tripDuration.TotalMinutes;
                    //}
                    
                    //Debug.Log("Attribute: " + attribute.ToString() + " Value: " + value.ToString() + " Min: " +attributeMinValue[attribute] + " Max: " + attributeMaxValue[attribute] + " Bin number" + MapAttributeValueToBinNumber(attribute, value).ToString());

                    attributeTripsPerBin[attribute][MapAttributeValueToBinNumber(attribute, value)]++;

                }
            }
        }
    }

    public void PopulateAttributeBinsBasedOnFullData()
    {
        for (int x = 0; x < qm.texSize; x++)
        {
            for (int y = 0; y < qm.texSize; y++)
            {
                int index = x + y * qm.texSize;
                if (index >= qm.sm.stc.csvdata.DataCount)
                    continue;

                foreach (Attribute attribute in (Attribute[])Enum.GetValues(typeof(Attribute)))
                {
                    float value = attributeDictionaries[attribute][index];
                    //if (attributeDataColumns[attribute] != "")
                    //    value = (float)(qm.sm.stc.csvdata.getOriginalValuePrecise(qm.sm.stc.csvdata[attributeDataColumns[attribute]].Data[index], attributeDataColumns[attribute]));
                    //else // trip duration 
                    //{
                    //    DateTime o_time = (DateTime)qm.sm.stc.csvdata.getOriginalValue(qm.sm.stc.csvdata["pickup_datetime"].Data[index], "pickup_datetime");
                    //    DateTime d_time = (DateTime)qm.sm.stc.csvdata.getOriginalValue(qm.sm.stc.csvdata["dropoff_datetime"].Data[index], "dropoff_datetime");
                    //    TimeSpan tripDuration = (d_time - o_time);
                    //    value = (float)tripDuration.TotalMinutes;
                    //}

                    //Debug.Log("Attribute: " + attribute.ToString() + " Value: " + value.ToString() + " Min: " +attributeMinValue[attribute] + " Max: " + attributeMaxValue[attribute] + " Bin number" + MapAttributeValueToBinNumber(attribute, value).ToString());

                    attributeTripsPerBin[attribute][MapAttributeValueToBinNumber(attribute, value)]++;

                }
            }
        }
    }

    void UpdateBinLabels()
    {
        foreach (Attribute attribute in (Attribute[])Enum.GetValues(typeof(Attribute)))
        {
            attributeBinLabels[attribute] = new string[numberOfBins];

            for (int i = 0; i < numberOfBins; i++)
            {
                if (i == numberOfBins - 1)
                    attributeBinLabels[attribute][i] = "[" + MapBinNumberToStartValue(attribute, i).ToString("N1") + "," + attributeMaxValue[attribute].ToString("N1") + "]";
                else
                    attributeBinLabels[attribute][i] = "[" + MapBinNumberToStartValue(attribute, i).ToString("N1") + "," + (MapBinNumberToStartValue(attribute, i + 1) - 0.1f).ToString("N1") + "]";
            }
        }
    }

    public void PlotAttributeHistograms()
    {

        foreach (Attribute attribute in (Attribute[])Enum.GetValues(typeof(Attribute)))
        {
            if(attributeHistograms.ContainsKey(attribute))
                attributeHistograms[attribute].UpdateGraph(attribute, attributeTitles[attribute], "Frequency", attributeMinValue[attribute], attributeMaxValue[attribute], attributeBinLabels[attribute], attributeTripsPerBin[attribute]);
        }
    }

    public void SetUserDefinedMinValue(Attribute attribute, float value)
    {
        if (!dictionariesReady || !attributeUserDefinedMinValue.ContainsKey(attribute))
            return;

        if (attributeUserDefinedMinValue[attribute] != value)
        {
            attributeUserDefinedMinValue[attribute] = value;
            SignalNeedForTextureUpdate();
        }
    }

    public void SetUserDefinedMaxValue(Attribute attribute, float value)
    {
        if (!dictionariesReady || !attributeUserDefinedMaxValue.ContainsKey(attribute))
            return;

        if (attributeUserDefinedMaxValue[attribute] != value)
        {
            attributeUserDefinedMaxValue[attribute] = value;
            SignalNeedForTextureUpdate();
        }
    }

}
