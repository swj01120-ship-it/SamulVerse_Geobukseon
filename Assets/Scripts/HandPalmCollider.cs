using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandPalmCollider : MonoBehaviour
{
    private OVRSkeleton skeleton;
    private GameObject palmColliderObj;
    private SphereCollider palmCollider;
    private Rigidbody palmRb;
    private HandVelocity handVelocity;

    void Start()
    {
        // OVRSkeleton 찾기
        skeleton = GetComponentInChildren<OVRSkeleton>();

        // 손바닥 Collider 생성
        palmColliderObj = new GameObject("RuntimePalmCollider");
        palmCollider = palmColliderObj.AddComponent<SphereCollider>();
        palmCollider.radius = 0.06f;

        palmRb = palmColliderObj.AddComponent<Rigidbody>();
        palmRb.useGravity = false;
        palmRb.isKinematic = true;
        palmRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        handVelocity = palmColliderObj.AddComponent<HandVelocity>();
        palmColliderObj.tag = "Hand";

        Debug.Log("HandPalmCollider 초기화 완료!");
    }

    void Update()
    {
        if (skeleton != null && skeleton.Bones != null && skeleton.Bones.Count > 0)
        {
            // Hand_WristRoot bone 추적 (index 0)
            var wristBone = skeleton.Bones[0];
            if (wristBone.Transform != null)
            {
                palmColliderObj.transform.position = wristBone.Transform.position;
            }
        }
    }
}
