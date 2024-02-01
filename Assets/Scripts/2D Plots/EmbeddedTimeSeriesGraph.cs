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

public class EmbeddedTimeSeriesGraph : MonoBehaviour
{
    public enum GraphType { LineGraph, HistogramGraph };


    [Header("Graph Objects")]
    //[SerializeField]
    public RectTransform _graphContainer;
    public RectTransform _labelContainer;

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

    public GraphType type = GraphType.HistogramGraph;

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
    public bool latestOnLeft = false;

    public DateTime[] binTimes;
    public DateTime minDateTime, maxDateTime;

    public float[][] myDataSeries;
    public Color[] myDataColors;

    public List<GameObject> _graphedObjList = new List<GameObject>();
    public List<Image> _yGridlinesList = new List<Image>();

    public GameObject lastDataPointObject;
    public GameObject newDataPointObject;
    public Vector2 lastDataPointPosition;
    public Vector2 newDataPointPosition;

    public bool showXLabelsAndGridlines = false;

    public bool plottingData = false;

    private void OnEnable()
    {
        _graphWidth = _graphContainer.sizeDelta.x;
        _graphHeight = _graphContainer.sizeDelta.y;
    }


    public void UpdateGraph(string graphTitle, string yAxisTitle, DateTime minTime, DateTime maxTime, DateTime[] binTimes, float[][] dataSeries, Color[] dataColors, bool latestOnLeft = false)
    {
        title.text = graphTitle;
        yAxisLabel.text = yAxisTitle;
        this.latestOnLeft = latestOnLeft;

        UpdateGraph(minTime, maxTime, binTimes, dataSeries, dataColors, latestOnLeft);
    }

    public void UpdateGraph(DateTime minTime, DateTime maxTime, DateTime[] binTimes, float[][] dataSeries, Color[] dataColors, bool latestOnLeft = false)
    {
        _graphedObjList.ForEach(obj => Destroy(obj));
        _graphedObjList.Clear();
        _yGridlinesList.Clear();
        this.latestOnLeft = latestOnLeft;

        this.binTimes = binTimes;

        myDataSeries = dataSeries;
        myDataColors = dataColors;

        SetXAxisMinMax(minTime, maxTime); // max should be timeperiods(n-1) + seconds per bin

        SetYAxisMinMax(dataSeries);

        PlotXAxisLabels();

        PlotYAxisLabels();

        PlotAllDataSeries(dataSeries, dataColors);

        plottingData = true;

        //if (latestOnLeft)
        //    _graphContainer.transform.localScale = new Vector3(-1, 1, 1);
        //else
        //    _graphContainer.transform.localScale = new Vector3(1, 1, 1);

    }

    public void SwitchGraphType()
    {
        if (type == GraphType.HistogramGraph)
            type = GraphType.LineGraph;
        else
            type = GraphType.HistogramGraph;

        UpdateGraph(minDateTime, maxDateTime, binTimes, myDataSeries, myDataColors, latestOnLeft);
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

        float xSliceWidth = _graphWidth / (xLabelCount); // + yEdgeBuffer);

        for (int i = 0; i <= xLabelCount; i++)
        {
            float currentLabelPos = i * xSliceWidth;

            if (i == 0)
            {
                minXLabelPos = currentLabelPos;
            }
            else if (i == xLabelCount) //  (i == xLabelCount - 1)
            {
                maxXLabelPos = currentLabelPos;
            }

            if(showXLabelsAndGridlines)
            { 
                float labelPosNormal = (i * 1f) / xLabelCount;
                DateTime labelTime = minDateTime.AddSeconds(labelPosNormal * (maxDateTime - minDateTime).TotalSeconds);

                RectTransform labelRect = Instantiate(xLabelPrefab, _labelContainer);
                labelRect.gameObject.SetActive(true);
                labelRect.anchoredPosition = new Vector2(currentLabelPos, labelRect.anchoredPosition.y);
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
    }

    private void PlotYAxisLabels()
    {
        /*if (yLabelCount > yValueRange)
        {
            int addTo(int to)
            {
                return (to % 2 == 0) ? to : (to + 2);
            }

            if (yValueRange % 2 != 0)
            {
                yLabelCount = addTo((int)yValueRange);
            }
            else
            {
                yLabelCount = (int)yValueRange;
            }
            
            if (yValueRange == 1)
            {
                yLabelCount = Mathf.RoundToInt(yValueRange) + 3;
                //_yAxis.minValue -= 2;
                maxYValue += 2;
            }
        }*/

        for (int i = 0; i <= yLabelCount; i++)
        {
            float labelPosNormal = (i * 1f) / yLabelCount;

            float labelPos = minYValue + (labelPosNormal * (maxYValue - minYValue));

            // Labels
            RectTransform labelRect = Instantiate(yLabelPrefab, _labelContainer);
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
                _yGridlinesList.Add(gridlineRect.GetComponent<Image>());
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
}