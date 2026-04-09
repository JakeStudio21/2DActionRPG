using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DynamicJoystick : Joystick
{
    public float MoveThreshold { get { return moveThreshold; } set { moveThreshold = Mathf.Abs(value); } }

    [SerializeField] private float moveThreshold = 1;
    [SerializeField] private float slideMultiplier = 0.1f;
    [SerializeField] private float maxSlideDistance = 50f;

    private Vector2 touchStartPosition;

    protected override void Start()
    {
        MoveThreshold = moveThreshold;
        base.Start();
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (RectTransformUtility.RectangleContainsScreenPoint(baseRect, eventData.position, eventData.pressEventCamera))
        {
            background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
            touchStartPosition = background.anchoredPosition;
            base.OnPointerDown(eventData);
        }
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
    }

    protected override void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, Camera cam)
    {
        if (magnitude > moveThreshold)
        {
            Vector2 difference = normalised * (magnitude - moveThreshold) * radius * slideMultiplier;
            background.anchoredPosition += difference;

            // 터치 시작 위치 기준 최대 이동 거리 제한
            Vector2 offset = background.anchoredPosition - touchStartPosition;
            if (offset.magnitude > maxSlideDistance)
                background.anchoredPosition = touchStartPosition + offset.normalized * maxSlideDistance;

            // 터치 영역(baseRect) 경계 내로 클램프
            Vector2 bgHalfSize = background.sizeDelta / 2f;
            Rect bounds = baseRect.rect;
            Vector3 localPos = background.localPosition;
            localPos.x = Mathf.Clamp(localPos.x, bounds.xMin + bgHalfSize.x, bounds.xMax - bgHalfSize.x);
            localPos.y = Mathf.Clamp(localPos.y, bounds.yMin + bgHalfSize.y, bounds.yMax - bgHalfSize.y);
            background.localPosition = localPos;
        }
        base.HandleInput(magnitude, normalised, radius, cam);
    }
}