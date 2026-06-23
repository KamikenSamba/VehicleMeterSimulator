# VehicleMeterSimulator 開発進捗レポート

作成日時: 2026-06-23 JST  
調査対象ルート: `D:\VehicleMeterSimulator`  
ビルド結果: `dotnet build` 成功。警告0、エラー0。  
Git変更状況: 未コミット変更多数あり。既存変更と未追跡ファイルが混在。

## 1. プロジェクト概要

- 使用技術: C# / WPF / .NET / JSON車種データ / NAudio / System.Windows.Media.MediaPlayer
- .NETバージョン: `net10.0-windows`
- WPFプロジェクト名: `VehicleMeterSimulator`
- 車種数: 3台
  - `Lexus LFA`
  - `Lexus LC500 Coupe`
  - `Nissan Note e-POWER E13`
- 現在の主な開発対象: `Nissan Note e-POWER E13` 専用メーター、E13専用音声、LFA/LC500向けRPM連動エンジン音

`.csproj` の主な設定:

- `OutputType`: `WinExe`
- `TargetFramework`: `net10.0-windows`
- `UseWPF`: `true`
- `Nullable`: `enable`
- NuGet: `NAudio` Version `2.3.0`
- `Data\Vehicles\*.json` は出力先へ `PreserveNewest` コピー
- `Assets\Sounds\**\*.*` は出力先へ `PreserveNewest` コピー

## 2. 現在のファイル構成

主要ファイルのみ:

```text
VehicleMeterSimulator
├─ App.xaml
├─ MainWindow.xaml
├─ MainWindow.xaml.cs
├─ VehicleMeterSimulator.csproj
├─ PROJECT_STATUS.md
├─ Data
│  └─ Vehicles
│     ├─ lexus-lfa.json
│     ├─ lexus-lc500.json
│     └─ nissan-note-e13-epower.json
├─ Models
│  ├─ AudioTuningSession.cs
│  ├─ DrivingModeProfile.cs
│  ├─ EngineAudioDebugInfo.cs
│  ├─ EngineAudioLayerDebugInfo.cs
│  ├─ EngineAudioLayerProfile.cs
│  ├─ NissanNoteE13RuntimeState.cs
│  ├─ NoteE13AudioProfile.cs
│  ├─ VehicleAudioProfile.cs
│  ├─ VehicleProfile.cs
│  └─ VehicleRuntimeState.cs
├─ Services
│  ├─ AudioService.cs
│  ├─ AudioTuningExportService.cs
│  ├─ EngineAudioService.cs
│  ├─ NoteE13AudioService.cs
│  └─ VehicleRepository.cs
├─ Views
│  ├─ MeterWindow.xaml
│  ├─ MeterWindow.xaml.cs
│  └─ Controls
│     ├─ AudioTuningPanel.xaml
│     ├─ CircularTachometer.xaml
│     ├─ IndicatorPanel.xaml
│     ├─ LexusLfa
│     │  ├─ LfaInstrumentCluster.xaml
│     │  └─ LfaWarningLampLayer.xaml
│     └─ NissanNoteE13
│        ├─ NoteE13InstrumentCluster.xaml
│        └─ NoteE13WarningLampLayer.xaml
├─ Assets
│  └─ Sounds
│     ├─ README.md
│     ├─ THIRD_PARTY_AUDIO.md
│     ├─ E13 Note e-POWER
│     │  ├─ Startup.wav
│     │  ├─ SystemStop.wav
│     │  ├─ Seatbelt.wav
│     │  ├─ signal.wav
│     │  └─ Reverse.wav
│     ├─ LexusLfa
│     │  └─ engine-loop.wav
│     ├─ LexusLc500
│     │  └─ engine-loop.wav
│     └─ NissanNoteE13
│        ├─ turn-signal-loop.wav
│        └─ reverse-loop.wav
└─ Materials
   └─ E13 Note e-POEWR
      ├─ note-epower-gazou-01737.jpg
      └─ スクリーンショット 2026-06-02 231154.png
```

