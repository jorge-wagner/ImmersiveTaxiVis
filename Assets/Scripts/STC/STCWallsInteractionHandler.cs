using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;
using UnityEngine;
using Microsoft.Maps.Unity;
using System;
using System.Globalization;
using static STCManager;


public class STCWallsInteractionHandler : MonoBehaviour, IMixedRealityPointerHandler
{
    public ScenarioManager sm;
    public STCManager stc;

    public bool _isFocused = false;
    bool hasValidPriorPosition = false, hasValidPriorPositionsForTwoPointers = false;
    Vector3 oldCenterBetweenTargets, oldTargetPoint, oldTargetPointLeft, oldTargetPointRight;
    public int holderPointersCount, hoveringPointersCount;

    List<IMixedRealityPointer> hoveringPointers;

    // Start is called before the first frame update
    void Start()
    {
        Time.fixedDeltaTime = 0.1f; 
        //Initialize();
    }

    // Update is called once per frame
    void Update()
    {

        /*
         * Handles refreshing the two red time inspection lines 
         */ 


        hoveringPointers = GetPointersHovering();

        _isFocused = (hoveringPointers.Count > 0);


        if (hoveringPointers.Count <= 1)// && stc.walls.rightInspectionLine.gameObject.activeSelf)
        {
            stc.walls.deactivateInspectionLine(1);
        }
        if (hoveringPointers.Count == 0)// && stc.walls.leftInspectionLine.gameObject.activeSelf)
        {
            stc.walls.deactivateInspectionLine(0);
        }

        if(_isFocused)
        {
            if(hoveringPointers.Count == 1)
            {
                if (hoveringPointers[0].Controller.ControllerHandedness == Microsoft.MixedReality.Toolkit.Utilities.Handedness.Left)
                {
                    stc.walls.RefreshInspectionLine(0, hoveringPointers[0].Result.Details.Point.y);
                    stc.walls.deactivateInspectionLine(1);
                }
                else
                {
                    stc.walls.RefreshInspectionLine(1, hoveringPointers[0].Result.Details.Point.y);
                    stc.walls.deactivateInspectionLine(0);
                }
            }
            if (hoveringPointers.Count == 2)
            {
                if(hoveringPointers[0].Controller.ControllerHandedness == Microsoft.MixedReality.Toolkit.Utilities.Handedness.Left)
                {
                    stc.walls.RefreshInspectionLine(0, hoveringPointers[0].Result.Details.Point.y);
                    stc.walls.RefreshInspectionLine(1, hoveringPointers[1].Result.Details.Point.y);
                }
                else
                {
                    stc.walls.RefreshInspectionLine(0, hoveringPointers[1].Result.Details.Point.y);
                    stc.walls.RefreshInspectionLine(1, hoveringPointers[0].Result.Details.Point.y);
                }
            }
        }
        else
        {
            //if (stc.walls.rightInspectionLine.gameObject.activeSelf)
            //{
                stc.walls.deactivateInspectionLine(1);
            //}
            //if (stc.walls.leftInspectionLine.gameObject.activeSelf)
            //{
                stc.walls.deactivateInspectionLine(0);
            //}
        }


        holderPointersCount = holderPointers.Count; // for debug only
        hoveringPointersCount = hoveringPointers.Count; // for debug only

        if (holderPointersCount > hoveringPointersCount)
        {
            //Debug.Log("Something is wrong, less holders than hoverers");

            foreach (PointerData holderPointer in holderPointers.ToArray())
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
        }
    }

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
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
            //if (focusDetails.Object.GetComponent<MixedRealityMapInteractionHandler>())
            //    focusDetails.Object.GetComponent<MixedRealityMapInteractionHandler>().ForceReleasePointer(eventData.Pointer.PointerId);

            sm.bingMap.GetComponent<MixedRealityMapInteractionHandler>().ForceReleasePointer(eventData.Pointer.PointerId);

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
        if(CoreServices.InputSystem.FocusProvider.GetFocusedObject(eventData.Pointer) != this.gameObject)
        {
            Debug.Log("I'm a WALL and i'm being betrayed.");
        }

