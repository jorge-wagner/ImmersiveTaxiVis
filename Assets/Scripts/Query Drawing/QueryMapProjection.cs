using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using UnityEngine;

public class QueryMapProjection : MonoBehaviour
{
    public GameObject projectionVisual;
    public GameObject edgeAndDotsAnchor;

    //public TextMesh upperBoundLabel, lowerBoundLabel;
    //public GameObject upperBoundWidget, lowerBoundWidget;
    public AtomicQuery myQuery;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

        if (myQuery != null)
        {
            if(projectionVisual.transform.position != edgeAndDotsAnchor.transform.position) // indicates the projection itself was moved 
            {
                myQuery.UpdateQueryAfterMapProjectionInteraction();
            }
        }
    }
}