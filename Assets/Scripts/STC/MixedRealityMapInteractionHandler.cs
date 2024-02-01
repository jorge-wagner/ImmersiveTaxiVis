// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Geospatial;
using Microsoft.Maps.Unity;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Physics;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Analytics.Internal;
using UnityEngine;



/*
 * This script was adapted by Jorge Wagner based on the original MixedRealityMapInteractionHandler
 * script from the Bing Maps SDK repository (https://github.com/microsoft/MapsSDK-Unity/blob/master/SampleProject/Assets/Microsoft.Maps.Unity.Examples/Common/Scripts/MixedRealityMapInteractionHandler.cs)
 * and the ObjectManipulator standard script from MRTK.
 * 
 * The goal was to implement zooming in and out based by holding the map with two pointers
 * and moving them closer or farther apart (as opposed to by double clicking as in the original).
 */




/// <summary>
/// Handles panning and dragging the <see cref="MapRenderer"/> via pointer rays, and zooming in and out of a selected location.
/// </summary>
[RequireComponent(typeof(MapInteractionController))]
//public class MixedRealityMapInteractionHandler : MapInteractionHandler, IMixedRealityPointerHandler, IMixedRealityInputHandler<Vector2>, IMixedRealityFocusHandler
public class MixedRealityMapInteractionHandler : MapInteractionHandler, IMixedRealityPointerHandler
{
    public bool _isFocused = false;
    bool hasValidPriorPosition = false, hasValidPriorPositionsForTwoPointers = false;
    Vector3 oldCenterBetweenTargets, oldTargetPointInLocalSpace, oldTargetPointInLocalSpaceLeft, oldTargetPointInLocalSpaceRight;
    public int holderPointersCount, hoveringPointersCount;

    /*
        public Vector3 cursorPosition;
        //public Vector3 lastClickPosition;
        public bool onFocus = false;
        */
    List<IMixedRealityPointer> hoveringPointers;
    

    private void OnEnable()
    {
        //if (CoreServices.InputSystem != null)
        //{
        //    CoreServices.InputSystem.RegisterHandler<IMixedRealityInputHandler<Vector2>>(this);
        //    CoreServices.InputSystem.RegisterHandler<IMixedRealityPointerHandler>(this);
        //}
    }

    // Start is called before the first frame update
    void Start()
    {
        //Time.fixedDeltaTime = 0.1f;
    }

    private void Update()
    {
        hoveringPointers = GetPointersHovering();

        _isFocused = (hoveringPointers.Count > 0);

        /*hoveringPointers = GetPointersHovering();

        if (hoveringPointers.Count == 1)
        {
            if (hoveringPointers[0].Result.Details.Object.name == this.gameObject.name)
            {
                onFocus = true;
                cursorPosition = hoveringPointers[0].Result.Details.Point;
            }
            else if (hoveringPointers[1].Result.Details.Object.name == this.gameObject.name)
            {
                onFocus = true;
                cursorPosition = hoveringPointers[1].Result.Details.Point;
            }
            else
            {
                onFocus = false;
                cursorPosition = Vector3.zero;
            }
        }
        else if (hoveringPointers.Count == 2)
        {
            onFocus = true;
        }
        else
        {
            onFocus = false;
            cursorPosition = Vector3.zero;
        }
        */

        holderPointersCount = holderPointers.Count; // for debug only
        hoveringPointersCount = hoveringPointers.Count; // for debug only


        if(holderPointersCount > hoveringPointersCount)
        {
            //Debug.Log("Something is wrong, less holders than hoverers");

            foreach(PointerData holderPointer in holderPointers.ToArray())
            {
                bool found = false;
                foreach (IMixedRealityPointer hoveringPointer in hoveringPointers)
                {
                    if (holderPointer.Pointer.PointerId == hoveringPointer.PointerId)
                        found = true;
                }

                if (!found)
                {
                    ForceReleasePointer(holderPointer.Pointer.PointerId);
                }
            }

            //List<PointerData> pointersToKill = holderPointers.Except(hoveringPointers).ToList();
        }

        //pointerDataListCount = holderPointers.Count; // for debug only

        //_isFocused = (holderPointers.Count > 0);
    }

