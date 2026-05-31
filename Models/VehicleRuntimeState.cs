namespace VehicleMeterSimulator.Models;

public class VehicleRuntimeState
{
    private const int EngineStartRpmStep = 100;
    private const int AccelerationRpmStep = 300;
    private const int ReturnToIdleRpmStep = 220;
    private const int EngineStopRpmStep = 150;
    private const double StoppedSpeedThresholdKmh = 0.1;
    private const int ShiftMessageHoldTicks = 20;
    private const string FallbackDrivingModeId = "normal";
    private const string FallbackTransmissionModeId = "manual";
    private const string AutomaticTransmissionModeId = "automatic";

    private int messageHoldTicks = 0;
    private double automaticShiftCooldownRemainingMilliseconds = 0.0;

    public bool IsIgnitionOn { get; private set; } = false;

    public bool IsEngineRunning { get; private set; } = false;

    public bool IsAcceleratorPressed { get; private set; } = false;

    public int ThrottlePercent { get; private set; } = 0;

    public bool IsBrakePressed { get; private set; } = false;

    public int BrakePercent { get; private set; } = 0;

    public bool IsParkingBrakeApplied { get; private set; } = true;

    public double CurrentSpeedKmh { get; private set; } = 0.0;

    public int CurrentRpm { get; private set; } = 0;

    public int CurrentGearNumber { get; private set; } = 0;

    public string CurrentDrivingModeId { get; private set; } = FallbackDrivingModeId;

    public string CurrentTransmissionModeId { get; private set; } = FallbackTransmissionModeId;

    public string CurrentGear => CurrentGearNumber switch
    {
        -1 => "R",
        0 => "N",
        _ => IsAutomaticTransmission ? $"D{CurrentGearNumber}" : CurrentGearNumber.ToString()
    };

    public bool IsAutomaticTransmission => string.Equals(
        CurrentTransmissionModeId,
        AutomaticTransmissionModeId,
        StringComparison.OrdinalIgnoreCase);

    public string SystemMessage { get; private set; } = "Press I to switch ignition on";

    public void InitializeDrivingMode(VehicleProfile vehicle)
    {
        CurrentDrivingModeId = string.IsNullOrWhiteSpace(vehicle.DefaultDrivingModeId)
            ? FallbackDrivingModeId
            : vehicle.DefaultDrivingModeId;
    }

    public void InitializeTransmissionMode(VehicleProfile vehicle)
    {
        CurrentTransmissionModeId = string.IsNullOrWhiteSpace(vehicle.DefaultTransmissionModeId)
            ? FallbackTransmissionModeId
            : vehicle.DefaultTransmissionModeId;
    }

    public void ToggleIgnition()
    {
        if (IsIgnitionOn)
        {
            IsIgnitionOn = false;
            IsEngineRunning = false;
            ClearDrivingInputs();
            ReturnToNeutral();
            SetSystemMessage("Vehicle Powered Off");
            return;
        }

        IsIgnitionOn = true;
        SetSystemMessage("Press S to start engine");
    }

    public void ToggleEngine()
    {
        if (!IsIgnitionOn)
        {
            IsEngineRunning = false;
            SetSystemMessage("Turn ignition on before starting engine");
            return;
        }

        if (IsEngineRunning)
        {
            IsEngineRunning = false;
            ClearDrivingInputs();
            ReturnToNeutral();
            SetSystemMessage("Engine Stopped - Gear Returned to Neutral");
            return;
        }

        IsEngineRunning = true;
        SetSystemMessage("Engine Running - Idle");
    }

