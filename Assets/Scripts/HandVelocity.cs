using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandVelocity : MonoBehaviour
{
    private Vector3 previousPosition;
    public float currentVelocity;

    void Start()
    {
        previousPosition = transform.position;
    }

    void Update()
    {
        // 현재 속도 계산
        currentVelocity = (transform.position - previousPosition).magnitude / Time.deltaTime;
        previousPosition = transform.position;
    }
}
