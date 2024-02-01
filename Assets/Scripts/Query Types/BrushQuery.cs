using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System.Collections.Generic;
using UnityEngine;

public class BrushQuery : Query 
{
    public ScenarioManager sm;

    public GameObject cube, spatialProjection, temporalProjections3Sides, temporalProjections4Sides;// spatialEdge, temporalEdge;
    private GameObject temporalProjections;
    public TextMesh upperLeftLabel, upperCenterLabel, upperRightLabel, lowerLeftLabel, lowerCenterLabel, lowerRightLabel;

    public IATKViewFilter queryIATKDataVertexIntersectionComputer;

    public Microsoft.MixedReality.Toolkit.Utilities.Handedness myHandedness;

    public float side;

    List<IMixedRealityPointer> hoveringPointers;
    public Vector3 cursorPosition;

    public bool mapOnFocus = false, wallOnFocus = false, isBrushing = false;


  /*  public override void RecomputeQueryResults()
    {
        RecomputeFilterTexture();
        //DeductAttributeAndTemporalConstraints();
        //RecomputeQueryStats();
    }
    */

    public override void RecomputeFilterTexture()
    {
        queryIATKDataVertexIntersectionComputer.Refilter();

        if (filterTextureAsTex2D == null)
            filterTextureAsTex2D = new Texture2D(qm.texSize, qm.texSize);

        if (filterTexture == null)
        {
            filterTexture = new RenderTexture(qm.texSize, qm.texSize, 24);
            filterTexture.enableRandomWrite = true;
            filterTexture.filterMode = FilterMode.Point;
            filterTexture.Create();
        }

        RenderTexture rt = RenderTexture.active;

        RenderTexture.active = queryIATKDataVertexIntersectionComputer.brushedIndicesTexture;
        filterTextureAsTex2D.ReadPixels(new Rect(0, 0, filterTextureAsTex2D.width, filterTextureAsTex2D.height), 0, 0, false);
        filterTextureAsTex2D.Apply();

        RenderTexture.active = filterTexture;
        Graphics.Blit(filterTextureAsTex2D, filterTexture);

        RenderTexture.active = rt;
    }




    public void Start()
    {
        Color c = cube.GetComponent<Renderer>().material.color;
        c.a = 0.2f;
        cube.GetComponent<Renderer>().material.color = c;

        //cube.transform.localScale = new Vector3(side, cube.transform.localScale.y, side);
        //spatialProjection.transform.localScale = new Vector3(side, spatialProjection.transform.localScale.y, side);
        //spatialEdge.transform.localScale = new Vector3(side, side, spatialEdge.transform.localScale.z);

    }


