using UnityEngine;
using Fusion;
using System;

public enum AttackType { None, Single, Double, Uppercut, Hook }

public class PlayerController : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private NetworkCharacterController _ncc;
    [SerializeField] private Animator _animator;
    [SerializeField] private NetworkMecanimAnimator _networkAnimator;
    [SerializeField] private Transform _hitPoint;

    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("Stats Settings")]
    [SerializeField] private int _maxHp = 100;

    [Header("Attack Damages")]
    [SerializeField] private int _singlePunchDamage = 10;
    [SerializeField] private int _doublePunchDamage = 15;
    [SerializeField] private int _uppercutDamage = 25;
    [SerializeField] private int _hookDamage = 18;

    [Header("Attack Combat Settings")]
    [SerializeField] private float _attackRadius = 1.2f;
    [SerializeField] private float _singleCooldownSeconds = 0.8f;
    [SerializeField] private float _doubleCooldownSeconds = 1.6f;
    [SerializeField] private float _uppercutCooldownSeconds = 1.25f;
    [SerializeField] private float _hookCooldownSeconds = 1.0f;
    [SerializeField] private float _knockbackStrength = 12f;

    [Header("Dodge Settings")]
    [SerializeField] private float _dodgeDurationSeconds = 0.5f;
    [SerializeField] private float _dodgeCooldownSeconds = 1.5f;

    private PlayerBillboardHealthUI _billboardUi;

    private bool _inputSingle;
    private bool _inputDouble;
    private bool _inputUppercut;
    private bool _inputHook;
    private bool _inputDodge;

    [Networked] public int HP { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] public NetworkString<_32> PlayerName { get; set; }

    [Networked] private TickTimer AttackCooldown { get; set; }
    [Networked] private TickTimer DodgeInvincibleTimer { get; set; }
    [Networked] private TickTimer DodgeCooldownTimer { get; set; }

    [Networked] private Vector3 KnockbackVelocity { get; set; }
    [Networked] private AttackType CurrentAttackExecuting { get; set; }

    private void Update()
    {
        if (!HasInputAuthority) return;

        // --- สลับปุ่มตามที่ต้องการ ---
        if (Input.GetMouseButtonDown(0)) _inputSingle = true;
        if (Input.GetMouseButtonDown(1)) _inputDouble = true;

        if (Input.GetKeyDown(KeyCode.Q)) _inputUppercut = true; // Q -> อัปเปอร์คัต
        if (Input.GetKeyDown(KeyCode.E)) _inputHook = true;     // E -> ฮุค

        if (Input.GetKeyDown(KeyCode.LeftShift)) _inputDodge = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (IsDead) return;

        KnockbackVelocity = Vector3.Lerp(KnockbackVelocity, Vector3.zero, Runner.DeltaTime * 10f);

        if (GetInput(out NetworkInputData data))
        {
            bool isDodgingTimer = !DodgeInvincibleTimer.ExpiredOrNotRunning(Runner);
            bool isAttackInCooldown = !AttackCooldown.ExpiredOrNotRunning(Runner);

            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            bool isPlayingActionAnim = stateInfo.IsName("attack") ||
                                       stateInfo.IsName("Attack Double") ||
                                       stateInfo.IsName("Attack Uppercut") ||
                                       stateInfo.IsName("Attack Hook") ||
                                       stateInfo.IsName("Dodge");

            bool isBusyFighting = isDodgingTimer || isAttackInCooldown || isPlayingActionAnim;

            Vector3 moveVector = new Vector3(data.direction.x, 0f, data.direction.y).normalized;
            Vector3 finalVelocity = Vector3.zero;

            if (isBusyFighting)
            {
                finalVelocity = KnockbackVelocity;
                _animator.SetFloat("Speed", 0);
            }
            else
            {
                finalVelocity = (moveVector * _moveSpeed) + KnockbackVelocity;
                _animator.SetFloat("Speed", moveVector.magnitude);

                if (moveVector != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveVector);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * _rotationSpeed);
                }
            }

            _ncc.Move(finalVelocity * Runner.DeltaTime);

            if (HasInputAuthority)
            {
                if (!isBusyFighting)
                {
                    if (_inputDodge) TryDodge();
                    else if (_inputSingle) TryAttack(AttackType.Single);
                    else if (_inputDouble) TryAttack(AttackType.Double);
                    else if (_inputUppercut) TryAttack(AttackType.Uppercut);
                    else if (_inputHook) TryAttack(AttackType.Hook);
                }
                _inputSingle = _inputDouble = _inputUppercut = _inputHook = _inputDodge = false;
            }
        }
    }

    private void TryDodge()
    {
        if (!DodgeCooldownTimer.ExpiredOrNotRunning(Runner)) return;
        DodgeInvincibleTimer = TickTimer.CreateFromSeconds(Runner, _dodgeDurationSeconds);
        DodgeCooldownTimer = TickTimer.CreateFromSeconds(Runner, _dodgeCooldownSeconds);
        AttackCooldown = TickTimer.CreateFromSeconds(Runner, _dodgeDurationSeconds);
        _networkAnimator.SetTrigger("dodge", true);
        if (!HasStateAuthority) RPC_RequestDodge();
    }

    private void TryAttack(AttackType type)
    {
        float cooldown = _singleCooldownSeconds;
        string animatorTrigger = "attack";

        switch (type)
        {
            case AttackType.Double:
                cooldown = _doubleCooldownSeconds;
                animatorTrigger = "attackDouble";
                break;
            case AttackType.Uppercut:
                cooldown = _uppercutCooldownSeconds;
                animatorTrigger = "attackUppercut";
                break;
            case AttackType.Hook:
                cooldown = _hookCooldownSeconds;
                animatorTrigger = "attackHook";
                break;
        }

        AttackCooldown = TickTimer.CreateFromSeconds(Runner, cooldown);
        _networkAnimator.SetTrigger(animatorTrigger, true);

        if (HasStateAuthority)
        {
            CurrentAttackExecuting = type;
            ExecuteAttack();
        }
        else if (HasInputAuthority)
        {
            RPC_RequestAttack(type);
        }
    }

    private void ExecuteAttack()
    {
        if (CurrentAttackExecuting == AttackType.None) return;

        Vector3 attackOrigin = _hitPoint != null ? _hitPoint.position : transform.position + transform.forward;
        Collider[] overlaps = Physics.OverlapSphere(attackOrigin, _attackRadius);

        int damage = _singlePunchDamage;
        switch (CurrentAttackExecuting)
        {
            case AttackType.Double: damage = _doublePunchDamage; break;
            case AttackType.Uppercut: damage = _uppercutDamage; break;
            case AttackType.Hook: damage = _hookDamage; break;
        }

        foreach (Collider overlap in overlaps)
        {
            if (!overlap.CompareTag("hit")) continue;

            Vector3 knockbackDir = (overlap.transform.position - transform.position).normalized;
            knockbackDir.y = 0;
            Vector3 force = knockbackDir * _knockbackStrength;

            PlayerController targetPlayer = overlap.GetComponentInParent<PlayerController>();
            if (targetPlayer != null && targetPlayer != this && !targetPlayer.IsDead)
            {
                targetPlayer.ApplyDamage(damage, force);
                continue;
            }

            DummyController targetDummy = overlap.GetComponentInParent<DummyController>();
            if (targetDummy != null) targetDummy.ApplyDamage(damage, force);
        }

        CurrentAttackExecuting = AttackType.None;
    }

    public void ApplyDamage(int damage, Vector3 knockbackForce)
    {
        if (!HasStateAuthority || IsDead) return;
        if (!DodgeInvincibleTimer.ExpiredOrNotRunning(Runner)) return;

        HP = Mathf.Max(0, HP - Mathf.Abs(damage));
        KnockbackVelocity += knockbackForce;

        if (HP <= 0)
        {
            IsDead = true;
            _animator.SetTrigger("Death");
        }
        else
        {
            _animator.SetTrigger("Hit");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestDodge()
    {
        DodgeInvincibleTimer = TickTimer.CreateFromSeconds(Runner, _dodgeDurationSeconds);
        DodgeCooldownTimer = TickTimer.CreateFromSeconds(Runner, _dodgeCooldownSeconds);
        AttackCooldown = TickTimer.CreateFromSeconds(Runner, _dodgeDurationSeconds);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestAttack(AttackType type)
    {
        CurrentAttackExecuting = type;
        ExecuteAttack();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerName(string rawName)
    {
        string finalName = string.IsNullOrWhiteSpace(rawName) ? "Player" : rawName.Trim();
        if (finalName.Length > 20) finalName = finalName.Substring(0, 20);
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
        if (HasStateAuthority && HP <= 0) { HP = _maxHp; IsDead = false; }
        if (_hitPoint == null) _hitPoint = transform.Find("Hit");
        _billboardUi = GetComponent<PlayerBillboardHealthUI>();
        if (_billboardUi == null) _billboardUi = gameObject.AddComponent<PlayerBillboardHealthUI>();
        _billboardUi.SetupIfNeeded();
        if (HasInputAuthority) RPC_SetPlayerName(LocalPlayerProfile.PlayerName);
    }
}