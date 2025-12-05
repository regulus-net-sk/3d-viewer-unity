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

    [Header("自動リセット設定")]
    [SerializeField] private bool autoResetWhenOutOfView = true; // 点群がビューから外れたときに自動リセット
    [SerializeField] private float resetCheckInterval = 0.5f; // チェック間隔（秒）
    [SerializeField] private float resetSmoothTime = 1.0f; // リセット時のスムーズ移動時間

    private Vector3 lastMousePosition;
    private bool isRotating = false;
    private bool isPanning = false;
    private PointCloudRenderer pointCloudRenderer;
    
    // デフォルト位置と回転
    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    private bool defaultPositionSet = false;
    
    // リセットチェック用
    private float lastResetCheckTime = 0f;

    void Start()
    {
        // デフォルト位置と回転を保存
        SaveDefaultCameraPosition();
    }

    void Update()
    {
        HandleRotation();
        HandleZoom();
        HandlePan();
        
        // 点群がビューから外れた場合の自動リセット
        if (autoResetWhenOutOfView)
        {
            CheckAndResetIfOutOfView();
        }
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

    /// <summary>
    /// デフォルトのカメラ位置と回転を保存
    /// </summary>
    private void SaveDefaultCameraPosition()
    {
        defaultPosition = transform.position;
        defaultRotation = transform.rotation;
        defaultPositionSet = true;
    }

    /// <summary>
    /// カメラをデフォルト位置にリセット
    /// </summary>
    public void ResetToDefaultPosition()
    {
        if (!defaultPositionSet)
        {
            SaveDefaultCameraPosition();
        }
        
        if (resetSmoothTime > 0f)
        {
            // スムーズに移動
            StartCoroutine(SmoothResetToDefault());
        }
        else
        {
            // 即座に移動
            transform.position = defaultPosition;
            transform.rotation = defaultRotation;
        }
    }

    /// <summary>
    /// スムーズにデフォルト位置にリセット
    /// </summary>
    private System.Collections.IEnumerator SmoothResetToDefault()
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float elapsed = 0f;

        while (elapsed < resetSmoothTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / resetSmoothTime;
            t = Mathf.SmoothStep(0f, 1f, t); // スムーズな補間

            transform.position = Vector3.Lerp(startPosition, defaultPosition, t);
            transform.rotation = Quaternion.Lerp(startRotation, defaultRotation, t);

            yield return null;
        }

        transform.position = defaultPosition;
        transform.rotation = defaultRotation;
    }

    /// <summary>
    /// 点群がビューから外れているかチェックし、外れている場合はリセット
    /// </summary>
    private void CheckAndResetIfOutOfView()
    {
        // チェック間隔を考慮
        if (Time.time - lastResetCheckTime < resetCheckInterval)
        {
            return;
        }
        lastResetCheckTime = Time.time;

        PointCloudRenderer renderer = GetPointCloudRenderer();
        if (renderer == null)
        {
            return;
        }

        // 点群のバウンディングボックスを取得
        Vector3 boundsCenter = renderer.GetBoundsCenter();
        if (boundsCenter == Vector3.zero)
        {
            return; // 点群がまだロードされていない
        }

        // Transformの位置を考慮
        if (renderer.transform != null)
        {
            boundsCenter += renderer.transform.position;
        }

        // カメラの視錐台内に点群の中心があるかチェック
        Camera cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam != null)
        {
            Vector3 viewportPoint = cam.WorldToViewportPoint(boundsCenter);
            
            // ビューポート外（X, Y, Zが範囲外）の場合、リセット
            // Z < 0 はカメラの後ろ、Z > 1 は遠すぎる
            // X, Y が 0-1 の範囲外は画面外
            bool isOutOfView = viewportPoint.z < 0 || 
                               viewportPoint.x < -0.5f || viewportPoint.x > 1.5f ||
                               viewportPoint.y < -0.5f || viewportPoint.y > 1.5f;

            if (isOutOfView)
            {
                Debug.Log("点群がビューから外れました。カメラをデフォルト位置にリセットします。");
                ResetToDefaultPosition();
            }
        }
    }
}