    public void Update()
    {
        hoveringPointers = GetPointersHoveringMapOrWall();

        if (hoveringPointers.Count == 1)
        {
            if (hoveringPointers[0].Controller.ControllerHandedness == myHandedness && hoveringPointers[0].Result.Details.Object.name == sm.bingMap.gameObject.name)
            {
                mapOnFocus = true;
                wallOnFocus = false;
                cursorPosition = hoveringPointers[0].Result.Details.Point;
            }
            else if (hoveringPointers[0].Controller.ControllerHandedness == myHandedness && hoveringPointers[0].Result.Details.Object.name == sm.stc.walls.gameObject.name)
            {
                mapOnFocus = false;
                wallOnFocus = true;
                cursorPosition = hoveringPointers[0].Result.Details.Point;
            }
            else
            {
                mapOnFocus = false;
                wallOnFocus = false;
            }
        }
        else if (hoveringPointers.Count == 2)
        {
            if (hoveringPointers[0].Controller.ControllerHandedness == myHandedness && hoveringPointers[0].Result.Details.Object.name == sm.bingMap.gameObject.name)
            {
                mapOnFocus = true;
                wallOnFocus = false;
                cursorPosition = hoveringPointers[0].Result.Details.Point;
            }
            else if (hoveringPointers[0].Controller.ControllerHandedness == myHandedness && hoveringPointers[0].Result.Details.Object.name == sm.stc.walls.gameObject.name)
            {
                mapOnFocus = false;
                wallOnFocus = true;
                cursorPosition = hoveringPointers[0].Result.Details.Point;
            }
            else if (hoveringPointers[1].Controller.ControllerHandedness == myHandedness && hoveringPointers[1].Result.Details.Object.name == sm.bingMap.gameObject.name)
            {
                mapOnFocus = true;
                wallOnFocus = false;
                cursorPosition = hoveringPointers[1].Result.Details.Point;
            }
            else if (hoveringPointers[1].Controller.ControllerHandedness == myHandedness && hoveringPointers[1].Result.Details.Object.name == sm.stc.walls.gameObject.name)
            {
                mapOnFocus = false;
                wallOnFocus = true;
                cursorPosition = hoveringPointers[1].Result.Details.Point;
            }
            else
            {
                mapOnFocus = false;
                wallOnFocus = false;
            }
        }
        else
        {
            mapOnFocus = false;
            wallOnFocus = false;
        }


        if (mapOnFocus)
        {
            isBrushing = true; 

            spatialProjection.SetActive(true);
            //spatialEdge.SetActive(true);
            temporalProjections3Sides.SetActive(false);
            temporalProjections4Sides.SetActive(false);
            lowerLeftLabel.gameObject.SetActive(false); lowerCenterLabel.gameObject.SetActive(false); lowerRightLabel.gameObject.SetActive(false);
            upperLeftLabel.gameObject.SetActive(false); upperCenterLabel.gameObject.SetActive(false); upperRightLabel.gameObject.SetActive(false);
            //temporalEdge.SetActive(false);
            cube.SetActive(true);

            spatialProjection.transform.localScale = new Vector3(side, spatialProjection.transform.localScale.y, side);
            spatialProjection.transform.position = new Vector3(cursorPosition.x, sm.bingMap.transform.position.y + sm.bingMap.transform.localScale.y * 0.055f + (sm.bingMap.mapRenderer.IsClippingVolumeWallEnabled ? 0.039f : 0f), cursorPosition.z);
            //spatialEdge.transform.position = new Vector3(cursorPosition.x, sm.bingMap.transform.position.y + sm.bingMap.transform.localScale.y * 0.055f + (sm.bingMap.mapRenderer.IsClippingVolumeWallEnabled ? 0.039f : 0f), cursorPosition.z);
            cube.transform.localScale = new Vector3(side, sm.stc.transform.localScale.y, side);
            cube.transform.position = new Vector3(cursorPosition.x, sm.stc.baseHeight + sm.stc.transform.localScale.y / 2, cursorPosition.z);
        }
        else if (wallOnFocus)
        {
            isBrushing = true;

            if (sm.VirtualDesk)
            {
                temporalProjections = temporalProjections3Sides;
                temporalProjections4Sides.SetActive(false);
            }
            else
            {
                temporalProjections = temporalProjections4Sides;
                temporalProjections3Sides.SetActive(false);
            }

            spatialProjection.SetActive(false);
            //spatialEdge.SetActive(false);
            temporalProjections.SetActive(true);
            //temporalEdge.SetActive(true);
            cube.SetActive(true);


            //temporalArea.transform.localScale = new Vector3(sm.stc.transform.localScale.x, side, temporalArea.transform.localScale.z);
            temporalProjections.transform.localScale = new Vector3(sm.stc.transform.localScale.x, side, sm.stc.transform.localScale.z);
            temporalProjections.transform.position = new Vector3(sm.bingMap.transform.position.x, cursorPosition.y, sm.bingMap.transform.position.z);
            //temporalEdge.transform.position = new Vector3(cursorPosition.x, sm.bingMap.transform.position.y + sm.bingMap.transform.localScale.y * 0.055f + (sm.bingMap.mapRenderer.IsClippingVolumeWallEnabled ? 0.039f : 0f), cursorPosition.z);
            cube.transform.localScale = new Vector3(sm.stc.transform.localScale.x, side, sm.stc.transform.localScale.z);
            cube.transform.position = new Vector3(sm.bingMap.transform.position.x, cursorPosition.y, sm.bingMap.transform.position.z);
            //cube.transform.position = new Vector3(cursorPosition.x, sm.stc.baseHeight + sm.stc.transform.localScale.y / 2, cursorPosition.z);

            //  labels

            lowerLeftLabel.gameObject.SetActive(true); lowerCenterLabel.gameObject.SetActive(true); lowerRightLabel.gameObject.SetActive(true);
            upperLeftLabel.gameObject.SetActive(true); upperCenterLabel.gameObject.SetActive(true); upperRightLabel.gameObject.SetActive(true);
            lowerLeftLabel.transform.position = new Vector3(sm.bingMap.transform.position.x - 0.4975f * sm.stc.transform.localScale.x, temporalProjections.transform.position.y - temporalProjections.transform.localScale.y / 2 , sm.bingMap.transform.position.z);
            lowerCenterLabel.transform.position = new Vector3(sm.bingMap.transform.position.x, temporalProjections.transform.position.y - temporalProjections.transform.localScale.y / 2, sm.bingMap.transform.position.z + 0.4975f * sm.stc.transform.localScale.z);
            lowerRightLabel.transform.position = new Vector3(sm.bingMap.transform.position.x + 0.4975f * sm.stc.transform.localScale.x, temporalProjections.transform.position.y - temporalProjections.transform.localScale.y / 2, sm.bingMap.transform.position.z);
            upperLeftLabel.transform.position = new Vector3(sm.bingMap.transform.position.x - 0.4975f * sm.stc.transform.localScale.x, temporalProjections.transform.position.y + temporalProjections.transform.localScale.y / 2, sm.bingMap.transform.position.z);
            upperCenterLabel.transform.position = new Vector3(sm.bingMap.transform.position.x, temporalProjections.transform.position.y + temporalProjections.transform.localScale.y / 2, sm.bingMap.transform.position.z + 0.4975f * sm.stc.transform.localScale.z);
            upperRightLabel.transform.position = new Vector3(sm.bingMap.transform.position.x + 0.4975f * sm.stc.transform.localScale.x, temporalProjections.transform.position.y + temporalProjections.transform.localScale.y / 2, sm.bingMap.transform.position.z);
            lowerLeftLabel.text = sm.stc.mapYToTime(temporalProjections.transform.position.y - temporalProjections.transform.localScale.y / 2).ToString(sm.stc.walls.dateFormat, sm.stc.walls.culture);
            lowerCenterLabel.text = lowerLeftLabel.text;
            lowerRightLabel.text = lowerLeftLabel.text;
            upperLeftLabel.text = sm.stc.mapYToTime(temporalProjections.transform.position.y + temporalProjections.transform.localScale.y / 2).ToString(sm.stc.walls.dateFormat, sm.stc.walls.culture);
            upperCenterLabel.text = upperLeftLabel.text;
            upperRightLabel.text = upperLeftLabel.text;

        }
        else
        {
            spatialProjection.SetActive(false);
            //spatialEdge.SetActive(false);
            temporalProjections3Sides.SetActive(false);
            temporalProjections4Sides.SetActive(false);
            //temporalEdge.SetActive(false);
            cube.SetActive(false);
            lowerLeftLabel.gameObject.SetActive(false); lowerCenterLabel.gameObject.SetActive(false); lowerRightLabel.gameObject.SetActive(false);
            upperLeftLabel.gameObject.SetActive(false); upperCenterLabel.gameObject.SetActive(false); upperRightLabel.gameObject.SetActive(false);

            isBrushing = false; 
        }




    }

