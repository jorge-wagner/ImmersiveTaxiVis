using Microsoft.Geospatial;
using Microsoft.Maps.Unity;
using Microsoft.Maps.Unity.Search;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class StreetviewInspector : MonoBehaviour
{
    public ScenarioManager sm;
    public GameObject inspectorBase;
    public GameObject inspectorConnector;
    public TextMesh inspectorLabel;
    public Cubemap myCubemap;
    public Material mySkyboxMaterial;

    private string key = "";
    Vector3 previousPosition;
    LatLonAlt myPos;
    bool manipulating = false;

    //float timeLastReload = 0;
    //bool shouldReload = false;

    // Start is called before the first frame update
    void Start()
    {
        string path = "Assets/Resources/GoogleSessionConfig.txt";
        key = File.ReadLines(path).First(); // gets the first line from file.
    }

    private void FixedUpdate()
    {
        if (manipulating)
            UpdateFocusPosition();
    }

    public void RecenterPosition()
    {
        this.transform.position = sm.bingMap.transform.position + new Vector3(0, 0.29f, 0);
        myPos = MapRendererTransformExtensions.TransformWorldPointToLatLonAlt(sm.bingMap.mapRenderer, this.transform.position);
        UpdateAddress();
        StartCoroutine(LoadStreetViewPanorama());
    }

    public void MoveWithMap()
    {
        Vector3 newWorldPos = MapRendererTransformExtensions.TransformLatLonAltToWorldPoint(sm.bingMap.mapRenderer, myPos);
        this.transform.position = new Vector3(newWorldPos.x, sm.bingMap.transform.position.y + 0.29f, newWorldPos.z);
    }

    public void UpdateFocusPositionAndReloadPanorama()
    {
        myPos = MapRendererTransformExtensions.TransformWorldPointToLatLonAlt(sm.bingMap.mapRenderer, this.transform.position);
        UpdateAddress();
        //if(myPos != null)  
        StartCoroutine(LoadStreetViewPanorama());
    }

    public void UpdateFocusPosition()
    {
        myPos = MapRendererTransformExtensions.TransformWorldPointToLatLonAlt(sm.bingMap.mapRenderer, this.transform.position);
        UpdateAddress();
    }

    async void UpdateAddress()
    {
        var finderResult = await MapLocationFinder.FindLocationsAt(myPos.LatLon);

        string formattedAddressString = null;
        if (finderResult.Locations.Count > 0)
        {
            formattedAddressString = finderResult.Locations[0].Address.FormattedAddress;
            inspectorLabel.text = formattedAddressString;
        }
        else
        {
            inspectorLabel.text = "";
        }

        /*if (_mapPinPrefab != null)
        {
            // Create a new MapPin instance at the specified location.
            var newMapPin = Instantiate(_mapPinPrefab);
            newMapPin.Location = latLonAlt.LatLon;
            var textMesh = newMapPin.GetComponentInChildren<TextMeshPro>();
            textMesh.text = formattedAddressString ?? "No address found.";

            _mapPinLayer.MapPins.Add(newMapPin);
        }*/
    }

    public bool hasLatLonPos()
    {
        if (myPos != null)
            return true;
        else
            return false;
    }

    public bool hasPosInsideBounds()
    {
        if (myPos != null && sm.bingMap.mapRenderer.Bounds.Intersects(myPos.LatLon))
            return true;
        else
            return false;
    }

    IEnumerator LoadStreetViewPanorama()
    {
        // new cubemap 
        //myCubemap = new Cubemap(256, TextureFormat.RGBA32, false); // 256
        myCubemap = new Cubemap(640, DefaultFormat.LDR, TextureCreationFlags.None);

        yield return null;

        yield return StartCoroutine(GetStreetviewTexture(CubemapFace.NegativeX, myPos.LatitudeInDegrees, myPos.LongitudeInDegrees, 0, 0));
        yield return StartCoroutine(GetStreetviewTexture(CubemapFace.PositiveZ, myPos.LatitudeInDegrees, myPos.LongitudeInDegrees, 90, 0));
        yield return StartCoroutine(GetStreetviewTexture(CubemapFace.PositiveX, myPos.LatitudeInDegrees, myPos.LongitudeInDegrees, 180, 0));
        yield return StartCoroutine(GetStreetviewTexture(CubemapFace.PositiveY, myPos.LatitudeInDegrees, myPos.LongitudeInDegrees, 90, 90));
        yield return StartCoroutine(GetStreetviewTexture(CubemapFace.NegativeY, myPos.LatitudeInDegrees, myPos.LongitudeInDegrees, 90, -90));
        yield return StartCoroutine(GetStreetviewTexture(CubemapFace.NegativeZ, myPos.LatitudeInDegrees, myPos.LongitudeInDegrees, 270, 0));

        //With help from https://stackoverflow.com/questions/45032579/editing-a-cubemap-skybox-from-remote-image

        myCubemap.Apply();
        mySkyboxMaterial.SetTexture("_Tex", myCubemap);
        RenderSettings.skybox = mySkyboxMaterial;
    }


    public void DiscardAddressLabel()
    {
        inspectorLabel.text = "";
    }

    public void SignalManipulationStarted()
    {
        manipulating = true;
    }

    public void SignalManipulationEnded()
    {
        manipulating = false;
    }

    public void ApplyCurrentCubemapSkybox()
    {
        RenderSettings.skybox = mySkyboxMaterial;
    }


    // Source: https://forum.unity.com/threads/google-streetview-in-oculus-rift-hmd.182867/
    private IEnumerator GetStreetviewTexture(CubemapFace face, double latitude, double longitude, double heading, double pitch = 0.0)
    {
        float width = 640, height = 640;
        float sideHeading = 0, sidePitch = 0;

        string url = "https://maps.googleapis.com/maps/api/streetview?"
            + "size=" + width + "x" + height
            + "&location=" + latitude + "," + longitude
            + "&heading=" + (heading + sideHeading) % 360.0 + "&pitch=" + (pitch + sidePitch) % 360.0
            + "&fov=90.0&sensor=false";

        if (key != "")
            url += "&key=" + key;

        WWW www = new WWW(url);
        yield return www;
        if (!string.IsNullOrEmpty(www.error))
            Debug.Log("Panorama " + name + ": " + www.error);
        else
            print("Panorama " + name + " loaded url " + url);

        //renderer.material.mainTexture = www.texture;
        //target.GetComponent<Renderer>().material.mainTexture = www.texture;
        //myCubemap.SetPixels(www.texture.GetPixels(), face);
        //if(flip)
            myCubemap.SetPixels(FlipTexture(www.texture), face);
        //else
        //    myCubemap.SetPixels(www.texture.GetPixels(), face);
    }

    // Source: https://stackoverflow.com/a/56494513
    Color[] FlipTexture(Texture2D texture)
    { 

        int width = texture.width;
        int height = texture.height;
        Texture2D snap = new Texture2D(width, height);
        Color[] pixels = texture.GetPixels();
        Color[] pixelsFlipped = new Color[pixels.Length];

        for (int i = 0; i<height; i++)
        {
            Array.Copy(pixels, i* width, pixelsFlipped, (height-i-1) * width , width);
        }

        return pixelsFlipped;
        //snap.SetPixels(pixelsFlipped);
        //snap.Apply();
    }


}
