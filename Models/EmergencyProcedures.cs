using System.Collections.Generic;

namespace WeightBalance.Models;

public enum ProcedureLineKind { Step, Condition, Warning, Note }

// One displayed line. Number is set for steps only ("1", "2", ...).
public sealed record ProcedureLine(string Number, string Text, ProcedureLineKind Kind)
{
    public bool IsStep      => Kind == ProcedureLineKind.Step;
    public bool IsCondition => Kind == ProcedureLineKind.Condition;
    public bool IsWarning   => Kind == ProcedureLineKind.Warning;
    public bool IsNote      => Kind == ProcedureLineKind.Note;
}

public sealed record Procedure(string Title, string Reference, IReadOnlyList<ProcedureLine> Lines);

public sealed record ProcedureGroup(string Title, string Summary, IReadOnlyList<Procedure> Procedures);

// Condensed from the S200 Aircraft Flight Manual SONAIR-S2-D-MAN-001 Rev C, Section 3
// (Emergency Procedures). A reminder aid only: the AFM is the authority.
public static class EmergencyProcedures
{
    public const string Source = "Condensed from S200 AFM SONAIR-S2-D-MAN-001 Rev C, Section 3. " +
                                 "A memory aid only: the AFM is the authority. Check against it before use.";

    public const string Priorities = "1 Fly the aircraft   2 Analyse   3 Apply the procedure   4 Tell ATC if there is time.   " +
                                     "Best glide: 65 KIAS, flaps up, about 1.9 NM per 1000 ft.";

    // Line builders. Steps are numbered by P().
    private static (ProcedureLineKind, string) S(string t) => (ProcedureLineKind.Step, t);
    private static (ProcedureLineKind, string) C(string t) => (ProcedureLineKind.Condition, t);
    private static (ProcedureLineKind, string) W(string t) => (ProcedureLineKind.Warning, t);
    private static (ProcedureLineKind, string) N(string t) => (ProcedureLineKind.Note, t);

    private static Procedure P(string title, string reference, params (ProcedureLineKind Kind, string Text)[] lines)
    {
        var result = new List<ProcedureLine>();
        int n = 0;
        foreach (var (kind, text) in lines)
            result.Add(new ProcedureLine(kind == ProcedureLineKind.Step ? (++n).ToString() : "", text, kind));
        return new Procedure(title, reference, result);
    }

