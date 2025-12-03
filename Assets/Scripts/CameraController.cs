using UnityEngine;

/// <summary>
/// カメラをマウスで操作できるようにするクラス
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("回転設定")]
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private bool invertY = false;

    [Header("ズーム設定")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 1f;
    [SerializeField] private float maxZoom = 50f;

    [Header("パン設定")]
    [SerializeField] private float panSpeed = 0.1f;

    private Vector3 lastMousePosition;
    private bool isRotating = false;
    private bool isPanning = false;

    void Update()
    {
        HandleRotation();
        HandleZoom();
        HandlePan();
    }

    /// <summary>
    /// 回転処理
    /// </summary>
    private void HandleRotation()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isRotating = true;
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isRotating = false;
        }

        if (isRotating)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            float yRotation = delta.x * rotationSpeed;
            float xRotation = delta.y * rotationSpeed * (invertY ? 1f : -1f);

            transform.RotateAround(Vector3.zero, Vector3.up, yRotation);
            transform.RotateAround(Vector3.zero, transform.right, xRotation);

            lastMousePosition = Input.mousePosition;
        }
    }

    /// <summary>
    /// ズーム処理
    /// </summary>
    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Vector3 direction = transform.position.normalized;
            float currentDistance = transform.position.magnitude;
            float newDistance = Mathf.Clamp(currentDistance - scroll * zoomSpeed, minZoom, maxZoom);
            transform.position = direction * newDistance;
        }
    }

    /// <summary>
    /// パン処理（中ボタンでドラッグ）
    /// </summary>
    private void HandlePan()
    {
        if (Input.GetMouseButtonDown(2))
        {
            isPanning = true;
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(2))
        {
            isPanning = false;
        }

        if (isPanning)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 right = transform.right * delta.x * panSpeed;
            Vector3 up = transform.up * delta.y * panSpeed;
            transform.position += right + up;
            lastMousePosition = Input.mousePosition;
        }
    }
}

