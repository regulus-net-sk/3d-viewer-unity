using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UIを管理するクラス
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI要素")]
    [SerializeField] private Button loadButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button nextFrameButton;
    [SerializeField] private Button previousFrameButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TMP_Text frameInfoText;
    [SerializeField] private Slider frameRateSlider;
    [SerializeField] private TMP_Text frameRateText;
    [SerializeField] private TMP_InputField filePathInput;

    [Header("参照")]
    [SerializeField] private PointCloudPlayer pointCloudPlayer;

    private string lastLoadedPath = "";

    void Start()
    {
        // ボタンのイベントを設定
        if (loadButton != null)
            loadButton.onClick.AddListener(OnLoadButtonClicked);

        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);

        if (stopButton != null)
            stopButton.onClick.AddListener(OnStopButtonClicked);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseButtonClicked);

        if (nextFrameButton != null)
            nextFrameButton.onClick.AddListener(OnNextFrameButtonClicked);

        if (previousFrameButton != null)
            previousFrameButton.onClick.AddListener(OnPreviousFrameButtonClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetButtonClicked);

        if (frameRateSlider != null)
        {
            frameRateSlider.onValueChanged.AddListener(OnFrameRateChanged);
            frameRateSlider.minValue = 1f;
            frameRateSlider.maxValue = 60f;
            frameRateSlider.value = 30f;
        }

        UpdateUI();
    }

    /// <summary>
    /// ファイル読み込みボタンがクリックされた
    /// </summary>
    private void OnLoadButtonClicked()
    {
        string filePath = filePathInput != null && !string.IsNullOrEmpty(filePathInput.text) 
            ? filePathInput.text 
            : lastLoadedPath;

        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogWarning("ファイルパスが指定されていません");
            return;
        }

        LoadOBJFile(filePath);
    }

    /// <summary>
    /// OBJファイルを読み込む
    /// </summary>
    private void LoadOBJFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"ファイルが見つかりません: {filePath}");
            return;
        }

        // 単一ファイルの場合
        List<Vector3> vertices = OBJLoader.LoadVertices(filePath);
        if (vertices.Count > 0)
        {
            List<List<Vector3>> frames = new List<List<Vector3>> { vertices };
            pointCloudPlayer.SetFrames(frames);
            lastLoadedPath = filePath;
            UpdateUI();
        }
    }

    /// <summary>
    /// 複数のOBJファイルを読み込む（アニメーション用）
    /// </summary>
    public void LoadOBJFiles(List<string> filePaths)
    {
        List<List<Vector3>> frames = OBJLoader.LoadAnimationFrames(filePaths);
        if (frames.Count > 0)
        {
            pointCloudPlayer.SetFrames(frames);
            UpdateUI();
        }
    }

    /// <summary>
    /// 再生ボタンがクリックされた
    /// </summary>
    private void OnPlayButtonClicked()
    {
        pointCloudPlayer.Play();
        UpdateUI();
    }

    /// <summary>
    /// 停止ボタンがクリックされた
    /// </summary>
    private void OnStopButtonClicked()
    {
        pointCloudPlayer.Stop();
        UpdateUI();
    }

    /// <summary>
    /// 一時停止ボタンがクリックされた
    /// </summary>
    private void OnPauseButtonClicked()
    {
        pointCloudPlayer.Pause();
        UpdateUI();
    }

    /// <summary>
    /// 次のフレームボタンがクリックされた
    /// </summary>
    private void OnNextFrameButtonClicked()
    {
        pointCloudPlayer.NextFrame();
        UpdateUI();
    }

    /// <summary>
    /// 前のフレームボタンがクリックされた
    /// </summary>
    private void OnPreviousFrameButtonClicked()
    {
        pointCloudPlayer.PreviousFrame();
        UpdateUI();
    }

    /// <summary>
    /// リセットボタンがクリックされた
    /// </summary>
    private void OnResetButtonClicked()
    {
        pointCloudPlayer.ResetToFirstFrame();
        UpdateUI();
    }

    /// <summary>
    /// フレームレートが変更された
    /// </summary>
    private void OnFrameRateChanged(float value)
    {
        pointCloudPlayer.SetFrameRate(value);
        if (frameRateText != null)
        {
            frameRateText.text = $"フレームレート: {value:F1} fps";
        }
    }

    /// <summary>
    /// UIを更新
    /// </summary>
    private void UpdateUI()
    {
        if (frameInfoText != null && pointCloudPlayer != null)
        {
            int current = pointCloudPlayer.GetCurrentFrameIndex() + 1;
            int total = pointCloudPlayer.GetTotalFrames();
            string status = pointCloudPlayer.IsPlaying() ? "再生中" : "停止中";
            frameInfoText.text = $"フレーム: {current} / {total} ({status})";
        }

        // ボタンの有効/無効を更新
        bool hasFrames = pointCloudPlayer != null && pointCloudPlayer.GetTotalFrames() > 0;
        
        if (playButton != null)
            playButton.interactable = hasFrames && !pointCloudPlayer.IsPlaying();
        
        if (stopButton != null)
            stopButton.interactable = hasFrames && pointCloudPlayer.IsPlaying();
        
        if (pauseButton != null)
            pauseButton.interactable = hasFrames && pointCloudPlayer.IsPlaying();
        
        if (nextFrameButton != null)
            nextFrameButton.interactable = hasFrames;
        
        if (previousFrameButton != null)
            previousFrameButton.interactable = hasFrames;
        
        if (resetButton != null)
            resetButton.interactable = hasFrames;
    }

    void Update()
    {
        // UIを定期的に更新
        if (Time.frameCount % 10 == 0) // 10フレームごとに更新
        {
            UpdateUI();
        }
    }
}

