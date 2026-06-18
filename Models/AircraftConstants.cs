namespace WeightBalance.Models;

public static class AircraftConstants
{
    // MAC reference — Sonair-S2-D-MAN-001 Rev B, §6-5
    public const double Lemac = 612.0;   // mm from datum (leading edge of MAC)
    public const double Mac   = 1318.0;  // mm (mean aerodynamic chord)

    // Fixed station arms (mm from datum)
    public const double EmptyArm        = 866.0;
    public const double PilotArm        = 1150.0;
    public const double BaggageArea1Arm = 1500.0;
    public const double BaggageArea2Arm = 2296.0;
    public const double FuelArm         = 706.0;

    // Default masses (kg)
    public const double DefaultEmptyMass        = 484.5;
    public const double DefaultPilotMass        = 175.0;
    public const double DefaultBaggageArea1Mass = 2.0;
    public const double DefaultBaggageArea2Mass = 0.0;
    public const double DefaultFuelMass         = 100.0;
    public const double DefaultFuelBurnMass     = 15.0;

    // Fuel — §2.6
    public const double FuelDensity   = 0.72;   // kg/L (avgas density)
    public const double MaxFuelLiters = 140.0;  // 2 tanks × 70 L
    public const double MaxFuelMass   = MaxFuelLiters * FuelDensity; // 100.8 kg

    // Mass limits — §2.6
    public const double MaxTakeOffWeight        = 750.0;
    public const double MaxBaggageArea1         = 35.0;
    public const double MaxBaggageArea2         = 25.0;
    public const double MaxBaggageCombined      = 35.0;

    // Chart display extents
    public const double ChartMinMass  = 400.0;
    public const double ChartMaxMass  = 800.0;
    public const double ChartMinCgPct = 15.0;
    public const double ChartMaxCgPct = 35.0;

    // CG Envelope vertices (% MAC, kg) — POH §2.7 "Datum & Center of Gravity Range"
    //   Forward limit at ≤ 650 kg : 0.876 m = 20% MAC
    //   Forward limit at   750 kg : 0.915 m = 23% MAC
    //   Rear    limit at ≤ 650 kg : 1.021 m = 31% MAC
    //   Rear    limit at   750 kg : 1.034 m = 32% MAC
    public static readonly (double CgPct, double Mass)[] EnvelopeVertices =
    [
        (20.0, 400.0),   // forward lower  (extrapolated down)
        (20.0, 650.0),   // forward knee   (exact from POH)
        (23.0, 750.0),   // forward upper  (exact from POH)
        (32.0, 750.0),   // aft upper      (exact from POH)
        (31.0, 650.0),   // aft knee       (exact from POH)
        (31.0, 400.0),   // aft lower      (extrapolated down)
    ];
}
