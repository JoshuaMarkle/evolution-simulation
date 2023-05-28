using UnityEngine;

public class CameraMovement : MonoBehaviour 
{
    private UI uiScript;

    // Changable Vars
    [Header("Custom")]
    public float movementSpeed = 1f;

    private Vector2 centerScreen;
    private Camera mainCamera;
    private GameObject objectToFollow;

    private void Start()
    {
        uiScript = Master.Instance.uiScript;

        centerScreen = new Vector2(Screen.width / 2f, Screen.height / 2f);
        mainCamera = Camera.main;
        mainCamera.orthographicSize = 55;
    }

    void Update()
    {
        // Switch Between Global View & Cell View
        if (Input.GetKeyDown("space")) 
        {
            Master.Instance.globalView = !Master.Instance.globalView;
            objectToFollow = null;
            Master.Instance.cellView = false;
            if (Master.Instance.globalView) 
            {
                mainCamera.orthographicSize = 55;
            } else 
            {
                mainCamera.orthographicSize = 20;
            }
        }
        if (Input.GetKeyDown("escape")) 
        {
            objectToFollow = null;
            Master.Instance.cellView = false;
        }

        if (Master.Instance.globalView) {
            // Zoom Out To Global View
            transform.position = new Vector3(30f, 0f, -10f);
        } else {
            // Check If Clicking On Cell
            if (Input.GetMouseButtonDown(0)) {
                RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                if (hit.collider != null && hit.collider.CompareTag("PlayerArea")) {
                    objectToFollow = hit.collider.gameObject.transform.parent.gameObject;
                    uiScript.tempCellScript = objectToFollow.GetComponent<Cell>();
                }
            }
            if (objectToFollow != null) {
                // Follow Cell
                Master.Instance.cellView = true;
                transform.position = new Vector3(objectToFollow.transform.position.x, objectToFollow.transform.position.y, -10f);
            } else {
                // Mouse Controlled Movement (Normal View)
                Master.Instance.cellView = false;
                Vector2 cursorVector = (Vector2) Input.mousePosition - centerScreen;
                transform.position += new Vector3(cursorVector.x, cursorVector.y, 0f) * movementSpeed;
                float i = Master.Instance.worldSize / 2f - mainCamera.orthographicSize;
                transform.position = new Vector3(
                    Mathf.Clamp(transform.position.x, -i/2f, i/2f),
                    Mathf.Clamp(transform.position.y, -i, i),
                    -10f);
            }
        }
    }
}
