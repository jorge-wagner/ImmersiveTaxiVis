using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private float deltaTime;

    // Update is called once per frame
    void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        GameObject.Find("FPSDebugDisplay").GetComponent<TextMesh>().text = Mathf.Ceil(fps).ToString() + " FPS";
    }
}
