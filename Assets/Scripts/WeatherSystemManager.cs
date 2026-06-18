using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


public class WeatherSystemManager : MonoBehaviour
{

    private HashSet<RainZone> localRainZones = new HashSet<RainZone>();
    private HashSet<WindZone> localWindZones = new HashSet<WindZone>();

    [SerializeField]
    private GameObject enviroment;
    [SerializeField]
    private bool showZoneCorners = false;

    void Awake()
    {
        if(enviroment == null) throw new System.Exception("No enviroment given");

        RainZone[] rainZones = enviroment.GetComponentsInChildren<RainZone>();
        WindZone[] windZones = enviroment.GetComponentsInChildren<WindZone>();

        localRainZones.AddRange(rainZones);
        localWindZones.AddRange(windZones);

        Debug.Log($"Weather log of environment: {rainZones.Length} - Rainzones, {windZones.Length} - Windzones");
    }


    //return maxCount amount of the closest zones, its set by the agents network, wich has a burnt in value for this 
    public List<WindZone> GetClosestWindZones(Vector3 agentPosition, int maxCount)
    {
        return localWindZones
            .OrderBy(zone => 
            {
                Collider col = zone.GetComponent<Collider>();
                return col != null ? Vector3.Distance(agentPosition, col.ClosestPoint(agentPosition)) : 9999f;
            })
            .Take(maxCount)
            .ToList();
    }
    
    public List<RainZone> GetClosestRainZones(Vector3 agentPosition, int maxCount)
    {
        return localRainZones
            .OrderBy(zone => 
            {
                Collider col = zone.GetComponent<Collider>();
                return col != null ? Vector3.Distance(agentPosition, col.ClosestPoint(agentPosition)) : 9999f;
            })
            .Take(maxCount)
            .ToList();
    }





    private void OnDrawGizmos()
    {
        if (!showZoneCorners || localRainZones == null || localWindZones == null)
            return;

        foreach (RainZone rainZone in localRainZones)
        {
            ZoneStruct parameters = rainZone.getZoneParameters();
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(parameters.center, 10f);
            DrawZoneCorners(parameters, Color.blue);
        }

        foreach (WindZone windZone in localWindZones)
        {
            ZoneStruct parameters = windZone.getZoneParameters();
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(parameters.center, 10f);
            DrawZoneCorners(parameters, Color.green);
        }
    }
    private void DrawZoneCorners(ZoneStruct zone, Color color)
    {

        float halfWidth = zone.width / 2f;
        float halfHeight = zone.height / 2f;
        float halfLength = zone.lenght / 2f;

        Vector3[] corners = new Vector3[8]
        {
            zone.center + new Vector3(-halfWidth, -halfHeight, -halfLength),
            zone.center + new Vector3(halfWidth, -halfHeight, -halfLength),
            zone.center + new Vector3(halfWidth, -halfHeight, halfLength),
            zone.center + new Vector3(-halfWidth, -halfHeight, halfLength),
            zone.center + new Vector3(-halfWidth, halfHeight, -halfLength),
            zone.center + new Vector3(halfWidth, halfHeight, -halfLength),
            zone.center + new Vector3(halfWidth, halfHeight, halfLength),
            zone.center + new Vector3(-halfWidth, halfHeight, halfLength),
        };

        Gizmos.color = color;
        foreach (Vector3 corner in corners)
        {
            Gizmos.DrawSphere(corner, 5f);
        }
    }
}