    public void SetAcceleratorPressed(bool isPressed)
    {
        if (!isPressed)
        {
            var wasAcceleratorPressed = IsAcceleratorPressed;
            ClearAccelerator();

            if (wasAcceleratorPressed && IsEngineRunning)
            {
                SetSystemMessage("Engine Running - Idle");
            }

            return;
        }

        if (!IsIgnitionOn)
        {
            ClearAccelerator();
            SetSystemMessage("Turn ignition on before using accelerator");
            return;
        }

        if (!IsEngineRunning)
        {
            ClearAccelerator();
            SetSystemMessage("Start engine before using accelerator");
            return;
        }

        IsAcceleratorPressed = true;
        ThrottlePercent = 100;
        if (CurrentGearNumber != 0 && IsParkingBrakeApplied)
        {
            IsAcceleratorPressed = true;
            ThrottlePercent = 100;
            SetSystemMessage("Release Parking Brake Before Moving");
            return;
        }

        IsAcceleratorPressed = true;
        ThrottlePercent = 100;
        SetSystemMessage(CurrentGearNumber switch
        {
            -1 => "Reversing",
            0 => "Throttle Applied - Neutral Revving",
            _ => "Accelerating"
        });
    }

    public void SetBrakePressed(bool isPressed)
    {
        IsBrakePressed = isPressed;
        BrakePercent = isPressed ? 100 : 0;

        if (isPressed)
        {
            SetSystemMessage(IsAcceleratorPressed ? "Brake Override Active" : "Braking");
        }
    }

    public void TryShiftUp(VehicleProfile vehicle)
    {
        if (!CanShift())
        {
            return;
        }

        if (IsAutomaticTransmission)
        {
            TrySelectAutomaticDriveRange();
            return;
        }

        if (CurrentGearNumber == -1)
        {
            SetSystemMessage("Select Neutral Before Forward Gear", ShiftMessageHoldTicks);
            return;
        }

        if (CurrentGearNumber >= vehicle.ForwardGearCount)
        {
            SetSystemMessage("Already in Highest Gear", ShiftMessageHoldTicks);
            return;
        }

        CurrentGearNumber++;
        SetSystemMessage($"Gear Selected: {CurrentGear} - Driving Dynamics Coming Soon", ShiftMessageHoldTicks);
    }

    public void TryShiftDown(VehicleProfile vehicle)
    {
        if (!CanShift())
        {
            return;
        }

        if (IsAutomaticTransmission)
        {
            TryReturnAutomaticDriveRangeToNeutral();
            return;
        }

        if (CurrentGearNumber == -1)
        {
            SetSystemMessage("Press R to Return to Neutral", ShiftMessageHoldTicks);
            return;
        }

        if (CurrentGearNumber == 0)
        {
            SetSystemMessage("Already in Neutral", ShiftMessageHoldTicks);
            return;
        }

        var nextGearNumber = CurrentGearNumber - 1;

        if (nextGearNumber == 0 && CurrentSpeedKmh > StoppedSpeedThresholdKmh)
        {
            SetSystemMessage("Stop Vehicle Before Selecting Neutral", ShiftMessageHoldTicks);
            return;
        }

        if (nextGearNumber > 0 && CalculateGearRpm(vehicle, nextGearNumber) > vehicle.RevLimiterRpm)
        {
            SetSystemMessage("Shift Down Rejected - Over Rev Risk", ShiftMessageHoldTicks);
            return;
        }

        CurrentGearNumber = nextGearNumber;
        SetSystemMessage($"Gear Selected: {CurrentGear} - Driving Dynamics Coming Soon", ShiftMessageHoldTicks);
    }

    public void TryToggleReverse()
    {
        if (!IsIgnitionOn)
        {
            SetSystemMessage("Turn ignition on before selecting reverse", ShiftMessageHoldTicks);
            return;
        }

        if (!IsEngineRunning)
        {
            SetSystemMessage("Start engine before selecting reverse", ShiftMessageHoldTicks);
            return;
        }

        if (CurrentSpeedKmh > StoppedSpeedThresholdKmh)
        {
            SetSystemMessage("Stop Vehicle Before Selecting Reverse", ShiftMessageHoldTicks);
            return;
        }

        if (IsAcceleratorPressed || IsBrakePressed)
        {
            SetSystemMessage("Release accelerator and brake before selecting reverse", ShiftMessageHoldTicks);
            return;
        }

        if (CurrentGearNumber > 0)
        {
            SetSystemMessage("Select Neutral Before Reverse", ShiftMessageHoldTicks);
            return;
        }

        if (CurrentGearNumber == -1)
        {
            CurrentGearNumber = 0;
            SetSystemMessage("Neutral Selected", ShiftMessageHoldTicks);
            return;
        }

        CurrentGearNumber = -1;
        SetSystemMessage("Reverse Gear Selected", ShiftMessageHoldTicks);
    }

