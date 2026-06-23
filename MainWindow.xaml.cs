using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VehicleMeterSimulator.Models;
using VehicleMeterSimulator.Services;
using VehicleMeterSimulator.Views;

namespace VehicleMeterSimulator;

public partial class MainWindow : Window
{
    private readonly List<VehicleProfile> vehicles;

    public MainWindow()
    {
        InitializeComponent();

        vehicles = LoadVehicles();
        VehicleComboBox.ItemsSource = vehicles;

        if (vehicles.Count > 0)
        {
            VehicleComboBox.SelectedItem = vehicles.FirstOrDefault(vehicle => vehicle.Id == "lexus-lfa") ?? vehicles[0];
        }
        else
        {
            OpenMeterButton.IsEnabled = false;
        }
    }

    private void VehicleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ShowVehicleSpecifications(VehicleComboBox.SelectedItem as VehicleProfile);
    }

    private void OpenMeterButton_Click(object sender, RoutedEventArgs e)
    {
        if (VehicleComboBox.SelectedItem is not VehicleProfile selectedVehicle)
        {
            MessageBox.Show(
                "Please select a vehicle.",
                "Vehicle Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var meterWindow = new MeterWindow(selectedVehicle);
        Application.Current.MainWindow = meterWindow;
        meterWindow.Show();
        Close();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void ShowVehicleSpecifications(VehicleProfile? vehicle)
    {
        if (vehicle is null)
        {
            EngineTextBlock.Text = "";
            PowerTextBlock.Text = "";
            TorqueTextBlock.Text = "";
            TransmissionTextBlock.Text = "";
            DriveTypeTextBlock.Text = "";
            ForwardGearsTextBlock.Text = "";
            return;
        }

        if (vehicle.UsesElectricPowerMeter)
        {
            EngineTextBlock.Text = vehicle.EngineDescription;
            PowerTextBlock.Text = "e-POWER display model";
            TorqueTextBlock.Text = "Not shown in this prototype";
            TransmissionTextBlock.Text = vehicle.Transmission;
            DriveTypeTextBlock.Text = vehicle.DriveType;
            ForwardGearsTextBlock.Text = string.Join(" / ", vehicle.SupportedShiftPositions);
            return;
        }

        EngineTextBlock.Text = vehicle.EngineDescription;
        PowerTextBlock.Text = vehicle.PowerDisplay;
        TorqueTextBlock.Text = vehicle.TorqueDisplay;
        TransmissionTextBlock.Text = vehicle.Transmission;
        DriveTypeTextBlock.Text = vehicle.DriveType;
        ForwardGearsTextBlock.Text = vehicle.ForwardGearCount.ToString();
    }

    private static List<VehicleProfile> LoadVehicles()
    {
        try
        {
            return new VehicleRepository().LoadVehicles();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(
                $"Vehicle data could not be loaded.{System.Environment.NewLine}{System.Environment.NewLine}{ex.Message}",
                "Vehicle Data Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return [];
        }
    }
}
