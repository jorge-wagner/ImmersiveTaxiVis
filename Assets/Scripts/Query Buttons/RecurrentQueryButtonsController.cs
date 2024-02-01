using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecurrentQueryButtonsController : QueryButtonsController
{
    public void SetQueryModeToPickupsOrDropoffs()
    {
        ((RecurrentQuery)myQuery).SetQueryModeToPickupsOrDropoffs();
    }

    public void SetQueryModeToOnlyPickups()
    {
        ((RecurrentQuery)myQuery).SetQueryModeToOnlyPickups();
    }

    public void SetQueryModeToOnlyDropoffs()
    {
        ((RecurrentQuery)myQuery).SetQueryModeToOnlyDropoffs();
    }

    public void SwitchLinkingModeOnOff()
    {
        myQuery.qm.SwitchLinkingModeStatus(myQuery);
    }

   
    public void DuplicateQuery()
    {
        //AudioSource.PlayClipAtPoint(myQuery.qm.sm.goodSoundClip, myQuery.queryPrism.transform.position);
        AudioSource.PlayClipAtPoint(myQuery.qm.sm.badSoundClip, myQuery.GetCentralPosition3D());

        //myQuery.Duplicate();
    }

}
