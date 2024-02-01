using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EmbeddedPlotsManager : MonoBehaviour
{
    public ScenarioManager sm;
    public GameObject leftPanel, rightPanel;
    //public GameObject plotPrefab;
    public List<EmbeddedTimeSeriesGraph> embeddedPlots = new List<EmbeddedTimeSeriesGraph>();
    //public List<EmbeddedTimeSeriesGraph> leftPlots = new List<EmbeddedTimeSeriesGraph>();
    //public List<EmbeddedTimeSeriesGraph> rightPlots = new List<EmbeddedTimeSeriesGraph>();


    private void Start()
    {
        embeddedPlots.Add(sm.qm.qr.numTripsPerTimeEmbedded);
        embeddedPlots.Add(sm.qm.qr.avgTotalAmountPerTimeEmbedded);
        embeddedPlots.Add(sm.qm.qr.avgDistancePerTimeEmbedded);
        embeddedPlots.Add(sm.qm.qr.avgDurationPerTimeEmbedded);
    }


    // Update is called once per frame
    void Update()
    {
        
    }


    public void ResetPositions()
    {
        if (sm.ClippedEgoRoom)
        {
            /*rightWallEmbeddedPlots.transform.position = new Vector3(floor.transform.position.x - floor.transform.localScale.x / 6,
                                                            table.transform.position.y - 0.25f,
                                                            floor.transform.position.z - floor.transform.localScale.z / 2 - 0.1f);
            attributesConstraintsPanel.transform.rotation = Quaternion.Euler(0, 180, 0);

            attributesConstraintsPanel.transform.localScale = new Vector3(0.83f, 0.084f, 0.007f);*/

            leftPanel.transform.position = new Vector3(sm.floor.transform.position.x - sm.floor.transform.localScale.x / 2,
                            sm.stc.transform.position.y - sm.stc.transform.localScale.y / 2f,
                            sm.floor.transform.position.z - sm.floor.transform.localScale.z / 2 - 0.23f);

            rightPanel.transform.position = new Vector3(sm.floor.transform.position.x + sm.floor.transform.localScale.x / 2,
                                                sm.stc.transform.position.y - sm.stc.transform.localScale.y / 2f,
                                                sm.floor.transform.position.z - sm.floor.transform.localScale.z / 2 - 0.23f);

        }
        else if (sm.VirtualDesk)
        {
            //rightWallEmbeddedPlots.transform.position = new Vector3(table.transform.position.x + table.transform.localScale.x / 2,
            //                                                stc.transform.position.y,
            //                                                table.transform.position.z - table.transform.localScale.z / 2 - 0.035f);

            leftPanel.transform.position = new Vector3(sm.table.transform.position.x - sm.table.transform.localScale.x / 2,
                                    sm.stc.transform.position.y - sm.stc.transform.localScale.y / 2f,
                                    sm.table.transform.position.z - sm.table.transform.localScale.z / 2 - 0.23f);

            rightPanel.transform.position = new Vector3(sm.table.transform.position.x + sm.table.transform.localScale.x / 2,
                                                sm.stc.transform.position.y - sm.stc.transform.localScale.y / 2f,
                                                sm.table.transform.position.z - sm.table.transform.localScale.z / 2 - 0.23f);
            //attributesConstraintsPanel.transform.rotation = Quaternion.Euler(0, -90, 0);

            //attributesConstraintsPanel.transform.localScale = new Vector3(0.83f, 0.084f, 0.007f);
        }

        UpdatePositionsAndScale();
    }

    public void UpdatePositionsAndScale()
    {
        /*if (sm.ClippedEgoRoom)
        {

        }
        else if (sm.VirtualDesk)
        {*/
            leftPanel.transform.position = new Vector3(leftPanel.transform.position.x,
                                            sm.stc.transform.position.y,
                                            leftPanel.transform.position.z);

            //leftPanel.transform.localScale = new Vector3(leftPanel.transform.localScale.x,
            //                                                        sm.stc.transform.localScale.y,
            //                                                        leftPanel.transform.localScale.z);




            rightPanel.transform.position = new Vector3(rightPanel.transform.position.x,
                                                        sm.stc.transform.position.y,
                                                        rightPanel.transform.position.z);


        

            //rightPanel.transform.localScale = new Vector3(rightPanel.transform.localScale.x,
            //                                                        sm.stc.transform.localScale.y,
            //                                                        rightPanel.transform.localScale.z);

        foreach(EmbeddedTimeSeriesGraph plot in embeddedPlots)
        {
            if(sm.stc.timeDirection == STCManager.Direction.LatestOnTop)
            {
                plot._graphContainer.transform.localScale = new Vector3(-1 * sm.stc.transform.localScale.y, plot._graphContainer.transform.localScale.y, plot._graphContainer.transform.localScale.z);
                plot._graphContainer.GetComponent<RectTransform>().localPosition = new Vector3((plot._graphContainer.transform.localScale.x + 1f) * 0.5f * 1000f, 0, 0);
            }
            else
            {
                plot._graphContainer.transform.localScale = new Vector3(sm.stc.transform.localScale.y, plot._graphContainer.transform.localScale.y, plot._graphContainer.transform.localScale.z);
                plot._graphContainer.GetComponent<RectTransform>().localPosition = new Vector3((plot._graphContainer.transform.localScale.x - 1f) * 0.5f * 1000f, 0, 0);
            }


            //plot._labelContainer.GetComponent<RectTransform>().localPosition = new Vector3((sm.stc.transform.localScale.y * 1000f - 1000f) / 2f, 0, 0);

            //plot._labelContainer.position = new Vector3((sm.stc.transform.localScale.y * 1000f - 1000f) / 2f, 0, 0);

            foreach (Image dashedLine in plot._yGridlinesList)
                dashedLine.pixelsPerUnitMultiplier = sm.stc.transform.localScale.y;

        }

        //}
    }

    

}
