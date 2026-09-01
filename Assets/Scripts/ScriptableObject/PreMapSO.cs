using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PreMapData", menuName = "Selection/PreMap Data")]
public class PreMapSO : ScriptableObject
{
    public GameObject mapPreviewPrefab; // Kéo thẳng Prefab MapPreview vào đây (๑•̀ㅂ•́)و✧
    public string sceneToLoad;
}
