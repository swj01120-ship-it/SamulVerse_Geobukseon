using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetPointVisualizer : MonoBehaviour
{
    void OnDrawGizmos()
    {
        // 빨간 구체로 TargetPoint 표시
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.2f);

        // 십자선
        Gizmos.DrawLine(transform.position + Vector3.left * 0.5f,
                       transform.position + Vector3.right * 0.5f);
        Gizmos.DrawLine(transform.position + Vector3.up * 0.5f,
                       transform.position + Vector3.down * 0.5f);
        Gizmos.DrawLine(transform.position + Vector3.forward * 0.5f,
                       transform.position + Vector3.back * 0.5f);
    }
}