    public void TryToggleParkingBrake()
    {
        if (!IsIgnitionOn)
        {
            SetSystemMessage("Turn ignition on before operating parking brake", ShiftMessageHoldTicks);
            return;
        }

        if (CurrentSpeedKmh > StoppedSpeedThresholdKmh)
        {
            SetSystemMessage("Stop Vehicle Before Applying Parking Brake", ShiftMessageHoldTicks);
            return;
        }

        IsParkingBrakeApplied = !IsParkingBrakeApplied;
        SetSystemMessage(
            IsParkingBrakeApplied ? "Parking Brake Applied" : "Parking Brake Released",
            ShiftMessageHoldTicks);
    }

    public void ReturnToNeutral()
    {
        CurrentGearNumber = 0;
    }

    public void TryCycleDrivingMode(VehicleProfile vehicle)
    {
        if (!IsIgnitionOn)
        {
            SetSystemMessage("Turn ignition on before changing drive mode", ShiftMessageHoldTicks);
            return;
        }

        if (!IsEngineRunning)
        {
            SetSystemMessage("Start engine before changing drive mode", ShiftMessageHoldTicks);
            return;
        }

        if (CurrentSpeedKmh > StoppedSpeedThresholdKmh)
        {
            SetSystemMessage("Stop Vehicle Before Changing Drive Mode", ShiftMessageHoldTicks);
            return;
        }

        if (IsAcceleratorPressed || IsBrakePressed)
        {
            SetSystemMessage("Release Pedals Before Changing Drive Mode", ShiftMessageHoldTicks);
            return;
        }

        var currentModeIndex = GetCurrentDrivingModeIndex(vehicle);
        if (currentModeIndex < 0)
        {
            CurrentDrivingModeId = vehicle.DefaultDrivingModeId;
            SetSystemMessage($"Drive Mode Selected: {GetCurrentDrivingMode(vehicle).DisplayName}", ShiftMessageHoldTicks);
            return;
        }

        var nextModeIndex = (currentModeIndex + 1) % vehicle.DrivingModes.Count;
        CurrentDrivingModeId = vehicle.DrivingModes[nextModeIndex].Id;
        SetSystemMessage($"Drive Mode Selected: {vehicle.DrivingModes[nextModeIndex].DisplayName}", ShiftMessageHoldTicks);
    }

    public void TryCycleTransmissionMode(VehicleProfile vehicle)
    {
        if (!IsIgnitionOn)
        {
            SetSystemMessage("Turn ignition on before changing transmission mode", ShiftMessageHoldTicks);
            return;
        }

        if (!IsEngineRunning)
        {
            SetSystemMessage("Start engine before changing transmission mode", ShiftMessageHoldTicks);
            return;
        }

        if (CurrentSpeedKmh > StoppedSpeedThresholdKmh)
        {
            SetSystemMessage("Stop Vehicle Before Changing Transmission Mode", ShiftMessageHoldTicks);
            return;
        }

        if (CurrentGearNumber != 0)
        {
            SetSystemMessage("Select Neutral Before Changing Transmission Mode", ShiftMessageHoldTicks);
            return;
        }

        if (IsAcceleratorPressed || IsBrakePressed)
        {
            SetSystemMessage("Release Pedals Before Changing Transmission Mode", ShiftMessageHoldTicks);
            return;
        }

        var currentModeIndex = GetCurrentTransmissionModeIndex(vehicle);
        if (currentModeIndex < 0)
        {
            CurrentTransmissionModeId = vehicle.DefaultTransmissionModeId;
        }
        else
        {
            var nextModeIndex = (currentModeIndex + 1) % vehicle.SupportedTransmissionModeIds.Count;
            CurrentTransmissionModeId = vehicle.SupportedTransmissionModeIds[nextModeIndex];
        }

        automaticShiftCooldownRemainingMilliseconds = 0.0;
        SetSystemMessage($"Transmission Mode Selected: {GetTransmissionModeDisplayName()}", ShiftMessageHoldTicks);
    }

