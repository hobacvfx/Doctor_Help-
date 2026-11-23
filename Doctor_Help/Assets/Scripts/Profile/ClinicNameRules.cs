using System;
using System.Text;

namespace DoctorHelp
{
    internal static class ClinicNameRules
    {
        internal const int MaxLength = 20;

        internal static bool TryValidate(string input, out string name, out string error)
        {
            name = (input ?? "").Trim();
            error = null;
            try { name = name.Normalize(NormalizationForm.FormC); }
            catch (ArgumentException)
            {
                error = "Your clinic name contains an unsupported character.";
                return false;
            }
            if (name.Length == 0) error = "Please enter your clinic name.";
            else if (name.Length > MaxLength) error = "Use no more than 20 characters.";
            else
            {
                bool hasLetterOrDigit = false;
                foreach (char character in name)
                {
                    if (char.IsLetterOrDigit(character)) { hasLetterOrDigit = true; continue; }
                    if (character == ' ' || character == '\'' || character == '-' || character == '&' || character == '.') continue;
                    error = "Use letters, numbers, spaces, apostrophes, hyphens, & or periods.";
                    break;
                }
                if (error == null && !hasLetterOrDigit) error = "Include at least one letter or number.";
            }
            return error == null;
        }
    }
}
