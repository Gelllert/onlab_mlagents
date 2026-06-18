using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.Cinemachine;
using System;


[RequireComponent(typeof(Rigidbody))]
public class PaperPlaneAgent : Agent
{
    public event System.Action<bool> OnGameOver;
    private InputSystem_Actions inputs;
    private Rigidbody rb;

#region Settings
    private const string warn = " [!!IF MODIFIED, TRAINING MUST RESTART!!]";

    [Header("Flight Settings")]
    [SerializeField, Range(50f, 150f), Tooltip("Rolling speed of the plane." + warn)] 
    private float baseRotationSpeed = 100f;
    
    [SerializeField, Range(30f, 60f), Tooltip("Speed multiplier." + warn)] 
    private float baseMovementSpeed = 40f;
    
    [SerializeField, Range(0.1f, 3f), Tooltip("Applies in wind or during diving." + warn)] 
    private float speedMultiplier = 1f;
    
    [SerializeField, Range(0f, 1f), Tooltip("How much altitude can it hold." + warn)] 
    private float liftEfficiency = 0.8f;
    
    [SerializeField, Tooltip("Acceleration in 0 speed wind." + warn)] 
    private Vector3 defaultDrift = new Vector3(0, -0.2f, 0.0f);
    
    [SerializeField, Range(1f, 5f), Tooltip("Cap multiplier for max speed." + warn)] 
    private float accalerationCap = 3f;

    [Header("Advanced Aerodynamics")]
    [SerializeField, Range(0f, 2f), Tooltip("How much speed is gained when diving." + warn)] 
    private float diveInfluence = 0.9f;
    
    [SerializeField, Range(0f, 3f), Tooltip("How harsh it loses speed when climbing." + warn)] 
    private float climbPenalty = 1.2f;

    [SerializeField, Range(-1.5f, 0f), Tooltip("Minimum wind multiplier limit." + warn)] 
    private float minWindInfluence = -1.0f;

    [SerializeField, Range(0f, 3f), Tooltip("Maximum wind multiplier limit." + warn)] 
    private float maxWindInfluence = 2.0f;

    [SerializeField, Range(1f, 2f), Tooltip("Multiplier for deceleration compared to acceleration." + warn)] 
    private float decelMultiplier = 1.0f;

    [SerializeField, Range(0.01f, 0.99f), Tooltip("Fraction of base speed where plane stalls." + warn)] 
    private float stallSpeedRatio = 0.4f;
    
    [SerializeField, Range(1f, 50f), Tooltip("Base torque applied when stalling." + warn)] 
    private float baseStallTorque = 10f;
    
    [SerializeField, Range(1f, 30f), Tooltip("How fast the plane aligns its velocity to its nose." + warn)] 
    private float steeringResponsiveness = 10f;
    
    [SerializeField, Range(0f, 100f), Tooltip("Initial speed when spawned to prevent instant stall.")] 
    private float startingSpeed = 30f;

    private float aerodynamicGrip;
    private float actualMaxSpeed;
    private float stallSpeedThreshold;
    private float verticalVelocity;

    private Vector3 currentWindVector;

    private float currentForwardSpeed = 0f;
    private bool isInWind = false;
    private bool isGrounded = false;
    private bool isWet = false;
    private bool isGameOver = false;

    [Header("Current State")]
    [SerializeField, Tooltip("Starting plane type")] private PlaneTypeEnum StartPlaneType = PlaneTypeEnum.Default;

    [SerializeField, Tooltip("List of plane types, has to contain starting plane")] private List<PaperPlaneType> planeTypes = new List<PaperPlaneType>();
    private Dictionary<PlaneTypeEnum, GameObject> planePool = new Dictionary<PlaneTypeEnum, GameObject>();
    private PaperPlaneType currentType;
    private GameObject activePrefab;
    private float pitch;
    private float roll;
    private float yaw;
    private int morph;

    private CinemachineCamera flightCam;
    private CinemachineCamera groundCam;

    [Header("Animation")]
    [SerializeField, Tooltip("Is the ransition animated")] private bool animated = true;
    [SerializeField, Tooltip("How much time to transform")] private float morphTime = 1f;
    private PlaneTypeEnum? pendingPlaneRequest = null;
    private PlaneFolder planeFolder;
    private float morphProgress = 0f;
    private float morphTarget;
    

    [Header("Weather Observation Settings")]
    [SerializeField, Tooltip("How many wind zones to track? If modified, Space Size changes!" + warn)] 
    private int maxWindZones = 10;
    
    [SerializeField, Tooltip("How many rain zones to track? If modified, Space Size changes!" + warn)] 
    private int maxRainZones = 10;  
    
    private int heuristicPlaneIndex = 0;
    private Vector3 previousActions = Vector3.zero;

    private Waypoint currentTarget;
    private Waypoint nextTarget;
    private float globalEpisodeStartTime;
    private float globalMaxDuration;
    private float checkpointStartTime;
    private float currentCheckpointDuration;

    private WeatherSystemManager weatherManager;
    private SceneGameManager levelController;

    [Header("Reward Tuning")]
    [SerializeField, Tooltip(warn)] private float rewardGoal = 5.0f; 
    [SerializeField, Tooltip(warn)] private float rewardWaypoint = 2f;
    [SerializeField, Tooltip(warn)] private float rewardDirection = 0.2f;
    [SerializeField, Tooltip(warn)] private float penaltyCollision = -5.0f; 


