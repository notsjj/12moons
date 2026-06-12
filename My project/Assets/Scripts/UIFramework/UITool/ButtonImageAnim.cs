using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonImageAnim : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    [Header("Ã˘Õº…Ë÷√")]
    public Sprite enterIcon;
    public Sprite exitIcon;

    private Button button;
    private Image image;

    private void OnEnable()
    {
        image = GetComponent<Image>();
        button = GetComponent<Button>();

        image.sprite = exitIcon;
        if(image.sprite == null)
            image.enabled = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {

    }

    public void OnPointerDown(PointerEventData eventData)
    {

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        image.sprite = enterIcon;
        if (image.sprite != null)
            image.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (exitIcon == null)
            image.enabled = false;
        else
            image.sprite = exitIcon;
    }
}