    private void OnDisable()
    {
        //if (CoreServices.InputSystem != null)
        //{
        //    CoreServices.InputSystem.UnregisterHandler<IMixedRealityInputHandler<Vector2>>(this);
        //    CoreServices.InputSystem.UnregisterHandler<IMixedRealityPointerHandler>(this);
        //}
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {

    }
        
    public virtual void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        if (!(TryGetPointerDataWithId(eventData.Pointer.PointerId, out _) && CoreServices.InputSystem.FocusProvider.TryGetFocusDetails(eventData.Pointer, out var focusDets) &&
    focusDets.Object == gameObject))
        {
            //CoreServices.InputSystem.FocusProvider.TryGetFocusDetails(eventData.Pointer, out var focusDetails);
            //if (focusDetails.Object.GetComponent<STCWallsInteractionHandler>())
            //    focusDetails.Object.GetComponent<STCWallsInteractionHandler>().ForceReleasePointer(eventData.Pointer.PointerId);

            sm.stc.walls.GetComponent<STCWallsInteractionHandler>().ForceReleasePointer(eventData.Pointer.PointerId);

            holderPointers.Add(new PointerData(eventData.Pointer, eventData.Pointer.Result.Details.Point));

            eventData.Pointer.IsTargetPositionLockedOnFocusLock = false;
            eventData.Pointer.IsFocusLocked = false;
            
        }

        if (holderPointers.Count > 0)
        {
            // Always mark the pointer data as used to prevent any other behavior to handle pointer events
            // as long as the ObjectManipulator is active.
            // This is due to us reacting to both "Select" and "Grip" events.
            //eventData.Use();
        }
    }

    public virtual void OnPointerDragged(MixedRealityPointerEventData eventData)
    {

        if (CoreServices.InputSystem.FocusProvider.GetFocusedObject(eventData.Pointer) != this.gameObject)
        {
            Debug.Log("I'm a MAP and i'm being betrayed.");
        }
        /*if (!(TryGetPointerDataWithId(eventData.Pointer.PointerId, out var pointerDataToRemove) && CoreServices.InputSystem.FocusProvider.TryGetFocusDetails(eventData.Pointer, out var focusDets) &&
focusDets.Object == gameObject))
        //if (eventData.Pointer.Result.Details.Object.name != this.gameObject.name)
        {
            //if (TryGetPointerDataWithId(eventData.Pointer.PointerId, out PointerData pointerDataToRemove))
            //{
                holderPointers.Remove(pointerDataToRemove);
            //}

            hasValidPriorPosition = false;
            hasValidPriorPositionsForTwoPointers = false;
            pointerDataListCount = holderPointers.Count; // for debug only
            _isFocused = (holderPointers.Count > 0);

            // Also override the FocusDetails so that the pointer ray tracks the target coordinate.
            //CoreServices.InputSystem.FocusProvider.TryGetFocusDetails(eventData.Pointer, out var focusDetails);
            //focusDetails.Object = null;
            //CoreServices.InputSystem.FocusProvider.TryOverrideFocusDetails(eventData.Pointer, focusDetails);

            return;
        }*/

        //if (sm.FreezeSTCInteractions)
        //    return;

        // Call manipulation updated handlers
        if (holderPointers.Count == 1)
        {
            HandleOneHandMoveUpdated(eventData);
        }
        else if (holderPointers.Count > 1)
        {
            HandleTwoHandManipulationUpdated();
        }

        //eventData.Use();
    }

    private Vector3 _smoothedPointInLocalSpace;
    public bool useSmoothing = false;

