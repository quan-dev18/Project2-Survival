using System.Collections;
using UnityEngine;

public class VFXAutoDestroy : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.3f;

    private void Start()
    {
        StartCoroutine(DestroyAfter());
    }

    private IEnumerator DestroyAfter()
    {
        yield return new WaitForSecondsRealtime(lifetime);
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (gameObject.activeInHierarchy)
            Destroy(gameObject);
    }
}