    public void UpdateSimulation(VehicleProfile vehicle, double deltaTimeSeconds)
    {
        UpdateSpeed(vehicle, deltaTimeSeconds);
        UpdateRpm(vehicle);
    }

    public AutomaticShiftResult UpdateAutomaticTransmission(VehicleProfile vehicle, double elapsedMilliseconds)
    {
        if (automaticShiftCooldownRemainingMilliseconds > 0.0)
        {
            automaticShiftCooldownRemainingMilliseconds = double.Max(
                0.0,
                automaticShiftCooldownRemainingMilliseconds - elapsedMilliseconds);
        }

        if (!IsAutomaticTransmission || !IsIgnitionOn || !IsEngineRunning || CurrentGearNumber < 1)
        {
            return AutomaticShiftResult.None;
        }

        if (CurrentSpeedKmh <= StoppedSpeedThresholdKmh)
        {
            if (CurrentGearNumber > 1)
            {
                CurrentGearNumber = 1;
            }

            automaticShiftCooldownRemainingMilliseconds = 0.0;
            return AutomaticShiftResult.None;
        }

        if (automaticShiftCooldownRemainingMilliseconds > 0.0)
        {
            return AutomaticShiftResult.None;
        }

        var drivingMode = GetCurrentDrivingMode(vehicle);
        if (CurrentGearNumber < vehicle.ForwardGearCount
            && CurrentRpm >= drivingMode.AutomaticUpshiftRpm)
        {
            CurrentGearNumber++;
            automaticShiftCooldownRemainingMilliseconds = drivingMode.AutomaticShiftCooldownMilliseconds;
            SetSystemMessage($"Automatic Shift Up: {CurrentGear}", ShiftMessageHoldTicks);
            return AutomaticShiftResult.ShiftUp;
        }

        if (CurrentGearNumber >= 2 && CurrentRpm <= drivingMode.AutomaticDownshiftRpm)
        {
            var nextGearNumber = CurrentGearNumber - 1;
            var predictedRpm = CalculateGearRpm(vehicle, nextGearNumber);
            if (predictedRpm <= vehicle.RevLimiterRpm)
            {
                CurrentGearNumber = nextGearNumber;
                automaticShiftCooldownRemainingMilliseconds = drivingMode.AutomaticShiftCooldownMilliseconds;
                SetSystemMessage($"Automatic Shift Down: {CurrentGear}", ShiftMessageHoldTicks);
                return AutomaticShiftResult.ShiftDown;
            }
        }

        return AutomaticShiftResult.None;
    }

    public void UpdateRpm(VehicleProfile vehicle)
    {
        var targetRpm = GetTargetRpm(vehicle);

        if (CurrentRpm < targetRpm)
        {
            CurrentRpm = int.Min(CurrentRpm + GetRpmIncreaseStep(vehicle), targetRpm);
            return;
        }

        if (CurrentRpm > targetRpm)
        {
            CurrentRpm = int.Max(CurrentRpm - GetRpmDecreaseStep(vehicle), targetRpm);
        }
    }

    private void ClearAccelerator()
    {
        IsAcceleratorPressed = false;
        ThrottlePercent = 0;
    }

    private void ClearBrake()
    {
        IsBrakePressed = false;
        BrakePercent = 0;
    }

    private void ClearDrivingInputs()
    {
        ClearAccelerator();
        ClearBrake();
    }

    private bool CanShift()
    {
        if (!IsIgnitionOn)
        {
            SetSystemMessage("Turn ignition on before shifting", ShiftMessageHoldTicks);
            return false;
        }

        if (!IsEngineRunning)
        {
            SetSystemMessage("Start engine before shifting", ShiftMessageHoldTicks);
            return false;
        }

        if (IsAcceleratorPressed)
        {
            SetSystemMessage("Release accelerator before shifting in prototype mode", ShiftMessageHoldTicks);
            return false;
        }

        return true;
    }

