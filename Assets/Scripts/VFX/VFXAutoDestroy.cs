using System.Collections;
using UnityEngine;

public class VFXAutoDestroy : MonoBehaviour, IPoolSpawnable
{
    [SerializeField] private float lifetime = 0.3f;
    private Coroutine destroyRoutine;

    public void OnSpawned()
    {
        if (destroyRoutine != null)
            StopCoroutine(destroyRoutine);
        destroyRoutine = StartCoroutine(DestroyAfter());
    }

    private IEnumerator DestroyAfter()
    {
        yield return new WaitForSecondsRealtime(lifetime);
        destroyRoutine = null;
        ObjectPooling.Instance.Despawn(gameObject);
    }
}
