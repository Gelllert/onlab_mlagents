using System.Collections.Generic;
using UnityEngine;

public class Route : MonoBehaviour
{
    [Header("Route Definition")]
    [SerializeField, Tooltip("First point of the route")] 
    private RoutePoint startPoint;
    
    [Header("Prefabs Needed")]
    [SerializeField, Tooltip("waypoint prefab, with trigger and script")] 
    private GameObject waypointPrefab;

    public List<Waypoint> GenerateDynamicPath(string agentName, bool isTraining)
    {
        List<Waypoint> generatedWaypoints = new List<Waypoint>();
        if (startPoint == null) return generatedWaypoints;

        RoutePoint currentMockNode = startPoint;
        Waypoint previousPhysicalWaypoint = null;

        while (currentMockNode != null)
        {
            Vector3 finalPos = currentMockNode.transform.position;
            
            if (isTraining)
            {
                float waypointRadius = currentMockNode.spawnRadius;
                finalPos += Random.insideUnitSphere * waypointRadius;
            }
            
            GameObject wpGo = Instantiate(waypointPrefab, finalPos, Quaternion.identity);
            wpGo.name = $"Dyn_WP_{agentName}_{generatedWaypoints.Count}";
            
            Waypoint physicalWp = wpGo.GetComponent<Waypoint>();
            physicalWp.timeToNextWaypoint = currentMockNode.timeToNext;
            physicalWp.isGoal = currentMockNode.isGoal;
            
            if (previousPhysicalWaypoint != null)
            {
                previousPhysicalWaypoint.nextWaypoint = physicalWp;
            }

            generatedWaypoints.Add(physicalWp);
            
            previousPhysicalWaypoint = physicalWp;
            currentMockNode = currentMockNode.nextPoint;
        }

        return generatedWaypoints;
    }
}