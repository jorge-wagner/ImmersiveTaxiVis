using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;
using System.Collections.Generic;
using Unity.Services.Analytics.Internal;

/*
 * The implementation of the line drawing feature was inspired by BLANKdev's Pen Tool System, including some heavily-adapted code in this class
 * Source: https://theblankdev.itch.io/linerenderseries, https://www.youtube.com/watch?v=pcLn2ze9JQA
 */

public class QueryCreationCanvas : MonoBehaviour, IMixedRealityPointerHandler {

    //public Action OnPenCanvasLeftClickEvent;
    //public Action OnPenCanvasRightClickEvent;
    public Action OnPenCanvasClickEvent;

    public Vector3 cursorPosition;
    public Vector3 lastClickPosition;
    public bool onFocus = false;

    public int dominantHand = 0;

    /*void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        if (eventData.pointerId == -1) {
            OnPenCanvasLeftClickEvent?.Invoke();
        }
        else if (eventData.pointerId == -2) {
            OnPenCanvasRightClickEvent?.Invoke();
        }
    }*/

    List<IMixedRealityPointer> hoveringPointers;

    void Update()
    {
        hoveringPointers = GetPointersHovering();

        if (hoveringPointers.Count == 1)
        {
            if (hoveringPointers[0].Result.Details.Object.name == this.gameObject.name)
            {
                onFocus = true;
                cursorPosition = hoveringPointers[0].Result.Details.Point;
            }
            else
            {
                onFocus = false;
                cursorPosition = Vector3.zero;
            }
        }
        else if (hoveringPointers.Count == 2)
        {
            if (hoveringPointers[dominantHand].Result.Details.Object.name == this.gameObject.name)
            {
                onFocus = true;
                cursorPosition = hoveringPointers[dominantHand].Result.Details.Point;
            }
            else
            {
                if (hoveringPointers[1 - dominantHand].Result.Details.Object.name == this.gameObject.name)
                {
                    onFocus = true;
                    cursorPosition = hoveringPointers[1 - dominantHand].Result.Details.Point;
                }
                else
                {
                    onFocus = false;
                    cursorPosition = Vector3.zero;
                }
            }
        }
        else 
        {
            onFocus = false;
            cursorPosition = Vector3.zero;
        }
    }

    public virtual void OnPointerDown(MixedRealityPointerEventData eventData)
    {

    }

    public virtual void OnPointerUp(MixedRealityPointerEventData eventData)
    {

    }

    public virtual void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
    }

    public virtual void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        //if()
        //if (eventData.pointerId == -1)
        //{
        //    OnPenCanvasLeftClickEvent?.Invoke();
        //}
        //else if (eventData.pointerId == -2)
        //{
        //    OnPenCanvasRightClickEvent?.Invoke();
        //}

        if(eventData.Pointer.Result.Details.Object.name == this.gameObject.name)
            lastClickPosition = eventData.Pointer.Result.Details.Point;

        OnPenCanvasClickEvent?.Invoke();


        // keeping track of dominant hand

        if (hoveringPointers.Count == 1 && eventData.Pointer.PointerName == hoveringPointers[0].PointerName)
        {
            dominantHand = 0;
        }
        else if (hoveringPointers.Count == 2)
        {
            if (eventData.Pointer.PointerName == hoveringPointers[0].PointerName)
            {
                dominantHand = 0;
            }
            else if(eventData.Pointer.PointerName == hoveringPointers[1].PointerName)
            {
                dominantHand = 1;
            }
        }
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


}
