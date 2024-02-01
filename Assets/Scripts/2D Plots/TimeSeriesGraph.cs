using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * The implementation of the Graph class, including Line Graph and Histogram Graph was originally inspired by benjmercier's TimeSeriesLineGraph
 * System, including some heavily-adapted code in this class
 * Source: https://github.com/benjmercier/TimeSeriesLineGraph, https://bjmercier.medium.com/graphing-time-series-data-through-unity-463a35821f77
 */

public class TimeSeriesGraph : MonoBehaviour
{
    public enum GraphType { LineGraph, HistogramGraph };


    [Header("Graph Objects")]
    [SerializeField]
    private RectTransform _graphContainer;

    [SerializeField]
    private RectTransform xLabelPrefab, yLabelPrefab;

    [SerializeField]
    private RectTransform xGridlinePrefab, yGridlinePrefab;

    public TMPro.TMP_Text xAxisLabel, yAxisLabel, title;


    [Header("Graph Settings")]
    [SerializeField]
    private Sprite _dataPointSprite;
    [SerializeField]
    private Vector2 _dataPointSize = new Vector2(15f, 15f);

    public GraphType type = GraphType.LineGraph;

    public float gridlineWidth = 2f;
    public float topEdgeBuffer = 0.1f;
    public bool renderDataPointCircleObjects = false;
    public bool renderLinesWithSegmentObjects = false;

    [Header("Graph Data")]

    public float _graphWidth;
    public float _graphHeight;
    public float minYValue, maxYValue, yValueRange;
    public float minXLabelPos, maxXLabelPos;
    public int yLabelCount = 10;
    public int xLabelCount = 10;

    public DateTime[] binTimes;
    public DateTime minDateTime, maxDateTime;

    public float[][] myDataSeries;
    public Color[] myDataColors;

    public List<GameObject> _graphedObjList = new List<GameObject>();

    public GameObject lastDataPointObject;
    public GameObject newDataPointObject;
    public Vector2 lastDataPointPosition;
    public Vector2 newDataPointPosition;




    private void OnEnable()
    {
        _graphWidth = _graphContainer.sizeDelta.x;
        _graphHeight = _graphContainer.sizeDelta.y;
    }


    public void UpdateGraph(string graphTitle, string yAxisTitle, DateTime minTime, DateTime maxTime, DateTime[] binTimes, float[][] dataSeries, Color[] dataColors)
    {
        title.text = graphTitle;
        yAxisLabel.text = yAxisTitle;

        UpdateGraph(minTime, maxTime, binTimes, dataSeries, dataColors);
    }

    public void UpdateGraph(DateTime minTime, DateTime maxTime, DateTime[] binTimes, float[][] dataSeries, Color[] dataColors)
    {
        _graphedObjList.ForEach(obj => Destroy(obj));
        _graphedObjList.Clear();

        this.binTimes = binTimes;

        myDataSeries = dataSeries;
        myDataColors = dataColors;

        //SetXAxisMinMax(timePeriods[0], timePeriods[timePeriods.Length - 1]); // max should be timeperiods(n-1) + seconds per bin
        SetXAxisMinMax(minTime, maxTime); // max should be timeperiods(n-1) + seconds per bin

        SetYAxisMinMax(dataSeries);

        PlotXAxisLabels();

        PlotYAxisLabels();

        PlotAllDataSeries(dataSeries, dataColors);

    }

    public void SwitchGraphType()
    {
        if (type == GraphType.HistogramGraph)
            type = GraphType.LineGraph;
        else
            type = GraphType.HistogramGraph;

        UpdateGraph(minDateTime, maxDateTime, binTimes, myDataSeries, myDataColors);
    }
   

    private void SetXAxisMinMax(DateTime minTime, DateTime maxTime)
    {
        minDateTime = minTime;
        maxDateTime = maxTime;
    }

