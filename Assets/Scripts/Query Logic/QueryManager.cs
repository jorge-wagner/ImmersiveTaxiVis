using IATK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// This class is partly adapted from IATK's BrushingAndLinking.cs
// The goal is to apply a series of volume filters 
// directly to our IATK Views created programatically
// without using IATK Visualizations, by adapting the original brushing 
// mechanism from IATK.

public class QueryManager : MonoBehaviour
{

    [Header("My queries")]

    public List<Query> allQueries;
    public List<AtomicQuery> atomicQueries;
    public List<DirectionalQuery> directionalQueries;
    public List<RecurrentQuery> recurrentQueries;
    public List<MergedQuery> mergedQueries;

    public HashSet<Query> updateQueue = new HashSet<Query>();


    [Header("Settings")]

    public QueryReporter qr;
    public AttributeConstraintManager acm;
    public ScenarioManager sm;
    public bool debug = false;
    public bool InNewQueryMode = false, InEditQueriesMode = false, InODBrushingMode = false, InRegularBrushingMode = false;
    public float MinInactiveIntervalBeforeUpdating = 0.5f;
    private float MinIntervalBeforeBrushUpdating = 1f;
    public bool verbose = false;
    public bool BrushesCountAsIndependentQueries = true;
    public bool FingerInspectionLensIsActive = true;

    [Header("Linking and merging modes")]

    public bool InLinkingMode = false;
    public Query linkingModeFirstQuery;
    public bool InMergingMode = false;
    public Query mergingModeFirstQuery;
    public LineRenderer newArrowPreview;
    public LineRenderer newMergeLinePreview;

    [Header("OD brushing")]
    public BrushQuery leftODBrush;
    public BrushQuery rightODBrush;
    public BrushQuery leftRegularBrush;
    public BrushQuery rightRegularBrush;
    public DirectionalQuery ODBrushingDirectionalQuery;
    public LineRenderer ODBrushingArrow;

    [Header("Materials")]
    public Material pickupQueryMaterial;
    public Material dropoffQueryMaterial;
    public Material doubleQueryMaterial;
    public Material pickupQueryProjectionMaterial; // uses additive transparency for improved blending
    public Material dropoffQueryProjectionMaterial; // uses additive transparency for improved blending
    public Material doubleQueryProjectionMaterial; // uses additive transparency for improved blending
    public float editModeQueryTransparency = 0.7f;
    public float useModeQueryTransparency = 0.05f;
    public ComputeShader textureCombineComputeShader;

    [Header("Elements for IATK filtering")]
    public int texSize = 0;
    public RenderTexture pickupFilterTexture, dropoffFilterTexture, linkedViewFilterTexture;
    public RenderTexture pickupBrushingTexture, dropoffBrushingTexture, linkedViewODBrushingTexture;
    public RenderTexture fullTexture, emptyTexture;
    public Texture2D pickupFilterTex2D, dropoffFilterTex2D, linkedViewFilterTex2D;

    public View pickupsView, dropoffsView, pickupsView_2D_SpaceProjected, dropoffsView_2D_SpaceProjected, pickupsView_2D_TimeProjected, dropoffsView_2D_TimeProjected;
    public LinkingViews linkingView, linkingView_2D_SpaceProjected, linkingView_2D_TimeProjected;
    public bool areFiltersBeingApplied = false;
    public bool RecentlyAddedQuery = false;
    public bool RecentlyDeletedQuery = false;
    public bool NeedsTextureUpdate = false;
    public float timeLastUpdateSignal = 0;
    public float timeLastBrushUpdate = 0;

    [Header("Prefabs")]

    public GameObject directionalQueryPrefab;
    public GameObject dqButtonsPrefab;
    public GameObject rqButtonsPrefab;
    public GameObject rqSelectorPrefab;
    public GameObject mqButtonsPrefab;


    [Header("Colors")]

    public Queue<Color> availableColors = new Queue<Color>();


    [Header("Full data stats")]

    public float[] numTripsPerTimeBin;
    public float[] avgTotalAmountPerTripPerTimeBin;
    public float[] avgFarePerMilePerTimeBin;
    public float[] avgSpeedPerTimeBin;
    public float[] avgDurationPerTripPerTimeBin;
    public float[] avgDistancePerTrimePerTimeBin;
    public float[] avgPassengerCountPerTripPerTimeBin;


    public Material dropoffCombiner;



    // Start is called before the first frame update
    void Start()
    {
        Time.fixedDeltaTime = 10f;

        if (!debug)
            atomicQueries = new List<AtomicQuery>();

        // Source: https://github.com/VIDA-NYU/TaxiVis/blob/master/src/TaxiVis/Resources/qualitative.colors
        availableColors.Enqueue(new Color32(31, 119, 180, 255));
        availableColors.Enqueue(new Color32(255, 127, 14, 255));
        availableColors.Enqueue(new Color32(44, 160, 44, 255));
        availableColors.Enqueue(new Color32(214, 39, 40, 255));
        availableColors.Enqueue(new Color32(148, 103, 189, 255));
        availableColors.Enqueue(new Color32(140, 86, 75, 255));
        availableColors.Enqueue(new Color32(227, 119, 194, 255));
        availableColors.Enqueue(new Color32(127, 127, 127, 255));
        availableColors.Enqueue(new Color32(188, 189, 34, 255));
        availableColors.Enqueue(new Color32(23, 190, 207, 255));

        textureCombineComputeShader = Instantiate(textureCombineComputeShader);
        //textureCombinerRenderMaterial = Instantiate(textureCombinerRenderMaterial);
        kernelComputeBrushTexture = textureCombineComputeShader.FindKernel("CSMain");

    }
    




    // Update is called once per frame
    void Update()
    {

        // UPDATING AUX VISUALS SUCH AS LINKING ARROWS BETWEEN BRUSHES

        UpdateAuxVisuals();


        // DEALING WITH OD BRUSHING

        if (InODBrushingMode)
        {
            if (Time.time - timeLastBrushUpdate > MinIntervalBeforeBrushUpdating)
            {
                if (leftODBrush.isBrushing || rightODBrush.isBrushing)
                {
                    HandleFilterTextureUpdatesBasedOnODBrushing();

                    if(allQueries.Count == 0)
                    {
                        if (qr != null)
                            qr.UpdateTimeSeriesBasedOnQueries();

                        if (acm != null)
                            acm.SignalImmediateNeedForHistogramUpdatesBasedOnQueries();
                    }
                    else
                    {
                        SignalNeedForImmediateTextureUpdate();
                    }

                }
                else
                {
                    ResetIATKBrushers();

                    if (allQueries.Count > 0)
                    {
                        if (qr != null)
                            qr.UpdateTimeSeriesBasedOnQueries();

                        if (acm != null)
                            acm.SignalImmediateNeedForHistogramUpdatesBasedOnQueries();
                    }
                }
                timeLastBrushUpdate = Time.time;
                return; // enough for this frame
            }
        }
        else if(InRegularBrushingMode)
        {
            if (Time.time - timeLastBrushUpdate > MinIntervalBeforeBrushUpdating)
            {
                if (leftRegularBrush.isBrushing || rightRegularBrush.isBrushing)
                {
                    HandleFilterTextureUpdatesBasedOnRegularBrushing();

                    if (allQueries.Count == 0)
                    {
                        if (qr != null)
                            qr.UpdateTimeSeriesBasedOnQueries();

                        if (acm != null)
                            acm.SignalImmediateNeedForHistogramUpdatesBasedOnQueries();
                    }
                    else
                    {
                        SignalNeedForImmediateTextureUpdate();
                    }
                }
                else
                {
                    ResetIATKBrushers();

                    if (allQueries.Count > 0)
                    {
                        if (qr != null)
                            qr.UpdateTimeSeriesBasedOnQueries();

                        if (acm != null)
                            acm.SignalImmediateNeedForHistogramUpdatesBasedOnQueries();
                    }
                }
                timeLastBrushUpdate = Time.time;
                return; // enough for this frame
            }
        }
        


        // IF THERE WERE ANY CHANGES TO QUERIES, LET'S RECOMPUTE THEIR DISJUNCTION (Q1 + Q2 + ... + QN)
        // WE WILL DEAL WITH ALL KINDS OF QUERIES: ATOMIC (I.E, ORIGIN, DESTINATION, OR EITHER), DIRECTIONAL (O -> D), RECURRENT (), MERGED ().

        // IF THERE ARE NO ACTIVE QUERIES AT THE MOMENT, DO NOTHING
        // IF THERE ARE NO ACTIVE QUERIES AT THE MOMENT, DO NOTHING


        if (allQueries.Count == 0 && !acm.isConstraining && qr != null && !qr.reportingFullDataStats && texSize != 0 && !IsBrushing()) // Query reporter should plot full data
        {
            ComputeFullDataStats();
            qr.UpdateFullDataTimeSeries();
        }
        else if (allQueries.Count == 0 && acm.isConstraining && qr != null && (!qr.reportingFullDataStats || acm.constraintsChanged) && texSize != 0 && !IsBrushing()) // Query reporter should plot full data minus attribute and temporal contraints
        {
            ComputeFullDataStatsMinusACMConstraints();
            qr.UpdateFullDataTimeSeries();
        }

        if (texSize == 0)
        {
            if (GameObject.Find("STC-Pickups"))
                ResetIATKFilterTexSize(NextPowerOf2((int)Mathf.Sqrt(GameObject.Find("STC-Pickups").GetComponent<View>().BigMesh.GetNumberVertices())));
            return;
        }

        if (allQueries.Count == 0 && !acm.isConstraining && (!IsBrushing() || !BrushesCountAsIndependentQueries))
        {
            if (areFiltersBeingApplied) // if filters were being applied, but aren't any longer, let's remove them
                ResetIATKFilters();
        }



        // IF NO QUERIES CHANGED AND BRUSHES ARE NOT BRUSHING, DO NOTHING
        if (allQueries.Count > 0 || acm.isConstraining)
            CheckQueryOrContraintChanges();

        if ((NeedsTextureUpdate && Time.time - timeLastUpdateSignal > MinInactiveIntervalBeforeUpdating) || RecentlyAddedQuery || RecentlyDeletedQuery)// || leftBrush.isBrushing || rightBrush.isBrushing)
        {
            ResetTimeLogger();
            if(verbose) Debug.Log("Starting clearing textures. " + LogTimeSincePreviousLog().ToString());
            ClearFilterTexturesAndFlags();
            if (verbose) Debug.Log("Starting main coroutine. " + LogTimeSincePreviousLog().ToString());

            StopAllCoroutines();
            StartCoroutine(UpdateCombineAndApplyFilterTexturesCoroutine());
            
        }
        else
        {
            //return;
        }





    }