    private float bestDistanceToCurrentTarget;
    private float initialDistanceToCurrentTarget;

    [Header("Morph Fatigue System")]
    [SerializeField, Tooltip("How much overheat does changing the plane generate (to stop the agent from spamming)")] 
    private float overheatSpike = 1.0f;
    [SerializeField, Tooltip("How fast does it cool down (if its not cooled, >0 then the agent is penalized for changing)")] 
    private float cooldownRate = 0.2f;
    [SerializeField, Tooltip("Overheat maximum")]
    private float overheatLimit = 3f; 
    private float currentMorphOverheat = 0f;
    private bool overheated = false;
    

    [Space(10)]
    [Header("OBSERVATION PARAMETERS INFO (DO NOT EDIT)")]
    [SerializeField, TextArea(2, 3)] 
    private string observationSpaceCalculator = "Calculating...";
    const int WIND_OBS = 12; // ValidBit (1) + LocalCenter (3) + W/H/L (3) + LocalDir (3) + Mag (1) + Dist (1)
    const int RAIN_OBS = 8;  // ValidBit (1) + LocalCenter (3) + W/H/L (3) + Dist (1)
    const int PLANE_OBS = 30; // Physics (18) + Morph&Identity (12)
    const int WAYPOINT_OBS = 11; // LocalDirCurr (3) + DistCurr (1) + LocalDirNext (3) + DistNext (1) + GoalBit (1) + Checkpoint time (1)
    private float nextLogTime = 0f;

    //physics engine limitations
    const float OBS_MAX_SPEED = 300f;
    const float OBS_MAX_ROT = 200f;
    const float OBS_MAX_DIST = 2000f;
    const float SAFE_MULTIPLIER_CAP = 10f; 
    const float MAX_ZONE_SIZE = 1000f;  
    const float MAX_WIND_FORCE = 50f;   
    const float OBS_MAX_TIME = 100f;

    [Header("Debug & Logging")]
    [SerializeField, Tooltip("I/O terhelés (filewrite), élesben ne legyen bekapcsolva")]
    private bool enableDebugLogging = false;
    private string logFilePath;

#endregion

    private void OnValidate()
    {

        int windSpace = maxWindZones * WIND_OBS;
        int rainSpace = maxRainZones * RAIN_OBS;
        int basePlaneSpace = PLANE_OBS;
        int navigationSpace = WAYPOINT_OBS;
        int totalSpace = windSpace + rainSpace + basePlaneSpace + navigationSpace;

        observationSpaceCalculator = $"SET 'Space Size' IN BEHAVIOR PARAMETERS TO: {totalSpace}\n" +
                                     $"(Wind: {windSpace} | Rain: {rainSpace} | Plane: {basePlaneSpace} | Nav: {navigationSpace})";
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        ObserveRigidbodyState(sensor);     
        ObserveMorphAndIdentity(sensor);   
        ObserveEnvironment(sensor);         
    }