## 3. 車種ごとの実装状況

### Lexus LFA

| 機能 | 状態 | 実装場所 | 備考 |
|---|---|---|---|
| JSON車種定義 | 実装済み | `Data/Vehicles/lexus-lfa.json` | `meterStyleId = lfa-inspired` |
| 車種選択 | 実装済み | `MainWindow.xaml.cs`, `VehicleRepository.cs` | JSONから読み込み |
| 専用メーター | 実装済み | `Views/Controls/LexusLfa/LfaInstrumentCluster.xaml(.cs)` | `MeterWindow` がLFA判定で表示 |
| 汎用タコメーター連携 | 不要 | `CircularTachometer.xaml(.cs)` | LFAは専用クラスター優先 |
| メータースイープ | 実装済み | `MeterWindow.xaml.cs`, `LfaInstrumentCluster.xaml.cs` | `I` キー後に表示用RPMでスイープ |
| 警告灯・表示灯 | 実装済み | `LfaWarningLampLayer.xaml(.cs)` | LFA用レイヤーあり |
| 前進/後退走行 | 実装済み | `VehicleRuntimeState.cs` | 6速、R、Pブレーキ対応 |
| MANUAL/AUTO変速 | 実装済み | `VehicleRuntimeState.cs`, `lexus-lfa.json` | `manual`, `automatic` |
| NORMAL/SPORT | 実装済み | `DrivingModeProfile.cs`, `VehicleRuntimeState.cs`, JSON | `normal`, `sport` |
| SHIFT UP | 実装済み | `MeterWindow.xaml.cs`, `IndicatorPanel.xaml.cs`, LFAクラスター | AUTO時は非表示 |
| イベント音 | 一部実装 | `AudioService.cs`, JSON | JSONパスはあるが、イベント音WAVの多くは実ファイル未配置 |
| RPM連動エンジン音 | 一部実装 | `EngineAudioService.cs`, JSON | `engine-loop.wav` は存在。多層用 `idle/low/mid/high` はJSON設定ありだが実ファイル未確認/未配置 |
| 音声デバッグ/チューニング | 実装済み | `AudioTuningPanel.xaml(.cs)`, `AudioTuningSession.cs` | Note E13では無効メッセージ |

### Lexus LC500 Coupe

| 機能 | 状態 | 実装場所 | 備考 |
|---|---|---|---|
| JSON車種定義 | 実装済み | `Data/Vehicles/lexus-lc500.json` | `meterStyleId = generic-sport` |
| 車種選択 | 実装済み | `MainWindow.xaml.cs`, `VehicleRepository.cs` | JSONから読み込み |
| 汎用スポーツメーター | 実装済み | `CircularTachometer.xaml(.cs)` | 車種名と `GENERIC SPORT DISPLAY` 表示 |
| 最大RPM可変目盛り | 実装済み | `CircularTachometer.xaml.cs` | `MaxRpm = 8000` に対応 |
| 10速対応 | 実装済み | `VehicleRuntimeState.cs`, JSON | `ForwardGearCount = 10` |
| メータースイープ | 実装済み | `MeterWindow.xaml.cs`, `CircularTachometer.xaml.cs` | `MaxRpm` に基づく |
| 警告灯 | 実装済み | `IndicatorPanel.xaml(.cs)` | CHECK/ABS/OIL/BATTERY/BRAKE/SHIFT UP |
| 前進/後退走行 | 実装済み | `VehicleRuntimeState.cs` | 10速、R、Pブレーキ対応 |
| MANUAL/AUTO変速 | 実装済み | `VehicleRuntimeState.cs`, `lexus-lc500.json` | `manual`, `automatic` |
| NORMAL/SPORT | 実装済み | `DrivingModeProfile.cs`, `VehicleRuntimeState.cs`, JSON | `normal`, `sport` |
| SHIFT UP | 実装済み | `MeterWindow.xaml.cs`, `IndicatorPanel.xaml.cs` | `ShiftUpIndicatorRpm` はモード別 |
| イベント音 | 一部実装 | `AudioService.cs`, JSON | JSONパスはあるが、イベント音WAVの多くは実ファイル未配置 |
| RPM連動エンジン音 | 一部実装 | `EngineAudioService.cs`, JSON | `engine-loop.wav` は存在。多層用 `idle/low/mid/high` はJSON設定ありだが実ファイル未確認/未配置 |

