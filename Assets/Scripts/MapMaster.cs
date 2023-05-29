using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapMaster : MonoBehaviour 
{    
    private float worldSize;
    private float time = 0f;

    void Start() 
    {
        BuildMap();
    }

    void Update() 
    {
        if (Master.Instance.runningSimulation) 
        {
            // Periodically Spawn Food
            time += Time.deltaTime / Master.Instance.foodSpawnRate;
            if (time >= 1f) 
            {
                time = 0f;
                Master.Instance.foodCount++;
                Instantiate(
                    Master.Instance.foodPrefab, 
                    new Vector3(Random.Range(-worldSize/2f+1, worldSize/2f-1), 
                    Random.Range(-worldSize/2f+1, worldSize/2f-1), 0f), 
                    Quaternion.identity, 
                    gameObject.transform);
            }
        }
    }

    void BuildMap() 
    {
        // Important
        worldSize = Master.Instance.worldSize;

        // Spawn Walls
        GameObject wall;
        wall = Instantiate(Master.Instance.wallPrefab, new Vector3(worldSize / 2, 0, 0), Quaternion.identity, gameObject.transform);
        wall.transform.localScale = new Vector3(1, worldSize, 1);
        wall = Instantiate(Master.Instance.wallPrefab, new Vector3(-1 * worldSize / 2, 0, 0), Quaternion.identity, gameObject.transform);
        wall.transform.localScale = new Vector3(1, worldSize, 1);
        wall = Instantiate(Master.Instance.wallPrefab, new Vector3(0, worldSize / 2, 0), Quaternion.identity, gameObject.transform);
        wall.transform.localScale = new Vector3(worldSize, 1, 1);
        wall = Instantiate(Master.Instance.wallPrefab, new Vector3(0, -1 * worldSize / 2, 0), Quaternion.identity, gameObject.transform);
        wall.transform.localScale = new Vector3(worldSize, 1, 1);
    }

    public void KillEntities() {
        // Update Master
        Master.Instance.cellCount = 0;
        Master.Instance.foodCount = 0;

        // Delete Cells and Food
        foreach (GameObject gameObject in GameObject.FindGameObjectsWithTag("Player"))
        {
            Destroy(gameObject);
        }
        foreach (GameObject gameObject in GameObject.FindGameObjectsWithTag("Food"))
        {
            Destroy(gameObject);
        }
    }

    public void SpawnEntities() 
    {
        // Spawn Cells
        Master.Instance.cellCount += Master.Instance.startingCells;
        for (int i = 0; i < Master.Instance.startingCells; i++) 
        {
            Instantiate(
                Master.Instance.cellPrefab, 
                new Vector3(Random.Range(-worldSize/2f+1, worldSize/2f-1), 
                Random.Range(-worldSize/2f+1, worldSize/2f-1), 0f), 
                Quaternion.identity);
        }

        // Spawn Food
        Master.Instance.foodCount += Master.Instance.startingFood;
        for (int i = 0; i < Master.Instance.startingFood; i++) 
        {
            Instantiate(
                Master.Instance.foodPrefab, 
                new Vector3(Random.Range(-worldSize/2f+1, worldSize/2f-1), 
                Random.Range(-worldSize/2f+1, worldSize/2f-1), 0f), 
                Quaternion.identity, 
                gameObject.transform);
        }
    }
}
