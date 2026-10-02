# VehicleMeterSimulator

Windows向けの車両メーター・エンジン音シミュレーターです。C#、.NET 10、WPFで構成され、車両定義は `Data/Vehicles/`、画面部品は `Views/`、音声処理は `Services/` に配置しています。

## 必要環境

- Windows
- .NET 10 SDK
- 音声・GUI確認が可能なデスクトップ環境

## Build

```powershell
dotnet restore VehicleMeterSimulator.csproj
dotnet build VehicleMeterSimulator.csproj --no-restore --configuration Release
```

起動する場合:

```powershell
dotnet run --project VehicleMeterSimulator.csproj
```

CIはWindows runnerでrestoreとRelease buildを実行します。WPF表示、入力操作、音声再生はCIでは確認できないため、関連変更ではローカルの手動確認結果をPull Requestへ記録してください。現在、自動test projectはありません。

## Development workflow

標準フローは次のとおりです。

```text
Issue → branch → Codex実装 → build/test → diff確認 → commit → push → PR → CI → review → Squash merge → local main同期
```

詳細は `docs/WORKFLOW.md` と `AGENTS.md` を参照してください。branchは `feature/`、`fix/`、`refactor/`、`docs/`、`chore/` を使用し、`main`へ直接commitしません。

## Assets

音声素材の扱いと第三者素材の記録は `Assets/Sounds/README.md` と `Assets/Sounds/THIRD_PARTY_AUDIO.md` を参照してください。`IncomingAudio/`、`TuningExports/`、`References/`、新しいAudacity projectや生成音声はGitへ追加しません。
