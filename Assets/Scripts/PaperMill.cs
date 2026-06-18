using UnityEngine;

public class PaperMill : MonoBehaviour
{
    
    [SerializeField] private Transform head;
    
    [Tooltip("Adjust this to make the visual spinning look right based on the magnitude")]
    [SerializeField] private float rotationSpeedMultiplier = 50f;

    private float magnitude;
    public void setWind(Vector3 wind){
        Debug.Log($"setwind inside {wind}");
        magnitude = wind.magnitude;
        Vector3 flatWind = new Vector3(wind.x, 0f, wind.z);

        if (flatWind.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(-flatWind, Vector3.up);
        }
    }    


    void Update()
    {
        if (head != null)
        {
            head.Rotate(Vector3.right, magnitude * rotationSpeedMultiplier * Time.deltaTime, Space.Self);
        }
    }
}