    void UpdateAuxVisuals()
    {
        
        // WE MUST DEAL WITH THE OD LINKING MODE, IF ACTIVE: PREVIEW DIRECTIONAL ARROW

        if (InLinkingMode)
        {
            newArrowPreview.SetPosition(0, linkingModeFirstQuery.GetCentralPosition2D() + new Vector3(0, 0.01f, 0));
            if (sm.penCanvas.onFocus)
                newArrowPreview.SetPosition(1, sm.penCanvas.cursorPosition + new Vector3(0, 0.01f, 0));
            //else
            //    newArrowPreview.SetPosition(1, newArrowPreview.GetPosition(0));
            RefreshArrowPreview();
        }
        else
        {
            if (newArrowPreview.gameObject.activeSelf)
                newArrowPreview.gameObject.SetActive(false);
        }

        // WE MUST DEAL WITH THE QUERY MERGING MODE, IF ACTIVE: PREVIEW +++ CONNECTOR 

        if (InMergingMode)
        {
            newMergeLinePreview.SetPosition(0, mergingModeFirstQuery.GetCentralPosition2D());
            if (sm.penCanvas.onFocus)
                newMergeLinePreview.SetPosition(1, sm.penCanvas.cursorPosition);
            //else
            //    newArrowPreview.SetPosition(1, newArrowPreview.GetPosition(0));
            RefreshMergeLinePreview();
        }
        else
        {
            if (newMergeLinePreview.gameObject.activeSelf)
                newMergeLinePreview.gameObject.SetActive(false);
        }


    }


    IEnumerator UpdateCombineAndApplyFilterTexturesCoroutine()
    {
        if (verbose) Debug.Log("Starting Recompute Queries. " + LogTimeSincePreviousLog().ToString());
        yield return StartCoroutine(RecomputeQueriesInUpdateQueueCoroutine());
        if (verbose) Debug.Log("Completed Recompute Queries. " + LogTimeSincePreviousLog().ToString());
        yield return StartCoroutine(RecombineQueryTexturesCoroutine());
        if (verbose) Debug.Log("Completed Recombine Textures. " + LogTimeSincePreviousLog().ToString());
        yield return StartCoroutine(ApplyFilterTexturesCoroutine());
        if (verbose) Debug.Log("Completed Apply Textures. " + LogTimeSincePreviousLog().ToString());

        yield break;
    }

    IEnumerator ApplyFilterTexturesCoroutine()
    {
        //
        // LASTLY, LET'S APPLY OUR NEW FILTER TEXTURES
        //

     
            // ADD PAIRS
            pickupFilterTexture = CombineTexturesWithOrUsingCS(pickupFilterTexture, CombineTexturesWithAndUsingCS(dropoffFilterTexture, fullTexture)); yield return null;
            dropoffFilterTexture = CombineTexturesWithOrUsingCS(dropoffFilterTexture, CombineTexturesWithAndUsingCS(pickupFilterTexture, fullTexture)); yield return null;


        // ADD BRUSHES, IF NEEDED

      if (BrushesCountAsIndependentQueries && IsBrushing())
        {
            pickupFilterTexture = CombineTexturesWithOrUsingCS(pickupFilterTexture, pickupBrushingTexture); yield return null;
            dropoffFilterTexture = CombineTexturesWithOrUsingCS(dropoffFilterTexture, dropoffBrushingTexture); yield return null;
        }


        linkedViewFilterTexture = CombineTexturesWithOrUsingCS(pickupFilterTexture, dropoffFilterTexture); yield return null;
            //CombineTexturesWithOr(pickupFilterTexture, dropoffFilterTexture, out linkedViewFilterTexture);    

            if (verbose) Debug.Log("Generated final textures. " + LogTimeSincePreviousLog().ToString());



            pickupsView.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", pickupFilterTexture); yield return null;
            dropoffsView.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", dropoffFilterTexture); yield return null;
            linkingView.View.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", linkedViewFilterTexture); yield return null;

            if (verbose) Debug.Log("Applied textures to main view. " + LogTimeSincePreviousLog().ToString());



            pickupsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", pickupFilterTexture); yield return null;
                dropoffsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", dropoffFilterTexture); yield return null;
                linkingView_2D_SpaceProjected.View.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", linkedViewFilterTexture); yield return null;

            if (verbose) Debug.Log("Applied textures to space projection view. " + LogTimeSincePreviousLog().ToString());


            pickupsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", pickupFilterTexture); yield return null;
            dropoffsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", dropoffFilterTexture); yield return null;
            linkingView_2D_TimeProjected.View.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", linkedViewFilterTexture); yield return null;


            if (verbose) Debug.Log("Applied textures to time projection view. " + LogTimeSincePreviousLog().ToString());


            //
            RenderTexture rt = RenderTexture.active;
            RenderTexture.active = linkedViewFilterTexture;
            linkedViewFilterTex2D.ReadPixels(new Rect(0, 0, linkedViewFilterTexture.width, linkedViewFilterTexture.height), 0, 0, false);
            linkedViewFilterTex2D.Apply();
            RenderTexture.active = rt;
            //

        

        areFiltersBeingApplied = true;

        yield return null;

        if (qr != null)
            qr.UpdateTimeSeriesBasedOnQueries();

        if (verbose) Debug.Log("Updated QR Time Series. " + LogTimeSincePreviousLog().ToString());

        yield return null;

        if (acm != null)
        {
            if(!IsBrushing())
                acm.SignalNeedForHistogramUpdatesBasedOnQueries();
            else
                acm.SignalImmediateNeedForHistogramUpdatesBasedOnQueries();
        }


        if (verbose) Debug.Log("Signaled need for ACM histogram update. " + LogTimeSincePreviousLog().ToString());

        yield break;
    }


    IEnumerator RecomputeQueriesInUpdateQueueCoroutine()
    {
        foreach (Query q in updateQueue)
        {
            q.RecomputeQueryResults();

            if (verbose) Debug.Log("Completed recomputing one query. " + LogTimeSincePreviousLog().ToString());
        
            yield return null; // continue in the next frame;
        }
        updateQueue.Clear();

        yield break;
    }


    void ClearFilterTexturesAndFlags()
    {
        // SINCE WE HAVE TO DO SOMETHING, LET'S FIRST CLEAR OUR FILTER TEXTURES AND FLAGS

        pickupFilterTexture = ClearFilterTexture(pickupFilterTexture);
        dropoffFilterTexture = ClearFilterTexture(dropoffFilterTexture);
        
        RecentlyAddedQuery = false;
        RecentlyDeletedQuery = false;
        NeedsTextureUpdate = false;
    }