    /*public override void SolverUpdate()
    {
        if (SolverHandler != null && SolverHandler.TransformTarget != null)
        {
            var target = SolverHandler.TransformTarget;
            //GoalPosition = new Vector3(target.position.x, sm.bingMap.transform.position.y + 0.0055f, target.position.z);
            //GoalScale = new Vector3(target.lossyScale.x, transform.localScale.y, target.lossyScale.z);

            square.transform.position = new Vector3(target.position.x, sm.bingMap.transform.position.y + 0.055f, target.position.z);
            edge.transform.position = new Vector3(target.position.x, sm.bingMap.transform.position.y + 0.055f, target.position.z);
            cube.transform.position = new Vector3(target.position.x, sm.stc.baseHeight + sm.stc.transform.localScale.y / 2, target.position.z);

        }
    }
    */


    // Adapted from https://stackoverflow.com/a/56082649
    List<IMixedRealityPointer> GetPointersHoveringMap()
    {
        List<IMixedRealityPointer> pointers = new List<IMixedRealityPointer>();

        foreach (var source in CoreServices.InputSystem.DetectedInputSources)
        {
            // Ignore anything that is not a hand because we want articulated hands
            if (source.SourceType == Microsoft.MixedReality.Toolkit.Input.InputSourceType.Hand || source.SourceType == Microsoft.MixedReality.Toolkit.Input.InputSourceType.Controller)
            {
                foreach (var p in source.Pointers)
                {
                    if (p is IMixedRealityNearPointer)
                    {
                        // Ignore near pointers, we only want the rays
                        continue;
                    }

                    if (p.Result != null && p.Result.Details.Object != null && p.Result.Details.Object.name == sm.bingMap.gameObject.name)
                    {
                        pointers.Add(p);
                    }

                }
            }
        }

        return pointers;
    }


