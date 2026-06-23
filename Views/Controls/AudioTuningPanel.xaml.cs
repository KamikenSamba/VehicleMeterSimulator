using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VehicleMeterSimulator.Models;

namespace VehicleMeterSimulator.Views.Controls;

public partial class AudioTuningPanel : UserControl
{
    private AudioTuningSession? session;
    private bool isUpdatingUi;

    public AudioTuningPanel()
    {
        InitializeComponent();
    }

    public event EventHandler? TuningChanged;

    public event EventHandler? ResetRequested;

    public event EventHandler? ExportRequested;

    public void Initialize(AudioTuningSession tuningSession, VehicleProfile vehicle)
    {
        session = tuningSession;
        isUpdatingUi = true;

        VehicleText.Text = tuningSession.VehicleName;
        PreviewRpmSlider.Minimum = vehicle.IdleRpm;
        PreviewRpmSlider.Maximum = vehicle.RevLimiterRpm;
        PreviewRpmSlider.Value = Math.Clamp(tuningSession.PreviewRpm, vehicle.IdleRpm, vehicle.RevLimiterRpm);
        PreviewCheckBox.IsChecked = tuningSession.IsPreviewEnabled;

        LayersMasterVolumeSlider.Value = Math.Clamp(tuningSession.EngineLayersMasterVolume, 0.0, 1.0);
        LoopBaseVolumeSlider.Value = Math.Clamp(tuningSession.EngineLoopBaseVolume, 0.0, 1.0);
        LoopThrottleVolumeSlider.Value = Math.Clamp(tuningSession.EngineLoopThrottleVolume, 0.0, 1.0);

        var layerSliderMaximum = Math.Max(vehicle.MaxRpm, vehicle.RevLimiterRpm);
        LayerReferenceRpmSlider.Maximum = layerSliderMaximum;
        LayerMinimumRpmSlider.Maximum = layerSliderMaximum;
        LayerPeakRpmSlider.Maximum = layerSliderMaximum;
        LayerMaximumRpmSlider.Maximum = layerSliderMaximum;

        LayerComboBox.ItemsSource = tuningSession.EngineAudioLayers;
        LayerComboBox.SelectedIndex = tuningSession.EngineAudioLayers.Count > 0 ? 0 : -1;

        isUpdatingUi = false;
        RefreshTexts();
        RefreshSelectedLayer();
    }

    public void SetPlaybackMode(string playbackMode)
    {
        PlaybackModeText.Text = playbackMode;
    }

    public void SetPreviewAvailability(bool isEngineRunning, bool isAudioAvailable)
    {
        isUpdatingUi = true;
        PreviewCheckBox.IsChecked = session?.IsPreviewEnabled == true;
        PreviewCheckBox.IsEnabled = isEngineRunning && isAudioAvailable;
        PreviewRpmSlider.IsEnabled = PreviewCheckBox.IsEnabled && PreviewCheckBox.IsChecked == true;
        isUpdatingUi = false;
    }

    private void PreviewCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (isUpdatingUi || session is null)
        {
            return;
        }

