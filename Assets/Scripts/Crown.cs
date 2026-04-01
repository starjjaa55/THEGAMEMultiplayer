    using UnityEngine;

public class Crown : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, 100 * Time.deltaTime, 0);

        float y = Mathf.Sin(Time.time * 2) * 0.05f;
        transform.localPosition = new Vector3(0, 0.3f + y, 0);

    }
}