    private int GetTargetRpm(VehicleProfile vehicle)
    {
        if (!IsIgnitionOn || !IsEngineRunning)
        {
            return 0;
        }

        if (CurrentGearNumber == 0)
        {
            return IsAcceleratorPressed && !IsBrakePressed ? vehicle.RevLimiterRpm : vehicle.IdleRpm;
        }

        var gearRpm = CurrentGearNumber == -1
            ? CurrentSpeedKmh * vehicle.ReverseRpmPerKmh
            : CalculateGearRpm(vehicle, CurrentGearNumber);
        return (int)Math.Clamp(gearRpm, vehicle.IdleRpm, vehicle.RevLimiterRpm);
    }

    private int GetRpmIncreaseStep(VehicleProfile vehicle)
    {
        return IsAcceleratorPressed && CurrentGearNumber == 0 && vehicle.RevLimiterRpm > vehicle.IdleRpm
            ? AccelerationRpmStep
            : EngineStartRpmStep;
    }

    private int GetRpmDecreaseStep(VehicleProfile vehicle)
    {
        return IsEngineRunning && CurrentRpm > vehicle.IdleRpm
            ? ReturnToIdleRpmStep
            : EngineStopRpmStep;
    }

    private void UpdateSpeed(VehicleProfile vehicle, double deltaTimeSeconds)
    {
        if (IsBrakePressed)
        {
            CurrentSpeedKmh = double.Max(0.0, CurrentSpeedKmh - vehicle.BrakeDecelerationKmhPerSecond * deltaTimeSeconds);
            SetSystemMessage(IsAcceleratorPressed ? "Brake Override Active" : "Braking");
            TickMessageHold();
            return;
        }

        if (IsEngineRunning && IsAcceleratorPressed && CurrentGearNumber > 0)
        {
            AccelerateInForwardGear(vehicle, deltaTimeSeconds);
            return;
        }

        if (IsEngineRunning && IsAcceleratorPressed && CurrentGearNumber == -1)
        {
            AccelerateInReverseGear(vehicle, deltaTimeSeconds);
            return;
        }

        if (CurrentSpeedKmh > 0.0)
        {
            var coastDeceleration = vehicle.CoastDecelerationKmhPerSecond
                * GetCurrentDrivingMode(vehicle).CoastDecelerationMultiplier;
            CurrentSpeedKmh = double.Max(0.0, CurrentSpeedKmh - coastDeceleration * deltaTimeSeconds);

            if (CurrentSpeedKmh > 0.0 && CurrentGearNumber > 0)
            {
                SetPassiveSystemMessage("Coasting");
            }
            else if (CurrentSpeedKmh > 0.0 && CurrentGearNumber == -1)
            {
                SetPassiveSystemMessage("Reverse Coasting");
            }
        }

        if (CurrentSpeedKmh <= StoppedSpeedThresholdKmh)
        {
            CurrentSpeedKmh = 0.0;

            if (IsEngineRunning && CurrentGearNumber > 0 && !IsAcceleratorPressed)
            {
                SetPassiveSystemMessage("Engine Running - Idle");
            }
            else if (IsEngineRunning && CurrentGearNumber == -1 && !IsAcceleratorPressed)
            {
                SetPassiveSystemMessage("Engine Running - Idle");
            }
        }

        TickMessageHold();
    }

    private void AccelerateInForwardGear(VehicleProfile vehicle, double deltaTimeSeconds)
    {
        if (IsParkingBrakeApplied)
        {
            SetSystemMessage("Release Parking Brake Before Moving");
            return;
        }

        var acceleration = vehicle.AccelerationKmhPerSecondByGear[CurrentGearNumber]
            * GetCurrentDrivingMode(vehicle).AccelerationMultiplier;
        var nextSpeed = double.Min(
            vehicle.MaxSimulationSpeedKmh,
            CurrentSpeedKmh + acceleration * deltaTimeSeconds);

        var nextRpm = CalculateGearRpm(vehicle, CurrentGearNumber, nextSpeed);

        if (nextRpm >= vehicle.RevLimiterRpm)
        {
            CurrentSpeedKmh = double.Min(CurrentSpeedKmh, vehicle.RevLimiterRpm / vehicle.RpmPerKmhByGear[CurrentGearNumber]);
            SetSystemMessage("Rev Limit Reached - Shift Up");
            return;
        }

        CurrentSpeedKmh = nextSpeed;
        SetSystemMessage("Accelerating");
    }

