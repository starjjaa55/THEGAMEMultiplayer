using UnityEngine;
using Fusion;
using System;

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
    [SerializeField] private float _attackCooldownSeconds = 0.5f; // ปรับเวลาให้พอดีกับท่าทาง
    [SerializeField] private float _moveSpeed = 5f;

    private PlayerBillboardHealthUI _billboardUi;

    [Networked] public int HP { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] public NetworkString<_32> PlayerName { get; set; }
    [Networked] private TickTimer AttackCooldown { get; set; }
    [Networked] private NetworkButtons ButtonsPrevious { get; set; }

    public override void FixedUpdateNetwork()
    {
        if (IsDead) return;

        if (GetInput(out NetworkInputData data))
        {
            // 1. เช็คสถานะการโจมตีจาก Cooldown (ถ้ายังไม่ Expired แสดงว่ายังอยู่ในช่วงต่อย)
            bool isAttacking = !AttackCooldown.ExpiredOrNotRunning(Runner);

            Vector3 moveVector = new Vector3(data.direction.x, 0f, data.direction.y).normalized;

            if (isAttacking)
            {
                // --- จังหวะโจมตี: ให้หยุดเดินและหยุดอนิเมชั่นวิ่ง ---
                _ncc.Move(Vector3.zero);
                _animator.SetFloat("Speed", 0);
            }
            else
            {
                // --- จังหวะปกติ: เดินและหมุนตัวตามปกติ ---
                _ncc.Move(_moveSpeed * moveVector * Runner.DeltaTime);
                _animator.SetFloat("Speed", moveVector.magnitude);

                if (moveVector != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveVector);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * 10f);
                }
            }

            // 2. เช็คการกดปุ่มโจมตี
            bool attackPressed = data.buttons.WasPressed(ButtonsPrevious, NetworkInputData.BUTTON_ATTACK);
            if (attackPressed)
            {
                TryAttack();
            }

            ButtonsPrevious = data.buttons;
        }
    }

    private void TryAttack()
    {
        // ถ้าคูลดาวน์ยังไม่หมด ห้ามต่อยซ้ำ
        if (!AttackCooldown.ExpiredOrNotRunning(Runner)) return;

        // เริ่มคูลดาวน์ (ตัวละครจะเริ่มหยุดเดินจากเช็คใน FixedUpdateNetwork)
        AttackCooldown = TickTimer.CreateFromSeconds(Runner, _attackCooldownSeconds);

        // เล่นอนิเมชั่นผ่าน Network
        _networkAnimator.SetTrigger("Attack", true);

        if (HasStateAuthority)
        {
            ExecuteAttack();
        }
        else if (HasInputAuthority)
        {
            RPC_RequestAttack();
        }
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

            target.ApplyDamage(_damagePerHit);
        }
    }

    public void ApplyDamage(int damage)
    {
        if (!HasStateAuthority || IsDead) return;

        HP = Mathf.Max(0, HP - Mathf.Abs(damage));
        if (HP <= 0)
        {
            IsDead = true;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestAttack()
    {
        ExecuteAttack();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerName(string rawName)
    {
        string finalName = string.IsNullOrWhiteSpace(rawName) ? "Player" : rawName.Trim();
        if (finalName.Length > 24) finalName = finalName.Substring(0, 24);
        PlayerName = finalName;
    }

    public override void Render()
    {
        if (_billboardUi == null) return;
        float hpNormalized = _maxHp <= 0 ? 0f : Mathf.Clamp01(HP / (float)_maxHp);
        _billboardUi.SetView(PlayerName.ToString(), hpNormalized);
    }

    public override void Spawned()
    {
        if (HasStateAuthority && HP <= 0)
        {
            HP = _maxHp;
            IsDead = false;
        }

        if (_hitPoint == null)
        {
            _hitPoint = transform.Find("Hit");
        }

        _billboardUi = GetComponent<PlayerBillboardHealthUI>();
        if (_billboardUi == null) _billboardUi = gameObject.AddComponent<PlayerBillboardHealthUI>();
        _billboardUi.SetupIfNeeded();

        if (HasInputAuthority)
        {
            RPC_SetPlayerName(LocalPlayerProfile.PlayerName);
        }
    }
}