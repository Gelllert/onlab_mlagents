using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;



[RequireComponent(typeof(Rigidbody))]

public class PaperPlaneController : MonoBehaviour
{
    /// <summary>
    /// Calls when game ends, the bool represents win or lose
    /// </summary>
    public event Action<bool> OnGameOver;
    private InputSystem_Actions inputs;
    private Rigidbody rb;

[Header("Flight Settings")]
    [SerializeField, Range(50f, 150f), Tooltip("Rolling speed of the plane")] 
    private float baseRotationSpeed = 100f;
    
    [SerializeField, Range(30f, 60f), Tooltip("Speed multiplier")] 
    private float baseMovementSpeed = 40f;
    
    [SerializeField, Range(0.1f, 3f), Tooltip("Applies in wind or during diving")] 
    private float speedMultiplier = 1f;
    
    [SerializeField, Range(0f, 1f), Tooltip("How much altitude can it hold")] 
    private float liftEfficiency = 0.8f;
    
    [SerializeField, Tooltip("Acceleration in 0 speed wind")] 
    private Vector3 defaultDrift = new Vector3(0, -0.2f, 0.0f);
    
    [SerializeField, Range(1f, 5f), Tooltip("Cap multiplier for max speed")] 
    private float accalerationCap = 3f;

    [Header("Advanced Aerodynamics")]
    [SerializeField, Range(0f, 2f), Tooltip("How much speed is gained when diving")] 
    private float diveInfluence = 0.9f;
    
    [SerializeField, Range(0f, 3f), Tooltip("How harsh it loses speed when climbing")] 
    private float climbPenalty = 1.2f;

    [SerializeField, Range(-1.5f, 0f), Tooltip("Minimum wind multiplier limit")] 
    private float minWindInfluence = -1.0f;

    [SerializeField, Range(0f, 3f), Tooltip("Maximum wind multiplier limit")] 
    private float maxWindInfluence = 2.0f;

    [SerializeField, Range(1f, 2f), Tooltip("Multiplier for deceleration compared to acceleration")] 
    private float decelMultiplier = 1.0f;

    [SerializeField, Range(0.01f, 0.99f), Tooltip("Fraction of base speed where plane stalls")] 
    private float stallSpeedRatio = 0.4f;
    
    [SerializeField, Range(1f, 50f), Tooltip("Base torque applied when stalling")] 
    private float baseStallTorque = 10f;
    
    [SerializeField, Range(1f, 30f), Tooltip("How fast the plane aligns its velocity to its nose")] 
    private float steeringResponsiveness = 10f;
    [SerializeField, Range(0f, 100f), Tooltip("Initial speed when spawned to prevent instant stall")] 
    private float startingSpeed = 30f;
    


    private Vector3 currentWindVector;
    private float currentForwardSpeed = 0f;
    private bool isInWind = false;
    private bool isGrounded = false;
    private bool isGameOver = false;

    [Header("Current State")]
    [SerializeField, Tooltip("Starting plane type")] private PlaneTypeEnum StartPlaneType = PlaneTypeEnum.Default;

    [SerializeField, Tooltip("List of plane types, has to contain starting plane")] private List<PaperPlaneType> planeTypes = new List<PaperPlaneType>();
    private Dictionary<PlaneTypeEnum, GameObject> planePool = new Dictionary<PlaneTypeEnum, GameObject>();
    private PaperPlaneType currentType;
    private GameObject activePrefab;

    [Header("Cameras")]
    [Tooltip("Virtual camera used during flight")]
    [SerializeField] private CinemachineCamera flightCam;
    [Tooltip("Virtual camera used when rolldown on ground")]
    [SerializeField] private CinemachineCamera groundCam;


    [Header("Animation")]
    [SerializeField, Tooltip("Is the ransition animated")] private bool animated = true;
    [SerializeField, Tooltip("How much time to transfor")] private float morphTime = 1f;
    private PlaneTypeEnum? pendingPlaneRequest = null;
    private PlaneFolder planeFolder;
    private float morphProgress = 0f;
    private bool isForcedToBall = false;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputs = InputSystemManager.Instance.IO;

        rb.useGravity = true;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 1f;
        rb.mass = 1f;