    IEnumerator RecombineQueryTexturesCoroutine() 
    {
  
        // IF WE HAVE QUERIES, LET'S ADD THEM TO FILTERTEXTURES TO APPLY TO OUR IATK VIEWS

        foreach (Query q in allQueries)
        {
            if (q.filterTexture == null)// ||
                //(acm != null && acm.isConstraining && acm.constraintsChanged)) // otherwise already computed by q.CheckChangesRecomputeAndResetFlag() during CheckQueryChanges     
                q.RecomputeQueryResults();

            if (q is MergedQuery)
            {
                // MERGED (COMPLEX) QUERIES CAN HAVE SUBQUERIES OF DIFFERENT TYPES, SO LET'S ADD THEM ONE AT A TIME
                foreach (Query sq in ((MergedQuery)q).subqueries)
                {
                    if (sq.filterTexture == null)// ||
                        //(acm != null && acm.isConstraining && acm.constraintsChanged)) // otherwise already computed by q.CheckChangesRecomputeAndResetFlag() during CheckQueryChanges
                        sq.RecomputeQueryResults();

                        if (sq.type == Query.QueryType.Origin || sq.type == Query.QueryType.Either || sq.type == Query.QueryType.Directional)
                            pickupFilterTexture = CombineTexturesWithOrUsingCS(pickupFilterTexture, sq.filterTexture); 
                        if (sq.type == Query.QueryType.Destination || sq.type == Query.QueryType.Either || sq.type == Query.QueryType.Directional)
                            dropoffFilterTexture = CombineTexturesWithOrUsingCS(dropoffFilterTexture, sq.filterTexture);
                    

                    yield return null;
                }
            }
            else
            {

                    // ADD QUERIES TO ORIGIN FILTER TEXTURE
                    if (q.type == Query.QueryType.Origin || q.type == Query.QueryType.Either || q.type == Query.QueryType.Directional)
                    {
                        pickupFilterTexture = CombineTexturesWithOrUsingCS(pickupFilterTexture, q.filterTexture);
                        if (verbose) Debug.Log("Added one query to origin filter texture. " + LogTimeSincePreviousLog().ToString());
                        yield return null;
                    }

                    // ADD QUERIES TO DESTINATION FILTER TEXTURE
                    if (q.type == Query.QueryType.Destination || q.type == Query.QueryType.Either || q.type == Query.QueryType.Directional)
                    {
                        dropoffFilterTexture = CombineTexturesWithOrUsingCS(dropoffFilterTexture, q.filterTexture);
                        if (verbose) Debug.Log("Added one query to destination filter texture. " + LogTimeSincePreviousLog().ToString());
                        yield return null;

                        //dropoffFilterTexture = CombineTexturesWithOrUsingShader(dropoffFilterTexture, q.filterTexture);
                        //yield return StartCoroutine(CombineTexturesWithOrUsingShader(dropoffFilterTexture, q.filterTexture));
                    }
                
            }

            yield return null;
        }

        //
        // ATTRIBUTE CONSTRAINTS 
        //

        if (acm.isConstraining)
        {
            // IF WE DO HAVE QUERIES, ACM CONSTRAINTS WERE ALREADY DEDUCTED DIRECTLY FROM THEM (OTHERWISE THEIR STATS AND PLOTS WOULD INCLUDE CONSTRAINED-OUT POINTS)

            if (allQueries.Count == 0)
            {
                    pickupFilterTexture = CombineTexturesWithOrUsingCS(pickupFilterTexture, acm.filterTexture); yield return null;
                    dropoffFilterTexture = CombineTexturesWithOrUsingCS(dropoffFilterTexture, acm.filterTexture); yield return null;
                
            }
        }

        yield break;
    } 

    
    public void HandleFilterTextureUpdatesBasedOnODBrushing()
    {
        // IF BOTH O AND D BRUSHES ARE ACTIVE, TAKE THEM INTO ACCOUNT
        if (leftODBrush.isBrushing && rightODBrush.isBrushing)
        {
            // COMPUTE BRUSH RESULTS

            leftODBrush.RecomputeQueryResults();
            rightODBrush.RecomputeQueryResults();

            ODBrushingDirectionalQuery.gameObject.SetActive(true);
            ODBrushingArrow.gameObject.SetActive(true);
            ODBrushingDirectionalQuery.RecomputeQueryResults();

            pickupBrushingTexture = ODBrushingDirectionalQuery.filterTexture;
            dropoffBrushingTexture = ODBrushingDirectionalQuery.filterTexture;


        }

        // IF EITHER THE O OR D BRUSH IS ACTIVE, TAKE IT INTO ACCOUNT
        else if (leftODBrush.isBrushing && !rightODBrush.isBrushing) ///&& leftBrush.queryIATKDataVertexIntersectionComputer.hasBrushed)
        {
            ODBrushingDirectionalQuery.gameObject.SetActive(false);
            ODBrushingArrow.gameObject.SetActive(false);

            leftODBrush.RecomputeQueryResults();
            
            dropoffBrushingTexture = ClearFilterTexture(dropoffBrushingTexture);
            
            pickupBrushingTexture = leftODBrush.queryIATKDataVertexIntersectionComputer.brushedIndicesTexture;

        }

        else if (rightODBrush.isBrushing && !leftODBrush.isBrushing) /// && rightBrush.queryIATKDataVertexIntersectionComputer.hasBrushed)
        {

            ODBrushingDirectionalQuery.gameObject.SetActive(false);
            ODBrushingArrow.gameObject.SetActive(false);

            rightODBrush.RecomputeQueryResults();

            pickupBrushingTexture = ClearFilterTexture(pickupBrushingTexture);
            dropoffBrushingTexture = rightODBrush.queryIATKDataVertexIntersectionComputer.brushedIndicesTexture;


        }


        // ADD PAIRS

        pickupBrushingTexture = CombineTexturesWithOrUsingCS(pickupBrushingTexture, CombineTexturesWithAndUsingCS(dropoffBrushingTexture, fullTexture));
        dropoffBrushingTexture = CombineTexturesWithOrUsingCS(dropoffBrushingTexture, CombineTexturesWithAndUsingCS(pickupBrushingTexture, fullTexture));
        linkedViewODBrushingTexture = CombineTexturesWithOrUsingCS(pickupBrushingTexture, dropoffBrushingTexture);

        pickupsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", pickupBrushingTexture);
        dropoffsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", dropoffBrushingTexture);
        linkingView_2D_SpaceProjected.View.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", linkedViewODBrushingTexture);

        pickupsView.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", pickupBrushingTexture);
        dropoffsView.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", dropoffBrushingTexture);
        linkingView.View.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", linkedViewODBrushingTexture);
    
        pickupsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", pickupBrushingTexture);
        dropoffsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", dropoffBrushingTexture);
        linkingView_2D_TimeProjected.View.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", linkedViewODBrushingTexture);

    }

    public void HandleFilterTextureUpdatesBasedOnRegularBrushing()
    {
        if (leftRegularBrush.isBrushing && rightRegularBrush.isBrushing)
        {
            leftRegularBrush.RecomputeQueryResults();
            rightRegularBrush.RecomputeQueryResults();

            RenderTexture combination = CombineTexturesWithOrUsingCS(leftRegularBrush.filterTexture,
                                                      rightRegularBrush.filterTexture);
            pickupBrushingTexture = combination; 
            dropoffBrushingTexture = combination;
        }

        else if (leftRegularBrush.isBrushing && !rightRegularBrush.isBrushing) 
        {
            ODBrushingDirectionalQuery.gameObject.SetActive(false);

            leftRegularBrush.RecomputeQueryResults();
            pickupBrushingTexture = ClearFilterTexture(pickupBrushingTexture);
            dropoffBrushingTexture = ClearFilterTexture(dropoffBrushingTexture);
            pickupBrushingTexture = leftRegularBrush.queryIATKDataVertexIntersectionComputer.brushedIndicesTexture;
            dropoffBrushingTexture = leftRegularBrush.queryIATKDataVertexIntersectionComputer.brushedIndicesTexture;
        }

        else if (rightRegularBrush.isBrushing && !leftRegularBrush.isBrushing)
        {
            rightRegularBrush.RecomputeQueryResults();
            pickupBrushingTexture = ClearFilterTexture(pickupBrushingTexture);
            dropoffBrushingTexture = ClearFilterTexture(dropoffBrushingTexture);
            pickupBrushingTexture = rightRegularBrush.queryIATKDataVertexIntersectionComputer.brushedIndicesTexture;
            dropoffBrushingTexture = rightRegularBrush.queryIATKDataVertexIntersectionComputer.brushedIndicesTexture;
        }
        
        pickupBrushingTexture = CombineTexturesWithOrUsingCS(pickupBrushingTexture, CombineTexturesWithAndUsingCS(dropoffBrushingTexture, fullTexture));
        dropoffBrushingTexture = CombineTexturesWithOrUsingCS(dropoffBrushingTexture, CombineTexturesWithAndUsingCS(pickupBrushingTexture, fullTexture));
        linkedViewODBrushingTexture = CombineTexturesWithOrUsingCS(pickupBrushingTexture, dropoffBrushingTexture);

        ApplyBrushingTextures();

    }

    void ApplyBrushingTextures()
    {
        pickupsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", pickupBrushingTexture);
        dropoffsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", dropoffBrushingTexture);
        linkingView_2D_SpaceProjected.View.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", linkedViewODBrushingTexture);

        pickupsView.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", pickupBrushingTexture);
        dropoffsView.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", dropoffBrushingTexture);
        linkingView.View.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", linkedViewODBrushingTexture);

        pickupsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", pickupBrushingTexture);
        dropoffsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", dropoffBrushingTexture);
        linkingView_2D_TimeProjected.View.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", linkedViewODBrushingTexture);
    }


    public void RefreshArrowPreview()
    {
        if (!newArrowPreview.gameObject.activeSelf)
            newArrowPreview.gameObject.SetActive(true);

        newArrowPreview.startColor = linkingModeFirstQuery.queryColor;
        newArrowPreview.endColor = linkingModeFirstQuery.queryColor;
        float arrowLength = Vector3.Distance(newArrowPreview.GetPosition(0), newArrowPreview.GetPosition(1));
        float direction;
        if (linkingModeFirstQuery.type == AtomicQuery.QueryType.Destination)
            direction = 1;
        else
            direction = -1;
        if (6 * arrowLength >= 1)
            newArrowPreview.textureScale = new Vector2(direction * 6 * arrowLength, 1);
        else
            newArrowPreview.textureScale = new Vector2(direction * 1, 1);
    }

    public void RefreshMergeLinePreview()
    {
        if (!newMergeLinePreview.gameObject.activeSelf)
            newMergeLinePreview.gameObject.SetActive(true);

        newMergeLinePreview.startColor = mergingModeFirstQuery.queryColor;
        newMergeLinePreview.endColor = mergingModeFirstQuery.queryColor;
        float arrowLength = Vector3.Distance(newMergeLinePreview.GetPosition(0), newMergeLinePreview.GetPosition(1));        
        if (12 * arrowLength >= 1)
            newMergeLinePreview.textureScale = new Vector2(12 * arrowLength, 1);
        else
            newMergeLinePreview.textureScale = new Vector2(1, 1);
    }