    void HandleOneHandMoveUpdated(MixedRealityPointerEventData eventData)
    {
        Debug.Assert(holderPointers.Count == 1);
        IMixedRealityPointer pointer = holderPointers[0].Pointer;

        if (hasValidPriorPosition)
        {
            if(useSmoothing)
            {
               /* CoreServices.InputSystem.FocusProvider.TryGetFocusDetails(pointer, out var focusDetails);
                var newTargetPointInLocalSpace = pointer.Result.Details.PointLocalSpace;


                // The current point the ray is targeting has been calculated in OnPointerDragged. Smooth it here.
                float _panSmoothness = 0.5f;
                var panSmoothness = Mathf.Lerp(0.0f, 0.5f, _panSmoothness);
                _smoothedPointInLocalSpace = DynamicExpDecay(_smoothedPointInLocalSpace, newTargetPointInLocalSpace, panSmoothness);

                // Reconstruct ray from pointer position to focus details.
                var rayTargetPoint = MapRenderer.transform.TransformPoint(_smoothedPointInLocalSpace);
                var ray = new Ray(pointer.Position, (rayTargetPoint - pointer.Position).normalized);
                MapInteractionController.PanAndZoom(ray, _targetPointInMercator, _targetAltitudeInMeters, ComputeZoomToApply());

                // Update starting point so that the focus point tracks with this point.
                _targetPointInLocalSpace =
                    MapRenderer.TransformMercatorWithAltitudeToLocalPoint(_targetPointInMercator, _targetAltitudeInMeters);

                // Also override the FocusDetails so that the pointer ray tracks the target coordinate.
                focusDetails.Point = MapRenderer.transform.TransformPoint(_targetPointInLocalSpace);
                focusDetails.PointLocalSpace = _targetPointInLocalSpace;
                CoreServices.InputSystem.FocusProvider.TryOverrideFocusDetails(_pointer, focusDetails);
                */
            }
            else
            {            
                var targetPointInMercator =
                    MapRenderer.TransformLocalPointToMercatorWithAltitude(
                    oldTargetPointInLocalSpace,
                    out var targetAltitudeInMeters,
                    out _);

                var newTargetPointInLocalSpace = pointer.Result.Details.PointLocalSpace;

                //GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                //sphere.transform.position = newTargetPointInLocalSpace; // pointer.Result.Details.Object.transform.TransformPoint(targetPointInLocalSpace);

                var rayTargetPoint = MapRenderer.transform.TransformPoint(newTargetPointInLocalSpace);
                var ray = new Ray(pointer.Position, (rayTargetPoint - pointer.Position).normalized);
                MapInteractionController.PanAndZoom(ray, targetPointInMercator, targetAltitudeInMeters, 0f);
            }

            UpdateScenario(false);
        }

        oldTargetPointInLocalSpace = pointer.Result.Details.PointLocalSpace;

        hasValidPriorPosition = true;


        /*
        CoreServices.InputSystem.FocusProvider.TryGetFocusDetails(pointer, out var focusDetails);

        // Raycast an imaginary plane orignating from the updated _targetPointInLocalSpace.

        var rayPositionInMapLocalSpace = MapRenderer.transform.InverseTransformPoint(pointer.Position);
        var rayDirectionInMapLocalSpace = MapRenderer.transform.InverseTransformDirection(pointer.Rotation * Vector3.forward).normalized;
        var rayInMapLocalSpace = new Ray(rayPositionInMapLocalSpace, rayDirectionInMapLocalSpace.normalized);
        var hitPlaneInMapLocalSpace = new Plane(Vector3.up, targetPointInLocalSpace);
        if (hitPlaneInMapLocalSpace.Raycast(rayInMapLocalSpace, out float enter))
        { 
            targetPointInLocalSpace = focusDetails.PointLocalSpace;           

            var targetPointInMercator =
                  MapRenderer.TransformLocalPointToMercatorWithAltitude(
                      targetPointInLocalSpace,
                      out var targetAltitudeInMeters,
                      out _);

            var newTargetPointInLocalSpace = rayInMapLocalSpace.GetPoint(enter);

            // Reconstruct ray from pointer position to focus details.
            var rayTargetPoint = MapRenderer.transform.TransformPoint(newTargetPointInLocalSpace);
            var ray = new Ray(pointer.Position, (rayTargetPoint - pointer.Position).normalized);
            MapInteractionController.PanAndZoom(ray, targetPointInMercator, targetAltitudeInMeters, 0f);

            // Also override the FocusDetails so that the pointer ray tracks the target coordinate.
            focusDetails.Point = MapRenderer.transform.TransformPoint(newTargetPointInLocalSpace);
            focusDetails.PointLocalSpace = newTargetPointInLocalSpace; 
            CoreServices.InputSystem.FocusProvider.TryOverrideFocusDetails(pointer, focusDetails);

            UpdatePlots(false);
        }*/
    }


