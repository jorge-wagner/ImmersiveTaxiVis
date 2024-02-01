using IATK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static STCManager;

public class STCTimeInspector : MonoBehaviour
{
    public STCWallsManager walls; 
    public GameObject line;
    public GameObject slice;
    public IATKViewFilter invisibleSlice;
    public GameObject plane;
    public TextMesh leftLabel, centerLabel, rightLabel;

    public bool highlightTimeSlice = true;
    public bool renderTimeSlice = false;
    public bool showCuttingPlane = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
       
    }


    public void SetPositionAndText(float newHeight)
    {
        System.DateTime date = walls.stc.mapYToTime(newHeight);

        line.transform.position = new Vector3(walls.sm.bingMap.transform.position.x, newHeight, walls.sm.bingMap.transform.position.z);


        if (highlightTimeSlice && !walls.sm.qm.InODBrushingMode)
        {
            if (!invisibleSlice.gameObject.activeSelf)
                invisibleSlice.gameObject.SetActive(true);
            invisibleSlice.gameObject.transform.localScale = new Vector3(10f, walls.DateToSliceHeightAtCurrentGranularity(date) / walls.transform.localScale.y, 10f);
            //invisibleSlice.gameObject.transform.localScale = new Vector3(0.995f, walls.DateToSliceHeightAtCurrentGranularity(date) / walls.transform.localScale.y, 0.995f);
            //slice.transform.position = new Vector3(line.transform.position.x, walls.DateToSliceCenterAtCurrentGranularity(date), line.transform.position.z);
            invisibleSlice.gameObject.transform.position = new Vector3(walls.sm.bingMap.transform.position.x, walls.DateToSliceCenterAtCurrentGranularity(date), walls.sm.bingMap.transform.position.z);
        }


        if (renderTimeSlice && !walls.sm.qm.InODBrushingMode)
        {
            if (!slice.activeSelf)
                slice.SetActive(true);
            slice.transform.localScale = new Vector3(0.995f, walls.DateToSliceHeightAtCurrentGranularity(date) / walls.transform.localScale.y, 0.995f);
            //slice.transform.position = new Vector3(line.transform.position.x, walls.DateToSliceCenterAtCurrentGranularity(date), line.transform.position.z);
            slice.transform.position = new Vector3(walls.sm.bingMap.transform.position.x, walls.DateToSliceCenterAtCurrentGranularity(date), walls.sm.bingMap.transform.position.z);
        }
        else
        {
            if (slice.activeSelf)
                slice.SetActive(false);
        }

        if (showCuttingPlane && !walls.sm.qm.InODBrushingMode)
        {
            if (!plane.activeSelf)
                plane.SetActive(true);
            //plane.transform.localScale = new Vector3(line.transform.localScale.x, 0.0025f, line.transform.localScale.z);
            plane.transform.localScale = new Vector3(0.995f, 0.0025f, 0.995f);
            //plane.transform.position = new Vector3(line.transform.position.x, newHeight, line.transform.position.z);
            plane.transform.position = new Vector3(walls.sm.bingMap.transform.position.x, newHeight, walls.sm.bingMap.transform.position.z);
        }
        else
        {
            if (plane.activeSelf)
                plane.SetActive(false);
        }

        if (walls.sm.VirtualDesk)
        {
            leftLabel.fontSize = 45;
            centerLabel.fontSize = leftLabel.fontSize = 45;
            rightLabel.fontSize = leftLabel.fontSize = 45;

            if (walls.sm.embeddedPlotsManager != null)
            {
                line.transform.localScale = new Vector3(0.995f, line.transform.localScale.y, 1.305f);
                line.transform.position = new Vector3(walls.sm.bingMap.transform.position.x, newHeight, walls.sm.bingMap.transform.position.z);
                line.transform.localPosition = new Vector3(line.transform.localPosition.x, line.transform.localPosition.y, line.transform.localPosition.z - 0.505f * 0.305f);
            }

        }
        else if (walls.sm.ClippedEgoRoom)
        {
            leftLabel.fontSize = 80;
            centerLabel.fontSize = leftLabel.fontSize = 80;
            rightLabel.fontSize = leftLabel.fontSize = 80;

            if (walls.sm.embeddedPlotsManager != null)
            {
                line.transform.localScale = new Vector3(0.995f, line.transform.localScale.y, 1.095f);
                line.transform.position = new Vector3(walls.sm.bingMap.transform.position.x, newHeight, walls.sm.bingMap.transform.position.z);
                line.transform.localPosition = new Vector3(line.transform.localPosition.x, line.transform.localPosition.y, line.transform.localPosition.z - 0.505f * 0.095f);
            }

        }

        leftLabel.transform.position = new Vector3(walls.sm.bingMap.transform.position.x - 0.4975f * walls.stc.transform.localScale.x, newHeight, walls.sm.bingMap.transform.position.z);
        leftLabel.transform.localScale = new Vector3(1 / walls.transform.localScale.z, 1 / walls.transform.localScale.y, 1 / walls.transform.localScale.x);

        centerLabel.transform.position = new Vector3(walls.sm.bingMap.transform.position.x, newHeight, walls.sm.bingMap.transform.position.z + 0.4975f * walls.transform.localScale.z);
        centerLabel.transform.localScale = new Vector3(1 / walls.transform.localScale.x, 1 / walls.transform.localScale.y, 1 / walls.transform.localScale.z);

        rightLabel.transform.position = new Vector3(walls.sm.bingMap.transform.position.x + 0.4975f * walls.stc.transform.localScale.x, newHeight, walls.sm.bingMap.transform.position.z);
        rightLabel.transform.localScale = new Vector3(1 / walls.transform.localScale.z, 1 / walls.transform.localScale.y, 1 / walls.transform.localScale.x);

        leftLabel.text = date.ToString(walls.dateFormat, walls.culture);
        centerLabel.text = leftLabel.text;
        rightLabel.text = leftLabel.text;

    }



}
