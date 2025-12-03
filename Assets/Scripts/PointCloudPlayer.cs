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
    private int currentFrameIndex = 0;
    private bool isPlaying = false;
    private float frameTimer = 0f;
    private PointCloudRenderer renderer;

    void Start()
    {
        renderer = GetComponent<PointCloudRenderer>();
        if (renderer == null)
        {
            renderer = gameObject.AddComponent<PointCloudRenderer>();
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
        currentFrameIndex = 0;
        if (frames.Count > 0)
        {
            renderer.RenderPoints(frames[0]);
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

        renderer.RenderPoints(frames[currentFrameIndex]);
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

        renderer.RenderPoints(frames[currentFrameIndex]);
    }

    /// <summary>
    /// 最初のフレームに戻る
    /// </summary>
    public void ResetToFirstFrame()
    {
        currentFrameIndex = 0;
        if (frames.Count > 0)
        {
            renderer.RenderPoints(frames[0]);
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