    void HandleTwoHandManipulationUpdated()
    { 
        Debug.Assert(holderPointers.Count == 2);
        IMixedRealityPointer p1 = holderPointers[0].Pointer;
        IMixedRealityPointer p2 = holderPointers[1].Pointer;

        if (hasValidPriorPositionsForTwoPointers)
        {
            var targetPointInMercator = MapRenderer.TransformLocalPointToMercatorWithAltitude(
                oldCenterBetweenTargets,
                out var targetAltitudeInMeters,
                out _);

            var newTargetPointInLocalSpaceLeft = p1.Result.Details.PointLocalSpace;
            var newTargetPointInLocalSpaceRight = p2.Result.Details.PointLocalSpace;
            var newCenterBetweenTargets = (newTargetPointInLocalSpaceLeft + newTargetPointInLocalSpaceRight) / 2f;

            //GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            //sphere.transform.position = transform.TransformPoint(centerBetweenCurrentTargets);

            float prevTouchDeltaMag = (new Vector2(oldTargetPointInLocalSpaceLeft.x, oldTargetPointInLocalSpaceLeft.z) - new Vector2(oldTargetPointInLocalSpaceRight.x, oldTargetPointInLocalSpaceRight.z)).magnitude;
            float touchDeltaMag = (new Vector2(newTargetPointInLocalSpaceLeft.x, newTargetPointInLocalSpaceLeft.z) - new Vector2(newTargetPointInLocalSpaceRight.x, newTargetPointInLocalSpaceRight.z)).magnitude;
            //float zoomFactor = 1.0f * (touchDeltaMag - prevTouchDeltaMag);
            var touchPointDeltaToInitialDeltaRatio = touchDeltaMag / prevTouchDeltaMag;
            var _initialMapDimensionInMercator = Mathf.Pow(2, MapRenderer.ZoomLevel - 1);
            var newMapDimensionInMercator = touchPointDeltaToInitialDeltaRatio * _initialMapDimensionInMercator;
            float newZoomLevel = Mathf.Log(newMapDimensionInMercator) / Mathf.Log(2) + 1f;
            float zoomspeed = (newZoomLevel - MapRenderer.ZoomLevel) / Time.deltaTime;

            // Reconstruct ray from pointer position to focus details.
            var rayTargetPoint = MapRenderer.transform.TransformPoint(newCenterBetweenTargets);
            var ray = new Ray((p1.Position + p2.Position) / 2f, (rayTargetPoint - (p1.Position + p2.Position) / 2f).normalized);
            MapInteractionController.PanAndZoom(ray, targetPointInMercator, targetAltitudeInMeters, zoomspeed);

            UpdateScenario(true);

        }

        oldTargetPointInLocalSpaceLeft = p1.Result.Details.PointLocalSpace;
        oldTargetPointInLocalSpaceRight = p2.Result.Details.PointLocalSpace;
        oldCenterBetweenTargets = (oldTargetPointInLocalSpaceLeft + oldTargetPointInLocalSpaceRight) / 2f;

        hasValidPriorPositionsForTwoPointers = true;


        /*
        CoreServices.InputSystem.FocusProvider.TryGetFocusDetails(p1, out var focus1);
        CoreServices.InputSystem.FocusProvider.TryGetFocusDetails(p2, out var focus2);


        var rayPositionInMapLocalSpace1 = MapRenderer.transform.InverseTransformPoint(p1.Position);
        var rayDirectionInMapLocalSpace1 = MapRenderer.transform.InverseTransformDirection(p1.Rotation * Vector3.forward).normalized;
        var rayInMapLocalSpace1 = new Ray(rayPositionInMapLocalSpace1, rayDirectionInMapLocalSpace1.normalized);

        var rayPositionInMapLocalSpace2 = MapRenderer.transform.InverseTransformPoint(p2.Position);
        var rayDirectionInMapLocalSpace2 = MapRenderer.transform.InverseTransformDirection(p2.Rotation * Vector3.forward).normalized;
        var rayInMapLocalSpace2 = new Ray(rayPositionInMapLocalSpace2, rayDirectionInMapLocalSpace2.normalized);


        var hitPlaneInMapLocalSpace = new Plane(Vector3.up, centerBetweenCurrentTargets);
        if (hitPlaneInMapLocalSpace.Raycast(rayInMapLocalSpace1, out float enter1) && hitPlaneInMapLocalSpace.Raycast(rayInMapLocalSpace2, out float enter2))
        {            
            var currentTargetPointInLocalSpace1 = focus1.PointLocalSpace;
            var currentTargetPointInLocalSpace2 = focus2.PointLocalSpace;
            centerBetweenCurrentTargets = (currentTargetPointInLocalSpace1 + currentTargetPointInLocalSpace2) /2f;

            var targetPointInMercator =
                  MapRenderer.TransformLocalPointToMercatorWithAltitude(
                      centerBetweenCurrentTargets,
                      out var targetAltitudeInMeters,
                      out _);

            if(hasValidPriorPosition)
            { 

                var newTargetPointInLocalSpace1 = rayInMapLocalSpace1.GetPoint(enter1);
                var newTargetPointInLocalSpace2 = rayInMapLocalSpace2.GetPoint(enter2);
                var centerBetweenNewTargets = (newTargetPointInLocalSpace1 + newTargetPointInLocalSpace2) / 2f;

                float prevTouchDeltaMag = (new Vector2(currentTargetPointInLocalSpace1.x, currentTargetPointInLocalSpace1.z) - new Vector2(currentTargetPointInLocalSpace2.x, currentTargetPointInLocalSpace2.z)).magnitude;
                float touchDeltaMag = (new Vector2(newTargetPointInLocalSpace1.x, newTargetPointInLocalSpace1.z) - new Vector2(newTargetPointInLocalSpace2.x, newTargetPointInLocalSpace2.z)).magnitude;
                float zoomFactor = 1.0f * (touchDeltaMag - prevTouchDeltaMag);
                var touchPointDeltaToInitialDeltaRatio = touchDeltaMag / prevTouchDeltaMag;
                var _initialMapDimensionInMercator = Mathf.Pow(2, MapRenderer.ZoomLevel - 1);
                var newMapDimensionInMercator = touchPointDeltaToInitialDeltaRatio * _initialMapDimensionInMercator;
                float newZoomLevel = Mathf.Log(newMapDimensionInMercator) / Mathf.Log(2) + 1f;
                float zoomspeed = (newZoomLevel - MapRenderer.ZoomLevel) / Time.deltaTime;

                // Reconstruct ray from pointer position to focus details.
                var rayTargetPoint = MapRenderer.transform.TransformPoint(centerBetweenNewTargets);
                var ray = new Ray((p1.Position + p2.Position)/2f, (rayTargetPoint - (p1.Position + p2.Position) / 2f).normalized);
                MapInteractionController.PanAndZoom(ray, targetPointInMercator, targetAltitudeInMeters, zoomspeed);

                // Also override the FocusDetails so that the pointer ray tracks the target coordinate.
                focus1.Point = MapRenderer.transform.TransformPoint(newTargetPointInLocalSpace1);
                focus1.PointLocalSpace = newTargetPointInLocalSpace1;
                CoreServices.InputSystem.FocusProvider.TryOverrideFocusDetails(p1, focus1);

                focus2.Point = MapRenderer.transform.TransformPoint(newTargetPointInLocalSpace2);
                focus2.PointLocalSpace = newTargetPointInLocalSpace2; 
                CoreServices.InputSystem.FocusProvider.TryOverrideFocusDetails(p2, focus2);

                UpdatePlots(true);

            }

            hasValidPriorPosition = true;

        }*/
    }


