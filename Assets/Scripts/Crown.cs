using UnityEngine;

public class Crown : MonoBehaviour
{
    private Vector3 startPos;

    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatHeight = 0.05f;
    [SerializeField] private Renderer crownRenderer;
    [SerializeField] private float glowSpeed = 2f;
    [SerializeField] private float glowIntensity = 2f;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        // ลอยขึ้นลง
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        transform.localPosition = new Vector3(
            startPos.x,
            startPos.y + yOffset,
            startPos.z
        );
        // แสงกระพริบ
        float emission = Mathf.Abs(Mathf.Sin(Time.time * glowSpeed)) * glowIntensity;
        crownRenderer.material.SetColor("_EmissionColor", Color.yellow * emission);
    }
}