### Nissan Note e-POWER E13

| 機能 | 状態 | 実装場所 | 備考 |
|---|---|---|---|
| JSON車種定義 | 実装済み | `Data/Vehicles/nissan-note-e13-epower.json` | `meterStyleId = note-e13-authentic` |
| 車種選択 | 実装済み | `MainWindow.xaml.cs`, `VehicleRepository.cs` | 電動車としてスペック表示分岐 |
| 専用メーター画面 | 実装済み | `NoteE13InstrumentCluster.xaml(.cs)`, `MeterWindow.xaml.cs` | `UsesElectricPowerMeter` で表示 |
| 左側パワーメーター | 実装済み | `NoteE13InstrumentCluster.xaml(.cs)` | POWER/CHARGEの針と数値 |
| 右側デジタル速度表示 | 実装済み | `NoteE13InstrumentCluster.xaml(.cs)` | `SpeedText` |
| 燃料計 | 実装済み | `NoteE13InstrumentCluster.xaml(.cs)` | `FuelLevelBar` |
| 航続可能距離 | 実装済み | `NissanNoteE13RuntimeState.cs`, `NoteE13InstrumentCluster.xaml.cs` | `EstimatedRangeKm` |
| ODO/TRIP | 実装済み | `NissanNoteE13RuntimeState.cs`, `NoteE13InstrumentCluster.xaml.cs` | 走行距離から更新 |
| 時計と外気温 | 実装済み | `NoteE13InstrumentCluster.xaml.cs` | 時計は現在時刻、外気温は状態値 |
| P/R/N/D/B表示 | 実装済み | `NissanNoteE13RuntimeState.cs`, `NoteE13InstrumentCluster.xaml.cs` | `ShiftPositionDisplay` |
| NORMAL/SPORT/ECO表示 | 実装済み | `NissanNoteE13RuntimeState.cs`, `NoteE13InstrumentCluster.xaml.cs` | `C` キーで循環 |
| Power OFF/ON/READY | 一部実装 | `NissanNoteE13RuntimeState.cs` | enumに `On` はあるが、現在の `I` 操作ではOFFから直接READY |
| 電動パーキングブレーキ | 実装済み | `NissanNoteE13RuntimeState.cs`, `NoteE13WarningLampLayer.xaml.cs` | `K` キー |
| AUTO HOLD | 実装済み | `NissanNoteE13RuntimeState.cs`, `NoteE13WarningLampLayer.xaml.cs` | `O` キー |
| ウインカー/ハザード表示 | 実装済み | `MeterWindow.xaml.cs`, `NoteE13WarningLampLayer.xaml.cs`, `NoteE13AudioService.cs` | 音声周期と表示周期を連動 |
| テールランプ/ハイビーム表示 | 実装済み | `MeterWindow.xaml.cs`, `NoteE13WarningLampLayer.xaml.cs` | `L` / `U` キー |
| 警告灯/表示灯のベクターアイコン | 一部実装 | `NoteE13WarningLampLayer.xaml` | Border/TextBlock中心。実車風ベクターアイコンとしては未完成 |
| Lamp/State Preview Panel | 実装済み | `NoteE13InstrumentCluster.xaml(.cs)` | `F3` キーで表示 |
| ウインカー音 | 実装済み | `NoteE13AudioService.cs`, `NoteE13AudioProfile.cs`, JSON | `turn-signal-loop.wav` に接続 |
| リバース音 | 実装済み | `NoteE13AudioService.cs`, `NoteE13AudioProfile.cs`, JSON | `reverse-loop.wav` に接続 |
| 前進時接近通報音 | 未実装 | 該当なし | `forward-approach-loop.wav` 実ファイルなし、JSONキーなし、再生処理なし |
| E13用音声のミュート/音量連携 | 実装済み | `MeterWindow.xaml.cs`, `NoteE13AudioService.cs`, `AudioService.cs` | `M` と Master Volume を渡している |

