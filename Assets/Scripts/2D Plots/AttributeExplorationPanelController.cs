using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttributeExplorationPanelController : MonoBehaviour
{
    public void SwitchGraphTypes()
    {
        TimeSeriesGraph[] myGraphs = GetComponentsInChildren<TimeSeriesGraph>();
        foreach(TimeSeriesGraph g in myGraphs)
        {
            g.SwitchGraphType();
        }
    }
}
