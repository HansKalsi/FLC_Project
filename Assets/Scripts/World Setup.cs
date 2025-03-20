using UnityEngine;

public class WorldSetup : MonoBehaviour
{
    // Static World Objects
    public GameObject ground;
    // Prefabs
    public GameObject lawnmowerPrefab;
    public GameObject grassPrefab;
    public GameObject fencePrefab;
    // World Setup Variables
    public float grassSpacing = 0.5f;

    void Start()
    {
        SpawnGrass();
        SpawnLawnmower();
    }

    void SpawnGrass() {
        Renderer groundRenderer = ground.GetComponent<Renderer>();
        Bounds groundBounds = groundRenderer.bounds;

        // Get the min and max corners in X and Z directions
        float minX = groundBounds.min.x;
        float maxX = groundBounds.max.x;
        float minZ = groundBounds.min.z;
        float maxZ = groundBounds.max.z;
        // Get ground's Y to place objects on top of it
        float groundY = groundBounds.min.y;
        // Get right Y for grass to spawn based on it's size
        float spawnY = groundY + grassPrefab.transform.localScale.y / 2;
        
        // Populate the ground with grass
        for (float x = minX; x <= maxX; x += grassSpacing) {
            for (float z = minZ; z <= maxZ; z += grassSpacing) {
                Vector3 spawnPos = new(x, spawnY, z);
                Instantiate(grassPrefab, spawnPos, Quaternion.identity);
            }
        }
    }

    void SpawnLawnmower() {
        Renderer groundRenderer = ground.GetComponent<Renderer>();
        Bounds groundBounds = groundRenderer.bounds;

        // Get the min and max corners in X and Z directions
        float minX = groundBounds.min.x;
        float maxX = groundBounds.max.x;
        float minZ = groundBounds.min.z;
        float maxZ = groundBounds.max.z;
        // Get ground's Y to place objects on top of it
        float groundY = groundBounds.min.y;
        // Get right Y for lawnmower to spawn based on it's size (plus offset for cut grass)
        float spawnY = (groundY + lawnmowerPrefab.transform.localScale.y / 2) + 0.15f;

        // Spawn lawnmower in the middle of the ground
        Vector3 spawnPos = new((minX + maxX) / 2, spawnY, (minZ + maxZ) / 2);
        Instantiate(lawnmowerPrefab, spawnPos, Quaternion.identity);
    }
}