## 4. E13の現在の操作一覧

| キー | 現在の実際の動作 | 実装ファイル | 備考 |
|---|---|---|---|
| `I` | パワースイッチ。OFFならREADYへ移行し、起動音とセルフチェック、シートベルト警告予約。ON/READY側から押すとOFFへ戻し、終了音。 | `MeterWindow.xaml.cs`, `NissanNoteE13RuntimeState.cs` | OFF→READYで、Power ONだけの中間状態は通常操作では未確認 |
| `A` | アクセル押下。READYでD/R/BかつEPB解除なら加速。 | `MeterWindow.xaml.cs`, `NissanNoteE13RuntimeState.cs` | KeyUpで解除 |
| `Z` | ブレーキ押下。減速/回生、AUTO HOLD条件にも関与。 | `MeterWindow.xaml.cs`, `NissanNoteE13RuntimeState.cs` | KeyUpで解除 |
| `P` | シフトP選択。 | `MeterWindow.xaml.cs`, `NissanNoteE13RuntimeState.cs` | 走行中R/P拒否ロジックあり |
| `R` | シフトR選択。READYでブレーキ必要。成功時は汎用 `ReverseEngageSound` を鳴らす処理はあるが、Note JSONには現在未設定。リバースループ音は専用サービスで再生。 | `MeterWindow.xaml.cs`, `NissanNoteE13RuntimeState.cs`, `NoteE13AudioService.cs` | R中は `reverse-loop.wav` |
| `N` | シフトN選択。 | `MeterWindow.xaml.cs`, `NissanNoteE13RuntimeState.cs` | READYでなくてもNは許可される可能性あり |
| `D` | シフトD選択。READYでブレーキ必要。 | `MeterWindow.xaml.cs`, `NissanNoteE13RuntimeState.cs` | 燃焼車のD=Drive Modeとは分岐済み |
| `B` | シフトB選択。READYでブレーキ必要。 | `MeterWindow.xaml.cs`, `NissanNoteE13RuntimeState.cs` | Bレンジでは自然減速が強くなる |
| `K` | 電動パーキングブレーキON/OFF。 | `MeterWindow.xaml.cs`, `NissanNoteE13RuntimeState.cs` | 走行中は拒否 |
| `O` | AUTO HOLD ON/OFF。 | `MeterWindow.xaml.cs`, `NissanNoteE13RuntimeState.cs` | READYかつEPB解除が必要 |
| `C` | Drive ModeをNORMAL→SPORT→ECOで循環。 | `MeterWindow.xaml.cs`, `NissanNoteE13RuntimeState.cs` | E13専用。燃焼車のCは未使用 |
| `Q` | 左ウインカーON/OFF。ON時はハザード解除。 | `MeterWindow.xaml.cs`, `NoteE13AudioService.cs` | 音声クリックとランプ点滅に接続 |
| `E` | 右ウインカーON/OFF。ON時はハザード解除。 | `MeterWindow.xaml.cs`, `NoteE13AudioService.cs` | 音声クリックとランプ点滅に接続 |
| `H` | ハザードON/OFF。ON時は左右ウインカー解除。 | `MeterWindow.xaml.cs`, `NoteE13AudioService.cs` | 左右同時点滅 |
| `L` | テールランプ表示ON/OFF。 | `MeterWindow.xaml.cs`, `NoteE13WarningLampLayer.xaml.cs` | 表示灯のみ |
| `U` | ハイビーム表示ON/OFF。 | `MeterWindow.xaml.cs`, `NoteE13WarningLampLayer.xaml.cs` | 表示灯のみ |
| `F3` | E13 State Preview Panel表示ON/OFF。 | `MeterWindow.xaml.cs`, `NoteE13InstrumentCluster.xaml(.cs)` | 状態プレビュー |
| `M` | 全体ミュートON/OFF。イベント音、エンジン音、E13専用音声へ反映。 | `MeterWindow.xaml.cs`, `AudioService.cs`, `EngineAudioService.cs`, `NoteE13AudioService.cs` | E13サービスには `IsMuted` を渡す |
| `G` | Charge Mode ON/OFF。 | `MeterWindow.xaml.cs`, `NissanNoteE13RuntimeState.cs` | 操作ガイドに表示あり |
| `Y` | Manner Mode ON/OFF。 | `MeterWindow.xaml.cs`, `NissanNoteE13RuntimeState.cs` | 操作ガイドに表示あり |
| `+ / - / PageUp / PageDown` | Master Volume増減。 | `MeterWindow.xaml.cs`, `AudioService.cs` | NoteKeyGuideには表示されていないが処理はグローバル |
| `V` | 音声デバッグパネル切替。ただしE13では「Audio Debug is not used...」表示。 | `MeterWindow.xaml.cs` | E13では無効 |
| `F2` | 音声チューニングパネル切替。ただしE13では「Audio Tuning is not used...」表示。 | `MeterWindow.xaml.cs` | E13では無効 |

