using Firebase;
using Firebase.Analytics;
using UnityEngine;

public class FirebaseInit : MonoBehaviour
{
    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available) {
                // 1. Ép Editor bật tính năng thu thập dữ liệu
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                
                // 2. Gửi một sự kiện mở app kèm thông số cụ thể để trang web dễ bắt dữ liệu
                FirebaseAnalytics.LogEvent("test_game_start", "environment", "unity_editor");
                
                Debug.Log("Firebase Analytics đã gọi lệnh gửi event test từ Editor!");
            } else {
                Debug.LogError($"Không thể khởi tạo Firebase: {dependencyStatus}");
            }
        });
    }
}
