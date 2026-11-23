using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    public enum TutorialAction
    {
        SelectTool = 4, UseTool = 5, OpenTreatment = 6, SelectMedicine = 7, GiveMedicine = 8,
        OpenDoctorBook = 9, CloseDoctorBook = 10, ReviewSymptoms = 11, ExplainStain = 12, SelectClean = 13,
        RemoveMedicine = 14
    }

    [Serializable]
    public sealed class TutorialStep
    {
        public TutorialAction Action;
        public ExaminationToolKind Tool;
        [TextArea(2, 4)] public string Instruction;
        [Min(0)] public float Padding = 6;
        [Tooltip("Only charge the hold progress while the X-ray lens covers the parasite.")]
        public bool RequireWormInLens;
        [Tooltip("Interaction area width and height as fractions of the screen during a hold.")]
        public Vector2 HoldArea = new Vector2(.26f, .20f);
        [Tooltip("Shift the hold interaction area to fit feedback graphics, in fractions of the screen.")]
        public Vector2 HoldAreaOffset;
        public Vector2 HandOffset;
    }

    [CreateAssetMenu(menuName = "Doctor Help/Tutorial Plan")]
    public sealed class TutorialPlan : ScriptableObject
    {
        [SerializeField, Tooltip("Stable ID from AllSickness. The tutorial patient must have only this condition.")]
        private string sicknessId;
        [SerializeField, Tooltip("Stable ID from PillProfile. This must be the sole medicine required by the tutorial condition.")]
        private string medicineId;
        [SerializeField, Min(5)] private float targetTimeout = 20;
        [SerializeField] private FirstDayGuide firstDayGuide;
        [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();
        public string SicknessId => sicknessId;
        public string MedicineId => medicineId;
        public float TargetTimeout => targetTimeout;
        internal FirstDayGuide FirstDayGuide => firstDayGuide;
        public IReadOnlyList<TutorialStep> Steps => steps;
    }
}
