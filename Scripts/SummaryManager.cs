using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SummaryManager : MonoBehaviour
{
    [SerializeField] private Image backgroundBlocker;//背景遮盖

    private float fadeDuration = 2f;//背景遮罩覆盖过渡时间

    void Start()
    {
        SetMask();
        StartUnmask();
    }

    void Update()
    {
        
    }

    IEnumerator FadeMask(float fromAlpha, float toAlpha)
    {
        float elapsedTime = 0f;
        Color color = backgroundBlocker.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeDuration);

            // 使用插值
            color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
            backgroundBlocker.color = color;

            yield return null;
        }
        // 确保最终值
        color.a = toAlpha;
        backgroundBlocker.color = color;
    }

    public void SetMask()//将背景遮罩显示出来虽然透明度为0
    {
        backgroundBlocker.gameObject.SetActive(true);
    }

    public void StartUnmask()//开始调整背景遮罩透明度，开启协程
    {
        StartCoroutine(FadeMask(1f, 0f));
    }
}
