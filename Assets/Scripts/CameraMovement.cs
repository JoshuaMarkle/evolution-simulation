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

    public float scrollSpeed = 10.0f;
    public float minOrthographicSize = 10f;
    public float maxOrthographicSize;

    private void Start()
    {
        uiScript = Master.Instance.uiScript;

        centerScreen = new Vector2(Screen.width / 2f, Screen.height / 2f);
        mainCamera = Camera.main;
        mainCamera.orthographicSize = (int) Master.Instance.worldSize / 2f + 5;
        maxOrthographicSize = (int) Master.Instance.worldSize / 2f + 5;
    }

    void Update()
    {
        // Camera Speed
        movementSpeed = mainCamera.orthographicSize / 50000f;

        // Switch Between Global View & Cell View
        if (Input.GetKeyDown("escape")) 
        {
            Master.Instance.globalView = !Master.Instance.globalView;
            objectToFollow = null;
            Master.Instance.cellView = false;
            if (Master.Instance.globalView) 
            {
                mainCamera.orthographicSize = (int) Master.Instance.worldSize / 2f + 5;
            } else 
            {
                mainCamera.orthographicSize = (int) Master.Instance.worldSize / 4f + 5;
            }
        }
        if (Input.GetKeyDown("space")) 
        {
            objectToFollow = null;
            Master.Instance.cellView = false;
        }

        if (Master.Instance.globalView) {
            // Zoom Out To Global View
            transform.position = new Vector3(Master.Instance.worldSize * 0.3f, 0f, -10f);
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

                float w = Master.Instance.worldSize / 2f - mainCamera.orthographicSize * 1.5f + 5f;
                float h = Master.Instance.worldSize / 2f - mainCamera.orthographicSize + 5f;
                transform.position = new Vector3(
                    Mathf.Clamp(transform.position.x, -w, w),
                    Mathf.Clamp(transform.position.y, -h, h),
                    -10f);
            }

            // Scroll Function
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            float newSize = mainCamera.orthographicSize - scrollInput * scrollSpeed;
            newSize = Mathf.Clamp(newSize, minOrthographicSize, maxOrthographicSize);
            mainCamera.orthographicSize = newSize;
        }
    }
}
