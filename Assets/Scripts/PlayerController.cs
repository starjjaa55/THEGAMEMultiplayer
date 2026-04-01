using UnityEngine;
using Fusion;

public class PlayerController : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private NetworkCharacterController _ncc;
    [SerializeField] private Animator _animator;
    [SerializeField] private NetworkMecanimAnimator _networkAnimator;
    [SerializeField] private Transform _hitPoint;

    [Header("Settings")]
    [SerializeField] private int _maxHp = 100;
    [SerializeField] private int _damagePerHit = 10;
    [SerializeField] private float _attackRadius = 1.0f;
    [SerializeField] private float _attackCooldownSeconds = 0.5f;
    [SerializeField] private float _moveSpeed = 5f;

    // --- ส่วนที่เพิ่มสำหรับ Knockback ---
    [SerializeField] private float _knockbackStrength = 10f; // ความแรงในการกระเด็น
    [Networked] private Vector3 _knockbackVelocity { get; set; } // แรงกระเด็นที่สะสมอยู่
    // -------------------------------

    private PlayerBillboardHealthUI _billboardUi;

    [Networked] public int HP { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] public NetworkString<_32> PlayerName { get; set; }
    [Networked] private TickTimer AttackCooldown { get; set; }
    [Networked] private NetworkButtons ButtonsPrevious { get; set; }

    public override void FixedUpdateNetwork()
    {
        if (IsDead) return;

        // --- จัดการแรง Knockback ให้ค่อยๆ ลดลง (Decay) ---
        // Lerp จากแรงปัจจุบันไปสู่ Zero เพื่อให้การกระเด็นดูนุ่มนวล
        _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, Runner.DeltaTime * 10f);

        if (GetInput(out NetworkInputData data))
        {
            bool isAttacking = !AttackCooldown.ExpiredOrNotRunning(Runner);
            Vector3 moveVector = new Vector3(data.direction.x, 0f, data.direction.y).normalized;

            // คำนวณความเร็วสุดท้าย = ความเร็วเดินปกติ + แรงกระเด็น
            Vector3 finalVelocity;

            if (isAttacking)
            {
                // ถ้าต่อยอยู่ เดินไม่ได้ แต่ยังกระเด็นได้ (ถ้าโดนสวน)
                finalVelocity = _knockbackVelocity;
                _animator.SetFloat("Speed", 0);
            }
            else
            {
                // ถ้าปกติ: เดิน + แรงกระเด็น
                finalVelocity = (moveSpeedVector(moveVector)) + _knockbackVelocity;
                _animator.SetFloat("Speed", moveVector.magnitude);

                if (moveVector != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveVector);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * 10f);
                }
            }

            // สั่งเคลื่อนที่ด้วยผลรวมของแรงทั้งหมด
            _ncc.Move(finalVelocity * Runner.DeltaTime);

            bool attackPressed = data.buttons.WasPressed(ButtonsPrevious, NetworkInputData.BUTTON_ATTACK);
            if (attackPressed) TryAttack();

            ButtonsPrevious = data.buttons;
        }
    }

    private Vector3 moveSpeedVector(Vector3 dir) => dir * _moveSpeed;

    private void TryAttack()
    {
        if (!AttackCooldown.ExpiredOrNotRunning(Runner)) return;
        AttackCooldown = TickTimer.CreateFromSeconds(Runner, _attackCooldownSeconds);
        _networkAnimator.SetTrigger("Attack", true);

        if (HasStateAuthority) ExecuteAttack();
        else if (HasInputAuthority) RPC_RequestAttack();
    }

    private void ExecuteAttack()
    {
        Vector3 attackOrigin = _hitPoint != null ? _hitPoint.position : transform.position + transform.forward;
        Collider[] overlaps = Physics.OverlapSphere(attackOrigin, _attackRadius);

        foreach (Collider overlap in overlaps)
        {
            if (!overlap.CompareTag("hit")) continue;

            PlayerController target = overlap.GetComponentInParent<PlayerController>();
            if (target == null || target == this || target.IsDead) continue;

            // --- คำนวณทิศทางกระเด็น ---
            Vector3 knockbackDir = (target.transform.position - transform.position).normalized;
            knockbackDir.y = 0; // ไม่ให้กระเด็นขึ้นฟ้าหรือลงดิน

            // ส่ง Damage พร้อมทิศทางแรงกระเด็น
            target.ApplyDamage(_damagePerHit, knockbackDir * _knockbackStrength);
        }
    }

    // แก้ไขฟังก์ชันรับ Damage ให้รับแรงกระเด็นมาด้วย
    public void ApplyDamage(int damage, Vector3 knockbackForce)
    {
        if (!HasStateAuthority || IsDead) return;

        HP = Mathf.Max(0, HP - Mathf.Abs(damage));

        // เพิ่มแรงกระเด็นเข้าไปในตัวละครที่โดน
        _knockbackVelocity += knockbackForce;

        if (HP <= 0) IsDead = true;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestAttack() => ExecuteAttack();

    // ... ส่วนที่เหลือ (Spawned, Render, RPC_SetPlayerName) เหมือนเดิม ...
}