using IATK;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Networking.Types;


// This class is adapted from IATK's BrushingAndLinking.cs
// The goal is to apply brushes directly to IATK Views created programatically
// without using IATK Visualizations
// It also implements additional experimental functionalities such as
// cube-transform brushing and "BrushAsFilter"



public class IATKViewFilter : MonoBehaviour
{
    //[SerializeField]
    //public 
    public ComputeShader computeShader;
    //[SerializeField]
    public Material myRenderMaterial;



    [SerializeField]
    public bool isActive; // isBrushing
    [SerializeField]
    public Color brushColor = Color.red;
    //[SerializeField]
    //[Range(0f, 1f)]
    //public float brushRadius;
    //[SerializeField]
    //public bool showBrush = false;
    /// <summary>
    ///[SerializeField]
    /// </summary>
    //public bool brushAsFilter = false;


    //[SerializeField]
    //[Range(1f, 10f)]
    //public float brushSizeFactor = 1f;

    //[SerializeField]
    public Transform refTransform;
    //[SerializeField]
    //public Transform input2;
    public float scaleMultiplier = 1f;
    //[SerializeField]
    public BrushShape BRUSH_SHAPE;

    public BrushMode BRUSH_MODE;

    public bool OnlyGenerateTexturesAndWaitForQueryManager = true;

    //    public AtomicQuery myQuery = null; 
    public Query myQuery = null;



    public enum BrushMode
    {
        BRUSH = 0, // showBrush = true
        SELECT = 1 // brushAsFilter = true
    };



    public enum BrushShape
    {
        SPHERE = 0,
        BOX = 1,
        CYLINDER = 2,
        PRISM = 3
    };

    [SerializeField]
    public BrushType BRUSH_TYPE;
    public enum BrushType
    {
        FREE = 0,
        ADD,
        SUBTRACT
    }

    [SerializeField]
    public List<View> brushedViews;
    [SerializeField]
    public List<LinkingViews> brushedLinkingViews;

    //[SerializeField]
    //private List<int> brushedIndices;


    private int brushedCount = 0; //debug
    private int selectedCount = 0;//debug

    //[SerializeField]
    //public Material debugObjectTexture;

    private int kernelComputeBrushTexture;
   /// private int kernelComputeBrushedIndices;

    public RenderTexture brushedIndicesTexture;
    public int texSize;

    private ComputeBuffer dataBuffer;
    private ComputeBuffer filteredIndicesBuffer;
    private ComputeBuffer brushedIndicesBuffer;

    public bool hasInitialised = false;
    private bool hasFreeBrushReset = false;
    public bool hasBrushed = false; 
    private AsyncGPUReadbackRequest brushedIndicesRequest;


    private void Start()
    {
        Time.fixedDeltaTime = 0.1f;

        InitialiseShaders();
    }

    /// <summary>
    /// Initialises the indices for the kernels in the compute shader.
    /// </summary>
    private void InitialiseShaders()
    {
        //computeShader = (ComputeShader)Instantiate(Resources.Load("MyComputeShader"));
        computeShader = Instantiate(computeShader);
        myRenderMaterial = Instantiate(myRenderMaterial);

        kernelComputeBrushTexture = computeShader.FindKernel("CSMain");
        //kernelComputeBrushedIndices = computeShader.FindKernel("ComputeBrushedIndicesArray");
    }