## 5. E13メーターの表示要素

| 表示要素 | 状態 | 実装場所 | 公式資料再現度に関するメモ |
|---|---|---|---|
| パワーメーター | 実装済み | `NoteE13InstrumentCluster.xaml(.cs)` | 左側に円形POWER/CHARGE表示。資料画像への厳密一致は未確認 |
| 速度 | 実装済み | `NoteE13InstrumentCluster.xaml(.cs)` | 右側大型デジタル表示 |
| 燃料計 | 実装済み | `NoteE13InstrumentCluster.xaml(.cs)` | 縦バー式。燃料アイコン文字に文字化けらしき表示あり |
| 航続可能距離 | 実装済み | `NissanNoteE13RuntimeState.cs`, `NoteE13InstrumentCluster.xaml.cs` | `RANGE xxx km` |
| ODO/TRIP | 実装済み | `NissanNoteE13RuntimeState.cs`, `NoteE13InstrumentCluster.xaml.cs` | 走行距離から更新 |
| P/R/N/D/B | 実装済み | `NissanNoteE13RuntimeState.cs`, `NoteE13InstrumentCluster.xaml.cs` | 中央表示 |
| READY | 実装済み | `NissanNoteE13RuntimeState.cs`, `NoteE13InstrumentCluster.xaml.cs`, `NoteE13WarningLampLayer.xaml.cs` | READY文字とランプ |
| ドライブモード | 実装済み | `NissanNoteE13RuntimeState.cs`, `NoteE13InstrumentCluster.xaml.cs` | NORMAL/SPORT/ECO色分け |
| AUTO HOLD | 実装済み | `NissanNoteE13RuntimeState.cs`, `NoteE13WarningLampLayer.xaml.cs` | ランプ表示あり |
| 警告灯 | 一部実装 | `NoteE13WarningLampLayer.xaml(.cs)` | READY/P/AUTO HOLD/左右/TAIL/HIGH/12V/BRAKE/ABS/CHG/MANNER。実車アイコンではなく文字中心 |
| 表示灯 | 一部実装 | `NoteE13WarningLampLayer.xaml(.cs)` | ウインカー等はあるが一部TextBlock文字が文字化けしている |
| 時計 | 実装済み | `NoteE13InstrumentCluster.xaml.cs` | `DateTime.Now.ToString("HH:mm")` |
| 外気温 | 実装済み | `NoteE13InstrumentCluster.xaml.cs` | 表示文字に `ﾂｰC` という文字化けらしき表現あり |
| E13 AUDIO状態 | 実装済み | `NoteE13InstrumentCluster.xaml(.cs)` | TURN/REVERSEのREADY/PLAYING/MUTED/NO FILE |
| State Preview Panel | 実装済み | `NoteE13InstrumentCluster.xaml(.cs)` | `F3` で表示 |

