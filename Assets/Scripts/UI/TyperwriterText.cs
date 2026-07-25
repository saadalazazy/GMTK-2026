using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TyperwriterText : MonoBehaviour
{
    [SerializeField] private float charactersPerSecond = 30f;
    [SerializeField] private float punctuationDelay = 0.15f;
    [SerializeField] private string punctuation = ".!?,;:";
    [SerializeField] private bool startOnEnable = true;
    [SerializeField] private float disappearDelay = 0f;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Events")]
    public UnityEvent OnTypewriterComplete;

    TextMeshProUGUI textComponent;
    string fullText;
    Coroutine activeRoutine;

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        if (startOnEnable && !string.IsNullOrEmpty(fullText))
            StartTyping(fullText);
    }

    public void StartTyping(string text)
    {
        fullText = text;
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);
        textComponent.alpha = 1f;
        activeRoutine = StartCoroutine(TypeRoutine());
    }

    public void Skip()
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);
        textComponent.text = fullText;
        activeRoutine = null;
        OnTypewriterComplete?.Invoke();
    }

    IEnumerator TypeRoutine()
    {
        textComponent.text = "";
        float timer = 0f;

        for (int i = 0; i < fullText.Length; i++)
        {
            textComponent.text += fullText[i];

            float delay = 1f / charactersPerSecond;
            if (punctuation.IndexOf(fullText[i]) >= 0)
                delay += punctuationDelay;

            timer = 0f;
            while (timer < delay)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        activeRoutine = null;
        OnTypewriterComplete?.Invoke();

        if (disappearDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(disappearDelay);
            textComponent.DOFade(0f, fadeDuration).SetUpdate(true);
        }
    }
}
