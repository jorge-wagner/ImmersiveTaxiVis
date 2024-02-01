using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtomicQueryButtonsController : QueryButtonsController
{

    public void SetQueryModeToPickupsOrDropoffs()
    {
        ((AtomicQuery)myQuery).SetQueryModeToPickupsOrDropoffs();
    }

    public void SetQueryModeToOnlyPickups()
    {
        ((AtomicQuery)myQuery).SetQueryModeToOnlyPickups();
    }

    public void SetQueryModeToOnlyDropoffs()
    {
        ((AtomicQuery)myQuery).SetQueryModeToOnlyDropoffs();
    }

    public void SwitchLinkingModeOnOff()
    {
        myQuery.qm.SwitchLinkingModeStatus(myQuery);
    }

    public void SwitchMergingModeOnOff()
    {
        myQuery.qm.SwitchMergingModeStatus(myQuery);
    }


    public void DuplicateQuery()
    {
        AudioSource.PlayClipAtPoint(myQuery.qm.sm.goodSoundClip, myQuery.GetCentralPosition3D());

        ((AtomicQuery)myQuery).Duplicate();
    }

}
 