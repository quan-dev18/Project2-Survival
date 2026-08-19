using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TabItem : MonoBehaviour
{
    [Header("Nội dung Panel")]
    public GameObject tabContent; // Panel hiển thị tương ứng (Tab_Shop, Tab_Heroes,...)

    [Header("Trạng thái UI")]
    public GameObject unselectState;
    public GameObject selectedState;

    [Header("Hiệu ứng Scale")]
    public bool animateScale = true;
    public Vector3 selectedScale = new Vector3(1.1f, 1.1f, 1.1f);
    public Vector3 normalScale = Vector3.one;

    public Button Button { get; private set; }

    private void Awake()
    {
        Button = GetComponent<Button>();
    }

    // Gọi khi Tab được chọn
    public void Select()
    {
        if (tabContent != null) tabContent.SetActive(true);
        if (unselectState != null) unselectState.SetActive(false);
        if (selectedState != null) selectedState.SetActive(true);

        if (animateScale) transform.localScale = selectedScale;
    }

    // Gọi khi Tab bị bỏ chọn
    public void Deselect()
    {
        if (tabContent != null) tabContent.SetActive(false);
        if (unselectState != null) unselectState.SetActive(true);
        if (selectedState != null) selectedState.SetActive(false);

        if (animateScale) transform.localScale = normalScale;
    }
}