using TMPro;
using UnityEngine;

public class GearsHint : MonoBehaviour
{
    public TextMeshProUGUI messageText;  // 指向 TMP 文字
    public float showDuration = 1.5f;    // 显示多久
    private Coroutine showRoutine;

    public void ShowMessage()
    {
        if (showRoutine != null)
            StopCoroutine(showRoutine);

        showRoutine = StartCoroutine(ShowRoutine());
    }

    private System.Collections.IEnumerator ShowRoutine()
    {
        messageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(showDuration);
        //messageText.gameObject.SetActive(false);
    }
}