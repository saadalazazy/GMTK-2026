using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TwoPointsLine : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    private LineRenderer line;

    // Start is called before the first frame update
    public void Start()
    {
        line = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    public void Update()
    {
        if (pointA != null && pointB != null && line != null)
        {
            line.positionCount = 2;
            line.SetPosition(0, pointA.position);
            line.SetPosition(1, pointB.position);
        }
    }
}