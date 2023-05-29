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

    // Animation Helpers
    public AnimationCurve animationCurve;
    private Vector2 startTransform;
    private Vector2 endTransform;
    private float startOrthoSize;
    private float endOrthoSize;
    private float time;

    private void Start()
    {
        uiScript = Master.Instance.uiScript;
        objectToFollow = null;

        centerScreen = new Vector2(Screen.width / 2f, Screen.height / 2f);
        mainCamera = Camera.main;
        startOrthoSize = Master.Instance.worldSize / 2f + 5;
        endOrthoSize = Master.Instance.worldSize / 2f + 5;
        startTransform = new Vector3(Master.Instance.worldSize * 0.3f, 0f, -10f);
        endTransform = new Vector3(Master.Instance.worldSize * 0.3f, 0f, -10f);
        time = 0f;
        maxOrthographicSize = (int) Master.Instance.worldSize + 5;
    }

    void Update()
    {
        // Camera Speed
        movementSpeed = mainCamera.orthographicSize / 50000f;

        if (time < 1f) {
            // Smooth Orthographic Transition
            time += Time.unscaledDeltaTime;
            mainCamera.orthographicSize = Mathf.Lerp(startOrthoSize, endOrthoSize, animationCurve.Evaluate(time));
            Vector2 tempPos = Vector2.Lerp(startTransform, endTransform, animationCurve.Evaluate(time));
            transform.position = new Vector3(tempPos.x, tempPos.y, -10f);
        }

        // Switch Between Global View & Cell View
        if (Input.GetKeyDown("escape")) 
        {
            Master.Instance.globalView = !Master.Instance.globalView;
            objectToFollow = null;
            Master.Instance.cellView = false;
            if (Master.Instance.globalView) 
            {
                // mainCamera.orthographicSize = (int) Master.Instance.worldSize / 2f + 5;
                startOrthoSize = mainCamera.orthographicSize;
                endOrthoSize = Master.Instance.worldSize / 2f + 5;
                time = 0f;
            } else 
            {
                // mainCamera.orthographicSize = (int) Master.Instance.worldSize / 4f + 5;
                startOrthoSize = mainCamera.orthographicSize;
                endOrthoSize = Master.Instance.worldSize / 4f + 5;
                time = 0f;
            }
        }
        if (Input.GetKeyDown("space")) 
        {
            objectToFollow = null;
            Master.Instance.cellView = false;
        }

        if (Master.Instance.globalView) 
        {
            // Zoom Out To Global View
            startTransform = transform.position;
            endTransform = new Vector3(Master.Instance.worldSize * 0.3f, 0f, -10f);

        } else 
        {
            // Check If Clicking On Cell
            if (Input.GetMouseButtonDown(0)) 
            {
                RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                if (hit.collider != null && hit.collider.CompareTag("PlayerArea")) {
                    objectToFollow = hit.collider.gameObject.transform.parent.gameObject;
                    uiScript.tempCellScript = objectToFollow.GetComponent<Cell>();
                    startTransform = transform.position;
                    endTransform = new Vector3(objectToFollow.transform.position.x, objectToFollow.transform.position.y, -10f);
                    startOrthoSize = mainCamera.orthographicSize;
                    endOrthoSize = mainCamera.orthographicSize;
                    time = 0f;
                }
            }
            if (objectToFollow != null) 
            {
                // Follow Cell
                Master.Instance.cellView = true;
                if (time > 1f)
                {
                    transform.position = new Vector3(objectToFollow.transform.position.x, objectToFollow.transform.position.y, -10f);
                } else{
                    endTransform = new Vector3(objectToFollow.transform.position.x, objectToFollow.transform.position.y, -10f);
                }
            } else 
            {
                if (time > 1f)
                {
                    // Mouse Controlled Movement (Normal View)
                    Master.Instance.cellView = false;
                    Vector2 cursorVector = (Vector2) Input.mousePosition - centerScreen;
                    transform.position += new Vector3(cursorVector.x, cursorVector.y, 0f) * movementSpeed;
                    startTransform = transform.position;
                    endTransform = transform.position;
                }

                float w = Master.Instance.worldSize / 4f;
                float h = Master.Instance.worldSize / 4f;
                transform.position = new Vector3(
                    Mathf.Clamp(transform.position.x, -w, w),
                    Mathf.Clamp(transform.position.y, -h, h),
                    -10f);
            }

            if (time > 1f) 
            {
                // Scroll Function
                float scrollInput = Input.GetAxis("Mouse ScrollWheel");
                float newSize = mainCamera.orthographicSize - scrollInput * scrollSpeed;
                newSize = Mathf.Clamp(newSize, minOrthographicSize, maxOrthographicSize);
                mainCamera.orthographicSize = newSize;
            }
        }
    }
}
