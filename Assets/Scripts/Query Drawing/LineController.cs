using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * The implementation of the line drawing feature was inspired by BLANKdev's Pen Tool System, including some heavily-adapted code in this class
 * Source: https://theblankdev.itch.io/linerenderseries, https://www.youtube.com/watch?v=pcLn2ze9JQA
 */

public class LineController : MonoBehaviour
{
    private LineRenderer lr;
    private List<DotController> dots;

    private void Awake() {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 0;

        dots = new List<DotController>();
    }

    public DotController GetFirstPoint() {
        return dots[0];
    }

    public void AddDot(DotController dot) {

        KillDotPreviewIfThereIsOne();

        dot.SetLine(this);
        dot.SetIndex(dots.Count);

        lr.positionCount++;
        dots.Add(dot);

        //if (dots.Count >= 2)
        //{
        //for (int i = 0; i < dots.Count; i++)
        //{
        lr.SetPosition(dot.index, this.transform.InverseTransformPoint(dot.transform.position));
        //}
        //}


    }

    private bool lastDotIsAPreview = false; 

    public void PreviewDot(DotController dot)
    {
        AddDot(dot);
        lastDotIsAPreview = true; 
    }

    public void SplitPointsAtIndex(int index, out List<DotController> beforePoints, out List<DotController> afterPoints) {
        List<DotController> before = new List<DotController>();
        List<DotController> after = new List<DotController>();

        int i = 0;
        for (; i < index; i++) {
            before.Add(dots[i]);
        }
        i++;
        for (; i < dots.Count; i++) {
            after.Add(dots[i]);
        }

        beforePoints = before;
        afterPoints = after;

        dots.RemoveAt(index);
    }

    public void SetColor(Color color) {
        lr.startColor = color;
        lr.endColor = color;
    }

    public void ToggleLoop() {
        lr.loop = !lr.loop;
    }

    public void SetToLoop()
    {
        lr.loop = true;
    }

    public void SetNotToLoop()
    {
        lr.loop = false;
    }

    public bool isLooped() {
        return lr.loop;
    }

    public Vector3 GetOrigin()
    {
        if (dots.Count > 0)
            return dots[0].transform.position;
        else
            return Vector3.zero;
    }

    public int Count()
    {
        return dots.Count;
    }

    public void KillDotPreviewIfThereIsOne()
    {
        if (lastDotIsAPreview)
        {
            Destroy(dots[dots.Count - 1].gameObject);
            dots.RemoveAt(dots.Count - 1);
            lr.positionCount--;

            lastDotIsAPreview = false;
        }
    }

    public Vector2[] GetPointsAsVectorsXY()
    {
        Vector2[] array = new Vector2[dots.Count];
        for (int i = 0; i < dots.Count; i++)
        {
            array[i] = new Vector2(dots[i].transform.position.x, dots[i].transform.position.z);
            //position += new Vector3(0, 0, 5);
        }
        return array;
    }

    public Vector3[] GetPointsAsVectorsXYZ()
    {
        Vector3[] array = new Vector3[dots.Count];
        for (int i = 0; i < dots.Count; i++)
        {
            array[i] = dots[i].transform.position;
            //position += new Vector3(0, 0, 5);
        }
        return array;
    }

    public Vector3 GetAveragePosition()
    {
        float accx = 0f, accy = 0f, accz = 0f;
        for (int i = 0; i < dots.Count; i++)
        {
            accx += dots[i].transform.position.x;
            accy += dots[i].transform.position.y;
            accz = dots[i].transform.position.z;
            //position += new Vector3(0, 0, 5);
        }
        return new Vector3(accx / dots.Count, accy / dots.Count, accz / dots.Count);
    }

    public void DonateDots(Transform t)
    {
        foreach(DotController dot in dots)
        {
            dot.transform.parent = t; 
        }
    }

    public List<DotController> GetDots()
    {
        return dots;
    }


    public List<GameObject> GetDotsObjects()
    {
        List<GameObject> lo = new List<GameObject>();
        foreach(DotController dot in dots)
        {
            lo.Add(dot.gameObject);
        }
        return lo;
    }


    public void DestroyDots()
    {
        foreach(DotController dot in dots)
        {
            Destroy(dot.gameObject);
        }
        dots = null;
    }


    //private void LateUpdate() {
    /*if (dots.Count >= 2) {
        for (int i = 0; i < dots.Count; i++) {
            Vector3 position = dots[i].transform.position;
            //position += new Vector3(0, 0, 5);

            lr.SetPosition(i, position);
        }
    }*/
    //}
}
