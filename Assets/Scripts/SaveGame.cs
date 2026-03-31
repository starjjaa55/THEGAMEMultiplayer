using UnityEngine;

public class SaveGame : MonoBehaviour
{
    void Start()
    {
        string saveName = PlayerPrefs.GetString("Save01");
        Debug.Log(saveName);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            PlayerPrefs.SetString("Save01", "Hello Witoon");
        }
    }
}
