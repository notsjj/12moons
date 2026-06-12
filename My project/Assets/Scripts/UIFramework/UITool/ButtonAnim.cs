using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAnim : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    private Button button;
    private Vector3 currentScale;

    private Tween scaleTween;

    private void OnEnable()
    {
        button = GetComponent<Button>();
        currentScale = transform.localScale;
    }

    public void OnExit()
    {
        if (scaleTween != null)
            scaleTween.Kill();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if(button.interactable)
            scaleTween = transform.DOScale(currentScale, 0.2f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(button.interactable)
            scaleTween = transform.DOScale(currentScale * 0.9f, 0.2f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable)
            scaleTween = transform.DOScale(currentScale * 1.1f, 0.2f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (button.interactable)
            scaleTween = transform.DOScale(currentScale, 0.2f);
    }
}