    /// <summary>
    /// Initialises the buffers and textures necessary for the brushing and linking to work.
    /// </summary>
    /// <param name="dataCount"></param>
    private void InitialiseBuffersAndTextures(int dataCount)
    {
        dataBuffer = new ComputeBuffer(dataCount, 12);
        dataBuffer.SetData(new Vector3[dataCount]);
        computeShader.SetBuffer(kernelComputeBrushTexture, "dataBuffer", dataBuffer);

        ///filteredIndicesBuffer = new ComputeBuffer(dataCount, 4);
        ///filteredIndicesBuffer.SetData(new float[dataCount]);
        ///computeShader.SetBuffer(kernelComputeBrushTexture, "filteredIndicesBuffer", filteredIndicesBuffer);

 ///       brushedIndicesBuffer = new ComputeBuffer(dataCount, 4);
 ///       brushedIndicesBuffer.SetData(Enumerable.Repeat(-1, dataCount).ToArray());
 ///       computeShader.SetBuffer(kernelComputeBrushedIndices, "brushedIndicesBuffer", brushedIndicesBuffer);

        texSize = NextPowerOf2((int)Mathf.Sqrt(dataCount));
        brushedIndicesTexture = new RenderTexture(texSize, texSize, 24);
        brushedIndicesTexture.enableRandomWrite = true;
        brushedIndicesTexture.filterMode = FilterMode.Point;
        brushedIndicesTexture.Create();

        //Material mat = new Material(myRenderMaterial.shader);
        //mat.CopyPropertiesFromMaterial(myRenderMaterial);
        //myRenderMaterial = mat;

        myRenderMaterial.SetTexture("_MainTex", brushedIndicesTexture);

        computeShader.SetFloat("_size", texSize);
        computeShader.SetTexture(kernelComputeBrushTexture, "Result", brushedIndicesTexture);
 ///       computeShader.SetTexture(kernelComputeBrushedIndices, "Result", brushedIndicesTexture);

        hasInitialised = true;
    }

