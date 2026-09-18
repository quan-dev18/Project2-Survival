#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class GameUITutorialBuilder
{
    private const string SCENE_PATH = "Assets/Scenes/GameTutorial.unity";
    private const string PREFAB_DIR = "Assets/Prefabs/UI/Tutorial";
    private const string PREFAB_PATH = "Assets/Prefabs/UI/Tutorial/GameUITutorial.prefab";
    private const string FONT_PATH = "Assets/Materials/Phonk/SVN-Determination Sans.asset";
    private const string TEST_VIDEO_PATH = "Assets/Videos/TestVideo.mp4";

    [MenuItem("Tools/Setup Video Illustration in GameUITutorial")]
    public static void SetupVideoIllustrationInCurrentScene()
    {
        Scene currentScene = EditorSceneManager.GetActiveScene();
        bool needReopen = false;
        string origPath = currentScene.path;

        if (currentScene.path != SCENE_PATH)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            currentScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            needReopen = true;
        }

        GameObject canvasObj = GameObject.Find("GameUITutorial");
        if (canvasObj == null)
        {
            BuildTutorialUI();
            canvasObj = GameObject.Find("GameUITutorial");
            if (canvasObj == null) return;
        }

        TutorialController controller = canvasObj.GetComponent<TutorialController>();
        if (controller == null)
            controller = canvasObj.AddComponent<TutorialController>();

        // Tìm IllustrationArea
        Transform illustTrans = canvasObj.transform.Find("TutorialPanel/CardDialog/IllustrationArea");
        if (illustTrans == null) return;

        // Tìm hoặc tạo VideoDiChuyen
        Transform videoTrans = illustTrans.Find("VideoDiChuyen");
        if (videoTrans == null)
        {
            GameObject vObj = new GameObject("VideoDiChuyen", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(VideoPlayer));
            vObj.transform.SetParent(illustTrans, false);
            vObj.layer = 5;
            videoTrans = vObj.transform;
        }

        RectTransform vRect = videoTrans.GetComponent<RectTransform>();
        vRect.anchorMin = Vector2.zero;
        vRect.anchorMax = Vector2.one;
        vRect.offsetMin = Vector2.zero;
        vRect.offsetMax = Vector2.zero;

        // Đảm bảo có RawImage
        RawImage rawImg = videoTrans.GetComponent<RawImage>();
        if (rawImg == null)
            rawImg = videoTrans.gameObject.AddComponent<RawImage>();

        // Đảm bảo có VideoPlayer
        VideoPlayer vp = videoTrans.GetComponent<VideoPlayer>();
        if (vp == null)
            vp = videoTrans.gameObject.AddComponent<VideoPlayer>();

        VideoClip testClip = AssetDatabase.LoadAssetAtPath<VideoClip>(TEST_VIDEO_PATH);
        if (vp.clip == null && testClip != null)
        {
            vp.clip = testClip;
        }
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.isLooping = true;
        vp.playOnAwake = false;
        vp.audioOutputMode = VideoAudioOutputMode.None;

        // Ẩn placeholder text nếu có
        Transform ph = illustTrans.Find("PlaceholderText");
        if (ph != null) ph.gameObject.SetActive(false);

        // Liên kết vào TutorialController qua SerializedObject
        SerializedObject so = new SerializedObject(controller);
        SerializedProperty vpProp = so.FindProperty("videoPlayer");
        if (vpProp != null) vpProp.objectReferenceValue = vp;

        SerializedProperty vdProp = so.FindProperty("videoDisplay");
        if (vdProp != null) vdProp.objectReferenceValue = rawImg;

        SerializedProperty stepVideosProp = so.FindProperty("stepVideos");
        if (stepVideosProp != null)
        {
            while (stepVideosProp.arraySize < 5)
            {
                stepVideosProp.InsertArrayElementAtIndex(stepVideosProp.arraySize);
            }

            if (stepVideosProp.GetArrayElementAtIndex(0).objectReferenceValue == null && testClip != null)
            {
                stepVideosProp.GetArrayElementAtIndex(0).objectReferenceValue = testClip;
            }
        }

        so.ApplyModifiedProperties();

        // Lưu cập nhật vào Prefab một cách an toàn
        if (File.Exists(PREFAB_PATH))
        {
            try
            {
                if (PrefabUtility.IsPartOfPrefabInstance(canvasObj))
                {
                    PrefabUtility.ApplyPrefabInstance(canvasObj, InteractionMode.AutomatedAction);
                }
                else
                {
                    PrefabUtility.SaveAsPrefabAssetAndConnect(canvasObj, PREFAB_PATH, InteractionMode.AutomatedAction);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[GameUITutorialBuilder] Không thể ApplyPrefabInstance tự động: " + ex.Message);
            }
        }

        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);

        Debug.Log("[GameUITutorialBuilder] Đã cấu hình thành công Video Loop cho các bước trong GameUITutorial!");

        if (needReopen && !string.IsNullOrEmpty(origPath))
        {
            EditorSceneManager.OpenScene(origPath, OpenSceneMode.Single);
        }
    }

    [MenuItem("Tools/Setup GameUITutorial Canvas (Portrait)")]
    public static void BuildTutorialUI()
    {
        Scene currentScene = EditorSceneManager.GetActiveScene();
        bool needReopenCurrent = false;
        string originalPath = currentScene.path;

        if (currentScene.path != SCENE_PATH)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            currentScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            needReopenCurrent = true;
        }

        GameObject oldCanvas = GameObject.Find("GameUITutorial");
        if (oldCanvas != null)
        {
            Undo.DestroyObjectImmediate(oldCanvas);
        }

        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        VideoClip testClip = AssetDatabase.LoadAssetAtPath<VideoClip>(TEST_VIDEO_PATH);

        // Canvas
        GameObject canvasObj = new GameObject("GameUITutorial", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.layer = 5;

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // TutorialPanel
        GameObject tutorialPanel = new GameObject("TutorialPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        tutorialPanel.transform.SetParent(canvasObj.transform, false);
        tutorialPanel.layer = 5;

        RectTransform panelRect = tutorialPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        Image panelBg = tutorialPanel.GetComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.75f);

        // CardDialog
        GameObject cardDialog = new GameObject("CardDialog", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cardDialog.transform.SetParent(tutorialPanel.transform, false);
        cardDialog.layer = 5;

        RectTransform cardRect = cardDialog.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(940, 1300);

        Image cardBg = cardDialog.GetComponent<Image>();
        cardBg.color = new Color(0.09f, 0.11f, 0.16f, 0.98f);

        // HeaderBar
        GameObject headerObj = new GameObject("HeaderBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        headerObj.transform.SetParent(cardDialog.transform, false);
        headerObj.layer = 5;

        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.anchoredPosition = new Vector2(0, -25);
        headerRect.sizeDelta = new Vector2(-40, 95);

        Image headerBg = headerObj.GetComponent<Image>();
        headerBg.color = new Color(0.18f, 0.28f, 0.45f, 1f);

        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(headerObj.transform, false);
        titleObj.layer = 5;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(15, 0);
        titleRect.offsetMax = new Vector2(-15, 0);

        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null) titleText.font = fontAsset;
        titleText.fontSize = 38;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.yellow;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.text = "1. CÁCH DI CHUYỂN";

        // IllustrationArea
        GameObject illustObj = new GameObject("IllustrationArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        illustObj.transform.SetParent(cardDialog.transform, false);
        illustObj.layer = 5;

        RectTransform illustRect = illustObj.GetComponent<RectTransform>();
        illustRect.anchorMin = new Vector2(0.5f, 1);
        illustRect.anchorMax = new Vector2(0.5f, 1);
        illustRect.pivot = new Vector2(0.5f, 1);
        illustRect.anchoredPosition = new Vector2(0, -145);
        illustRect.sizeDelta = new Vector2(860, 480);

        Image illustBg = illustObj.GetComponent<Image>();
        illustBg.color = new Color(0.05f, 0.07f, 0.1f, 0.95f);

        // VideoDiChuyen
        GameObject vObj = new GameObject("VideoDiChuyen", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(VideoPlayer));
        vObj.transform.SetParent(illustObj.transform, false);
        vObj.layer = 5;

        RectTransform vRect = vObj.GetComponent<RectTransform>();
        vRect.anchorMin = Vector2.zero;
        vRect.anchorMax = Vector2.one;
        vRect.offsetMin = Vector2.zero;
        vRect.offsetMax = Vector2.zero;

        RawImage rawImg = vObj.GetComponent<RawImage>();
        VideoPlayer vp = vObj.GetComponent<VideoPlayer>();
        if (testClip != null) vp.clip = testClip;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.isLooping = true;
        vp.playOnAwake = false;
        vp.audioOutputMode = VideoAudioOutputMode.None;

        // ContentText
        GameObject contentObj = new GameObject("ContentText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        contentObj.transform.SetParent(cardDialog.transform, false);
        contentObj.layer = 5;

        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 1);
        contentRect.anchorMax = new Vector2(0.5f, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = new Vector2(0, -650);
        contentRect.sizeDelta = new Vector2(840, 270);

        TextMeshProUGUI contentText = contentObj.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null) contentText.font = fontAsset;
        contentText.fontSize = 32;
        contentText.color = Color.white;
        contentText.alignment = TextAlignmentOptions.Center;
        contentText.enableWordWrapping = true;
        contentText.text = "Sử dụng Cần điều khiển (Joystick) ở góc trái màn hình (hoặc phím W, A, S, D / Mũi tên) để điều khiển nhân vật.";

        // StepIndicatorText
        GameObject indicatorObj = new GameObject("StepIndicatorText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        indicatorObj.transform.SetParent(cardDialog.transform, false);
        indicatorObj.layer = 5;

        RectTransform indicatorRect = indicatorObj.GetComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(0.5f, 0);
        indicatorRect.anchorMax = new Vector2(0.5f, 0);
        indicatorRect.pivot = new Vector2(0.5f, 0);
        indicatorRect.anchoredPosition = new Vector2(0, 160);
        indicatorRect.sizeDelta = new Vector2(400, 50);

        TextMeshProUGUI indicatorText = indicatorObj.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null) indicatorText.font = fontAsset;
        indicatorText.fontSize = 28;
        indicatorText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        indicatorText.alignment = TextAlignmentOptions.Center;
        indicatorText.text = "Bước 1 / 5";

        // ButtonRow
        GameObject buttonRowObj = new GameObject("ButtonRow", typeof(RectTransform));
        buttonRowObj.transform.SetParent(cardDialog.transform, false);
        buttonRowObj.layer = 5;

        RectTransform rowRect = buttonRowObj.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0);
        rowRect.anchorMax = new Vector2(0.5f, 0);
        rowRect.pivot = new Vector2(0.5f, 0);
        rowRect.anchoredPosition = new Vector2(0, 35);
        rowRect.sizeDelta = new Vector2(860, 105);

        // PrevButton
        GameObject prevBtnObj = new GameObject("PrevButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        prevBtnObj.transform.SetParent(buttonRowObj.transform, false);
        prevBtnObj.layer = 5;

        RectTransform prevBtnRect = prevBtnObj.GetComponent<RectTransform>();
        prevBtnRect.anchorMin = new Vector2(0, 0.5f);
        prevBtnRect.anchorMax = new Vector2(0, 0.5f);
        prevBtnRect.pivot = new Vector2(0, 0.5f);
        prevBtnRect.anchoredPosition = new Vector2(10, 0);
        prevBtnRect.sizeDelta = new Vector2(260, 90);

        Image prevBtnImg = prevBtnObj.GetComponent<Image>();
        prevBtnImg.color = new Color(0.32f, 0.35f, 0.42f, 1f);
        Button prevButton = prevBtnObj.GetComponent<Button>();

        GameObject prevTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        prevTextObj.transform.SetParent(prevBtnObj.transform, false);
        prevTextObj.layer = 5;
        RectTransform prevTextRect = prevTextObj.GetComponent<RectTransform>();
        prevTextRect.anchorMin = Vector2.zero;
        prevTextRect.anchorMax = Vector2.one;

        TextMeshProUGUI prevBtnText = prevTextObj.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null) prevBtnText.font = fontAsset;
        prevBtnText.fontSize = 28;
        prevBtnText.fontStyle = FontStyles.Bold;
        prevBtnText.color = Color.white;
        prevBtnText.alignment = TextAlignmentOptions.Center;
        prevBtnText.text = "QUAY LẠI";

        // NextButton
        GameObject nextBtnObj = new GameObject("NextButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        nextBtnObj.transform.SetParent(buttonRowObj.transform, false);
        nextBtnObj.layer = 5;

        RectTransform nextBtnRect = nextBtnObj.GetComponent<RectTransform>();
        nextBtnRect.anchorMin = new Vector2(1, 0.5f);
        nextBtnRect.anchorMax = new Vector2(1, 0.5f);
        nextBtnRect.pivot = new Vector2(1, 0.5f);
        nextBtnRect.anchoredPosition = new Vector2(-10, 0);
        nextBtnRect.sizeDelta = new Vector2(530, 90);

        Image nextBtnImg = nextBtnObj.GetComponent<Image>();
        nextBtnImg.color = new Color(0.15f, 0.65f, 0.28f, 1f);
        Button nextButton = nextBtnObj.GetComponent<Button>();

        GameObject nextTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        nextTextObj.transform.SetParent(nextBtnObj.transform, false);
        nextTextObj.layer = 5;
        RectTransform nextTextRect = nextTextObj.GetComponent<RectTransform>();
        nextTextRect.anchorMin = Vector2.zero;
        nextTextRect.anchorMax = Vector2.one;

        TextMeshProUGUI nextBtnText = nextTextObj.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null) nextBtnText.font = fontAsset;
        nextBtnText.fontSize = 32;
        nextBtnText.fontStyle = FontStyles.Bold;
        nextBtnText.color = Color.white;
        nextBtnText.alignment = TextAlignmentOptions.Center;
        nextBtnText.text = "TIẾP TỤC";

        // WaveStartBanner
        GameObject bannerObj = new GameObject("WaveStartBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bannerObj.transform.SetParent(canvasObj.transform, false);
        bannerObj.layer = 5;

        RectTransform bannerRect = bannerObj.GetComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0, 0.62f);
        bannerRect.anchorMax = new Vector2(1, 0.72f);
        bannerRect.sizeDelta = Vector2.zero;

        Image bannerImg = bannerObj.GetComponent<Image>();
        bannerImg.color = new Color(0.85f, 0.12f, 0.12f, 0.9f);

        GameObject bannerTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        bannerTextObj.transform.SetParent(bannerObj.transform, false);
        bannerTextObj.layer = 5;

        RectTransform bannerTextRect = bannerTextObj.GetComponent<RectTransform>();
        bannerTextRect.anchorMin = Vector2.zero;
        bannerTextRect.anchorMax = Vector2.one;

        TextMeshProUGUI bannerText = bannerTextObj.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null) bannerText.font = fontAsset;
        bannerText.fontSize = 42;
        bannerText.fontStyle = FontStyles.Bold;
        bannerText.color = Color.yellow;
        bannerText.alignment = TextAlignmentOptions.Center;
        bannerText.text = "LÀN SÓNG QUÁI VẬT BẮT ĐẦU!";

        bannerObj.SetActive(false);

        // TutorialController
        TutorialController controller = canvasObj.AddComponent<TutorialController>();
        EnemySpawner spawner = Object.FindObjectOfType<EnemySpawner>();

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("enemySpawner").objectReferenceValue = spawner;
        so.FindProperty("tutorialUIRoot").objectReferenceValue = tutorialPanel;
        so.FindProperty("stepTitleText").objectReferenceValue = titleText;
        so.FindProperty("stepContentText").objectReferenceValue = contentText;
        so.FindProperty("stepIndicatorText").objectReferenceValue = indicatorText;
        so.FindProperty("nextButton").objectReferenceValue = nextButton;
        so.FindProperty("nextButtonText").objectReferenceValue = nextBtnText;
        so.FindProperty("prevButton").objectReferenceValue = prevButton;
        so.FindProperty("waveStartBanner").objectReferenceValue = bannerObj;
        so.FindProperty("bannerText").objectReferenceValue = bannerText;
        SerializedProperty vpProp = so.FindProperty("videoPlayer");
        if (vpProp != null) vpProp.objectReferenceValue = vp;

        SerializedProperty vdProp = so.FindProperty("videoDisplay");
        if (vdProp != null) vdProp.objectReferenceValue = rawImg;

        SerializedProperty stepVideosProp = so.FindProperty("stepVideos");
        if (stepVideosProp != null)
        {
            stepVideosProp.arraySize = 5;
            stepVideosProp.GetArrayElementAtIndex(0).objectReferenceValue = testClip;
            for (int i = 1; i < 5; i++)
            {
                stepVideosProp.GetArrayElementAtIndex(i).objectReferenceValue = null;
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        if (!Directory.Exists(PREFAB_DIR))
        {
            Directory.CreateDirectory(PREFAB_DIR);
        }
        PrefabUtility.SaveAsPrefabAssetAndConnect(canvasObj, PREFAB_PATH, InteractionMode.AutomatedAction);

        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);

        Debug.Log("[GameUITutorialBuilder] Đã tạo thành công Canvas 'GameUITutorial' với Video Illustration cho màn hình dọc!");

        if (needReopenCurrent && !string.IsNullOrEmpty(originalPath))
        {
            EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
        }
    }
}
#endif
