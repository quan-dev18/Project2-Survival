using System.Collections.Generic;
using UnityEngine;

public class TabManager : MonoBehaviour
{
    [Header("Danh sách các TabItem")]
    public List<TabItem> tabList = new List<TabItem>();
    public int defaultTabIndex = 0;

    private void Start()
    {
        // Đăng ký sự kiện Click cho từng TabItem
        for (int i = 0; i < tabList.Count; i++)
        {
            int index = i;
            if (tabList[i] != null && tabList[i].Button != null)
            {
                tabList[i].Button.onClick.AddListener(() => SelectTab(index));
            }
        }

        // Mở Tab mặc định
        SelectTab(defaultTabIndex);
    }

    public void SelectTab(int indexToOpen)
    {
        for (int i = 0; i < tabList.Count; i++)
        {
            if (tabList[i] == null) continue;

            if (i == indexToOpen)
            {
                tabList[i].Select();   // Kick hoạt UI / Animation được chọn
            }
            else
            {
                tabList[i].Deselect(); // Trở về trạng thái bình thường
            }
        }
    }
}