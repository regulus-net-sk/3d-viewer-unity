using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 点群の再生/停止/コマ送りを制御するクラス
/// </summary>
public class PointCloudPlayer : MonoBehaviour
{
    [Header("再生設定")]
    [SerializeField] private float frameRate = 30f; // フレームレート（fps）
    [SerializeField] private bool loop = true; // ループ再生

    private List<List<Vector3>> frames = new List<List<Vector3>>();
    private List<List<Color>> frameColors = new List<List<Color>>(); // 各フレームの色情報
    private bool hasColorData = false; // 色情報があるかどうか
    private int currentFrameIndex = 0;
    private bool isPlaying = false;
    private float frameTimer = 0f;
    private PointCloudRenderer pointCloudRenderer;

    void Start()
    {
        pointCloudRenderer = GetComponent<PointCloudRenderer>();
        if (pointCloudRenderer == null)
        {
            pointCloudRenderer = gameObject.AddComponent<PointCloudRenderer>();
        }
    }

    void Update()
    {
        if (isPlaying && frames.Count > 0)
        {
            frameTimer += Time.deltaTime;
            float frameInterval = 1f / frameRate;

            if (frameTimer >= frameInterval)
            {
                frameTimer = 0f;
                NextFrame();
            }
        }
    }

    /// <summary>
    /// アニメーションフレームを設定
    /// </summary>
    /// <param name="animationFrames">各フレームの頂点データ</param>
    public void SetFrames(List<List<Vector3>> animationFrames)
    {
        frames = animationFrames;
        frameColors.Clear();
        hasColorData = false;
        currentFrameIndex = 0;
        if (frames.Count > 0 && pointCloudRenderer != null)
        {
            pointCloudRenderer.RenderPoints(frames[0]);
        }
    }

    /// <summary>
    /// アニメーションフレームを設定（色情報付き）
    /// </summary>
    /// <param name="animationFrames">各フレームの頂点データ</param>
    /// <param name="colors">各フレームの色データ</param>
    public void SetFramesWithColors(List<List<Vector3>> animationFrames, List<List<Color>> colors)
    {
        frames = animationFrames;
        if (colors != null && colors.Count == animationFrames.Count)
        {
            frameColors = new List<List<Color>>(colors);
            hasColorData = true;
        }
        else
        {
            frameColors.Clear();
            hasColorData = false;
        }
        currentFrameIndex = 0;
        if (frames.Count > 0 && pointCloudRenderer != null)
        {
            if (hasColorData && frameColors.Count > 0 && frameColors[0].Count == frames[0].Count)
            {
                pointCloudRenderer.RenderPointsWithColors(frames[0], frameColors[0]);
            }
            else
            {
                pointCloudRenderer.RenderPoints(frames[0]);
            }
        }
    }

    /// <summary>
    /// 再生を開始
    /// </summary>
    public void Play()
    {
        if (frames.Count > 0)
        {
            isPlaying = true;
        }
    }

    /// <summary>
    /// 再生を停止
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        frameTimer = 0f;
    }

    /// <summary>
    /// 一時停止（再生状態を保持）
    /// </summary>
    public void Pause()
    {
        isPlaying = false;
    }

    /// <summary>
    /// 次のフレームに進む（コマ送り）
    /// </summary>
    public void NextFrame()
    {
        if (frames.Count == 0) return;

        currentFrameIndex++;
        if (currentFrameIndex >= frames.Count)
        {
            if (loop)
            {
                currentFrameIndex = 0;
            }
            else
            {
                currentFrameIndex = frames.Count - 1;
                Stop();
            }
        }

        if (pointCloudRenderer != null)
        {
            if (hasColorData && currentFrameIndex < frameColors.Count && 
                frameColors[currentFrameIndex].Count == frames[currentFrameIndex].Count)
            {
                pointCloudRenderer.RenderPointsWithColors(frames[currentFrameIndex], frameColors[currentFrameIndex]);
            }
            else
            {
                pointCloudRenderer.RenderPoints(frames[currentFrameIndex]);
            }
        }
    }

    /// <summary>
    /// 前のフレームに戻る（逆コマ送り）
    /// </summary>
    public void PreviousFrame()
    {
        if (frames.Count == 0) return;

        currentFrameIndex--;
        if (currentFrameIndex < 0)
        {
            if (loop)
            {
                currentFrameIndex = frames.Count - 1;
            }
            else
            {
                currentFrameIndex = 0;
            }
        }

        if (pointCloudRenderer != null)
        {
            if (hasColorData && currentFrameIndex < frameColors.Count && 
                frameColors[currentFrameIndex].Count == frames[currentFrameIndex].Count)
            {
                pointCloudRenderer.RenderPointsWithColors(frames[currentFrameIndex], frameColors[currentFrameIndex]);
            }
            else
            {
                pointCloudRenderer.RenderPoints(frames[currentFrameIndex]);
            }
        }
    }

    /// <summary>
    /// 最初のフレームに戻る
    /// </summary>
    public void ResetToFirstFrame()
    {
        currentFrameIndex = 0;
        if (frames.Count > 0 && pointCloudRenderer != null)
        {
            if (hasColorData && frameColors.Count > 0 && frameColors[0].Count == frames[0].Count)
            {
                pointCloudRenderer.RenderPointsWithColors(frames[0], frameColors[0]);
            }
            else
            {
                pointCloudRenderer.RenderPoints(frames[0]);
            }
        }
        Stop();
    }

    /// <summary>
    /// 現在のフレームインデックスを取得
    /// </summary>
    public int GetCurrentFrameIndex()
    {
        return currentFrameIndex;
    }

    /// <summary>
    /// 総フレーム数を取得
    /// </summary>
    public int GetTotalFrames()
    {
        return frames.Count;
    }

    /// <summary>
    /// 再生中かどうかを取得
    /// </summary>
    public bool IsPlaying()
    {
        return isPlaying;
    }

    /// <summary>
    /// フレームレートを設定
    /// </summary>
    public void SetFrameRate(float fps)
    {
        frameRate = Mathf.Max(1f, fps);
    }
}

