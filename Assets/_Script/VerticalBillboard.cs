using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerticalBillboard : MonoBehaviour
{
    public Transform target;

    private void Start()
    {
        target = Camera.main.transform;
    }
    void Update()
    {
        if (target != null)
        {
            transform.LookAt(target, Vector3.up);
        }
    }
}