    private void ObserveRigidbodyState(VectorSensor sensor)
    {
        Vector3 localWind = transform.InverseTransformDirection(currentWindVector);

        sensor.AddObservation(transform.InverseTransformDirection(rb.linearVelocity) / OBS_MAX_SPEED);  // 3
        sensor.AddObservation(transform.InverseTransformDirection(rb.angularVelocity) / OBS_MAX_ROT);   // 3

        sensor.AddObservation(transform.forward);                                       // 3
        sensor.AddObservation(transform.up);                                            // 3

        sensor.AddObservation(currentForwardSpeed / OBS_MAX_SPEED);                     // 1
        sensor.AddObservation(aerodynamicGrip);                                         // 1

        sensor.AddObservation(isInWind ? 1f : 0f);                                      // 1
        sensor.AddObservation(Vector3.ClampMagnitude(localWind / MAX_WIND_FORCE, 1f));  // 3
    }
    private void ObserveMorphAndIdentity(VectorSensor sensor)
    {
        float curPlaneNormalized = GetNormalizedPlaneType(currentType.planeType);
        float pendingPlaneNormalized = pendingPlaneRequest.HasValue ? GetNormalizedPlaneType(pendingPlaneRequest.Value) : -1f;
        float overheatNormalized = Mathf.Clamp01(currentMorphOverheat / overheatLimit); 
        float overheatFloat = overheated? 1f : 0f;

        sensor.AddObservation(morphProgress);           // 1
        sensor.AddObservation(overheatNormalized);      // 1
        sensor.AddObservation(overheatFloat);           // 1
        sensor.AddObservation(curPlaneNormalized);      // 1
        sensor.AddObservation(pendingPlaneNormalized);  // 1

        sensor.AddObservation(Mathf.Clamp01(actualMaxSpeed / OBS_MAX_SPEED));                               // 1
        sensor.AddObservation(Mathf.Clamp01(stallSpeedThreshold / OBS_MAX_SPEED));                          // 1

        sensor.AddObservation(Mathf.Clamp01(currentType.SpeedMultiplier / SAFE_MULTIPLIER_CAP));            // 1
        sensor.AddObservation(Mathf.Clamp01(currentType.ResponsivenessMultiplier / SAFE_MULTIPLIER_CAP));   // 1

        sensor.AddObservation(previousActions);         // 3
    }
    private void ObserveEnvironment(VectorSensor sensor)
    {
        List<WindZone> winds = weatherManager.GetClosestWindZones(transform.position, maxWindZones);
        for (int i = 0; i < maxWindZones; i++)
        {
            if (i < winds.Count)
            {
                var zone = winds[i].getZoneParameters();
                
                sensor.AddObservation(1f); //VALID BIT: 1 = igazi zóna                           // 1

                Vector3 localCenter = transform.InverseTransformPoint(zone.center);
                sensor.AddObservation(Vector3.ClampMagnitude(localCenter / OBS_MAX_DIST, 1f));   // 3

                sensor.AddObservation(Mathf.Clamp01(zone.width / MAX_ZONE_SIZE));                // 1
                sensor.AddObservation(Mathf.Clamp01(zone.height / MAX_ZONE_SIZE));               // 1
                sensor.AddObservation(Mathf.Clamp01(zone.lenght / MAX_ZONE_SIZE));               // 1
                
                //wind inside zone
                sensor.AddObservation(transform.InverseTransformDirection(winds[i].Direction).normalized); // 3
                sensor.AddObservation(Mathf.Clamp01(winds[i].Direction.magnitude / MAX_WIND_FORCE));       // 1
                //distance to zone
                float dist = Vector3.Distance(transform.position, zone.center);
                sensor.AddObservation(Mathf.Clamp01(dist / OBS_MAX_DIST));                     // 1
            }
            else 
            { 
                for (int j = 0; j < WIND_OBS; j++) sensor.AddObservation(0f); //INVALID BIT (végig 0 ráadásul)
            } //padding
        }

        // Observe Closest Rain Zones
        List<RainZone> rains = weatherManager.GetClosestRainZones(transform.position, maxRainZones);
        for (int i = 0; i < maxRainZones; i++)
        {
            if (i < rains.Count)
            {
                var zone = rains[i].getZoneParameters();
                
                sensor.AddObservation(1f); // VALID BIT: 1 = igazi zóna                          // 1 

                Vector3 localCenter = transform.InverseTransformPoint(zone.center);
                sensor.AddObservation(Vector3.ClampMagnitude(localCenter / OBS_MAX_DIST, 1f));   // 3
                
                sensor.AddObservation(Mathf.Clamp01(zone.width / MAX_ZONE_SIZE));                // 1
                sensor.AddObservation(Mathf.Clamp01(zone.height / MAX_ZONE_SIZE));               // 1
                sensor.AddObservation(Mathf.Clamp01(zone.lenght / MAX_ZONE_SIZE));               // 1
                //distance to zone
                float dist = Vector3.Distance(transform.position, zone.center);
                sensor.AddObservation(Mathf.Clamp01(dist / OBS_MAX_DIST));                     // 1
            }
            else 
            { 
                for (int j = 0; j < RAIN_OBS; j++) sensor.AddObservation(0f); 
            } 
        }
        
        //observe waypoints
        Vector3 localDirToCurr = Vector3.zero;
        Vector3 localDirToNext = Vector3.zero;

        float distToCurr = 0f;
        float distToNext = 0f;

        float remainingTime = currentCheckpointDuration - (Time.fixedTime - checkpointStartTime);
        float observedTime = Mathf.Clamp01(remainingTime / OBS_MAX_TIME);
        float progressTime = Mathf.Clamp01((Time.fixedTime - checkpointStartTime) / currentCheckpointDuration);
        float goalBit = 0f;

        if (currentTarget != null)
        {
            localDirToCurr = transform.InverseTransformPoint(currentTarget.transform.position);
            distToCurr = Vector3.Distance(transform.position, currentTarget.transform.position);
            goalBit = currentTarget.isGoal ? 1f : 0f;
            
            if (nextTarget != null)
            {
                localDirToNext = transform.InverseTransformPoint(nextTarget.transform.position);
                distToNext = Vector3.Distance(transform.position, nextTarget.transform.position);
            }
        }


        sensor.AddObservation(Vector3.ClampMagnitude(localDirToCurr / OBS_MAX_DIST, 1f)); // 3
        sensor.AddObservation(Mathf.Clamp01(distToCurr / OBS_MAX_DIST));                  // 1 (távolság)
        sensor.AddObservation(Vector3.ClampMagnitude(localDirToNext / OBS_MAX_DIST, 1f)); // 3
        sensor.AddObservation(Mathf.Clamp01(distToNext / OBS_MAX_DIST));                  // 1 (távolság)
        sensor.AddObservation(observedTime);                                              // 1 (time)
        sensor.AddObservation(progressTime);                                              // 1 (progress of time)
        sensor.AddObservation(goalBit);                                                   // 1 (is it the last wp?)
    }


    void Update()
    {
        if (inputs.Plane.FoldDefault.WasPressedThisFrame()) heuristicPlaneIndex = 1;
        else if (inputs.Plane.FoldDart.WasPressedThisFrame()) heuristicPlaneIndex = 2;
        else if (inputs.Plane.FoldHunt.WasPressedThisFrame()) heuristicPlaneIndex = 3;
        else if (inputs.Plane.FoldBall.WasPressedThisFrame()) heuristicPlaneIndex = 4;
    }
    void FixedUpdate()
    {
        if(enableDebugLogging) LogDetailedObservations();
        if (isGameOver) return;
        
        if (!isGrounded && !isWet)
        {
            
            HandleMorphLogic(morph);

            if(currentMorphOverheat > 0)
            {
                currentMorphOverheat = Mathf.Max(0f, currentMorphOverheat - (cooldownRate * Time.fixedDeltaTime));
            }

            if (currentType.planeType != PlaneTypeEnum.PaperBall) 
            {
                ApplyFlightPhysics(pitch, roll, yaw);
            }
        }
        RewardDirectionalProgress();
        PaperBallSurviveCheck();
        TimeOutCheck();
    }
    
