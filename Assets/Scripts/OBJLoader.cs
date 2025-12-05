using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// OBJファイルを読み込んで点群データを取得するクラス
/// </summary>
public class OBJLoader : MonoBehaviour
{
    /// <summary>
    /// 頂点と色のペアを保持する構造体
    /// </summary>
    public struct VertexWithColor
    {
        public Vector3 position;
        public Color color;
        public bool hasColor;

        public VertexWithColor(Vector3 pos, Color col, bool hasCol)
        {
            position = pos;
            color = col;
            hasColor = hasCol;
        }
    }

    /// <summary>
    /// OBJファイルから頂点データを読み込む（色情報も含む）
    /// </summary>
    /// <param name="filePath">OBJファイルのパス</param>
    /// <returns>頂点と色のペアのリスト</returns>
    public static List<VertexWithColor> LoadVerticesWithColors(string filePath)
    {
        List<VertexWithColor> vertices = new List<VertexWithColor>();
        bool hasColorData = false;

        if (!File.Exists(filePath))
        {
            Debug.LogError($"ファイルが見つかりません: {filePath}");
            return vertices;
        }

        try
        {
            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                // 頂点データ（vで始まる行）を読み込む
                // 形式: v x y z [r g b] または v x y z
                if (trimmedLine.StartsWith("v "))
                {
                    string[] parts = trimmedLine.Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                    // 最低限 x, y, z の3つの値が必要（parts[0]は"v"なので、parts[1-3]がx,y,z）
                    if (parts.Length >= 4)
                    {
                        try
                        {
                            float x = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                            float y = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                            float z = float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
                            Vector3 pos = new Vector3(x, y, z);

                            // 色情報があるかチェック（parts[4-6]がr, g, b）
                            Color color = Color.white;
                            bool hasColor = false;
                            if (parts.Length >= 7)
                            {
                                try
                                {
                                    float r = float.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture);
                                    float g = float.Parse(parts[5], System.Globalization.CultureInfo.InvariantCulture);
                                    float b = float.Parse(parts[6], System.Globalization.CultureInfo.InvariantCulture);
                                    color = new Color(r, g, b, 1f);
                                    hasColor = true;
                                    hasColorData = true;
                                }
                                catch
                                {
                                    // 色情報の解析に失敗した場合は白を使用
                                }
                            }

                            vertices.Add(new VertexWithColor(pos, color, hasColor));
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"頂点データの解析エラー: {trimmedLine} - {e.Message}");
                        }
                    }
                }
            }

            Debug.Log($"頂点数: {vertices.Count}, 色情報あり: {hasColorData}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"OBJファイルの読み込みエラー: {e.Message}");
        }

        return vertices;
    }

    /// <summary>
    /// OBJファイルから頂点データを読み込む（後方互換性のため）
    /// </summary>
    /// <param name="filePath">OBJファイルのパス</param>
    /// <returns>頂点のリスト</returns>
    public static List<Vector3> LoadVertices(string filePath)
    {
        List<VertexWithColor> verticesWithColors = LoadVerticesWithColors(filePath);
        List<Vector3> vertices = new List<Vector3>();
        
        foreach (var v in verticesWithColors)
        {
            vertices.Add(v.position);
        }
        
        return vertices;
    }

    /// <summary>
    /// 複数のOBJファイルを時系列で読み込む（アニメーション用）
    /// </summary>
    /// <param name="filePaths">OBJファイルのパスのリスト</param>
    /// <returns>各フレームの頂点データのリスト</returns>
    public static List<List<Vector3>> LoadAnimationFrames(List<string> filePaths)
    {
        List<List<Vector3>> frames = new List<List<Vector3>>();

        foreach (string filePath in filePaths)
        {
            List<Vector3> vertices = LoadVertices(filePath);
            frames.Add(vertices);
        }

        return frames;
    }

    /// <summary>
    /// 複数のOBJファイルを時系列で読み込む（アニメーション用、色情報付き）
    /// </summary>
    /// <param name="filePaths">OBJファイルのパスのリスト</param>
    /// <returns>各フレームの頂点データと色データのタプルのリスト</returns>
    public static (List<List<Vector3>> frames, List<List<Color>> frameColors, bool hasColorData) LoadAnimationFramesWithColors(List<string> filePaths)
    {
        List<List<Vector3>> frames = new List<List<Vector3>>();
        List<List<Color>> frameColors = new List<List<Color>>();
        bool hasColorData = false;

        foreach (string filePath in filePaths)
        {
            List<VertexWithColor> verticesWithColors = LoadVerticesWithColors(filePath);
            List<Vector3> vertices = new List<Vector3>();
            List<Color> colors = new List<Color>();
            
            foreach (var v in verticesWithColors)
            {
                vertices.Add(v.position);
                if (v.hasColor)
                {
                    colors.Add(v.color);
                    hasColorData = true;
                }
                else
                {
                    colors.Add(Color.white);
                }
            }
            
            frames.Add(vertices);
            frameColors.Add(colors);
        }

        return (frames, frameColors, hasColorData);
    }
}

