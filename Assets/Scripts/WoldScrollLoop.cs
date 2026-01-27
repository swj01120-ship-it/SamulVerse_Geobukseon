using UnityEngine;

public class WorldScrollLoop : MonoBehaviour
{
    [Header("Direction (local)")]
    public Vector3 moveDir = new Vector3(0, 0, -1);

    [Header("Speed (m/s)")]
    public float baseSpeed = 0.25f;
    public float speedMul = 1f; // ComboSystem에서 넣어줄 값

    [Header("Loop")]
    public float loopDistance = 30f; // 이 거리만큼 이동하면 원위치 보정

    private Vector3 _startLocalPos;
    private float _accum;

    void Awake()
    {
        _startLocalPos = transform.localPosition;
    }

    void Update()
    {
        float v = baseSpeed * speedMul;
        float d = v * Time.deltaTime;

        transform.localPosition += moveDir.normalized * d;
        _accum += d;

        if (_accum >= loopDistance)
        {
            // 뒤로 이동한 만큼 앞쪽으로 당겨서 루프
            transform.localPosition = _startLocalPos;
            _accum = 0f;
        }
    }
}
