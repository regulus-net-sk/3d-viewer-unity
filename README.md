# Unity 3D点群ビューア

UnityとC#で作成されたOBJファイルを読み込んで3D点群を表示・再生するアプリケーションです。

## 機能

- **OBJファイルの読み込み**: OBJファイルから頂点データを読み込み、3D点群として表示
- **描画**: 点群を3D空間に描画
- **再生/停止**: 複数のOBJファイルを時系列で再生・停止
- **コマ送り**: フレーム単位で前後に移動
- **カメラ操作**: マウスでカメラを回転・ズーム・パン

## セットアップ

### 前提条件

- Unity Hubがインストールされていること
- Unity 2022.3 LTS以降がインストールされていること（推奨）

### 1. Unityプロジェクトを開く

1. Unity Hubを起動します
2. 「開く」または「Open」をクリックします
3. このプロジェクトのフォルダ（`3d-viewer`）を選択します
4. Unityエディタが起動します（初回は時間がかかる場合があります）

**注意**: このプロジェクトは既にUnityプロジェクトとして設定されています。新しいプロジェクトを作成する必要はありません。

### 2. シーンの作成と設定

詳細なセットアップ手順は [SETUP.md](SETUP.md) を参照してください。

基本的な手順：

1. Unityエディタで新しいシーンを作成（`File > New Scene`）
2. シーンを `Assets/Scenes/MainScene.unity` として保存

1. Unityエディタで新しいシーンを作成（`File > New Scene`）
2. シーンに以下のGameObjectを追加：
   - **Main Camera**: カメラに`CameraController`スクリプトをアタッチ
   - **PointCloudObject**: 空のGameObjectを作成し、`PointCloudPlayer`と`PointCloudRenderer`スクリプトをアタッチ
   - **Canvas**: UI用のCanvasを作成（`GameObject > UI > Canvas`）

### 3. UIの設定

Canvas内に以下のUI要素を作成：

- **Button** (Load): OBJファイルを読み込む
- **Button** (Play): 再生
- **Button** (Stop): 停止
- **Button** (Pause): 一時停止
- **Button** (Next Frame): 次のフレーム
- **Button** (Previous Frame): 前のフレーム
- **Button** (Reset): 最初のフレームに戻る
- **Text** (Frame Info): フレーム情報を表示
- **Slider** (Frame Rate): フレームレート調整
- **Text** (Frame Rate Text): フレームレート表示
- **InputField** (FilePath): ファイルパス入力

### 4. UIManagerの設定

1. Canvasに空のGameObjectを作成し、`UIManager`スクリプトをアタッチ
2. Inspectorで各UI要素を`UIManager`のフィールドに割り当て
3. `PointCloudPlayer`の参照を設定

### 5. マテリアルの設定（オプション）

`PointCloudRenderer`に点の描画用マテリアルを設定できます。設定しない場合はデフォルトのマテリアルが使用されます。

## 使用方法

### 単一OBJファイルの読み込み

1. `FilePath`入力フィールドにOBJファイルのパスを入力
2. `Load`ボタンをクリック
3. 点群が3D空間に表示されます

### アニメーション（複数OBJファイル）の読み込み

コードから`UIManager.LoadOBJFiles()`メソッドを使用して複数のOBJファイルを読み込むことができます。

```csharp
List<string> filePaths = new List<string>
{
    "path/to/frame1.obj",
    "path/to/frame2.obj",
    "path/to/frame3.obj"
};
uiManager.LoadOBJFiles(filePaths);
```

### 操作

- **マウス左ドラッグ**: カメラを回転
- **マウスホイール**: ズームイン/アウト
- **マウス中ボタンドラッグ**: カメラをパン
- **UIボタン**: 再生、停止、コマ送りなどの操作

## スクリプト説明

### OBJLoader.cs
OBJファイルから頂点データ（`v`で始まる行）を読み込みます。

### PointCloudRenderer.cs
点群をメッシュとして描画します。各点は小さな四角形として表示されます。

### PointCloudPlayer.cs
点群アニメーションの再生、停止、コマ送りを制御します。

### UIManager.cs
UI要素と点群プレイヤーを連携させ、ユーザー操作を処理します。

### CameraController.cs
マウス操作でカメラを制御します。

## 注意事項

- OBJファイルのパスは絶対パスまたはUnityプロジェクト内の相対パスを指定してください
- 大きな点群ファイルを読み込む場合、パフォーマンスに影響する可能性があります
- 現在の実装では、OBJファイルの頂点データ（`v`行）のみを読み込みます。面データ（`f`行）は無視されます
- Unityエディタで初めてプロジェクトを開く際、スクリプトのコンパイルに時間がかかる場合があります

## トラブルシューティング

問題が発生した場合は、[SETUP.md](SETUP.md) のトラブルシューティングセクションを参照してください。

## ライセンス

このプロジェクトは自由に使用・改変できます。

