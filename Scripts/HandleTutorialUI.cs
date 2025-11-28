using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HandleTutorialUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Button proceedButton;

    [Header("Typewriter Setting")]
    [SerializeField] private string messageText;
    [SerializeField] private float typeWriterDelay;

    [Header("Animation Setting")]
    [SerializeField] private float animationIntroDuration;

    private void OnEnable()
    {
        StartUI();
    }

    private void Update()
    {
        HandleCursorSettings.instance.EnableCursor(true, false);
    }

    
    private void StartUI()
    {
        proceedButton.interactable = false;

        text.text = "";

        StartCoroutine(StartTypeWriterEffect());
    }
    private IEnumerator StartTypeWriterEffect()
    {
        yield return new WaitForSeconds(animationIntroDuration);

        foreach (char letter in messageText)
        {
            text.text += letter;
            yield return new WaitForSeconds(typeWriterDelay);
        }

        // wait a second before allowing the user to proceed
        yield return new WaitForSeconds(1);
        proceedButton.interactable = true;
    }
}
