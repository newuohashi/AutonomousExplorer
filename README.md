# AutonomousExplorer
ゲーム

## Unity CLI・Pipeline

Unityプロジェクトはこのリポジトリのルートです。Unity 6000.3.8f1と
`com.unity.pipeline` 1.0.1を使用します。

リポジトリのルートで以下を実行すると、Editorを開いて接続を確認できます。
`unity`が見つからない場合はUnity CLIをインストールし、ターミナルを開き直してください。

```powershell
unity open .
unity status --until-ready --project-path . --format json
unity command --project-path . editor status --format json
unity command --project-path . console status --format json
```

複数のEditorを開いている場合も、`--project-path`でこのプロジェクトを指定してください。
別の作業ディレクトリから実行する場合は、`.`をこのリポジトリの絶対パスに置き換えます。

バックグラウンドでCLI操作を続ける場合は、Editorの自動tickを有効にできます。
この設定はEditorセッション内で有効です。

```powershell
unity command --project-path . editor set_auto_tick --enabled true
```

`STATUS_PIPELINE_LOAD_PENDING`が表示される場合は、Editorに切り替えてパッケージの
インポート・コンパイル完了を待ってください。Auto Refreshが無効な場合、以降の
スクリプト変更は手動Refreshまたは`unity recompile --project-path .`で反映します。
サーバーを手動で開始する場合は、Editorの`Window > Pipeline > Start Server`を使用します。