    public override void Initialize()
    {
        levelController = FindAnyObjectByType<SceneGameManager>();
        if(levelController is null)
        { 
            Debug.LogError("Unable to load agent env: "+this);
        } 
        else 
        {
            levelController.RegisterAgent(this);
            weatherManager = levelController.GetWeatherManager();
            globalMaxDuration = levelController.GetTimeLimit();
        }



        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.linearDamping = 0.05f; // Matched Controller
        rb.angularDamping = 1f;   // Matched Controller
        rb.mass = 1f;

        InitializePool();
        inputs = InputSystemManager.Instance.IO; 
        
        currentWindVector = defaultDrift;
        currentForwardSpeed = startingSpeed;


        string logDir = Application.dataPath + "/Logs";
        if (!System.IO.Directory.Exists(logDir))
        {
            System.IO.Directory.CreateDirectory(logDir);
        }
        logFilePath = $"{logDir}/agent_{gameObject.name}_{gameObject.GetInstanceID()}_log.txt";
        if (enableDebugLogging) 
        {
            System.IO.File.WriteAllText(logFilePath, $"=== NEW DEBUG SESSION INITIALIZED: {DateTime.Now} ===\n");
        }
    }
    private void InitializePool()
    {
        foreach(var pt in planeTypes)
        {
            GameObject go = Instantiate(pt.prefab, transform);
            go.SetActive(false);
            planePool[pt.planeType] = go;
        }
        currentType = planeTypes.FirstOrDefault(pt => pt.planeType == StartPlaneType);

        activePrefab = planePool[StartPlaneType];
        activePrefab.SetActive(true);

        planeFolder = new PlaneFolder(animated);
        planeFolder.Setup(activePrefab);
    }
    private void ResetPlane()
    {
        //state reset
        isGameOver = false;
        isGrounded = false;
        isWet = false;
        isInWind = false;
        previousActions = Vector3.zero;

        float safeBaseSpeed = Mathf.Min(baseMovementSpeed * currentType.SpeedMultiplier, OBS_MAX_SPEED); 
        float currentStallThreshold = safeBaseSpeed * stallSpeedRatio; 
        const float stallProtection = 1.2f;
        //dont stall on start
        currentForwardSpeed = Mathf.Max(startingSpeed, currentStallThreshold * stallProtection); //ha túl nagy
        currentForwardSpeed = Mathf.Min(currentForwardSpeed, OBS_MAX_SPEED); //ha túl kicsi
        currentWindVector = defaultDrift;

        //rb speed&rotation reset
        rb.linearVelocity = transform.forward * currentForwardSpeed;
        rb.angularVelocity = Vector3.zero;

        //morph Reset
        morphProgress = 0f;
        currentMorphOverheat = 0f;
        overheated = false;
        pendingPlaneRequest = null;

        pitch = 0f;
        roll = 0f;
        yaw = 0f;
        morph = 0;
        
        //visual / type Reset
        SwapPlaneType(StartPlaneType);
        planeFolder.SetProgress(0f);
        heuristicPlaneIndex = 0;

        if(enableDebugLogging) LogResetState();
    }
    public void SetupNewRoute(List<Waypoint> path, float episodeTimeLimit, WeatherSystemManager weatherManager, CinemachineCamera fCam, CinemachineCamera gCam)
    {
        this.globalMaxDuration = episodeTimeLimit;
        this.weatherManager = weatherManager;
        this.flightCam = fCam;
        this.groundCam = gCam;

        if (path == null || path.Count == 0) return;

        
        Waypoint spawnPoint = path[0];
        transform.position = spawnPoint.transform.position;


        this.currentTarget = spawnPoint.nextWaypoint;
        this.nextTarget = (this.currentTarget != null) ? this.currentTarget.nextWaypoint : null; 


        if (this.currentTarget != null)
        {
            Vector3 directionToTarget = (this.currentTarget.transform.position - transform.position).normalized;
            if (directionToTarget.sqrMagnitude > 0f)
            {
                transform.rotation = Quaternion.LookRotation(directionToTarget, Vector3.up);
            }
        }
        else
        {
            transform.rotation = spawnPoint.transform.rotation;
        }


        this.currentCheckpointDuration = spawnPoint.timeToNextWaypoint; 
        
        if (this.currentTarget != null)
        {
            this.bestDistanceToCurrentTarget = Vector3.Distance(transform.position, this.currentTarget.transform.position);
            this.initialDistanceToCurrentTarget = this.bestDistanceToCurrentTarget;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if(isGameOver) return;

        var continuousActionsOut = actionsOut.ContinuousActions;
        var discreteActionsOut = actionsOut.DiscreteActions;

        continuousActionsOut[0] = inputs.Plane.Pitch.ReadValue<float>(); 
        continuousActionsOut[1] = inputs.Plane.Yawn.ReadValue<float>();
        continuousActionsOut[2] = inputs.Plane.Roll.ReadValue<float>();   


        discreteActionsOut[0] = heuristicPlaneIndex;
        heuristicPlaneIndex = 0;

    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (isGameOver || isGrounded || isWet) return;

        pitch = actions.ContinuousActions[0];
        yaw   = actions.ContinuousActions[1];
        roll  = actions.ContinuousActions[2];

        morph = actions.DiscreteActions[0];

        previousActions = new Vector3(pitch, roll, yaw);
    }
    public override void OnEpisodeBegin()
    {
        levelController.RefreshAgentRoute(this);
        
        globalEpisodeStartTime = Time.fixedTime;
        checkpointStartTime = Time.fixedTime;

        ResetPlane();
    }

    private void ApplyFlightPhysics(float pitchInput, float rollInput, float yawInput){

        float deltaTime = Time.fixedDeltaTime;
        float response = currentType.ResponsivenessMultiplier;
        float actualRotSpeed = Mathf.Min(baseRotationSpeed * response, OBS_MAX_ROT); //rotation limit

        Vector3 rotInput = new Vector3(pitchInput, yawInput, -rollInput);
        rotInput = Vector3.ClampMagnitude(rotInput, 1f);

        Vector3 deltaRot = rotInput * actualRotSpeed * deltaTime;
        Quaternion deltaRotation = Quaternion.Euler(deltaRot);
        rb.MoveRotation(rb.rotation * deltaRotation);


        float planetypeBaseSpeed = baseMovementSpeed * currentType.SpeedMultiplier;
        float baseSpeed = Mathf.Min(planetypeBaseSpeed, OBS_MAX_SPEED); //speed limit

        //vertical rotation, -1 up (90°) / 1 down (-90°)
        float verticalAlignment = Vector3.Dot(transform.forward, Vector3.down);

        float speedMod;
        verticalVelocity = rb.linearVelocity.y;
        if (verticalAlignment > 0 && verticalVelocity < 0f)
        {
            speedMod = verticalAlignment * baseSpeed * diveInfluence;
        }
        else
        {
            speedMod = verticalAlignment * baseSpeed * climbPenalty;
        }

        //wind logic
        Vector3 effectiveWind = isInWind ? currentWindVector : defaultDrift;
        float windStrength = effectiveWind.magnitude;
        Vector3 windDir = effectiveWind.normalized;
        float windAlignment = Vector3.Dot(transform.forward, windDir);

        float windMultiplier = 1f + Mathf.Clamp(windAlignment * windStrength, minWindInfluence, maxWindInfluence);

        //final speed
        float targetSpeed = (baseSpeed + speedMod) * windMultiplier;
        
        float theoreticalMax = baseSpeed * accalerationCap;
        float hardMaxSpeed = Mathf.Min(theoreticalMax, OBS_MAX_SPEED); //speed limit aswel
        targetSpeed = Mathf.Clamp(targetSpeed, 0f, hardMaxSpeed);

        if (targetSpeed > currentForwardSpeed)
        {
            //curve --> smoother transition
            currentForwardSpeed = Mathf.Lerp(currentForwardSpeed, targetSpeed, speedMultiplier * deltaTime);
        }
        else
        {
            float dragBleedRate = (baseSpeed * 0.2f) * decelMultiplier * currentType.ResponsivenessMultiplier; 
            
            //wind drag
            if (isInWind && windAlignment < 0)
            {
                //divide 0 guard
                float safeMultiplier = Mathf.Max(currentType.SpeedMultiplier, 0.1f);
                float windDrag = (Mathf.Abs(windAlignment) * windStrength * 20f) / safeMultiplier;
                dragBleedRate += windDrag;
            }

            if (currentForwardSpeed > hardMaxSpeed) //speed limit
            {
                //smooth slowdown --> bleed value
                dragBleedRate *= 4f; 
            }

            currentForwardSpeed = Mathf.MoveTowards(
                currentForwardSpeed, 
                targetSpeed, 
                dragBleedRate * deltaTime
            );
        }


        actualMaxSpeed = baseSpeed; 
        stallSpeedThreshold = actualMaxSpeed * stallSpeedRatio; //stall limit
        
        if (currentForwardSpeed < stallSpeedThreshold)
        {
            float stallSeverity = 1f - (currentForwardSpeed / stallSpeedThreshold); 
            
            //divide 0 guard
            float safeResponse = Mathf.Max(currentType.ResponsivenessMultiplier, 0.1f);
            float stallTorque = baseStallTorque / safeResponse; 
            
            if (isInWind) //stall --> wind, blows it away
            {
                Vector3 activeWindDir = currentWindVector.normalized;
                Vector3 weathervaneTorque = Vector3.Cross(transform.forward, activeWindDir);
                
                rb.AddTorque(weathervaneTorque * stallSeverity * stallTorque, ForceMode.Acceleration);
                //AddReward(penaltyWindStallStep);
            }
            else //stall, no wind --> gravity, torque nose down
            {
                float orientationModifier = Mathf.Sign(transform.up.y); 
                rb.AddRelativeTorque(Vector3.right * stallSeverity * stallTorque * orientationModifier, ForceMode.Acceleration);
                //AddReward(penaltyStallStep);
            }
        }

        //airres
        aerodynamicGrip = Mathf.Clamp01(currentForwardSpeed / baseSpeed);
        Vector3 forwardVelocity = transform.forward * currentForwardSpeed;
        
        float currentYVelocity = rb.linearVelocity.y;

        //steering Lerp
        Vector3 newVelocity = Vector3.Lerp(rb.linearVelocity, forwardVelocity, aerodynamicGrip * deltaTime * steeringResponsiveness);

        //gravity or steering
        float sinkMultiplier = 1f - (liftEfficiency * aerodynamicGrip);
        
        //steering Y with gravity Y
        float blendedY = Mathf.Lerp(newVelocity.y, currentYVelocity, sinkMultiplier);

        newVelocity.y = Mathf.Min(newVelocity.y, blendedY);

        //wind Drift
        if (isInWind)
        {
            float safeMultiplier = Mathf.Max(currentType.SpeedMultiplier, 0.1f);
            float driftSusceptibility = 5f / safeMultiplier;
            newVelocity += currentWindVector * deltaTime * driftSusceptibility;
        }

        //max speed is max speed
        newVelocity = Vector3.ClampMagnitude(newVelocity, OBS_MAX_SPEED);
        
        rb.linearVelocity = newVelocity;
    }   
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<WindZone>(out var wind)) {

            isInWind = true;
            currentWindVector = wind.Direction;

        } else if (other.TryGetComponent<Waypoint>(out var wp)) {

            if (wp == currentTarget) {

                float timeElapsed = Time.fixedTime - checkpointStartTime;
                float quicknessRatio = 1f - Mathf.Clamp01(timeElapsed / currentCheckpointDuration);
                float speedAdjustedReward = rewardWaypoint * (0.75f + 0.25f * quicknessRatio); // 50% biztosított, maradék a gyorsaságánmúlik


                AddReward(speedAdjustedReward); 
                wp.gameObject.SetActive(false);
                
                if (wp.isGoal) {
                    GameOver(true);
                } else {
                    currentTarget = wp.nextWaypoint;
                    nextTarget = (currentTarget != null) ? currentTarget.nextWaypoint : null;

                    checkpointStartTime = Time.fixedTime;
                    currentCheckpointDuration= wp.timeToNextWaypoint;

                    if (currentTarget != null) 
                    {
                        bestDistanceToCurrentTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
                        initialDistanceToCurrentTarget = bestDistanceToCurrentTarget; 
                    }
                }
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<WindZone>(out var wind)) {
            isInWind = false;
            currentWindVector = defaultDrift;
        }
        
    }
    private void OnCollisionEnter(Collision collision)
    {
        GameObject go = collision.gameObject;
        if (go.CompareTag("Ground"))
        {
            HitGround();
        } else if (go.CompareTag("Water"))
        {
            HitWater();
        }
    }
    private void HitGround()
    {
        if(!isGrounded) AddReward(penaltyCollision);
        else return;

        isGrounded = true;
        SwapPlaneType(PlaneTypeEnum.PaperBall);
        pendingPlaneRequest = null;
        morphProgress = 0f;
        
    }
    private void HitWater()
    {
        if(isGameOver) return; //threadsafe, dont spam it

        if(!isWet) AddReward(penaltyCollision);
        else return;

        isWet = true;
        SwapPlaneType(PlaneTypeEnum.PaperBall);
        GameOver(false);
    }
    private void GameOver(bool won)
    {
        if(isGameOver)return;
        isGameOver = true;

        if(won) AddReward(rewardGoal);
        
        OnGameOver?.Invoke(won);

        EndEpisode();
    }

