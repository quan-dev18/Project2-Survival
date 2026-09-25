using UnityEngine;

public class ThunderCloudController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform vfxTarget; // blue square "Thunder" point or separate target
    [SerializeField] private GameObject cloudVFX;
    [SerializeField] private GameObject lightningVFX;

    [Header("TC_1 base (independent)")]
    [SerializeField] private float strikeRange = 4f;
    [SerializeField] private float strikeDamage = 10f;
    [SerializeField] private float strikeInterval = 5f;
    [SerializeField] private float fieldRadius = 2f;
    [SerializeField] private float fieldDps = 6f;
    [Header("TC_2B burn")]
    [SerializeField] private float burnDps = 5f;
    [SerializeField] private float burnDuration = 3f;

    [Header("Sound")]
    [Tooltip("Tiếng sét khi trúng (Lighting.wav).")]
    [SerializeField] private AudioClip strikeSFX;
    [Tooltip("Độ lớn tiếng sét (0-1).")]
    [Range(0f, 1f)]
    [SerializeField] private float strikeVolume = 1f;
    [Tooltip("Pitch tối thiểu — ngẫu nhiên hóa để tiếng sét đỡ máy móc.")]
    [SerializeField] private float minStrikePitch = 0.9f;
    [Tooltip("Pitch tối đa.")]
    [SerializeField] private float maxStrikePitch = 1.1f;

    private float strikeTimer;
    private float fieldTimer;
    private bool tc1Enabled;
    private bool tc2AEnabled; // faster
    private bool tc2BEnabled; // burn 5/s for 3s
    private bool tc3Enabled; // field
    private static System.Collections.Generic.Dictionary<EnemyHealth, float> thunderBurns = new System.Collections.Generic.Dictionary<EnemyHealth, float>();

    [Header("Debug")]
    [SerializeField] private bool startEnabledForTest = false;

    private void Awake()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (lightningVFX != null) lightningVFX.SetActive(false);
        if (cloudVFX != null) cloudVFX.SetActive(false);
        if (startEnabledForTest) EnableTC1();
    }

    private void Update()
    {
        if (player != null)
            transform.position = player.position + Vector3.up * 3f;

        if (!tc1Enabled) return;

        float interval = tc2AEnabled ? 2.5f : strikeInterval;
        strikeTimer += Time.deltaTime;
        if (strikeTimer >= interval)
        {
            strikeTimer = 0f;
            Strike();
        }

        if (tc3Enabled)
        {
            fieldTimer += Time.deltaTime;
            if (fieldTimer >= 1f / fieldDps)
            {
                fieldTimer = 0f;
                FieldTick();
            }
        }
    }

    private void Strike()
    {
        if (player == null) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.position, strikeRange);
        Transform nearest = null;
        float best = float.MaxValue;
        foreach (var h in hits)
        {
            EnemyHealth eh;
            if (!h.TryGetComponent(out eh))
            {
                eh = h.GetComponentInChildren<EnemyHealth>();
                if (eh == null) eh = h.GetComponentInParent<EnemyHealth>();
            }
            if (eh == null) continue;
            float d = Vector2.Distance(player.position, h.transform.position);
            if (d < best) { best = d; nearest = h.transform; }
        }
        if (nearest == null) return;

        PlayStrikeSFX();

        if (vfxTarget != null) vfxTarget.position = nearest.position;
        if (lightningVFX != null)
        {
            // Anchor bolt at cloud (Thunder) and point to target so it visually hits
            Vector2 from = transform.position;
            Vector2 to = nearest.position;
            Vector2 dir = (to - from).normalized;
            lightningVFX.transform.position = from;
            // BUMBAMB mesh tip points down in local space, so up must be opposite
            lightningVFX.transform.up = -dir;
            lightningVFX.transform.localScale = Vector3.one;
            lightningVFX.SetActive(true);
            foreach (var ps in lightningVFX.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                // Make sure it fires downwards along local Y toward target
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                ps.Clear();
                ps.Play(true);
            }
            CancelInvoke(nameof(HideLightning));
            Invoke(nameof(HideLightning), 0.8f);
        }

        // TC_1 now AoE natively: hit nearest + nearby
        Collider2D[] aoe = Physics2D.OverlapCircleAll(nearest.position, 1.5f);
        foreach (var h in aoe)
        {
            EnemyHealth eh;
            if (!h.TryGetComponent(out eh))
            {
                eh = h.GetComponentInChildren<EnemyHealth>();
                if (eh == null) eh = h.GetComponentInParent<EnemyHealth>();
            }
            if (eh == null) continue;
            eh.TakeDamage(strikeDamage);
            if (tc2BEnabled) ApplyThunderBurn(eh);
        }
    }

    private void FieldTick()
    {
        if (player == null) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.position, fieldRadius);
        foreach (var h in hits)
        {
            EnemyHealth eh;
            if (!h.TryGetComponent(out eh))
            {
                eh = h.GetComponentInChildren<EnemyHealth>();
                if (eh == null) eh = h.GetComponentInParent<EnemyHealth>();
            }
            if (eh != null) eh.TakeDamage(1f);
        }
    }

    private void HideLightning() { if (lightningVFX != null) lightningVFX.SetActive(false); }

    /// <summary>
    /// Phát tiếng sét 2D (game top-down nên không cần không gian 3D).
    /// </summary>
    private void PlayStrikeSFX()
    {
        AudioManager manager = AudioManager.Instance;
        if (manager == null || strikeSFX == null) return;

        manager.PlaySFX(strikeSFX, strikeVolume, 1f, StrikePitchRange());
    }

    /// <summary>
    /// Mức ngẫu nhiên hóa pitch (±) dựa trên minStrikePitch ~ maxStrikePitch.
    /// </summary>
    private float StrikePitchRange()
    {
        float center = (minStrikePitch + maxStrikePitch) * 0.5f;
        float half = Mathf.Max(0f, (maxStrikePitch - minStrikePitch) * 0.5f);
        return Mathf.Max(0f, half / Mathf.Max(0.01f, center));
    }

    private void ApplyThunderBurn(EnemyHealth eh)
    {
        float end = Time.time + burnDuration;
        if (thunderBurns.ContainsKey(eh)) { thunderBurns[eh] = end; eh.ShowBurnVFX(burnDuration); return; }
        thunderBurns[eh] = end;
        eh.ShowBurnVFX(burnDuration);
        eh.StartCoroutine(ThunderBurnRoutine(eh, end));
    }
    private System.Collections.IEnumerator ThunderBurnRoutine(EnemyHealth eh, float endTime)
    {
        float tick = 0f;
        while (eh != null && eh.CurrentHealth > 0f && Time.time < endTime)
        {
            // refresh check
            if (thunderBurns.TryGetValue(eh, out float newEnd) && newEnd > endTime) endTime = newEnd;
            tick += Time.deltaTime;
            if (tick >= 1f / burnDps)
            {
                tick = 0f;
                if (eh != null && eh.CurrentHealth > 0f) eh.TakeDamage(1f);
            }
            yield return null;
        }
        thunderBurns.Remove(eh);
    }

    public void EnableTC1()
    {
        tc1Enabled = true;
        if (cloudVFX != null)
        {
            cloudVFX.SetActive(true);
            foreach (var ps in cloudVFX.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Clear();
                ps.Play(true);
            }
        }
    }
    public void EnableTC2A() => tc2AEnabled = true;
    public void EnableTC2B() => tc2BEnabled = true;
    public void EnableTC3() => tc3Enabled = true;
}
