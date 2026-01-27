using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buoyancy : MonoBehaviour
{
    public float waterLevel = 0f;          // 물 높이
    public float floatHeight = 2f;         // 떠오르는 높이
    public float bounceDamp = 0.05f;       // 감쇠
    public Vector3 buoyancyCenter;         // 부력 중심

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // 설정
        rb.useGravity = false;
        rb.drag = 1f;
        rb.angularDrag = 3f;
    }

    void FixedUpdate()
    {
        // 부력 중심점
        Vector3 buoyancyPos = transform.position + transform.TransformDirection(buoyancyCenter);

        // 물 밑에 있으면 위로 힘
        if (buoyancyPos.y < waterLevel)
        {
            float displacementAmount = waterLevel - buoyancyPos.y;
            Vector3 buoyancyForce = new Vector3(0, Mathf.Abs(Physics.gravity.y) * displacementAmount * floatHeight, 0);
            rb.AddForceAtPosition(buoyancyForce, buoyancyPos, ForceMode.Force);
        }

        // 약간의 감쇠 (안정화)
        rb.AddForce(-rb.velocity * bounceDamp, ForceMode.VelocityChange);
        rb.AddTorque(-rb.angularVelocity * bounceDamp, ForceMode.VelocityChange);
    }
}