    private void SwapPlaneType(PlaneTypeEnum newType)
    {
        currentType = planeTypes.FirstOrDefault(pt => pt.planeType == newType);
        if(currentType == null) {
            return;
        }
        else
        {
            activePrefab.SetActive(false);
            activePrefab = planePool[newType];

            activePrefab.SetActive(true);
            planeFolder.Setup(activePrefab);

            planeFolder.SetProgress(morphProgress);
        }

    }
    private void HandleMorphLogic(int requestedMorph)
    {
        overheated = currentMorphOverheat >= overheatLimit;

        if (requestedMorph != 0 && !overheated) 
        {
            PlaneTypeEnum? targetType = ParsePlaneIndex(requestedMorph);

            if (targetType != null && targetType != currentType.planeType)
            {
                if (pendingPlaneRequest != targetType)
                {
                    pendingPlaneRequest = targetType;
                    currentMorphOverheat += overheatSpike;

                    if (currentMorphOverheat >= overheatLimit)
                    {
                        overheated = true;
                    }
                }
            }
        }


        if (pendingPlaneRequest != null) morphTarget = 1f; else morphTarget = 0f;

        morphProgress = Mathf.MoveTowards(morphProgress, morphTarget, Time.fixedDeltaTime * morphTime);
        
        planeFolder.SetProgress(morphProgress);


        if (morphProgress >= 1f && pendingPlaneRequest != null) 
        {
            SwapPlaneType(pendingPlaneRequest.Value);
            pendingPlaneRequest = null;
        }
    }
    private PlaneTypeEnum? ParsePlaneIndex(int i) => i switch
    {
        1 => PlaneTypeEnum.Default,
        2 => PlaneTypeEnum.Dart,
        3 => PlaneTypeEnum.HuntingFlight,
        4 => PlaneTypeEnum.PaperBall,
        _ => null
    };
    private int ParsePlane(PlaneTypeEnum p) => p switch
    {
        PlaneTypeEnum.Default => 1,
        PlaneTypeEnum.Dart => 2,
        PlaneTypeEnum.HuntingFlight => 3,
        PlaneTypeEnum.PaperBall => 4,
        _ => 1
    };
    private float GetNormalizedPlaneType(PlaneTypeEnum? type)
    {
        int index = type switch
        {
            PlaneTypeEnum.Default => 1,
            PlaneTypeEnum.Dart => 2,
            PlaneTypeEnum.HuntingFlight => 3,
            PlaneTypeEnum.PaperBall => 4,
            _ => 0
        };

        //normális vagy?
        return index / 4f;
    }
    
