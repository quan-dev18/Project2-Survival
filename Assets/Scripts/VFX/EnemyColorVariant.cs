using UnityEngine;

public class EnemyColorVariant : MonoBehaviour
{
    [SerializeField] private Color[] colors = { Color.white };

    private SpriteRenderer[] renderers;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        mpb = new MaterialPropertyBlock();
    }

    public void ApplyRandomColor()
    {
        if (colors == null || colors.Length == 0) return;
        Color randomColor = colors[Random.Range(0, colors.Length)];

        foreach (var renderer in renderers)
        {
            renderer.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", randomColor);
            renderer.SetPropertyBlock(mpb);
        }
    }
}