## 6. 音声実装の進捗

| 音源／機能 | ファイル存在 | JSON設定 | 再生実装 | 実際の再生条件 | 備考 |
|---|---|---|---|---|---|
| `turn-signal-loop.wav` | あり: `Assets/Sounds/NissanNoteE13/turn-signal-loop.wav` | あり: `noteE13AudioProfile.turnSignalSound` | 実装済み | E13でPowerStateがOFF以外、かつQ/E/Hいずれかが要求中。周期 `turnSignalPeriodMs=700`、点灯 `turnSignalLampOnMs=350` | `NoteE13AudioService` がNAudio `WaveOutEvent` + `AudioFileReader` を単一保持。点灯区間外で停止し、多重再生を抑制 |
| `reverse-loop.wav` | あり: `Assets/Sounds/NissanNoteE13/reverse-loop.wav` | あり: `noteE13AudioProfile.reverseLoopSound` | 実装済み | E13で `PowerState == Ready` かつ `ShiftPosition == R` | `MediaPlayer` でループ。`reverseFadeMilliseconds=80` を音量追従係数に利用 |
| `forward-approach-loop.wav` | なし | なし | 未実装 | 該当なし | 前進時接近通報音の処理は検索で見つからず |
| E13用音声サービス | 不要 | `noteE13AudioProfile` | 実装済み | `MeterWindow.UpdateTimer_Tick` から50msごとに状態を渡す | `NoteE13AudioService.cs` |
| ミュート | 不要 | 不要 | 実装済み | `M` キーで `AudioService.IsMuted` を切替しE13サービスに渡す | ミュート中はウインカー/リバース音停止または再生しない |
| Master Volume | 不要 | 不要 | 実装済み | `+/-/PageUp/PageDown` で `AudioService.MasterVolume` を変更しE13サービスに渡す | ウインカー/リバースの音量に乗算 |
| 多重再生防止 | 不要 | 不要 | 一部実装 | ウインカーは単一NAudio出力、リバースは単一MediaPlayer | 実聴での完全確認は未確認。コード上は多重生成を抑制 |
| リソース解放 | 不要 | 不要 | 実装済み | Back、Window Closed、Deactivated、Power OFF系で停止 | `noteE13AudioService.Dispose()` / `StopAll()` |
| E13起動音 | あり: `Assets/Sounds/E13 Note e-POWER/Startup.wav` | あり: `audioProfile.ignitionOnSound` | 実装済み | E13で `I` キー、OFFからREADYへ遷移時 | `AudioService.PlaySound` |
| E13シートベルト警告音 | あり: `Assets/Sounds/E13 Note e-POWER/Seatbelt.wav` | あり: `audioProfile.seatbeltWarningSound` | 実装済み | 起動音の長さ + 1秒後、PowerStateがOFFでない場合 | `ScheduleNoteSeatbeltWarning()` |
| E13終了音 | あり: `Assets/Sounds/E13 Note e-POWER/SystemStop.wav` | あり: `audioProfile.systemStopSound` | 実装済み | E13で `I` キー、OFFへ戻る時 | `AudioService.PlaySound` |
| 出力フォルダコピー | あり | `.csproj` | 実装済み | `Assets\Sounds\**\*.*` が `PreserveNewest` | `bin\Debug\net10.0-windows\Assets\Sounds\NissanNoteE13` に2ファイル確認 |

`Assets/Sounds/NissanNoteE13/` の実在ファイル:

```text
.gitkeep
reverse-loop.wav      1,382,144 bytes
turn-signal-loop.wav    750,208 bytes
```

出力フォルダ `bin\Debug\net10.0-windows\Assets\Sounds\NissanNoteE13` にも同名ファイルがコピーされていることを確認済み。

## 7. 現在のデータモデルと責務

