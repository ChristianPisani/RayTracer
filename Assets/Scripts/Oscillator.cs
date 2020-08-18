using UnityEngine;

public class Oscillator : MonoBehaviour {
    public float Strength = 1f;
    public float CycleTime = 1f;

    Vector3 originalPos;

    private void Start()
    {
        originalPos = transform.position;
    }

    void Update()
    {
        if (CycleTime == 0) CycleTime = 1;

        var pos = transform.position;

        pos.y = originalPos.y + Mathf.Sin((Time.time * Mathf.PI + pos.x) + Mathf.Cos(Time.time * Mathf.PI + pos.z) / CycleTime) * Strength;

        transform.position = pos;
    }
}
