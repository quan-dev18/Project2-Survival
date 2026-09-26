using DG.Tweening;
using UnityEngine;

public class UpgradePickup : MonoBehaviour
{
    [SerializeField] private float magnetSpeed = 16f;
    [SerializeField] private float backDistance = 1f;
    [SerializeField] private float backDuration = 0.15f;

    private const float RetargetThreshold = 0.1f;
    private const float ArrivalTolerance = 0.5f;

    private Transform target;
    private PlayerStats cachedStats;
    private bool magnetized;
    private bool bouncing;
    private Tween magnetTween;
    private Vector3 aimPoint;

    private void Start()
    {
        ResolveTarget();
    }

    private void ResolveTarget()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                cachedStats = player.GetComponent<PlayerStats>();
            }
        }
        else if (cachedStats == null)
        {
            cachedStats = target.GetComponent<PlayerStats>();
        }
    }

    private void Update()
    {
        if (target == null || cachedStats == null)
        {
            ResolveTarget();
            if (cachedStats == null) return;
        }

        float dist = Vector2.Distance(target.position, transform.position);

        if (!magnetized)
        {
            if (dist <= cachedStats.CollectRange)
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

    private void Chase()
    {
        bouncing = false;
        magnetTween?.Kill();
        aimPoint = target.position;
        float dist = Vector2.Distance(transform.position, aimPoint);
        magnetTween = transform.DOMove(aimPoint, dist / magnetSpeed)
            .SetEase(Ease.OutQuad)
            .OnComplete(Collect);
    }

    private void Collect()
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

        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Tutorial)
            GameManager.Instance.SetState(GameState.LevelUp);
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        magnetTween?.Kill();
    }
}
