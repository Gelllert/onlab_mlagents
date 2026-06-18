using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GoalMarker : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer goalSprite;
    [SerializeField] private Color boundaryColor = Color.cyan;
    [SerializeField] private int boundarySegments = 32; // mennyire sima az a kör
    [SerializeField] private float lineWidth = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;

    private LineRenderer lineRenderer;
    private SphereCollider sphereCollider;
    private Camera mainCamera;

    void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        mainCamera = Camera.main;
        
        // LineRenderer setup
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        SetupLineRenderer();
    }

    void Start()
    {
        DrawBoundary();
    }

    void Update()
    {
        // Billboard effect: sprite mindig a kamera felé fordul
        if (goalSprite != null && mainCamera != null)
        {
            // Sprite pozíciója fix, csak a rotáció fordul
            Vector3 dirToCamera = mainCamera.transform.position - transform.position;
            goalSprite.transform.rotation = Quaternion.LookRotation(dirToCamera);
        }
    }

    private void SetupLineRenderer()
    {
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = boundaryColor;
        lineRenderer.endColor = boundaryColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.alignment = LineAlignment.TransformZ;
    }

    private void DrawBoundary()
    {
        if (sphereCollider == null) return;

        // Körívek az X-Z síkon (vízszintes)
        Vector3[] positions = new Vector3[boundarySegments + 1];
        float radius = sphereCollider.radius;
        Vector3 center = sphereCollider.center;

        for (int i = 0; i <= boundarySegments; i++)
        {
            float angle = (i / (float)boundarySegments) * 2f * Mathf.PI;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            positions[i] = center + new Vector3(x, 0, z);
        }

        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);
    }

    void OnDrawGizmos()
    {
        if (!showGizmo) return;

        SphereCollider collider = GetComponent<SphereCollider>();
        if (collider != null)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Vector3 center = transform.position + collider.center;
            Gizmos.DrawWireSphere(center, collider.radius);
        }
    }
}