    public virtual void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        if (TryGetPointerDataWithId(eventData.Pointer.PointerId, out PointerData pointerDataToRemove))
        {
            holderPointers.Remove(pointerDataToRemove);
        }

        //if(pointerDataList.Count == 0)
        //{
        //   
        //}

        hasValidPriorPosition = false;
        hasValidPriorPositionsForTwoPointers = false;

        //eventData.Use();
    }
    
    /*
    public void OnInputChanged(InputEventData<Vector2> eventData)
    {
       
    }*/

    /*
    public void OnFocusEnter(FocusEventData eventData)
    {
        _isFocused = eventData.NewFocusedObject == gameObject;
    }

    public void OnFocusExit(FocusEventData eventData)
    {
        _isFocused = false;
    }*/




    #region Private methods

    private bool TryGetPointerDataWithId(uint id, out PointerData pointerData)
    {
        int pointerDataListCount = holderPointers.Count;
        for (int i = 0; i < pointerDataListCount; i++)
        {
            PointerData data = holderPointers[i];
            if (data.Pointer.PointerId == id)
            {
                pointerData = data;
                return true;
            }
        }

        pointerData = default(PointerData);
        return false;
    }
    #endregion Private methods

    #region Private Properties

    /// <summary>
    /// Holds the pointer and the initial intersection point of the pointer ray
    /// with the object on pointer down in pointer space
    /// </summary>
    private readonly struct PointerData 
    {
        public PointerData(IMixedRealityPointer pointer, Vector3 worldGrabPoint) : this()
        {
            initialGrabPointInPointer = Quaternion.Inverse(pointer.Rotation) * (worldGrabPoint - pointer.Position);
            Pointer = pointer;
            IsNearPointer = pointer is IMixedRealityNearPointer;
        }

        private readonly Vector3 initialGrabPointInPointer;

        public IMixedRealityPointer Pointer { get; }

        public bool IsNearPointer { get; }

        /// <summary>
        /// Returns the grab point on the manipulated object in world space.
        /// </summary>
        public Vector3 GrabPoint => (Pointer.Rotation * initialGrabPointInPointer) + Pointer.Position;
    }

    private List<PointerData> holderPointers = new List<PointerData>();

    #endregion Private Properties

    // Adapted from https://stackoverflow.com/a/56082649
    List<IMixedRealityPointer> GetAllPointers()
    {
        List<IMixedRealityPointer> pointers = new List<IMixedRealityPointer>();

        foreach (var source in CoreServices.InputSystem.DetectedInputSources)
        {
            // Ignore anything that is not a hand because we want articulated hands
            if (source.SourceType == Microsoft.MixedReality.Toolkit.Input.InputSourceType.Hand)
            {
                foreach (var p in source.Pointers)
                {
                    if (p is IMixedRealityNearPointer)
                    {
                        // Ignore near pointers, we only want the rays
                        continue;
                    }

                    pointers.Add(p);


                    /*if (p.Result != null)
                    {
                        var startPoint = p.Position;
                        var endPoint = p.Result.Details.Point;
                        var hitObject = p.Result.Details.Object;
                        if (hitObject)
                        {
                            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            sphere.transform.localScale = Vector3.one * 0.01f;
                            sphere.transform.position = endPoint;
                        }
                    }*/

                }
            }
        }

        return pointers;
    }


    // Adapted from https://stackoverflow.com/a/56082649
    List<IMixedRealityPointer> GetPointersHovering()
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

                    if (p.Result != null && p.Result.Details.Object != null && p.Result.Details.Object.name == this.gameObject.name)
                    {
                        pointers.Add(p);
                    }

                }
            }
        }

        return pointers;
    }


    /*
     * FOR STC VISUALIZATION
     */

    public ScenarioManager sm;

    void UpdateScenario(bool changedScaleAndThereforeShouldUpdateGlyphSize)
    {
        sm.UpdateScenarioComponentsAfterSTCInteractions(changedScaleAndThereforeShouldUpdateGlyphSize);
    }
    /*
    void UpdatePlots(bool changedScaleAndThereforeShouldUpdateGlyphSize)
    {
        if (sm.flatplot)
            sm.flatplot.UpdatePlots();
        if (sm.trajsprojs)
        {
            if (changedScaleAndThereforeShouldUpdateGlyphSize)
                sm.trajsprojs.UpdateTrajWidth();
            sm.trajsprojs.UpdatePlots();
        }
        if (sm.stc)
        {
            if (changedScaleAndThereforeShouldUpdateGlyphSize)
                sm.stc.UpdateTrajWidth();
            sm.stc.UpdatePlots();
        }
    }*/


    private static Vector3 DynamicExpDecay(Vector3 from, Vector3 to, float halfLife)
    {
        return Vector3.Lerp(from, to, DynamicExpCoefficient(halfLife, Vector3.Distance(to, from)));
    }

    private static float DynamicExpCoefficient(float halfLife, float delta)
    {
        if (halfLife == 0)
        {
            return 1;
        }

        return 1.0f - Mathf.Pow(0.5f, delta / halfLife);
    }


    public void ForceReleasePointer(uint pointer)
    {
        if (TryGetPointerDataWithId(pointer, out PointerData pointerDataToRemove))
        {
            holderPointers.Remove(pointerDataToRemove);
            hasValidPriorPosition = false;
            hasValidPriorPositionsForTwoPointers = false;
        }
    }

}