using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(RectTransform))]
public class UIManager : MonoBehaviour
{
    public Camera cam;
    Canvas canvas;
    RectTransform canvasRect;
    public RectTransform realAim;
    public RaycastLookAt realAimLookAt;
    void Start()
    {
        canvas = GetComponent<Canvas>();
        canvasRect = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        
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