    private void AccelerateInReverseGear(VehicleProfile vehicle, double deltaTimeSeconds)
    {
        if (IsParkingBrakeApplied)
        {
            SetSystemMessage("Release Parking Brake Before Moving");
            return;
        }

        var nextSpeed = double.Min(
            vehicle.MaxReverseSpeedKmh,
            CurrentSpeedKmh + vehicle.ReverseAccelerationKmhPerSecond
            * GetCurrentDrivingMode(vehicle).AccelerationMultiplier
            * deltaTimeSeconds);

        var nextRpm = nextSpeed * vehicle.ReverseRpmPerKmh;

        if (nextRpm >= vehicle.RevLimiterRpm)
        {
            CurrentSpeedKmh = double.Min(CurrentSpeedKmh, vehicle.RevLimiterRpm / vehicle.ReverseRpmPerKmh);
            SetSystemMessage("Rev Limit Reached");
            return;
        }

        CurrentSpeedKmh = nextSpeed;
        SetSystemMessage("Reversing");
    }

    private double CalculateGearRpm(VehicleProfile vehicle, int gearNumber)
    {
        return CalculateGearRpm(vehicle, gearNumber, CurrentSpeedKmh);
    }

    private static double CalculateGearRpm(VehicleProfile vehicle, int gearNumber, double speedKmh)
    {
        return speedKmh * vehicle.RpmPerKmhByGear[gearNumber];
    }

    public string GetTransmissionModeDisplayName()
    {
        return IsAutomaticTransmission ? "AUTO" : "MANUAL";
    }

    public DrivingModeProfile GetCurrentDrivingMode(VehicleProfile vehicle)
    {
        return vehicle.DrivingModes.FirstOrDefault(
                mode => string.Equals(mode.Id, CurrentDrivingModeId, StringComparison.OrdinalIgnoreCase))
            ?? vehicle.DrivingModes.First(
                mode => string.Equals(mode.Id, vehicle.DefaultDrivingModeId, StringComparison.OrdinalIgnoreCase));
    }

    private int GetCurrentDrivingModeIndex(VehicleProfile vehicle)
    {
        for (var i = 0; i < vehicle.DrivingModes.Count; i++)
        {
            if (string.Equals(vehicle.DrivingModes[i].Id, CurrentDrivingModeId, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private int GetCurrentTransmissionModeIndex(VehicleProfile vehicle)
    {
        for (var i = 0; i < vehicle.SupportedTransmissionModeIds.Count; i++)
        {
            if (string.Equals(
                vehicle.SupportedTransmissionModeIds[i],
                CurrentTransmissionModeId,
                StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private void TrySelectAutomaticDriveRange()
    {
        if (CurrentGearNumber == -1)
        {
            SetSystemMessage("Select Neutral Before Forward Gear", ShiftMessageHoldTicks);
            return;
        }

        if (CurrentGearNumber == 0)
        {
            CurrentGearNumber = 1;
            automaticShiftCooldownRemainingMilliseconds = 0.0;
            SetSystemMessage("Drive Selected - Automatic Mode", ShiftMessageHoldTicks);
            return;
        }

        SetSystemMessage("Automatic Transmission Controls Forward Gears", ShiftMessageHoldTicks);
    }

    private void TryReturnAutomaticDriveRangeToNeutral()
    {
        if (CurrentGearNumber == -1)
        {
            SetSystemMessage("Press R to Return to Neutral", ShiftMessageHoldTicks);
            return;
        }

        if (CurrentGearNumber == 0)
        {
            SetSystemMessage("Already in Neutral", ShiftMessageHoldTicks);
            return;
        }

        if (CurrentSpeedKmh > StoppedSpeedThresholdKmh)
        {
            SetSystemMessage("Stop Vehicle Before Selecting Neutral", ShiftMessageHoldTicks);
            return;
        }

        CurrentGearNumber = 0;
        automaticShiftCooldownRemainingMilliseconds = 0.0;
        SetSystemMessage("Neutral Selected", ShiftMessageHoldTicks);
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

public enum AutomaticShiftResult
{
    None,
    ShiftUp,
    ShiftDown
}
