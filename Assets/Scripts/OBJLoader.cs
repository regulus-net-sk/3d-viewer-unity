using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// OBJファイルを読み込んで点群データを取得するクラス
/// </summary>
public class OBJLoader : MonoBehaviour
{
    /// <summary>
    /// OBJファイルから頂点データを読み込む
    /// </summary>
    /// <param name="filePath">OBJファイルのパス</param>
    /// <returns>頂点のリスト</returns>
    public static List<Vector3> LoadVertices(string filePath)
    {
        List<Vector3> vertices = new List<Vector3>();

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
                line.Trim();

                // 頂点データ（vで始まる行）を読み込む
                if (line.StartsWith("v "))
                {
                    string[] parts = line.Split(' ');
                    if (parts.Length >= 4)
                    {
                        float x = float.Parse(parts[1]);
                        float y = float.Parse(parts[2]);
                        float z = float.Parse(parts[3]);
                        vertices.Add(new Vector3(x, y, z));
                    }
                }
            }

            Debug.Log($"頂点数: {vertices.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"OBJファイルの読み込みエラー: {e.Message}");
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
}