    private void PaperBallSurviveCheck()
    {
        if (!isGrounded || isGameOver || isWet || currentType.planeType != PlaneTypeEnum.PaperBall) return;

        if (rb.linearVelocity.sqrMagnitude < 0.01f && rb.angularVelocity.sqrMagnitude < 0.01f)
        {
            GameOver(false);
        }

    }
    private void TimeOutCheck()
    {
        if(isGameOver)return;

        float currentTime = Time.fixedTime;

        if (currentTime - checkpointStartTime > currentCheckpointDuration)
        {
            GameOver(false);
        }

        if(currentTime - globalEpisodeStartTime > globalMaxDuration)
        {
            GameOver(false);
        }
    }
    private void RewardDirectionalProgress()
    {
        if (isGameOver || isWet || isGrounded || currentTarget == null) return;
        if (initialDistanceToCurrentTarget <= 0.001f) return; 

        float currentDistance = Vector3.Distance(transform.position, currentTarget.transform.position);
        
        if (currentDistance < bestDistanceToCurrentTarget)
        {
            float distanceDelta = bestDistanceToCurrentTarget - currentDistance;
            float rewardSlice = (distanceDelta / initialDistanceToCurrentTarget) * rewardDirection;


            if (currentType.planeType != PlaneTypeEnum.PaperBall)
            {
                //goal felé nézzen, ne mellé
                Vector3 dirToTarget = (currentTarget.transform.position - transform.position).normalized;
                float alignment = Vector3.Dot(transform.forward, dirToTarget);
                if (alignment > 0.7f)
                {
                    rewardSlice = rewardSlice * alignment;
                } 
                else
                {
                    rewardSlice = 0;
                }                
            }

            AddReward(rewardSlice); 
            bestDistanceToCurrentTarget = currentDistance;
        }
    }
    
