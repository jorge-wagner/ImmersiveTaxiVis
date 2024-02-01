using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class DemoManager : MonoBehaviour
{
    public enum Example {   NYCTypeIn,
                            NYCTaxiFirstWeekMay2011_10k,
                            NYCTaxiFirstWeekMay2011_50k,
                            NYCTaxiFirstWeekMay2011_100k,
                            NYCTaxiIrene2011_50k,
                            NYCTaxiIrene2011_100k,
                            NYCTaxiSandy2012_50k,
                            NYCTaxiSandy2012_100k,
                            NYCTaxiMay2011_10k,
                            NYCTaxiMay2011_50k,
                            NYCTaxiMay2011_100k  
    };

    [Space(10)]
    [Header("Elements")]

    public ScenarioManager sm;
    public ODSTCManager taxiSTC;
    public FlatPlotManager taxiFlat;
    public TemporalFlatPlotManager taxiFlatTime;

    [Space(10)]
    [Header("Demo Settings")]

    public Example example;
    public TextAsset dataSource;
    public string dateFormat;
    public string typeInFileName;

    private bool refresh = false;

    // Start is called before the first frame update
    void Start()
    {

        if (example == Example.NYCTaxiFirstWeekMay2011_10k)
            dataSource = Resources.Load<TextAsset>("Data/taxi/first_week_of_may_2011_10k_sample");
        else if (example == Example.NYCTaxiFirstWeekMay2011_50k)
            dataSource = Resources.Load<TextAsset>("Data/taxi/first_week_of_may_2011_50k_sample");
        else if (example == Example.NYCTaxiFirstWeekMay2011_100k)
            dataSource = Resources.Load<TextAsset>("Data/taxi/first_week_of_may_2011_100k_sample");
        else if (example == Example.NYCTaxiIrene2011_50k)
            dataSource = Resources.Load<TextAsset>("Data/taxi/hurricane_irene_2011_50k_sample");
        else if (example == Example.NYCTaxiIrene2011_100k)
            dataSource = Resources.Load<TextAsset>("Data/taxi/hurricane_irene_2011_100k_sample");
        else if (example == Example.NYCTaxiSandy2012_50k)
            dataSource = Resources.Load<TextAsset>("Data/taxi/hurricane_sandy_2012_50k_sample");
        else if (example == Example.NYCTaxiSandy2012_100k)
            dataSource = Resources.Load<TextAsset>("Data/taxi/hurricane_sandy_2012_10k_sample");
        else if (example == Example.NYCTaxiMay2011_10k)
            dataSource = Resources.Load<TextAsset>("Data/taxi/may_2011_10k_sample");
        else if (example == Example.NYCTaxiMay2011_50k)
            dataSource = Resources.Load<TextAsset>("Data/taxi/may_2011_50k_sample");
        else if (example == Example.NYCTaxiMay2011_100k)
            dataSource = Resources.Load<TextAsset>("Data/taxi/may_2011_100k_sample");
        else if (example == Example.NYCTypeIn)
                dataSource = Resources.Load<TextAsset>(typeInFileName);



        dateFormat = "yyyy-MM-dd HH:mm:ss";

        sm.stc = taxiSTC;
        taxiSTC.Load(dataSource, dateFormat);
        taxiSTC.transform.parent.gameObject.SetActive(true);

        taxiFlat.Load(dataSource, dateFormat);
        taxiFlat.gameObject.SetActive(false); 

        taxiFlatTime.Load(dataSource, dateFormat);
        taxiFlatTime.gameObject.SetActive(false); 

        refresh = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            sm.stc.UpdatePlotsXZNormalizedRanges();

        //In case trajectories are not appearing on startup
        if(refresh)
        {
            if (taxiSTC.transform.parent.gameObject.activeSelf)
            { 
                taxiSTC.UpdatePlotsXZNormalizedRanges();
            }
            
            if (taxiFlat.gameObject.activeSelf)
            {
                taxiFlat.UpdatePlotsXZNormalizedRanges();
            }
            refresh = false;
        }
        
    }
   
}
