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
    [SerializeField] private float pointSize = 0.01f;
    [SerializeField] private Color pointColor = Color.white;

    private List<Vector3> currentVertices = new List<Vector3>();
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh pointMesh;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        // デフォルトマテリアルの設定
        if (pointMaterial == null)
        {
            pointMaterial = CreateDefaultMaterial();
        }
        meshRenderer.material = pointMaterial;

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
            UpdateMesh();
            return;
        }

        currentVertices = new List<Vector3>(vertices);
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

        // 各頂点を小さな四角形として描画するためのインデックスを作成
        List<int> indices = new List<int>();
        List<Vector3> meshVertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < currentVertices.Count; i++)
        {
            Vector3 center = currentVertices[i];
            float halfSize = pointSize * 0.5f;

            // 各点を小さな四角形（2つの三角形）として描画
            int baseIndex = meshVertices.Count;

            // 四角形の4つの頂点（カメラ方向を考慮）
            // 簡易実装として、XY平面に配置
            meshVertices.Add(center + new Vector3(-halfSize, -halfSize, 0));
            meshVertices.Add(center + new Vector3(halfSize, -halfSize, 0));
            meshVertices.Add(center + new Vector3(halfSize, halfSize, 0));
            meshVertices.Add(center + new Vector3(-halfSize, halfSize, 0));

            // UV座標
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(0, 1));

            // 2つの三角形のインデックス
            indices.Add(baseIndex);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);

            indices.Add(baseIndex);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);
        }

        pointMesh.Clear();
        pointMesh.vertices = meshVertices.ToArray();
        pointMesh.triangles = indices.ToArray();
        pointMesh.uv = uvs.ToArray();
        pointMesh.RecalculateNormals();
        pointMesh.RecalculateBounds();
    }

    /// <summary>
    /// デフォルトのマテリアルを作成
    /// </summary>
    private Material CreateDefaultMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = pointColor;
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
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = color;
        }
    }

    void OnDestroy()
    {
        if (pointMesh != null)
        {
            Destroy(pointMesh);
        }
    }
}