    private void SetYAxisMinMax(float[][] dataSeries)
    {
        if (dataSeries.Length > 0)
        {
            minYValue = 0; // dataSeries[0,0];
            maxYValue = dataSeries[0][0];

            for (int i = 0; i < dataSeries.Length; i++)
            {
                for (int j = 0; j < dataSeries[i].Length; j++)
                {
                    //if (dataSeries[i,j] < _yAxis.minValue)
                    //{
                    //    _yAxis.minValue = dataSeries[i,j];
                    //}

                    if (dataSeries[i][j] > maxYValue)
                    {
                        maxYValue = dataSeries[i][j];
                    }
                }
            }

            if (maxYValue < 10)
                maxYValue = 10;

            yValueRange = maxYValue - minYValue;

            /*if (yValueRange <= 0)
            {
                yValueRange = 1f;
            }*/

            //_yAxis.minValue -= (_yAxis.valueRange * _yAxis.edgeBuffer);
            maxYValue += (yValueRange * topEdgeBuffer);
        }
        else
        {
            minYValue = 0;  
            maxYValue = 100; 
        }
    }

    private void PlotXAxisLabels() //(DateTime[] binTimes)
    {        
        minXLabelPos = 0f;
        maxXLabelPos = 0f;

        //int labelIndex = 0;
        float xSliceWidth = _graphWidth / (xLabelCount); // + yEdgeBuffer);

        for (int i = 0; i <= xLabelCount; i++)
        //for (int i = 0; i < binTimes.Length; i++)
        {
            // Labels
            //float currentLabelSpread = _graphWidth / (binTimes.Length + yEdgeBuffer);
            //float currentLabelPos = xLabelSpread + i * xLabelSpread;
            float currentLabelPos = i * xSliceWidth;

            if (i == 0)
            {
                minXLabelPos = currentLabelPos;
            }
            else if (i == xLabelCount) //  (i == xLabelCount - 1)
            {
                maxXLabelPos = currentLabelPos;
            }

            float labelPosNormal = (i * 1f) / xLabelCount;
            DateTime labelTime = minDateTime.AddSeconds(labelPosNormal * (maxDateTime - minDateTime).TotalSeconds);

            RectTransform labelRect = Instantiate(xLabelPrefab, _graphContainer);
            labelRect.gameObject.SetActive(true);
            labelRect.anchoredPosition = new Vector2(currentLabelPos, labelRect.anchoredPosition.y);
            //labelRect.GetComponent<TextMeshProUGUI>().text = binTimes[i].ToString("HH:mm\nMMM dd"); // hh:mm tt\nMMM dd
            labelRect.GetComponent<TextMeshProUGUI>().text = labelTime.ToString("HH:mm\nMMM dd"); // hh:mm tt\nMMM dd

            _graphedObjList.Add(labelRect.gameObject);

            // Gridlines
            if (i != 0 && i != xLabelCount)
            {
                RectTransform gridlineRect = Instantiate(xGridlinePrefab, _graphContainer);
                gridlineRect.gameObject.SetActive(true);
                gridlineRect.anchoredPosition = new Vector2(currentLabelPos, gridlineRect.anchoredPosition.y);

                _graphedObjList.Add(gridlineRect.gameObject);
            }
        }
    }

    private void PlotYAxisLabels()
    {
     
        for (int i = 0; i <= yLabelCount; i++)
        {
            float labelPosNormal = (i * 1f) / yLabelCount;

            float labelPos = minYValue + (labelPosNormal * (maxYValue - minYValue));

            // Labels
            RectTransform labelRect = Instantiate(yLabelPrefab, _graphContainer);
            labelRect.gameObject.SetActive(true);
            labelRect.anchoredPosition = new Vector2(labelRect.anchoredPosition.x, labelPosNormal * _graphHeight);
            labelRect.GetComponent<TextMeshProUGUI>().text = Mathf.RoundToInt(labelPos).ToString();

            _graphedObjList.Add(labelRect.gameObject);

            // Gridlines
            if (i != 0 && i != yLabelCount)
            {
                RectTransform gridlineRect = Instantiate(yGridlinePrefab, _graphContainer);
                gridlineRect.gameObject.SetActive(true);
                gridlineRect.anchoredPosition = new Vector2(gridlineRect.anchoredPosition.x, labelPosNormal * _graphHeight);

                _graphedObjList.Add(gridlineRect.gameObject);
            }
        }
    }

