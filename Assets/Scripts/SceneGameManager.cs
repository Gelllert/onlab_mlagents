using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class SceneGameManager : MonoBehaviour
{
    [Header("Route Factory")]
    [SerializeField, Tooltip("A 6 db Route objektum (amin a kezdő RoutePoint van)")] 
    private List<Route> availableRoutes;
    
    
    [Header("Area Parameters")]
    [SerializeField] private float episodeTimeLimit = 60f;
    [SerializeField] private WeatherSystemManager weatherManager;

    [Header("Cameras")]
    [Tooltip("Virtual camera used during flight")]
    [SerializeField] private CinemachineCamera flightCam;
    [Tooltip("Virtual camera used when rolldown on ground")]
    [SerializeField] private CinemachineCamera groundCam;

    [Header("ML-Agents Control")]
    [SerializeField, Tooltip("Randomize waypoints during episode generation.")] 
    private bool isTrainingMode = true;


    private Dictionary<PaperPlaneAgent, List<Waypoint>> activeAgentPaths = new Dictionary<PaperPlaneAgent, List<Waypoint>>();

    public WeatherSystemManager GetWeatherManager() => weatherManager;
    public float GetTimeLimit() => episodeTimeLimit;


    public void RegisterAgent(PaperPlaneAgent agent)
    {
        if (!activeAgentPaths.ContainsKey(agent))
        {
            activeAgentPaths.Add(agent, new List<Waypoint>());
        }
    }


    public void RefreshAgentRoute(PaperPlaneAgent agent)
    {
        if (activeAgentPaths.ContainsKey(agent) && activeAgentPaths[agent] != null)
        {
            List<Waypoint> oldPath = activeAgentPaths[agent];
            for (int i = 0; i < oldPath.Count; i++)
            {
                if (oldPath[i] != null) Destroy(oldPath[i].gameObject);
            }
            oldPath.Clear();
        }
        
        if (!activeAgentPaths.ContainsKey(agent)) RegisterAgent(agent);

        if (availableRoutes == null || availableRoutes.Count == 0) 
        {
            Debug.LogError("Nincs beállítva Route a SceneGameManager-ben!");
            return;
        }
        Route selectedRoute = availableRoutes[Random.Range(0, availableRoutes.Count)];

        // Lekérjük a láncot a gyárból
        List<Waypoint> newPath = selectedRoute.GenerateDynamicPath(agent.gameObject.name, isTrainingMode);
        activeAgentPaths[agent] = newPath;

        // JAVÍTÁS: Meghívjuk az ágens új, dedikált inicializáló metódusát
        if (newPath.Count > 0)
        {
            agent.SetupNewRoute(
                newPath, 
                episodeTimeLimit, 
                weatherManager, 
                flightCam, 
                groundCam
            );
        }
    }
}