    // Adapted from https://stackoverflow.com/a/56082649
    List<IMixedRealityPointer> GetPointersHoveringMapOrWall()
    {
        List<IMixedRealityPointer> pointers = new List<IMixedRealityPointer>();

        foreach (var source in CoreServices.InputSystem.DetectedInputSources)
        {
            // Ignore anything that is not a hand because we want articulated hands
            if (source.SourceType == Microsoft.MixedReality.Toolkit.Input.InputSourceType.Hand || source.SourceType == Microsoft.MixedReality.Toolkit.Input.InputSourceType.Controller)
            {
                foreach (var p in source.Pointers)
                {
                    if (p is IMixedRealityNearPointer)
                    {
                        // Ignore near pointers, we only want the rays
                        continue;
                    }

                    if (p.Result != null && p.Result.Details.Object != null)
                    {
                        if(p.Result.Details.Object.name == sm.bingMap.gameObject.name || p.Result.Details.Object.name == sm.stc.walls.gameObject.name)
                            pointers.Add(p);
                    }

                }
            }
        }

        return pointers;
    }









    public override bool CheckChangesRecomputeAndResetFlag()
    {
        throw new System.NotImplementedException();
    }

    public override bool CheckChangesAndResetFlag()
    {
        throw new System.NotImplementedException();
    }

    public override void DisableColliders()
    {
        throw new System.NotImplementedException();
    }

    public override void EnableColliders()
    {
        throw new System.NotImplementedException();
    }

    public override void SetTransparency(float a)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateQueryAfterSTCInteraction()
    {
        throw new System.NotImplementedException();
    }

    public override void RemoveQuery()
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateStatsTooltipPosition()
    {
        throw new System.NotImplementedException();
    }

    public override bool CheckChanges()
    {
        return true;
        //throw new System.NotImplementedException();
    }

    public override Vector3 GetCentralPosition2D()
    {
        if (mapOnFocus && spatialProjection.activeSelf)
            return spatialProjection.transform.position;
        else if (wallOnFocus && temporalProjections.activeSelf)
            return temporalProjections.transform.position;
        else
            return Vector3.zero;
    }

    public override void RefreshColor()
    {
        throw new System.NotImplementedException();
    }

    public override Vector3 GetCentralPosition3D()
    {
        return cube.transform.position;
    }
}