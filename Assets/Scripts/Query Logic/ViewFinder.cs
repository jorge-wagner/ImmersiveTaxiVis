using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IATK; 

public class ViewFinder : MonoBehaviour
{
    IATKViewFilter brush;
    public string targetViewObjName = "View";
    public bool findLinkingView = false;
    public string linkingViewObjName = "TaxiSTCManager";
    public string secondTargetViewObjName = "View";
    public bool secondTarget = false;

    // Start is called before the first frame update
    void Start()
    {
        brush = this.GetComponent<IATKViewFilter>();
    }

    // Update is called once per frame
    void Update()
    {
        if (brush.brushedViews.Count == 0)
        {
            if(GameObject.Find(targetViewObjName))
                brush.brushedViews.Add(GameObject.Find(targetViewObjName).GetComponent<View>());
            //else if (GameObject.Find("Pickups"))
            //    brush.brushedViews.Add(GameObject.Find("Pickups").GetComponent<View>());
        }

        if (brush.brushedViews.Count < 2 && secondTarget)
        {
            if (GameObject.Find(secondTargetViewObjName))
                brush.brushedViews.Add(GameObject.Find(secondTargetViewObjName).GetComponent<View>());
            //else if (GameObject.Find("Pickups"))
            //    brush.brushedViews.Add(GameObject.Find("Pickups").GetComponent<View>());
        }

        if (findLinkingView && brush.brushedLinkingViews.Count == 0)
        {
            if (GameObject.Find(linkingViewObjName))
                brush.brushedLinkingViews.Add(GameObject.Find(linkingViewObjName).GetComponent<LinkingViews>());

        }


        //debug

        //var indices = brush.GetBrushedIndices();
        //if (indices.Count > 0)
        //    Debug.Log("CUBE Brushed indices: " + indices.ToString());

    }
}
