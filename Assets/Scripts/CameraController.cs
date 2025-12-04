using UnityEngine;
using UnityEngine.EventSystems;

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

    [Header("回転中心")]
    [SerializeField] private Transform rotationTarget; // 回転中心となるオブジェクト（PointCloudObjectなど）

    private Vector3 lastMousePosition;
    private bool isRotating = false;
    private bool isPanning = false;
    private PointCloudRenderer pointCloudRenderer;

    void Update()
    {
        HandleRotation();
        HandleZoom();
        HandlePan();
    }

    /// <summary>
    /// PointCloudRendererを取得（必要に応じて再検索）
    /// </summary>
    private PointCloudRenderer GetPointCloudRenderer()
    {
        // 既に取得済みで有効な場合はそれを返す
        if (pointCloudRenderer != null && pointCloudRenderer.gameObject.activeInHierarchy)
        {
            return pointCloudRenderer;
        }

        // 指定されたTransformから取得
        if (rotationTarget != null)
        {
            pointCloudRenderer = rotationTarget.GetComponent<PointCloudRenderer>();
            if (pointCloudRenderer != null)
            {
                return pointCloudRenderer;
            }
        }

        // 自動検索
        pointCloudRenderer = FindObjectOfType<PointCloudRenderer>();
        return pointCloudRenderer;
    }

    /// <summary>
    /// 回転中心を取得
    /// </summary>
    private Vector3 GetRotationCenter()
    {
        PointCloudRenderer renderer = GetPointCloudRenderer();
        
        if (renderer != null)
        {
            Vector3 center = renderer.GetBoundsCenter();
            // 点群がロードされていない場合は、Transformの位置を使う
            if (center == Vector3.zero)
            {
                if (renderer.transform != null)
                {
                    return renderer.transform.position;
                }
                return Vector3.zero;
            }
            
            // currentVerticesは既にワールド座標（OBJファイルから読み込んだ座標）なので、
            // Transformの位置を加算する必要がある
            if (renderer.transform != null)
            {
                // Transformの位置を加算（点群がTransformの子として配置されている場合）
                center += renderer.transform.position;
            }
            return center;
        }
        
        // フォールバック：指定されたTransformの位置
        if (rotationTarget != null)
        {
            return rotationTarget.position;
        }

        // デフォルト：原点
        return Vector3.zero;
    }

    /// <summary>
    /// UI要素の上にマウスがあるかチェック
    /// </summary>
    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// 回転処理
    /// </summary>
    private void HandleRotation()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // UI要素の上をクリックした場合は無視
            if (IsPointerOverUI())
            {
                return;
            }
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

            Vector3 center = GetRotationCenter();
            transform.RotateAround(center, Vector3.up, yRotation);
            transform.RotateAround(center, transform.right, xRotation);

            lastMousePosition = Input.mousePosition;
        }
    }

    /// <summary>
    /// ズーム処理
    /// </summary>
    private void HandleZoom()
    {
        // UI要素の上でスクロールした場合は無視
        if (IsPointerOverUI())
        {
            return;
        }

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
            // UI要素の上をクリックした場合は無視
            if (IsPointerOverUI())
            {
                return;
            }
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

