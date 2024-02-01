using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Query : MonoBehaviour
{
    public enum QueryType
    {
        Origin, Destination, Either, Directional, Merged
    };

    [Header("Query Settings")]

    public QueryType type;

    public QueryManager qm;

    public Color queryColor;

    public List<DirectionalQuery> associatedDirectionalQueries = new List<DirectionalQuery>();
    public MergedQuery associatedMergedQuery = null;


    [Header("Query Components")]

    public GameObject myButtons; // includes scripts such as AtomicQueryButtonsController and DirectionalQueryButtonsController

    public ToolTip queryStatsTooltip;



    [Header("Selection details")]

    public RenderTexture filterTexture = null;
    public Texture2D filterTextureAsTex2D = null;
    public int numberOfFilteredPoints = 0;

    [Header("Selection stats")]
    public float passengerCountAcc = 0;
    public float totalAmountAcc = 0;
    public float tripDistanceAcc = 0;
    public float tripDurationInMinutesAcc = 0;
    public float tripSpeedInMphAcc = 0;
    public float tripFarePerMileAcc = 0;
    public List<Vector3> tripOriginsWorldScale, tripDestinationsWorldScale;
    public bool RetrievePositions = false;
    public bool GenerateBinnedStats = true;

    public float avgAmount = 0;
    public float avgPassengerCount = 0;
    public float avgDistance = 0;
    public float avgDurationInMinutes = 0;
    public float avgSpeedInMph = 0;
    public float avgFarePerMile = 0;

    [HideInInspector]
    public float[] numTripsPerTimeBin;
    [HideInInspector]
    public float[] avgTotalAmountPerTripPerTimeBin;
    [HideInInspector]
    public float[] avgFarePerMilePerTimeBin;
    [HideInInspector]
    public float[] avgSpeedPerTimeBin;
    [HideInInspector]
    public float[] avgDurationPerTripPerTimeBin;
    [HideInInspector]
    public float[] avgDistancePerTrimePerTimeBin;
    [HideInInspector]
    public float[] avgPassengerCountPerTripPerTimeBin;
 




    abstract public void RecomputeFilterTexture();

    abstract public bool CheckChanges();

    abstract public bool CheckChangesAndResetFlag();

    abstract public bool CheckChangesRecomputeAndResetFlag();

    abstract public void DisableColliders();

    abstract public void EnableColliders();

    abstract public void SetTransparency(float a);

    abstract public void UpdateQueryAfterSTCInteraction();

    abstract public void RemoveQuery();

    abstract public void UpdateStatsTooltipPosition();

    abstract public Vector3 GetCentralPosition2D();

    abstract public Vector3 GetCentralPosition3D();

    abstract public void RefreshColor();

    //abstract public Query Duplicate();

    //abstract public Query Clone();




    public virtual void RecomputeQueryResults()
    {
        RecomputeFilterTexture();
        if (qm.verbose) Debug.Log("Completed recomputing filter texture for this query. " + qm.LogTimeSincePreviousLog().ToString());
        DeductAttributeAndTemporalConstraints();
        if (qm.verbose) Debug.Log("Completed deducting attribute and temporal constraints for this query. " + qm.LogTimeSincePreviousLog().ToString());
        RecomputeQueryStats();
        if (qm.verbose) Debug.Log("Completed recomputing query stats for this query. " + qm.LogTimeSincePreviousLog().ToString());
    }

    public void DeductAttributeAndTemporalConstraints()
    {
        if(qm.acm && qm.acm.isConstraining) // includes time constraints
        {
            //filterTextureAsTex2D = FilterTextureCombiner.CombineTextures2DWithAnd(filterTextureAsTex2D, qm.acm.filterTextureAsTex2D);
            //filterTexture = FilterTextureCombiner.CombineTexturesWithAnd(filterTexture, qm.acm.filterTexture);
            filterTexture = qm.CombineTexturesWithAndUsingCS(filterTexture, qm.acm.filterTexture);
        }
        //if(qm.sm.stc.walls.allowTimeMinMaxConstraining && qm.sm.stc.walls.isTimeMinMaxConstraining)
        //{
        //
        //       }
    }

    public void RecomputeQueryStats()
    {
        numberOfFilteredPoints = 0;
        ResetStatsVars();

        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = filterTexture;
        filterTextureAsTex2D.ReadPixels(new Rect(0, 0, filterTexture.width, filterTexture.height), 0, 0, false);
        filterTextureAsTex2D.Apply();
        RenderTexture.active = rt;

        for (int x = 0; x < filterTextureAsTex2D.width; x++)
        {
            for (int y = 0; y < filterTextureAsTex2D.height; y++)
            {
                if (filterTextureAsTex2D.GetPixel(x, y).r == 1f)
                {
                    numberOfFilteredPoints++;
                    int index = x + y * filterTextureAsTex2D.height;

                    RetrieveStats(index);
                }
            }
        }

        CalculateAvgRelatedStats();

        UpdateStatsTooltip();
    }

    public void HideButtons()
    {
        myButtons.SetActive(false);
    }

    public void RevealButtons()
    {
        myButtons.SetActive(true);
    }

    public void HideTooltip()
    {
        queryStatsTooltip.gameObject.SetActive(false);
    }

    public void RevealTooltip()
    {
        queryStatsTooltip.gameObject.SetActive(true);
    }

    public void ResetStatsVars()
    {
        passengerCountAcc = 0;
        totalAmountAcc = 0;
        tripDistanceAcc = 0;
        tripDurationInMinutesAcc = 0;
        tripSpeedInMphAcc = 0;
        tripFarePerMileAcc = 0;

        if(RetrievePositions)
        {
            if (tripOriginsWorldScale == null)
                tripOriginsWorldScale = new List<Vector3>();
            else
                tripOriginsWorldScale.Clear();
            if (tripDestinationsWorldScale == null)
                tripDestinationsWorldScale = new List<Vector3>();
            else
                tripDestinationsWorldScale.Clear();
        }

        if (GenerateBinnedStats)
        { 
            if(numTripsPerTimeBin == null || numTripsPerTimeBin.Length != qm.qr.numberOfBins)
            {
                numTripsPerTimeBin = new float[qm.qr.numberOfBins];
                avgTotalAmountPerTripPerTimeBin = new float[qm.qr.numberOfBins];
                avgFarePerMilePerTimeBin = new float[qm.qr.numberOfBins];
                avgSpeedPerTimeBin = new float[qm.qr.numberOfBins];
                avgDurationPerTripPerTimeBin = new float[qm.qr.numberOfBins];
                avgDistancePerTrimePerTimeBin = new float[qm.qr.numberOfBins];
                avgPassengerCountPerTripPerTimeBin = new float[qm.qr.numberOfBins];
            }
            else
            {
                Array.Clear(numTripsPerTimeBin, 0, qm.qr.numberOfBins);
                Array.Clear(avgTotalAmountPerTripPerTimeBin, 0, qm.qr.numberOfBins);
                Array.Clear(avgFarePerMilePerTimeBin, 0, qm.qr.numberOfBins);
                Array.Clear(avgSpeedPerTimeBin, 0, qm.qr.numberOfBins);
                Array.Clear(avgDurationPerTripPerTimeBin, 0, qm.qr.numberOfBins);
                Array.Clear(avgDistancePerTrimePerTimeBin, 0, qm.qr.numberOfBins);
                Array.Clear(avgPassengerCountPerTripPerTimeBin, 0, qm.qr.numberOfBins);
            }
        }

    }


    public void RetrieveStats(int index)
    {
        if (!(qm.sm.stc is ODSTCManager) || !((ODSTCManager)qm.sm.stc).tripDistances.ContainsKey(index))
            return;

        float tripDistance = ((ODSTCManager)qm.sm.stc).tripDistances[index];
        //DateTime o_time = (DateTime)qm.sm.stc.csvdata.getOriginalValue(qm.sm.stc.csvdata["pickup_datetime"].Data[index], "pickup_datetime");
        //DateTime d_time = (DateTime)qm.sm.stc.csvdata.getOriginalValue(qm.sm.stc.csvdata["dropoff_datetime"].Data[index], "dropoff_datetime");
        //TimeSpan tripDuration = (d_time - o_time);

        float passengerCount = ((ODSTCManager)qm.sm.stc).tripPassengerCounts[index];
        float totalAmount = ((ODSTCManager)qm.sm.stc).tripTotalAmounts[index];

        passengerCountAcc += passengerCount;
        totalAmountAcc += totalAmount;
        tripDistanceAcc += tripDistance;
        tripDurationInMinutesAcc += ((ODSTCManager)qm.sm.stc).tripDurationsInMinutes[index];
        if (((ODSTCManager)qm.sm.stc).tripDurationsInHours[index] > 0)
            tripSpeedInMphAcc += tripDistance / ((ODSTCManager)qm.sm.stc).tripDurationsInHours[index];
        if (tripDistance > 0)
            tripFarePerMileAcc += totalAmount / tripDistance;

        if (RetrievePositions)
        {
            float o_lat = ((ODSTCManager)qm.sm.stc).tripOriginLats[index];
            float o_lon = ((ODSTCManager)qm.sm.stc).tripOriginLons[index];

            float d_lat = ((ODSTCManager)qm.sm.stc).tripDestinationLats[index];
            float d_lon = ((ODSTCManager)qm.sm.stc).tripDestinationLons[index];

            Vector3 o_wp = Microsoft.Maps.Unity.MapRendererTransformExtensions.TransformLatLonAltToWorldPoint(qm.sm.bingMap.mapRenderer, new Microsoft.Geospatial.LatLonAlt(o_lat, o_lon, 0f));
            Vector3 d_wp = Microsoft.Maps.Unity.MapRendererTransformExtensions.TransformLatLonAltToWorldPoint(qm.sm.bingMap.mapRenderer, new Microsoft.Geospatial.LatLonAlt(d_lat, d_lon, 0f));

            Vector3 o = new Vector3(o_wp.x, qm.sm.stc.mapTimeToY(((ODSTCManager)qm.sm.stc).tripOriginTimes[index]), o_wp.z);
            Vector3 d = new Vector3(d_wp.x, qm.sm.stc.mapTimeToY(((ODSTCManager)qm.sm.stc).tripDestinationTimes[index]), d_wp.z);

            tripOriginsWorldScale.Add(o);
            tripDestinationsWorldScale.Add(d);
        }

        if (GenerateBinnedStats)
        {
            int i = qm.qr.MapTimeToBinNumber(((ODSTCManager)qm.sm.stc).tripOriginTimes[index]);

            numTripsPerTimeBin[i]++;

            avgTotalAmountPerTripPerTimeBin[i] += totalAmount;
            if (tripDistance > 0)
                avgFarePerMilePerTimeBin[i] += totalAmount / tripDistance;
            if (((ODSTCManager)qm.sm.stc).tripDurationsInHours[index] > 0)
                avgSpeedPerTimeBin[i] += tripDistance / ((ODSTCManager)qm.sm.stc).tripDurationsInHours[index];
            avgDurationPerTripPerTimeBin[i] += ((ODSTCManager)qm.sm.stc).tripDurationsInMinutes[index];
            avgDistancePerTrimePerTimeBin[i] += tripDistance;
            avgPassengerCountPerTripPerTimeBin[i] += passengerCount;
        }

    }


    public void CalculateAvgRelatedStats()
    {
        if (numberOfFilteredPoints > 0)
        {
            avgAmount = totalAmountAcc / numberOfFilteredPoints;
            avgPassengerCount = passengerCountAcc / numberOfFilteredPoints;
            avgDistance = tripDistanceAcc / numberOfFilteredPoints;
            avgDurationInMinutes = tripDurationInMinutesAcc / numberOfFilteredPoints;
            avgSpeedInMph = tripSpeedInMphAcc / numberOfFilteredPoints;
            avgFarePerMile = tripFarePerMileAcc / numberOfFilteredPoints;
        }

        if(GenerateBinnedStats)
        {
            for(int i =0; i<qm.qr.numberOfBins; i++)
            {
                if(numTripsPerTimeBin[i] > 0)
                {
                    avgTotalAmountPerTripPerTimeBin[i] /= numTripsPerTimeBin[i];
                    avgFarePerMilePerTimeBin[i] /= numTripsPerTimeBin[i];
                    avgSpeedPerTimeBin[i] /= numTripsPerTimeBin[i];
                    avgDurationPerTripPerTimeBin[i] /= numTripsPerTimeBin[i];
                    avgDistancePerTrimePerTimeBin[i] /= numTripsPerTimeBin[i];
                    avgPassengerCountPerTripPerTimeBin[i] /= numTripsPerTimeBin[i];
                }
                    
            }
        }


    }


    /*
    public Texture2D CombineTexturesWithAnd(Texture2D tex1, Texture2D tex2)
    {
        return CombineTextures(false, tex1, tex2);
    }

    public Texture2D CombineTexturesWithOr(Texture2D tex1, Texture2D tex2)
    {
        return CombineTextures(true, tex1, tex2);
    }

    Texture2D CombineTextures(bool combineUsingOrLogic, Texture2D tex1, Texture2D tex2) // IN THE FUTURE THIS SHOULD LIKELY BE IMPROVED BY USING A SHADER TO COMBINE TEXTURES
    {
        Texture2D result = new Texture2D(tex1.width, tex1.height);

        for (int x = 0; x < tex1.width; x++)
        {
            for (int y = 0; y < tex1.height; y++)
            {
                if (combineUsingOrLogic)
                {
                    if (tex1.GetPixel(x, y).r == 1f || tex2.GetPixel(x, y).r == 1f)
                        result.SetPixel(x, y, Color.red);
                    else
                        result.SetPixel(x, y, Color.black);
                }
                else
                {
                    if (tex1.GetPixel(x, y).r == 1f && tex2.GetPixel(x, y).r == 1f)
                        result.SetPixel(x, y, Color.red);
                    else
                        result.SetPixel(x, y, Color.black);
                }
            }
        }
        result.Apply();
        return result;
    }

    public void ClearFilterTexture(Texture2D texture)
    {
        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                texture.SetPixel(x, y, Color.black);
            }
        }
        texture.Apply();
    }*/


    public void UpdateStatsTooltip()
    {
        if (queryStatsTooltip != null && queryStatsTooltip.gameObject.activeSelf)
        {
            UpdateStatsTooltipPosition();

            if (numberOfFilteredPoints == 0)
            {
                queryStatsTooltip.ToolTipText = "No trips selected";
            }
            else if (numberOfFilteredPoints == 1)
            {
                queryStatsTooltip.ToolTipText = numberOfFilteredPoints + " trip selected\nPassenger count: " + avgPassengerCount + "\nTrip distance: " + avgDistance.ToString("N1") + " mi\nTrip duration: " + avgDurationInMinutes.ToString("N1") + " min\nAvg. speed: " + avgSpeedInMph.ToString("N1") + " mph\nFare/mile: " + avgFarePerMile.ToString("C2") + "\nTotal amount: " + avgAmount.ToString("C2");
            }
            else
            {
                queryStatsTooltip.ToolTipText = numberOfFilteredPoints + " trips selected\nAvg. passenger count: " + avgPassengerCount.ToString("N2") + "\nAvg. trip distance: " + avgDistance.ToString("N1") + " mi\nAvg. trip duration: " + avgDurationInMinutes.ToString("N1") + " min\nAvg. trip speed: " + avgSpeedInMph.ToString("N1") + " mph\nAvg. fare/mile: " + avgFarePerMile.ToString("C2") + "\nAvg. total amount: " + avgAmount.ToString("C2");
            }
        }
    }


    public static RenderTexture ClearFilterTexture(RenderTexture renderTexture)
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = rt;
        return renderTexture;
    }

    public static RenderTexture FillFilterTexture(RenderTexture renderTexture)
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.red);
        RenderTexture.active = rt;
        return renderTexture;
    }
}
