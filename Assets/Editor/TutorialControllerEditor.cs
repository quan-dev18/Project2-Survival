#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

[CustomEditor(typeof(TutorialController))]
public class TutorialControllerEditor : Editor
{
    private static readonly string[] StepTitles = new string[]
    {
        "Bước 1: Cách di chuyển (Joystick / Phím)",
        "Bước 2: Tự động ngắm & bắn quái vật",
        "Bước 3: Thu thập Exp & Lên cấp nâng kỹ năng",
        "Bước 4: Phá hòm nhặt đồ (Props & Vàng)",
        "Bước 5: Sẵn sàng sinh tồn qua các đợt sóng quái"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(6);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft
        };

        EditorGUILayout.LabelField("HỆ THỐNG HƯỚNG DẪN TÂN THỦ (GAME TUTORIAL)", titleStyle);
        EditorGUILayout.HelpBox(
            "Kéo thả các file VideoClip tương ứng cho từng bước bên dưới.\n" +
            "• Video sẽ tự động lặp (Loop) và căn chỉnh khung hình.\n" +
            "• Nếu bước nào chưa có video clip, khung minh họa sẽ ẩn video và hiển thị nội dung chữ.",
            MessageType.Info);

        EditorGUILayout.Space(6);

        SerializedProperty stepVideosProp = serializedObject.FindProperty("stepVideos");
        if (stepVideosProp != null)
        {
            while (stepVideosProp.arraySize < 5)
            {
                stepVideosProp.InsertArrayElementAtIndex(stepVideosProp.arraySize);
            }

            EditorGUILayout.LabelField("DANH SÁCH VIDEO TỪNG BƯỚC (5 BƯỚC)", EditorStyles.boldLabel);

            for (int i = 0; i < 5; i++)
            {
                SerializedProperty elem = stepVideosProp.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(StepTitles[i], EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(elem, new GUIContent("Video Clip"));
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(target);
                }

                if (elem.objectReferenceValue == null)
                {
                    EditorGUILayout.LabelField("  ↳ (Chưa gán video - bước này sẽ chỉ hiện chữ)", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("CÁC THÀNH PHẦN LIÊN KẾT (UI & SPAWNER REFERENCES)", EditorStyles.boldLabel);
        DrawPropertiesExcluding(serializedObject, "stepVideos", "m_Script");

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
