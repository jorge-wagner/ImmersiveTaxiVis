using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Geospatial;
using UnityEngine.EventSystems;
using Microsoft.Maps.Unity;
using System;

public class BingMapInteractible : MonoBehaviour
{
    bool leftTargetInside = false, rightTargetInside = false, centerTargetInside = false;

    public MapInteractionController mapInteractionController;
    public MapRenderer mapRenderer;

    //public FlatPlotManager plot; // TO DO TO MAKE MORE GENERIC: List<VisualizationManager>()
    //public STCManager stc; // TO DO TO MAKE MORE GENERIC: List<VisualizationManager>()
    public ScenarioManager sm;


    [SerializeField]
    private float _zoomSpeed = 5.0f;


    // Update is called once per frame
    void Update()
    {


    }
    

    void UpdatePlots(bool updateGlyphSize)
    {
        if (sm.flatplot)
            sm.flatplot.UpdatePlotsXZNormalizedRanges();
        
        if (sm.stc)
        {
            if (updateGlyphSize)
                sm.stc.UpdateGlyphSizeOrTrajWidth();
            sm.stc.UpdatePlotsXZNormalizedRanges();
        }
    }

}