- `VehicleProfile`
  - 車種ごとの固定情報を保持するモデル。
  - 主なプロパティ: `Id`, `Manufacturer`, `Name`, `MeterStyleId`, `EngineDescription`, `PowertrainType`, `SupportedShiftPositions`, `SupportedDriveModes`, `DefaultShiftPosition`, `DefaultDriveMode`, `MaxRpm`, `IdleRpm`, `RevLimiterRpm`, `ForwardGearCount`, `DrivingModes`, `SupportedTransmissionModeIds`, `AudioProfile`, `NoteE13AudioProfile`。
  - `UsesElectricPowerMeter` で `meterStyleId == note-e13-authentic` を判定。

- `VehicleRuntimeState`
  - LFA/LC500など燃焼エンジン車側の実行中状態と走行ロジック。
  - 主な責務: イグニッション、エンジン、アクセル、ブレーキ、Pブレーキ、速度、RPM、ギア、走行モード、MANUAL/AUTO変速。
  - 主なプロパティ: `IsIgnitionOn`, `IsEngineRunning`, `CurrentSpeedKmh`, `CurrentRpm`, `CurrentGearNumber`, `CurrentDrivingModeId`, `CurrentTransmissionModeId`, `SystemMessage`。

- `NissanNoteE13RuntimeState`
  - E13専用の実行中状態と簡易e-POWER走行ロジック。
  - 主な責務: `PowerState`, `ShiftPosition`, `DriveMode`, 速度、パワーメーター、航続距離、ODO/TRIP、燃料、外気温、EPB、AUTO HOLD、Charge/Manner。
  - 主なプロパティ: `PowerState`, `ShiftPosition`, `DriveMode`, `CurrentSpeedKmh`, `PowerMeterPercent`, `EstimatedRangeKm`, `OdometerKm`, `TripMeterKm`, `FuelLevelPercent`, `IsElectricParkingBrakeOn`, `IsAutoBrakeHoldEnabled`。

- `NoteE13IndicatorState`
  - 未作成。検索で該当クラスは見つからない。
  - E13の表示灯状態は現在 `MeterWindow.xaml.cs` のbool群と `NissanNoteE13RuntimeState`、`NoteE13WarningLampLayer` で分散管理されている。

- `NoteE13AudioProfile`
  - E13専用音声設定モデル。
  - 主なプロパティ: `TurnSignalSound`, `ReverseLoopSound`, `TurnSignalPeriodMs`, `TurnSignalLampOnMs`, `TurnSignalVolume`, `ReverseLoopVolume`, `ReverseFadeMilliseconds`。

- `NoteE13AudioService`
  - E13専用のウインカー音とリバース音を担当。
  - ウインカーはNAudioの単一 `WaveOutEvent` / `AudioFileReader` で周期再生し、ランプ点灯タイミングも返す。
  - リバース音は `MediaPlayer` でループ再生。
  - ミュート、Master Volume、ファイル未配置時のNO FILE状態、パス安全性、リソース解放に対応。

- `MeterWindow`
  - メーター画面全体、キー入力、タイマー更新、車種別表示切替、音声サービス呼び出しを担当。
  - LFA専用、Note E13専用、汎用タコメーターの表示切替を行う。
  - E13の場合は `NissanNoteE13RuntimeState` と `NoteE13AudioService` を使用し、燃焼車の `VehicleRuntimeState` とは別経路。

- `NoteE13InstrumentCluster`
  - E13専用メーターUI本体。
  - パワーメーター、速度、シフト、ドライブモード、READY/POWER状態、航続距離、ODO/TRIP、燃料、時計、外気温、PreviewPanel、E13 AUDIO状態を表示。

- `NoteE13WarningLampLayer`
  - E13専用の警告灯/表示灯レイヤー。
  - READY、P、AUTO HOLD、左右ウインカー、TAIL、HIGH、12V、BRAKE、ABS、CHG、MANNERを表示。
  - 現状はBorder/TextBlock中心で、実車アイコンのベクター再現は未完成。

## 8. ビルド・実行状態

