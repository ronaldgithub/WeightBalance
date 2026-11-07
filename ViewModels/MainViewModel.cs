using System;
using System.Collections.Generic;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WeightBalance.Models;

namespace WeightBalance.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // ── Editable inputs ─────────────────────────────────────────────────
    // The fields hold text. Only valid text updates the mass, so a blank or malformed
    // field keeps the last valid mass and MassInputError says so.
    [ObservableProperty] private string _emptyMassText        = FormatMass(AircraftConstants.DefaultEmptyMass);
    [ObservableProperty] private string _pilotMassText        = FormatMass(AircraftConstants.DefaultPilotMass);
    [ObservableProperty] private string _baggageArea1MassText = FormatMass(AircraftConstants.DefaultBaggageArea1Mass);
    [ObservableProperty] private string _baggageArea2MassText = FormatMass(AircraftConstants.DefaultBaggageArea2Mass);
    [ObservableProperty] private string _fuelMassText         = FormatMass(AircraftConstants.DefaultFuelMass);
    [ObservableProperty] private string _fuelBurnMassText     = FormatMass(AircraftConstants.DefaultFuelBurnMass);

    partial void OnEmptyMassTextChanged(string value)        => ApplyMassText(value, m => EmptyMass = m);
    partial void OnPilotMassTextChanged(string value)        => ApplyMassText(value, m => PilotMass = m);
    partial void OnBaggageArea1MassTextChanged(string value) => ApplyMassText(value, m => BaggageArea1Mass = m);
    partial void OnBaggageArea2MassTextChanged(string value) => ApplyMassText(value, m => BaggageArea2Mass = m);
    partial void OnFuelMassTextChanged(string value)         => ApplyMassText(value, m => FuelMass = m);
    partial void OnFuelBurnMassTextChanged(string value)     => ApplyMassText(value, m => FuelBurnMass = m);

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

    public string MassInputError
    {
        get
        {
            var errors = new List<string>();
            AddMassError(errors, "Empty aircraft",     EmptyMassText,        EmptyMass);
            AddMassError(errors, "Pilot & Passengers", PilotMassText,        PilotMass);
            AddMassError(errors, "Baggage (area 1)",   BaggageArea1MassText, BaggageArea1Mass);
            AddMassError(errors, "Baggage (area 2)",   BaggageArea2MassText, BaggageArea2Mass);
            AddMassError(errors, "Fuel",               FuelMassText,         FuelMass);
            AddMassError(errors, "Fuel burn",          FuelBurnMassText,     FuelBurnMass);
            return string.Join("\n", errors);
        }
    }

    private static void AddMassError(List<string> errors, string field, string text, double lastValid)
    {
        if (ParseMass(text) == null)
            errors.Add($"{field}: enter 0 or more, using a dot for decimals. Totals still use {FormatMass(lastValid)} kg.");
    }

    private void ApplyMassText(string text, Action<double> setMass)
    {
        if (ParseMass(text) is { } mass) setMass(mass);
        OnPropertyChanged(nameof(MassInputError));
        UpdateTakeoffLoad();
        UpdateLandingLoad();
    }

    private static double? ParseMass(string? text) => NumberInput.Parse(text) is >= 0 and var mass ? mass : null;

    private static string FormatMass(double mass) => mass.ToString("F1", CultureInfo.InvariantCulture);

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

    public string FuelLitersText =>
        $"[{(FuelMass / AircraftConstants.FuelDensity).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} liter]";

    public string FuelBurnDurationText
    {
        get
        {
            double hours = FuelBurnMass / AircraftConstants.FuelBurnKgPerHour;
            double rounded = Math.Round(hours, 1);
            string unit = rounded == 1 ? "hour" : "hours";
            return $"[{rounded.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} {unit}]";
        }
    }

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

    public string SuggestedFuelExplanation
    {
        get
        {
            double baseMass   = EmptyMass + PilotMass + BaggageArea1Mass + BaggageArea2Mass;
            double baseMoment = EmptyMoment + PilotMoment + BaggageArea1Moment + BaggageArea2Moment;
            double mtowRoom   = AircraftConstants.MaxTakeOffWeight - baseMass;
            double tankKg     = AircraftConstants.MaxFuelMass;
            double upper      = Math.Min(tankKg, mtowRoom);
            double fuel       = SuggestedFuelMass;
            if (fuel <= 0) return "";

            string limiter = tankKg <= mtowRoom ? "tank" : "MTOW";
            double mass    = baseMass + fuel;
            double cgPct   = 100.0 * ((baseMoment + fuel * AircraftConstants.FuelArm) / mass
                                      - AircraftConstants.Lemac) / AircraftConstants.Mac;

            string last = fuel < Math.Round(upper, 1) - 0.05
                ? $"CG envelope limits it further, down to {fuel:F1} kg"
                : $"CG at {fuel:F1} kg = {cgPct:F2}% MAC → inside envelope";

            return
                $"Non-fuel: {EmptyMass:F1} + {PilotMass:F1} + {BaggageArea1Mass:F1} + {BaggageArea2Mass:F1} = {baseMass:F1} kg\n" +
                $"MTOW room: {AircraftConstants.MaxTakeOffWeight:F1} − {baseMass:F1} = {mtowRoom:F1} kg\n" +
                $"Tank: 140 L × {AircraftConstants.FuelDensity} = {tankKg:F1} kg   → lower of the two = {upper:F1} kg ({limiter} limits)\n" +
                $"{last}\n" +
                $"{fuel:F1} kg ÷ {AircraftConstants.FuelDensity} = {fuel / AircraftConstants.FuelDensity:F1} L";
        }
    }

    [RelayCommand]
    private void ApplySuggestedFuel() => FuelMassText = FormatMass(SuggestedFuelMass);

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

    // ── Takeoff and Landing tabs ─────────────────────────────────────────
    public RunwayPerformanceViewModel Takeoff { get; } = new(
        RunwayPerformance.Takeoff,
        "Takeoff — Sonaca S2 (Sling 2)",
        "POH §5.3.6  ·  Flaps T/O  ·  Full power  ·  Rotate at VLOF = 55 KIAS",
        "Takeoff run (estimate)",
        "Takeoff distance over 15 m (50 ft)");

    public RunwayPerformanceViewModel Landing { get; } = new(
        RunwayPerformance.Landing,
        "Landing — Sonaca S2 (Sling 2)",
        "POH §5.3.10  ·  Flaps LD  ·  Idle power  ·  Approach at 55 KIAS",
        "Landing run, braked (estimate)",
        "Landing distance over 15 m (50 ft)",
        isLanding: true);

    public MainViewModel()
    {
        UpdateTakeoffLoad();
        UpdateLandingLoad();
    }

    private void UpdateTakeoffLoad()
    {
        Takeoff.MassText    = $"T/O mass from Weight & Balance: {TotalMassTakeoff:F1} kg";
        Takeoff.MassWarning = LoadWarning(TotalMassTakeoff, IsTakeoffWithinEnvelope);
    }

    private void UpdateLandingLoad()
    {
        Landing.MassText    = $"Landing mass from Weight & Balance: {TotalMassLanding:F1} kg";
        Landing.MassWarning = LoadWarning(TotalMassLanding, IsLandingWithinEnvelope);
    }

    private string LoadWarning(double mass, bool withinEnvelope) =>
        MassInputError.Length > 0
            ? "Weight & Balance has an invalid entry, so this mass may be wrong. Correct it on the first tab."
        : mass > AircraftConstants.MaxTakeOffWeight
            ? $"Over {AircraftConstants.MaxTakeOffWeight:F0} kg MTOW: these distances are not valid for this load."
        : !withinEnvelope
            ? "Outside the CG envelope: these distances are not valid for this load."
            : "";

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
        OnPropertyChanged(nameof(FuelLitersText));
        OnPropertyChanged(nameof(TotalMassTakeoff));
        OnPropertyChanged(nameof(TotalMomentTakeoff));
        OnPropertyChanged(nameof(CgMmTakeoff));
        OnPropertyChanged(nameof(CgPctMacTakeoff));
        OnPropertyChanged(nameof(IsTakeoffWithinEnvelope));
        OnPropertyChanged(nameof(SuggestedFuelMass));
        OnPropertyChanged(nameof(SuggestedFuelLiters));
        OnPropertyChanged(nameof(SuggestedFuelText));
        OnPropertyChanged(nameof(SuggestedFuelExplanation));
        UpdateTakeoffLoad();
        NotifyLandingOutputs();
    }

    private void NotifyLandingOutputs()
    {
        OnPropertyChanged(nameof(FuelBurnMoment));
        OnPropertyChanged(nameof(FuelBurnDurationText));
        OnPropertyChanged(nameof(TotalMassLanding));
        OnPropertyChanged(nameof(TotalMomentLanding));
        OnPropertyChanged(nameof(CgMmLanding));
        OnPropertyChanged(nameof(CgPctMacLanding));
        OnPropertyChanged(nameof(IsLandingWithinEnvelope));
        UpdateLandingLoad();
    }
}
