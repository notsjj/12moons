using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegisterUICamera : MonoBehaviour
{
    private Canvas canvas;

    private void Start()
    {
        canvas = GetComponent<Canvas>();

        canvas.worldCamera = GameObject.FindWithTag("UI Camera").GetComponent<Camera>();
    }
}