    private void PlotAllDataSeries(float[][] dataSeries, Color[] queryColors)
    {
        if (renderLinesWithSegmentObjects)
        {
            lastDataPointObject = null;
            lastDataPointPosition = new Vector2(-10000, -10000);
        }

        for (int query = 0; query < dataSeries.GetLength(0); query++)
        {
            if (type == GraphType.LineGraph)
                PlotLine(dataSeries[query], queryColors[query]);
            else
                PlotBars(dataSeries[query], queryColors[query], query);
        }
    }

    public void PlotLine(float[] data, Color color)
    {
        if(!renderLinesWithSegmentObjects)
        {
            // IMPROVED VERSION USING LINE RENDERERS

            GameObject newLine = new GameObject("QueryLine", typeof(RectTransform));
            newLine.transform.SetParent(_graphContainer, false);
            _graphedObjList.Add(newLine);
  
            RectTransform newLineRectTransform = newLine.GetComponent<RectTransform>();
            newLineRectTransform.anchorMax = Vector3.zero;
            newLineRectTransform.anchorMin = Vector3.zero;

            LineRenderer lineRenderer = newLine.AddComponent<LineRenderer>();
            lineRenderer.alignment = LineAlignment.TransformZ;
            lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            lineRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            lineRenderer.useWorldSpace = false;
            lineRenderer.numCornerVertices = 3;
            lineRenderer.sortingOrder = 1;
            lineRenderer.startWidth = 2f;
            lineRenderer.endWidth = 2f;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.positionCount = data.Length;

            for (int timeBin = 0; timeBin < data.Length; timeBin++)
            {
                float _dataPointHours = (float)(binTimes[timeBin] - minDateTime).TotalHours;
                float _totalHours = (float)(maxDateTime - minDateTime).TotalHours;

                float xAxisAmplitude = maxXLabelPos - minXLabelPos;

                float x = (_dataPointHours / _totalHours) * xAxisAmplitude + minXLabelPos;
                float y = ((data[timeBin] - minYValue) / (maxYValue - minYValue)) * _graphHeight;

                lineRenderer.SetPosition(timeBin, new Vector3(x, y, -1));
            }

        }

        else { 
            // ORIGINAL VERSION WITH OBJECTS:

            for (int timeBin = 0; timeBin < data.Length; timeBin++)
            {
                float _dataPointHours = (float)(binTimes[timeBin] - minDateTime).TotalHours;
                float _totalHours = (float)(maxDateTime - minDateTime).TotalHours;
        
                float minMaxXLabelVariance = maxXLabelPos - minXLabelPos;

                float x = (_dataPointHours / _totalHours) * minMaxXLabelVariance + minXLabelPos;
                float y = ((data[timeBin] - minYValue) / (maxYValue - minYValue)) * _graphHeight;

                newDataPointPosition = new Vector2(x, y);

                if(renderDataPointCircleObjects)
                {
                    newDataPointObject = CreateDataPoint(newDataPointPosition);
                    _graphedObjList.Add(newDataPointObject);
                }
            
                if (lastDataPointPosition.x != -10000)
                {
                    GameObject _dataConnector = CreateDataConnector(lastDataPointPosition, newDataPointPosition, color);
                    _graphedObjList.Add(_dataConnector);
                }

                if(renderDataPointCircleObjects)
                    lastDataPointObject = newDataPointObject;
                lastDataPointPosition = newDataPointPosition;
            }

            lastDataPointObject = null;
            lastDataPointPosition = new Vector2(-10000, -10000);
        }
    }




