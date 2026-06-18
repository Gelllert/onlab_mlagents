using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class InspectorCameraManager : MonoBehaviour
{
    [SerializeField] CinemachineCamera spectatorCamera;
    [SerializeField] List<GameObject> spectatedAgents;
    private InputSystem_Actions io;
    private int agentIndex;

    void Start()
    {
        agentIndex = 0;
        io = InputSystemManager.Instance.IO;
        if(spectatedAgents.Count > 0) setTarget(agentIndex);
    }

    void Update()
    {
        if (io.Plane.Select.WasPressedThisFrame())
        {
            agentIndex = (agentIndex + 1) % spectatedAgents.Count; 
        } 
        else if (io.Plane.Select2.WasPressedThisFrame())
        {   
            agentIndex--;
            if(agentIndex < 0) agentIndex = spectatedAgents.Count - 1; 
        } else 
        {
            return;
        }
        setTarget(agentIndex);

    }

    private void setTarget(int i)
    {
        if(i < 0 || spectatedAgents.Count-1 < i) return;

        Transform newTarget = spectatedAgents[i].transform;

        spectatorCamera.Follow = newTarget;
        spectatorCamera.LookAt = newTarget;
    }
}
