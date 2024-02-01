# *Immersive TaxiVis*

This repository is a supplementary material to the research paper "***Reimagining TaxiVis through an Immersive Space-Time Cube metaphor and reflecting on potential benefits of Immersive Analytics for urban data exploration***", to be presented at the [IEEE VR 2024](https://ieeevr.org/2024/) conference by [Jorge Wagner](https://jorgewagner.com) (UFRGS), [Claudio Silva](https://ctsilva.github.io) (NYU), [Wolfgang Stuerzlinger](https://vvise.iat.sfu.ca/people/wolfgang-stuerzlinger) (SFU), and [Luciana Nedel](https://www.inf.ufrgs.br/~nedel/) (UFRGS). A preprint for the full publication is available [here](arxiv.com), and our supplementary video [here](https://www.youtube.com/watch?v=doBPhUHEyXo).

![Teaser image of some of the features of Immersive TaxiVis: bi-manual brushing, Space-Time Cube view, 3D city models, query prisms, and embedded time series plots](https://static.wixstatic.com/media/3cc1d5_a9a672a32a784dbcabfac05696582f4c~mv2.png)


Here, you can find the Unity-based implementation for our proof-of-concept research prototype demonstrating some of our ideas for *immersive extensions* and *immersive adaptations* conceived as part of an "immersive reimagination" of the landmark [*TaxiVis* system](https://github.com/VIDA-NYU/TaxiVis) by [Ferreira et al., 2013](https://ieeexplore.ieee.org/document/6634127). 

We implemented this prototype building on the [*Immersive Analytics Toolkit (IATK)*](https://github.com/MaximeCordeil/IATK) for data rendering, the [*Mixed Reality Toolkit (MRTK)*](https://github.com/microsoft/MixedRealityToolkit-Unity) for interaction support, and the [*Bing Maps SDK*](https://github.com/microsoft/MapsSDK-Unity) for mapping resources.





### Table of Contents
1. [How to try out our research prototype](#how)
2. [Explanation of main classes and scene elements](#explanation)
3. [How we integrated STC queries into IATK](#integrating)
4. [Limitations](#limitations)
5. [Known issues](#issues)
6. [Roadmap](#roadmap)
7. [Acknowledgments](#ack)
8. [Citation](#cite)


## <a id="how"></a> How to try out our research prototype

### Requirements

1. Unity (preferably version 2022.1.10f1)
2. This repository, which includes the following dependencies:
	1. IATK (an adapted version is included in `Assets/AdaptedIATK`)
	2. MRTK (loaded on startup via the Unity Package Manager)
	3. Bing Maps SDK (loaded on startup via the Unity Package Manager)
	4. Oculus Integration Package (included in `Assets/Oculus`)
	5. ProBuilder (loaded on startup via the Unity Package Manager)
	6. Formatted samples of the [NYC taxi dataset](https://databank.illinois.edu/datasets/IDB-9610843) (some included in `Assets/Resources/Data/taxi`)
3. Mapping API Keys 
	1. In order to view and interact with the STC base map, you will necessarily need to [create a Bing Maps API key](https://github.com/microsoft/MapsSDK-Unity/wiki/Getting-Started#2-create-a-bing-maps-key) and add it to a new text file `Assets/Resources/MapSessionConfig.txt`.
	2. If you want to use the egocentric 360&deg; images tool, you will also need to [create a Google Maps Static API key](https://developers.google.com/maps/documentation/maps-static/get-api-key) and add it to a new text file `Assets/Resources/GoogleSessionConfig.txt`.


### Instructions 

1. Download the repository.
2. Add your API key files (see above).
3. Open the Unity project.
4. Open the Assets/ImmersiveTaxiVis scene.
5. (Optional) Using the Unity inspector, select one of the available examples in the `DemoManager` scene element under *Demo Settings - Example*. 
6. You can then try the prototype by entering the *Play mode*.	
	-  If you have a VR HMD such as the Meta Quest 2/3 connected to your computer, both hand tracking and hand controller interactions are supported through MRTK.
	- For a limited experience without a VR HMD, you can use the mouse and keyboard through the [MRTK's input simulation system](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/input-simulation/input-simulation-service?view=mrtkunity-2022-05). 
	- Most features are accessible through the main menu attached to the desk edge or through the "palm up" hand menu if using the hand tracking mode. 
	- Please note that IATK-based visualizations unfortunately only work on Windows. 




## <a id="explanation"></a> Explanation of main classes and scene elements

To be added.


##  <a id="integrating"></a> How we integrated STC queries into IATK

Our prototype implementation demonstrates how the IATK code and its core optimizations can be programmatically adapted to construct a query-able Space-Time Cube. We adapted the original `Scatterplot View` from IATK to be dynamically attached to the interactions performed on the STC map and time walls. For datasets with a bipartite nature such as taxi data (independent OD pairs), we create two `View` components with `Point` topologies, as well as a `LinkingView`, whose visual OD connections are hidden by default but can be displayed on demand. We also create four additional `View` components to render bidimensional projections on space and time when enabled.

To implement efficient display and querying of millions of data points, *TaxiVis*’ architecture decoupled its rendering and storage components. Replicating this behavior would require dynamically generating new IATK big meshes for each query result, which could be impracticable without hindering interactivity and breaking the immersive experience. Therefore, for our proof-of-concept, we opted to instead heavily adapt the original `BrushingAndLinking` IATK class to compute all queries through Unity shaders and encode query results into three texture images (encoding filtered points, brushed points, and highlighted points). We also significantly extended the IATK `MyComputeShader` to enable filtering by arbitrary 3D prisms, and to enable several such prisms to be combined, through texture post-processing done by a second ComputeShader program, depending on the intended query logic. The overall query logic is managed by our `QueryManager` class.

By coupling the rendering and querying components of the system, we gain the ability to efficiently apply spatio-temporal filters that scale up to hundreds of thousands of data points, without having to regenerate the IATK `View` mesh. At the same time, this prevents us from computing queries on millions of data points, something that *TaxiVis* achieved (technically speaking, *TaxiVis* never rendered more than one million points at the same time, but could query and compute stats over larger sets of points in the background). We see this as a reasonable implementation compromise at this time.

Given our performance concerns to maintain a comfortable frame rate for VR exploration, we also worked to reduce the number of view updates when possible. 



## <a id="limitations"></a> Limitations

To be added.



## <a id="issues"></a> Known issues

We are aware of some minor implementation issues in our proof-of-concept prototype that we plan to fix in the future.

  

- The hand menu is not available when using hand controllers, leaving the user without access to the menu after switching to the egocentric room mode
-   ProBuilder is not able to render prisms / polygons correctly for some neighborhoods
-   OD linking lines sometimes disappear depending on camera position
-   Simultaneous DirectionalQueries should always be assigned independent colors but currently can share the same one
-   The current approach to data loading can be too slow depending on dataset size
-   Choropleth prism stack brushing can become too slow with a large number of time slices
-   The finger inspection lenses do not ignore data points clipped out from the STC view by the desk edges
-   The QueryCreator does not allow “drawing” a new query on the STC time wall first
-   Query map dots can’t be moved to modify a query
-   Map dot/arrow previews sometimes do not disappear when leaving the query mode
-   Query stats tooltip and query button panel positioning must be improved
-   Sometimes data points selected by the finger inspection lenses are not released after moving the finger away
-   2D map projections should take into account the 3D terrain when enabled
-   Data on the embedded and coordinated views can sometimes disappear
-   The geographical position of data points can be wrongly calculated when the map is too zoomed out

## <a id="roadmap"></a> Roadmap

Please refer to the paper for our discussion on other possible *immersive extensions* beyond those currently implemented into our proof-of-concept prototype and on other ideas for future work.

## <a id="ack"></a> Acknowledgments

### Implementation

The *RoomScenario* background prefab belongs to the Oculus Integration Package. The implementation of the *Graph* class, including *Line Graph* and *Histogram Graph* was originally inspired by benjmercier's [TimeSeriesLineGraph System](https://github.com/benjmercier/TimeSeriesLineGraph). The implementation of the line drawing feature was originally inspired by BLANKdev's [Pen Tool System](https://theblankdev.itch.io/linerenderseries).

### Research Project

*Jorge Wagner* was supported in this project by the Fulbright Foreign Student Program, by a Microsoft Research PhD Fellowship, and by the Brazilian National Council for Scientific and Technological Development (CNPq). 

*Claudio Silva* is partially supported by the DARPA PTG program, National Science Foundation CNS-1828576, and NASA. Any opinions, findings, conclusions, or recommendations expressed in this material are those of the authors and do not necessarily reflect the views of DARPA. 

*Wolfgang Stuerzlinger* acknowledges support from NSERC. 

*Luciana Nedel* also acknowledges support from CNPq. This study was financed in part by the Coordenação de Aperfeiçoamento de Pessoal de Nível Superior - Brasil (CAPES) - Finance Code 001.

## <a id="cite"></a> Citation

```
To be added.
```