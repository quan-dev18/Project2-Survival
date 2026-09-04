using System.Collections.Generic;
using UnityEngine;

public class TabManager : MonoBehaviour
{
    [Header("Danh sách các TabItem")]
    public List<TabItem> tabList = new List<TabItem>();
    public int defaultTabIndex = 0;

    private int currentTabIndex = -1;

    private void Start()
    {
        for (int i = 0; i < tabList.Count; i++)
        {
            int index = i;
            if (tabList[i] != null && tabList[i].Button != null)
            {
                tabList[i].Button.onClick.AddListener(() => SelectTab(index));
            }
        }

        SelectTab(defaultTabIndex, false);
    }

    public void SelectTab(int indexToOpen, bool animate = true)
    {
        if (indexToOpen == currentTabIndex) return;

        int direction = (currentTabIndex < 0 || indexToOpen > currentTabIndex) ? 1 : -1;
        currentTabIndex = indexToOpen;

        for (int i = 0; i < tabList.Count; i++)
        {
            if (tabList[i] == null) continue;

            if (i == indexToOpen)
            {
                tabList[i].Select(direction, animate);
            }
            else
            {
                tabList[i].Deselect(animate);
            }
        }
    }
}