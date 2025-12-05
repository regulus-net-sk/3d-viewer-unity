using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 点群を描画するクラス
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PointCloudRenderer : MonoBehaviour
{
    [Header("描画設定")]
    [SerializeField] private Material pointMaterial;
    [SerializeField] private float pointSize = 0.1f; // デフォルトを0.1に変更（より見やすく）
    [SerializeField] private Color pointColor = Color.white;
    [SerializeField] private bool useBillboard = true; // ビルボード方式を使用するか（常にカメラを向くため、どの角度からでも見える）
    [SerializeField] private bool showDebugInfo = true; // デバッグ情報を表示するか
    
    [Header("デバッグ表示")]
    [SerializeField] private bool showBoundingBox = true; // バウンディングボックスを表示するか
    [SerializeField] private bool showCenter = true; // 中心点（赤丸）を表示するか

    private List<Vector3> currentVertices = new List<Vector3>();
    private List<Color> currentColors = new List<Color>(); // 点群の色情報
    private bool useVertexColors = false; // 頂点カラーを使用するか
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh pointMesh;
    private Camera mainCamera;
    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        // メインカメラを取得
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        // ビルボード方式用の初期化
        if (useBillboard && mainCamera != null)
        {
            lastCameraPosition = mainCamera.transform.position;
            lastCameraRotation = mainCamera.transform.rotation;
        }
        
        // ビルボード方式用の初期化
        if (useBillboard && mainCamera != null)
        {
            lastCameraPosition = mainCamera.transform.position;
            lastCameraRotation = mainCamera.transform.rotation;
        }

        // デフォルトマテリアルの設定
        if (pointMaterial == null)
        {
            pointMaterial = CreateDefaultMaterial();
        }
        // 複数のサブメッシュに対応するため、マテリアル配列を設定
        meshRenderer.material = pointMaterial;
        
        // レンダラーの設定
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        
        // 両面描画を設定（どのシェーダーでも有効）
        if (meshRenderer.material != null)
        {
            meshRenderer.material.SetInt("_Cull", 0); // Cull Off
            // シェーダーがStandardの場合、レンダリングモードを変更
            if (meshRenderer.material.shader.name.Contains("Standard"))
            {
                meshRenderer.material.SetFloat("_Mode", 0); // Opaque
                meshRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                meshRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                meshRenderer.material.SetInt("_ZWrite", 1);
                meshRenderer.material.DisableKeyword("_ALPHATEST_ON");
                meshRenderer.material.DisableKeyword("_ALPHABLEND_ON");
                meshRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                meshRenderer.material.renderQueue = -1;
            }
        }

        InitializeMesh();
    }

    /// <summary>
    /// メッシュを初期化
    /// </summary>
    private void InitializeMesh()
    {
        pointMesh = new Mesh();
        pointMesh.name = "PointCloud";
        meshFilter.mesh = pointMesh;
    }

    /// <summary>
    /// 点群を描画
    /// </summary>
    /// <param name="vertices">頂点のリスト</param>
    public void RenderPoints(List<Vector3> vertices)
    {
        if (vertices == null || vertices.Count == 0)
        {
            currentVertices.Clear();
            currentColors.Clear();
            useVertexColors = false;
            UpdateMesh();
            return;
        }

        currentVertices = new List<Vector3>(vertices);
        currentColors.Clear();
        useVertexColors = false;
        Debug.Log($"点群を描画: {currentVertices.Count}点（色情報なし）");
        UpdateMesh();
    }

    /// <summary>
    /// 点群を描画（色情報付き）
    /// </summary>
    /// <param name="vertices">頂点のリスト</param>
    /// <param name="colors">色のリスト</param>
    public void RenderPointsWithColors(List<Vector3> vertices, List<Color> colors)
    {
        if (vertices == null || vertices.Count == 0)
        {
            currentVertices.Clear();
            currentColors.Clear();
            useVertexColors = false;
            UpdateMesh();
            return;
        }

            currentVertices = new List<Vector3>(vertices);
        if (colors != null && colors.Count == vertices.Count)
        {
            currentColors = new List<Color>(colors);
            useVertexColors = true;
            
            // 頂点カラーを使用する場合、マテリアルの色を白に設定
            if (pointMaterial != null)
            {
                // カスタムシェーダーを使用する場合
                if (pointMaterial.shader.name.Contains("UnlitVertexColor"))
                {
                    pointMaterial.color = Color.white;
                    pointMaterial.SetColor("_Color", Color.white);
                    Debug.Log($"カスタムシェーダー（UnlitVertexColor）で頂点カラーを使用");
                }
                else if (pointMaterial.shader.name.Contains("Standard"))
                {
                    // Standardシェーダーの場合、_Colorとエミッションを白に設定して頂点カラーがそのまま表示されるようにする
                    pointMaterial.color = Color.white;
                    pointMaterial.SetColor("_Color", Color.white);
                    // エミッションを有効にして光の影響を受けないようにする（頂点カラー × _Color + エミッション）
                    pointMaterial.EnableKeyword("_EMISSION");
                    pointMaterial.SetColor("_EmissionColor", Color.white);
                    pointMaterial.SetFloat("_Metallic", 0f);
                    pointMaterial.SetFloat("_Glossiness", 0f);
                    pointMaterial.SetFloat("_Smoothness", 0f);
                    pointMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    Debug.Log($"Standardシェーダーで頂点カラーを使用: _Colorとエミッションを白に設定");
                }
                else if (pointMaterial.shader.name.Contains("Unlit/Color"))
                {
                    // Unlit/Colorシェーダーは頂点カラーをサポートしていないため、カスタムシェーダーに切り替える
                    Shader customShader = Shader.Find("Custom/UnlitVertexColor");
                    if (customShader != null)
                    {
                        pointMaterial.shader = customShader;
                        pointMaterial.color = Color.white;
                        pointMaterial.SetColor("_Color", Color.white);
                        Debug.Log($"Unlit/Colorからカスタムシェーダー（UnlitVertexColor）に切り替え");
                    }
                    else
                    {
                        // カスタムシェーダーが見つからない場合はStandardに切り替える
                        Shader standardShader = Shader.Find("Standard");
                        if (standardShader != null)
                        {
                            pointMaterial.shader = standardShader;
                            pointMaterial.color = Color.white;
                            pointMaterial.SetColor("_Color", Color.white);
                            pointMaterial.EnableKeyword("_EMISSION");
                            pointMaterial.SetColor("_EmissionColor", Color.white);
                            pointMaterial.SetFloat("_Metallic", 0f);
                            pointMaterial.SetFloat("_Glossiness", 0f);
                            pointMaterial.SetFloat("_Smoothness", 0f);
                            pointMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                            Debug.Log($"Unlit/ColorからStandardシェーダーに切り替え（頂点カラーをサポート）");
                        }
                        else
                        {
                            pointMaterial.color = Color.white;
                            if (pointMaterial.HasProperty("_Color"))
                            {
                                pointMaterial.SetColor("_Color", Color.white);
                            }
                            Debug.Log($"マテリアルの色を白に設定（頂点カラーを使用）: シェーダー={pointMaterial.shader.name}");
                        }
                    }
                }
            }
            
            // 最初と最後の色を確認
            if (colors.Count > 0)
            {
                Debug.Log($"点群を描画: {currentVertices.Count}点（色情報あり）、最初の色={colors[0]}, 最後の色={colors[colors.Count - 1]}");
            }
            else
            {
                Debug.Log($"点群を描画: {currentVertices.Count}点（色情報あり）");
            }
        }
        else
        {
            currentColors.Clear();
            useVertexColors = false;
            Debug.Log($"点群を描画: {currentVertices.Count}点（色情報なし、色の数が一致しません）");
        }
        UpdateMesh();
    }

    /// <summary>
    /// メッシュを更新
    /// </summary>
    private void UpdateMesh()
    {
        if (pointMesh == null)
        {
            InitializeMesh();
        }

        if (currentVertices.Count == 0)
        {
            pointMesh.Clear();
            return;
        }

        // Unityのメッシュは65535頂点まで（各点を4頂点に展開するので、16383点まで）
        const int maxVerticesPerMesh = 65535;
        const int maxPointsPerMesh = maxVerticesPerMesh / 4;

        // 点が多すぎる場合は複数のサブメッシュに分割
        int totalPoints = currentVertices.Count;
        int subMeshCount = Mathf.CeilToInt((float)totalPoints / maxPointsPerMesh);
        
        if (totalPoints > maxPointsPerMesh)
        {
            Debug.Log($"点群の点数が多いため、{subMeshCount}個のサブメッシュに分割します（{totalPoints}点）。");
        }

        // サブメッシュ用のリストを準備
        List<List<int>> subMeshIndices = new List<List<int>>();
        List<Vector3> allMeshVertices = new List<Vector3>();
        List<Vector2> allUvs = new List<Vector2>();
        List<Color> allColors = new List<Color>(); // 頂点カラー

        // 各サブメッシュを生成
        for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
        {
            int startIndex = subMeshIndex * maxPointsPerMesh;
            int endIndex = Mathf.Min(startIndex + maxPointsPerMesh, totalPoints);
            int pointCount = endIndex - startIndex;

            List<int> indices = new List<int>();

            for (int i = 0; i < pointCount; i++)
            {
                int vertexIndex = startIndex + i;
                Vector3 center = currentVertices[vertexIndex];
                float halfSize = pointSize * 0.5f;

                // 各点を小さな四角形（2つの三角形）として描画
                int baseIndex = allMeshVertices.Count;

                Vector3 right, up;
                if (useBillboard && mainCamera != null)
                {
                    // ビルボード方式：各点からカメラへの方向を計算
                    Vector3 toCamera = (mainCamera.transform.position - center).normalized;
                    // カメラが真上/真下にある場合の処理
                    if (Mathf.Abs(Vector3.Dot(toCamera, Vector3.up)) > 0.99f)
                    {
                        right = Vector3.right;
                        up = Vector3.Cross(toCamera, right).normalized;
                    }
                    else
                    {
                        right = Vector3.Cross(Vector3.up, toCamera).normalized;
                        up = Vector3.Cross(toCamera, right).normalized;
                    }
                }
                else
                {
                    // 固定方向（XY平面）
                    right = Vector3.right;
                    up = Vector3.up;
                }

                // 四角形の4つの頂点
                allMeshVertices.Add(center + (-right - up) * halfSize);
                allMeshVertices.Add(center + (right - up) * halfSize);
                allMeshVertices.Add(center + (right + up) * halfSize);
                allMeshVertices.Add(center + (-right + up) * halfSize);

                // UV座標
                allUvs.Add(new Vector2(0, 0));
                allUvs.Add(new Vector2(1, 0));
                allUvs.Add(new Vector2(1, 1));
                allUvs.Add(new Vector2(0, 1));

                // 頂点カラー（色情報がある場合は使用、ない場合はpointColorを使用）
                Color pointColorToUse = useVertexColors && vertexIndex < currentColors.Count 
                    ? currentColors[vertexIndex] 
                    : pointColor;
                allColors.Add(pointColorToUse);
                allColors.Add(pointColorToUse);
                allColors.Add(pointColorToUse);
                allColors.Add(pointColorToUse);

                // 2つの三角形のインデックス（時計回り）
                indices.Add(baseIndex);
                indices.Add(baseIndex + 1);
                indices.Add(baseIndex + 2);

                indices.Add(baseIndex);
                indices.Add(baseIndex + 2);
                indices.Add(baseIndex + 3);
            }

            subMeshIndices.Add(indices);
        }

        pointMesh.Clear();
        
        // メッシュデータを設定
        pointMesh.SetVertices(allMeshVertices);
        pointMesh.SetUVs(0, allUvs);
        pointMesh.SetColors(allColors); // 頂点カラーを設定
        
        // デバッグ: 頂点カラーが正しく設定されているか確認
        if (useVertexColors && allColors.Count > 0)
        {
            Debug.Log($"頂点カラーを設定: {allColors.Count}個の頂点カラー、最初の色={allColors[0]}, 最後の色={allColors[allColors.Count - 1]}");
        }
        
        // サブメッシュを設定
        pointMesh.subMeshCount = subMeshCount;
        for (int i = 0; i < subMeshCount; i++)
        {
            pointMesh.SetTriangles(subMeshIndices[i], i);
        }
        
        // 複数のサブメッシュに対応するため、マテリアル配列を設定
        // 頂点カラーを使用する場合は、マテリアルの色を白に設定
        if (useVertexColors && pointMaterial != null)
        {
            if (pointMaterial.shader.name.Contains("UnlitVertexColor"))
            {
                // カスタムシェーダーの場合
                pointMaterial.color = Color.white;
                pointMaterial.SetColor("_Color", Color.white);
            }
            else if (pointMaterial.shader.name.Contains("Standard"))
            {
                // Standardシェーダーの場合、_Colorとエミッションを白に設定して頂点カラーがそのまま表示されるようにする
                pointMaterial.color = Color.white;
                pointMaterial.SetColor("_Color", Color.white);
                // エミッションを有効にして光の影響を受けないようにする
                pointMaterial.EnableKeyword("_EMISSION");
                pointMaterial.SetColor("_EmissionColor", Color.white);
            }
            else if (pointMaterial.shader.name.Contains("Unlit/Color"))
            {
                pointMaterial.color = Color.white;
                if (pointMaterial.HasProperty("_Color"))
                {
                    pointMaterial.SetColor("_Color", Color.white);
                }
            }
        }
        
        Material[] materials = new Material[subMeshCount];
        for (int i = 0; i < subMeshCount; i++)
        {
            materials[i] = pointMaterial;
        }
        meshRenderer.materials = materials;
        
        // 法線を再計算（両面描画のため重要）
        pointMesh.RecalculateNormals();
        pointMesh.RecalculateBounds();
        pointMesh.RecalculateTangents();
        
        // メッシュの設定
        pointMesh.MarkDynamic(); // 動的メッシュとしてマーク（更新が頻繁な場合）
        
        // メッシュが正しく設定されているか確認
        if (pointMesh.vertexCount == 0)
        {
            Debug.LogError("メッシュの頂点数が0です！");
        }
        else if (showDebugInfo)
        {
            Debug.Log($"メッシュ有効性チェック: 頂点数={pointMesh.vertexCount}, サブメッシュ数={pointMesh.subMeshCount}, 有効={pointMesh.isReadable}");
        }
        
        // マテリアルが正しく設定されているか確認
        if (meshRenderer != null)
        {
            if (meshRenderer.materials == null || meshRenderer.materials.Length == 0)
            {
                Debug.LogError("マテリアルが設定されていません！");
            }
            else if (showDebugInfo)
            {
                Debug.Log($"マテリアル設定: {meshRenderer.materials.Length}個のマテリアルが設定されています");
            }
        }
        
        // デバッグ情報
        int totalTriangles = 0;
        foreach (var idx in subMeshIndices)
        {
            totalTriangles += idx.Count / 3;
        }
        
        Bounds bounds = pointMesh.bounds;
        Vector3 boundsCenter = GetBoundsCenter();
        
        if (showDebugInfo)
        {
            Debug.Log($"点群メッシュ更新: {totalPoints}点, {allMeshVertices.Count}頂点, {totalTriangles}三角形, {subMeshCount}サブメッシュ");
            Debug.Log($"バウンディングボックス: 中心=({bounds.center.x:F3}, {bounds.center.y:F3}, {bounds.center.z:F3}), サイズ=({bounds.size.x:F3}, {bounds.size.y:F3}, {bounds.size.z:F3})");
            Debug.Log($"点群の中心: ({boundsCenter.x:F3}, {boundsCenter.y:F3}, {boundsCenter.z:F3})");
            Debug.Log($"点のサイズ: {pointSize}, マテリアル: {(pointMaterial != null ? pointMaterial.name : "null")}");
        }
    }

    /// <summary>
    /// デフォルトのマテリアルを作成
    /// </summary>
    private Material CreateDefaultMaterial()
    {
        // カスタムシェーダー（頂点カラーを直接表示）を優先的に使用
        Shader shader = Shader.Find("Custom/UnlitVertexColor");
        if (shader == null)
        {
            // カスタムシェーダーが見つからない場合はStandardを使用
            shader = Shader.Find("Standard");
            if (shader == null)
            {
                // Standardも見つからない場合はUnlit/Colorを使用
                shader = Shader.Find("Unlit/Color");
            }
        }
        
        Material mat = new Material(shader);
        mat.color = pointColor;
        
        // Standardシェーダーの場合、エミッションを有効にして光の影響を受けないようにする（頂点カラーを使用する場合に備える）
        if (shader.name.Contains("Standard"))
        {
            mat.SetColor("_Color", pointColor);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", pointColor);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Glossiness", 0f);
            mat.SetFloat("_Smoothness", 0f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        
        // 両面描画にする（裏面カリングを無効化）
        mat.SetInt("_Cull", 0); // Cull Off
        return mat;
    }

    /// <summary>
    /// 点のサイズを設定
    /// </summary>
    public void SetPointSize(float size)
    {
        pointSize = size;
        if (currentVertices.Count > 0)
        {
            UpdateMesh();
        }
    }

    /// <summary>
    /// 点の色を設定
    /// </summary>
    public void SetPointColor(Color color)
    {
        pointColor = color;
        if (meshRenderer != null && pointMaterial != null)
        {
            pointMaterial.color = color;
            // 両面描画を維持
            pointMaterial.SetInt("_Cull", 0); // Cull Off
            
            // マテリアル配列を更新
            if (meshRenderer.materials != null && meshRenderer.materials.Length > 0)
            {
                Material[] materials = meshRenderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null)
                    {
                        materials[i].color = color;
                        materials[i].SetInt("_Cull", 0);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 点群の中心位置を取得
    /// </summary>
    public Vector3 GetCenter()
    {
        if (currentVertices == null || currentVertices.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 sum = Vector3.zero;
        foreach (Vector3 vertex in currentVertices)
        {
            sum += vertex;
        }
        return sum / currentVertices.Count;
    }

    /// <summary>
    /// 点群のバウンディングボックスの中心を取得
    /// </summary>
    public Vector3 GetBoundsCenter()
    {
        if (currentVertices == null || currentVertices.Count == 0)
        {
            return Vector3.zero;
        }

        // 元の頂点座標から直接計算（メッシュのバウンディングボックスは各点を四角形に展開したものなので使わない）
        Vector3 min = currentVertices[0];
        Vector3 max = currentVertices[0];
        foreach (Vector3 vertex in currentVertices)
        {
            min = Vector3.Min(min, vertex);
            max = Vector3.Max(max, vertex);
        }
        return (min + max) * 0.5f;
    }

    void Update()
    {
        // ビルボード方式の場合、カメラが動いたらメッシュを更新
        if (useBillboard && mainCamera != null && currentVertices.Count > 0)
        {
            // カメラの位置や回転が変わった場合のみ更新（パフォーマンス最適化）
            Vector3 currentCameraPosition = mainCamera.transform.position;
            Quaternion currentCameraRotation = mainCamera.transform.rotation;
            
            // カメラが動いたかチェック（位置が0.01以上、または回転が1度以上変わった場合）
            if (Vector3.Distance(currentCameraPosition, lastCameraPosition) > 0.01f ||
                Quaternion.Angle(currentCameraRotation, lastCameraRotation) > 1f)
            {
                UpdateMesh();
                lastCameraPosition = currentCameraPosition;
                lastCameraRotation = currentCameraRotation;
            }
        }
    }

    /// <summary>
    /// デバッグ用：点群の情報を表示
    /// </summary>
    void OnDrawGizmos()
    {
        // Sceneビューでの表示
        DrawDebugVisualizations();
    }

    void OnDrawGizmosSelected()
    {
        // 選択時にも表示
        DrawDebugVisualizations();
    }

    /// <summary>
    /// デバッグ視覚化を描画（SceneビューとGameビューの両方で表示）
    /// </summary>
    private void DrawDebugVisualizations()
    {
        if (currentVertices == null || currentVertices.Count == 0)
            return;

        try
        {
            Vector3 center = GetBoundsCenter();
            
            // Transformの位置を考慮
            Vector3 worldCenter = transform.TransformPoint(center);

            // バウンディングボックスを描画
            if (showBoundingBox)
            {
                // バウンディングボックスのサイズを計算
                Vector3 min = currentVertices[0];
                Vector3 max = currentVertices[0];
                foreach (Vector3 vertex in currentVertices)
                {
                    min = Vector3.Min(min, vertex);
                    max = Vector3.Max(max, vertex);
                }
                Vector3 size = max - min;
                Vector3 worldSize = transform.TransformVector(size);

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(worldCenter, worldSize);
            }

            // 中心点を描画
            if (showCenter)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(worldCenter, 0.1f);
            }
        }
        catch (System.Exception e)
        {
            // エラーを無視（Gizmo描画中のエラーは表示しない）
            Debug.LogWarning($"Gizmo描画エラー: {e.Message}");
        }
    }

    /// <summary>
    /// Gameビューでも表示するためにOnRenderObjectを使用
    /// </summary>
    void OnRenderObject()
    {
        // Gameビューでの表示（GizmosはGameビューでは表示されないため、GLを使用）
        if (Camera.current == null || Camera.current.name != "Main Camera")
            return;

        DrawDebugVisualizationsGL();
    }

    /// <summary>
    /// GLを使用してGameビューにデバッグ視覚化を描画
    /// </summary>
    private void DrawDebugVisualizationsGL()
    {
        if (currentVertices == null || currentVertices.Count == 0)
            return;

        try
        {
            Vector3 center = GetBoundsCenter();
            Vector3 worldCenter = transform.TransformPoint(center);

            // バウンディングボックスを描画
            if (showBoundingBox)
            {
                Vector3 min = currentVertices[0];
                Vector3 max = currentVertices[0];
                foreach (Vector3 vertex in currentVertices)
                {
                    min = Vector3.Min(min, vertex);
                    max = Vector3.Max(max, vertex);
                }
                Vector3 size = max - min;
                Vector3 worldSize = transform.TransformVector(size);

                // GLを使用してワイヤーフレームボックスを描画
                GL.PushMatrix();
                GL.MultMatrix(Matrix4x4.TRS(worldCenter, Quaternion.identity, worldSize));
                
                Material lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                lineMaterial.SetInt("_ZWrite", 0);
                lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                
                lineMaterial.SetPass(0);
                
                GL.Begin(GL.LINES);
                GL.Color(Color.yellow);
                
                // ボックスの12本のエッジを描画
                Vector3[] corners = new Vector3[]
                {
                    new Vector3(-0.5f, -0.5f, -0.5f), // 0
                    new Vector3(0.5f, -0.5f, -0.5f),  // 1
                    new Vector3(0.5f, 0.5f, -0.5f),   // 2
                    new Vector3(-0.5f, 0.5f, -0.5f),  // 3
                    new Vector3(-0.5f, -0.5f, 0.5f),  // 4
                    new Vector3(0.5f, -0.5f, 0.5f),   // 5
                    new Vector3(0.5f, 0.5f, 0.5f),    // 6
                    new Vector3(-0.5f, 0.5f, 0.5f)     // 7
                };
                
                // 前面
                GL.Vertex(corners[0]); GL.Vertex(corners[1]);
                GL.Vertex(corners[1]); GL.Vertex(corners[2]);
                GL.Vertex(corners[2]); GL.Vertex(corners[3]);
                GL.Vertex(corners[3]); GL.Vertex(corners[0]);
                
                // 後面
                GL.Vertex(corners[4]); GL.Vertex(corners[5]);
                GL.Vertex(corners[5]); GL.Vertex(corners[6]);
                GL.Vertex(corners[6]); GL.Vertex(corners[7]);
                GL.Vertex(corners[7]); GL.Vertex(corners[4]);
                
                // 接続
                GL.Vertex(corners[0]); GL.Vertex(corners[4]);
                GL.Vertex(corners[1]); GL.Vertex(corners[5]);
                GL.Vertex(corners[2]); GL.Vertex(corners[6]);
                GL.Vertex(corners[3]); GL.Vertex(corners[7]);
                
                GL.End();
                GL.PopMatrix();
            }

            // 中心点を描画
            if (showCenter)
            {
                // 球を描画するために複数の線を使用
                GL.PushMatrix();
                GL.MultMatrix(Matrix4x4.TRS(worldCenter, Quaternion.identity, Vector3.one * 0.1f));
                
                Material lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                lineMaterial.SetInt("_ZWrite", 0);
                lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                
                lineMaterial.SetPass(0);
                
                GL.Begin(GL.LINES);
                GL.Color(Color.red);
                
                // 簡単な球のワイヤーフレーム（3つの円で表現）
                int segments = 16;
                float radius = 1f;
                
                // XY平面の円
                for (int i = 0; i < segments; i++)
                {
                    float angle1 = (float)i / segments * Mathf.PI * 2f;
                    float angle2 = (float)(i + 1) / segments * Mathf.PI * 2f;
                    Vector3 p1 = new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0);
                    Vector3 p2 = new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0);
                    GL.Vertex(p1); GL.Vertex(p2);
                }
                
                // XZ平面の円
                for (int i = 0; i < segments; i++)
                {
                    float angle1 = (float)i / segments * Mathf.PI * 2f;
                    float angle2 = (float)(i + 1) / segments * Mathf.PI * 2f;
                    Vector3 p1 = new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
                    Vector3 p2 = new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
                    GL.Vertex(p1); GL.Vertex(p2);
                }
                
                // YZ平面の円
                for (int i = 0; i < segments; i++)
                {
                    float angle1 = (float)i / segments * Mathf.PI * 2f;
                    float angle2 = (float)(i + 1) / segments * Mathf.PI * 2f;
                    Vector3 p1 = new Vector3(0, Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius);
                    Vector3 p2 = new Vector3(0, Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius);
                    GL.Vertex(p1); GL.Vertex(p2);
                }
                
                GL.End();
                GL.PopMatrix();
            }
        }
        catch (System.Exception e)
        {
            // エラーを無視
            Debug.LogWarning($"GL描画エラー: {e.Message}");
        }
    }
    
    /// <summary>
    /// バウンディングボックスの表示を設定
    /// </summary>
    public void SetShowBoundingBox(bool show)
    {
        showBoundingBox = show;
    }
    
    /// <summary>
    /// 中心点の表示を設定
    /// </summary>
    public void SetShowCenter(bool show)
    {
        showCenter = show;
    }
    
    /// <summary>
    /// バウンディングボックスの表示状態を取得
    /// </summary>
    public bool GetShowBoundingBox()
    {
        return showBoundingBox;
    }
    
    /// <summary>
    /// 中心点の表示状態を取得
    /// </summary>
    public bool GetShowCenter()
    {
        return showCenter;
    }

    void OnDestroy()
    {
        if (pointMesh != null)
        {
            Destroy(pointMesh);
        }
    }
}