    public void ResetIATKFilterTexSize(int newSize)
    {
        if (texSize != newSize)
        {
            texSize = newSize;

            fullTexture = new RenderTexture(texSize, texSize, 24);
            fullTexture.enableRandomWrite = true;
            fullTexture.filterMode = FilterMode.Point;
            fullTexture.Create();
            FillFilterTexture(fullTexture);

            emptyTexture = new RenderTexture(texSize, texSize, 24);
            emptyTexture.enableRandomWrite = true;
            emptyTexture.filterMode = FilterMode.Point;
            emptyTexture.Create();
            ClearFilterTexture(emptyTexture);


            pickupFilterTexture = new RenderTexture(texSize, texSize, 24);
            pickupFilterTexture.enableRandomWrite = true;
            pickupFilterTexture.filterMode = FilterMode.Point;
            pickupFilterTexture.Create();
            ClearFilterTexture(pickupFilterTexture);


            pickupFilterTexture = new RenderTexture(texSize, texSize, 24);
            pickupFilterTexture.enableRandomWrite = true;
            pickupFilterTexture.filterMode = FilterMode.Point;
            pickupFilterTexture.Create();
            ClearFilterTexture(pickupFilterTexture);

            dropoffFilterTexture = new RenderTexture(texSize, texSize, 24);
            dropoffFilterTexture.enableRandomWrite = true;
            dropoffFilterTexture.filterMode = FilterMode.Point;
            dropoffFilterTexture.Create();
            ClearFilterTexture(dropoffFilterTexture);

            linkedViewFilterTexture = new RenderTexture(texSize, texSize, 24);
            linkedViewFilterTexture.enableRandomWrite = true;
            linkedViewFilterTexture.filterMode = FilterMode.Point;
            linkedViewFilterTexture.Create();
            ClearFilterTexture(linkedViewFilterTexture);

            pickupBrushingTexture = new RenderTexture(texSize, texSize, 24);
            pickupBrushingTexture.enableRandomWrite = true;
            pickupBrushingTexture.filterMode = FilterMode.Point;
            pickupBrushingTexture.Create();
            ClearFilterTexture(pickupBrushingTexture);

            dropoffBrushingTexture = new RenderTexture(texSize, texSize, 24);
            dropoffBrushingTexture.enableRandomWrite = true;
            dropoffBrushingTexture.filterMode = FilterMode.Point;
            dropoffBrushingTexture.Create();
            ClearFilterTexture(dropoffBrushingTexture);

            linkedViewODBrushingTexture = new RenderTexture(texSize, texSize, 24);
            linkedViewODBrushingTexture.enableRandomWrite = true;
            linkedViewODBrushingTexture.filterMode = FilterMode.Point;
            linkedViewODBrushingTexture.Create();
            ClearFilterTexture(linkedViewODBrushingTexture);

            pickupFilterTex2D = new Texture2D(texSize, texSize);
            dropoffFilterTex2D = new Texture2D(texSize, texSize);
            linkedViewFilterTex2D = new Texture2D(texSize, texSize); 

            if(sm.stc is ODSTCManager)
            {
                pickupsView = GameObject.Find("STC-Pickups").GetComponent<View>(); 
                if(pickupsView) { 
                    //pickupsView.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", pickupFilterTexture);
                    pickupsView.BigMesh.SharedMaterial.SetFloat("_DataWidth", texSize); 
                    pickupsView.BigMesh.SharedMaterial.SetFloat("_DataHeight", texSize); 
                    pickupsView.BigMesh.SharedMaterial.SetFloat("_ShowBrush", 0f); 
                    pickupsView.BigMesh.SharedMaterial.SetFloat("_BrushAsFilter", 1f);
                    //pickupsView.BigMesh.SharedMaterial.SetFloat("_IndependentBrushing", BrushesAreIndependentFromQueries ? 1f : 0f);
                }

                dropoffsView = GameObject.Find("STC-Dropoffs").GetComponent<View>();
                if(dropoffsView) { 
                    //dropoffsView.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", dropoffFilterTexture);
                    dropoffsView.BigMesh.SharedMaterial.SetFloat("_DataWidth", texSize);
                    dropoffsView.BigMesh.SharedMaterial.SetFloat("_DataHeight", texSize);
                    dropoffsView.BigMesh.SharedMaterial.SetFloat("_ShowBrush", 0f);
                    dropoffsView.BigMesh.SharedMaterial.SetFloat("_BrushAsFilter", 1f);
                    //dropoffsView.BigMesh.SharedMaterial.SetFloat("_IndependentBrushing", BrushesAreIndependentFromQueries ? 1f : 0f);
                }

                linkingView = GameObject.Find("TaxiSTCManager").GetComponent<LinkingViews>();
                if(linkingView) { 
                    //linkingView.View.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", linkedViewFilterTexture);
                    linkingView.View.BigMesh.SharedMaterial.SetFloat("_DataWidth", texSize);
                    linkingView.View.BigMesh.SharedMaterial.SetFloat("_DataHeight", texSize);
                    linkingView.View.BigMesh.SharedMaterial.SetFloat("_ShowBrush", 0f);
                    linkingView.View.BigMesh.SharedMaterial.SetFloat("_BrushAsFilter", 1f);
                    //linkingView.View.BigMesh.SharedMaterial.SetFloat("_IndependentBrushing", BrushesAreIndependentFromQueries ? 1f : 0f);
                }
            }
            

            if(sm.flatplot != null)
            {
                pickupsView_2D_SpaceProjected = sm.flatplot.transform.Find("Pickups-Space").gameObject.GetComponent<View>();
                if (pickupsView_2D_SpaceProjected)
                {
                    //pickupsView.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", pickupFilterTexture);
                    pickupsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetFloat("_DataWidth", texSize);
                    pickupsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetFloat("_DataHeight", texSize);
                    pickupsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetFloat("_ShowBrush", 0f);
                    pickupsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetFloat("_BrushAsFilter", 1f);
                    //pickupsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetFloat("_IndependentBrushing", BrushesAreIndependentFromQueries ? 1f : 0f);
                }

                dropoffsView_2D_SpaceProjected = sm.flatplot.transform.Find("Dropoffs-Space").gameObject.GetComponent<View>();
                if (dropoffsView_2D_SpaceProjected)
                {
                    //dropoffsView.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", dropoffFilterTexture);
                    dropoffsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetFloat("_DataWidth", texSize);
                    dropoffsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetFloat("_DataHeight", texSize);
                    dropoffsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetFloat("_ShowBrush", 0f);
                    dropoffsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetFloat("_BrushAsFilter", 1f);
                    //dropoffsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetFloat("_IndependentBrushing", BrushesAreIndependentFromQueries ? 1f : 0f);
                }

                linkingView_2D_SpaceProjected = sm.flatplot.GetComponent<LinkingViews>();
                if (linkingView_2D_SpaceProjected)
                {
                    //linkingView.View.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", linkedViewFilterTexture);
                    linkingView_2D_SpaceProjected.View.BigMesh.SharedMaterial.SetFloat("_DataWidth", texSize);
                    linkingView_2D_SpaceProjected.View.BigMesh.SharedMaterial.SetFloat("_DataHeight", texSize);
                    linkingView_2D_SpaceProjected.View.BigMesh.SharedMaterial.SetFloat("_ShowBrush", 0f);
                    linkingView_2D_SpaceProjected.View.BigMesh.SharedMaterial.SetFloat("_BrushAsFilter", 1f);
                    //linkingView_2D_SpaceProjected.View.BigMesh.SharedMaterial.SetFloat("_IndependentBrushing", BrushesAreIndependentFromQueries ? 1f : 0f);
                }
            }

            if (sm.temporalflatplot != null)
            {
                pickupsView_2D_TimeProjected = sm.temporalflatplot.transform.Find("Pickups-Time").gameObject.GetComponent<View>();
                if (pickupsView_2D_TimeProjected)
                {
                    //pickupsView.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", pickupFilterTexture);
                    pickupsView_2D_TimeProjected.BigMesh.SharedMaterial.SetFloat("_DataWidth", texSize);
                    pickupsView_2D_TimeProjected.BigMesh.SharedMaterial.SetFloat("_DataHeight", texSize);
                    pickupsView_2D_TimeProjected.BigMesh.SharedMaterial.SetFloat("_ShowBrush", 0f);
                    pickupsView_2D_TimeProjected.BigMesh.SharedMaterial.SetFloat("_BrushAsFilter", 1f);
                    //pickupsView_2D_TimeProjected.BigMesh.SharedMaterial.SetFloat("_IndependentBrushing", BrushesAreIndependentFromQueries ? 1f : 0f);
                }

                dropoffsView_2D_TimeProjected = sm.temporalflatplot.transform.Find("Dropoffs-Time").gameObject.GetComponent<View>();
                if (dropoffsView_2D_TimeProjected)
                {
                    //dropoffsView.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", dropoffFilterTexture);
                    dropoffsView_2D_TimeProjected.BigMesh.SharedMaterial.SetFloat("_DataWidth", texSize);
                    dropoffsView_2D_TimeProjected.BigMesh.SharedMaterial.SetFloat("_DataHeight", texSize);
                    dropoffsView_2D_TimeProjected.BigMesh.SharedMaterial.SetFloat("_ShowBrush", 0f);
                    dropoffsView_2D_TimeProjected.BigMesh.SharedMaterial.SetFloat("_BrushAsFilter", 1f);
                    //dropoffsView_2D_TimeProjected.BigMesh.SharedMaterial.SetFloat("_IndependentBrushing", BrushesAreIndependentFromQueries ? 1f : 0f);
                }

                linkingView_2D_TimeProjected = sm.temporalflatplot.GetComponent<LinkingViews>();
                if (linkingView_2D_TimeProjected)
                {
                    //linkingView.View.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", linkedViewFilterTexture);
                    linkingView_2D_TimeProjected.View.BigMesh.SharedMaterial.SetFloat("_DataWidth", texSize);
                    linkingView_2D_TimeProjected.View.BigMesh.SharedMaterial.SetFloat("_DataHeight", texSize);
                    linkingView_2D_TimeProjected.View.BigMesh.SharedMaterial.SetFloat("_ShowBrush", 0f);
                    linkingView_2D_TimeProjected.View.BigMesh.SharedMaterial.SetFloat("_BrushAsFilter", 1f);
                    //linkingView_2D_TimeProjected.View.BigMesh.SharedMaterial.SetFloat("_IndependentBrushing", BrushesAreIndependentFromQueries ? 1f : 0f);
                }
            }

            /*
            combinedRenderTexture = new RenderTexture(texSize, texSize, 24);
            combinedRenderTexture.enableRandomWrite = true;
            combinedRenderTexture.filterMode = FilterMode.Point;
            combinedRenderTexture.Create();

            textureCombineComputeShader.SetFloat("_size", texSize);
            textureCombineComputeShader.SetTexture(kernelComputeBrushTexture, "Result", combinedRenderTexture);
            */
        }
    }



