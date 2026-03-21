# EmsPlusCalloutExample

This guide explains how to create addon callouts for EmsPlus using the component-based medical system.

EmsPlus Modding Guide: Creating Custom Callouts
1. Project Setup

To create an addon, you must create a new Class Library (.NET Framework 4.8) project in Visual Studio.

# Required References:

Add the following .dll files as references to your project:

```
RagePluginHookSDK.dll (found in the manual install of LSPD:FR).
EmsPlus.dll (found in Plugins Folder of every EmsPlus release).
```

# Namespace and Inheritance:
Your callout class must be public and inherit from EmsCallout.

```
using EmsPlus.Framework;
using EmsPlus.Framework.Medical;
using Rage;

namespace MyEmsPack.Callouts
{
    public class MyCustomCallout : EmsCallout
    {
        // Class level variables
        private Ped patient;
        private Blip patientBlip;
        private Vector3 spawnPos;

        // ...
    }
}
```

# Defining Callout Details
The OnBeforeCalloutDisplayed method handles the metadata and spawn logic.

```
public override bool OnBeforeCalloutDisplayed()
{
    // 1. Dispatch Info
    // CalloutName MUST match your Audio file (REPORT_MY_CALLOUT_NAME_01.wav)
    CalloutName = "Assault Victim"; 
    CalloutMessage = "Subject injured during a physical altercation.";

    // 2. Station Filtering (Optional)
    // Only allow this to spawn if the player is at Davis or Rockford
    // AllowedStationIDs.Add("DAVIS");
    // AllowedStationIDs.Add("ROCKFORD");

    // 3. Find Spawn Point
    Vector3 center = StationManager.ActiveStation?.Position ?? Game.LocalPlayer.Character.Position;
    spawnPos = World.GetNextPositionOnStreet(center.Around(300f, 600f));
    
    if (spawnPos == Vector3.Zero) return false;

    CalloutPosition = spawnPos;
    ShowCalloutAreaBlipBeforeAccepting(spawnPos, 30f);

    return true;
}
```

# Setting Up the Patient (Components)
This is where you define the medical logic using Components (Lego blocks). You don't need to write code for the menus; EmsPlus builds them automatically based on the conditions you add.

```
public override bool OnCalloutAccepted()
{
    base.OnCalloutAccepted();

    // 1. Spawn Ped
    Vector3 sidewalk = GetSidewalkPosition(spawnPos);
    patient = new Ped(sidewalk);
    patient.IsPersistent = true;
    patient.BlockPermanentEvents = true;

    // 2. Initialize EmsPlus Patient
    GameState.CurrentPatient = new Patient(patient);
    var p = GameState.CurrentPatient;

    p.DispatchDiagnosis = "Stabbing Victim";
    p.Consciousness = ConsciousnessLevel.Pain;

    // 3. Add Medical Components (The "Lego Blocks")
    // PhysicalInjury: (Name, Bone, BleedSeverity, RequiredTreatments...)
    p.Conditions.Add(new PhysicalInjury("Stab Wound", PedBoneId.Spine3, 0.8f, EmsTreatment.ChestSeal));
    
    // Using the Factory for standard injuries
    p.Conditions.Add(InjuryFactory.ArterialBleed(PedBoneId.RightThigh));

    // SystemicCondition: Affects the whole body (Overdose, Shock, etc)
    p.Conditions.Add(new SystemicCondition("Hypovolemic Shock", EmsTreatment.IVAccess, EmsTreatment.IVFluids));

    // 4. Set Vitals
    p.HeartRate = VitalState.Elevated;
    p.BloodPressure = VitalState.Low;

    // 5. Visuals & Blips
    p.ApplyVisuals();
    patientBlip = new Blip(patient) { Color = System.Drawing.Color.Yellow };
    patientBlip.Sprite = (BlipSprite)280;

    return true;
}
```

# Custom Dispatch Audio
EmsPlus uses the LSPDFR Audio Standard.

Filename Convention:

If your CalloutName = "Assault Victim", the mod cleans the string and looks for:
"Plugins/EmsPlus/Audio/Dispatch/Callouts/REPORT_ASSAULT_VICTIM_01.wav"

Folder Structure:
```
Streets: STREET_[NAME]_01.wav
Zones: AREA_[NAME]_01.wav
Callouts: REPORT_[NAME]_01.wav
```

# Scene Management (Process and End)
Handle the transition from "responding" to "on-scene."

```
public override void Process()
{
    base.Process();

    // If player gets close, remove the GPS route but keep the patient blip
    if (patientBlip.Exists() && Game.LocalPlayer.Character.DistanceTo(patient) < 20f)
    {
        patientBlip.IsRouteEnabled = false;
    }
}

public override void End()
{
    base.End();
    // Cleanup your specific objects
    if (patientBlip.Exists()) patientBlip.Delete();
    
    // NOTE: GameState.CurrentPatient is cleaned up automatically by EmsPlus.
    // If the patient is on a stretcher in the ambulance, EmsPlus prevents
    // them from being deleted to allow for hospital transport.
    if (patient.Exists() && !GameState.CurrentPatient.IsOnStretcher) 
        patient.Dismiss();
}
```

# Tips for Callout Creators
Anatomical Awareness
The AnatomicalRegistry in EmsPlus enforces rules.
If you add a Tourniquet requirement to a wound on the Head, the paramedic cannot treat it. The marker will not turn green.
Always ensure your RequiredTreatments match the bone (e.g., Use WoundPacking for Head/Torso bleeds).

# Creating Medical Puzzles
You can create custom conditions with side effects using ReactToTreatment.

```
public class MyAllergy : MedicalCondition {
    public override void ReactToTreatment(Patient p, EmsTreatment t) {
        if (t == EmsTreatment.Analgesia) {
            Game.DisplayNotification("~r~Patient is having an allergic reaction to the meds!");
            p.HeartRate = VitalState.CriticalHigh;
        }
    }
}
```

# Installation for Users

Addon developers should distribute their mod as a single .dll. Users install it by placing it in:
```
Grand Theft Auto V/Plugins/EmsPlus/Plugins/"YourModName.dll"
```
EmsPlus will automatically detect the file, load the classes, and inject them into the random callout pool.
