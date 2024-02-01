using Microsoft.Maps.Unity;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSubmenu : MonoBehaviour
{
    public GameObject submenu;
    public ScenarioManager sm;

    public Interactable terrainToggle;
    public Interactable buildingToggle;
    public Interactable satelliteToggle;

    /*public Interactable deskTerrainToggle;
    public Interactable deskBuildingToggle;
    public Interactable deskSatelliteToggle;

    public bool wasTerrainToggleOn;
    public bool wasBuildingToggleOn;
    public bool wasSatelliteToggleOn;
    */
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // TO DO:
        // BUILDINGS VS 3D TERRAIN
        // REVERSE DIRECTION TO DESK

        if (submenu.activeSelf)
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

            /*
            if (terrainToggle.IsToggled != wasTerrainToggleOn)
                deskTerrainToggle.IsToggled = terrainToggle.IsToggled;

            if (buildingToggle.IsToggled != wasBuildingToggleOn)
                deskBuildingToggle.IsToggled = buildingToggle.IsToggled;

            if (satelliteToggle.IsToggled != wasSatelliteToggleOn)
                deskSatelliteToggle.IsToggled = satelliteToggle.IsToggled;
                


            wasTerrainToggleOn = terrainToggle.IsToggled;
            wasBuildingToggleOn = buildingToggle.IsToggled;
            wasSatelliteToggleOn = satelliteToggle.IsToggled;*/

        }
    }

    public void Toggle()
    {
        submenu.SetActive(!submenu.activeSelf);
    }
}