    public void UpdateQueries()
    {
        foreach(Query q in allQueries)
        {
            q.UpdateQueryAfterSTCInteraction();
        }
    }

    public void UpdateQueriesStats()
    {
        foreach (Query q in allQueries)
        {
            q.RecomputeQueryStats();
        }
    }

    public void SetQueriesToFadedMode()
    {
        InEditQueriesMode = false;
        InNewQueryMode = false;

        foreach (Query q in allQueries)
        {
            q.SetTransparency(useModeQueryTransparency);
            q.DisableColliders();
            q.HideButtons();
        }
    }

    public void SetQueriesToEditMode()
    {
        InEditQueriesMode = true;

        foreach (Query q in allQueries)
        {
            q.SetTransparency(editModeQueryTransparency);
            q.EnableColliders();
            q.RevealButtons();
        }

        if (InODBrushingMode)
        {
            SwitchODBrushVisibility();
        }
        if (InRegularBrushingMode)
        {
            SwitchRegularBrushVisibility();
        }
    }


    public void SwitchODBrushVisibility()
    {
        leftODBrush.gameObject.SetActive(!leftODBrush.gameObject.activeSelf);
        rightODBrush.gameObject.SetActive(!rightODBrush.gameObject.activeSelf);

        ODBrushingDirectionalQuery.gameObject.SetActive(false);
        ODBrushingArrow.gameObject.SetActive(false);

        InODBrushingMode = !InODBrushingMode;

        if (InRegularBrushingMode && InODBrushingMode)
        {
            SwitchRegularBrushVisibility();
        }

        if (InODBrushingMode && texSize == 0)
        {
            ResetIATKFilterTexSize(NextPowerOf2((int)Mathf.Sqrt(GameObject.Find("STC-Pickups").GetComponent<View>().BigMesh.GetNumberVertices())));
        }


        if (!InODBrushingMode)
        {
            ResetIATKBrushers();
        }
        
    }

    public void SwitchRegularBrushVisibility()
    {
        leftRegularBrush.gameObject.SetActive(!leftRegularBrush.gameObject.activeSelf);
        rightRegularBrush.gameObject.SetActive(!rightRegularBrush.gameObject.activeSelf);

        InRegularBrushingMode = !InRegularBrushingMode;

        if(InRegularBrushingMode && InODBrushingMode)
        {
            SwitchODBrushVisibility();
        }

        if (InRegularBrushingMode && texSize == 0)
        {
            ResetIATKFilterTexSize(NextPowerOf2((int)Mathf.Sqrt(GameObject.Find("STC-Pickups").GetComponent<View>().BigMesh.GetNumberVertices())));
        }


        if (!InRegularBrushingMode)
        {
            ResetIATKBrushers();
        }

    }


    public void AddAtomicQuery(AtomicQuery q)
    {
        allQueries.Add(q);
        atomicQueries.Add(q);

        //switch(q.type)
        //{
        //    case AtomicQuery.QueryType.Destination: atomicDestinationQueriesCount += 1; break;
        //    case AtomicQuery.QueryType.Origin: atomicOriginQueriesCount += 1; break;
        //    case AtomicQuery.QueryType.Either: atomicEitherOriginOrDestinationQueriesCount += 1; break;
        //}

        RecentlyAddedQuery = true; 
        ResetIATKFilterTexSize(NextPowerOf2((int)Mathf.Sqrt(q.queryIATKDataVertexIntersectionComputer.brushedViews[0].BigMesh.GetNumberVertices())));
    }

    public void RemoveAtomicQuery(AtomicQuery q)
    {
        allQueries.Remove(q);
        bool done = atomicQueries.Remove(q);

        if(done)
        { 
            RecentlyDeletedQuery = true;
        }

    }

    public void AddDirectionalQuery(DirectionalQuery dq)
    {
        allQueries.Add(dq);
        directionalQueries.Add(dq);    
        RecentlyAddedQuery = true;
    }

    public void RemoveDirectionalQuery(DirectionalQuery dq)
    {
        allQueries.Remove(dq);
        directionalQueries.Remove(dq);
        RecentlyDeletedQuery = true;
    }

    public void AddMergedQuery(MergedQuery mq)
    {
        allQueries.Add(mq);
        mergedQueries.Add(mq);
        RecentlyAddedQuery = true;
    }

    public void RemoveMergedQuery(MergedQuery mq)
    {
        allQueries.Remove(mq);
        mergedQueries.Remove(mq);
        RecentlyDeletedQuery = true;
    }

    public void AddRecurrentQuery(RecurrentQuery rq)
    {
        allQueries.Add(rq);
        recurrentQueries.Add(rq);
        RecentlyAddedQuery = true;
    }

    public void RemoveRecurrentQuery(RecurrentQuery rq)
    {
        allQueries.Remove(rq);
        recurrentQueries.Remove(rq);
        RecentlyDeletedQuery = true;
    }



    private void ResetIATKFilters()
    {
        // RESET FILTERS AND SHOW ALL DATA POINTS

        if (texSize == 0)
            return;

       
            FillFilterTexture(pickupFilterTexture);
            FillFilterTexture(dropoffFilterTexture);
            FillFilterTexture(linkedViewFilterTexture);

            pickupsView.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", pickupFilterTexture);
            dropoffsView.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", dropoffFilterTexture);
            linkingView.View.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", linkedViewFilterTexture);

            pickupsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", pickupFilterTexture);
            dropoffsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", dropoffFilterTexture);
            linkingView_2D_SpaceProjected.View.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", linkedViewFilterTexture);

            pickupsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", pickupFilterTexture);
            dropoffsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", dropoffFilterTexture);
            linkingView_2D_TimeProjected.View.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", linkedViewFilterTexture);
        

        areFiltersBeingApplied = false; 
    }

    private void ResetIATKBrushers()
    {
        // RESET FILTERS AND SHOW ALL DATA POINTS

        if (texSize == 0)
            return;

       
            FillFilterTexture(pickupBrushingTexture);
            FillFilterTexture(dropoffBrushingTexture);
            FillFilterTexture(linkedViewODBrushingTexture);
        

        // APPLY PICKUP FILTER TEXTURE
        pickupsView.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", pickupBrushingTexture);

        // APPLY DROPOFF FILTER TEXTURE
        dropoffsView.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", dropoffBrushingTexture);

        // APPLY LINKING-VIEW FILTER TEXTURE
        linkingView.View.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", linkedViewODBrushingTexture);




        // APPLY PICKUP FILTER TEXTURE
        pickupsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", pickupBrushingTexture);

        // APPLY DROPOFF FILTER TEXTURE
        dropoffsView_2D_SpaceProjected.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", dropoffBrushingTexture);

        // APPLY LINKING-VIEW FILTER TEXTURE
        linkingView_2D_SpaceProjected.View.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", linkedViewODBrushingTexture);


        // APPLY PICKUP FILTER TEXTURE
        pickupsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", pickupBrushingTexture);

        // APPLY DROPOFF FILTER TEXTURE
        dropoffsView_2D_TimeProjected.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", dropoffBrushingTexture);

        // APPLY LINKING-VIEW FILTER TEXTURE
        linkingView_2D_TimeProjected.View.BigMesh.SharedMaterial.SetTexture("_ODBrushedTexture", linkedViewODBrushingTexture);


    }


    private void CheckQueryOrContraintChanges()
    {
        foreach(Query q in allQueries)
        {
            if (q.CheckChangesAndResetFlag())
            {
                updateQueue.Add(q);
                SignalNeedForTextureUpdate(); 
            }
        }

        if(acm != null && acm.isConstraining && acm.constraintsChanged)
        {
            acm.constraintsChanged = false;

            // All queries will have to be recomputed to deduct the new constraints
            foreach (Query q in allQueries)
            {
                updateQueue.Add(q);
            }

            SignalNeedForTextureUpdate(MinInactiveIntervalBeforeUpdating); // The ACM already waited the delay period before updating

        }
    }

    private void SignalNeedForTextureUpdate()
    {
        NeedsTextureUpdate = true;
        timeLastUpdateSignal = Time.time;
    }

    private void SignalNeedForImmediateTextureUpdate()
    {
        NeedsTextureUpdate = true;
        timeLastUpdateSignal = Time.time - MinInactiveIntervalBeforeUpdating;
    }

    private void SignalNeedForTextureUpdate(float deductTime)
    {
        NeedsTextureUpdate = true;
        timeLastUpdateSignal = Time.time - deductTime;
    }




    public void TransformDQSubqueriesIntoRecurrent(DirectionalQuery dq, List<int> years, List<int> months, List<DayOfWeek> daysOfTheWeek, List<int> hours)
    {
        Query q1 = dq.originQuery;
        Query q2 = dq.destinationQuery;
        RecurrentQuery rq1 = null, rq2 = null; 

        UnmergeQueriesFromDirectionalQuery(dq);

        if(q1 is AtomicQuery)
            rq1 = TransformQueryIntoRecurrentQuery((AtomicQuery)q1, years, months, daysOfTheWeek, hours);
        else if (q1 is RecurrentQuery)
            rq1 = EditRecurrentQuery((RecurrentQuery)q1, years, months, daysOfTheWeek, hours);

        if (q2 is AtomicQuery)
            rq2 = TransformQueryIntoRecurrentQuery((AtomicQuery)q2, years, months, daysOfTheWeek, hours);
        else if (q2 is RecurrentQuery)
            rq2 = EditRecurrentQuery((RecurrentQuery)q2, years, months, daysOfTheWeek, hours);

        dq.RemoveQuery();

        MergeQueriesIntoDirectionalQuery(rq1, rq2);

    }


