using EmsPlus;
using EmsPlus.Core;
using EmsPlus.Framework;
using EmsPlus.Managers;
using EmsPlus.Medical.Conditions;
using EmsPlus.Medical.Frameworks;
using Rage;

namespace MyEmsPack.Callouts
{
    public class MyCustomCallout : EmsCallout
    {
        private Ped patient;
        private Blip patientBlip;
        private Vector3 spawnPos;

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
            p.Conditions.Add(InjuryFactory.Haemorrhage.Arterial(PedBoneId.RightThigh));

            // SystemicCondition: Affects the whole body (Overdose, Shock, etc)
            p.Conditions.Add(new SystemicCondition("Hypovolemic Shock", EmsTreatment.IVAccess, EmsTreatment.SalineBag));

            // 4. Set Vitals
            p.HeartRate = VitalState.Elevated;
            p.BloodPressure = VitalState.Low;

            // 5. Visuals & Blips
            p.ApplyVisuals();
            patientBlip = new Blip(patient) { Color = System.Drawing.Color.Yellow };
            patientBlip.Sprite = (BlipSprite)280;

            return true;
        }

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

        public class MyAllergy : MedicalCondition
        {
            public override void ReactToTreatment(Patient p, EmsTreatment t)
            {
                if (t == EmsTreatment.Analgesia)
                {
                    Game.DisplayNotification("~r~Patient is having an allergic reaction to the meds!");
                    p.HeartRate = VitalState.CriticalHigh;
                }
            }
        }
    }
}