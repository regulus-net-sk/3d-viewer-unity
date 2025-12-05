# Unity プロジェクトセットアップガイド

このガイドでは、Unityプロジェクトを初めて開く際の手順を説明します。

## 前提条件

- Unity Hubがインストールされていること
- Unity 2022.3 LTS以降がインストールされていること（推奨）

## セットアップ手順

### 1. Unity Hubでプロジェクトを開く

1. Unity Hubを起動します
2. 「開く」または「Open」をクリックします
3. このプロジェクトのフォルダ（`3d-viewer`）を選択します
4. Unityエディタが起動します（初回は時間がかかる場合があります）

### 2. シーンの作成

1. Unityエディタで `File > New Scene` を選択
2. 「Basic (Built-in)」を選択して「Create」をクリック
3. `File > Save As` で `Assets/Scenes/MainScene.unity` として保存

### 3. シーンの設定

#### 3.1 カメラの設定

1. Hierarchyで `Main Camera` を選択
2. Inspectorで以下のように設定：
   - Position: (0, 0, -5)
   - Rotation: (0, 0, 0)
3. `Add Component` をクリックして `Camera Controller` スクリプトを追加

#### 3.2 点群オブジェクトの作成

1. Hierarchyで右クリック > `Create Empty`
2. 名前を `PointCloudObject` に変更
3. `Add Component` をクリックして以下を追加：
   - `Point Cloud Player`
   - `Point Cloud Renderer`

#### 3.2.1 床（Y=0）の作成（オプション）

1. Hierarchyで右クリック > `Create Empty`
2. 名前を `Floor` に変更
3. InspectorでPositionを (0, 0, 0) に設定
4. `Add Component` をクリックして以下を追加：
   - `Mesh Filter`（自動的に追加されます）
   - `Mesh Renderer`（自動的に追加されます）
   - `Floor Renderer`
5. Inspectorで `Floor Renderer` コンポーネントの設定を調整（必要に応じて）：
   - **Floor Size**: 床のサイズ（デフォルト: 100）
   - **Grid Spacing**: グリッドの間隔（デフォルト: 1）
   - **Grid Line Width**: グリッド線の太さ（デフォルト: 0.02）
   - **Floor Color**: 床の色（デフォルト: 半透明のグレー）
   - **Grid Color**: グリッド線の色（デフォルト: 半透明のグレー）
   - **Show Grid**: グリッドを表示するか（デフォルト: 有効）
   - **Grid Subdivisions**: 太いグリッド線の間隔（デフォルト: 10）

#### 3.3 UIの作成

1. Hierarchyで右クリック > `UI > Canvas`
2. Canvasを選択し、Inspectorで以下を設定：
   - Render Mode: Screen Space - Overlay
   - Canvas Scaler > UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080

#### 3.4 UI要素の追加

Canvasの子として以下のUI要素を作成：

**ボタン（各ボタンは `GameObject > UI > Button - TextMeshPro` または `Button`）:**

1. **LoadButton** - OBJファイルを読み込む
2. **PlayButton** - 再生
3. **StopButton** - 停止
4. **PauseButton** - 一時停止
5. **NextFrameButton** - 次のフレーム
6. **PreviousFrameButton** - 前のフレーム
7. **ResetButton** - リセット

**その他のUI要素:**

1. **FrameInfoText** - `GameObject > UI > Text - TextMeshPro` または `Text`
   - テキスト: "フレーム: 0 / 0 (停止中)"
   
2. **FrameRateSlider** - `GameObject > UI > Slider`
   - Min Value: 1
   - Max Value: 60
   - Value: 30

3. **FrameRateText** - `GameObject > UI > Text - TextMeshPro` または `Text`
   - テキスト: "フレームレート: 30.0 fps"

4. **FilePathInput** - `GameObject > UI > Input Field - TextMeshPro` または `Input Field`
   - Placeholder: "OBJファイルのパスを入力..."

5. **ShowBoundingBoxToggle** - `GameObject > UI > Toggle` または `GameObject > UI > Toggle - TextMeshPro`
   - **注意**: どちらでも動作しますが、`Toggle`（標準）の方がシンプルです
   - Label: "バウンディングボックス表示"
   - **Toggle作成後の重要な設定**:
     1. Hierarchyで`ShowBoundingBoxToggle`を選択
     2. Inspectorで`Toggle`コンポーネントを確認
     3. **`Graphic`フィールド**: `Checkmark`または`Background`（Imageコンポーネント）を参照していることを確認。Label（Textコンポーネント）を参照している場合は、`Checkmark`または`Background`に変更してください
     4. **`Transition`**: 「None」に設定（Labelの色が透明にならないように）
     5. Hierarchyで`ShowBoundingBoxToggle`を展開し、`Label`を選択
     6. Inspectorで`Text`コンポーネント（または`TextMeshProUGUI`コンポーネント）を確認
     7. **`Color`**: 黒（RGBA: 0, 0, 0, 255）に設定（背景が明るい場合）
     8. **`Font Size`**: 適切なサイズ（例: 14-16）に設定
     9. **`RectTransform`**: 
        - Anchors: (0, 0) to (1, 1)（Toggle全体をカバー）
        - Pivot: (0, 0.5)（左中央）
        - Offset Min: (20, 0)（左から20ピクセル）
        - Offset Max: (-20, 0)（右から20ピクセル）

