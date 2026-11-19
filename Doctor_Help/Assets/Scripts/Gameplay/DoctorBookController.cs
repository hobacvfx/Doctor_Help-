using System;
using UnityEngine;

namespace DoctorHelp
{
    // Uses the existing mouse/touch selection system, so a book click cannot also examine the patient.
    public sealed class DoctorBookController : MonoBehaviour
    {
        [SerializeField] private ExaminationToolSelection examinationTools;
        [SerializeField] private DiseaseReferencePopup popup;
        internal DiseaseReferencePopup Popup => popup;
        private void OnEnable()
        {
            if (examinationTools == null || popup == null)
                throw new InvalidOperationException("DoctorBookController requires the scene tool selection and casebook popup.");
            examinationTools.ToolSelected += ToolSelected;
        }
        private void ToolSelected(ExaminationToolKind kind)
        {
            if (kind != ExaminationToolKind.Book) return;
            examinationTools.ClearSelection();
            popup.Open();
        }
        private void LateUpdate()
        {
            if (popup != null && popup.IsOpen && !examinationTools.IsAvailable) popup.Close();
        }
        private void OnDisable()
        {
            if (examinationTools != null) examinationTools.ToolSelected -= ToolSelected;
            if (popup != null) popup.Close();
        }
    }
}
