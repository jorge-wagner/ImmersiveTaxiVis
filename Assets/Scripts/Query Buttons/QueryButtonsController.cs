using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QueryButtonsController : MonoBehaviour
{
    public Query myQuery;
    public GameObject anchor;
    public GameObject ButtonsCollection;

    public RecurrentQueryTimeSelectorMenu timeSelector;


    public void Update()
    {
        if (anchor != null && myQuery != null)
            anchor.transform.position = myQuery.GetCentralPosition2D();
        this.transform.LookAt(Camera.main.transform);
        this.transform.position = (6 * Camera.main.transform.position + 4 * myQuery.GetCentralPosition3D()) / 10;
    }

    public void SwitchRecurrentSelectionModeOn()
    {
        if(timeSelector == null)
            timeSelector = GameObject.Instantiate(myQuery.qm.rqSelectorPrefab, transform).GetComponent<RecurrentQueryTimeSelectorMenu>();
        else
            timeSelector.gameObject.SetActive(true);
        timeSelector.transform.localPosition = new Vector3(0, -0.02f, 0.015f);
        timeSelector.transform.localRotation = Quaternion.Euler(0, 180, 0);
        timeSelector.qbc = this;
        DisableAllButtons();
    }

    public void SwitchRecurrentSelectionModeOff()
    {
        timeSelector.gameObject.SetActive(false);// Destroy(timeSelector.gameObject);
        ReenableAllButtons();
    }


    public void RemoveQuery()
    {
        AudioSource.PlayClipAtPoint(myQuery.qm.sm.trashSound, myQuery.GetCentralPosition2D());
        myQuery.RemoveQuery();
    }

    public void DisableAllButtons()
    {
        ButtonsCollection.SetActive(false);
    }

    public void ReenableAllButtons()
    {
        ButtonsCollection.SetActive(true);
    }

}
