using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WeightBalance.Models;

namespace WeightBalance.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // ── Editable inputs ─────────────────────────────────────────────────
    [ObservableProperty] private double _emptyMass        = AircraftConstants.DefaultEmptyMass;
    [ObservableProperty] private double _pilotMass        = AircraftConstants.DefaultPilotMass;
    [ObservableProperty] private double _baggageArea1Mass = AircraftConstants.DefaultBaggageArea1Mass;
    [ObservableProperty] private double _baggageArea2Mass = AircraftConstants.DefaultBaggageArea2Mass;
    [ObservableProperty] private double _fuelMass         = AircraftConstants.DefaultFuelMass;
    [ObservableProperty] private double _fuelBurnMass     = AircraftConstants.DefaultFuelBurnMass;

    // Trigger full recalc when any takeoff input changes
    partial void OnEmptyMassChanged(double value)        => NotifyAllOutputs();
    partial void OnPilotMassChanged(double value)        => NotifyAllOutputs();
    partial void OnBaggageArea1MassChanged(double value) => NotifyAllOutputs();
    partial void OnBaggageArea2MassChanged(double value) => NotifyAllOutputs();
    partial void OnFuelMassChanged(double value)         => NotifyAllOutputs();
    partial void OnFuelBurnMassChanged(double value)     => NotifyLandingOutputs();

    // ── Fixed arms (exposed for AXAML binding) ───────────────────────────
    public double EmptyArm        => AircraftConstants.EmptyArm;
    public double PilotArm        => AircraftConstants.PilotArm;
    public double BaggageArea1Arm => AircraftConstants.BaggageArea1Arm;
    public double BaggageArea2Arm => AircraftConstants.BaggageArea2Arm;
    public double FuelArm         => AircraftConstants.FuelArm;

    // ── Per-row moments ──────────────────────────────────────────────────
    public double EmptyMoment        => EmptyMass        * AircraftConstants.EmptyArm;
    public double PilotMoment        => PilotMass        * AircraftConstants.PilotArm;
    public double BaggageArea1Moment => BaggageArea1Mass * AircraftConstants.BaggageArea1Arm;
    public double BaggageArea2Moment => BaggageArea2Mass * AircraftConstants.BaggageArea2Arm;
    public double FuelMoment         => FuelMass         * AircraftConstants.FuelArm;
    public double FuelBurnMoment     => FuelBurnMass     * AircraftConstants.FuelArm;

    // ── Takeoff totals ───────────────────────────────────────────────────
    public double TotalMassTakeoff =>
        EmptyMass + PilotMass + BaggageArea1Mass + BaggageArea2Mass + FuelMass;

    public double TotalMomentTakeoff =>
        EmptyMoment + PilotMoment + BaggageArea1Moment + BaggageArea2Moment + FuelMoment;

    public double CgMmTakeoff =>
        TotalMassTakeoff == 0 ? 0 : TotalMomentTakeoff / TotalMassTakeoff;

    public double CgPctMacTakeoff =>
        100.0 * (CgMmTakeoff - AircraftConstants.Lemac) / AircraftConstants.Mac;

    public bool IsTakeoffWithinEnvelope =>
        IsPointInEnvelope(TotalMassTakeoff, CgPctMacTakeoff);

    // ── Fuel suggestion ──────────────────────────────────────────────────
    // Largest fuel load (empty/pilot/baggage held fixed) that keeps the
    // takeoff point within the CG envelope and at/under MTOW.
    public double SuggestedFuelMass => CalculateMaxFuelWithinEnvelope();

    public double SuggestedFuelLiters => SuggestedFuelMass / AircraftConstants.FuelDensity;

    public string SuggestedFuelText =>
        SuggestedFuelMass > 0
            ? $"suggest ≈ {SuggestedFuelLiters:F1} L ({SuggestedFuelMass:F1} kg) to stay within envelope"
            : "no fuel load keeps this within the envelope — reduce load";

    [RelayCommand]
    private void ApplySuggestedFuel() => FuelMass = SuggestedFuelMass;

    private double CalculateMaxFuelWithinEnvelope()
    {
        double baseMass   = EmptyMass + PilotMass + BaggageArea1Mass + BaggageArea2Mass;
        double baseMoment = EmptyMoment + PilotMoment + BaggageArea1Moment + BaggageArea2Moment;

        double upperBound = Math.Min(AircraftConstants.MaxFuelMass,
                                      AircraftConstants.MaxTakeOffWeight - baseMass);
        if (upperBound <= 0) return 0;

        const double stepKg = 0.05;
        for (double f = upperBound; f >= 0; f -= stepKg)
        {
            double mass   = baseMass + f;
            double moment = baseMoment + f * AircraftConstants.FuelArm;
            double cgPct  = 100.0 * (moment / mass - AircraftConstants.Lemac) / AircraftConstants.Mac;
            if (IsPointInEnvelope(mass, cgPct))
                return Math.Round(f, 1);
        }
        return 0;
    }

    // ── Landing totals ───────────────────────────────────────────────────
    public double TotalMassLanding   => TotalMassTakeoff - FuelBurnMass;
    public double TotalMomentLanding => TotalMomentTakeoff - FuelBurnMoment;

    public double CgMmLanding =>
        TotalMassLanding == 0 ? 0 : TotalMomentLanding / TotalMassLanding;

    public double CgPctMacLanding =>
        100.0 * (CgMmLanding - AircraftConstants.Lemac) / AircraftConstants.Mac;

    public bool IsLandingWithinEnvelope =>
        IsPointInEnvelope(TotalMassLanding, CgPctMacLanding);

    // ── Helpers ──────────────────────────────────────────────────────────
    private static bool IsPointInEnvelope(double mass, double cgPct)
    {
        var v = AircraftConstants.EnvelopeVertices;
        int n = v.Length;
        bool inside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            bool crosses = (v[i].Mass > mass) != (v[j].Mass > mass) &&
                           cgPct < v[j].CgPct + (v[i].CgPct - v[j].CgPct)
                                   * (mass - v[j].Mass) / (v[i].Mass - v[j].Mass);
            if (crosses) inside = !inside;
        }
        return inside;
    }

    private void NotifyAllOutputs()
    {
        OnPropertyChanged(nameof(EmptyMoment));
        OnPropertyChanged(nameof(PilotMoment));
        OnPropertyChanged(nameof(BaggageArea1Moment));
        OnPropertyChanged(nameof(BaggageArea2Moment));
        OnPropertyChanged(nameof(FuelMoment));
        OnPropertyChanged(nameof(TotalMassTakeoff));
        OnPropertyChanged(nameof(TotalMomentTakeoff));
        OnPropertyChanged(nameof(CgMmTakeoff));
        OnPropertyChanged(nameof(CgPctMacTakeoff));
        OnPropertyChanged(nameof(IsTakeoffWithinEnvelope));
        OnPropertyChanged(nameof(SuggestedFuelMass));
        OnPropertyChanged(nameof(SuggestedFuelLiters));
        OnPropertyChanged(nameof(SuggestedFuelText));
        NotifyLandingOutputs();
    }

    private void NotifyLandingOutputs()
    {
        OnPropertyChanged(nameof(FuelBurnMoment));
        OnPropertyChanged(nameof(TotalMassLanding));
        OnPropertyChanged(nameof(TotalMomentLanding));
        OnPropertyChanged(nameof(CgMmLanding));
        OnPropertyChanged(nameof(CgPctMacLanding));
        OnPropertyChanged(nameof(IsLandingWithinEnvelope));
    }
}
