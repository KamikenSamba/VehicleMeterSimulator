using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using VehicleMeterSimulator.Models;
using VehicleMeterSimulator.Views;

namespace VehicleMeterSimulator;

public partial class MainWindow : Window
{
    private readonly List<VehicleProfile> vehicles;

    public MainWindow()
    {
        InitializeComponent();

        vehicles = VehicleProfile.CreateDefaultVehicles();
        VehicleComboBox.ItemsSource = vehicles;
        VehicleComboBox.SelectedIndex = 0;
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
            return;
        }

        EngineTextBlock.Text = vehicle.EngineDescription;
        PowerTextBlock.Text = vehicle.PowerDisplay;
        TorqueTextBlock.Text = vehicle.TorqueDisplay;
        TransmissionTextBlock.Text = vehicle.Transmission;
        DriveTypeTextBlock.Text = vehicle.DriveType;
    }
}
