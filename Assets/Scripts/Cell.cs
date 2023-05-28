using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour {

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

    // Characteristics
    [Header("Characteristics")]
    public string genome = "1111 1100 1010 1001 1110 0011";
    public string color = "#FF0000";
    public float decisionFrequency = 1f;
    public float energyCapacity = 1f;
    public float moveSpeed = 100f;
    public float maxSpeed = 5f;
    public float viewDistance = 5f;
    public float wanderDeviation = 20f;

    [SerializeField] private Vector2 tempFood;
    private float time = 0f;

    void Start() {
        // Inherit Genes
        InheritGenome();

        // Set State
        energy = energyCapacity / 4f;
        
        viewDistanceScript.xradius = viewDistance / 2f;
        viewDistanceScript.yradius = viewDistance / 2f;
        viewDistanceCollider.radius = viewDistance / 2f;

        // Color newColor;
        // if (ColorUtility.TryParseHtmlString(color, out newColor))
        // {
        //     gameObject.GetComponent<SpriteRenderer>().color = newColor;
        //     TrailRenderer trail = transform.Find("Trail").GetComponent<TrailRenderer>();
        //     trail.startColor = newColor;
        //     trail.endColor = newColor;

        // } else {
        //     Debug.Log("I don't like this color: " + color);
        // }
    }

    void Update() {
        time += Time.deltaTime / decisionFrequency;
        if (time > 1f) {
            time = 0f;
            SetState();
            Move();
            FindNearestFood();
        }

        //TODO: Scale According To Energy
        // gameObject.transform.localScale = (Vector3) Vector2.one * (energy + 0.5f);

        // Debug (Draw Line To Target Food)
        if (tempFood != Vector2.zero) 
        {
            Debug.DrawLine(gameObject.transform.position, gameObject.transform.position + (Vector3) moveDirection, Color.red);
        }
    }

    // Limit Velocity
    void FixedUpdate() {
        if(rb.velocity.magnitude > maxSpeed) {
            rb.velocity = rb.velocity.normalized * maxSpeed;
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
        moveSpeed = (float) System.Convert.ToInt32(splitGenome[0], 2);
        maxSpeed = (float) System.Convert.ToInt32(splitGenome[1], 2);
        viewDistance = (float) System.Convert.ToInt32(splitGenome[2], 2);
        //wanderDeviation = (float) System.Convert.ToInt32(splitGenome[4] + splitGenome[5], 2);

        // New Color Gene (Uses all genes)
        string genomeNoSpaces = genome.Replace(" ", "");
        int colorValue = System.Convert.ToInt32(genomeNoSpaces, 2);
        Color newColor = new Color(
            (float)((colorValue >> 16) & 0xFF) / 255f,    // Red component
            (float)((colorValue >> 8) & 0xFF) / 255f,     // Green component
            (float)(colorValue & 0xFF) / 255f,            // Blue component
            1f                                           // Alpha component (fully opaque)
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

    void SetState() {
        // Cell life is 20s without food
        energy -= Time.deltaTime / 20;
        if (energy <= 0f) {
            Master.Instance.cellCount--;
            Destroy(gameObject);
            return;
        }

        // Repopulate
        if (energy >= 1f) {
            energy = energy / 2f;
            Master.Instance.cellCount++;
            GameObject childCell = Instantiate(
                Master.Instance.cellPrefab,
                gameObject.transform.position,
                Quaternion.identity);
            childCell.GetComponent<Cell>().genome = genome;
        }

        // Reset closest food
        if (nearbyFood.Count == 0) {
            closestFood = Vector2.zero;
        }
    }

    void Move() 
    {

        // Main Movement Loop
        if (tempFood != Vector2.zero) {
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

        rb.AddForce(moveDirection * moveSpeed * 1000 * Time.deltaTime);
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

    void OnTriggerEnter2D(Collider2D col) 
    {
        if (col.gameObject.tag == "Food") 
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
        if (col.gameObject.tag == "Food") {
            nearbyFood.Remove(col.transform);
        }
    }
}
