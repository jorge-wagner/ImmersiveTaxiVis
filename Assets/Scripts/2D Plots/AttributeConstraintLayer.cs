using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttributeConstraintLayer : MonoBehaviour
{
    public GameObject projectionVisual;
    public LineRenderer edgeLine;
    public TextMesh upperBoundLabel, lowerBoundLabel;
    public GameObject upperBoundWidget, lowerBoundWidget;
    public AttributeConstraintManager acm;
    public BasicHistogram myHistogram;

    float minGap = 0.03f;
    float previousLowerBoundWidgetXPos, previousUpperBoundWidgetXPos, previousProjectionXPos;

    float MinLowerBoundXPos, MaxUpperBoundXPos;
    float CurrentLowerBoundValue = 0, CurrentUpperBoundValue = 0;

    // Start is called before the first frame update
    void Start()
    {
        acm = GameObject.Find("AttributeConstraintManager").GetComponent<AttributeConstraintManager>();
        MinLowerBoundXPos = lowerBoundWidget.transform.localPosition.x;
        MaxUpperBoundXPos = upperBoundWidget.transform.localPosition.x;
    }

    private void FixedUpdate()
    {
        float ValuePerXUnit = (myHistogram.maxXValue - myHistogram.minXValue) / (MaxUpperBoundXPos - MinLowerBoundXPos);
        CurrentUpperBoundValue = (myHistogram.minXValue + (upperBoundWidget.transform.localPosition.x - MinLowerBoundXPos) * ValuePerXUnit);
        CurrentLowerBoundValue = (myHistogram.minXValue + (lowerBoundWidget.transform.localPosition.x - MinLowerBoundXPos) * ValuePerXUnit);
        upperBoundLabel.text = CurrentUpperBoundValue.ToString("N1");
        lowerBoundLabel.text = CurrentLowerBoundValue.ToString("N1");
    }

    // Update is called once per frame
    void Update()
    {
        if (acm != null)
        {
            // ENSURE THAT LOWER BOUND >= HISTOGRAM MIN AND UPPER BOUND <= HISTOGRAM MAX

            if(upperBoundWidget.transform.localPosition.x > MaxUpperBoundXPos)
                upperBoundWidget.transform.localPosition = new Vector3(MaxUpperBoundXPos, upperBoundWidget.transform.localPosition.y, upperBoundWidget.transform.localPosition.z);
            if (lowerBoundWidget.transform.localPosition.x < MinLowerBoundXPos)
                lowerBoundWidget.transform.localPosition = new Vector3(MinLowerBoundXPos, lowerBoundWidget.transform.localPosition.y, lowerBoundWidget.transform.localPosition.z);


            // ENSURE THAT UPPER BOUND > LOWER BOUND

            if (upperBoundWidget.transform.localPosition.x < lowerBoundWidget.transform.localPosition.x + minGap)
            {
                if (UpperBoundWasMoved())
                {
                    lowerBoundWidget.transform.localPosition = new Vector3(upperBoundWidget.transform.localPosition.x - minGap, lowerBoundWidget.transform.localPosition.y, lowerBoundWidget.transform.localPosition.z);
                }
                else if (LowerBoundWasMoved()) // (theoretically both could be moved at the same time, but let's ignore that for now)
                {
                    upperBoundWidget.transform.localPosition = new Vector3(lowerBoundWidget.transform.localPosition.x + minGap, upperBoundWidget.transform.localPosition.y, upperBoundWidget.transform.localPosition.z);
                }
            }


            // IF ANY OF MY WIDGETS WERE MOVED, UPDATE ATTRIBUTE CONSTRAINTS 

            if (LowerBoundWasMoved() || UpperBoundWasMoved()) 
            {
                // CALCULATE CURRENT VALUES

                float ValuePerXUnit = (myHistogram.maxXValue - myHistogram.minXValue) / (MaxUpperBoundXPos - MinLowerBoundXPos);
                CurrentUpperBoundValue = (myHistogram.minXValue + (upperBoundWidget.transform.localPosition.x - MinLowerBoundXPos) * ValuePerXUnit);
                CurrentLowerBoundValue = (myHistogram.minXValue + (lowerBoundWidget.transform.localPosition.x - MinLowerBoundXPos) * ValuePerXUnit);
                upperBoundLabel.text = CurrentUpperBoundValue.ToString("N1");
                lowerBoundLabel.text = CurrentLowerBoundValue.ToString("N1");

                UpdateWidgetsAndLabelsAfterWidgetMovement();
                acm.SetUserDefinedMinValue(myHistogram.myAttribute, CurrentLowerBoundValue);
                acm.SetUserDefinedMaxValue(myHistogram.myAttribute, CurrentUpperBoundValue);
            }


            previousLowerBoundWidgetXPos = lowerBoundWidget.transform.localPosition.x;
            previousUpperBoundWidgetXPos = upperBoundWidget.transform.localPosition.x;
            previousProjectionXPos = projectionVisual.transform.localPosition.x;
        }

    }

    public bool UpperBoundWasMoved()
    {
        if (Mathf.Abs(lowerBoundWidget.transform.localPosition.x - previousLowerBoundWidgetXPos) > 0.0001f)
            return true;
        else
            return false;
    }

    public bool LowerBoundWasMoved()
    {
        if (Mathf.Abs(upperBoundWidget.transform.localPosition.x - previousUpperBoundWidgetXPos) > 0.0001f)
            return true;
        else
            return false;
    }

    public bool ProjectionWasMoved()
    {
        if (Mathf.Abs(projectionVisual.transform.localPosition.x - previousProjectionXPos) > 0.0001f)
            return true;
        else
            return false;
    }

    /*public void SetColor(Color c)
    {
        edgeLine.startColor = c;
        edgeLine.endColor = c;
        upperBoundLabel.color = c;
        lowerBoundLabel.color = c;
        upperBoundWidget.GetComponent<Renderer>().material.color = c; // not the best way
        lowerBoundWidget.GetComponent<Renderer>().material.color = c; // not the best way
    }*/

   
    private void UpdateProjectionEdge()
    {
        edgeLine.SetPosition(0, new Vector3(projectionVisual.transform.localPosition.x - 0.5f * projectionVisual.transform.localScale.x, projectionVisual.transform.localPosition.y + 0.5f * projectionVisual.transform.localScale.y, 0));
        edgeLine.SetPosition(1, new Vector3(projectionVisual.transform.localPosition.x + 0.5f * projectionVisual.transform.localScale.x, projectionVisual.transform.localPosition.y + 0.5f * projectionVisual.transform.localScale.y,0 ));
        edgeLine.SetPosition(2, new Vector3(projectionVisual.transform.localPosition.x + 0.5f * projectionVisual.transform.localScale.x, projectionVisual.transform.localPosition.y - 0.5f * projectionVisual.transform.localScale.y, 0));
        edgeLine.SetPosition(3, new Vector3(projectionVisual.transform.localPosition.x - 0.5f * projectionVisual.transform.localScale.x, projectionVisual.transform.localPosition.y - 0.5f * projectionVisual.transform.localScale.y, 0));
    }

    /*private void UpdateWidgetsAndLabelsAfterProjectionMovement()
    {
        upperBoundWidget.transform.localPosition = new Vector3(projectionVisual.transform.localPosition.x + projectionVisual.transform.localScale.x / 2, projectionVisual.transform.localPosition.y, projectionVisual.transform.localPosition.z);
        lowerBoundWidget.transform.localPosition = new Vector3(projectionVisual.transform.localPosition.x - projectionVisual.transform.localScale.x / 2, projectionVisual.transform.localPosition.y, projectionVisual.transform.localPosition.z);

        UpdateProjectionEdge();
    }*/

    private void UpdateWidgetsAndLabelsAfterWidgetMovement()
    {
        projectionVisual.transform.localScale = new Vector3(upperBoundWidget.transform.localPosition.x - lowerBoundWidget.transform.localPosition.x, projectionVisual.transform.localScale.y, projectionVisual.transform.localScale.z);
        projectionVisual.transform.localPosition = new Vector3((upperBoundWidget.transform.localPosition.x + lowerBoundWidget.transform.localPosition.x)/2, projectionVisual.transform.localPosition.y, projectionVisual.transform.localPosition.z);

        UpdateProjectionEdge();

        //upperBoundWidget.transform.localPosition = new Vector3(projectionVisual.transform.localPosition.x + projectionVisual.transform.localScale.x / 2, projectionVisual.transform.localPosition.y, projectionVisual.transform.localPosition.z);
        //lowerBoundWidget.transform.localPosition = new Vector3(projectionVisual.transform.localPosition.x - projectionVisual.transform.localScale.x / 2, projectionVisual.transform.localPosition.y, projectionVisual.transform.localPosition.z);

        ///if (myQuery != null)
        ///{
        ///    upperBoundLabel.text = myQuery.qm.sm.stc.mapYToTime(myQuery.maxY).ToString("f");
        ///    lowerBoundLabel.text = myQuery.qm.sm.stc.mapYToTime(myQuery.minY).ToString("f");
        ///}

        //wasMoved = true;
    }
}

