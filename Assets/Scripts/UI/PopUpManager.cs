using UnityEngine;

public enum PopupType
{
    Damage,
    PlayerDamage,
    Heal,
    Crit
}

public class PopUpManager : MonoBehaviour
{
    public static PopUpManager Instance { get; private set; }

    [SerializeField] private GameObject popupPrefab;

    [Header("Styles")]
    [SerializeField] private PopupStyle damageStyle = new PopupStyle
    {
        color = Color.white,
        fontSize = 2f,
        floatDistance = 1.5f,
        lifetime = 0.8f,
        critFontSizeMultiplier = 1.6f
    };
    [SerializeField] private PopupStyle playerDamageStyle = new PopupStyle
    {
        color = new Color(1f, 0.3f, 0.3f),
        fontSize = 2f,
        floatDistance = 1.5f,
        lifetime = 0.8f,
        critFontSizeMultiplier = 1.6f
    };
    [SerializeField] private PopupStyle healStyle = new PopupStyle
    {
        color = new Color(0.3f, 1f, 0.3f),
        fontSize = 2f,
        floatDistance = 1.5f,
        lifetime = 0.8f,
        critFontSizeMultiplier = 1.6f,
        healPrefix = "+"
    };
    [SerializeField] private PopupStyle critStyle = new PopupStyle
    {
        color = new Color(1f, 0.85f, 0.2f),
        fontSize = 2.5f,
        floatDistance = 2f,
        lifetime = 1f,
        critFontSizeMultiplier = 1.6f,
        critSuffix = "!"
    };

    [SerializeField] private float positionJitter = 0.3f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Show(Vector3 position, float amount, PopupType type = PopupType.Damage)
    {
        if (popupPrefab == null || ObjectPooling.Instance == null) return;

        Vector2 jitter = Random.insideUnitCircle * positionJitter;
        Vector3 pos = position + (Vector3)jitter;

        GameObject go = ObjectPooling.Instance.Spawn(popupPrefab, pos, Quaternion.identity);
        if (go == null || !go.TryGetComponent(out DamagePopup popup)) return;

        PopupStyle style = GetStyle(type);
        popup.Show(amount, style, type == PopupType.Crit, type == PopupType.Heal);
    }

    private PopupStyle GetStyle(PopupType type)
    {
        switch (type)
        {
            case PopupType.PlayerDamage: return playerDamageStyle;
            case PopupType.Heal: return healStyle;
            case PopupType.Crit: return critStyle;
            default: return damageStyle;
        }
    }
}

[System.Serializable]
public struct PopupStyle
{
    public Color color;
    public float fontSize;
    public float floatDistance;
    public float lifetime;
    public float critFontSizeMultiplier;
    public string healPrefix;
    public string critSuffix;
}