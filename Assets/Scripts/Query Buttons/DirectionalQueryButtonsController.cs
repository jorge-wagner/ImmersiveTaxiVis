using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionalQueryButtonsController : QueryButtonsController
{ 



    public void UnlinkQuery()
    {
        AudioSource.PlayClipAtPoint(myQuery.qm.sm.trashSound, myQuery.GetCentralPosition2D());

        ((DirectionalQuery)myQuery).UnlinkQuery();
    }

    public void SwitchMergingModeOnOff()
    {
        myQuery.qm.SwitchMergingModeStatus(myQuery);
    }


}