        InitializePool();
        currentWindVector = defaultDrift;
        currentForwardSpeed = startingSpeed; 
    }

    private void OnEnable() => inputs.Plane.Enable();
    private void OnDisable() => inputs.Plane.Disable();

    void Update()
    {

        float pitchInput = inputs.Plane.Pitch.ReadValue<float>(); // W = 1, S = -1
        float rollInput = inputs.Plane.Roll.ReadValue<float>();   // D = 1, A = -1
        float yawnInput = inputs.Plane.Yawn.ReadValue<float>();   // E = 1, Q = -1

        float dt = Time.deltaTime;
        if (currentType.planeType != PlaneTypeEnum.PaperBall) ApplyFlightPhysics(pitchInput, rollInput, yawnInput, dt);
        HandleMorphLogic(dt);
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
    private void ApplyFlightPhysics(float pitchInput, float rollInput, float yawnInput, float deltaTime)
    {
        float response = currentType.ResponsivenessMultiplier;

        Vector3 rotInput = new Vector3(pitchInput, yawnInput, -rollInput);
        rotInput = Vector3.ClampMagnitude(rotInput, 1f);

        Vector3 deltaRot = rotInput * baseRotationSpeed * response * deltaTime;
        Quaternion deltaRotation = Quaternion.Euler(deltaRot);
        rb.MoveRotation(rb.rotation * deltaRotation);

        float baseSpeed = baseMovementSpeed * currentType.SpeedMultiplier;

        //unclamped Vertical Factor (-1 = straight up, 1 = straight down)
        float verticalAlignment = Vector3.Dot(transform.forward, Vector3.down);

        float speedMod;
        float verticalVelocity = rb.linearVelocity.y;
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

        float targetSpeed = (baseSpeed + speedMod) * windMultiplier;
        float maxSpeed = baseSpeed * accalerationCap;
        targetSpeed = Mathf.Clamp(targetSpeed, 0f, maxSpeed);

        if (targetSpeed > currentForwardSpeed)
        {
            //Accelerating: Smoothly curve up to the target speed
            currentForwardSpeed = Mathf.Lerp(currentForwardSpeed, targetSpeed, speedMultiplier * deltaTime);
        }
        else
        {
            float dragBleedRate = (baseSpeed * 0.2f) * decelMultiplier * currentType.ResponsivenessMultiplier; 
            
            if (isInWind && windAlignment < 0)
            {
                float windDrag = (Mathf.Abs(windAlignment) * windStrength * 20f) / currentType.SpeedMultiplier;
                dragBleedRate += windDrag;
            }

            if (currentForwardSpeed > maxSpeed)
            {
                dragBleedRate *= 4f; 
            }

            currentForwardSpeed = Mathf.MoveTowards(
                currentForwardSpeed, 
                targetSpeed, 
                dragBleedRate * deltaTime
            );
        }

        //stall 
        float actualMaxSpeed = baseMovementSpeed * currentType.SpeedMultiplier;
        float stallSpeedThreshold = actualMaxSpeed * stallSpeedRatio; // Stalls if speed is below threshold
        if (currentForwardSpeed < stallSpeedThreshold)
        {
            float stallSeverity = 1f - (currentForwardSpeed / stallSpeedThreshold); 
            float stallTorque = baseStallTorque / currentType.ResponsivenessMultiplier; 
            if (isInWind) //stall = wind takes over
            {
                Vector3 activeWindDir = currentWindVector.normalized;
                Vector3 weathervaneTorque = Vector3.Cross(transform.forward, activeWindDir);
                
                rb.AddTorque(weathervaneTorque * stallSeverity * stallTorque, ForceMode.Acceleration);
            }
            else //stall = torque nose down
            {
                float orientationModifier = Mathf.Sign(transform.up.y); 
                rb.AddRelativeTorque(Vector3.right * stallSeverity * stallTorque * orientationModifier, ForceMode.Acceleration);
            }
        }

        //aerodynamic grip 
        //if grip is 1, it flies straight / if grip is 0, it falls like a rock.
        float aerodynamicGrip = Mathf.Clamp01(currentForwardSpeed / baseSpeed);
        Vector3 forwardVelocity = transform.forward * currentForwardSpeed;
        
        float currentYVelocity = rb.linearVelocity.y;

        // Steering Lerp
        Vector3 newVelocity = Vector3.Lerp(rb.linearVelocity, forwardVelocity, aerodynamicGrip * deltaTime * steeringResponsiveness);

        // Calculate how much gravity should overpower the steering
        float sinkMultiplier = 1f - (liftEfficiency * aerodynamicGrip);
        
        // Blend steering Y with gravity Y
        float blendedY = Mathf.Lerp(newVelocity.y, currentYVelocity, sinkMultiplier);

        // ANTI-EXPLOIT: If our nose-dive (newVelocity.y) is dropping FASTER than gravity (blendedY),
        // we keep the nose-dive speed. We only use the blend if it forces the plane to sink more.
        newVelocity.y = Mathf.Min(newVelocity.y, blendedY);

        rb.linearVelocity = newVelocity;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<WindZone>(out var wind)) {
            isInWind = true;
            currentWindVector = wind.Direction;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<WindZone>() != null) {
            isInWind = false;
            currentWindVector = defaultDrift;
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Collided with {collision.gameObject.name}");

        GameObject go = collision.gameObject;
        if (go.CompareTag("Goal"))
        {
            Debug.Log("GAME WON");
            GameOver(true);
        }
        else if (go.CompareTag("Ground"))
        {
            HitGround();
            Debug.Log("Hit the ground!");

        } else if (go.CompareTag("Water"))
        {
            HitWater();
            Debug.Log("Got wet!");
        }
    }
    private void HitGround()
    {
        isGrounded = true;
        ActivateGroundCam();
        SwapPlaneType(PlaneTypeEnum.PaperBall);
    }
    private void HitWater()
    {
        if(isGameOver) return; //threadsafe, dont spam it
        GameOver(false);
        HitGround();
    }
    private void GameOver(bool won)
    {
        isGameOver = true;
        OnGameOver?.Invoke(won);

        var debug = won? "WINNER":"LOSER";
        Debug.Log($"GAME OVER - {debug}");
    }
    public bool IsGrounded()
    {
        return isGrounded;
    }

    public PlaneTypeEnum GetCurrentPlaneType()
    {
        return currentType.planeType;
    }
    public void RequestPlaneChange(PlaneTypeEnum newType, bool isFinal = false)
    {
        if (currentType.planeType == newType && !isFinal) return;

        pendingPlaneRequest = newType;
        isForcedToBall = isFinal; 
    }
    private void SwapPlaneType(PlaneTypeEnum newType)
    {
        currentType = planeTypes.FirstOrDefault(pt => pt.planeType == newType);
        if(currentType == null) {

            Debug.LogError($"Plane type {newType} not found in planeTypes list!");
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
    private void HandleMorphLogic(float deltaTime)
    {
        bool spaceHeld = inputs.Plane.Fold.IsPressed();
        float targetProgress = (spaceHeld || isForcedToBall) ? 1f : 0f;

        morphProgress = Mathf.MoveTowards(morphProgress, targetProgress, deltaTime * morphTime);
        planeFolder.SetProgress(morphProgress);


        if (morphProgress >= 1f) 
        {
            if (pendingPlaneRequest != null)
            {
                SwapPlaneType(pendingPlaneRequest.Value);
                pendingPlaneRequest = null;
                isForcedToBall = false; 
            }
            else if (spaceHeld && currentType.planeType != PlaneTypeEnum.PaperBall)
            {
                SwapPlaneType(PlaneTypeEnum.PaperBall);
            }
        }
    }

    private void ActivateGroundCam()
    {
        if (flightCam != null && groundCam != null)
        {
            flightCam.Priority = 0;
            groundCam.Priority = 10;
        }
    }
    void OnGUI()
    {
        if (rb == null) return;

        // Physical total speed (includes falling)
        float totalSpeed = rb.linearVelocity.magnitude;
        
        // Physical vertical speed (sink rate)
        float sinkRate = rb.linearVelocity.y;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 22;
        style.normal.textColor = Color.white;

        GUI.Label(
            new Rect(20, 20, 500, 100),
            $"Physics Speed (Mag): {totalSpeed:F1} m/s\n" +
            $"Vertical Speed (Y): {sinkRate:F1} m/s\n" +
            $"Aero Airspeed (Forward): {currentForwardSpeed:F1} m/s",
            style
        );
    }
}