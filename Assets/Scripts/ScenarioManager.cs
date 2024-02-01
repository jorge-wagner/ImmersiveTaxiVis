using Microsoft.Maps.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioManager : MonoBehaviour
{

    [Space(10)]
    [Header("Mode")]
    public bool VirtualDesk;
    public bool ClippedEgoRoom;
    public bool EgoRoomIsFourWalled = false;
    public bool ExoRoom;
    public bool Huge;
    public bool UseBingMaps;
    public bool UseMRTK;
    public bool UseOculusRoomScenario;


    [Space(10)]
    [Header("Arguments")]
   
    public bool devMode = false;
    public float roomSide = 5.0f;
    public float tableSide = 2.0f;
    public float initialZoom = 15f;

    public bool gestureAreaBounding = false;
    public bool loadedMap = false;
    public bool autoAdjustOnStart = true;
    public bool adjustOnKeypress = true;
    public bool adjustLengthUsingControllersPositions = false;
    public bool loadScenarioConfigFromPlayerPrefs = true;
    public bool adjustmentComplete = false; 
    public bool useDataBoundingBox = false;
    public bool useQuestDeskPosition = true;
    public bool useSystemModeLabel = true;

    public float pointSizeMultiplier = 1f;
    private float previousPointSizeMultiplier = 1f;


    [Space(10)]
    [Header("Elements")]

    public GameObject floor;
    public GameObject table;
    public GameObject infiniteFloor;
    public GameObject attributesExplorationPanel, attributesConstraintsPanel;
    public EmbeddedPlotsManager embeddedPlotsManager;
    public ChoroplethManager choropleths;
    public TMPro.TMP_Text fpsDebug;
    public TMPro.TMP_Text modeLabel;
    public GameObject deskMapSettingsButtons;
    public GameObject deskMenu;
    public Material mySkybox, oculusSkybox;
    public GameObject oculusRoomScenario;
    public GameObject oculusRoomScenarioFurniture;
    public StreetviewInspector streetviewInspector;


    [Space(10)]
    [Header("Data Elements")]

    public STCManager stc;
    public FlatPlotManager flatplot;
    public TemporalFlatPlotManager temporalflatplot;
    public BingMapInteractible bingMap;
    public QueryManager qm;
    public QueryCreator qc;
    public QueryCreationCanvas penCanvas;

    [Space(10)]
    [Header("Sound Elements")]
    public AudioClip miniGoodSoundClip;
    public AudioClip goodSoundClip;
    public AudioClip superGoodSoundClip;
    public AudioClip miniBadSoundClip;
    public AudioClip badSoundClip;
    public AudioClip superBadSoundClip;
    public AudioClip trashSound;
    public AudioClip transitionSoundClip;

    private float deltaTime;



    // Start is called before the first frame update
    void Start()
    {
        Time.fixedDeltaTime = 0.4f;

        if (VirtualDesk)
            switchToDesk();
       
        if(bingMap != null)
            bingMap.gameObject.SetActive(true);

        SetSkybox();
    }

    void SetSkybox()
    {
        if (UseOculusRoomScenario)
        {
            RenderSettings.skybox = oculusSkybox;
            oculusRoomScenario.SetActive(true);
            floor.SetActive(false);
            infiniteFloor.SetActive(false);
        }
        else
        {
            RenderSettings.skybox = mySkybox;
            oculusRoomScenario.SetActive(false);
            floor.SetActive(true);
            infiniteFloor.SetActive(true);
        }
    }






    private bool justSwitchedRefTransform = false;

    void Update()
    {

        if (devMode)
        {
            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            fpsDebug.text = Mathf.Ceil(fps).ToString() + " FPS";
        }
        else
        {
            fpsDebug.text = "";
        }

        if (useSystemModeLabel)
        {
            if (qm.InEditQueriesMode)
                modeLabel.text = "Query Mode: You can add, edit, or combine origin-destination queries.";
            else
                modeLabel.text = "Exploration Mode: You can move and scale the space and time dimensions.";
        }
        else
        {
            modeLabel.text = "";
        }


        if (justSwitchedRefTransform && (Time.time - refTransformSwitchTime >= 0.05f)) // likely due to a noticeable delay in updating the mapRenderer bounds, updating the plots only works some time after adjusting the map to a new position/scale 
        {
            justSwitchedRefTransform = false;
            //UpdatePlots();
            UpdateScenarioComponentsAfterSTCInteractions(true);
        }


        if(pointSizeMultiplier != previousPointSizeMultiplier)
        {
            UpdatePointSizeMultiplier(pointSizeMultiplier);
        }



        /*
         * Initialization
         */

        // SPAWN AND POSITION ELEMENTS

        if (autoAdjustOnStart && !adjustmentComplete)
        {
            adjustmentComplete = adjustScenario();
        }

        /*
         * Adjust on keypress
         */

        if (adjustOnKeypress)
        {
            if (Input.GetKeyDown(KeyCode.F2) ||
                      (OVRInput.Get(OVRInput.Button.SecondaryThumbstick) && OVRInput.Get(OVRInput.Button.Start)))
            {
                    adjustScenario();
            }
        }
    }



    public void UpdatePointSizeMultiplier(float newPointSizeMultiplier)
    {
        pointSizeMultiplier = newPointSizeMultiplier;
        UpdateScenarioComponentsAfterSTCInteractions(true);
        previousPointSizeMultiplier = pointSizeMultiplier;
    }

    public void switchPerspective()
    {
        AudioSource.PlayClipAtPoint(transitionSoundClip, floor.transform.position);

        if (ClippedEgoRoom)
            switchToDesk();
        else if (VirtualDesk)
            switchToClippedEgo();
    }



    public void switchToClippedEgo()
    {
        VirtualDesk = false; ClippedEgoRoom = true; ExoRoom = false; Huge = false;

        table.SetActive(false);
        deskMenu.SetActive(false);
        deskMapSettingsButtons.SetActive(false);
        if (!UseOculusRoomScenario)
            floor.SetActive(true);

        if (stc.gameObject.activeSelf)
            stc.walls.numLines = 21;

        ResetAttributesExplorationPanel();
        ResetAttributesConstraintsPanel();
        embeddedPlotsManager.ResetPositions();

        modeLabel.transform.rotation = Quaternion.Euler(0, 180, 0);
        modeLabel.transform.localScale = new Vector3(2, 2, 2);
        modeLabel.transform.position = new Vector3(floor.transform.position.x,
                                                floor.transform.position.y + 0.1f,
                                                floor.transform.position.z - floor.transform.localScale.z / 2 - 0.1f);

        //stc.walls.transform.localScale = new Vector3(stc.walls.transform.localScale.x, 2 * stc.walls.transform.localScale.y, stc.walls.transform.localScale.z);
        if(stc.transform.localScale.y < 2f)
            stc.walls.SuperRescaleTime(2f, stc.baseHeight);

        switchToRefTransform(floor.transform);
    }



    public void switchToDesk()
    {
        VirtualDesk = true; ClippedEgoRoom = false; ExoRoom = false; Huge = false;

        table.SetActive(true);
        deskMenu.SetActive(true);
        deskMapSettingsButtons.SetActive(true);

        if (!UseOculusRoomScenario)  
            floor.SetActive(true);

        if (stc && stc.gameObject.activeSelf)
            stc.walls.numLines = 11;

        ResetAttributesExplorationPanel();
        ResetAttributesConstraintsPanel();
        embeddedPlotsManager.ResetPositions();

        modeLabel.transform.rotation = Quaternion.Euler(90, 0, 0);
        modeLabel.transform.localScale = Vector3.one;
        modeLabel.transform.position = new Vector3(table.transform.position.x, table.transform.position.y + 0.012f, table.transform.position.z - table.transform.localScale.z / 2 + 0.051f);

        if (stc.transform.localScale.y > 1f)
            stc.walls.SuperRescaleTime(0.5f, stc.baseHeight);

        switchToRefTransform(table.transform);
    }

    public void switchToHuge()
    {
        VirtualDesk = false; ClippedEgoRoom = false; ExoRoom = false; Huge = true;

        table.SetActive(false);
        deskMenu.SetActive(false);
        deskMapSettingsButtons.SetActive(false);
        floor.SetActive(false);

        switchToRefTransform(infiniteFloor.transform);
    }

    float refTransformSwitchTime = 0;

    void switchToRefTransform(Transform t)
    {
        //stc.transform.localScale = new Vector3(t.localScale.x, stc.transform.localScale.y, t.localScale.z);
        stc.transform.localScale = new Vector3(t.localScale.x, stc.transform.localScale.y, t.localScale.z);
        //stc.transform.position = new Vector3(bingMap.transform.position.x - bingMap.mapRenderer.MapDimension.x / 2f, transform.position.y, bingMap.transform.position.z - bingMap.mapRenderer.MapDimension.y / 2f);
        ///stc.transform.position = new Vector3(t.position.x - t.localScale.x / 2, t.position.y - 0.039f, t.position.z - t.localScale.z / 2);
        stc.transform.position = new Vector3(t.position.x - t.localScale.x / 2, t.position.y + t.localScale.y / 2, t.position.z - t.localScale.z / 2);

        //fpsDebug.transform.position = new Vector3(mapSurface.transform.position.x - mapSurface.transform.localScale.x / 2, mapSurface.transform.position.y + mapSurface.transform.localScale.y / 2, mapSurface.transform.position.z - mapSurface.transform.localScale.z / 2);
        fpsDebug.transform.position = new Vector3(t.transform.position.x - t.transform.localScale.x / 2 + 0.01f, t.transform.position.y + 0.1f, t.transform.position.z + t.transform.localScale.z / 2 - 0.01f);

        if (UseBingMaps)
        {
            bingMap.transform.position = t.position - new Vector3(0f, 0.039f, 0f);

            if (useQuestDeskPosition)
                bingMap.mapRenderer.LocalMapDimension = new Vector2(t.localScale.z + 0.001f, t.localScale.x + 0.001f);
            else
                bingMap.mapRenderer.LocalMapDimension = new Vector2(t.localScale.x + 0.001f, t.localScale.z + 0.001f);

            if (t.localScale.x > 3.0f || t.localScale.z > 3.0f)
                bingMap.transform.localScale = new Vector3(Mathf.Min(t.localScale.x, t.localScale.z) / 3.0f, Mathf.Min(t.localScale.x, t.localScale.z) / 3.0f, Mathf.Min(t.localScale.x, t.localScale.z) / 3.0f);
            else
                bingMap.transform.localScale = Vector3.one;
           

            if (stc != null && stc.gameObject.activeSelf)
            {
                stc.walls.Reinitialize();
            }

            if (qc.isPenModeActive || qc.isNeighborhoodModeActive)
                qc.ClearPreviews();
            if (qc.isNeighborhoodModeActive)
                qc.CreateNeighborhoodPolygonsForCurrentSelection();

        }

        refTransformSwitchTime = Time.time;
        justSwitchedRefTransform = true;
    }

    public void ToggleStreetViewInspecton()
    {
        streetviewInspector.gameObject.SetActive(!streetviewInspector.gameObject.activeSelf);

        if (streetviewInspector.gameObject.activeSelf)
        {
            HideFurniture();

            if (!streetviewInspector.hasPosInsideBounds())
                streetviewInspector.RecenterPosition();
            else
                streetviewInspector.ApplyCurrentCubemapSkybox();
        }
        else
        {
            SetSkybox();
            ShowFurniture();
        }

    }
    

    public void ResetAttributesExplorationPanel()
    {
        if(ClippedEgoRoom)
        {
            attributesExplorationPanel.transform.position = new Vector3(floor.transform.position.x + floor.transform.localScale.x / 6,
                                                            table.transform.position.y - 0.25f,
                                                            floor.transform.position.z - floor.transform.localScale.z / 2 - 0.1f);
            attributesExplorationPanel.transform.rotation = Quaternion.Euler(0, 180, 0);

            attributesExplorationPanel.transform.localScale = new Vector3(0.83f, 0.084f, 0.007f);
        }
        else if(VirtualDesk)
        {
            attributesExplorationPanel.transform.position = new Vector3(table.transform.position.x + table.transform.localScale.x / 2 + 0.15f,
                                                            table.transform.position.y - 0.25f,
                                                            table.transform.position.z - table.transform.localScale.z / 2 - 1.25f); //0.75
            attributesExplorationPanel.transform.rotation = Quaternion.Euler(0, 90, 0);

            attributesExplorationPanel.transform.localScale = new Vector3(0.83f, 0.084f, 0.007f);
        }
    }

    public void ResetAttributesConstraintsPanel()
    {
        if (ClippedEgoRoom)
        {
            attributesConstraintsPanel.transform.position = new Vector3(floor.transform.position.x - floor.transform.localScale.x / 6,
                                                            table.transform.position.y - 0.25f,
                                                            floor.transform.position.z - floor.transform.localScale.z / 2 - 0.1f);
            attributesConstraintsPanel.transform.rotation = Quaternion.Euler(0, 180, 0);

            attributesConstraintsPanel.transform.localScale = new Vector3(0.83f, 0.084f, 0.007f);
        }
        else if (VirtualDesk)
        {
            attributesConstraintsPanel.transform.position = new Vector3(table.transform.position.x - table.transform.localScale.x / 2 - 0.15f,
                                                            table.transform.position.y - 0.25f,
                                                            table.transform.position.z - table.transform.localScale.z / 2 - 1.25f); //0.75
            attributesConstraintsPanel.transform.rotation = Quaternion.Euler(0, -90, 0);

            attributesConstraintsPanel.transform.localScale = new Vector3(0.83f, 0.084f, 0.007f);
        }
    }



    public void ResetSettings()
    {
        stc.ResetPositionAndScale();

        UpdateScenarioComponentsAfterSTCInteractions(true);

        if (VirtualDesk)
            switchToDesk();
        else if (ClippedEgoRoom)
            switchToClippedEgo();


    }

   

    public void switchMapDimensionality()
    {
        if (UseBingMaps)
        {
            if (bingMap.mapRenderer.MapTerrainType == Microsoft.Maps.Unity.MapTerrainType.Flat)
            {
                bingMap.mapRenderer.MapTerrainType = Microsoft.Maps.Unity.MapTerrainType.Elevated;
               

                if (VirtualDesk)
                    bingMap.transform.position = table.transform.position;// - new Vector3(0f, 0.039f, 0f);
                else if (ClippedEgoRoom)
                    bingMap.transform.position = floor.transform.position;// - new Vector3(0f, 0.039f, 0f);

                bingMap.mapRenderer.IsClippingVolumeWallEnabled = true;

                fpsDebug.transform.position += new Vector3(0, 0.039f, 0);
                modeLabel.transform.position += new Vector3(0, 0.039f,0);

            }

            else if (bingMap.mapRenderer.MapTerrainType == Microsoft.Maps.Unity.MapTerrainType.Elevated || bingMap.mapRenderer.MapTerrainType == Microsoft.Maps.Unity.MapTerrainType.Default)
            {
                bingMap.mapRenderer.MapTerrainType = Microsoft.Maps.Unity.MapTerrainType.Flat;

                if (VirtualDesk)
                    bingMap.transform.position = table.transform.position - new Vector3(0f, 0.039f, 0f);
                else if (ClippedEgoRoom)
                    bingMap.transform.position = floor.transform.position - new Vector3(0f, 0.039f, 0f);

                fpsDebug.transform.position -= new Vector3(0, 0.039f, 0);
                modeLabel.transform.position -= new Vector3(0, 0.039f, 0);

                bingMap.mapRenderer.IsClippingVolumeWallEnabled = false;
            }

            UpdateScenarioComponentsAfterSTCInteractions(false);

        }
    }

    private MapTerrainType previousTerrainType = MapTerrainType.Flat;

    public void switchMapBuildingVisibility()
    {
        if (UseBingMaps)
        {
            if (bingMap.mapRenderer.MapTerrainType == Microsoft.Maps.Unity.MapTerrainType.Default)
            {
                if(previousTerrainType == MapTerrainType.Flat)
                {
                    switchMapDimensionality();
                }
                else if (previousTerrainType == MapTerrainType.Elevated)
                {
                    bingMap.mapRenderer.MapTerrainType = MapTerrainType.Elevated;
                }
               

            }
            else if (bingMap.mapRenderer.MapTerrainType == Microsoft.Maps.Unity.MapTerrainType.Elevated)
            {
                bingMap.mapRenderer.MapTerrainType = Microsoft.Maps.Unity.MapTerrainType.Default;
                previousTerrainType = MapTerrainType.Elevated;
            }
            else if (bingMap.mapRenderer.MapTerrainType == Microsoft.Maps.Unity.MapTerrainType.Flat)
            {
                switchMapDimensionality();

                bingMap.mapRenderer.MapTerrainType = Microsoft.Maps.Unity.MapTerrainType.Default;
                previousTerrainType = MapTerrainType.Flat;
            }



        }
    }

    public void switchMapLayer()
    {
        if (UseBingMaps)
        {

            if (bingMap.GetComponent<DefaultTextureTileLayer>().ImageryType == MapImageryType.Symbolic)
                bingMap.GetComponent<DefaultTextureTileLayer>().ImageryType = MapImageryType.Aerial;
            else if (bingMap.GetComponent<DefaultTextureTileLayer>().ImageryType == MapImageryType.Aerial)
            {
                bingMap.GetComponent<DefaultTextureTileLayer>().ImageryType = MapImageryType.Symbolic;
            }
           
          
        }
    }


    public void switchDataLinksVisibility()
    {
        if (stc != null)
        {
                stc.ShowLinks = !stc.ShowLinks;
        }

    }

    public void switchDataFlatPlotVisibility()
    {
        if (flatplot != null && flatplot.dataSource != null)
        {
            flatplot.gameObject.SetActive(!flatplot.gameObject.activeSelf);
        }

        if (temporalflatplot != null && temporalflatplot.dataSource != null)
        {
            temporalflatplot.gameObject.SetActive(!temporalflatplot.gameObject.activeSelf);
        }
    }

    public void switchDataSTCVisibility()
    {
        if (stc != null)
        {
            stc.gameObject.SetActive(!stc.gameObject.activeSelf);
            stc.walls.gameObject.SetActive(stc.gameObject.activeSelf);
        }
    }

    public void switchDataBrushVisibility()
    {

    }



    public void switchDataBrushOrigVisibility()
    {

    }

    public void switchDataBrushDestVisibility()
    {

    }

    public void switchLeftInspectionMapVisibility()
    {
        if (stc.gameObject.activeSelf)
        {
            stc.walls.viewLeftInspectionPlane = !stc.walls.viewLeftInspectionPlane;
            stc.walls.leftMap.gameObject.SetActive(stc.walls.viewLeftInspectionPlane);
        }
    }

    public void switchRightInspectionMapVisibility()
    {
        if (stc.gameObject.activeSelf)
        {
            stc.walls.viewRightInspectionPlane = !stc.walls.viewRightInspectionPlane;
            stc.walls.rightMap.gameObject.SetActive(stc.walls.viewRightInspectionPlane);
        }
    }

    //public void switchToAndFromQueryCreationMode()
    //{

        //this.FreezeSTCInteractions = !this.FreezeSTCInteractions;
    //}


    



    public bool adjustScenario()
    {
        if(useQuestDeskPosition) { 

        float deskThickness = 0.015f;

        Transform deskPos = GetDeskTransformFromOVRSceneManager();
        if(deskPos)
        {

            // VERSION THAT WORKS WITH DIFFERENT COORDINATE SETUP (Z-UP)
            //table.transform.rotation = deskPos.rotation;
            //table.transform.localScale = new Vector3(deskPos.localScale.x, deskPos.localScale.y, deskThickness);
            //table.transform.localPosition = deskPos.position - new Vector3(0, 0, deskThickness / 2f);

            // VERSION THAT WORKS WITH UNITY COMMON COORDINATE SETUP (Y-UP)
            table.transform.rotation = Quaternion.Euler(0, deskPos.rotation.eulerAngles.y, 0);
            table.transform.localScale = new Vector3(deskPos.localScale.x, deskThickness, deskPos.localScale.y);
            table.transform.position = deskPos.position - new Vector3(0, deskThickness / 2f, 0);

            floor.transform.rotation = table.transform.rotation;

        }
        else
        {
            return false;
        }
        

        }
        switchToDesk();
        


        return true;

    }




    public void UpdateScenarioComponentsAfterSTCInteractions(bool changedScaleAndThereforeShouldUpdateGlyphSize)
    {

        // PLOTS 

        if (flatplot)
        {
            if (changedScaleAndThereforeShouldUpdateGlyphSize)
                flatplot.UpdateGlyphSizeOrTrajWidth();

            flatplot.UpdatePlotsXZNormalizedRanges();
        }
        if (temporalflatplot)
        {
            if (changedScaleAndThereforeShouldUpdateGlyphSize)
                temporalflatplot.UpdateGlyphSizeOrTrajWidth();

            temporalflatplot.UpdatePlotsYZNormalizedRanges();
        }

        if (stc)
        {
           

            if (changedScaleAndThereforeShouldUpdateGlyphSize)
                stc.UpdateGlyphSizeOrTrajWidth();

            stc.UpdatePlotsXZNormalizedRanges();
        }


        // EMBEDDED PLOTS, IF ANY
        embeddedPlotsManager.UpdatePositionsAndScale();



        // FILTERS

        if (qm)
            qm.UpdateQueries();


        if (choropleths)
            choropleths.UpdateChoroplethsAfterSTCInteractions();

        if (streetviewInspector != null && streetviewInspector.gameObject.activeSelf)
            streetviewInspector.MoveWithMap();

    }










    /// <summary>
    /// When the Scene has loaded, instantiate all the wall and furniture items.
    /// OVRSceneManager creates proxy anchors, that we use as parent tranforms for these instantiated items.
    /// ADAPTED FROM: https://github.com/oculus-samples/Unity-TheWorldBeyond/blob/main/Assets/Scripts/WorldBeyondManager.cs
    /// AND: https://github.com/oculus-samples/Unity-TheWorldBeyond/blob/main/Assets/Scripts/VirtualRoom.cs
    /// </summary>
    Transform GetDeskTransformFromOVRSceneManager()
    {
        try
        {
            OVRSceneAnchor[] sceneAnchors = FindObjectsOfType<OVRSceneAnchor>();

            for (int i = 0; i < sceneAnchors.Length; i++)
            {
                OVRSceneAnchor instance = sceneAnchors[i];
                OVRSemanticClassification classification = instance.GetComponent<OVRSemanticClassification>();
                //if (classification) Debug.Log(string.Format("TWB Anchor {0}: {1}", i, classification.Labels[0]));


                //if (classification.Contains(OVRSceneManager.Classification.Floor))
                //{
                    // ....
                //}
                //else 
                if (classification.Contains(OVRSceneManager.Classification.Desk))
                {
                    return instance.transform.GetChild(0);
                    //Debug.Log("DESK POS: " + instance.transform.GetChild(0).position);
                    //Debug.Log("DESK ROT: " + instance.transform.GetChild(0).rotation);
                    //Debug.Log("DESK SCA: " + instance.transform.GetChild(0).localScale);
                }

            }

            return null;

            //WorldBeyondEnvironment.Instance.Initialize();
        }
        catch
        {

            return null;
            // if initialization messes up for some reason, quit the app
            //WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ERROR_NO_SCENE_DATA);
        }
    }

    public void HideFurniture()
    {
        if(UseOculusRoomScenario)
        {
            oculusRoomScenarioFurniture.SetActive(false);
        }
    }

    public void ShowFurniture()
    {
        if (UseOculusRoomScenario)
        {
            oculusRoomScenarioFurniture.SetActive(true);
        }
    }




    public void SetShaderLimits(Transform t)
    {
        Quaternion _rotation;
        Vector3 _localScale, _position;

        _position = t.position;
        _rotation = Quaternion.Inverse(t.rotation);
        _localScale = new Vector3(t.localScale.x, 1000, t.localScale.z);

        Shader.SetGlobalVector("_Origin", new Vector4(
              _position.x,
              _position.y,
              _position.z,
              0f));
        Shader.SetGlobalVector("_BoxRotation", new Vector4(
               _rotation.eulerAngles.x,
               _rotation.eulerAngles.y,
               _rotation.eulerAngles.z,
               0f));

        Shader.SetGlobalVector("_BoxSize", new Vector4(
            _localScale.x,
            _localScale.y,
            _localScale.z,
            0f));
    }

}
