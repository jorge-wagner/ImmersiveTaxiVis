using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;

/*
 * The implementation of the line drawing feature was inspired by BLANKdev's Pen Tool System, including some heavily-adapted code in this class
 * Source: https://theblankdev.itch.io/linerenderseries, https://www.youtube.com/watch?v=pcLn2ze9JQA
 */

public class DotController : MonoBehaviour //, IDragHandler, IPointerClickHandler, IBeginDragHandler {
{ 
    public LineController line;
    public int index;

    /*
    public Action<DotController> OnDragEvent;
    public void OnDrag(PointerEventData eventData) {
        OnDragEvent?.Invoke(this);
    }

    public Action<DotController> OnRightClickEvent;
    public Action<LineController> OnLeftClickEvent;
    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.pointerId == -2) {
            //Right Click

            OnRightClickEvent?.Invoke(this);
        }
        else if (eventData.pointerId == -1) {
            //Left Click

            OnLeftClickEvent?.Invoke(line);
        }
    }*/

    public void SetLine(LineController line) {
        this.line = line;
    }

    public void SetIndex(int index) {
        this.index = index;
    }

    /*
    public void OnBeginDrag(PointerEventData eventData) {
        if (eventData.pointerId == -1) {
            //Left Drag

            OnLeftClickEvent?.Invoke(line);
        }
    }*/
}
