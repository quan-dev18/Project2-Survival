using System.Collections;
using UnityEngine;

public static class CameraShake
{
    private static Coroutine activeShake;
    private static float currentIntensity;
    private static float currentDuration;
    private static float elapsed;
    private static CameraController controller;

    public static void Shake(float intensity, float duration)
    {
        if (controller == null)
            controller = Object.FindAnyObjectByType<CameraController>();
        if (controller == null) return;

        currentIntensity += intensity;
        currentDuration = Mathf.Max(currentDuration - elapsed, duration);
        elapsed = 0f;

        if (activeShake == null)
            activeShake = controller.StartCoroutine(ShakeRoutine());
    }

    private static IEnumerator ShakeRoutine()
    {
        while (elapsed < currentDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / currentDuration);
            float x = (Mathf.PerlinNoise(Time.unscaledTime * 25f, 0f) * 2f - 1f) * currentIntensity * t;
            float y = (Mathf.PerlinNoise(0f, Time.unscaledTime * 25f) * 2f - 1f) * currentIntensity * t;
            controller.ShakeOffset = new Vector3(x, y, 0f);
            yield return null;
        }

        controller.ShakeOffset = Vector3.zero;
        currentIntensity = 0f;
        currentDuration = 0f;
        elapsed = 0f;
        activeShake = null;
    }
}