    public static readonly IReadOnlyList<ProcedureGroup> Groups =
    [
        new("Engine failures", "Fire or failure, fuel and oil, restart, engine controls, turbo",
        [
            P("Fire or failure on the take-off run", "3.3.1",
                C("Reject the take-off."),
                S("Throttle: idle"),
                S("Brakes: as needed"),
                C("When stopped:"),
                S("Electric fuel pumps (both): off"),
                S("Fuel selector: off"),
                S("Magnetos / ignition: off"),
                S("Radio call as required"),
                S("Master switches: off"),
                S("Parking brake: on"),
                C("If required: evacuate."),
                W("Do not open the cowlings. If you use the extinguisher, discharge it into the air intakes after the propeller has stopped. It is in the baggage compartment behind the right seat.")),

            P("Fire or failure on the take-off climb", "3.3.2",
                S("Throttle: check full power"),
                S("Main and aux fuel pumps: check on"),
                S("Fuel selector: change to the other tank"),
                S("Ignition: check both channels on"),
                C("Not enough power:"),
                S("Emergency landing, engine off (3.3.4)"),
                C("Running rough but holds 65 KIAS in level flight:"),
                S("Cabin heat: close"),
                S("Land as soon as practical"),
                S("ELT: on (if fitted)"),
                W("Minimum altitude to turn back is 500 ft AGL. Maximum bank is 40° at 65 KIAS with flaps T/O; below that speed expect a stall or spin.")),

            P("Engine failure in flight", "3.3.3",
                S("Ignition: check both channels on"),
                S("Throttle: check position"),
                S("Fuel selector: other tank (unless it is empty)"),
                S("Aux fuel pump: on"),
                S("Main fuel pump: check circuit breaker pushed in"),
                C("No restart:"),
                S("Glide at 65 KIAS, flaps up"),
                S("Emergency landing, engine off (3.3.4)"),
                N("A clogged main pump filter can make the engine run rough and quit at full power. Reduce power to 65%.")),

            P("Emergency landing, engine off", "3.3.4",
                S("Find a landing area free of obstacles, preferably into wind"),
                S("If time and height allow, find the cause and try a restart (3.3.8)"),
                S("Tighten harnesses; declare an emergency and report your position"),
                C("If the restart fails:"),
                S("Ignition: off"),
                S("Fuel selector: shut off"),
                S("Aux fuel pump: off"),
                S("Main fuel pump: pull circuit breaker"),
                S("Join the landing area at or above 1500 ft AGL"),
                S("Speed: 65 KIAS"),
                S("Flaps: T/O"),
                S("ELT: on (if fitted)"),
                S("Align for final approach"),
                S("Flaps: as required"),
                S("Master switches: off before landing"),
                S("Canopy: unlock before landing"),
                S("Land at the normal speed for the flap setting"),
                S("Evacuate the aircraft"),
                N("Flaps do not work without power on the main bus. Make the final flap selection before the master is turned off. After switching the master back on, the flap system needs 11 s to reboot.")),

            P("Low fuel pressure", "3.3.5",
                C("EMS low fuel pressure, RAL flashing, or RPM fluctuating:"),
                S("Throttle: reduce to 65% max if possible"),
                S("Aux fuel pump: on"),
                S("Fuel selector: other tank (unless it is empty)"),
                C("Pressure stays low:"),
                S("Expect engine failure (3.3.3)")),

            P("Low oil pressure", "3.3.6",
                S("Oil temperature: check. If high or rising, go to High oil temperature (3.3.7)"),
                S("Throttle: minimum for level flight"),
                S("Land as soon as practical; expect the engine to fail")),

            P("High oil temperature", "3.3.7",
                C("Above 110 °C:"),
                S("Throttle: below max continuous"),
                S("Airspeed: increase"),
                S("Oil pressure: check"),
                C("Above 130 °C:"),
                S("Power: idle"),
                S("Prepare for an emergency landing, engine off (3.3.4)")),

            P("Engine restart", "3.3.8",
                W("After an engine fire, never try to restart the engine."),
                N("Fly the aircraft and pick a landing area before trying a restart. The propeller can windmill down to about 60 KIAS; do not use the starter then. If it has stopped, use the starter or dive to 80 KIAS to windmill it. That costs at least 300 ft."),
                S("Aux fuel pump: on"),
                S("Fuel selector: unused / fullest tank"),
                S("Throttle: middle position"),
                S("Master switches: check on"),
                S("Ignition: check both on"),
                S("Starter: engage if necessary (no windmilling)"),
                S("Aux fuel pump: off after a positive start"),
                C("No restart, or not enough height:"),
                S("Emergency landing, engine off (3.3.4)")),

            P("Defective engine controls", "3.3.9",
                C("RPM rising and high vibration (throttle cable detached):"),
                S("Throttle: full power"),
                C("If the vibration stops: keep full power, select a landing area, and when above it continue below."),
                C("If the vibration continues or increases:"),
                S("Throttle: idle"),
                S("Fuel selector: off"),
                S("Main fuel pump: pull circuit breaker"),
                S("Aux fuel pump: off"),
                S("Emergency landing, engine off (3.3.4)"),
                C("Throttle blocked, or engine not responding:"),
                S("Throttle: keep the current setting"),
                S("Land as soon as practicable; if needed, land as in 3.3.4")),

            P("Sudden drop in RPM and MAP", "3.3.10",
                N("Possible turbocharger failure, with or without the TCU caution light flashing."),
                S("Throttle: reduce to the minimum acceptable"),
                S("Oil pressure: monitor"),
                S("Land as soon as practicable")),

            P("Sudden rise in RPM and MAP", "3.3.11",
                N("Possible turbo overboost, with the TCU caution light flashing."),
                S("Throttle: reduce immediately to within RPM/MAP limits"),
                S("Land as soon as practicable"),
                C("Throttle ineffective: Defective engine controls (3.3.9).")),

            P("RPM and MAP rising and dropping", "3.3.12",
                N("Possible automatic regulation at the airbox temperature limit (88 °C)."),
                S("Carburettor heater: check pushed in"),
                S("Throttle: 65% or below if practicable"),
                C("Still varying after 30 seconds:"),
                S("TCU switch: off"),
                S("Continue; set power for OAT and monitor engine parameters")),

            P("Warning lights: RAL and TCU", "3.3.13, 3.3.14",
                N("Remote alert LED flashes for: a primary parameter in the red, low fuel (below 20 L total), or a disconnected sensor."),
                N("TCU yellow light: sensor, wiring, TCU or airbox leak, or wastegate not fully closed. Reduce speed and MAP to max continuous or below. Boost control may be limited."),
                N("TCU red light: the 5-minute full-power limit is exceeded (clears when MAP is below 36.9 in.Hg) or the max MAP is exceeded (clears below 45.7 in.Hg). Reduce speed and MAP to within limits."),
                N("Log any exceedance with its duration, time and extent.")),
        ]),

        new("Smoke and Fire", "Engine fire, electrical fire",
        [
            P("Engine fire", "3.4.1",
                S("Throttle: full power"),
                S("Fuel selector: close"),
                S("Aux fuel pump: off"),
                S("Cabin heat: close"),
                S("Main fuel pump: pull circuit breaker"),
                S("Fresh air vents: open (unless smoke comes in through them)"),
                S("Ignition: off after the fuel in the carburettors is used and the engine has stopped (about 15 s)"),
                S("Emergency landing, engine off (3.3.4)"),
                W("After an engine fire, never try to restart the engine.")),

            P("Electrical fire", "3.4.2",
                N("Usually white smoke and an acrid smell."),
                S("Master switches: BATT and ALT 2 off"),
                S("Cabin heat: close"),
                S("Fresh air vents: open"),
                S("In level flight you may open the canopy slightly or fully (100 KIAS max)"),
                S("Circuit breaker panel: look for pulled breakers"),
                W("Never push a pulled breaker back in after an electrical fire."),
                N("If conditions allow, land as soon as possible instead of looking for the source."),
                C("If the source is found and can be isolated, do so, then:"),
                S("All switches: off"),
                S("ALT 2 switch: on"),
                S("Restore switches one by one, at least 10 s apart; switch off any circuit that smokes again"),
                S("BATT switch: on (last, to protect it for the approach and landing)"),
                S("Land as soon as practicable, canopy closed"),
                W("The engine monitor, radio and transponder do not work with the master off. The engine keeps running with the master off as long as the ignition is on and the Main fuel pump has power from ALT 1 or the battery.")),
        ]),

        new("Landing and handling", "Tyre, brakes, spin, precautionary landing, instruments",
        [
            P("Landing with a defective tyre", "3.5",
                C("Aircraft veers off the centreline: expect a main tyre to be flat."),
                S("Rudder and/or brake on the opposite side"),
                S("Runway excursion: follow Fire or failure on the take-off run (3.3.1)"),
                S("If possible, vacate the runway at low speed and stop"),
                C("High vibration at de-rotation: expect the nose tyre to be flat."),
                S("Take weight off the nose gear with back stick"),
                S("If possible, vacate the runway at low speed and stop")),

            P("Landing with defective brakes", "3.6",
                N("Inconsistent brakes on approach: expect a longer ground roll."),
                C("Aircraft veers when braking:"),
                S("Release the brakes and use rudder to stay on the runway"),
                S("Brake smoothly on the remaining side as much as possible"),
                S("Runway excursion: follow 3.3.1")),

            P("Recovery from unintentional spin", "3.7",
                W("Intentional spins are prohibited."),
                S("Throttle: idle"),
                S("Rudder: full, opposite to the spin"),
                S("Stick: forward"),
                S("Ailerons: neutral"),
                C("When the rotation stops:"),
                S("Neutralise the rudder immediately"),
                S("Flaps: use as required to level the wings"),
                S("Pull out gently, avoiding the stall warning")),

            P("Precautionary landing", "3.8",
                N("For when you cannot continue, have no fuel reserve, or in bad weather. Keep the chosen area in sight."),
                S("Choose a landing area and find the wind direction"),
                S("Report your intention and the location by radio"),
                S("Fly a low pass into wind over the right-hand side, flaps as required, and inspect the area"),
                S("Fly a circuit"),
                S("Normal approach and landing (4.4.12 and 4.4.14)"),
                S("After stopping: all switches off, fuel selector off, lock the aircraft, get help")),

            P("Instrument outside the green range", "3.9",
                S("Over max RPM: reduce RPM and log the duration and extent"),
                S("One coolant or EGT value out of range: probably a loose sensor; have it serviced"),
                S("Several sensors in the caution range: minimum power to keep flying, then a precautionary landing"),
                S("Oil temperature below 50 °C: raise power slowly and reduce airspeed")),
        ]),

        new("Other emergencies", "Carb icing, electrical, alternator, CO, flaps",
        [
            P("Carburettor icing", "3.10.1",
                N("Slow power loss with rising EGT, in visible moisture below +15 °C OAT, mostly at low power."),
                S("Carburettor heater: on (pull slowly)"),
                S("Throttle: increase"),
                C("No recovery: land at the nearest airfield, or make a precautionary landing (3.8).")),

            P("Electrical equipment failure", "3.10.2",
                W("Do not try to reset a tripped breaker."),
                S("Check the equipment is on and the dimmer is suitable"),
                S("Check its circuit breaker is pushed in"),
                S("Check voltage and amperage"),
                S("See section 7.13, minimum instruments and equipment, to decide on continuing")),

            P("Alternator failure (ALT 1 / ALT 2 lights)", "3.10.3",
                C("ALT 1 light:"),
                S("Heated pitot: off (if the weather allows)"),
                S("Ammeter on EMS: check for positive battery charge"),
                S("Reduce load if needed to keep a positive charge"),
                N("Flight can continue, but the Main fuel pump no longer has a backup power source. Leave the Aux fuel pump on for the whole flight."),
                C("ALT 2 light:"),
                S("ALT 2 switch: off for 10 s, then on"),
                S("Light stays on: switch ALT 2 and other equipment off; keep the load at or below 14 A and the ammeter positive"),
                C("ALT 1 and ALT 2 lights:"),
                S("Avionics 2: off (max 14 A load)"),
                S("Keep radio use to the minimum"),
                S("Consider Avionics 1 off"),
                S("Land as soon as practicable"),
                N("A full battery lasts about 30 minutes; then the engine stops. Consider max continuous power to shorten the trip.")),

            P("Suspected carbon monoxide or fumes in the cabin", "3.10.4",
                N("CO has no smell. Only a CO detector shows it. Use the same procedure for any other abnormal smell or fumes."),
                S("Cabin heat: close"),
                S("Fresh air vents: open"),
                S("In level flight you may open the canopy slightly or fully (100 KIAS max)"),
                S("Land as soon as practical, canopy closed")),

            P("Flap system failure", "3.10.5",
                C("Flap indicator does not follow the flap lever:"),
                S("Confirm the flap position visually"),
                S("Set the lever back to the actual position"),
                S("Check the flaps circuit breaker; do not reset it if pulled"),
                S("Keep to the speed limit for the flap setting"),
                N("Flaps UP / T/O / APP / LD:  VFE n/a / 105 / 95 / 85 KIAS.  Approach speed 65 / 63 / 57 / 55 KIAS.  Landing distance over 15 m: 585 / 560 / 495 / 475 m.  Landing run: 305 / 285 / 245 / 225 m."),
                N("With flaps up, speed builds in the descent and the final is longer. Approach at idle power with flaps UP or T/O. Flaps out in the cruise raise drag and fuel burn.")),
        ]),
    ];
}
