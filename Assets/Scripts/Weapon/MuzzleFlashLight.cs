using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MuzzleFlashLight : MonoBehaviour
{
    [SerializeField] private Light2D muzzleLight;
    [SerializeField] private float maxIntensity = 1.5f;
    [SerializeField] private float fadeTime = 0.12f;
    [SerializeField] private float shadowIntensity = 0.5f;

    private void Awake()
    {
        if (muzzleLight == null)
            muzzleLight = GetComponent<Light2D>();

        if (muzzleLight == null)
            return;

        muzzleLight.intensity = 0f;

        if (muzzleLight.blendStyleIndex != 1)
            muzzleLight.blendStyleIndex = 1;

        muzzleLight.shadowsEnabled = true;
        muzzleLight.shadowIntensity = shadowIntensity;
    }

    private void Update()
    {
        if (muzzleLight == null || muzzleLight.intensity <= 0f)
            return;

        muzzleLight.intensity = Mathf.MoveTowards(
            muzzleLight.intensity, 0f, Time.deltaTime * maxIntensity / fadeTime);
    }

    public void Flash()
    {
        if (muzzleLight != null)
            muzzleLight.intensity = maxIntensity;
    }
}
