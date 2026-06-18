using UnityEngine;

public enum PlaneTypeEnum
{
    Default,
    Dart,
    HuntingFlight,
    PaperBall
}

[CreateAssetMenu(fileName = "PaperPlaneType", menuName = "Scriptable Objects/PaperPlaneType")]
public class PaperPlaneType : ScriptableObject
{
    public float SpeedMultiplier = 1f;
    public float ResponsivenessMultiplier = 1f;
    public float UpLiftMultiplier = 1f;
    public PlaneTypeEnum planeType = PlaneTypeEnum.Default;
    public GameObject prefab;
}
