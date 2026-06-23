using System;

namespace VehicleMeterSimulator.Models;

public class NissanNoteE13RuntimeState
{
    private const double MaxForwardSpeedKmh = 160.0;
    private const double MaxReverseSpeedKmh = 35.0;
    private const double NormalAccelerationKmhPerSecond = 16.0;
    private const double SportAccelerationKmhPerSecond = 22.0;
    private const double EcoAccelerationKmhPerSecond = 10.0;
    private const double BrakeDecelerationKmhPerSecond = 42.0;
    private const double CoastDecelerationKmhPerSecond = 4.0;
    private const double StoppedSpeedThresholdKmh = 0.1;
    private const int MessageHoldTicks = 32;

    private int messageHoldTicks;

    public E13PowerState PowerState { get; private set; } = E13PowerState.Off;

    public E13ShiftPosition ShiftPosition { get; private set; } = E13ShiftPosition.P;

    public E13DriveMode DriveMode { get; private set; } = E13DriveMode.Normal;

    public double CurrentSpeedKmh { get; private set; }

    public int PowerMeterPercent { get; private set; }

    public int EstimatedRangeKm { get; private set; } = 450;

    public int OdometerKm { get; private set; }

    public double TripMeterKm { get; private set; }

    public int FuelLevelPercent { get; private set; } = 75;

    public int OutsideTemperatureCelsius { get; private set; } = 24;

    public bool IsAcceleratorPressed { get; private set; }

    public bool IsBrakePressed { get; private set; }

    public bool IsElectricParkingBrakeOn { get; private set; } = true;

    public bool IsAutoBrakeHoldEnabled { get; private set; }

    public bool IsAutoBrakeHoldHolding { get; private set; }

    public bool IsChargeModeOn { get; private set; }

    public bool IsMannerModeOn { get; private set; }

    public string SystemMessage { get; private set; } = "Power OFF";

    public bool IsReady => PowerState == E13PowerState.Ready;

    public string PowerStateDisplay => PowerState switch
    {
        E13PowerState.Ready => "READY",
        E13PowerState.On => "ON",
        _ => "OFF"
    };

    public string ShiftPositionDisplay => ShiftPosition.ToString().ToUpperInvariant();

    public string DriveModeDisplay => DriveMode.ToString().ToUpperInvariant();

    public void PressPowerSwitch()
    {
        if (PowerState == E13PowerState.Off)
        {
            PowerState = E13PowerState.Ready;
            SetSystemMessage("READY");
            return;
        }

        if (PowerState == E13PowerState.On && IsBrakePressed)
        {
            PowerState = E13PowerState.Ready;
            SetSystemMessage("READY");
            return;
        }

        PowerState = E13PowerState.Off;
        ShiftPosition = E13ShiftPosition.P;
        ClearPedals();
        CurrentSpeedKmh = 0.0;
        PowerMeterPercent = 0;
        IsAutoBrakeHoldHolding = false;
        SetSystemMessage("Power OFF - Shift Returned to P");
    }

    public void SetAcceleratorPressed(bool isPressed)
    {
        IsAcceleratorPressed = isPressed;
        if (isPressed)
        {
            if (!IsReady)
            {
                SetSystemMessage("READY required before driving");
            }
            else if (IsElectricParkingBrakeOn && ShiftPosition is E13ShiftPosition.D or E13ShiftPosition.R or E13ShiftPosition.B)
            {
                SetSystemMessage("Release electric parking brake before moving");
            }
            else if (IsAutoBrakeHoldHolding)
            {
                IsAutoBrakeHoldHolding = false;
                SetSystemMessage("AUTO HOLD released");
            }
        }
    }

    public void SetBrakePressed(bool isPressed)
    {
        IsBrakePressed = isPressed;
        if (isPressed)
        {
            SetSystemMessage("Brake pedal");
        }
    }

    public void SelectShiftPosition(E13ShiftPosition position)
    {
        if (!IsReady && position != E13ShiftPosition.P)
        {
            if (PowerState == E13PowerState.On && IsBrakePressed)
            {
                PowerState = E13PowerState.Ready;
            }
            else
            {
                SetSystemMessage("READY required before selecting drive position", MessageHoldTicks);
                return;
            }
        }

        if (CurrentSpeedKmh > StoppedSpeedThresholdKmh && position is E13ShiftPosition.P or E13ShiftPosition.R)
        {
            SetSystemMessage("Stop vehicle before selecting this position", MessageHoldTicks);
            return;
        }

        if (position is E13ShiftPosition.D or E13ShiftPosition.R or E13ShiftPosition.B)
        {
            if (!IsBrakePressed)
            {
                SetSystemMessage("Press brake pedal before shifting", MessageHoldTicks);
                return;
            }
        }

        ShiftPosition = position;
        SetSystemMessage($"Shift Position: {ShiftPositionDisplay}", MessageHoldTicks);
    }

    public void ToggleElectricParkingBrake()
    {
        if (CurrentSpeedKmh > StoppedSpeedThresholdKmh)
        {
            SetSystemMessage("Stop vehicle before operating electric parking brake", MessageHoldTicks);
            return;
        }

        IsElectricParkingBrakeOn = !IsElectricParkingBrakeOn;
        SetSystemMessage(
            IsElectricParkingBrakeOn ? "Electric Parking Brake ON" : "Electric Parking Brake OFF",
            MessageHoldTicks);
    }

    public void ToggleAutoBrakeHold()
    {
        if (!IsReady)
        {
            SetSystemMessage("READY required before AUTO HOLD", MessageHoldTicks);
            return;
        }

        if (IsElectricParkingBrakeOn)
        {
            SetSystemMessage("Release electric parking brake before AUTO HOLD", MessageHoldTicks);
            return;
        }

        IsAutoBrakeHoldEnabled = !IsAutoBrakeHoldEnabled;
        IsAutoBrakeHoldHolding = false;
        SetSystemMessage(IsAutoBrakeHoldEnabled ? "AUTO HOLD ON" : "AUTO HOLD OFF", MessageHoldTicks);
    }

