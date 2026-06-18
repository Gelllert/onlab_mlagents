using UnityEngine;


public class WindZone : WeatherZone
{
        
    [Header("Zone Dimensions")]
    [SerializeField, Tooltip("Width (X axis) of the wind zone")]
    private float zoneWidth = 10f;
    
    [SerializeField, Tooltip("Height (Y axis) of the wind zone")]
    private float zoneHeight = 10f;
    
    [SerializeField, Tooltip("Length (Z axis) of the wind zone")]
    private float zoneLength = 10f;


    [Header("The direction and strength of the wind in world space.")]
    [SerializeField]
    private Vector3 windVector = Vector3.forward * 5f;
    public Vector3 Direction {get => windVector;}

    [SerializeField]
    private GameObject windPrefab;
    private GameObject activeWind;


    [Header("Per‑volume settings")]
    [SerializeField, Tooltip("Emission rate multiplier per unit volume (1 means 1 particle/sec per 1 cubic unit)")]
    private float emissionPerUnitVolume = 5f;
    [SerializeField, Tooltip("Max particles multiplier per unit volume")] 
    private float maxParticlesPerUnitVolume = 100f;
    [SerializeField]
    private float emissionTime = 7f;
    [SerializeField]
    private float simSpeed = 2f;

    [Header("Safety limits")]
    [SerializeField, Tooltip("Maximum emission rate to prevent crashes")]
    private float maxEmissionRate = 1000f;
    [SerializeField, Tooltip("Maximum particles to prevent crashes")]
    private int maxParticlesLimit = 10000;
    [SerializeField, Tooltip("Minimum spawntime")]
    private int minTimeLimit = 4;

    protected override void Init()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        Vector3 calculatedSize = new Vector3(zoneWidth, zoneHeight, zoneLength);

        if (windPrefab == null || trigger == null) return;

        trigger.isTrigger = true;
        trigger.size = calculatedSize;
        trigger.center = Vector3.zero;

        activeWind = Instantiate(windPrefab, transform.position, Quaternion.identity, transform);

        ParticleSystem ps = activeWind.GetComponent<ParticleSystem>();
            var mainModule = ps.main;
            var shapeModule = ps.shape;
            var emissionModule = ps.emission;
        
        mainModule.startSpeed = 0f; 
        mainModule.gravityModifier = 0f;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;

        shapeModule.scale = calculatedSize;
        mainModule.simulationSpeed = simSpeed;

        Vector3 scaled = shapeModule.scale;
        float size_normalizer = 1000000f; //volume too big
        float volume = (scaled.x * scaled.y * scaled.z)/size_normalizer;

        float rawEmissionRate = volume * emissionPerUnitVolume;
        emissionModule.rateOverTime = Mathf.Min(rawEmissionRate, maxEmissionRate);
        
        int rawMaxParticles = Mathf.CeilToInt(volume * maxParticlesPerUnitVolume);
        mainModule.maxParticles = Mathf.Min(rawMaxParticles, maxParticlesLimit);

        float rawEmisionTime = emissionTime;
        mainModule.startLifetime = Mathf.Max(rawEmisionTime, minTimeLimit);


        //velocity --> particle velocity (match direction to wind vector)
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(windVector.x * simSpeed);
            vel.y = new ParticleSystem.MinMaxCurve(windVector.y * simSpeed);
            vel.z = new ParticleSystem.MinMaxCurve(windVector.z * simSpeed);

        PaperMill[] affectedWindMills = GetComponentsInChildren<PaperMill>();

        foreach(var mill in affectedWindMills)
        {
            mill.setWind(windVector);
        }
    }

    public override ZoneStruct getZoneParameters()
    {
        ZoneStruct zone = new ZoneStruct(){
            center = transform.position,
            width = zoneWidth,
            height = zoneHeight,
            lenght = zoneLength
        };
        return zone;
    }
    private void OnDrawGizmos()
    {
        if (windVector.sqrMagnitude < 0.01f) return;

        Gizmos.color = Color.green;
        Vector3 zoneSize = new Vector3(zoneWidth, zoneHeight, zoneLength);
        Gizmos.DrawWireCube(transform.position, zoneSize);

        Gizmos.color = Color.cyan;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + windVector;


        Gizmos.DrawLine(startPos, endPos);
        Gizmos.DrawSphere(endPos, 0.5f);
    }
}