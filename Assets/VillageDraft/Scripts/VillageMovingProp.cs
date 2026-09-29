using UnityEngine;

/// <summary>Lightweight motion for a decorative sign, windmill, or floating pool prop.</summary>
public sealed class VillageMovingProp : MonoBehaviour
{
    public Vector3 spinDegreesPerSecond = new Vector3(0f, 25f, 0f);
    public Vector3 bobAxis = Vector3.up;
    [Min(0f)] public float bobAmplitude = 0.08f;
    [Min(0f)] public float bobFrequency = 0.7f;

    Vector3 startLocalPosition;
    float elapsed;

    void OnEnable()
    {
        startLocalPosition = transform.localPosition;
        elapsed = 0f;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        transform.Rotate(spinDegreesPerSecond * Time.deltaTime, Space.Self);
        transform.localPosition = startLocalPosition +
            bobAxis.normalized * (Mathf.Sin(elapsed * Mathf.PI * 2f * bobFrequency) * bobAmplitude);
    }
}