    public RecurrentQuery TransformQueryIntoRecurrentQuery(AtomicQuery originalQuery, List<int> years, List<int> months, List<DayOfWeek> daysOfTheWeek, List<int> hours)
    {
        RecurrentQuery rq = (new GameObject("Recurrent Query")).AddComponent<RecurrentQuery>();

        // Let's iterate over our lists creating the query slices

        rq.SystemUpdatingQueries = true;
        DateTime currentSliceTime = new DateTime(1500, 4, 22, 0, 0, 0); // this is just a marker for the very first slice
        int currentSliceExtentInHours = 0;

        foreach (int year in years)
        {
            foreach(int month in months)
            {
                // Our first challenge is transforming the daysOfWeek in specific days for each month
                List<int> days = new List<int>();
                foreach(DayOfWeek dow in daysOfTheWeek)
                {
                    days.AddRange(WeekdayToDayNumbers(year, month, dow));
                }

                foreach (int day in days)
                {
                    foreach(int hour in hours)
                    {
                        DateTime possibleNewSliceTime = new DateTime(year, month, day, hour, 00, 00);
                        if (sm.stc.containsTime(possibleNewSliceTime) || sm.stc.containsTime(possibleNewSliceTime.AddHours(1))) // optimization to minimize the number of useless slices and the incurred overhead
                        { 
                            if(Math.Abs((possibleNewSliceTime - currentSliceTime.AddHours(currentSliceExtentInHours)).TotalHours) > 0)
                            { // there is a gap between the slice we were building and the new slice we found, so let's finalize the previous one and start building a new one
                                if (currentSliceTime.Year != 1500) 
                                {
                                    // creating the previous one, unless it is the invalid marker
                                    AtomicQuery newSlice = (AtomicQuery)originalQuery.Clone();
                                    newSlice.SetQueryTimeSpanFromDates(currentSliceTime, TimeSpan.FromHours(currentSliceExtentInHours));
                                    newSlice.AdjustWallProjectionsVertically();
                                    rq.slices.Add(newSlice);
                                }
                                // starting to build a new one
                                currentSliceTime = possibleNewSliceTime;
                                currentSliceExtentInHours = 1;
                            }
                            else
                            { // there is no gap, so let's just increment the slice we were already building
                                currentSliceExtentInHours += 1;
                            }
                        }
                    }
                }
            }
        }

        // creating the last slice
        AtomicQuery lastSlice = (AtomicQuery)originalQuery.Clone();
        lastSlice.SetQueryTimeSpanFromDates(currentSliceTime, TimeSpan.FromHours(currentSliceExtentInHours));
        lastSlice.AdjustWallProjectionsVertically();
        rq.slices.Add(lastSlice);

        rq.years = years;
        rq.months = months;
        rq.daysOfTheWeek = daysOfTheWeek;
        rq.hours = hours;

        rq.InitializeQuery();

        if(originalQuery.myButtons.GetComponent<AtomicQueryButtonsController>().timeSelector != null)
            rq.AdoptTimeSelector(originalQuery.myButtons.GetComponent<AtomicQueryButtonsController>().timeSelector); // inherit the same time selector to preserve user selections

        AddRecurrentQuery(rq);
        if(atomicQueries.Contains(originalQuery))
            RemoveAtomicQuery(originalQuery);
        Destroy(originalQuery.gameObject);

        return rq;
    }

    public void TransformAllQueriesIntoRecurrentQueries(List<int> years, List<int> months, List<DayOfWeek> daysOfTheWeek, List<int> hours)
    {
        List<Query> currentQueries = new List<Query>(allQueries);

        foreach(Query q in currentQueries)
        {
            if (q is RecurrentQuery)
                EditRecurrentQuery((RecurrentQuery)q, years, months, daysOfTheWeek, hours);
            else if (q is AtomicQuery)
                TransformQueryIntoRecurrentQuery((AtomicQuery)q, years, months, daysOfTheWeek, hours);
            else if (q is DirectionalQuery)
                TransformDQSubqueriesIntoRecurrent((DirectionalQuery)q, years, months, daysOfTheWeek, hours);
        }
    }

    public RecurrentQuery EditRecurrentQuery(RecurrentQuery rq, List<int> years, List<int> months, List<DayOfWeek> daysOfTheWeek, List<int> hours)
    {
        // THIS IS A TEMPORARY IMPLEMENTATION THAT SIMPLY DESTROYS THE RQ AND CREATES A NEW ONE
        // Ideally, we should determine which new slices must be created, which slices must be destroyed, and which slices can be preserved

        RecurrentQuery newRQ = TransformQueryIntoRecurrentQuery(rq.slices[0], years, months, daysOfTheWeek, hours);

        RemoveRecurrentQuery(rq);
        Destroy(rq.gameObject);

        return newRQ;
    }

    // Adapted from https://stackoverflow.com/a/7928172
    internal List<int> WeekdayToDayNumbers(int year, int month, DayOfWeek weekday)
    {
        int daysInMonth = DateTime.DaysInMonth(year, month);

        List<int> days = new List<int>();

        for (int n = 1; n <= daysInMonth; n++)
        {
            var date = new DateTime(year, month, n);

            if (date.DayOfWeek == weekday)
                days.Add(date.Day);
        }

        return days;
    }


    public void MergeQueriesIntoDirectionalQuery(Query originQuery, Query destinationQuery)
    {
        if (originQuery.type != Query.QueryType.Origin || destinationQuery.type != Query.QueryType.Destination)
            return;

        DirectionalQuery dq = GameObject.Instantiate(directionalQueryPrefab).GetComponent<DirectionalQuery>();
        dq.transform.name = "Directional Query";
        dq.originQuery = originQuery;
        dq.destinationQuery = destinationQuery;

        dq.InitializeQuery();
        AddDirectionalQuery(dq);

        originQuery.associatedDirectionalQueries.Add(dq);
        destinationQuery.associatedDirectionalQueries.Add(dq);

        if(originQuery is AtomicQuery)
            RemoveAtomicQuery((AtomicQuery)originQuery);
        else if (originQuery is RecurrentQuery)
            RemoveRecurrentQuery((RecurrentQuery)originQuery);
        if (destinationQuery is AtomicQuery)
            RemoveAtomicQuery((AtomicQuery)destinationQuery);
        else if (destinationQuery is RecurrentQuery)
            RemoveRecurrentQuery((RecurrentQuery)destinationQuery);
    }

    public void UnmergeQueriesFromDirectionalQuery(DirectionalQuery dq)
    {
        dq.originQuery.associatedDirectionalQueries.Remove(dq);
        dq.destinationQuery.associatedDirectionalQueries.Remove(dq);
       
        if(dq.originQuery.associatedDirectionalQueries.Count == 0) // If the only DQ linked to a subquery was dq, then let's transform it back into an atomic query. 
        {
            if (dq.originQuery is AtomicQuery)
                AddAtomicQuery((AtomicQuery)dq.originQuery);
            else if (dq.originQuery is RecurrentQuery)
                AddRecurrentQuery((RecurrentQuery)dq.originQuery);
            dq.originQuery.transform.parent = this.transform;
            dq.originQuery.RevealButtons();
            dq.originQuery.RevealTooltip();

            //originQuery gets to keep its color
        }
        else // Otherwise, let's "donate" it to another of its DQs.
        {
            dq.originQuery.associatedDirectionalQueries.Remove(dq);
            dq.originQuery.transform.parent = dq.originQuery.associatedDirectionalQueries[0].transform;
            dq.originQuery.queryColor = dq.originQuery.associatedDirectionalQueries[0].queryColor;
            dq.originQuery.RefreshColor(); // just in case it was colored in dq's color
        }

        if (dq.destinationQuery.associatedDirectionalQueries.Count == 0) // If the only DQ linked to a subquery was dq, then let's transform it back into an atomic query. 
        {
            if(dq.destinationQuery is AtomicQuery)
                AddAtomicQuery((AtomicQuery)dq.destinationQuery);
            else if (dq.destinationQuery is RecurrentQuery)
                AddRecurrentQuery((RecurrentQuery)dq.destinationQuery);
            dq.destinationQuery.transform.parent = this.transform;
            dq.destinationQuery.RevealButtons();
            dq.destinationQuery.RevealTooltip();

            // DEAL WITH QUERY COLORS -- if the originQuery is also becoming an atomic query, then we must find a new color for destinationQuery

            if(dq.destinationQuery.associatedDirectionalQueries.Count == 0)
            {
                if (availableColors.Count >= 1)
                    dq.destinationQuery.queryColor = availableColors.Dequeue();
                else
                    dq.destinationQuery.queryColor = Color.black;

                dq.destinationQuery.RefreshColor();
            }
        }
        else // Otherwise, let's "donate" it to another of its DQs.
        {
            dq.destinationQuery.associatedDirectionalQueries.Remove(dq);
            dq.destinationQuery.transform.parent = dq.destinationQuery.associatedDirectionalQueries[0].transform;
            dq.destinationQuery.queryColor = dq.destinationQuery.associatedDirectionalQueries[0].queryColor;
            dq.destinationQuery.RefreshColor(); // just in case it was colored in dq's color
        }

        RemoveDirectionalQuery(dq);
        GameObject.Destroy(dq.gameObject);
    }


