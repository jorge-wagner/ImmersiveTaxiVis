using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MergedQueryButtonsController : QueryButtonsController
{
    public void UnmergeQuery()
    {
        AudioSource.PlayClipAtPoint(myQuery.qm.sm.trashSound, myQuery.GetCentralPosition2D());

        ((MergedQuery)myQuery).UnmergeQuery();
    }

    public void SwitchMergingModeOnOff()
    {
        myQuery.qm.SwitchMergingModeStatus(myQuery);
    }
}
