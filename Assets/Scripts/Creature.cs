using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Creature : MonoBehaviour {

     // State:
    [Header("State")]
    public float energy;
    private Vector2 moveDirection = Vector2.up;
    [SerializeField] private List<Transform> nearbyFood = new List<Transform>();
    [SerializeField] private Vector2 closestFood;

    // Components
    [Header("Components")]
    public Rigidbody2D rb;
    public ViewDistance viewDistanceScript;
    public CircleCollider2D viewDistanceCollider;

    // Genes
    [Header("Genes")]
    public string genome = "1111 1100 1010 1001 1110 0011";
    public int generation = 1;
    public string color = "#FF0000";
    public float energyCapacity = 1f;
    public float moveSpeed = 100f;
    public float viewDistance = 5f;
    public float wanderDeviation = 20f;

    public string foodType;
    private Vector2 tempFood;
    private float time = 0f;

    private float maxSpeed = 16f;
    private float maxViewDistance = 15f;
    private float maxWanderDeviation = 360f;

    protected virtual void Awake()
    {
        // Set State
        energy = energyCapacity / 4f;
        
        viewDistanceScript.xradius = viewDistance / 2f;
        viewDistanceScript.yradius = viewDistance / 2f;
        viewDistanceCollider.radius = viewDistance / 2f;
    }

    void Start() 
    {
        // Randomize Genome
        if (generation == 1)
        {
            genome = "";
            for (int i = 0; i < 24; i++)
            {
                genome += Random.Range(0, 2);
                if ((i + 1) % 4 == 0 && i != 23)
                {
                    genome += " ";
                }
            }
        }
        
        // Inherit Genes
        InheritGenome();
    }

    void Update() 
    {
        if (Master.Instance.runningSimulation)
        {
            time += Time.deltaTime / Master.Instance.decisionFrequency;
            if (time > 1f) 
            {
                time = 0f;
                SetState();
                Move();
                FindNearestFood();
            }
        }
    }

    // Limit Velocity
    void FixedUpdate() 
    {
        if(rb.velocity.magnitude > moveSpeed) 
        {
            rb.velocity = rb.velocity.normalized * moveSpeed;
        }
    }

    void SetState() 
    {
        // Apply Energy Cost Function (1/2mv^2)
        float energyCost = 1/2 * Mathf.Pow(moveSpeed, 2) * viewDistance;
        energy -= energyCost * Master.Instance.energyDialator / 50000f + Time.deltaTime / 20f;
        if (energy <= 0f) 
        {
            Master.Instance.cellCount--;
            Destroy(gameObject);
            return;
        }

        // Repopulate
        if (energy >= 1f) 
        {
            energy = energy / 2f;
            Master.Instance.cellCount++;
            GameObject childCell = Instantiate(
                Master.Instance.cellPrefab,
                gameObject.transform.position,
                Quaternion.identity);
            childCell.GetComponent<Cell>().genome = genome;
            childCell.GetComponent<Cell>().generation = generation + 1;
        }

        // Reset closest food
        if (nearbyFood.Count == 0) 
        {
            closestFood = Vector2.zero;
        }
    }

    void Move() 
    {
        // Main Movement Loop
        if (tempFood != Vector2.zero) 
        {
            moveDirection = (tempFood - (Vector2)transform.position).normalized;
        }
        if (closestFood != Vector2.zero) 
        {
            tempFood = closestFood;
            moveDirection = (tempFood - (Vector2)transform.position).normalized;
            rb.velocity = ((Vector3)closestFood - transform.position).normalized * Mathf.Abs(rb.velocity.magnitude);
        } else 
        {
            tempFood = Vector2.zero;
            Wander();
        }

        rb.AddForce(moveDirection * 10 * 1000 * Time.deltaTime);
    }

    void Wander() 
    {
        moveDirection = rb.velocity.normalized;
        if (moveDirection == Vector2.zero)
        {
            moveDirection = Vector2.up;
        }
        
        int rotationDirection = Random.Range(0, 2) == 0 ? -1 : 1;
        float angleInRadians = wanderDeviation * rotationDirection * Mathf.Deg2Rad;

        float sin = Mathf.Sin(angleInRadians);
        float cos = Mathf.Cos(angleInRadians);
        float x = (cos * moveDirection.x) - (sin * moveDirection.y);
        float y = (sin * moveDirection.x) + (cos * moveDirection.y);

        moveDirection = (new Vector2(x, y)).normalized;
    }

    void FindNearestFood() 
    {
        // Clean Food List (Can Get Messy With Null Values)
        List<Transform> cleanedList = new List<Transform>();
        foreach (Transform transform in nearbyFood)
        {
            if (transform != null)
            {
                cleanedList.Add(transform);
            }
        }
        nearbyFood = cleanedList;

        // Is There Food?
        if (nearbyFood.Count == 0) 
        {
            closestFood = Vector2.zero;
        } else {
            // Find Nearest Food
            foreach (Transform i in nearbyFood) 
            {
                if (Vector2.Distance((Vector2) transform.position, i.position) < closestFood.magnitude || closestFood.magnitude == 0f) 
                {
                    closestFood = (Vector2) i.position;
                }
            }
        }
    }

    void InheritGenome() 
    {
        // Split Genome Into Parts
        string[] splitGenome = genome.Split(" "); 

        // MUTATION!!! >:)
        if (Random.Range(0f, 1f) < Master.Instance.mutationFrequency)
        {
            int mutateIndex = UnityEngine.Random.Range(0, splitGenome.Length);
            string mutatedBinary = MutateBinary(splitGenome[mutateIndex]);
            splitGenome[mutateIndex] = mutatedBinary;
        }

        // Apply Changes   
        genome = string.Join(" ", splitGenome);   
        moveSpeed = (float) System.Convert.ToInt32(splitGenome[0] + splitGenome[1], 2) / 255f * maxSpeed;
        viewDistance = (float) System.Convert.ToInt32(splitGenome[2] + splitGenome[3], 2) / 255f * maxViewDistance;
        wanderDeviation = (float) System.Convert.ToInt32(splitGenome[4] + splitGenome[5], 2) / 255f * maxWanderDeviation;

        // New Color Gene (Uses all genes)
        string genomeNoSpaces = genome.Replace(" ", "");
        int colorValue = System.Convert.ToInt32(genomeNoSpaces, 2);
        Color newColor = new Color(
            (float)((colorValue >> 16) & 0xFF) / 255f,    // Red component
            (float)((colorValue >> 8) & 0xFF) / 255f,     // Green component
            (float)(colorValue & 0xFF) / 255f,            // Blue component
            1f                                            // Alpha component (fully opaque)
        );
        gameObject.GetComponent<SpriteRenderer>().color = newColor;
        TrailRenderer trail = transform.Find("Trail").GetComponent<TrailRenderer>();
        trail.startColor = newColor;
        trail.endColor = newColor;
        color = ColorUtility.ToHtmlStringRGB(newColor);
    }

    string MutateBinary(string binary)
    {
        int mutateBitIndex = UnityEngine.Random.Range(0, binary.Length);
        char mutatedBit = binary[mutateBitIndex] == '0' ? '1' : '0';

        char[] binaryArray = binary.ToCharArray();
        binaryArray[mutateBitIndex] = mutatedBit;

        return new string(binaryArray);
    }

    void OnTriggerEnter2D(Collider2D col) 
    {
        if (col.gameObject.tag == foodType) 
        {
            nearbyFood.Add(col.transform);
            if (Vector3.Distance(col.gameObject.transform.position, transform.position) <= 1f) 
            {
                energy += Master.Instance.foodGain;
                Destroy(col.gameObject);
                Master.Instance.foodCount--;
            }
        }
    }

    // Detect Food Leaving View Area
    void OnTriggerExit2D(Collider2D col) 
    {
        if (col.gameObject.tag == foodType) {
            nearbyFood.Remove(col.transform);
        }
    }
}
