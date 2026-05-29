namespace VehicleMeterSimulator.Models;

public class VehicleRuntimeState
{
    private const int EngineStartRpmStep = 100;
    private const int AccelerationRpmStep = 300;
    private const int ReturnToIdleRpmStep = 220;
    private const int EngineStopRpmStep = 150;
    private const double StoppedSpeedThresholdKmh = 0.1;
    private const int ShiftMessageHoldTicks = 20;

    private int messageHoldTicks = 0;

    public bool IsIgnitionOn { get; private set; } = false;

    public bool IsEngineRunning { get; private set; } = false;

    public bool IsAcceleratorPressed { get; private set; } = false;

    public int ThrottlePercent { get; private set; } = 0;

    public bool IsBrakePressed { get; private set; } = false;

    public int BrakePercent { get; private set; } = 0;

    public double CurrentSpeedKmh { get; private set; } = 0.0;

    public int CurrentRpm { get; private set; } = 0;

    public int CurrentGearNumber { get; private set; } = 0;

    public string CurrentGear => CurrentGearNumber == 0 ? "N" : CurrentGearNumber.ToString();

    public string SystemMessage { get; private set; } = "Press I to switch ignition on";

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
        SetSystemMessage(CurrentGearNumber == 0 ? "Throttle Applied - Neutral Revving" : "Accelerating");
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

    public void ReturnToNeutral()
    {
        CurrentGearNumber = 0;
    }

    public void UpdateSimulation(VehicleProfile vehicle, double deltaTimeSeconds)
    {
        UpdateSpeed(vehicle, deltaTimeSeconds);
        UpdateRpm(vehicle);
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

        var gearRpm = CalculateGearRpm(vehicle, CurrentGearNumber);
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

        if (CurrentSpeedKmh > 0.0)
        {
            CurrentSpeedKmh = double.Max(0.0, CurrentSpeedKmh - vehicle.CoastDecelerationKmhPerSecond * deltaTimeSeconds);

            if (CurrentSpeedKmh > 0.0 && CurrentGearNumber > 0)
            {
                SetPassiveSystemMessage("Coasting");
            }
        }

        if (CurrentSpeedKmh <= StoppedSpeedThresholdKmh)
        {
            CurrentSpeedKmh = 0.0;

            if (IsEngineRunning && CurrentGearNumber > 0 && !IsAcceleratorPressed)
            {
                SetPassiveSystemMessage("Engine Running - Idle");
            }
        }

        TickMessageHold();
    }

    private void AccelerateInForwardGear(VehicleProfile vehicle, double deltaTimeSeconds)
    {
        var acceleration = vehicle.AccelerationKmhPerSecondByGear[CurrentGearNumber];
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

    private double CalculateGearRpm(VehicleProfile vehicle, int gearNumber)
    {
        return CalculateGearRpm(vehicle, gearNumber, CurrentSpeedKmh);
    }

    private static double CalculateGearRpm(VehicleProfile vehicle, int gearNumber, double speedKmh)
    {
        return speedKmh * vehicle.RpmPerKmhByGear[gearNumber];
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
