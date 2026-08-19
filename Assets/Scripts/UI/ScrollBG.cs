using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollBG : MonoBehaviour
{
    [SerializeField] private RawImage rawImage;
    [SerializeField] private float _x, _y;
    // Update is called once per frame
    void Update()
    {
        if (rawImage != null)
        {
            rawImage.uvRect = new Rect(rawImage.uvRect.position + new Vector2(_x, _y) * Time.deltaTime, rawImage.uvRect.size);
        }
    }
}
