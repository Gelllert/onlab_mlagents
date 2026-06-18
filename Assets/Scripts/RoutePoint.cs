using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RoutePoint : MonoBehaviour
{
    [Header("Node Data")]
    public RoutePoint nextPoint;
    public float spawnRadius = 30f;
    public float timeToNext = 20f;
    public bool isGoal = false;
    [SerializeField] private bool hideData = true;


    //átlag default plane
    private float baseSpeed = 40f;
    private float diveInfluence = 0.9f;
    private float climbPenalty = 1.2f;



    private void OnDrawGizmos()
    {
        Gizmos.color = isGoal ? new Color(0f, 1f, 0f, 0.3f) : new Color(1f, 0.92f, 0.016f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        if (nextPoint != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, nextPoint.transform.position);

            Vector3 diff = nextPoint.transform.position - transform.position;
            Vector3 direction = diff.normalized;

            Vector3 arrowHead = transform.position + (direction * (spawnRadius + 2f));
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(arrowHead, 10f);

#if UNITY_EDITOR

            if(hideData) return;

            float distance = diff.magnitude;
            float heightDelta = diff.y;
            float angle = Mathf.Asin(heightDelta / distance) * Mathf.Rad2Deg;
            float verticalAlignment = Vector3.Dot(direction, Vector3.down); 
            float speedMod = 0f;

            if (verticalAlignment > 0) {
                speedMod = verticalAlignment * baseSpeed * diveInfluence; //zuhan
            } else {
                speedMod = verticalAlignment * baseSpeed * climbPenalty;  //emelkedik
            }

            float expectedSpeed = Mathf.Max(baseSpeed + speedMod, 0f);
            float recommendedTime = distance / Mathf.Max(expectedSpeed, 1f);

            Vector3 labelPosition = Vector3.Lerp(transform.position, nextPoint.transform.position, 0.5f);
            GUIStyle style = new GUIStyle();
            
            //ha a sebesség a stall határ (40 * 0.4 = 16) alá esik, túl magas
            bool willStall = expectedSpeed < (baseSpeed * 0.4f);
            style.normal.textColor = willStall ? Color.red : Color.cyan;
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;

            string infoText = $"Táv: {distance:F1}m | deltaY: {heightDelta:F1}m\n" +
                              $"Szög: {angle:F1}fok\n" +
                              $"Becsült sebesség: {expectedSpeed:F1} m/s {(willStall ? "[STALL!]" : "")}\n" +
                              $"Ajánlott idő: {recommendedTime:F1}s";

            Handles.Label(labelPosition, infoText, style);
#endif
        }
    }
}