using DG.Tweening;
using UnityEngine;

public class XPGem : MonoBehaviour, IPoolSpawnable
{
    [SerializeField] private float xpAmount = 1f;
    [SerializeField] private float magnetSpeed = 16f;
    [SerializeField] private float backDistance = 1f;
    [SerializeField] private float backDuration = 0.15f;
    private const float RetargetThreshold = 0.1f;
    protected const float ArrivalTolerance = 0.5f;

    protected Transform target;
    private bool magnetized;
    private bool bouncing;
    private Tween magnetTween;
    private Vector3 aimPoint;

    public void OnSpawned()
    {
        magnetized = false;
        bouncing = false;
        magnetTween?.Kill();
        ResolveTarget();
    }

    public void SetAmount(float amount) => xpAmount = amount;

    private void ResolveTarget()
    {
        if (target == null && PlayerXP.Instance != null)
            target = PlayerXP.Instance.transform;
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            target = player != null ? player.transform : null;
        }
        if (target == null)
            Debug.LogWarning($"XPGem: no player found at {transform.position}, orb cannot magnetize. Tag the player 'Player' or add PlayerXP to it.");
    }

    private void Update()
    {
        if (target == null)
        {
            ResolveTarget();
            return;
        }

        PlayerStats stats = target.GetComponent<PlayerStats>();
        if (stats == null) return;

        float dist = Vector2.Distance(target.position, transform.position);

        if (!magnetized)
        {
            if (dist <= stats.CollectRange)
            {
                magnetized = true;
                BackBounce();
            }
            return;
        }

        if (bouncing) return;

        if (magnetTween == null || !magnetTween.IsActive()
            || Vector2.Distance(aimPoint, target.position) > RetargetThreshold)
        {
            Chase();
        }
    }

    private void BackBounce()
    {
        bouncing = true;
        magnetTween?.Kill();
        Vector3 away = (transform.position - target.position).normalized;
        Vector3 backPos = transform.position + away * backDistance;
        magnetTween = transform.DOMove(backPos, backDuration)
            .SetEase(Ease.OutSine)
            .OnComplete(Chase);
    }

    protected virtual void Chase()
    {
        bouncing = false;
        magnetTween?.Kill();
        aimPoint = target.position;
        float dist = Vector2.Distance(transform.position, aimPoint);
        magnetTween = transform.DOMove(aimPoint, dist / magnetSpeed)
            .SetEase(Ease.OutQuad)
            .OnComplete(Collect);
    }

    protected virtual void Collect()
    {
        if (target == null)
        {
            ResolveTarget();
            return;
        }

        if (Vector2.Distance(target.position, transform.position) > ArrivalTolerance)
        {
            Chase();
            return;
        }

        if (target.TryGetComponent(out PlayerXP playerXP))
            playerXP.AddExperience(playerXP.PickupValue(xpAmount));
        if (ObjectPooling.Instance != null)
            ObjectPooling.Instance.Despawn(gameObject);
    }

    private void OnDisable()
    {
        magnetTween?.Kill();
    }
}