    public void MergeQueriesIntoMergedQuery(Query q1, Query q2) // Q1 is defined through query buttons, so it can be AQ, DQ, or RQ
        // Q2 is defined through prism selection, so at first sight it is always AQ (but it might be a subquery to a DQ or RQ, or even to another MQ)
    {
        MergedQuery existingMq;
        Query newSubquery;

        if(IsQueryAlreadyPartOfAMerge(q1) && IsQueryAlreadyPartOfAMerge(q2)) // MERGE LISTS AND DESTROY MQ2
        {
            existingMq = IsQueryAlreadyPartOfAMerge(q1);
            existingMq.subqueries.AddRange(IsQueryAlreadyPartOfAMerge(q2).subqueries);
            existingMq.ReinitializeQuery();

            RemoveMergedQuery(IsQueryAlreadyPartOfAMerge(q2));
            Destroy(IsQueryAlreadyPartOfAMerge(q2).gameObject);
        }
        else if(IsQueryAlreadyPartOfAMerge(q1))
        {
            existingMq = IsQueryAlreadyPartOfAMerge(q1);
            newSubquery = q2;
            AddQueryToMergedQuery(existingMq, newSubquery);

            existingMq.ReinitializeQuery();
        }
        else if(IsQueryAlreadyPartOfAMerge(q2))
        {
            existingMq = IsQueryAlreadyPartOfAMerge(q2);
            newSubquery = q1;
            AddQueryToMergedQuery(existingMq, newSubquery);

            existingMq.ReinitializeQuery();
        }
        else
        {
            MergedQuery newMq = new GameObject().AddComponent<MergedQuery>();
            newMq.transform.name = "Merged Query";
            newMq.subqueries = new List<Query>();
            AddMergedQuery(newMq);

            AddQueryToMergedQuery(newMq, q1);
            AddQueryToMergedQuery(newMq, q2);

            newMq.InitializeQuery();
        }
    }

    public MergedQuery IsQueryAlreadyPartOfAMerge(Query q)
    {
        if (q is MergedQuery)
            return (MergedQuery)q;
        else if (q.associatedMergedQuery)
            return q.associatedMergedQuery;
        else
            return null;
    }

    public void AddQueryToMergedQuery(MergedQuery mq, Query q)
    {
        if (q is AtomicQuery)
        {
            AtomicQuery aq = (AtomicQuery)q;
            
            if(aq.associatedDirectionalQueries.Count > 0)
            {
                foreach(DirectionalQuery dq in aq.associatedDirectionalQueries)
                {
                    AddQueryToMergedQuery(mq, dq);
                }
            }
            
            if(aq.isPartOfARecurrentQuery) 
            {
                AddQueryToMergedQuery(mq, aq.associatedRecurrentQuery);
            }
            
            if(aq.associatedDirectionalQueries.Count == 0 && !aq.isPartOfARecurrentQuery) // purely an AQ
            {
                mq.subqueries.Add(aq);
                aq.associatedMergedQuery = mq;
                RemoveAtomicQuery(aq);
            }
        }
        else if(q is DirectionalQuery)
        {
            mq.subqueries.Add((DirectionalQuery)q);
            q.associatedMergedQuery = mq;
            RemoveDirectionalQuery((DirectionalQuery)q);
        }
        else if (q is RecurrentQuery)
        {
            mq.subqueries.Add((RecurrentQuery)q);
            q.associatedMergedQuery = mq;
            RemoveRecurrentQuery((RecurrentQuery)q);
        }
    }

   
    public void UnmergeQueriesFromMergedQuery(MergedQuery mq)
    {
        foreach (Query q in mq.subqueries)
        {
            q.transform.parent = transform;

            if (q is AtomicQuery)
                AddAtomicQuery((AtomicQuery)q);
            else if (q is RecurrentQuery)
                AddRecurrentQuery((RecurrentQuery)q);
            else if (q is DirectionalQuery)
                AddDirectionalQuery((DirectionalQuery)q);
        }


        foreach (Query q in mq.subqueries)
        {
            if (q is DirectionalQuery)
            {
                ((DirectionalQuery)q).originQuery.associatedMergedQuery = null;
                ((DirectionalQuery)q).destinationQuery.associatedMergedQuery = null;
            }
            if (q is RecurrentQuery)
            {
                foreach (Query slice in ((RecurrentQuery)q).slices)
                    slice.associatedMergedQuery = null;
            }
            if (q is AtomicQuery)
            {
                q.associatedDirectionalQueries = null;
            }
        }

        foreach (Query q in mq.subqueries)
            q.RevealButtons();

        foreach (Query q in mq.subqueries)
            q.RevealTooltip();

        // DEAL WITH QUERY COLORS

        availableColors.Enqueue(mq.queryColor);

        foreach (Query q in mq.subqueries)
        {
            if (availableColors.Count >= 1)
                q.queryColor = availableColors.Dequeue();
            else
                q.queryColor = Color.black;
            q.RefreshColor();
        }

        RemoveMergedQuery(mq);

        Destroy(mq.gameObject);
    }

    public void EnableLinkingMode(Query q)
    {
        linkingModeFirstQuery = q;
        InLinkingMode = true;
    }

    public void DisableLinkingMode()
    {
        linkingModeFirstQuery = null;
        InLinkingMode = false; 
    }

    public void EnableMergingMode(Query q)
    {
        mergingModeFirstQuery = q;
        InMergingMode = true;
    }

    public void DisableMergingMode()
    {
        mergingModeFirstQuery = null;
        InMergingMode = false;
    }

    public void SwitchLinkingModeStatus(Query q)
    {
        if (InLinkingMode)
            DisableLinkingMode();
        else
            EnableLinkingMode(q);
    }

    public void SwitchMergingModeStatus(Query q)
    {
        if (InLinkingMode)
            DisableMergingMode();
        else
            EnableMergingMode(q);
    }

    public void HandleQueryPrismClick(Query clickedQuery)
    {
        if(InLinkingMode)
        {
            bool done = TryToLinkQueries(clickedQuery);
        }
        if (InMergingMode)
        {
            bool done = TryToMergeQueries(clickedQuery);
        }

    }

    public void HandleQueryPrismHover(Query hoveredQuery)
    {
        if (InLinkingMode)
        {
            newArrowPreview.SetPosition(1, hoveredQuery.GetCentralPosition2D());
        }
        if (InMergingMode)
        {
            newMergeLinePreview.SetPosition(1, hoveredQuery.GetCentralPosition2D());
        }
    }


