using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QueryReporter : MonoBehaviour
{
    public QueryManager qm;

    public int numberOfBins = 10;
    public bool reportingFullDataStats = false;
    public bool useTimeAtMiddleOfBin = true;
    public bool fixedStartingResolution = true;

    public TMPro.TMP_Text numBinsLabel;
    public PinchSlider numBinsSlider;

    public float secondsPerBin = -1;
    private float previousNumberOfBins = -1;

    private float timeLastBinCountChange = 0;
    private bool needsBinningUpdate = false;

    public DateTime[] binLabelTimes;

    public TimeSeriesGraph numTripsPerTime;
    public TimeSeriesGraph avgTotalAmountPerTime;
    public TimeSeriesGraph avgDistancePerTime;
    public TimeSeriesGraph avgDurationPerTime;

    public EmbeddedTimeSeriesGraph numTripsPerTimeEmbedded;
    public EmbeddedTimeSeriesGraph avgTotalAmountPerTimeEmbedded;
    public EmbeddedTimeSeriesGraph avgDistancePerTimeEmbedded;
    public EmbeddedTimeSeriesGraph avgDurationPerTimeEmbedded;


    // Start is called before the first frame update
    void Start()
    {
        Time.fixedDeltaTime = 2.5f;
    }


    void FixedUpdate()
    {
        if(secondsPerBin == -1)
        {
            if(fixedStartingResolution)
            {
                numberOfBins = Mathf.RoundToInt((float)((qm.sm.stc.maxTime - qm.sm.stc.minTime).TotalSeconds) / 3600f);
                numBinsSlider.SliderValue = (float)numberOfBins / numBinsSlider.SliderStepDivisions;
            }

            secondsPerBin = (float)((qm.sm.stc.maxTime - qm.sm.stc.minTime).TotalSeconds) / numberOfBins;
            UpdateBinLabels();
        }

        if (numberOfBins != previousNumberOfBins)
        {
            needsBinningUpdate = true;
            timeLastBinCountChange = Time.time;

            previousNumberOfBins = numberOfBins;
            secondsPerBin = (float)((qm.sm.stc.maxTime - qm.sm.stc.minTime).TotalSeconds) / numberOfBins;
            numBinsLabel.text = "Num Bins: " + numberOfBins.ToString() + "  (" + (secondsPerBin / 3600f).ToString("N1") + "h per bin)";

            //qm.acm.SignalNeedForTextureUpdate();
        }

        if (needsBinningUpdate && (Time.time - timeLastBinCountChange) > qm.MinInactiveIntervalBeforeUpdating)
        {
            needsBinningUpdate = false;

            qm.UpdateQueriesStats();
            UpdateBinLabels();

            if (reportingFullDataStats && !qm.acm.isConstraining)
            {
                qm.ComputeFullDataStats();
                UpdateFullDataTimeSeries();
            }
            else if (reportingFullDataStats && qm.acm.isConstraining)
            {
                qm.ComputeFullDataStatsMinusACMConstraints();
                UpdateFullDataTimeSeries();
            }
            else if(qm.allQueries.Count > 0)
            {
                UpdateTimeSeriesBasedOnQueries();
            }
        }

    }

    
    public void UpdateNumberOfBinsViaSlider(SliderEventData eventData)
    {
        numberOfBins = Math.Max(1, Mathf.RoundToInt(eventData.NewValue * eventData.Slider.SliderStepDivisions));
    }

    public int MapTimeToBinNumber(DateTime time)
    {
        float secondsSinceSTCMinTime = (float)((time - qm.sm.stc.minTime).TotalSeconds);
        int binNumber = Mathf.FloorToInt(secondsSinceSTCMinTime / secondsPerBin); // returns bin number from 0 to n-1
        binNumber = Math.Max(0, Math.Min(binNumber, numberOfBins - 1));

        //Debug.Log("Seconds since MinTime: " + secondsSinceSTCMinTime.ToString() + " -> Bin number: " + binNumber.ToString());

        return binNumber;
    }

    public DateTime MapBinNumberToStartTime(int binNumber)
    {
        float secondsSinceSTCMinTime = binNumber * secondsPerBin;
        return qm.sm.stc.minTime.AddSeconds(secondsSinceSTCMinTime);
    }

    public DateTime MapBinNumberToMiddleTime(int binNumber)
    {
        float secondsSinceSTCMinTime = binNumber * secondsPerBin + secondsPerBin / 2f;
        return qm.sm.stc.minTime.AddSeconds(secondsSinceSTCMinTime);
    }

    void UpdateBinLabels()
    {
        binLabelTimes = new DateTime[numberOfBins];
        for(int i=0; i<numberOfBins; i++)
        {
            if(useTimeAtMiddleOfBin)
                binLabelTimes[i] = MapBinNumberToMiddleTime(i);
            else
                binLabelTimes[i] = MapBinNumberToStartTime(i);
        }
    }

    public void UpdateTimeSeriesBasedOnQueries()
    {
        int queryCount = qm.allQueries.Count;
        List<Query> brushQueries = new List<Query>();
        if (qm.InODBrushingMode)
        {
            if (qm.leftODBrush.isBrushing && !qm.rightODBrush.isBrushing)
            {
                brushQueries.Add(qm.leftODBrush);
                queryCount++;
            }
            else if (qm.rightODBrush.isBrushing && !qm.leftODBrush.isBrushing)
            {
                brushQueries.Add(qm.rightODBrush);
                queryCount++;
            }
            else if (qm.leftODBrush.isBrushing && qm.rightODBrush.isBrushing)
            {
                brushQueries.Add(qm.ODBrushingDirectionalQuery);
                queryCount++;
            }
        }
        else if (qm.InRegularBrushingMode)
        {
            if(qm.leftRegularBrush.isBrushing)
            {
                brushQueries.Add(qm.leftRegularBrush);
                queryCount++;
            }
            if (qm.rightRegularBrush.isBrushing)
            {
                brushQueries.Add(qm.rightRegularBrush);
                queryCount++;
            }
        }

        float[][] numTripsDataSeries = new float[queryCount][];
        float[][] avgTotalAmountDataSeries = new float[queryCount][];
        float[][] avgDurationDataSeries = new float[queryCount][];
        float[][] avgDistanceDataSeries = new float[queryCount][];

        Color[] queryColors = new Color[queryCount];

        int i = 0;
        //foreach (Query q in qm.allQueries)
        foreach (Query q in qm.allQueries.Concat(brushQueries))
        {
            numTripsDataSeries[i] = q.numTripsPerTimeBin;
            avgTotalAmountDataSeries[i] = q.avgTotalAmountPerTripPerTimeBin;
            avgDurationDataSeries[i] = q.avgDurationPerTripPerTimeBin;
            avgDistanceDataSeries[i] = q.avgDistancePerTrimePerTimeBin;
            queryColors[i] = q.queryColor;
            i++;
        }

        numTripsPerTime.UpdateGraph("Num trips per time" , "Num Trips", qm.sm.stc.minTime, qm.sm.stc.maxTime, binLabelTimes, numTripsDataSeries, queryColors);
        avgTotalAmountPerTime.UpdateGraph("Fare/trip per time", "$ / Trip", qm.sm.stc.minTime, qm.sm.stc.maxTime, binLabelTimes, avgTotalAmountDataSeries, queryColors);
        avgDistancePerTime.UpdateGraph("Distance per trip per time", "Miles", qm.sm.stc.minTime, qm.sm.stc.maxTime, binLabelTimes, avgDistanceDataSeries, queryColors);
        avgDurationPerTime.UpdateGraph("Duration per trip per time", "Minutes", qm.sm.stc.minTime, qm.sm.stc.maxTime, binLabelTimes, avgDurationDataSeries, queryColors);

        numTripsPerTimeEmbedded.UpdateGraph("Num trips per time", "Num Trips", qm.sm.stc.minTime, qm.sm.stc.maxTime, binLabelTimes, numTripsDataSeries, queryColors, qm.sm.stc.timeDirection == STCManager.Direction.LatestOnTop);
        avgTotalAmountPerTimeEmbedded.UpdateGraph("Fare/trip per time", "$ / Trip", qm.sm.stc.minTime, qm.sm.stc.maxTime, binLabelTimes, avgTotalAmountDataSeries, queryColors, qm.sm.stc.timeDirection == STCManager.Direction.LatestOnTop);
        avgDistancePerTimeEmbedded.UpdateGraph("Distance per trip per time", "Miles", qm.sm.stc.minTime, qm.sm.stc.maxTime, binLabelTimes, avgDistanceDataSeries, queryColors, qm.sm.stc.timeDirection == STCManager.Direction.LatestOnTop);
        avgDurationPerTimeEmbedded.UpdateGraph("Duration per trip per time", "Minutes", qm.sm.stc.minTime, qm.sm.stc.maxTime, binLabelTimes, avgDurationDataSeries, queryColors, qm.sm.stc.timeDirection == STCManager.Direction.LatestOnTop);
       
        reportingFullDataStats = false;
    }

    public void UpdateFullDataTimeSeries()
    {
        if (secondsPerBin == -1)
            FixedUpdate();

        float[][] numTripsDataSeries = new float[1][];
        float[][] avgTotalAmountDataSeries = new float[1][];
        float[][] avgDurationDataSeries = new float[1][];
        float[][] avgDistanceDataSeries = new float[1][];
        Color[] queryColors = new Color[1];

        numTripsDataSeries[0] = qm.numTripsPerTimeBin;
        avgTotalAmountDataSeries[0] = qm.avgTotalAmountPerTripPerTimeBin;
        avgDurationDataSeries[0] = qm.avgDurationPerTripPerTimeBin;
        avgDistanceDataSeries[0] = qm.avgDistancePerTrimePerTimeBin;
        queryColors[0] = Color.black;

        
        numTripsPerTime.UpdateGraph("Num trips per time", "Num Trips", qm.sm.stc.minTime, qm.sm.stc.maxTime, binLabelTimes, numTripsDataSeries, queryColors);
        avgTotalAmountPerTime.UpdateGraph("Fare/trip per time", "$ / Trip", qm.sm.stc.minTime, qm.sm.stc.maxTime, binLabelTimes, avgTotalAmountDataSeries, queryColors);
        avgDistancePerTime.UpdateGraph("Distance per trip per time", "Miles", qm.sm.stc.minTime, qm.sm.stc.maxTime, binLabelTimes, avgDistanceDataSeries, queryColors);
        avgDurationPerTime.UpdateGraph("Duration per trip per time", "Minutes", qm.sm.stc.minTime, qm.sm.stc.maxTime, binLabelTimes, avgDurationDataSeries, queryColors);

        //if(!numTripsPerTimeEmbedded.plottingData)
            numTripsPerTimeEmbedded.UpdateGraph("Num trips per time", "Num Trips", qm.sm.stc.minTime, qm.sm.stc.maxTime, binLabelTimes, numTripsDataSeries, queryColors, qm.sm.stc.timeDirection == STCManager.Direction.LatestOnTop);
        //if (!avgTotalAmountPerTimeEmbedded.plottingData)
            avgTotalAmountPerTimeEmbedded.UpdateGraph("Fare/trip per time", "$ / Trip", qm.sm.stc.minTime, qm.sm.stc.maxTime, binLabelTimes, avgTotalAmountDataSeries, queryColors, qm.sm.stc.timeDirection == STCManager.Direction.LatestOnTop);
        //if (!avgDistancePerTimeEmbedded.plottingData)
            avgDistancePerTimeEmbedded.UpdateGraph("Distance per trip per time", "Miles", qm.sm.stc.minTime, qm.sm.stc.maxTime, binLabelTimes, avgDistanceDataSeries, queryColors, qm.sm.stc.timeDirection == STCManager.Direction.LatestOnTop);
        //if (!avgDurationPerTimeEmbedded.plottingData)
            avgDurationPerTimeEmbedded.UpdateGraph("Duration per trip per time", "Minutes", qm.sm.stc.minTime, qm.sm.stc.maxTime, binLabelTimes, avgDurationDataSeries, queryColors, qm.sm.stc.timeDirection == STCManager.Direction.LatestOnTop);


        reportingFullDataStats = true;
    }

    /*public void UpdatePositionAndScaleOfEmbeddedPlots()
    {

    }*/

    public void SwitchGraphTypes()
    {
        numTripsPerTime.SwitchGraphType();
        avgTotalAmountPerTime.SwitchGraphType();
        avgDistancePerTime.SwitchGraphType();
        avgDurationPerTime.SwitchGraphType();

        numTripsPerTimeEmbedded.SwitchGraphType();
        avgTotalAmountPerTimeEmbedded.SwitchGraphType();
        avgDistancePerTimeEmbedded.SwitchGraphType();
        avgDurationPerTimeEmbedded.SwitchGraphType();
    }

}
