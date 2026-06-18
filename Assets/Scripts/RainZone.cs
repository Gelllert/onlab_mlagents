using System.Collections.Generic;
using UnityEngine;



public class RainZone : WeatherZone
{
    [SerializeField] private GameObject raindropPrefab;
    [SerializeField] private GameObject rainTunnelPrefab;
    [SerializeField] private float raindropSpeed = 5f;
    [SerializeField] private int density = 10; 
    [Tooltip("0 = cell center, 1 = full-cell random jitter")]
    [SerializeField, Range(0f, 1f)] private float gridJitter = 0.75f;
    [Tooltip("Jitter amount in the tunnel")]
    [SerializeField] private float spawnJitter = 1f;

    [Tooltip("On the Y-axis, how far below do raindrops travel")]
    [SerializeField] private float zoneHight = 10f;

    [Tooltip("how far from the center (on the X and Z axes) of the area can raindrops spawn")]
    [SerializeField] private float areaOfSpawn = 10f; 

    [Tooltip("on the choosen x,z point traveling on the Y axis, how many active raindrops are at the same time")]
    [SerializeField] private int modularity = 1;

    
    private List<RainTunnel> raindropTunnels = new List<RainTunnel>();

    protected override void Init()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        if (trigger == null) return;

            trigger.isTrigger = true;
            trigger.size = new Vector3(areaOfSpawn * 2f, zoneHight, areaOfSpawn * 2f);
            trigger.center = new Vector3(0f, zoneHight / 2f, 0f);
        

        int cellsPerSide = Mathf.Max(0, density);
        int cellCount = cellsPerSide * cellsPerSide;
        float randomizeTime = zoneHight / (raindropSpeed * modularity);

        for (int i = 0; i < cellCount; i++)
        {  

            Vector3 pos = GetCeilingPosition(i);
            GameObject tunnel = Instantiate(rainTunnelPrefab, pos, Quaternion.identity, transform);
            RainTunnel tunnelComponent = tunnel.GetComponent<RainTunnel>();

            if(tunnelComponent == null) throw new System.Exception("Cant find RainTunnel component.");

            tunnelComponent.Init(
                raindropPrefab, // raindrop prefab
                Random.Range(0f, randomizeTime), // Randomize the start time offset for each raindrop
                zoneHight, // height of the tunnel
                raindropSpeed, // how fast should it fall
                modularity, // how many should fall
                spawnJitter
            );
            raindropTunnels.Add(tunnelComponent);
        }
    }

    private Vector3 GetCeilingPosition(int index)
    {

        int cellsPerSide = Mathf.Max(1, density);
        int cols = cellsPerSide;
        int rows = cellsPerSide;

        float totalWidth = areaOfSpawn * 2f;
        float totalDepth = areaOfSpawn * 2f;

        float cellWidth = totalWidth / cols;
        float cellDepth = totalDepth / rows;

        int col = index % cols;
        int row = index / cols;

        //cell center in world space
        float centerX = transform.position.x - areaOfSpawn + (col + 0.5f) * cellWidth;
        float centerZ = transform.position.z - areaOfSpawn + (row + 0.5f) * cellDepth;

        //jitter within the cell
        float jitterX = (Random.value - 0.5f) * cellWidth * gridJitter;
        float jitterZ = (Random.value - 0.5f) * cellDepth * gridJitter;

        float x = centerX + jitterX;
        float y = transform.position.y + zoneHight;
        float z = centerZ + jitterZ;

        return new Vector3(x, y, z);
    }

    public override ZoneStruct getZoneParameters()
    {
        ZoneStruct zone = new ZoneStruct();
        
        // RainZone transform is at the bottom, so center is offset up by half height
        zone.center = transform.position + new Vector3(0, zoneHight / 2f, 0);
        zone.width = areaOfSpawn*2;
        zone.height =zoneHight;
        zone.lenght = areaOfSpawn*2;
        
        return zone;
    }

    void Update()
    {
        raindropTunnels.ForEach((tunnel) => {
            tunnel.UpdateTunnel(Time.deltaTime);
        });
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        
        int cellsPerSide = Mathf.Max(1, density);
        float totalWidth = areaOfSpawn * 2f;
        float totalDepth = areaOfSpawn * 2f;
        float cellWidth = totalWidth / cellsPerSide;
        float cellDepth = totalDepth / cellsPerSide;

        Vector3 topCenter = transform.position + Vector3.up * zoneHight;
        Vector3 minCorner = topCenter + new Vector3(-areaOfSpawn, 0, -areaOfSpawn);
        Vector3 maxCorner = topCenter + new Vector3(areaOfSpawn, 0, areaOfSpawn);
        
        Gizmos.DrawLine(minCorner, new Vector3(maxCorner.x, minCorner.y, minCorner.z));
        Gizmos.DrawLine(new Vector3(maxCorner.x, minCorner.y, minCorner.z), maxCorner);
        Gizmos.DrawLine(maxCorner, new Vector3(minCorner.x, maxCorner.y, maxCorner.z));
        Gizmos.DrawLine(new Vector3(minCorner.x, maxCorner.y, maxCorner.z), minCorner);

        Gizmos.color = Color.yellow;
        for (int col = 1; col < cellsPerSide; col++)
        {
            float x = minCorner.x + col * cellWidth;
            Vector3 start = new Vector3(x, topCenter.y, minCorner.z);
            Vector3 end = new Vector3(x, topCenter.y, maxCorner.z);
            Gizmos.DrawLine(start, end);
        }
        
        for (int row = 1; row < cellsPerSide; row++)
        {
            float z = minCorner.z + row * cellDepth;
            Vector3 start = new Vector3(minCorner.x, topCenter.y, z);
            Vector3 end = new Vector3(maxCorner.x, topCenter.y, z);
            Gizmos.DrawLine(start, end);
        }
        
        Gizmos.color = Color.magenta;
        Vector3 bottomCenter = transform.position;
        Vector3 bottomMin = bottomCenter + new Vector3(-areaOfSpawn, 0f, -areaOfSpawn);
        Vector3 bottomMax = bottomCenter + new Vector3(areaOfSpawn, 0f, areaOfSpawn);
        
        Gizmos.DrawLine(minCorner, bottomMin);
        Gizmos.DrawLine(new Vector3(maxCorner.x, minCorner.y, minCorner.z), new Vector3(bottomMax.x, bottomMin.y, bottomMin.z));
        Gizmos.DrawLine(maxCorner, bottomMax);
        Gizmos.DrawLine(new Vector3(minCorner.x, maxCorner.y, maxCorner.z), new Vector3(bottomMin.x, bottomMin.y, bottomMax.z));
    }

}
