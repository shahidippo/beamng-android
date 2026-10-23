using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ImpactDrive.Runtime
{
    /// <summary>
    /// Owns one pointer until it is released. Dragging off a pedal never transfers
    /// that finger to steering; a second finger may independently own another control.
    /// </summary>
    public sealed class VirtualControl : MonoBehaviour, IPointerDownHandler, IDragHandler,
        IPointerUpHandler, IEndDragHandler
    {
        public enum ControlKind { Stick, Pedal, Button, Wheel }
        public ControlKind Kind;
        public RectTransform Thumb;
        public Image Fill;
        public Vector2 Value { get; private set; }
        public bool IsHeld { get; private set; }
        private int pointerId;
        private RectTransform rect;
        private float previousAngle;
        private float wheelAngle;
        private const float WheelLockDegrees = 130f;

        private void Awake() { rect = (RectTransform)transform; }

        public void OnPointerDown(PointerEventData data)
        {
            if (IsHeld) return;
            pointerId = data.pointerId;
            IsHeld = true;
            if (Kind == ControlKind.Wheel)
            {
                Vector2 point;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position,
                    data.pressEventCamera, out point);
                previousAngle = Mathf.Atan2(point.y, point.x) * Mathf.Rad2Deg;
            }
            ReadPointer(data);
        }

        public void OnDrag(PointerEventData data)
        {
            if (IsHeld && data.pointerId == pointerId) ReadPointer(data);
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (IsHeld && data.pointerId == pointerId) Release();
        }

        public void OnEndDrag(PointerEventData data) { OnPointerUp(data); }

        private void ReadPointer(PointerEventData data)
        {
            Vector2 point;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position,
                data.pressEventCamera, out point)) return;
            if (Kind == ControlKind.Button)
                Value = Vector2.one;
            else if (Kind == ControlKind.Pedal)
                Value = new Vector2(0f, Mathf.Clamp01((point.y - rect.rect.yMin) / rect.rect.height));
            else if (Kind == ControlKind.Wheel)
            {
                // Positive rack input is clockwise in the driver's view.
                float angle = Mathf.Atan2(point.y, point.x) * Mathf.Rad2Deg;
                if (point.sqrMagnitude > 100f)
                    wheelAngle = Mathf.Clamp(wheelAngle - Mathf.DeltaAngle(previousAngle, angle),
                        -WheelLockDegrees, WheelLockDegrees);
                previousAngle = angle;
                Value = new Vector2(wheelAngle / WheelLockDegrees, 0f);
            }
            else
                Value = Vector2.ClampMagnitude(new Vector2(point.x / (rect.rect.width * .4f),
                    point.y / (rect.rect.height * .4f)), 1f);
            RefreshVisual();
        }

        private void Update()
        {
            if (Kind != ControlKind.Wheel || IsHeld || Mathf.Abs(wheelAngle) < .01f) return;
            wheelAngle = Mathf.MoveTowards(wheelAngle, 0f, 300f * Time.unscaledDeltaTime);
            Value = new Vector2(wheelAngle / WheelLockDegrees, 0f);
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (Thumb)
            {
                if (Kind == ControlKind.Wheel)
                    Thumb.localRotation = Quaternion.Euler(0f, 0f, -wheelAngle);
                else
                    Thumb.anchoredPosition = new Vector2(Value.x * rect.rect.width * .32f,
                        Kind == ControlKind.Stick ? Value.y * rect.rect.height * .32f : 0f);
            }
            if (Fill) Fill.fillAmount = Kind == ControlKind.Pedal ? Value.y : (IsHeld ? 1f : .15f);
        }

        public void Release()
        {
            IsHeld = false;
            pointerId = int.MinValue;
            wheelAngle = 0f;
            Value = Vector2.zero;
            RefreshVisual();
        }

        private void OnDisable() { Release(); }
        private void OnApplicationFocus(bool focused) { if (!focused) Release(); }
        private void OnApplicationPause(bool paused) { if (paused) Release(); }
    }
}