    public void CycleDriveMode()
    {
        DriveMode = DriveMode switch
        {
            E13DriveMode.Normal => E13DriveMode.Sport,
            E13DriveMode.Sport => E13DriveMode.Eco,
            _ => E13DriveMode.Normal
        };

        SetSystemMessage($"Drive Mode: {DriveModeDisplay}", MessageHoldTicks);
    }

    public void ToggleChargeMode()
    {
        IsChargeModeOn = !IsChargeModeOn;
        SetSystemMessage(IsChargeModeOn ? "Charge Mode ON" : "Charge Mode OFF", MessageHoldTicks);
    }

    public void ToggleMannerMode()
    {
        IsMannerModeOn = !IsMannerModeOn;
        SetSystemMessage(IsMannerModeOn ? "Manner Mode ON" : "Manner Mode OFF", MessageHoldTicks);
    }

    public void Update(double deltaTimeSeconds)
    {
        UpdateSpeed(deltaTimeSeconds);
        UpdatePowerMeter();
        UpdateDistance(deltaTimeSeconds);
        TickMessageHold();
    }

    private void UpdateSpeed(double deltaTimeSeconds)
    {
        if (PowerState != E13PowerState.Ready)
        {
            CurrentSpeedKmh = double.Max(0.0, CurrentSpeedKmh - CoastDecelerationKmhPerSecond * deltaTimeSeconds);
            return;
        }

        if (IsBrakePressed)
        {
            CurrentSpeedKmh = double.Max(0.0, CurrentSpeedKmh - BrakeDecelerationKmhPerSecond * deltaTimeSeconds);
            if (CurrentSpeedKmh <= StoppedSpeedThresholdKmh && IsAutoBrakeHoldEnabled && !IsAcceleratorPressed)
            {
                IsAutoBrakeHoldHolding = true;
            }

            SetPassiveSystemMessage("Regenerating / braking");
            return;
        }

        if (IsAcceleratorPressed && CanMove())
        {
            var acceleration = GetAcceleration();
            var maxSpeed = ShiftPosition == E13ShiftPosition.R ? MaxReverseSpeedKmh : MaxForwardSpeedKmh;
            CurrentSpeedKmh = double.Min(maxSpeed, CurrentSpeedKmh + acceleration * deltaTimeSeconds);
            SetPassiveSystemMessage(ShiftPosition == E13ShiftPosition.R ? "Reversing" : "Driving");
            return;
        }

        if (CurrentSpeedKmh > 0.0)
        {
            var coast = ShiftPosition == E13ShiftPosition.B
                ? CoastDecelerationKmhPerSecond * 2.2
                : CoastDecelerationKmhPerSecond;
            CurrentSpeedKmh = double.Max(0.0, CurrentSpeedKmh - coast * deltaTimeSeconds);
            SetPassiveSystemMessage(ShiftPosition == E13ShiftPosition.B ? "B range regeneration" : "Coasting");
        }

        if (CurrentSpeedKmh <= StoppedSpeedThresholdKmh)
        {
            CurrentSpeedKmh = 0.0;
        }
    }

    private void UpdatePowerMeter()
    {
        if (PowerState != E13PowerState.Ready)
        {
            PowerMeterPercent = 0;
            return;
        }

        if (IsBrakePressed || (CurrentSpeedKmh > 0.0 && !IsAcceleratorPressed))
        {
            PowerMeterPercent = ShiftPosition == E13ShiftPosition.B ? -62 : -34;
            return;
        }

        if (IsAcceleratorPressed && CanMove())
        {
            PowerMeterPercent = DriveMode switch
            {
                E13DriveMode.Sport => 86,
                E13DriveMode.Eco => 46,
                _ => 64
            };
            return;
        }

        PowerMeterPercent = 0;
    }

    private void UpdateDistance(double deltaTimeSeconds)
    {
        var distanceKm = CurrentSpeedKmh * deltaTimeSeconds / 3600.0;
        TripMeterKm += distanceKm;
        OdometerKm = (int)Math.Floor(TripMeterKm);
        EstimatedRangeKm = Math.Max(0, 450 - (int)Math.Floor(TripMeterKm * 0.7));
    }

    private bool CanMove()
    {
        return ShiftPosition is E13ShiftPosition.D or E13ShiftPosition.R or E13ShiftPosition.B
            && !IsElectricParkingBrakeOn
            && !IsAutoBrakeHoldHolding;
    }

    private double GetAcceleration()
    {
        return DriveMode switch
        {
            E13DriveMode.Sport => SportAccelerationKmhPerSecond,
            E13DriveMode.Eco => EcoAccelerationKmhPerSecond,
            _ => NormalAccelerationKmhPerSecond
        };
    }

    private void ClearPedals()
    {
        IsAcceleratorPressed = false;
        IsBrakePressed = false;
    }

    private void SetSystemMessage(string message, int holdTicks = 0)
    {
        SystemMessage = message;
        messageHoldTicks = holdTicks;
    }

    private void SetPassiveSystemMessage(string message)
    {
        if (messageHoldTicks == 0)
        {
            SystemMessage = message;
        }
    }

    private void TickMessageHold()
    {
        if (messageHoldTicks > 0)
        {
            messageHoldTicks--;
        }
    }
}

public enum E13PowerState
{
    Off,
    On,
    Ready
}

public enum E13ShiftPosition
{
    P,
    R,
    N,
    D,
    B
}

public enum E13DriveMode
{
    Normal,
    Sport,
    Eco
}
