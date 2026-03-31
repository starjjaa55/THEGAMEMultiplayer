using Fusion;
using Unity.Cinemachine;
using UnityEngine;
public class PlayerCameraSetup : NetworkBehaviour
{
    [Header("Camera Target")]
    [SerializeField] private Transform cameraTarget;
    [Header("Follow Settings")]
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 2f, -5f);
    [SerializeField] private bool useLookAtTarget = true;
    public override void Spawned()
    {
        if (!HasInputAuthority) { return; }
        SetupLocalCamera();
    }
    private void SetupLocalCamera()
    {
        var target = cameraTarget != null ? cameraTarget : transform;
        var cmCamera = FindFirstObjectByType<CinemachineCamera>();
        if (cmCamera == null)
        {
            Debug.LogWarning("PlayerCameraSetup: CinemachineCamera not found in scene.");
            return;
        }
        cmCamera.Follow = target;
        cmCamera.LookAt = useLookAtTarget ? target : null;
        var follow = cmCamera.GetComponent<CinemachineFollow>();
        if (follow == null)
        {
            follow = cmCamera.gameObject.AddComponent<CinemachineFollow>();
        }
        follow.FollowOffset = followOffset;
    }
}