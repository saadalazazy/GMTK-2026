using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class IntroSequence : MonoBehaviour
{
    [Header("Typewriter")]
    [SerializeField] private TyperwriterText typewriter;
    [SerializeField] private string[] introTexts;
    [SerializeField] private float textDelay = 1f;

    [Header("Disable During Intro")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;
    [SerializeField] private CinemachineCamera introCamera;

    [Header("Events")]
    public UnityEvent onIntroStart;
    public UnityEvent onIntroEnd;

    void Start()
    {
        StartCoroutine(RunIntro());
    }

    IEnumerator RunIntro()
    {
        onIntroStart?.Invoke();

        foreach (var script in scriptsToDisable)
            script.enabled = false;

        foreach (var text in introTexts)
        {
            typewriter.StartTyping(text);
            yield return new WaitUntil(() => typewriter.IsTyping == false);
            yield return new WaitForSeconds(textDelay);
        }

        if (introCamera != null)
            introCamera.Priority = -1;

        foreach (var script in scriptsToDisable)
            script.enabled = true;

        onIntroEnd?.Invoke();
    }
}
