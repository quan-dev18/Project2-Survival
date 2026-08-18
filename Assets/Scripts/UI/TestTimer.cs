using UnityEngine;
using TMPro;

public class TestTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;

    private float elapsedSeconds;

    private void Start()
    {
        if (timerText == null)
        {
            timerText = GetComponent<TextMeshProUGUI>();
        }

        if (timerText != null)
        {
            timerText.text = "0s";
        }
    }

    private void Update()
    {
        elapsedSeconds += Time.deltaTime;

        if (timerText != null)
        {
            int seconds = Mathf.FloorToInt(elapsedSeconds);
            timerText.text = seconds + "s";
        }
    }
}