     /*   if (!(TryGetPointerDataWithId(eventData.Pointer.PointerId, out _) && CoreServices.InputSystem.FocusProvider.TryGetFocusDetails(eventData.Pointer, out var focusDets) &&
focusDets.Object == gameObject))
        {
            //hasValidPriorPosition = false;
            //hasValidPriorPositionsForTwoPointers = false;
            OnPointerUp(eventData);
            return;
        }*/
        //if (eventData.Pointer.Result.Details.Object.name != this.gameObject.name)

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

    public virtual void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        if (TryGetPointerDataWithId(eventData.Pointer.PointerId, out PointerData pointerDataToRemove))
        {
            holderPointers.Remove(pointerDataToRemove);
        }

        hasValidPriorPosition = false;
        hasValidPriorPositionsForTwoPointers = false;

        //eventData.Use();
    }


    private float lastOneHandManipulationUpdate = 0;

    void HandleOneHandMoveUpdated(MixedRealityPointerEventData eventData)
    {
        /*if (Time.time - lastOneHandManipulationUpdate < 0.05f)
            return;
        else
            lastOneHandManipulationUpdate = Time.time;*/

        Debug.Assert(holderPointers.Count == 1);
        IMixedRealityPointer pointer = holderPointers[0].Pointer;

        if (hasValidPriorPosition) {

            var newTargetPoint = pointer.Result.Details.Point;

            //GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            //sphere.transform.position = newTargetPoint; // pointer.Result.Details.Object.transform.TransformPoint(targetPoint);

            stc.walls.PanTime(newTargetPoint.y - oldTargetPoint.y);

            UpdateScenario(false); //

            stc.walls.Refresh();
        }

        oldTargetPoint = pointer.Result.Details.Point;

        hasValidPriorPosition = true;
    }

    private float lastTwoHandManipulationUpdate = 0;

    void HandleTwoHandManipulationUpdated()
    {
        /*
        if (Time.time - lastTwoHandManipulationUpdate < 0.05f)
            return;
        else
            lastTwoHandManipulationUpdate = Time.time;*/

        Debug.Assert(holderPointers.Count == 2);
        IMixedRealityPointer p1 = holderPointers[0].Pointer;
        IMixedRealityPointer p2 = holderPointers[1].Pointer;
            
        if (hasValidPriorPositionsForTwoPointers)
        {

            var newTargetPointLeft = p1.Result.Details.Point;
            var newTargetPointRight = p2.Result.Details.Point;
            var newCenterBetweenTargets = (newTargetPointLeft + newTargetPointRight) / 2f;

            //GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            //sphere.transform.position = transform.TransformPoint(centerBetweenCurrentTargets);

            stc.walls.PanTime(newCenterBetweenTargets.y - oldCenterBetweenTargets.y);

            float verticalDistanceBetweenHands = Mathf.Abs(newTargetPointLeft.y - newTargetPointRight.y);
            float previousVerticalDistanceBetweenHands = Mathf.Abs(oldTargetPointLeft.y - oldTargetPointRight.y);
            float ratio = (verticalDistanceBetweenHands / previousVerticalDistanceBetweenHands);
            stc.walls.RescaleTime(ratio, newCenterBetweenTargets.y);

            /*if (stc)
            {
                stc.UpdateTrajWidth();
                stc.UpdatePlots();
            }*/
            UpdateScenario(true);

            stc.walls.Refresh();

        }
        hasValidPriorPositionsForTwoPointers = true;

        oldTargetPointLeft = p1.Result.Details.Point;
        oldTargetPointRight = p2.Result.Details.Point;
        oldCenterBetweenTargets = (oldTargetPointLeft + oldTargetPointRight) / 2f;

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


    void UpdateScenario(bool changedScaleAndThereforeShouldUpdateGlyphSize)
    {
        sm.UpdateScenarioComponentsAfterSTCInteractions(changedScaleAndThereforeShouldUpdateGlyphSize);
    }



    /*
     * The following accessory methods for MRTK interaction were adapted from ObjectManipulator.cs
     */

    #region Private methods

    private bool TryGetPointerDataWithId(uint id, out PointerData pointerData)
    {
        int holderPointersCount = holderPointers.Count;
        for (int i = 0; i < holderPointersCount; i++)
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




}
