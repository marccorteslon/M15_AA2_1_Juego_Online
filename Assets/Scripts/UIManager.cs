using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(RectTransform))]
public class UIManager : MonoBehaviour
{
    public static bool interfaceVisible;

    public Camera cam;
    Canvas canvas;
    RectTransform canvasRect;
    public RectTransform realAim;
    public RaycastLookAt realAimLookAt;

    public GameObject panel;

    void Start()
    {
        canvas = GetComponent<Canvas>();
        canvasRect = GetComponent<RectTransform>();

        SetInterface(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            SetInterface(!interfaceVisible);
        }
    }

    void SetInterface(bool visible)
    {
        interfaceVisible = visible;

        if (panel)
        {
            panel.SetActive(visible);
        }

        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void OnGUI()
    {
        Vector2 ViewportPosition = cam.WorldToViewportPoint(realAimLookAt.lookingAt);
        Vector2 WorldObject_ScreenPosition = new Vector2(
        ((ViewportPosition.x * canvasRect.sizeDelta.x) - (canvasRect.sizeDelta.x * 0.5f)),
        ((ViewportPosition.y * canvasRect.sizeDelta.y) - (canvasRect.sizeDelta.y * 0.5f)));
        realAim.anchoredPosition = WorldObject_ScreenPosition;
    }
}