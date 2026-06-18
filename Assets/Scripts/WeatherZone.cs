using UnityEngine;

public struct ZoneStruct{
    public Vector3 center;
    //side to side on X axis
    public float width;
    //side to side on Y axis
    public float height;
    //side to side on Z axis
    public float lenght;
}
[RequireComponent(typeof(Collider))]
public abstract class WeatherZone : MonoBehaviour {
    
    private void Awake() {
        Init();
    }
    protected abstract void Init();

    public abstract ZoneStruct getZoneParameters();

}