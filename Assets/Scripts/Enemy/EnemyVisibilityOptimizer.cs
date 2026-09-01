using UnityEngine;

public class EnemyVisibilityOptimizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private Animator animator;
    [SerializeField] private MonoBehaviour shadowComponent;

    [Header("Settings")]
    [SerializeField] private float offScreenSpeedMultiplier = 3f;
    [SerializeField] private float checkInterval = 0.3f;
    [SerializeField] private float viewportMargin = 0.1f;

    private Camera mainCam;
    private float checkTimer;
    private bool isOnScreen = true;

    private void Awake()
    {
        if (enemyController == null)
            enemyController = GetComponent<EnemyController>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        mainCam = Camera.main;
    }

    private void OnEnable()
    {
        isOnScreen = true;
        checkTimer = 0f;
        SetOnScreenState(true);
    }

    private void Update()
    {
        checkTimer -= Time.deltaTime;
        if (checkTimer > 0f) return;
        checkTimer = checkInterval;

        if (mainCam == null) return;

        Vector3 viewportPos = mainCam.WorldToViewportPoint(transform.position);
        bool visible = viewportPos.x > -viewportMargin && viewportPos.x < 1f + viewportMargin
                    && viewportPos.y > -viewportMargin && viewportPos.y < 1f + viewportMargin;

        if (visible == isOnScreen) return;
        isOnScreen = visible;

        SetOnScreenState(visible);
    }

    private void SetOnScreenState(bool onScreen)
    {
        if (enemyController != null)
            enemyController.speedMultiplier = onScreen ? 1f : offScreenSpeedMultiplier;

        if (animator != null)
            animator.enabled = onScreen;

        if (shadowComponent != null)
            shadowComponent.enabled = onScreen;
    }
}