- `dotnet build`: 成功
- 警告: 0
- エラー: 0
- 起動可否: `dotnet run --no-build` でプロセスが5秒以上維持されることを確認
- 未確認事項:
  - UI上で全キー操作を手動確認したわけではない
  - 実車資料とのピクセルレベル比較は未確認
  - E13ウインカー音/リバース音の実聴品質は未確認
  - LFA/LC500の全走行/変速/音声挙動の手動回帰確認は未確認

## 9. 未完了・問題点

| 優先度 | 内容 | 原因または不足情報 | 推奨対応 |
|---|---|---|---|
| 高 | E13前進時接近通報音が未実装 | `forward-approach-loop.wav` が存在せず、JSONキーもサービス実装もない | `NoteE13AudioProfile` に前進音設定を追加し、READY+D/Bで低速走行中に再生する仕様を決める |
| 高 | E13警告灯/表示灯が実車風ベクターアイコンではない | `NoteE13WarningLampLayer.xaml` は文字中心。一部文字化けあり | SVG/Pathベースのアイコンに置換し、文字化けを修正 |
| 中 | E13のPower ON状態が通常操作でほぼ使われていない | `PressPowerSwitch()` はOFFから直接READYへ移行。`On` enumはあるが遷移が限定的 | 実車仕様に合わせるか、現在仕様として「IでREADY」を明文化する |
| 中 | E13音声ファイルのライセンス/由来が一部不明 | `NissanNoteE13` のWAVはローカル試作扱い | `THIRD_PARTY_AUDIO.md` に確定した素材情報を追記する |
| 中 | Git状態が大きくdirty | 多数の変更/未追跡ファイルがある | 機能単位で差分確認し、不要ファイルと必要ファイルを整理してコミット単位を分ける |
| 中 | E13表示に文字化けらしき文字がある | `OutsideTempText` の `ﾂｰC`、燃料/矢印表示の文字 | XAMLの文字を安全なASCII/Path/正しいUnicodeへ修正 |
| 低 | E13ではAudio Debug/Tuningが無効 | コード上でE13選択時に無効メッセージ | 必要ならE13専用音声デバッグ表示を追加 |
| 低 | LFA/LC500のイベント音WAVが多く未配置 | JSONパスはあるが実ファイルは主にengine-loopのみ | 必要なイベント音素材を配置するか、No Files表示を整理 |

## 10. 次に実装すべき候補

1. E13前進時接近通報音の追加
   - 理由: ユーザーが明示的に確認したい音源リストに含まれており、現状完全に未実装。

2. E13警告灯/表示灯のベクターアイコン化と文字化け修正
   - 理由: 現在のE13メーター再現度で最も見た目に影響する未完成点。資料画像再現にも直結する。

3. Git差分整理と機能単位のコミット準備
   - 理由: 未追跡ファイルと変更が多く、次の実装前に現在地点を固定した方が安全。

## 11. ChatGPTへ共有する要約

- 現在もっとも完成している車種: Lexus LFA / Lexus LC500 Coupe は基本走行・変速・メーター・音声基盤が揃っている
- 現在もっとも開発中の車種: Nissan Note e-POWER E13
- E13メーターの完成度: 専用画面、パワーメーター、速度、燃料、航続距離、ODO/TRIP、シフト、ドライブモード、READY表示は実装済み
- E13操作系の完成度: I/A/Z/P/R/N/D/B/K/O/C/Q/E/H/L/U/F3/M はコード上実装済み
- E13音声の完成度: 起動音、終了音、シートベルト警告、ウインカー音、リバース音は接続済み。前進時接近通報音は未実装
- ビルド状態: `dotnet build` 成功、警告0、エラー0
- 起動状態: `dotnet run --no-build` で5秒以上プロセス維持を確認
- 現在の最大の問題: E13前進時接近通報音が未実装、E13表示灯が文字中心で一部文字化けあり
- 次に推奨する作業: E13前進時接近通報音の仕様追加と `forward-approach-loop.wav` 接続
- 注意点: Git作業ツリーには多数の未コミット変更と未追跡ファイルがある

