using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

        // ファイルパス表示の初期状態を設定（表示専用）
        if (filePathInput != null)
        {
            filePathInput.interactable = false;
            filePathInput.text = "未ロード";
        }

        UpdateUI();
    }

    /// <summary>
    /// ファイル読み込みボタンがクリックされた
    /// </summary>
    private void OnLoadButtonClicked()
    {
        // ファイル選択ダイアログを表示（Loadボタンからのみロードを行う）
        string selectedPath = ShowFileOrFolderDialog();

        // キャンセルされた場合は何もしない
        if (string.IsNullOrEmpty(selectedPath))
        {
            Debug.Log("ファイル選択がキャンセルされました");
            return;
        }

        // 選択されたパスを処理
        ProcessSelectedPath(selectedPath);
    }

    /// <summary>
    /// ファイルまたはフォルダ選択ダイアログを表示
    /// </summary>
    private string ShowFileOrFolderDialog()
    {
#if UNITY_EDITOR
        // エディタではUnityの標準ダイアログを使用
        // まずファイル選択ダイアログを表示
        string path = EditorUtility.OpenFilePanel("OBJファイルまたはフォルダを選択", lastLoadedPath, "obj");
        
        // ファイルが選択されなかった場合、フォルダ選択を試みる
        if (string.IsNullOrEmpty(path))
        {
            path = EditorUtility.OpenFolderPanel("OBJファイルを含むフォルダを選択", lastLoadedPath, "");
        }
        
        return path;
#else
        // その他のプラットフォームでは入力フィールドを使用
        Debug.LogWarning("このプラットフォームではファイル選択ダイアログがサポートされていません。入力フィールドを使用してください。");
        return "";
#endif
    }

    /// <summary>
    /// 選択されたパスを処理（ファイルまたはフォルダ）
    /// </summary>
    private void ProcessSelectedPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        // 入力フィールドにはファイル名だけを表示（パス入力はさせない）
        if (filePathInput != null)
        {
            filePathInput.text = Path.GetFileName(path);
        }

        // ファイルの場合
        if (File.Exists(path))
        {
            string extension = Path.GetExtension(path).ToLower();
            if (extension == ".obj")
            {
                LoadOBJFile(path);
            }
            else
            {
                Debug.LogWarning($"選択されたファイルはOBJファイルではありません: {path}");
            }
        }
        // フォルダの場合
        else if (Directory.Exists(path))
        {
            LoadOBJFilesFromFolder(path);
        }
        else
        {
            Debug.LogError($"パスが存在しません: {path}");
        }
    }

    /// <summary>
    /// フォルダ内のすべてのOBJファイルを読み込む
    /// </summary>
    private void LoadOBJFilesFromFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"フォルダが見つかりません: {folderPath}");
            return;
        }

        // フォルダ内のすべてのOBJファイルを取得（名前順にソート）
        string[] objFiles = Directory.GetFiles(folderPath, "*.obj", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f)
            .ToArray();

        if (objFiles.Length == 0)
        {
            Debug.LogWarning($"フォルダ内にOBJファイルが見つかりません: {folderPath}");
            return;
        }

        Debug.Log($"フォルダ内のOBJファイル数: {objFiles.Length}");

        // 複数ファイルの場合はアニメーションとして読み込む
        if (objFiles.Length == 1)
        {
            LoadOBJFile(objFiles[0]);
        }
        else
        {
            LoadOBJFiles(objFiles.ToList());
        }
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

