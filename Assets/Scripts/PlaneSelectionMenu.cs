using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class PlaneSelectionMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PaperPlaneController controller;

    [Header("Settings")]
    [SerializeField] private float deadzoneRadius = 100f; 
    [SerializeField] private float outerVisualRadius = 250f;
    [SerializeField] private float lineThickness = 6f; 

    [Header("Appearance & Transparency")]
    [SerializeField] private Color cancelColor = new Color(1f, 0f, 0f, 0.8f);
    [SerializeField] private Color activeColor = new Color(0f, 1f, 1f, 0.9f);
    [SerializeField] private Color inactiveOuterColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] private Color sliceLineColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color labelColor = Color.white;

    private InputSystem_Actions inputs;
    private PlaneTypeEnum currentHoverSelection;
    private bool isMouseInSelectionZone = false;
    private bool menuOpen;

    void Awake()
    {
        GetComponent<Canvas>().enabled = false;
        inputs = InputSystemManager.Instance.IO;
    }

    private void OnEnable() => inputs.Plane.Enable();
    private void OnDisable() => inputs.Plane.Disable();

    void Update()
    {
        if (controller.IsGrounded()) return;

        if (!menuOpen)
        {
            if (inputs.Plane.Fold.WasPressedThisFrame()) OpenMenu();
        }
        else
        {
            UpdateSelection();
            if (inputs.Plane.Fold.WasReleasedThisFrame()) CloseMenuAndApply();
        }
    }

    private void OpenMenu()
    {
        menuOpen = true;
        isMouseInSelectionZone = false;
        controller.RequestPlaneChange(PlaneTypeEnum.PaperBall, false);
    }

    private void CloseMenuAndApply()
    {
        menuOpen = false;
        if (isMouseInSelectionZone)
            controller.RequestPlaneChange(currentHoverSelection, true);
        else
            controller.RequestPlaneChange(controller.GetCurrentPlaneType(), false);
    }

    private void UpdateSelection()
    {
        Vector2 mousePos = inputs.Plane.MousePosition.ReadValue<Vector2>();
        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 dir = mousePos - center;
        float distance = dir.magnitude; 

        if (distance > deadzoneRadius)
        {
            isMouseInSelectionZone = true;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            if (angle >= 330 || angle < 90)       currentHoverSelection = PlaneTypeEnum.Default;
            else if (angle >= 90 && angle < 210)  currentHoverSelection = PlaneTypeEnum.HuntingFlight;
            else                                  currentHoverSelection = PlaneTypeEnum.Dart;
        }
        else
        {
            isMouseInSelectionZone = false;
        }
    }

    void OnGUI()
    {
        if (!menuOpen) return;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        
        GUI.color = isMouseInSelectionZone ? Color.white : cancelColor;
        DrawDebugCircle(screenCenter, deadzoneRadius, 32);

        GUI.color = isMouseInSelectionZone ? activeColor : inactiveOuterColor;
        DrawDebugCircle(screenCenter, outerVisualRadius, 64);


        GUI.color = sliceLineColor;
        DrawSliceLine(screenCenter, 90, deadzoneRadius, outerVisualRadius);
        DrawSliceLine(screenCenter, 210, deadzoneRadius, outerVisualRadius);
        DrawSliceLine(screenCenter, 330, deadzoneRadius, outerVisualRadius);


        if (isMouseInSelectionZone)
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) {
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };

            Vector2 mousePos = inputs.Plane.MousePosition.ReadValue<Vector2>();
            float guiMouseY = Screen.height - mousePos.y;
            

            string hexColor = ColorUtility.ToHtmlStringRGBA(activeColor);
            string text = $"<color=#{hexColor}>{currentHoverSelection}</color>";
            
            GUI.color = labelColor;
            GUI.Label(new Rect(mousePos.x - 100, guiMouseY - 45, 200, 40), text, labelStyle);
        }
        
        Vector2 mPos = inputs.Plane.MousePosition.ReadValue<Vector2>();
        GUI.color = isMouseInSelectionZone ? activeColor : cancelColor;
        GUI.Box(new Rect(mPos.x - 4, Screen.height - mPos.y - 4, 8, 8), "");
    }

    private void DrawSliceLine(Vector2 center, float angleDeg, float innerR, float outerR)
    {
        Matrix4x4 matrixBackup = GUI.matrix;
        Vector2 startPos = center + GetUnitVector(angleDeg) * innerR;
        GUIUtility.RotateAroundPivot(-angleDeg, startPos); 
        GUI.DrawTexture(new Rect(startPos.x, startPos.y, outerR - innerR, lineThickness), Texture2D.whiteTexture);
        GUI.matrix = matrixBackup;
    }

    private void DrawDebugCircle(Vector2 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        float segmentLen = (2 * Mathf.PI * radius) / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            Vector2 pos = center + GetUnitVector(angle) * radius;
            Matrix4x4 matrixBackup = GUI.matrix;
            GUIUtility.RotateAroundPivot(-angle - 90 - angleStep/2, pos); 
            GUI.DrawTexture(new Rect(pos.x, pos.y, segmentLen + 1, lineThickness), Texture2D.whiteTexture);
            GUI.matrix = matrixBackup;
        }
    }

    private Vector2 GetUnitVector(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));
    }
}