        session.IsPreviewEnabled = PreviewCheckBox.IsChecked == true;
        PreviewRpmSlider.IsEnabled = PreviewCheckBox.IsEnabled && session.IsPreviewEnabled;
        RaiseTuningChanged();
    }

    private void PreviewRpmSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (isUpdatingUi || session is null)
        {
            return;
        }

        session.PreviewRpm = (int)Math.Round(PreviewRpmSlider.Value / 100.0) * 100;
        RefreshTexts();
        RaiseTuningChanged();
    }

    private void CommonSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (isUpdatingUi || session is null)
        {
            return;
        }

        session.EngineLayersMasterVolume = LayersMasterVolumeSlider.Value;
        session.EngineLoopBaseVolume = LoopBaseVolumeSlider.Value;
        session.EngineLoopThrottleVolume = LoopThrottleVolumeSlider.Value;
        RefreshTexts();
        RaiseTuningChanged();
    }

    private void LayerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isUpdatingUi)
        {
            return;
        }

        RefreshSelectedLayer();
    }

    private void LayerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (isUpdatingUi || session is null || LayerComboBox.SelectedItem is not EngineAudioLayerProfile layer)
        {
            return;
        }

        layer.ReferenceRpm = ToRpm(LayerReferenceRpmSlider.Value);
        layer.MinimumRpm = ToRpm(LayerMinimumRpmSlider.Value);
        layer.PeakRpm = ToRpm(LayerPeakRpmSlider.Value);
        layer.MaximumRpm = ToRpm(LayerMaximumRpmSlider.Value);
        layer.MinimumPitchFactor = LayerMinimumPitchSlider.Value;
        layer.MaximumPitchFactor = LayerMaximumPitchSlider.Value;
        layer.BaseVolume = LayerBaseVolumeSlider.Value;

        ValidateLayer(layer);
        RefreshLayerTexts(layer);
        RaiseTuningChanged();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        ResetRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        ExportRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshSelectedLayer()
    {
        if (LayerComboBox.SelectedItem is not EngineAudioLayerProfile layer)
        {
            SetLayerControlsEnabled(false);
            LayerRangeText.Text = "RPM Range: No layer";
            LayerPitchText.Text = "Pitch: -";
            LayerVolumeText.Text = "Base Volume: -";
            ValidationText.Text = string.Empty;
            return;
        }

        isUpdatingUi = true;
        SetLayerControlsEnabled(true);
        LayerReferenceRpmSlider.Value = layer.ReferenceRpm;
        LayerMinimumRpmSlider.Value = layer.MinimumRpm;
        LayerPeakRpmSlider.Value = layer.PeakRpm;
        LayerMaximumRpmSlider.Value = layer.MaximumRpm;
        LayerMinimumPitchSlider.Value = layer.MinimumPitchFactor;
        LayerMaximumPitchSlider.Value = layer.MaximumPitchFactor;
        LayerBaseVolumeSlider.Value = layer.BaseVolume;
        isUpdatingUi = false;

        ValidateLayer(layer);
        RefreshLayerTexts(layer);
    }

    private void RefreshTexts()
    {
        if (session is null)
        {
            return;
        }

        PreviewRpmText.Text = $"{session.PreviewRpm} rpm";
        LayersMasterVolumeText.Text = $"Layers Master Volume: {ToPercent(session.EngineLayersMasterVolume)}";
        LoopBaseVolumeText.Text = $"Loop Base Volume: {ToPercent(session.EngineLoopBaseVolume)}";
        LoopThrottleVolumeText.Text = $"Loop Throttle Volume: {ToPercent(session.EngineLoopThrottleVolume)}";
    }

    private void RefreshLayerTexts(EngineAudioLayerProfile layer)
    {
        LayerRangeText.Text = $"RPM Range: {layer.MinimumRpm} - {layer.PeakRpm} - {layer.MaximumRpm}";
        LayerPitchText.Text = $"Pitch: {layer.MinimumPitchFactor:0.00} - {layer.MaximumPitchFactor:0.00}";
        LayerVolumeText.Text = $"Base Volume: {layer.BaseVolume:0.00}";
    }

    private void ValidateLayer(EngineAudioLayerProfile layer)
    {
        var errors = new[]
        {
            layer.MinimumRpm <= layer.PeakRpm ? null : "Minimum RPM must be less than or equal to Peak RPM.",
            layer.PeakRpm <= layer.MaximumRpm ? null : "Peak RPM must be less than or equal to Maximum RPM.",
            layer.ReferenceRpm > 0 ? null : "Reference RPM must be greater than 0.",
            layer.MinimumPitchFactor <= layer.MaximumPitchFactor ? null : "Minimum Pitch must be less than or equal to Maximum Pitch."
        }.Where(error => error is not null);

        ValidationText.Text = string.Join(Environment.NewLine, errors);
    }

    private void SetLayerControlsEnabled(bool isEnabled)
    {
        LayerReferenceRpmSlider.IsEnabled = isEnabled;
        LayerMinimumRpmSlider.IsEnabled = isEnabled;
        LayerPeakRpmSlider.IsEnabled = isEnabled;
        LayerMaximumRpmSlider.IsEnabled = isEnabled;
        LayerMinimumPitchSlider.IsEnabled = isEnabled;
        LayerMaximumPitchSlider.IsEnabled = isEnabled;
        LayerBaseVolumeSlider.IsEnabled = isEnabled;
    }

    private void RaiseTuningChanged()
    {
        TuningChanged?.Invoke(this, EventArgs.Empty);
    }

    private static int ToRpm(double value)
    {
        return (int)Math.Round(value / 100.0) * 100;
    }

    private static string ToPercent(double value)
    {
        return $"{Math.Round(value * 100.0):0}%";
    }
}
