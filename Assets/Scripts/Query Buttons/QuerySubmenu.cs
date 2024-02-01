using Microsoft.Maps.Unity;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuerySubmenu : MonoBehaviour
{
    public GameObject submenu;
    public ScenarioManager sm;

    public Interactable freeSelectionToggle;
    public Interactable neighborhoodSelectionToggle;

    public RecurrentQueryTimeSelectorMenu timeSelector;
    public bool handMenu;


    //public Interactable satelliteToggle;

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
            
           /* if (sm.bingMap.mapRenderer.MapTerrainType == Microsoft.Maps.Unity.MapTerrainType.Elevated || sm.bingMap.mapRenderer.MapTerrainType == Microsoft.Maps.Unity.MapTerrainType.Default)
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
*/
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

        //SyncWithReality();

        if (submenu.activeSelf) // user just activated the menu
        {
            if (freeSelectionToggle.IsToggled) // user is turning free selection mode on
            {
                sm.qc.SwitchPenModeStatus();
            }
            else // user is turning free selection mode on
            {
                sm.qc.SwitchNeighborhoodModeStatus();
            }
        }
        else
        {
            if (freeSelectionToggle.IsToggled) // user is turning free selection mode off
            {
                sm.qc.SwitchPenModeStatus();
            }
            else // user is turning free selection mode off
            {
                sm.qc.SwitchNeighborhoodModeStatus();
            }
        }
    }

    public void SwitchFreeMode()
    {
        if(freeSelectionToggle.IsToggled) // user is turning free selection mode on
        {
            if (neighborhoodSelectionToggle.IsToggled)
                neighborhoodSelectionToggle.IsToggled = false;

            if(!sm.qc.isPenModeActive)
                sm.qc.SwitchPenModeStatus();
        }
        else // user is turning free selection mode off
        {
            if (!neighborhoodSelectionToggle.IsToggled)
            {
                neighborhoodSelectionToggle.IsToggled = true;
                SwitchNeighborhoodMode();
            }
        }
    }

    public void SwitchNeighborhoodMode()
    {
        if (neighborhoodSelectionToggle.IsToggled) // user is turning neighborhood selection mode on
        {
            if (freeSelectionToggle.IsToggled)
                freeSelectionToggle.IsToggled = false;

            if (!sm.qc.isNeighborhoodModeActive)
                sm.qc.SwitchNeighborhoodModeStatus();
        }
        else // user is turning neighborhood selection mode off
        {
            if (!freeSelectionToggle.IsToggled)
            {
                freeSelectionToggle.IsToggled = true;
                SwitchFreeMode();
            }
        }
    }

    /*void SyncWithReality()
    {
        if (sm.qc.isPenModeActive)
        {
            freeSelectionToggle.IsToggled = true;
            neighborhoodSelectionToggle.IsToggled = false;
        }
        else
        {
            freeSelectionToggle.IsToggled = false;
            neighborhoodSelectionToggle.IsToggled = true;
        }
    }*/

    public void ToggleRecurrentSelectionMode()
    {
        if (timeSelector == null || !timeSelector.gameObject.activeSelf)
            SwitchRecurrentSelectionModeOn();
        else
            SwitchRecurrentSelectionModeOff();
    }

    public void SwitchRecurrentSelectionModeOn()
    {
        if (timeSelector == null)
            timeSelector = GameObject.Instantiate(sm.qm.rqSelectorPrefab, transform).GetComponent<RecurrentQueryTimeSelectorMenu>();
        else
            timeSelector.gameObject.SetActive(true);
        if(handMenu)
        {
            timeSelector.transform.localScale = new Vector3(-4f, 4f, -1f);
            timeSelector.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.75f;
            timeSelector.transform.LookAt(Camera.main.transform);
        }
        else
        {
            timeSelector.transform.localPosition = new Vector3(0, -0.02f, -0.09f);
            timeSelector.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        timeSelector.qsm = this;
        //DisableAllButtons();
    }

    public void SwitchRecurrentSelectionModeOff()
    {
        timeSelector.gameObject.SetActive(false);// Destroy(timeSelector.gameObject);
        //ReenableAllButtons();
    }


}
