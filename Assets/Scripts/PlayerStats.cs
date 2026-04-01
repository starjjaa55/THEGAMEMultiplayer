using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public int killCount = 0;
    public GameObject crown; // ¡ß°ÿÆ (≈“°„ Ë„π Inspector)

    public void AddKill()
    {
        killCount++;
        GameManager.Instance.UpdateLeader();
    }

    public void SetCrown(bool active)
    {
        if (crown != null)
            crown.SetActive(active);
    }
}