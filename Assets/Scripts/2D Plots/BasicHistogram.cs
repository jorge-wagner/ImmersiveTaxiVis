using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
//using System.Numerics;

/*
 * The implementation of the Graph class, including Line Graph and Histogram Graph was originally inspired by benjmercier's TimeSeriesLineGraph
 * System, including some heavily-adapted code in this class
 * Source: https://github.com/benjmercier/TimeSeriesLineGraph, https://bjmercier.medium.com/graphing-time-series-data-through-unity-463a35821f77
 */

public class BasicHistogram : MonoBehaviour
{
    [Header("Graph Objects")]
    [SerializeField]
    public RectTransform _graphContainer;

    [SerializeField]
    private RectTransform xLabelPrefab, yLabelPrefab;

    [SerializeField]
    private RectTransform xGridlinePrefab, yGridlinePrefab;

    public TMPro.TMP_Text xAxisLabel, yAxisLabel, title;


    [Header("Graph Settings")]

    public float gridlineWidth = 2f;
    public float topEdgeBuffer = 0.1f;


    [Header("Graph Data")]

    public AttributeConstraintManager.Attribute myAttribute;
    public float _graphWidth;
    public float _graphHeight;
    public float minYValue, maxYValue, yValueRange;
    public float minXValue, maxXValue, xValueRange;

    public float minXLabelPos, maxXLabelPos;
    public int yLabelCount = 10;
    public int xLabelCount = 10;

    public string[] binLabels;
    //public DateTime[] binTimes;
    //public DateTime minDateTime, maxDateTime;

    public float[] myDataSeries;

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


    public void UpdateGraph(AttributeConstraintManager.Attribute attribute, string graphTitle, string yAxisTitle, float minX, float maxX, string[] binLabels, float[] dataSeries)
    {
        title.text = graphTitle;
        yAxisLabel.text = yAxisTitle;

        UpdateGraph(attribute, minX, maxX, binLabels, dataSeries);
    }

    public void UpdateGraph(AttributeConstraintManager.Attribute attribute, float minX, float maxX, string[] binLabels, float[] dataSeries)
    {
        _graphedObjList.ForEach(obj => Destroy(obj));
        _graphedObjList.Clear();

        myAttribute = attribute;

        this.binLabels = binLabels;

        myDataSeries = dataSeries;

        SetXAxisMinMax(minX, maxX); // max should be timeperiods(n-1) + seconds per bin

        SetYAxisMinMax(dataSeries);

        PlotXAxisLabels();

        PlotYAxisLabels();

        PlotBars(dataSeries, Color.grey);
    }

    /*public void SwitchGraphType()
    {
        if (type == GraphType.HistogramGraph)
            type = GraphType.LineGraph;
        else
            type = GraphType.HistogramGraph;

        UpdateGraph(minDateTime, maxDateTime, binTimes, myDataSeries, myDataColors);
    }*/


    private void SetXAxisMinMax(float minX, float maxX)
    {
        minXValue = minX;
        maxXValue = maxX;
    }

    private void SetYAxisMinMax(float[] dataSeries)
    {
        if (dataSeries.Length > 0)
        {
            minYValue = 0; // dataSeries[0,0];
            maxYValue = 0; // dataSeries[0][0];

            for (int i = 0; i < dataSeries.Length; i++)
            {
                //for (int j = 0; j < dataSeries[i].Length; j++)
                //{
                    //if (dataSeries[i,j] < _yAxis.minValue)
                    //{
                    //    _yAxis.minValue = dataSeries[i,j];
                    //}

                    //if (dataSeries[i][j] > maxYValue)
                    //{
                    //    maxYValue = dataSeries[i][j];
                    //}

                if (dataSeries[i] > maxYValue)
                {
                    maxYValue = dataSeries[i];
                }
                //}
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

        float xSliceWidth = _graphWidth / (xLabelCount); 

        for (int i = 0; i < xLabelCount; i++)
        {
            // Labels
            float currentLabelPos = i * xSliceWidth + _graphWidth / binLabels.Length / 2;

            if (i == 0)
            {
                minXLabelPos = currentLabelPos;
            }
            else if (i == xLabelCount -1) //  (i == xLabelCount - 1)
            {
                maxXLabelPos = currentLabelPos;
            }


            //float labelPosNormal = (i * 1f) / xLabelCount;
            //float labelPos = minXValue + (labelPosNormal * (maxXValue - minXValue));

            RectTransform labelRect = Instantiate(xLabelPrefab, _graphContainer);
            labelRect.gameObject.SetActive(true);
            labelRect.anchoredPosition = new Vector2(currentLabelPos, labelRect.anchoredPosition.y);
            labelRect.GetComponent<TextMeshProUGUI>().text = binLabels[i]; // labelTime.ToString("HH:mm\nMMM dd"); // hh:mm tt\nMMM dd

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

    public void PlotBars(float[] data, Color color)
    {
        float xAxisAmplitude = maxXLabelPos - minXLabelPos;
        float binAmplitude = xAxisAmplitude / binLabels.Length;

        for (int bin = 0; bin < data.Length; bin++)
        {
            float x = (bin * binAmplitude) + minXLabelPos; // + (binAmplitude / 2f);
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
            lineRenderer.sortingOrder = 1;
            lineRenderer.startWidth = binAmplitude;
            lineRenderer.endWidth = binAmplitude;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.positionCount = 2;

            lineRenderer.SetPosition(0, new Vector3(x, 0, -1));
            lineRenderer.SetPosition(1, new Vector3(x, y, -1));
        }
    }

}