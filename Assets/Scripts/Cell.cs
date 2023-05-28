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
    public string genome = "0000 0000 0000 0000 0000 0000";
    public string color = "FFFFFF";
    public float decisionFrequency = 1f;
    public float energyCapacity = 1f;
    public float foodGain = 0.5f;
    public float moveSpeed = 100f;
    public float maxSpeed = 5f;
    public float viewDistance = 5f;
    public float wanderDeviation = 20f;

    [SerializeField] private Vector2 tempFood;
    private float time = 0f;

    void Start() {
        // Set State
        energy = energyCapacity / 4f;
        viewDistanceScript.xradius = viewDistance / 2f;
        viewDistanceScript.yradius = viewDistance / 2f;
        viewDistanceCollider.radius = viewDistance / 2f;
    }

    void Update() {
        time += Time.deltaTime / decisionFrequency;
        if (time > 1f) {
            time = 0f;
            SetState();
            Move();
            FindNearestFood();
        }

        // Debug (Draw Line To Target Food)
        if (tempFood != Vector2.zero) 
        {
            Debug.DrawLine(gameObject.transform.position, (Vector3) tempFood, Color.red);
        }
    }

    // Limit Velocity
    void FixedUpdate() {
        if(rb.velocity.magnitude > maxSpeed) {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    void SetState() {
        // Cell life is 60s without food
        energy -= Time.deltaTime / 60;
        if (energy <= 0f) {
            Debug.Log("Cell has died");
        }

        // Repopulate
        if (energy >= 1f) {
            energy = energy / 2f;
            Master.Instance.cellCount++;
            Instantiate(
                Master.Instance.cellPrefab,
                gameObject.transform.position,
                Quaternion.identity);
        }

        // Reset closest food
        if (nearbyFood.Count == 0) {
            closestFood = Vector2.zero;
        }
    }

    void Move() {

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
        
        // Randomly determine the rotation direction
        int rotationDirection = Random.Range(0, 2) == 0 ? -1 : 1;
        
        // Calculate the angle in radians with the random deviation
        float angleInRadians = wanderDeviation * rotationDirection * Mathf.Deg2Rad;
        
        // Perform the rotation transformation
        float sin = Mathf.Sin(angleInRadians);
        float cos = Mathf.Cos(angleInRadians);
        float x = (cos * moveDirection.x) - (sin * moveDirection.y);
        float y = (sin * moveDirection.x) + (cos * moveDirection.y);

        moveDirection = (new Vector2(x, y)).normalized;
    }

    void FindNearestFood() {
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
                energy += foodGain;
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
