using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QueryWallProjection : MonoBehaviour
{
    public GameObject projectionVisual;
    public LineRenderer edgeLine;
    public TextMesh upperBoundLabel, lowerBoundLabel;
    public GameObject upperBoundWidget, lowerBoundWidget;
    public AtomicQuery myQuery;

    //bool wasMoved = false;
    float minGap = 0.02f;
    float previousLowerBoundWidgetYPos, previousUpperBoundWidgetYPos, previousProjectionYPos;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if(myQuery != null) {

            if (myQuery.isPartOfARecurrentQuery)
                return; 

            // ENSURE THAT UPPER BOUND > LOWER BOUND

            if (upperBoundWidget.transform.position.y < lowerBoundWidget.transform.position.y + minGap)
            {
                if (UpperBoundWasMoved())
                {
                    lowerBoundWidget.transform.position = new Vector3(lowerBoundWidget.transform.position.x, upperBoundWidget.transform.position.y - minGap, lowerBoundWidget.transform.position.z);
                }
                else if (LowerBoundWasMoved()) // (theoretically both could be moved at the same time, but let's ignore that for now)
                {
                    upperBoundWidget.transform.position = new Vector3(upperBoundWidget.transform.position.x, lowerBoundWidget.transform.position.y + minGap, upperBoundWidget.transform.position.z);
                }
            }


            // HANDLE WIDGET OR PROJECTION MANIPULATION 
            //if (upperBoundWidget.transform.position.y != projectionVisual.transform.position.y + projectionVisual.transform.localScale.y / 2 ||
            //                lowerBoundWidget.transform.position.y != projectionVisual.transform.position.y - projectionVisual.transform.localScale.y / 2)
            //{
            if(ProjectionWasMoved()) // IF PROJECTION WAS MOVED, UPDATE QUERY SO THAT THE PRISM AND OTHER PROJECTIONS WILL FOLLOW MY MOVEMENT 
            {
                myQuery.UpdateQueryAfterWallProjectionInteraction(projectionVisual.transform.position.y - projectionVisual.transform.localScale.y / 2, projectionVisual.transform.position.y + projectionVisual.transform.localScale.y / 2);
                UpdateWidgetsAndLabels();
            }
                else if(LowerBoundWasMoved() || UpperBoundWasMoved()) // IF ANY OF MY WIDGETS WERE MOVED, UPDATE QUERY 
                {
                    myQuery.UpdateQueryAfterWallProjectionInteraction(lowerBoundWidget.transform.position.y, upperBoundWidget.transform.position.y);
                    UpdateWidgetsAndLabels();
                }
              
            //}


           // if(projectionVisual.transform.position.y != myQuery.queryPrism.transform.position.y)// && !wasMoved)
           // {
           //     myQuery.UpdateQueryAfterWallProjectionInteraction(projectionVisual.transform.position.y - projectionVisual.transform.localScale.y / 2, projectionVisual.transform.position.y + projectionVisual.transform.localScale.y / 2);
           //     UpdateWidgetsAndLabels();
           //     myQuery.UpdateQueryAfterWallProjectionInteraction(lowerBoundWidget.transform.position.y, upperBoundWidget.transform.position.y);
           // }

            previousLowerBoundWidgetYPos = lowerBoundWidget.transform.position.y;
            previousUpperBoundWidgetYPos = upperBoundWidget.transform.position.y;
            previousProjectionYPos = projectionVisual.transform.position.y;
        }

    }

    public bool UpperBoundWasMoved()
    {
        if (Mathf.Abs(lowerBoundWidget.transform.position.y - previousLowerBoundWidgetYPos) > 0.001f)
            return true;
        else
            return false; 
    }

    public bool LowerBoundWasMoved()
    {
        if (Mathf.Abs(upperBoundWidget.transform.position.y - previousUpperBoundWidgetYPos) > 0.001f)
            return true;
        else
            return false;
    }

    public bool ProjectionWasMoved()
    {
        if (Mathf.Abs(projectionVisual.transform.position.y - previousProjectionYPos) > 0.001f)
            return true;
        else
            return false;
    }

    public void SetColor(Color c)
    {
        edgeLine.startColor = c;
        edgeLine.endColor = c;
        upperBoundLabel.color = c;
        lowerBoundLabel.color = c;
        upperBoundWidget.GetComponent<Renderer>().material.color = c; // not the best way
        lowerBoundWidget.GetComponent<Renderer>().material.color = c; // not the best way
    }

    public void SetProjectionMaterial(Material m)
    {
        projectionVisual.GetComponent<Renderer>().material = m; // not the best way
    }

    public void SetProjectionScale(Vector3 scale)
    {
        projectionVisual.transform.localScale = scale;

        //edgeLine.transform.localScale = scale; // NO, THIS WILL STRETCH THE LINE

        UpdateProjectionEdge();
        UpdateWidgetsAndLabels();
    }

    public void SetProjectionPosition(Vector3 position)
    {
        projectionVisual.transform.position = position;
        edgeLine.transform.position = position;
        UpdateProjectionEdge();
        UpdateWidgetsAndLabels();
    }

    private void UpdateProjectionEdge()
    {

        edgeLine.SetPosition(0, new Vector3(- 0.5f * projectionVisual.transform.localScale.x, 0.5f * projectionVisual.transform.localScale.y, 0));
        edgeLine.SetPosition(1, new Vector3(0.5f * projectionVisual.transform.localScale.x, 0.5f * projectionVisual.transform.localScale.y, 0));
        edgeLine.SetPosition(2, new Vector3(0.5f * projectionVisual.transform.localScale.x, - 0.5f * projectionVisual.transform.localScale.y, 0));
        edgeLine.SetPosition(3, new Vector3(- 0.5f * projectionVisual.transform.localScale.x, - 0.5f * projectionVisual.transform.localScale.y, 0));
    }

    private void UpdateWidgetsAndLabels()
    {
        upperBoundWidget.transform.position = new Vector3(projectionVisual.transform.position.x, projectionVisual.transform.position.y + 0.5f * projectionVisual.transform.localScale.y, projectionVisual.transform.position.z);
        lowerBoundWidget.transform.position = new Vector3(projectionVisual.transform.position.x, projectionVisual.transform.position.y - 0.5f * projectionVisual.transform.localScale.y, projectionVisual.transform.position.z);

        if (myQuery != null)
        {
            upperBoundLabel.text = myQuery.qm.sm.stc.mapYToTime(myQuery.maxY).ToString("f");
            lowerBoundLabel.text = myQuery.qm.sm.stc.mapYToTime(myQuery.minY).ToString("f");
        }

        //wasMoved = true;
    }


    public Vector3 GetProjectionScale()
    {
        return projectionVisual.transform.localScale;
    }

    public Vector3 GetProjectionPosition()
    {
        return projectionVisual.transform.position;
    }

    public void EnableColliders()
    {
       projectionVisual.GetComponent<Collider>().enabled = true;
       upperBoundWidget.GetComponent<Collider>().enabled = true;
       lowerBoundWidget.GetComponent<Collider>().enabled = true;
    }

    public void DisableColliders()
    {
        projectionVisual.GetComponent<Collider>().enabled = false;
        upperBoundWidget.GetComponent<Collider>().enabled = false;
        lowerBoundWidget.GetComponent<Collider>().enabled = false;
    }

    public void HideWidgets()
    {
        upperBoundWidget.GetComponent<MeshRenderer>().enabled = false;
        lowerBoundWidget.GetComponent<MeshRenderer>().enabled = false;
    }

    public void RevealWidgets()
    {
        upperBoundWidget.GetComponent<MeshRenderer>().enabled = true;
        lowerBoundWidget.GetComponent<MeshRenderer>().enabled = true;
    }

    public void Rotate(Vector3 rot)
    {
        this.transform.Rotate(rot);
        //projectionVisual.transform.Rotate(rot);
        //edgeLine.transform.Rotate(rot);
    }

    public void RevealWidgetsAndLabels()
    {
        upperBoundWidget.SetActive(true);
        lowerBoundWidget.SetActive(true);
    }

    public void HideWidgetsAndLabels()
    {
        upperBoundWidget.SetActive(false);
        lowerBoundWidget.SetActive(false);
    }

}
