using EmsPlus;
using EmsPlus.Callouts;
using EmsPlus.Core;
using EmsPlus.Managers;
using EmsPlus.Medical;
using Rage;
using System.Drawing;

namespace MyEmsPack.Callouts
{
    public class MyCustomCallout : EmsCallout
    {
        private Ped patient;
        private Blip blip;
        private Vector3 spawnPos;
        private bool hasArrivedAtScene = false;

        public override bool OnBeforeCalloutDisplayed()
        {
            CalloutName = "Assault Victim";
            CalloutMessage = "Subject injured during a physical altercation.";

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

            Vector3 sidewalk = GetSidewalkPosition(spawnPos);
            patient = new Ped(sidewalk);
            patient.IsPersistent = true;
            patient.BlockPermanentEvents = true;

            GameState.CurrentPatient = new Patient(patient);
            var p = GameState.CurrentPatient;

            p.DispatchDiagnosis = "Stabbing Victim";
            p.Consciousness = ConsciousnessLevel.Pain;

            p.Conditions.Add(new PhysicalInjury("Stab Wound", PedBoneId.Spine3, 0.8f, EmsTreatment.ChestSeal));

            p.Conditions.Add(InjuryFactory.Haemorrhage.Arterial(PedBoneId.RightThigh));

            p.Conditions.Add(new SystemicCondition("Hypovolemic Shock", EmsTreatment.IVAccess, EmsTreatment.SalineBag));

            p.HeartRate = VitalState.Elevated;
            p.BloodPressure = VitalState.Low;

            p.ApplyVisuals();

            blip = new Blip(patient);
            blip.Color = Color.Red;
            blip.Name = "Medical Emergency";
            blip.IsRouteEnabled = true;

            return true;
        }

        public override void Process()
        {
            base.Process();

            if (!hasArrivedAtScene && Game.LocalPlayer.Character.DistanceTo(patient) < 25f)
            {
                hasArrivedAtScene = true;

                if (blip.Exists()) blip.Delete();

                blip = patient.AttachBlip();
                blip.Sprite = (BlipSprite)280;
                blip.Color = Color.Yellow;
                blip.Name = "Patient";
                blip.IsRouteEnabled = false;
            }

            if (hasArrivedAtScene && blip.Exists()
                && GameState.CurrentPatient != null
                && GameState.CurrentPatient.IsOnStretcher)
            {
                blip.Delete();
            }
        }

        public override void End()
        {
            base.End();
            if (blip.Exists()) blip.Delete();

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