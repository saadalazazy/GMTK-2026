using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class HoverInfo3D : MonoBehaviour
{
    [Header("Info UI")]
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private CanvasGroup infoCanvasGroup;

    [TextArea(2, 5)]
    [SerializeField] private string objectInfo;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.2f;

    private readonly List<Outline> outlineScripts = new();

    private void Awake()
    {
        FindOutlines();
        SetOutlinesEnabled(false);

        if (infoCanvasGroup != null)
            infoCanvasGroup.alpha = 0f;
    }

    private void OnMouseEnter()
    {
        if (infoText != null)
            infoText.text = objectInfo;

        SetOutlinesEnabled(true);
        FadeInfo(1f);
    }

    private void OnMouseExit()
    {
        SetOutlinesEnabled(false);
        FadeInfo(0f);
    }

    private void FindOutlines()
    {
        AddOutlineScripts(GetComponentsInParent<Outline>(true));
        AddOutlineScripts(GetComponentsInChildren<Outline>(true));
    }

    private void AddOutlineScripts(Outline[] scripts)
    {
        foreach (Outline script in scripts)
        {
            if (script == null)
                continue;

            // Works with common outline scripts named "Outline".
            if (script.GetType().Name.Equals("Outline", StringComparison.OrdinalIgnoreCase)
                && !outlineScripts.Contains(script))
            {
                outlineScripts.Add(script);
            }
        }
    }

    private void SetOutlinesEnabled(bool enabled)
    {
        foreach (Behaviour outline in outlineScripts)
        {
            if (outline != null)
                outline.enabled = enabled;
        }
    }

    private void FadeInfo(float targetAlpha)
    {
        if (infoCanvasGroup == null)
            return;

        infoCanvasGroup.DOKill();
        infoCanvasGroup.DOFade(targetAlpha, fadeDuration);
    }
}