    /// <summary>
    /// Updates the computebuffers with the values specific to the currently brushed visualisation.
    /// </summary>
    /// <param name="visualisation"></param>
    public void UpdateComputeBuffers(View view)
    {
        //if (visualisation.visualisationType == AbstractVisualisation.VisualisationTypes.SCATTERPLOT)
        //{
        dataBuffer.SetData(view.BigMesh.getBigMeshVertices());
        computeShader.SetBuffer(kernelComputeBrushTexture, "dataBuffer", dataBuffer);

        ///filteredIndicesBuffer.SetData(view.GetFilterChannel());
        ///computeShader.SetBuffer(kernelComputeBrushTexture, "filteredIndicesBuffer", filteredIndicesBuffer);
        //}
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

    //public void Update()
/*    public void FixedUpdate()
    {

        if (OnlyGenerateTexturesAndWaitForQueryManager)
            return;

        if (isActive && brushedViews.Count > 0 && !hasInitialised)
            InitialiseBuffersAndTextures(brushedViews[0].BigMesh.GetNumberVertices());

        if (isActive && brushedViews.Count > 0)
        //if (isActive && brushedViews.Count > 0 && (myQuery == null || myQuery.prismChanged))
        ///if (isActive && brushedViews.Count > 0 && refTransform != null && (myQuery == null || myQuery.prismChanged)) 
        {
            if (hasInitialised)
            {
                UpdateBrushTexture();

                UpdateBrushedIndices();

                hasBrushed = true;

                //UnityEngine.Debug.Log("Brushing");

                DebugBrushedCount();
            }
            else
            {
                //InitialiseBuffersAndTextures(brushedViews[0].dataSource.DataCount);
                InitialiseBuffersAndTextures(brushedViews[0].BigMesh.GetNumberVertices());
            }
        }

    }*/

    public void Refilter()
    {
        if (isActive && brushedViews.Count > 0)
        {
            if (hasInitialised)
            {
                UpdateBrushTexture();

                ///UpdateBrushedIndices();

                hasBrushed = true;

                //UnityEngine.Debug.Log("Brushing");

                //DebugBrushedCount();
            }
            else
            {
                //InitialiseBuffersAndTextures(brushedViews[0].dataSource.DataCount);
                InitialiseBuffersAndTextures(brushedViews[0].BigMesh.GetNumberVertices());

                this.Refilter();
            }
        }
    }


  //  void DebugBrushedCount()
  //  {
  //      this.brushedCount = this.GetBrushedIndices().Count;//
//
   //     this.selectedCount = this.GetBrushedIndices().Count;
  //  }


    /// <summary>
    /// Returns a list with all indices - if index > 0, index is brushed. It's not otherwise
    /// </summary>
    /// <returns></returns>
   /* public List<int> GetBrushedIndices()
    {

        
        UpdateBrushedIndices();
        List<int> indicesBrushed = new List<int>();

        if(brushedIndices != null)
        { 
            for (int i = 0; i < brushedIndices.Count; i++)
            {
                if (brushedIndices[i] > 0)
                    indicesBrushed.Add(i);
            }
        }
        */

        /*
        // por alguma razao metodo correto nao funciona mais, gambiarra nao paralelistica abaixo 
        List<int> indicesBrushed = new List<int>();

        for(int i = 0; i< brushedViews[0].BigMesh.GetNumberVertices(); i++)
        {
            int x = i % texSize;
            int y = (int)Mathf.Floor(i / texSize);
            float2 pos = float2(x, y);

            if (Result[pos].x > 0.0)
                brushedIndicesBuffer[id.x] = 1;
            else
                brushedIndicesBuffer[id.x] = -1;

        }
        */






        //foreach (var item in indicesBrushed)
        //{
        //    float xVal = brushingVisualisations[0].dataSource[brushingVisualisations[0].xDimension.Attribute].Data[item];
        //    float yVal = brushingVisualisations[0].dataSource[brushingVisualisations[0].yDimension.Attribute].Data[item];
        //    float zVal = brushingVisualisations[0].dataSource[brushingVisualisations[0].zDimension.Attribute].Data[item];

        //    //print("X: " + brushingVisualisations[0].dataSource.getOriginalValue(xVal, brushingVisualisations[0].xDimension.Attribute)
        //    //   + " Y: " + brushingVisualisations[0].dataSource.getOriginalValue(yVal, brushingVisualisations[0].yDimension.Attribute)
        //    //   + " Z: " + brushingVisualisations[0].dataSource.getOriginalValue(zVal, brushingVisualisations[0].zDimension.Attribute));
        //}

       // return indicesBrushed;
   // }

    /// <summary>
    /// Updates the brushedIndicesTexture using the visualisations set in the brushingVisualisations list.
    /// </summary>
    private void UpdateBrushTexture()
    {
        Vector3 projectedRefPos, projectedRefScale;
        //Vector3 projectedPointer2;

        computeShader.SetInt("BrushShape", (int)BRUSH_SHAPE);
        computeShader.SetInt("BrushMode", (int)BRUSH_MODE);
        computeShader.SetInt("BrushType", (int)BRUSH_TYPE);

        hasFreeBrushReset = false;

        foreach (var brushingView in brushedViews)
        {
            UpdateComputeBuffers(brushingView);

            switch (BRUSH_SHAPE)
            {
                case BrushShape.SPHERE:
                    //projectedRefPos = view.transform.InverseTransformPoint(refTransform.transform.position);
                    //projectedRefPos = brushedLinkingViews[0].transform.InverseTransformPoint(refTransform.transform.position);
                    projectedRefPos = brushingView.transform.InverseTransformPoint(refTransform.position);
                    //projectedRefPos = transform.transform.InverseTransformPoint(refTransform.transform.position);
                    //projectedRefPos = refTransform.transform.position;
                    projectedRefScale = brushingView.transform.InverseTransformVector(refTransform.localScale);

                    computeShader.SetFloat("RadiusSphere", scaleMultiplier * projectedRefScale.x / 2f);


                    computeShader.SetFloats("refPos", projectedRefPos.x, projectedRefPos.y, projectedRefPos.z);
                    computeShader.SetFloats("refScale", projectedRefScale.x, projectedRefScale.y, projectedRefScale.z);
                    //if (debugSphere)
                    //{ 
                    //debugSphere.transform.position = refTransform.transform.position;// + new Vector3(.5f, .5f, .5f); ;// projectedRefPos;
                    //debugSphere.transform.position = projectedRefPos;
                    //debugSphere.transform.localScale = new Vector3(2 * brushRadius, 2 * brushRadius, 2 * brushRadius);
                    //}
                    break;
                case BrushShape.CYLINDER:

                    projectedRefPos = brushingView.transform.InverseTransformPoint(refTransform.position);
                    //var projectedRefPosEdge = brushingView.transform.InverseTransformPoint(refTransform.position + new Vector3(refTransform.localScale.x/2,0,refTransform.localScale.z/2));

                    projectedRefScale = brushingView.transform.InverseTransformVector(refTransform.localScale);
                    computeShader.SetFloats("refScale", projectedRefScale.x, projectedRefScale.y, projectedRefScale.z);

                    //computeShader.SetFloats("refPos", refTransform.position.x, refTransform.position.y, refTransform.position.z);
                    computeShader.SetFloats("refPos", projectedRefPos.x, projectedRefPos.y, projectedRefPos.z);
                    computeShader.SetFloat("prismMinY", brushingView.transform.InverseTransformPoint(new Vector3(0, refTransform.position.y - refTransform.localScale.y, 0)).y);
                    computeShader.SetFloat("prismMaxY", brushingView.transform.InverseTransformPoint(new Vector3(0, refTransform.position.y + refTransform.localScale.y, 0)).y);
                    //computeShader.SetFloat("cylRadius", projectedRefPosEdge.z - projectedRefPos.z);
                    //computeShader.SetFloat("cylRadius", brushingView.transform.InverseTransformPoint(new Vector3(refTransform.localScale.x / 2f, 0, 0)).x);

                    //UnityEngine.Debug.Log(projectedRefPosEdge.z - projectedRefPos.z);

                    break;




                case BrushShape.BOX:
                    //projectedRefPos = view.transform.InverseTransformPoint(refTransform.transform.position);
                    ///projectedRefPos = transform.InverseTransformPoint(refTransform.transform.position);
                    projectedRefPos = brushingView.transform.InverseTransformPoint(refTransform.position);
                    //projectedPointer2 = view.transform.InverseTransformPoint(input2.transform.position);
                    //projectedPointer2 = transform.InverseTransformPoint(input2.transform.position);

                    projectedRefScale = brushingView.transform.InverseTransformVector(refTransform.localScale);
                    //projectedRefScale = brushingView.transform.InverseTransformPointUnscaled(refTransform.localScale);
                    //projectedRefScale = refTransform.localScale;

                    computeShader.SetFloats("refPos", projectedRefPos.x, projectedRefPos.y, projectedRefPos.z);
                    //computeShader.SetFloats("refPos", refTransform.transform.position.x, refTransform.transform.position.y, refTransform.transform.position.z);
                    ///computeShader.SetFloats("refScale", refTransform.localScale.z, refTransform.localScale.y, refTransform.localScale.x);
                    ///computeShader.SetFloats("refScale", refTransform.localScale.x, refTransform.localScale.y, refTransform.localScale.z);
                    computeShader.SetFloats("refScale", projectedRefScale.x, projectedRefScale.y, projectedRefScale.z);
                    //computeShader.SetFloats("pointer2", projectedPointer2.x, projectedPointer2.y, projectedPointer2.z);
                    break;

                case BrushShape.PRISM:

                    // https://cmwdexint.com/2017/12/04/computeshader-setfloats/

                    Vector4[] vertices = new Vector4[300];
                    int i = 0;

                    //Query q = this.transform.parent.GetComponent<Query>();
                    //List<GameObject> dots = q.queryListOfSpatialDots;
                    //foreach (GameObject dot in dots)
                    if(myQuery is AtomicQuery) { 
                        foreach (GameObject dot in ((AtomicQuery)myQuery).queryListOfSpatialDots)
                        {
                            Vector3 dotPos = brushingView.transform.InverseTransformPoint(dot.transform.position);
                            vertices[i++] = new Vector4(dotPos.x, dotPos.y, dotPos.z, 0f);
                        }
                        computeShader.SetFloat("numpolyvertices", ((AtomicQuery)myQuery).queryListOfSpatialDots.Count);
                        computeShader.SetFloat("prismMinY", brushingView.transform.InverseTransformPoint(new Vector3(0, ((AtomicQuery)myQuery).minY, 0)).y);
                        computeShader.SetFloat("prismMaxY", brushingView.transform.InverseTransformPoint(new Vector3(0, ((AtomicQuery)myQuery).maxY, 0)).y);
                    }
                    else if(myQuery is PointCounter)
                    {
                        foreach (GameObject dot in ((PointCounter)myQuery).queryListOfSpatialDots)
                        {
                            Vector3 dotPos = brushingView.transform.InverseTransformPoint(dot.transform.position);
                            vertices[i++] = new Vector4(dotPos.x, dotPos.y, dotPos.z, 0f);
                        }
                        computeShader.SetFloat("numpolyvertices", ((PointCounter)myQuery).queryListOfSpatialDots.Count);
                        computeShader.SetFloat("prismMinY", brushingView.transform.InverseTransformPoint(new Vector3(0, ((PointCounter)myQuery).minY, 0)).y);
                        computeShader.SetFloat("prismMaxY", brushingView.transform.InverseTransformPoint(new Vector3(0, ((PointCounter)myQuery).maxY, 0)).y);
                    }
                    else
                    {
                        break;
                    }

                    computeShader.SetVectorArray("prismPolyVertices", vertices);
                    //computeShader.SetFloat("prismMinY", brushingView.transform.InverseTransformPoint(new Vector3(0, q.minY, 0)).y);
                    //computeShader.SetFloat("prismMaxY", brushingView.transform.InverseTransformPoint(new Vector3(0, q.maxY, 0)).y);
                    //computeShader.SetFloat("numpolyvertices", dots.Count);



                    //computeShader.SetFloats("prismPolyVertices", new Vector3[5]);



                    break; 
                default:
                    break;
            }

            //set the filters and normalisation values of the brushing visualisation to the computer shader
            computeShader.SetFloat("_MinNormX", brushingView.GetMinNormX());
            computeShader.SetFloat("_MaxNormX", brushingView.GetMaxNormX());
            computeShader.SetFloat("_MinNormY", brushingView.GetMinNormY());
            computeShader.SetFloat("_MaxNormY", brushingView.GetMaxNormY());
            computeShader.SetFloat("_MinNormZ", brushingView.GetMinNormZ());
            computeShader.SetFloat("_MaxNormZ", brushingView.GetMaxNormZ());

            computeShader.SetFloat("_MinX", brushingView.GetMinX());
            computeShader.SetFloat("_MaxX", brushingView.GetMaxX());
            computeShader.SetFloat("_MinY", brushingView.GetMinY());
            computeShader.SetFloat("_MaxY", brushingView.GetMaxY());
            computeShader.SetFloat("_MinZ", brushingView.GetMinZ());
            computeShader.SetFloat("_MaxZ", brushingView.GetMaxZ());

            //computeShader.SetFloat("RadiusSphere", brushRadius);

            //computeShader.SetFloat("width", this.transform.localScale.x);
            //computeShader.SetFloat("height", this.transform.localScale.y);
            //computeShader.SetFloat("depth", this.transform.localScale.z);

            computeShader.SetFloat("width", brushingView.transform.localScale.x);
            computeShader.SetFloat("height", brushingView.transform.localScale.y);
            computeShader.SetFloat("depth", brushingView.transform.localScale.z);

            //computeShader.SetFloat("width", view.width);
            //computeShader.SetFloat("height", view.height);
            //computeShader.SetFloat("depth", view.depth);

            // Tell the shader whether or not the visualisation's points have already been reset by a previous brush, required to allow for
            // multiple visualisations to be brushed with the free selection tool
            if (BRUSH_TYPE == BrushType.FREE)
                computeShader.SetBool("HasFreeBrushReset", hasFreeBrushReset);


            //foreach (var view in view.theVisualizationObject.viewList)
            //{

            if(!OnlyGenerateTexturesAndWaitForQueryManager)
            { 
            brushingView.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", brushedIndicesTexture);
            brushingView.BigMesh.SharedMaterial.SetFloat("_DataWidth", texSize);
            brushingView.BigMesh.SharedMaterial.SetFloat("_DataHeight", texSize);
            brushingView.BigMesh.SharedMaterial.SetFloat("_ShowBrush", Convert.ToSingle(BRUSH_MODE == BrushMode.BRUSH));
            brushingView.BigMesh.SharedMaterial.SetFloat("_BrushAsFilter", Convert.ToSingle(BRUSH_MODE == BrushMode.SELECT));
            brushingView.BigMesh.SharedMaterial.SetColor("_BrushColor", brushColor);
                //}
            }

            // Run the compute shader
            computeShader.Dispatch(kernelComputeBrushTexture, Mathf.CeilToInt(texSize / 32f), Mathf.CeilToInt(texSize / 32f), 1);

            // Dispatch again
            //computeShader.Dispatch(kernelComputeBrushedIndices, Mathf.CeilToInt(brushedIndicesBuffer.count / 32f), 1, 1);
            // brushedIndicesRequest = AsyncGPUReadback.Request(brushedIndicesBuffer);


            hasFreeBrushReset = true;
        }

        if (!OnlyGenerateTexturesAndWaitForQueryManager)
        {
            if (brushedLinkingViews != null)
            {
                foreach (var linkingView in brushedLinkingViews)
                {
                    if (linkingView != null && linkingView.showLinks)
                    {
                        linkingView.View.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", brushedIndicesTexture);
                        linkingView.View.BigMesh.SharedMaterial.SetFloat("_DataWidth", texSize);
                        linkingView.View.BigMesh.SharedMaterial.SetFloat("_DataHeight", texSize);
                        linkingView.View.BigMesh.SharedMaterial.SetFloat("_ShowBrush", Convert.ToSingle(BRUSH_MODE == BrushMode.BRUSH));
                        linkingView.View.BigMesh.SharedMaterial.SetFloat("_BrushAsFilter", Convert.ToSingle(BRUSH_MODE == BrushMode.SELECT));
                        linkingView.View.BigMesh.SharedMaterial.SetColor("_BrushColor", brushColor);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Updates the brushedIndices list with the currently brushed indices. A value of 1 represents brushed, -1 represents not brushed (boolean values are not supported).
    /// </summary>
   /* private void UpdateBrushedIndices()
    {
        brushedIndicesRequest = AsyncGPUReadback.Request(brushedIndicesBuffer);

        // Wait for request to finish
        if (brushedIndicesRequest.done)
        {
            // Get values from request
            if (!brushedIndicesRequest.hasError)
            {
                brushedIndices = brushedIndicesRequest.GetData<int>().ToList();
                UnityEngine.Debug.Log("Updated brushed indices with " + brushedIndices.Count + " points");
            }

            // Dispatch again
            computeShader.Dispatch(kernelComputeBrushedIndices, Mathf.CeilToInt(brushedIndicesBuffer.count / 32f), 1, 1);
            brushedIndicesRequest = AsyncGPUReadback.Request(brushedIndicesBuffer);
        }
       
    }*/

    /// <summary>
    /// Releases the buffers on the graphics card.
    /// </summary>
    private void OnDestroy()
    {
        if (dataBuffer != null)
            dataBuffer.Release();

        if (filteredIndicesBuffer != null)
            filteredIndicesBuffer.Release();

        if (brushedIndicesBuffer != null)
            brushedIndicesBuffer.Release();
    }

    private void OnApplicationQuit()
    {
        if (dataBuffer != null)
            dataBuffer.Release();

        if (filteredIndicesBuffer != null)
            filteredIndicesBuffer.Release();

        if (brushedIndicesBuffer != null)
            brushedIndicesBuffer.Release();
    }
}
