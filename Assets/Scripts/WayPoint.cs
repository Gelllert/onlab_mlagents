using UnityEngine;


public class Waypoint : MonoBehaviour
{


        public Waypoint nextWaypoint;
        public float timeToNextWaypoint;
        public bool isGoal;
        
        [Header("Visuals")]
        [SerializeField]
        private MeshRenderer checkpointVisual;
        [SerializeField]
        private MeshRenderer goalVisual;

        [Header("Training Randomization")]
        [SerializeField, Tooltip("Radius of randomized position (sphere)")] 
        private float randomizationRadius = 15f;
    
        private Vector3 originalPosition;
        private bool isOriginalPositionSet = false;
        private bool isRandomized = false;

        private void OnValidate()
        {
            UpdateVisuals(); //inspector visuals
        }

        private void Start()
        {
            UpdateVisuals(); //live visuals
        }

        private void UpdateVisuals()
        {
            if (goalVisual != null && checkpointVisual != null)
            {
                goalVisual.gameObject.SetActive(isGoal);
                checkpointVisual.gameObject.SetActive(!isGoal);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f); 
            Vector3 center = Application.isPlaying && isOriginalPositionSet ? originalPosition : transform.position;

            if (isRandomized)
            {
                Gizmos.DrawWireSphere(center, randomizationRadius);
            }

            if (nextWaypoint != null)
            {
                Gizmos.color = isGoal ? Color.red : Color.cyan;
                Gizmos.DrawLine(transform.position, nextWaypoint.transform.position);
            }
        }

        public void SetRandomizedMode(bool isRandom)
        {
            isRandomized = isRandom;
        }
        public void SaveOriginalPosition()
        {
            if (!isOriginalPositionSet)
            {
                originalPosition = transform.position;
                isOriginalPositionSet = true;
            }
        }

        public void RandomizePosition()
        {
            if (!isRandomized) return;
            SaveOriginalPosition();
            transform.position = originalPosition + Random.insideUnitSphere * randomizationRadius;
        }
}
