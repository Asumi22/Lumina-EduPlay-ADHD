using TMPro;
using UnityEngine;

public class LoadingText : MonoBehaviour
{
    public TMP_Text loadingText;
    private string baseText = "Loading";
    private float timer;
    private int dotCount;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= 0.5f) // cada medio segundo cambia
        {
            timer = 0f;
            dotCount = (dotCount + 1) % 4; // 0, 1, 2, 3 puntos
            loadingText.text = baseText + new string('.', dotCount);
        }
    }
}
