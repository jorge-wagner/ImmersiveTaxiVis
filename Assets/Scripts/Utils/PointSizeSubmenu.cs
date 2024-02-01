using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointSizeSubmenu : MonoBehaviour
{
    public GameObject submenu;
    public ScenarioManager sm;

    public TMPro.TMP_Text sliderLabel;
    public PinchSlider multiplierSlider;

    public float minMultiplier = 0.1f;
    public float maxMultiplier = 3f;

    public bool valueChanged = false;
    public float lastValueChangeTime = 0f;
    public float newMultiplier = 1f;

    public void Toggle()
    {
        submenu.SetActive(!submenu.activeSelf);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(valueChanged && (Time.time - lastValueChangeTime) > 0.5f)
        {
            sm.UpdatePointSizeMultiplier(newMultiplier);
            valueChanged = false;
            lastValueChangeTime = Time.time;
        }
    }

    public void MapSliderToMultiplierAndRequestUpdate(SliderEventData eventData)
    {
        newMultiplier = minMultiplier + (eventData.NewValue * (maxMultiplier - minMultiplier));
        sliderLabel.text = "Point Size Multiplier: " + newMultiplier.ToString() + "x";
        valueChanged = true;
    }

}
