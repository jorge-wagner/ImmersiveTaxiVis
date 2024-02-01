using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchBackplateVisual : MonoBehaviour
{
    public GameObject backplate;
    public bool state = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void switchVisual()
    {
        state = !state;
        backplate.SetActive(state);
    }

}
