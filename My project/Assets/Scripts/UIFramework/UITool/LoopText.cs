using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LoopText : MonoBehaviour
{
    public List<string> contents;

    private TextMeshProUGUI tmpText;
    private Tween textTween;

    private void Start()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        int contentIndex = 0;

        textTween = DOVirtual.DelayedCall(0.5f, () =>
        {
            tmpText.text = contents[contentIndex];
            contentIndex++;
            if(contentIndex >= contents.Count)
                contentIndex = 0;
        }).SetLoops(-1, LoopType.Restart);
    }

    private void OnDisable()
    {
        textTween.Kill();
    }
}
