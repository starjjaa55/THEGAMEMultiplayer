using UnityEngine;
using Fusion;

public class DummyController : NetworkBehaviour
{
    [SerializeField] private NetworkCharacterController _ncc;
    [Networked] private Vector3 _knockbackVelocity { get; set; }

    public override void FixedUpdateNetwork()
    {
        // ลดแรงกระแทกสะสมลงเรื่อยๆ (Decay)
        _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, Runner.DeltaTime * 10f);

        // ถ้ายังมีแรงเหลือ ให้ขยับดัมมี่ถอยหลัง
        if (_knockbackVelocity.magnitude > 0.01f)
        {
            _ncc.Move(_knockbackVelocity * Runner.DeltaTime);
        }
    }

    // ฟังก์ชันรับ Damage และแรงกระเด็น (ชื่อและ Parameter ต้องตรงกับที่ Player เรียก)
    public void ApplyDamage(int damage, Vector3 knockbackForce)
    {
        if (!HasStateAuthority) return;

        // รับแรงกระเด็นเข้ามา
        _knockbackVelocity += knockbackForce;

        Debug.Log($"Dummy hit! Damage: {damage}, KB: {knockbackForce.magnitude}");
    }
}