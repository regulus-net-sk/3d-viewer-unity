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
    [SerializeField] private Button nextFrameButton;
    [SerializeField] private Button previousFrameButton;
    [SerializeField] private Button viewResetButton; // ビューリセットボタン
    [SerializeField] private TMP_Text frameInfoText;
    [SerializeField] private Slider frameRateSlider;
    [SerializeField] private TMP_Text frameRateText;
    [SerializeField] private TMP_InputField filePathInput;
    [SerializeField] private Toggle showBoundingBoxToggle; // バウンディングボックス表示トグル
    [SerializeField] private Toggle showCenterToggle; // 中心点表示トグル

    [Header("参照")]
    [SerializeField] private PointCloudPlayer pointCloudPlayer;
    [SerializeField] private CameraController cameraController; // カメラコントローラーへの参照

    private PointCloudRenderer pointCloudRenderer; // 点群レンダラー
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

        if (nextFrameButton != null)
            nextFrameButton.onClick.AddListener(OnNextFrameButtonClicked);

        if (previousFrameButton != null)
            previousFrameButton.onClick.AddListener(OnPreviousFrameButtonClicked);

        if (viewResetButton != null)
            viewResetButton.onClick.AddListener(OnViewResetButtonClicked);

        // フレームレート固定のため、スライダーは非表示
        if (frameRateSlider != null)
        {
            frameRateSlider.gameObject.SetActive(false);
        }
        
        if (frameRateText != null)
        {
            frameRateText.gameObject.SetActive(false);
        }

        // ファイルパス表示の初期状態を設定（表示専用）
        if (filePathInput != null)
        {
            filePathInput.interactable = false;
            filePathInput.text = "未ロード";
        }

        // 点群レンダラーを取得（pointCloudPlayerから取得）
        if (pointCloudPlayer != null)
        {
            pointCloudRenderer = pointCloudPlayer.GetComponent<PointCloudRenderer>();
            if (pointCloudRenderer == null)
            {
                pointCloudRenderer = pointCloudPlayer.gameObject.AddComponent<PointCloudRenderer>();
            }
        }

        // トグルのイベントを設定
        if (showBoundingBoxToggle != null)
        {
            showBoundingBoxToggle.isOn = pointCloudRenderer != null ? pointCloudRenderer.GetShowBoundingBox() : true;
            showBoundingBoxToggle.onValueChanged.AddListener(OnShowBoundingBoxToggleChanged);
        }

        if (showCenterToggle != null)
        {
            showCenterToggle.isOn = pointCloudRenderer != null ? pointCloudRenderer.GetShowCenter() : true;
            showCenterToggle.onValueChanged.AddListener(OnShowCenterToggleChanged);
        }

        // ラベルの色を設定（遅延実行で確実に設定）
        StartCoroutine(SetToggleLabelsColorDelayed());

        // Toggleの位置をView Resetボタンの下に配置
        PositionTogglesBelowViewResetButton();

        UpdateUI();
    }

    /// <summary>
    /// ToggleをView Resetボタンの下に配置
    /// </summary>
    private void PositionTogglesBelowViewResetButton()
    {
        if (viewResetButton == null) return;

        RectTransform viewResetRect = viewResetButton.GetComponent<RectTransform>();
        if (viewResetRect == null) return;

        // View Resetボタンの位置を取得
        float viewResetBottom = viewResetRect.anchoredPosition.y - viewResetRect.sizeDelta.y / 2f;
        float spacing = 10f; // Toggle間の間隔
        float toggleHeight = 20f; // Toggleの高さ

        // ShowBoundingBoxToggleの位置を設定
        if (showBoundingBoxToggle != null)
        {
            RectTransform toggleRect = showBoundingBoxToggle.GetComponent<RectTransform>();
            if (toggleRect != null)
            {
                // View Resetボタンと同じX位置、下に配置
                toggleRect.anchoredPosition = new Vector2(
                    viewResetRect.anchoredPosition.x,
                    viewResetBottom - spacing - toggleHeight / 2f
                );
            }
        }

        // ShowCenterToggleの位置を設定（ShowBoundingBoxToggleの下）
        if (showCenterToggle != null && showBoundingBoxToggle != null)
        {
            RectTransform toggleRect = showCenterToggle.GetComponent<RectTransform>();
            RectTransform boundingBoxRect = showBoundingBoxToggle.GetComponent<RectTransform>();
            if (toggleRect != null && boundingBoxRect != null)
            {
                float boundingBoxBottom = boundingBoxRect.anchoredPosition.y - boundingBoxRect.sizeDelta.y / 2f;
                toggleRect.anchoredPosition = new Vector2(
                    viewResetRect.anchoredPosition.x,
                    boundingBoxBottom - spacing - toggleHeight / 2f
                );
            }
        }
    }

    /// <summary>
    /// Toggleのラベル色を遅延実行で設定（初期化を待つ）
    /// </summary>
    private System.Collections.IEnumerator SetToggleLabelsColorDelayed()
    {
        // 1フレーム待つ（UIの初期化を待つ）
        yield return null;
        
        // ラベルの色を設定
        if (showBoundingBoxToggle != null)
        {
            SetToggleLabelColor(showBoundingBoxToggle, Color.white);
        }
        if (showCenterToggle != null)
        {
            SetToggleLabelColor(showCenterToggle, Color.white);
        }
        
        // さらに1フレーム待って再設定（確実に設定するため）
        yield return null;
        
        if (showBoundingBoxToggle != null)
        {
            SetToggleLabelColor(showBoundingBoxToggle, Color.white);
        }
        if (showCenterToggle != null)
        {
            SetToggleLabelColor(showCenterToggle, Color.white);
        }
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
        // まずファイル選択ダイアログを表示（単一ファイル用）
        string path = EditorUtility.OpenFilePanel("OBJファイルを選択（またはフォルダを選択する場合はキャンセル）", lastLoadedPath, "obj");
        
        // ファイルが選択されなかった場合、フォルダ選択ダイアログを表示（複数ファイルのアニメーション用）
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

        // 単一ファイルの場合（色情報も読み込む）
        List<OBJLoader.VertexWithColor> verticesWithColors = OBJLoader.LoadVerticesWithColors(filePath);
        if (verticesWithColors.Count > 0)
        {
            // 色情報が含まれているかチェック
            bool hasColorData = verticesWithColors.Any(v => v.hasColor);
            
            // 頂点と色を分離
            List<Vector3> vertices = new List<Vector3>();
            List<Color> colors = new List<Color>();
            foreach (var v in verticesWithColors)
            {
                vertices.Add(v.position);
                if (hasColorData)
                {
                    colors.Add(v.color);
                }
            }
            
            // 点群を描画（色情報がある場合は使用）
            if (hasColorData && colors.Count == vertices.Count)
            {
                pointCloudRenderer.RenderPointsWithColors(vertices, colors);
                Debug.Log($"点群を読み込み: {vertices.Count}点（色情報あり）");
            }
            else
            {
                pointCloudRenderer.RenderPoints(vertices);
                Debug.Log($"点群を読み込み: {vertices.Count}点（色情報なし）");
            }
            
            // アニメーションフレームとして設定（色情報も含む）
            List<List<Vector3>> frames = new List<List<Vector3>> { vertices };
            if (hasColorData && colors.Count == vertices.Count)
            {
                List<List<Color>> frameColors = new List<List<Color>> { colors };
                pointCloudPlayer.SetFramesWithColors(frames, frameColors);
            }
            else
            {
                pointCloudPlayer.SetFrames(frames);
            }
            lastLoadedPath = filePath;
            UpdateUI();
        }
    }

    /// <summary>
    /// 複数のOBJファイルを読み込む（アニメーション用、色情報付き）
    /// </summary>
    public void LoadOBJFiles(List<string> filePaths)
    {
        if (filePaths == null || filePaths.Count == 0)
        {
            Debug.LogWarning("読み込むファイルが指定されていません");
            return;
        }

        Debug.Log($"複数OBJファイルを読み込み開始: {filePaths.Count}ファイル");

        // 色情報付きで読み込む
        var (frames, frameColors, hasColorData) = OBJLoader.LoadAnimationFramesWithColors(filePaths);
        
        if (frames.Count > 0)
        {
            // 最初のフレームを表示
            if (hasColorData && frameColors.Count > 0 && frameColors[0].Count == frames[0].Count)
            {
                pointCloudRenderer.RenderPointsWithColors(frames[0], frameColors[0]);
                pointCloudPlayer.SetFramesWithColors(frames, frameColors);
                Debug.Log($"アニメーションを読み込み: {frames.Count}フレーム（色情報あり）");
            }
            else
            {
                pointCloudRenderer.RenderPoints(frames[0]);
                pointCloudPlayer.SetFrames(frames);
                Debug.Log($"アニメーションを読み込み: {frames.Count}フレーム（色情報なし）");
            }
            
            lastLoadedPath = Path.GetDirectoryName(filePaths[0]);
            UpdateUI();
        }
        else
        {
            Debug.LogError("フレームデータの読み込みに失敗しました");
        }
    }

    /// <summary>
    /// 再生/一時停止ボタンがクリックされた（トグル）
    /// </summary>
    private void OnPlayButtonClicked()
    {
        if (pointCloudPlayer.IsPlaying())
        {
            // 再生中なら一時停止
            pointCloudPlayer.Pause();
        }
        else
        {
            // 停止中なら再生
            pointCloudPlayer.Play();
        }
        UpdateUI();
    }

    /// <summary>
    /// 停止ボタンがクリックされた（最初のフレームに戻る）
    /// </summary>
    private void OnStopButtonClicked()
    {
        pointCloudPlayer.ResetToFirstFrame();
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
    /// ビューリセットボタンがクリックされた
    /// </summary>
    private void OnViewResetButtonClicked()
    {
        // CameraControllerを取得（参照が設定されていない場合）
        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
        }

        if (cameraController != null)
        {
            cameraController.ResetToDefaultPosition();
        }
        else
        {
            Debug.LogWarning("CameraControllerが見つかりません。");
        }
    }

    /// <summary>
    /// バウンディングボックス表示トグルが変更された
    /// </summary>
    private void OnShowBoundingBoxToggleChanged(bool value)
    {
        if (pointCloudRenderer != null)
        {
            pointCloudRenderer.SetShowBoundingBox(value);
        }
    }

    /// <summary>
    /// 中心点表示トグルが変更された
    /// </summary>
    private void OnShowCenterToggleChanged(bool value)
    {
        if (pointCloudRenderer != null)
        {
            pointCloudRenderer.SetShowCenter(value);
        }
    }

    /// <summary>
    /// Toggleのラベルテキストの色を設定（透明にならないように）
    /// </summary>
    private void SetToggleLabelColor(Toggle toggle, Color color)
    {
        if (toggle == null) return;

        // ToggleコンポーネントのtargetGraphicとgraphicがTextコンポーネントを参照している場合、それを変更
        // TextコンポーネントはColor Transitionの影響を受けないようにする
        UnityEngine.UI.Text labelText = toggle.GetComponentInChildren<UnityEngine.UI.Text>();
        TextMeshProUGUI labelTextTMP = toggle.GetComponentInChildren<TextMeshProUGUI>();
        
        // 背景画像を探す（Imageコンポーネント）
        UnityEngine.UI.Image backgroundImage = toggle.GetComponent<UnityEngine.UI.Image>();
        
        // targetGraphicがTextコンポーネントを参照している場合は、背景画像に変更
        if (toggle.targetGraphic != null && 
            (toggle.targetGraphic == labelText || toggle.targetGraphic == labelTextTMP))
        {
            if (backgroundImage != null)
            {
                toggle.targetGraphic = backgroundImage;
                Debug.Log($"ToggleのtargetGraphicをTextからImageに変更: {toggle.name}");
            }
        }
        
        // graphicプロパティも確認（Toggleコンポーネントの内部プロパティ）
        // graphicがTextコンポーネントを参照している場合も変更
        if (backgroundImage != null && labelText != null && toggle.graphic == labelText)
        {
            toggle.graphic = backgroundImage;
            Debug.Log($"ToggleのgraphicをTextからImageに変更: {toggle.name}");
        }
        if (backgroundImage != null && labelTextTMP != null && toggle.graphic == labelTextTMP)
        {
            toggle.graphic = backgroundImage;
            Debug.Log($"ToggleのgraphicをTextMeshProUGUIからImageに変更: {toggle.name}");
        }
        
        // すべての子要素を再帰的に検索
        SetLabelColorRecursive(toggle.transform, color);
        
        // Toggleコンポーネント自体のColor Transition設定も確認
        ColorBlock colors = toggle.colors;
        Color normalColor = colors.normalColor;
        normalColor.a = 1.0f;
        colors.normalColor = normalColor;
        colors.highlightedColor = new Color(colors.highlightedColor.r, colors.highlightedColor.g, colors.highlightedColor.b, 1.0f);
        colors.pressedColor = new Color(colors.pressedColor.r, colors.pressedColor.g, colors.pressedColor.b, 1.0f);
        colors.selectedColor = new Color(colors.selectedColor.r, colors.selectedColor.g, colors.selectedColor.b, 1.0f);
        colors.disabledColor = new Color(colors.disabledColor.r, colors.disabledColor.g, colors.disabledColor.b, 1.0f);
        toggle.colors = colors;
        
        // Color Transitionを無効化（Noneに設定）
        // ただし、これは実行時には変更できないので、Inspectorで設定する必要がある
    }

    /// <summary>
    /// 再帰的にラベルテキストの色を設定
    /// </summary>
    private void SetLabelColorRecursive(Transform parent, Color color)
    {
        if (parent == null) return;

        // アルファ値を確実に1.0に設定
        Color textColor = color;
        textColor.a = 1.0f;

        // TextMeshProUGUIを検索
        TextMeshProUGUI labelText = parent.GetComponent<TextMeshProUGUI>();
        if (labelText != null)
        {
            // GameObjectがアクティブか確認
            if (!labelText.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"TextMeshProUGUIラベルのGameObjectが非アクティブです: {parent.name}");
                labelText.gameObject.SetActive(true);
            }
            
            labelText.color = textColor;
            CanvasRenderer canvasRenderer = labelText.GetComponent<CanvasRenderer>();
            if (canvasRenderer != null)
            {
                canvasRenderer.SetColor(textColor);
                // Cull Transparent Meshを無効化（透明なメッシュをカリングしない）
                canvasRenderer.cullTransparentMesh = false;
            }
            // 強制的に再描画
            labelText.SetAllDirty();
            
            Debug.Log($"TextMeshProUGUIラベルの色を設定: {parent.name}, Color={textColor}, Alpha={textColor.a}, Active={labelText.gameObject.activeInHierarchy}, Enabled={labelText.enabled}");
        }

        // 通常のTextを検索
        UnityEngine.UI.Text labelTextLegacy = parent.GetComponent<UnityEngine.UI.Text>();
        if (labelTextLegacy != null)
        {
            // GameObjectがアクティブか確認
            if (!labelTextLegacy.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"TextラベルのGameObjectが非アクティブです: {parent.name}");
                labelTextLegacy.gameObject.SetActive(true);
            }
            
            // Color Transitionの影響を受けないように、直接色を設定
            labelTextLegacy.color = textColor;
            // Canvas Rendererの設定も確認（Textコンポーネント用）
            CanvasRenderer canvasRenderer = labelTextLegacy.GetComponent<CanvasRenderer>();
            if (canvasRenderer != null)
            {
                canvasRenderer.SetColor(textColor);
                // Cull Transparent Meshを無効化（透明なメッシュをカリングしない）
                canvasRenderer.cullTransparentMesh = false;
            }
            // 強制的に再描画
            labelTextLegacy.SetAllDirty();
            
            // 親のToggleコンポーネントを取得して、targetGraphicを変更
            Toggle parentToggle = parent.GetComponentInParent<Toggle>();
            if (parentToggle != null && parentToggle.targetGraphic == labelTextLegacy)
            {
                // targetGraphicを背景画像に変更（TextコンポーネントはColor Transitionの影響を受けないように）
                UnityEngine.UI.Image bgImage = parentToggle.GetComponent<UnityEngine.UI.Image>();
                if (bgImage != null)
                {
                    parentToggle.targetGraphic = bgImage;
                }
            }
            
            Debug.Log($"Textラベルの色を設定: {parent.name}, Color={textColor}, Alpha={textColor.a}, Active={labelTextLegacy.gameObject.activeInHierarchy}, Enabled={labelTextLegacy.enabled}");
        }

        // すべての子要素を再帰的に処理
        foreach (Transform child in parent)
        {
            SetLabelColorRecursive(child, color);
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
        bool isPlaying = pointCloudPlayer != null && pointCloudPlayer.IsPlaying();
        
        if (playButton != null)
        {
            playButton.interactable = hasFrames;
            // ボタンのテキストを更新（再生中は「Pause」、停止中は「Play」）
            TMP_Text buttonText = playButton.GetComponentInChildren<TMP_Text>();
            if (buttonText == null)
            {
                UnityEngine.UI.Text buttonTextLegacy = playButton.GetComponentInChildren<UnityEngine.UI.Text>();
                if (buttonTextLegacy != null)
                {
                    buttonTextLegacy.text = isPlaying ? "Pause" : "Play";
                }
            }
            else
            {
                buttonText.text = isPlaying ? "Pause" : "Play";
            }
        }
        
        if (stopButton != null)
            stopButton.interactable = hasFrames;
        
        if (nextFrameButton != null)
            nextFrameButton.interactable = hasFrames;
        
        if (previousFrameButton != null)
            previousFrameButton.interactable = hasFrames;
    }

    void Update()
    {
        // UIを定期的に更新
        if (Time.frameCount % 10 == 0) // 10フレームごとに更新
        {
            UpdateUI();
        }

        // Toggleのラベル色を定期的に確認・修正（透明にならないように）
        // 毎フレーム設定して、ToggleのColor Transitionの影響を上書き
        if (showBoundingBoxToggle != null)
        {
            // 直接Textコンポーネントの色を設定（Toggleの影響を受けないように）
            UnityEngine.UI.Text labelText = showBoundingBoxToggle.GetComponentInChildren<UnityEngine.UI.Text>();
            if (labelText != null)
            {
                // GameObjectが非アクティブの場合は有効化
                if (!labelText.gameObject.activeInHierarchy)
                {
                    labelText.gameObject.SetActive(true);
                    Debug.LogWarning($"ShowBoundingBoxToggleのLabelが非アクティブでした。有効化しました。");
                }
                
                // 背景が明るい場合、テキストは黒にする
                Color textColor = Color.black;
                textColor.a = 1.0f;
                labelText.color = textColor;
                
                // Canvas Rendererの色も設定
                CanvasRenderer canvasRenderer = labelText.GetComponent<CanvasRenderer>();
                if (canvasRenderer != null)
                {
                    canvasRenderer.SetColor(textColor);
                    canvasRenderer.cullTransparentMesh = false;
                }
                
                // フォントサイズが0でないか確認し、必要に応じて設定
                if (labelText.fontSize <= 0)
                {
                    labelText.fontSize = 14; // デフォルトサイズを設定
                }
                
                // 強制的に再描画
                labelText.SetAllDirty();
                
            }
            else
            {
                // Labelが見つからない場合のデバッグ
                if (Time.frameCount % 60 == 0)
                {
                    Debug.LogWarning($"ShowBoundingBoxToggleのLabel（Textコンポーネント）が見つかりません。");
                }
            }
            
            TextMeshProUGUI labelTextTMP = showBoundingBoxToggle.GetComponentInChildren<TextMeshProUGUI>();
            if (labelTextTMP != null)
            {
                // GameObjectが非アクティブの場合は有効化
                if (!labelTextTMP.gameObject.activeInHierarchy)
                {
                    labelTextTMP.gameObject.SetActive(true);
                    Debug.LogWarning($"ShowBoundingBoxToggleのLabel（TextMeshProUGUI）が非アクティブでした。有効化しました。");
                }
                
                // 背景が明るい場合、テキストは黒にする
                Color textColor = Color.black;
                textColor.a = 1.0f;
                labelTextTMP.color = textColor;
                
                // Canvas Rendererの色も設定
                CanvasRenderer canvasRenderer = labelTextTMP.GetComponent<CanvasRenderer>();
                if (canvasRenderer != null)
                {
                    canvasRenderer.SetColor(textColor);
                    canvasRenderer.cullTransparentMesh = false;
                }
                // 強制的に再描画
                labelTextTMP.SetAllDirty();
                
                // デバッグ情報
                if (Time.frameCount % 60 == 0)
                {
                    RectTransform rectTransform = labelTextTMP.GetComponent<RectTransform>();
                    Debug.Log($"ShowBoundingBoxToggle Label (TMP): Active={labelTextTMP.gameObject.activeInHierarchy}, Enabled={labelTextTMP.enabled}, Color={labelTextTMP.color}, Text='{labelTextTMP.text}'");
                    Debug.Log($"  RectTransform: Anchors=({rectTransform.anchorMin.x}, {rectTransform.anchorMin.y}) to ({rectTransform.anchorMax.x}, {rectTransform.anchorMax.y}), Pivot=({rectTransform.pivot.x}, {rectTransform.pivot.y})");
                    Debug.Log($"  CanvasRenderer: CullTransparentMesh={canvasRenderer?.cullTransparentMesh}");
                    
                    // Canvas RendererのCull Transparent Meshを強制的に無効化
                    if (canvasRenderer != null && canvasRenderer.cullTransparentMesh)
                    {
                        canvasRenderer.cullTransparentMesh = false;
                        Debug.LogWarning($"ShowBoundingBoxToggleのLabel（TMP）のCanvasRendererのCullTransparentMeshが有効でした。無効化しました。");
                    }
                }
            }
        }
        if (showCenterToggle != null)
        {
            // 直接Textコンポーネントの色を設定（Toggleの影響を受けないように）
            UnityEngine.UI.Text labelText = showCenterToggle.GetComponentInChildren<UnityEngine.UI.Text>();
            if (labelText != null)
            {
                // GameObjectが非アクティブの場合は有効化
                if (!labelText.gameObject.activeInHierarchy)
                {
                    labelText.gameObject.SetActive(true);
                    Debug.LogWarning($"ShowCenterToggleのLabelが非アクティブでした。有効化しました。");
                }
                
                // 背景が明るい場合、テキストは黒にする
                Color textColor = Color.black;
                textColor.a = 1.0f;
                labelText.color = textColor;
                
                // Canvas Rendererの色も設定
                CanvasRenderer canvasRenderer = labelText.GetComponent<CanvasRenderer>();
                if (canvasRenderer != null)
                {
                    canvasRenderer.SetColor(textColor);
                    canvasRenderer.cullTransparentMesh = false;
                }
                
                // フォントサイズが0でないか確認し、必要に応じて設定
                if (labelText.fontSize <= 0)
                {
                    labelText.fontSize = 14; // デフォルトサイズを設定
                }
                
                // 強制的に再描画
                labelText.SetAllDirty();
            }
            else
            {
                // Labelが見つからない場合のデバッグ
                if (Time.frameCount % 60 == 0)
                {
                    Debug.LogWarning($"ShowCenterToggleのLabel（Textコンポーネント）が見つかりません。");
                }
            }
            
            TextMeshProUGUI labelTextTMP = showCenterToggle.GetComponentInChildren<TextMeshProUGUI>();
            if (labelTextTMP != null)
            {
                // GameObjectが非アクティブの場合は有効化
                if (!labelTextTMP.gameObject.activeInHierarchy)
                {
                    labelTextTMP.gameObject.SetActive(true);
                    Debug.LogWarning($"ShowCenterToggleのLabel（TextMeshProUGUI）が非アクティブでした。有効化しました。");
                }
                
                // 背景が明るい場合、テキストは黒にする
                Color textColor = Color.black;
                textColor.a = 1.0f;
                labelTextTMP.color = textColor;
                
                // Canvas Rendererの色も設定
                CanvasRenderer canvasRenderer = labelTextTMP.GetComponent<CanvasRenderer>();
                if (canvasRenderer != null)
                {
                    canvasRenderer.SetColor(textColor);
                    canvasRenderer.cullTransparentMesh = false;
                }
                // 強制的に再描画
                labelTextTMP.SetAllDirty();
            }
        }
    }
}