    public void PlotBars(float[] data, Color color, int seriesIndex)
    {
        float _totalHours = (float)(maxDateTime - minDateTime).TotalHours;
        float xAxisAmplitude = maxXLabelPos - minXLabelPos;
        float binAmplitude = xAxisAmplitude / binTimes.Length;
        float intraBinBarAmplitude = binAmplitude / myDataSeries.Length;
        float intraBinDelta = seriesIndex * intraBinBarAmplitude;

        for (int bin = 0; bin < data.Length; bin++)
        {
            float _dataPointHours = (float)(binTimes[bin] - minDateTime).TotalHours;
       

            float x = ((_dataPointHours / _totalHours) * xAxisAmplitude) + minXLabelPos + (binAmplitude / 2f) + intraBinDelta;
            float y = ((data[bin] - minYValue) / (maxYValue - minYValue)) * _graphHeight;

            GameObject newBarLine = new GameObject("Bar Line", typeof(RectTransform));
            newBarLine.transform.SetParent(_graphContainer, false);
            _graphedObjList.Add(newBarLine);

            RectTransform newLineRectTransform = newBarLine.GetComponent<RectTransform>();
            newLineRectTransform.anchorMax = Vector3.zero;
            newLineRectTransform.anchorMin = Vector3.zero;

            LineRenderer lineRenderer = newBarLine.AddComponent<LineRenderer>();
            lineRenderer.alignment = LineAlignment.TransformZ;
            lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            lineRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            lineRenderer.useWorldSpace = false;
            //lineRenderer.numCornerVertices = 3;
            lineRenderer.sortingOrder = 1;
            lineRenderer.startWidth = intraBinBarAmplitude;
            lineRenderer.endWidth = intraBinBarAmplitude;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.positionCount = 2;

            lineRenderer.SetPosition(0, new Vector3(x, 0, -1));
            lineRenderer.SetPosition(1, new Vector3(x, y, -1));
        }
    }
















    private GameObject CreateDataPoint(Vector2 pos)
    {
        GameObject _dataPointObj = new GameObject("Data", typeof(Image));
        _dataPointObj.transform.SetParent(_graphContainer, false);
        _dataPointObj.GetComponent<Image>().sprite = _dataPointSprite;
        
        RectTransform _dataPointRect = _dataPointObj.GetComponent<RectTransform>();
        _dataPointRect.anchoredPosition = pos;
        _dataPointRect.sizeDelta = _dataPointSize;
        _dataPointRect.anchorMax = Vector3.zero;
        _dataPointRect.anchorMin = Vector3.zero;

        return _dataPointObj;
    }

    private GameObject CreateDataConnector(Vector2 pointA, Vector2 pointB, Color color)
    {
        GameObject _connectorObj = new GameObject("Connection", typeof(Image));
        _connectorObj.transform.SetParent(_graphContainer, false);
        _connectorObj.GetComponent<Image>().color = color;

        Vector2 _connectorDirection = (pointB - pointA).normalized;

        float _connectorDistance = Vector2.Distance(pointA, pointB);

        float _connectorAngle = Mathf.Atan2(_connectorDirection.y, _connectorDirection.x) * Mathf.Rad2Deg;

        RectTransform _connectorRect = _connectorObj.GetComponent<RectTransform>();
        _connectorRect.anchoredPosition = pointA + _connectorDirection * _connectorDistance * 0.5f;
        _connectorRect.sizeDelta = new Vector2(_connectorDistance, gridlineWidth);
        _connectorRect.anchorMin = Vector3.zero;
        _connectorRect.anchorMax = Vector3.zero;
        _connectorRect.localEulerAngles = new Vector3(0, 0, _connectorAngle);

        return _connectorObj;
    }
}