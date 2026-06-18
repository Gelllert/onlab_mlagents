using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class RainTunnel : MonoBehaviour
{
    private List<GameObject> raindrops = new List<GameObject>();
    private float startTimeOffSet; // offset for spawning raindrops, so they don't all spawn at the same time
    private float tunnelHeight; // where the raindrop should be destroyed 
    private float distanceUntilNextRaindrop = 0f; // distance until the next raindrop should be spawned
    private float raindropSpeed; // speed at which raindrops fall
    private float modularityDistance; // spawn distance between raindrops based on tunnel height and count
    private float jitter; // small amount of randomized position 

    public void Init(GameObject raindropPrefab, float startTimeOffSet, float tunnelHeight, float raindropSpeed, int modularity, float jitter)
    {
        this.startTimeOffSet = startTimeOffSet;
        this.tunnelHeight = tunnelHeight;
        this.raindropSpeed = raindropSpeed;
        this.modularityDistance = tunnelHeight / modularity; // spawn interval based on tunnel height and count
        this.jitter = jitter;

        for(int i = 0; i < modularity; i++){

            Vector3 jitteredSpawn = new Vector3(Random.Range(0f, jitter), 0f, Random.Range(0f, jitter));

            var raindrop = Instantiate(raindropPrefab, transform);
            raindrop.transform.localPosition = jitteredSpawn;
            raindrop.SetActive(false);
            raindrops.Add(raindrop);
        } 
    }

    public void UpdateTunnel(float dt)
    {
        if(startTimeOffSet > 0f)
        {
            startTimeOffSet -= dt;
            return;
        }

        for (int i = 0; i < raindrops.Count; i++)
        {
            var drop = raindrops[i];
            if (drop == null || !drop.activeSelf) continue;
            var lp = drop.transform.localPosition;
            lp += Vector3.down * raindropSpeed * dt;
            drop.transform.localPosition = lp;
        }
        
        distanceUntilNextRaindrop -= raindropSpeed * dt;
        if(distanceUntilNextRaindrop <= 0f)
        {
            SpawnRaindrop();
            distanceUntilNextRaindrop = modularityDistance; 
        }

        raindrops.ForEach(drop => 
        {
            if(drop.transform.localPosition.y <= -tunnelHeight){
                drop.SetActive(false);

                Vector3 jitteredSpawn = new Vector3(Random.Range(0f, jitter), 0f, Random.Range(0f, jitter));
                drop.transform.localPosition = jitteredSpawn;
            }
        });

    }

    private void SpawnRaindrop()
    {
        GameObject raindrop = raindrops.FirstOrDefault((drop)=>{return !drop.activeSelf;});
        if(raindrop != null){
            raindrop.SetActive(true);
        }
    }
}
