using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Maps.Unity;
using Microsoft.MixedReality.Toolkit.UI;

public class MapMenu : MonoBehaviour
{
    public ScenarioManager sm;

    public Interactable terrainToggle;
    public Interactable buildingToggle;
    public Interactable satelliteToggle;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (sm.bingMap.mapRenderer.MapTerrainType == Microsoft.Maps.Unity.MapTerrainType.Elevated || sm.bingMap.mapRenderer.MapTerrainType == Microsoft.Maps.Unity.MapTerrainType.Default)
            terrainToggle.IsToggled = true;
        else
            terrainToggle.IsToggled = false;

        if (sm.bingMap.mapRenderer.MapTerrainType == Microsoft.Maps.Unity.MapTerrainType.Default)
            buildingToggle.IsToggled = true;
        else
            buildingToggle.IsToggled = false;

        if (sm.bingMap.GetComponent<DefaultTextureTileLayer>().ImageryType == MapImageryType.Aerial)
            satelliteToggle.IsToggled = true;
        else
            satelliteToggle.IsToggled = false;
    }
}