    public bool TryToLinkQueries(Query possibleLinkingTarget)
    {
        if (!InLinkingMode)
            return false; 

        if(linkingModeFirstQuery.type == AtomicQuery.QueryType.Origin && possibleLinkingTarget.type == AtomicQuery.QueryType.Destination)
        {
            MergeQueriesIntoDirectionalQuery(linkingModeFirstQuery, possibleLinkingTarget);
            DisableLinkingMode();
            return true; 
        }
        else if (linkingModeFirstQuery.type == AtomicQuery.QueryType.Destination && possibleLinkingTarget.type == AtomicQuery.QueryType.Origin)
        {
            MergeQueriesIntoDirectionalQuery(possibleLinkingTarget, linkingModeFirstQuery);
            DisableLinkingMode();
            return true; 
        }
        else if (linkingModeFirstQuery.type == AtomicQuery.QueryType.Either && possibleLinkingTarget.type == AtomicQuery.QueryType.Destination)
        {
            if (linkingModeFirstQuery is AtomicQuery)
                ((AtomicQuery)linkingModeFirstQuery).SetQueryModeToOnlyPickups();
            else if (linkingModeFirstQuery is RecurrentQuery)
                ((RecurrentQuery)linkingModeFirstQuery).SetQueryModeToOnlyPickups();
            else return false;
            MergeQueriesIntoDirectionalQuery(linkingModeFirstQuery, possibleLinkingTarget);
            DisableLinkingMode();
            return true;
        }
        else if (linkingModeFirstQuery.type == AtomicQuery.QueryType.Destination && possibleLinkingTarget.type == AtomicQuery.QueryType.Either)
        {
            if (possibleLinkingTarget is AtomicQuery)
                ((AtomicQuery)possibleLinkingTarget).SetQueryModeToOnlyPickups();
            else if (possibleLinkingTarget is RecurrentQuery)
                ((RecurrentQuery)possibleLinkingTarget).SetQueryModeToOnlyPickups();
            else return false;
            MergeQueriesIntoDirectionalQuery(possibleLinkingTarget, linkingModeFirstQuery);
            DisableLinkingMode();
            return true;
        }
        else if (linkingModeFirstQuery.type == AtomicQuery.QueryType.Either && possibleLinkingTarget.type == AtomicQuery.QueryType.Origin)
        {
            if (linkingModeFirstQuery is AtomicQuery)
                ((AtomicQuery)linkingModeFirstQuery).SetQueryModeToOnlyDropoffs();
            else if (linkingModeFirstQuery is RecurrentQuery)
                ((RecurrentQuery)linkingModeFirstQuery).SetQueryModeToOnlyDropoffs();
            else return false;
            MergeQueriesIntoDirectionalQuery(possibleLinkingTarget, linkingModeFirstQuery);
            DisableLinkingMode();
            return true;
        }
        else if (linkingModeFirstQuery.type == AtomicQuery.QueryType.Origin && possibleLinkingTarget.type == AtomicQuery.QueryType.Either)
        {
            if (possibleLinkingTarget is AtomicQuery)
                ((AtomicQuery)possibleLinkingTarget).SetQueryModeToOnlyDropoffs();
            else if (possibleLinkingTarget is RecurrentQuery)
                ((RecurrentQuery)possibleLinkingTarget).SetQueryModeToOnlyDropoffs();
            else return false;
            MergeQueriesIntoDirectionalQuery(linkingModeFirstQuery, possibleLinkingTarget);
            DisableLinkingMode();
            return true;
        }
        else if (linkingModeFirstQuery.type == AtomicQuery.QueryType.Either && possibleLinkingTarget.type == AtomicQuery.QueryType.Either)
        {
            if (linkingModeFirstQuery is AtomicQuery)
                ((AtomicQuery)linkingModeFirstQuery).SetQueryModeToOnlyPickups();
            else if (linkingModeFirstQuery is RecurrentQuery)
                ((RecurrentQuery)linkingModeFirstQuery).SetQueryModeToOnlyPickups();
            else return false;

            if (possibleLinkingTarget is AtomicQuery)
                ((AtomicQuery)possibleLinkingTarget).SetQueryModeToOnlyDropoffs();
            else if (possibleLinkingTarget is RecurrentQuery)
                ((RecurrentQuery)possibleLinkingTarget).SetQueryModeToOnlyDropoffs();
            else return false;

            MergeQueriesIntoDirectionalQuery(linkingModeFirstQuery, possibleLinkingTarget);
            DisableLinkingMode();
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool TryToMergeQueries(Query possibleMergingTarget)
    {
        if (!InMergingMode)
            return false;

        MergeQueriesIntoMergedQuery(mergingModeFirstQuery, possibleMergingTarget);
        DisableMergingMode();
        return true;
       
    }


    public void ComputeFullDataStats()
    {
        if (!(sm.stc is ODSTCManager))
            return;

        // Reset arrays

        numTripsPerTimeBin = new float[qr.numberOfBins];
        avgTotalAmountPerTripPerTimeBin = new float[qr.numberOfBins];
        avgFarePerMilePerTimeBin = new float[qr.numberOfBins];
        avgSpeedPerTimeBin = new float[qr.numberOfBins];
        avgDurationPerTripPerTimeBin = new float[qr.numberOfBins];
        avgDistancePerTrimePerTimeBin = new float[qr.numberOfBins];
        avgPassengerCountPerTripPerTimeBin = new float[qr.numberOfBins];

        // Compute vars per time bin

        for (int x = 0; x < texSize; x++)
        {
            for (int y = 0; y < texSize; y++)
            {
                //if (linkedViewFilterTex2D.GetPixel(x, y).r == 1f)
                //{
                    int index = x + y * texSize;
                    if (index >= sm.stc.csvdata.DataCount)
                        continue;

                float tripDistance = ((ODSTCManager)sm.stc).tripDistances[index];
                //  DateTime o_time = (DateTime)sm.stc.csvdata.getOriginalValue(sm.stc.csvdata["pickup_datetime"].Data[index], "pickup_datetime");
                //  DateTime d_time = (DateTime)sm.stc.csvdata.getOriginalValue(sm.stc.csvdata["dropoff_datetime"].Data[index], "dropoff_datetime");
                //  TimeSpan tripDuration = (d_time - o_time);

                float passengerCount = ((ODSTCManager)sm.stc).tripPassengerCounts[index];
                float totalAmount = ((ODSTCManager)sm.stc).tripTotalAmounts[index];

                    int i = qr.MapTimeToBinNumber(((ODSTCManager)sm.stc).tripOriginTimes[index]);

                    numTripsPerTimeBin[i]++;

                    avgTotalAmountPerTripPerTimeBin[i] += totalAmount;
                    if (tripDistance > 0)
                        avgFarePerMilePerTimeBin[i] += totalAmount / tripDistance;
                    if (((ODSTCManager)sm.stc).tripDurationsInHours[index] > 0)
                        avgSpeedPerTimeBin[i] += tripDistance / ((ODSTCManager)sm.stc).tripDurationsInHours[index];
                    avgDurationPerTripPerTimeBin[i] += ((ODSTCManager)sm.stc).tripDurationsInMinutes[index];
                    avgDistancePerTrimePerTimeBin[i] += tripDistance;
                    avgPassengerCountPerTripPerTimeBin[i] += passengerCount;
                //}
            }
        }

        // Compute averages

        for (int i = 0; i < qr.numberOfBins; i++)
        {
            if (numTripsPerTimeBin[i] > 0)
            {
                avgTotalAmountPerTripPerTimeBin[i] /= numTripsPerTimeBin[i];
                avgFarePerMilePerTimeBin[i] /= numTripsPerTimeBin[i];
                avgSpeedPerTimeBin[i] /= numTripsPerTimeBin[i];
                avgDurationPerTripPerTimeBin[i] /= numTripsPerTimeBin[i];
                avgDistancePerTrimePerTimeBin[i] /= numTripsPerTimeBin[i];
                avgPassengerCountPerTripPerTimeBin[i] /= numTripsPerTimeBin[i];
            }

        }
    }


    public void ComputeFullDataStatsMinusACMConstraints()
    {
        if (!(sm.stc is ODSTCManager))
            return;

        // Reset arrays

        numTripsPerTimeBin = new float[qr.numberOfBins];
        avgTotalAmountPerTripPerTimeBin = new float[qr.numberOfBins];
        avgFarePerMilePerTimeBin = new float[qr.numberOfBins];
        avgSpeedPerTimeBin = new float[qr.numberOfBins];
        avgDurationPerTripPerTimeBin = new float[qr.numberOfBins];
        avgDistancePerTrimePerTimeBin = new float[qr.numberOfBins];
        avgPassengerCountPerTripPerTimeBin = new float[qr.numberOfBins];

        // Compute vars per time bin

        for (int x = 0; x < acm.filterTextureAsTex2D.width; x++)
        {
            for (int y = 0; y < acm.filterTextureAsTex2D.height; y++)
            {
                if (acm.filterTextureAsTex2D.GetPixel(x, y).r == 1f)
                {
                    int index = x + y * texSize;
                    if (index >= sm.stc.csvdata.DataCount)
                        continue;

                    float tripDistance = ((ODSTCManager)sm.stc).tripDistances[index];
                    //DateTime o_time = (DateTime)sm.stc.csvdata.getOriginalValue(sm.stc.csvdata["pickup_datetime"].Data[index], "pickup_datetime");
                    //DateTime d_time = (DateTime)sm.stc.csvdata.getOriginalValue(sm.stc.csvdata["dropoff_datetime"].Data[index], "dropoff_datetime");
                    //TimeSpan tripDuration = (d_time - o_time);

                    float passengerCount = ((ODSTCManager)sm.stc).tripPassengerCounts[index];
                    float totalAmount = ((ODSTCManager)sm.stc).tripTotalAmounts[index];

                    int i = qr.MapTimeToBinNumber(((ODSTCManager)sm.stc).tripOriginTimes[index]);

                    numTripsPerTimeBin[i]++;

                    avgTotalAmountPerTripPerTimeBin[i] += totalAmount;
                    if (tripDistance > 0)
                        avgFarePerMilePerTimeBin[i] += totalAmount / tripDistance;
                    if (((ODSTCManager)sm.stc).tripDurationsInHours[index] > 0)
                        avgSpeedPerTimeBin[i] += tripDistance / ((ODSTCManager)sm.stc).tripDurationsInHours[index];
                    avgDurationPerTripPerTimeBin[i] += ((ODSTCManager)sm.stc).tripDurationsInMinutes[index];
                    avgDistancePerTrimePerTimeBin[i] += tripDistance;
                    avgPassengerCountPerTripPerTimeBin[i] += passengerCount;

                }
            }
        }

        // Compute averages

        for (int i = 0; i < qr.numberOfBins; i++)
        {
            if (numTripsPerTimeBin[i] > 0)
            {
                avgTotalAmountPerTripPerTimeBin[i] /= numTripsPerTimeBin[i];
                avgFarePerMilePerTimeBin[i] /= numTripsPerTimeBin[i];
                avgSpeedPerTimeBin[i] /= numTripsPerTimeBin[i];
                avgDurationPerTripPerTimeBin[i] /= numTripsPerTimeBin[i];
                avgDistancePerTrimePerTimeBin[i] /= numTripsPerTimeBin[i];
                avgPassengerCountPerTripPerTimeBin[i] /= numTripsPerTimeBin[i];
            }

        }
    }







    /// <summary>
    /// Finds the next power of 2 for a given number.
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    private int NextPowerOf2(int number)
    {
        int pos = 0;

        while (number > 0)
        {
            pos++;
            number = number >> 1;
        }
        return (int)Mathf.Pow(2, pos);
    }



    private float currentTime = 0;
    public float LogTimeSincePreviousLog()
    {
        float delta = Time.time - currentTime;
        currentTime = Time.time;
        return delta;
    }
    public void ResetTimeLogger()
    {
        currentTime = Time.time;
    }

    public bool IsBrushing()
    {
        return leftODBrush.isBrushing || rightODBrush.isBrushing || leftRegularBrush.isBrushing || rightRegularBrush.isBrushing;
    }















    private int kernelComputeBrushTexture;
    private bool areBuffersInitialized = false;
    private RenderTexture combinedRenderTexture;

 


    private RenderTexture CombineTexturesUsingCS(RenderTexture rt1, RenderTexture rt2)
    {
        combinedRenderTexture = new RenderTexture(texSize, texSize, 24);
        combinedRenderTexture.enableRandomWrite = true;
        combinedRenderTexture.filterMode = FilterMode.Point;
        combinedRenderTexture.Create();

        textureCombineComputeShader.SetFloat("_size", texSize);
        textureCombineComputeShader.SetTexture(kernelComputeBrushTexture, "Result", combinedRenderTexture);

        textureCombineComputeShader.SetTexture(kernelComputeBrushTexture, "T1", rt1);

        textureCombineComputeShader.SetTexture(kernelComputeBrushTexture, "T2", rt2);

        // Run the compute shader
        textureCombineComputeShader.Dispatch(kernelComputeBrushTexture, Mathf.CeilToInt(texSize / 32f), Mathf.CeilToInt(texSize / 32f), 1);


        return combinedRenderTexture;
    }

    /// <summary>
    /// Updates the brushedIndicesTexture using the visualisations set in the brushingVisualisations list.
    /// </summary>
    public RenderTexture CombineTexturesWithOrUsingCS(RenderTexture rt1, RenderTexture rt2)
    {
        textureCombineComputeShader.SetBool("useAnd", false);

        return CombineTexturesUsingCS(rt1, rt2);
    }

    public RenderTexture CombineTexturesWithAndUsingCS(RenderTexture rt1, RenderTexture rt2)
    {
        textureCombineComputeShader.SetBool("useAnd", true);

        return CombineTexturesUsingCS(rt1, rt2);
    }

    public static RenderTexture ClearFilterTexture(RenderTexture renderTexture)
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = rt;
        return renderTexture;
    }

    public static RenderTexture FillFilterTexture(RenderTexture renderTexture)
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.red);
        RenderTexture.active = rt;
        return renderTexture;
    }

}
