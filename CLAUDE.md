# CLAUDE.md — Weight & Balance Calculator (Sonaca S2 / Sling 2)

## Project overview

Desktop Weight & Balance calculator for the Sonaca S2 (also sold as the Sling 2) light aircraft.
Built with C# / Avalonia 12 / .NET 10, MVVM via CommunityToolkit.Mvvm.

## Build & run

```
dotnet build
dotnet run
```

No extra setup required — all packages restore automatically.

## Architecture

```
WeightBalance/
├── Models/
│   ├── AircraftConstants.cs      — all POH constants (arms, envelope, fuel)
│   ├── RunwayPerformance.cs      — POH §5.3.6 / §5.3.10 tables and corrections
│   └── NumberInput.cs            — strict number parsing for all input fields
├── ViewModels/
│   ├── MainViewModel.cs          — inputs, computed totals, envelope check
│   └── RunwayPerformanceViewModel.cs — inputs and outputs of one Takeoff/Landing tab
├── Converters/
│   └── BoolToEnvelopeColorConverter.cs
├── Controls/
│   ├── CentogramControl.cs       — custom-rendered centogram chart
│   └── RunwayPerformancePanel.axaml(.cs) — shared Takeoff/Landing tab layout
├── Assets/
│   └── icon.ico                   — app/window icon (multi-res)
├── App.axaml / App.axaml.cs      — FluentTheme + converter resources
├── MainWindow.axaml               — TabControl: Weight & Balance (input | centogram), Takeoff, Landing
└── MainWindow.axaml.cs            — sets DataContext = new MainViewModel()
```

Compiled bindings are **disabled** (`AvaloniaUseCompiledBindingsByDefault=false`) so
reflection bindings with converters work without `x:DataType` annotations.

`MainWindow.Title` includes the maintainer contact
(`ronald.de.groot@opendata.nl`) after the aircraft name — keep it when editing
the title string.

## Key domain knowledge

**CG formula** (Sonair-S2-D-MAN-001 Rev B, §6-5):
- CG position (mm) = Total Moment / Total Mass
- CG % MAC = 100 × (CG_mm − 612) / 1318   [LEMAC = 612 mm, MAC = 1318 mm]

**Envelope vertices** (POH §2.7) — defined as `(% MAC, kg)` pairs in `AircraftConstants`:

| % MAC | Mass (kg) | POH source |
|---|---|---|
| 20 | 400 | forward lower (extrapolated) |
| 20 | 650 | forward limit ≤ 650 kg (exact) |
| 23 | 750 | forward limit at MTOW (exact) |
| 32 | 750 | aft limit at MTOW (exact) |
| 31 | 650 | aft limit ≤ 650 kg (exact) |
| 31 | 400 | aft lower (extrapolated) |

**Fixed station arms** (all in mm from datum):

| Station | Arm (mm) |
|---|---|
| Empty aircraft | 866 |
| Pilot & Passengers | 1150 |
| Baggage area 1 | 1500 |
| Baggage area 2 | 2296 |
| Fuel | 706 |

**Limits** (POH §2.6): MTOW 750 kg; baggage area 1 max 35 kg; area 2 max 25 kg; combined ≤ 35 kg; fuel density 0.72 kg/L; 2 × 70 L tanks.

## Verification reference values

With default inputs the app should show:

| Field | Value |
|---|---|
| Total mass takeoff | 761.5 kg |
| Total moment takeoff | 694,427 kg·mm |
| CG takeoff | 911.9 mm = 22.76% MAC |
| Takeoff in envelope | **false** (overweight + forward of 23% limit) |
| Total mass landing (−15 kg burn) | 746.5 kg |
| CG landing | 916.1 mm = 23.07% MAC |
| Landing in envelope | **true** |

## Fuel suggestion ("Calc" button)

`MainViewModel.SuggestedFuelMass` finds the largest fuel load (empty/pilot/baggage
held fixed) that keeps the takeoff point at/under MTOW **and** inside the CG
envelope. It scans down from `min(MaxFuelMass, MTOW − non-fuel mass)` in 0.05 kg
steps and returns the first mass for which `IsPointInEnvelope` is true (no
closed-form solve — the envelope's forward/aft limits are piecewise-linear in
mass, so a scan is simplest and cheap enough for a button click).
`SuggestedFuelLiters` divides by `FuelDensity`; `SuggestedFuelText` formats both
for the red hint label under the Fuel row. The `ApplySuggestedFuelCommand`
(`[RelayCommand]`) writes the suggestion straight into `FuelMass`. These three
outputs are recalculated in `NotifyAllOutputs()`, not `NotifyLandingOutputs()`,
since they depend on the non-fuel masses, not on `FuelMass` or `FuelBurnMass`
itself.

## Takeoff and Landing tabs (POH §5.3.6, §5.3.10)

Both tabs share `RunwayPerformance`, `RunwayPerformanceViewModel` and
`RunwayPerformancePanel`. Each tab has its own inputs. Mass comes from the W&B totals
and only drives the warning, because the POH pages give no mass correction.

Order of calculation: bilinear Hp/OAT table lookup → surface factor → wind (−6 m/kt
headwind, +24 m/kt tailwind) and slope.

| | Takeoff | Landing |
|---|---|---|
| Run / distance at 0 ft, 15 °C, concrete | 325 / 470 m | 225 / 475 m |
| Wet grass factor (dry grass is 1.2 for both) | 1.3 | 1.4 |
| Per degree of upslope | +30 m | −13 m |

- Inputs outside the table, or tailwind above 10 kt, give an error rather than a
  number. Nothing is extrapolated.
- `<470` and `<475` table cells use the baseline distance, and the tab says so.
- The POH tables cover only the distance over 15 m. The run is an estimate: table
  distance × baseline run / baseline distance. It must stay labelled as an estimate.
- If a W&B entry is invalid, both tabs warn that the mass may be wrong.

Reference values at 3000 ft, 20 °C, dry grass, 5 kt tailwind, 1° upslope:
takeoff run 623 m, distance 834 m; landing run 405 m, distance 737 m.

## CentogramControl rendering order

`DrawBackground` → `DrawGrid` → `DrawEnvelope` → `DrawMtowLine` →
`DrawAxesAndLabels` → `DrawMeterMarkers` → `DrawPoints` → `DrawTitle` → `DrawLegend`

The control extends `Avalonia.Controls.Control` directly (not `UserControl`) and
overrides `Render(DrawingContext)`. Four `StyledProperty<double>` trigger `InvalidateVisual`
via `AffectsRender<CentogramControl>(...)`.

## Property change cascade

`MainViewModel` uses `partial void OnXxxChanged(double value)` callbacks generated by
CommunityToolkit to call `NotifyAllOutputs()` or `NotifyLandingOutputs()`. All computed
properties are pure getters — no backing fields.

## Input fields

All input fields bind to string properties parsed by `NumberInput.Parse` (no commas),
so invalid text can never silently become 0 or be misread. On the W&B tab, `XxxMassText`
updates `XxxMass` only when valid (0 or more). Otherwise the mass keeps its last valid
value and `MassInputError` names the field and that value. Do not return a
`BindingNotification` from a converter's `ConvertBack`: Avalonia shows it as a raw cast
exception on the TextBox.
