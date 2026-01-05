using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubtleFloat : MonoBehaviour
{
    [Header("Gentle Motion")]
    public float gentleFloat = 0.15f;    // 아주 미묘하게
    public float slowSpeed = 0.3f;       // 천천히

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // 매우 부드러운 위아래
        float y = Mathf.Sin(Time.time * slowSpeed) * gentleFloat;

        // 거의 감지 안 될 정도의 기울기
        float tilt = Mathf.Sin(Time.time * slowSpeed * 0.8f) * 0.5f;

        transform.position = startPosition + new Vector3(0, y, 0);
        transform.rotation = Quaternion.Euler(tilt * 0.3f, 0, tilt);
    }
}
