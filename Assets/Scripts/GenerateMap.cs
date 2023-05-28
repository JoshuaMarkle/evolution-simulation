using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateMap : MonoBehaviour 
{    
    private float worldSize;
    private float time = 0f;

    void Start() 
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

    void Update() 
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
