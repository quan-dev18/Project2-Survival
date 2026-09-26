using UnityEngine;

[CreateAssetMenu(fileName = "PreMapData", menuName = "Selection/PreMap Data")]
public class PreMapSO : ScriptableObject
{
    public GameObject mapPreviewPrefab; // Kéo thẳng Prefab MapPreview vào đây (๑•̀ㅂ•́)و✧
    public string sceneToLoad;

    [Tooltip("StageSO tương ứng của map này (chứa StageID + MaxProgress) để hiển thị kỷ lục tiến trình.")]
    public StageSO stageData;
}