    private void LogDetailedObservations()
    {
        if (!enableDebugLogging || Time.fixedTime < nextLogTime) return;

        nextLogTime = Time.fixedTime + 2f;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine($"=== DUAL-STATE OBSERVATION AUDIT: {Time.fixedTime:F2} ===");

        //WORLD STATE
        sb.AppendLine("[WORLD_STATE (RAW)]");
        sb.AppendLine($"  - Position: {transform.position}");
        sb.AppendLine($"  - Rotation: {transform.eulerAngles}");
        sb.AppendLine($"  - Forward : {transform.forward}");
        sb.AppendLine($"  - Up      : {transform.up}");

        //PHYSICS (raw / observed)
        Vector3 rawLinVel = rb.linearVelocity;
        Vector3 obsLinVel = transform.InverseTransformDirection(rawLinVel) / OBS_MAX_SPEED;

        Vector3 rawAngVel = rb.angularVelocity;
        Vector3 obsAngVel = transform.InverseTransformDirection(rawAngVel) / OBS_MAX_ROT;

        Vector3 rawWind = currentWindVector;
        Vector3 obsWind = Vector3.ClampMagnitude(transform.InverseTransformDirection(rawWind) / MAX_WIND_FORCE, 1f);

        sb.AppendLine("[PHYSICS]");
        sb.AppendLine($"  - LinVel  -> RAW (World): {rawLinVel} | OBS (Local/Scaled): {obsLinVel}");
        sb.AppendLine($"  - AngVel  -> RAW (World): {rawAngVel} | OBS (Local/Scaled): {obsAngVel}");
        sb.AppendLine($"  - FwdSpd  -> RAW: {currentForwardSpeed:F2} | OBS: {(currentForwardSpeed / OBS_MAX_SPEED):F3}");
        sb.AppendLine($"  - WindVec -> RAW (World): {rawWind} | OBS (Local/Scaled): {obsWind}");
        sb.AppendLine($"  - State   -> Grip: {aerodynamicGrip:F3} | InWind: {(isInWind ? 1 : 0)}");

        //MORPH & METADATA (raw / observed)
        float curPlaneNormalized = GetNormalizedPlaneType(currentType.planeType);
        float pendingPlaneNormalized = pendingPlaneRequest.HasValue ? GetNormalizedPlaneType(pendingPlaneRequest.Value) : -1f;

        float rawLimSpd = actualMaxSpeed;
        float obsLimSpd = Mathf.Clamp01(actualMaxSpeed / OBS_MAX_SPEED);
        float rawLimStall = stallSpeedThreshold;
        float obsLimStall = Mathf.Clamp01(stallSpeedThreshold / OBS_MAX_SPEED);

        float rawMultSpd = currentType.SpeedMultiplier;
        float obsMultSpd = Mathf.Clamp01(rawMultSpd / SAFE_MULTIPLIER_CAP);
        float rawMultResp = currentType.ResponsivenessMultiplier;
        float obsMultResp = Mathf.Clamp01(rawMultResp / SAFE_MULTIPLIER_CAP);
        
        sb.AppendLine("[META]");
        sb.AppendLine($"  - Morph   -> Prog: {morphProgress:F2} | CurNorm: {curPlaneNormalized:F2} | PendNorm: {pendingPlaneNormalized:F2}");
        sb.AppendLine($"  - LimSpd  -> RAW: {rawLimSpd:F2} | OBS: {obsLimSpd:F3}");
        sb.AppendLine($"  - LimStall-> RAW: {rawLimStall:F2} | OBS: {obsLimStall:F3}");
        sb.AppendLine($"  - MultSpd -> RAW: {rawMultSpd:F2} | OBS: {obsMultSpd:F3}");
        sb.AppendLine($"  - MultResp-> RAW: {rawMultResp:F2} | OBS: {obsMultResp:F3}");
        sb.AppendLine($"  - PrevAct -> RAW: {previousActions}");

        //ENVIRONMENT & NAV (raw / observed)
        sb.AppendLine("[ENV_NAV]");
        
        if (currentTarget != null) 
        {
            Vector3 targetPos = currentTarget.transform.position;
            Vector3 rawDirToCurr = targetPos - transform.position;
            Vector3 localDirToCurr = transform.InverseTransformPoint(targetPos);
            Vector3 obsDirToCurr = Vector3.ClampMagnitude(localDirToCurr / OBS_MAX_DIST, 1f);

            float rawDistToCurr = Vector3.Distance(transform.position, targetPos);
            float obsDistToCurr = Mathf.Clamp01(rawDistToCurr / OBS_MAX_DIST);
            
            float rawTime = currentCheckpointDuration - (Time.fixedTime - checkpointStartTime);
            float obsTime = Mathf.Clamp01(rawTime / OBS_MAX_TIME);

            sb.AppendLine($"  - Target  -> RAW Position: {targetPos}");
            sb.AppendLine($"  - DirCurr -> RAW (WorldDir): {rawDirToCurr} | OBS (Local/Scaled): {obsDirToCurr}");
            sb.AppendLine($"  - DistCurr-> RAW: {rawDistToCurr:F1}m | OBS: {obsDistToCurr:F3} | IsGoal: {(currentTarget.isGoal ? 1 : 0)}");
            sb.AppendLine($"  - TimeRem -> RAW: {rawTime:F2}s | OBS: {obsTime:F3}");
        }

        var winds = weatherManager.GetClosestWindZones(transform.position, 1);
        if (winds.Count > 0) 
        {
            Vector3 rawCenter = winds[0].getZoneParameters().center;
            Vector3 localCenter = transform.InverseTransformPoint(rawCenter);
            Vector3 obsCenter = Vector3.ClampMagnitude(localCenter / OBS_MAX_DIST, 1f);
            sb.AppendLine($"  - Wind0Ctr-> RAW (WorldPos): {rawCenter} | OBS (Local/Scaled): {obsCenter} | IsValid: 1");
        }

        sb.AppendLine("------------------------------------------\n");

        try 
        {
            System.IO.File.AppendAllText(logFilePath, sb.ToString());
        } 
        catch (System.Exception e) 
        {
            Debug.LogWarning("Logolási hiba: " + e.Message);
        }
    }
    private void LogResetState()
    {
        if (!enableDebugLogging) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine($"\n=====================================================================");
        sb.AppendLine($"=== PLANE RESET / INITIALIZATION AUDIT AT FIXED_TIME: {Time.fixedTime:F2} ===");
        sb.AppendLine($"=====================================================================");
        
        sb.AppendLine($"[CONFIG] Plane Type: {currentType.planeType} | StartSpd (Requested): {startingSpeed}");
        sb.AppendLine($"[MULTS] SpeedMult: {currentType.SpeedMultiplier:F2} | RespMult: {currentType.ResponsivenessMultiplier:F2}");
        
        float safeBaseSpeed = Mathf.Min(baseMovementSpeed * currentType.SpeedMultiplier, OBS_MAX_SPEED); 
        float currentStallThreshold = safeBaseSpeed * stallSpeedRatio; 
        
        sb.AppendLine($"[LIMITS] SafeBaseSpd: {safeBaseSpeed:F2} | StallThreshold: {currentStallThreshold:F2} | AxiomMax: {OBS_MAX_SPEED}");
        
        sb.AppendLine($"[RAW_WORLD_SPAWN]");
        sb.AppendLine($"  - Position: {transform.position}");
        sb.AppendLine($"  - Rotation (Euler): {transform.eulerAngles}");
        sb.AppendLine($"  - ForwardVec: {transform.forward} | UpVec: {transform.up}");

        sb.AppendLine($"[PHYSICS] Spawn FwdSpd: {currentForwardSpeed:F2} | Spawn Rigidbody Vel: {rb.linearVelocity}");
        sb.AppendLine($"[NAV] Target: {(currentTarget != null ? currentTarget.name : "NULL")} | Checkpoint Time Allowed: {currentCheckpointDuration:F2}s");
        sb.AppendLine($"=====================================================================\n");

        try { System.IO.File.AppendAllText(logFilePath, sb.ToString()); } 
        catch (System.Exception e) { Debug.LogWarning("Logolási hiba: " + e.Message); }
    }
}