6. **ShowCenterToggle** - `GameObject > UI > Toggle` または `GameObject > UI > Toggle - TextMeshPro`
   - **注意**: どちらでも動作しますが、`Toggle`（標準）の方がシンプルです
   - Label: "中心点表示"
   - **Toggle作成後の重要な設定**（ShowBoundingBoxToggleと同じ手順）:
     1. Hierarchyで`ShowCenterToggle`を選択
     2. Inspectorで`Toggle`コンポーネントを確認
     3. **`Graphic`フィールド**: `Checkmark`または`Background`（Imageコンポーネント）を参照していることを確認。Label（Textコンポーネント）を参照している場合は、`Checkmark`または`Background`に変更してください
     4. **`Transition`**: 「None」に設定（Labelの色が透明にならないように）
     5. Hierarchyで`ShowCenterToggle`を展開し、`Label`を選択
     6. Inspectorで`Text`コンポーネント（または`TextMeshProUGUI`コンポーネント）を確認
     7. **`Color`**: 黒（RGBA: 0, 0, 0, 255）に設定（背景が明るい場合）
     8. **`Font Size`**: 適切なサイズ（例: 14-16）に設定
     9. **`RectTransform`**: 
        - Anchors: (0, 0) to (1, 1)（Toggle全体をカバー）
        - Pivot: (0, 0.5)（左中央）
        - Offset Min: (20, 0)（左から20ピクセル）
        - Offset Max: (-20, 0)（右から20ピクセル）

#### 3.5 UIManagerの設定

1. Canvasの子として空のGameObjectを作成（`Create Empty`）
2. 名前を `UIManager` に変更
3. `Add Component` をクリックして `UIManager` スクリプトを追加
4. Inspectorで各UI要素をUIManagerのフィールドにドラッグ&ドロップ：
   - Load Button → Load Button
   - Play Button → Play Button
   - Stop Button → Stop Button
   - Pause Button → Pause Button
   - Next Frame Button → Next Frame Button
   - Previous Frame Button → Previous Frame Button
   - Reset Button → Reset Button
   - Frame Info Text → Frame Info Text
   - Frame Rate Slider → Frame Rate Slider
   - Frame Rate Text → Frame Rate Text
   - File Path Input → File Path Input
   - Point Cloud Object → Point Cloud Player

### 4. マテリアルの作成（オプション）

1. `Assets` フォルダで右クリック > `Create > Material`
2. 名前を `PointMaterial` に変更
3. 色やその他のプロパティを設定
4. `PointCloudObject` の `Point Cloud Renderer` コンポーネントの `Point Material` フィールドにドラッグ&ドロップ

### 5. テスト

1. `Play` ボタンをクリックしてエディタで実行
2. File Path InputにOBJファイルのパスを入力
3. Load Buttonをクリックしてファイルを読み込む
4. 点群が表示されることを確認

## トラブルシューティング

### スクリプトのコンパイルエラー

- Unityエディタの `Window > Package Manager` で必要なパッケージがインストールされているか確認
- `Assets > Reimport All` でアセットを再インポート

### UIが表示されない

- Canvasの `Render Mode` が正しく設定されているか確認
- EventSystemが自動的に作成されているか確認（なければ `GameObject > UI > Event System` で作成）

### 点群が表示されない

- `PointCloudObject` に `Point Cloud Renderer` と `Point Cloud Player` がアタッチされているか確認
- OBJファイルのパスが正しいか確認
- Consoleウィンドウでエラーメッセージを確認

### TextMeshProで日本語が表示されない（警告: Unicode character not found）

TextMeshProで日本語を表示するには、日本語フォントアセットを作成する必要があります。

**解決方法1: 日本語フォントアセットを作成する（推奨）**

1. Unityエディタで `Window > TextMeshPro > Font Asset Creator` を開く
2. **Source Font File** に `Assets/Fonts/meiryo.ttc` を設定
3. **Character Set** を `Unicode Range (Hex)` に設定
4. **Unicode Range (Hex)** に `3040-309F,30A0-30FF,4E00-9FAF` を入力（ひらがな、カタカナ、漢字）
5. **Padding** を `5` に設定
6. **Atlas Resolution** を `1024 x 1024` または `2048 x 2048` に設定
7. **Generate Font Atlas** をクリック
8. 生成されたフォントアセットを `Assets/Fonts/` に保存（例: `Meiryo SDF`）
9. 各TextMeshProコンポーネントの **Font Asset** に作成したフォントアセットを設定

**解決方法2: 標準のTextコンポーネントを使用する**

- TextMeshProの代わりに、Unityの標準 `Text` コンポーネントを使用すると、システムフォントで日本語が表示されます
- ボタンやテキストを作成する際に、`Button - TextMeshPro` ではなく `Button` を選択してください

## 次のステップ

- OBJファイルを `Assets/StreamingAssets` フォルダに配置すると、ビルド時に含めることができます
- ファイル選択ダイアログを使用する場合は、`UIManager.cs` にファイル選択機能を追加できます

