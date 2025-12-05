using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Y=0にグリッド付きの床を表示するクラス（テクスチャ方式）
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class FloorRenderer : MonoBehaviour
{
    [Header("床の設定")]
    [SerializeField] private float floorSize = 100f; // 床のサイズ（一辺の長さ）
    [SerializeField] private float gridSpacing = 1f; // グリッドの間隔
    [SerializeField] private int gridTextureSize = 512; // グリッドテクスチャのサイズ（ピクセル）
    [SerializeField] private Color floorColor = new Color(0.2f, 0.2f, 0.25f, 0.9f); // 床の色（ダークグレー、やや不透明）
    [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 1f); // グリッド線の色（白、不透明、最大のコントラスト）
    [SerializeField] private bool showGrid = true; // グリッドを表示するか
    [SerializeField] private int gridSubdivisions = 10; // グリッドの細分化（10なら1mごと、5なら2mごと）

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh floorMesh;
    private Material floorMaterial;
    private Texture2D gridTexture; // グリッドテクスチャ
    private bool needsMeshRegeneration = false;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        // 床をY=0に配置
        transform.position = new Vector3(0, 0, 0);
        transform.rotation = Quaternion.identity;

        // 色が白（デフォルト値）の場合は、ダークグレーにリセット
        if (floorColor.r >= 0.99f && floorColor.g >= 0.99f && floorColor.b >= 0.99f)
        {
            floorColor = new Color(0.2f, 0.2f, 0.25f, 0.9f);
        }
        if (gridColor.r >= 0.99f && gridColor.g >= 0.99f && gridColor.b >= 0.99f)
        {
            gridColor = new Color(0.4f, 0.4f, 0.5f, 0.8f);
        }

        // マテリアルを設定
        SetupMaterial();

        // メッシュを生成
        GenerateFloorMesh();
    }

    void Update()
    {
        if (needsMeshRegeneration)
        {
            needsMeshRegeneration = false;
            GenerateFloorMesh();
        }
    }

    /// <summary>
    /// マテリアルを設定
    /// </summary>
    private void SetupMaterial()
    {
        // 既存のマテリアルを削除
        if (floorMaterial != null)
        {
            DestroyImmediate(floorMaterial);
            floorMaterial = null;
        }

        // Unlit/Textureシェーダーを使用（テクスチャを表示するため）
        Shader shader = Shader.Find("Unlit/Texture");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        floorMaterial = new Material(shader);
        floorMaterial.SetInt("_Cull", 0); // 両面描画
        
        // 床の色を設定
        if (floorMaterial.HasProperty("_Color"))
        {
            floorMaterial.SetColor("_Color", floorColor);
        }
        floorMaterial.color = floorColor;

        meshRenderer.material = floorMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        
        Debug.Log($"床のマテリアルを設定: シェーダー={floorMaterial.shader.name}, 色={floorColor}");
    }

    /// <summary>
    /// グリッドテクスチャを生成
    /// </summary>
    private Texture2D GenerateGridTexture()
    {
        Texture2D texture = new Texture2D(gridTextureSize, gridTextureSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Repeat;

        // テクスチャの各ピクセルを床の色で初期化
        Color[] pixels = new Color[gridTextureSize * gridTextureSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = floorColor;
        }

        // グリッド線を描画
        if (showGrid)
        {
            float pixelsPerUnit = gridTextureSize / gridSpacing; // 1単位あたりのピクセル数
            
            for (int y = 0; y < gridTextureSize; y++)
            {
                for (int x = 0; x < gridTextureSize; x++)
                {
                    float worldX = (x / (float)gridTextureSize) * gridSpacing;
                    float worldY = (y / (float)gridTextureSize) * gridSpacing;
                    
                    // グリッド線の位置を計算
                    float gridX = worldX % gridSpacing;
                    float gridY = worldY % gridSpacing;
                    
                    // グリッド線の太さ（ワールド単位）
                    float lineWidth = gridSpacing / gridTextureSize * 2f; // 2ピクセル分の太さ
                    
                    // 太い線（gridSubdivisionsごと）かどうか
                    bool isThickLine = (Mathf.FloorToInt(worldX / gridSpacing) % gridSubdivisions == 0) ||
                                      (Mathf.FloorToInt(worldY / gridSpacing) % gridSubdivisions == 0);
                    
                    // グリッド線を描画
                    if (gridX < lineWidth || gridX > gridSpacing - lineWidth ||
                        gridY < lineWidth || gridY > gridSpacing - lineWidth)
                    {
                        Color lineColor = gridColor;
                        if (!isThickLine)
                        {
                            lineColor = new Color(gridColor.r, gridColor.g, gridColor.b, gridColor.a * 0.7f);
                        }
                        pixels[y * gridTextureSize + x] = lineColor;
                    }
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        
        Debug.Log($"グリッドテクスチャを生成: サイズ={gridTextureSize}x{gridTextureSize}, Show Grid={showGrid}");
        
        return texture;
    }

    /// <summary>
    /// 床のメッシュを生成
    /// </summary>
    private void GenerateFloorMesh()
    {
        if (floorMesh == null)
        {
            floorMesh = new Mesh();
            floorMesh.name = "Floor";
        }
        else
        {
            floorMesh.Clear();
        }

        float halfSize = floorSize * 0.5f;

        // 床の頂点（上面と下面の両方を作成）
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // 上面の4つの頂点
        vertices.Add(new Vector3(-halfSize, 0, -halfSize));
        vertices.Add(new Vector3(halfSize, 0, -halfSize));
        vertices.Add(new Vector3(halfSize, 0, halfSize));
        vertices.Add(new Vector3(-halfSize, 0, halfSize));

        // 上面のUV座標（テクスチャを繰り返す）
        float uvScale = floorSize / gridSpacing; // テクスチャの繰り返し回数
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(uvScale, 0));
        uvs.Add(new Vector2(uvScale, uvScale));
        uvs.Add(new Vector2(0, uvScale));

        // 上面の三角形
        triangles.Add(0);
        triangles.Add(1);
        triangles.Add(2);
        triangles.Add(0);
        triangles.Add(2);
        triangles.Add(3);

        // 下面の4つの頂点
        vertices.Add(new Vector3(-halfSize, 0, -halfSize));
        vertices.Add(new Vector3(halfSize, 0, -halfSize));
        vertices.Add(new Vector3(halfSize, 0, halfSize));
        vertices.Add(new Vector3(-halfSize, 0, halfSize));

        // 下面のUV座標
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(uvScale, 0));
        uvs.Add(new Vector2(uvScale, uvScale));
        uvs.Add(new Vector2(0, uvScale));

        // 下面の三角形（反時計回り）
        triangles.Add(4);
        triangles.Add(6);
        triangles.Add(5);
        triangles.Add(4);
        triangles.Add(7);
        triangles.Add(6);

        // メッシュにデータを設定
        floorMesh.SetVertices(vertices);
        floorMesh.SetTriangles(triangles, 0);
        floorMesh.SetUVs(0, uvs);

        // 法線を設定
        List<Vector3> normals = new List<Vector3>();
        for (int i = 0; i < 4; i++)
        {
            normals.Add(Vector3.up); // 上面は上向き
        }
        for (int i = 0; i < 4; i++)
        {
            normals.Add(Vector3.down); // 下面は下向き
        }
        floorMesh.SetNormals(normals);

        floorMesh.RecalculateBounds();
        meshFilter.mesh = floorMesh;

        // グリッドテクスチャを生成してマテリアルに設定
        if (gridTexture != null)
        {
            DestroyImmediate(gridTexture);
            gridTexture = null;
        }

        gridTexture = GenerateGridTexture();
        
        if (floorMaterial != null && floorMaterial.HasProperty("_MainTex"))
        {
            floorMaterial.SetTexture("_MainTex", gridTexture);
            Debug.Log($"グリッドテクスチャをマテリアルに設定: Show Grid={showGrid}");
        }

        Debug.Log($"床のメッシュを生成: サイズ={floorSize}, 頂点数={vertices.Count}, 三角形数={triangles.Count / 3}");
    }

    /// <summary>
    /// Inspectorで値が変更されたときに呼ばれる
    /// </summary>
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            needsMeshRegeneration = true;
        }
    }
}
