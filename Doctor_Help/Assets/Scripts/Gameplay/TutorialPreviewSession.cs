#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;

namespace DoctorHelp
{
    public static class TutorialPreviewSession
    {
        private const string SaveDirectoryKey = "DoctorHelp.TutorialPreview.SaveDirectory";
        public static string SaveDirectory => SessionState.GetString(SaveDirectoryKey, "");
        public static bool IsActive => !string.IsNullOrEmpty(SaveDirectory);

        public static void Begin()
        {
            var directory = Path.GetFullPath(Path.Combine("Library", "TutorialPreview", Guid.NewGuid().ToString("N")));
            SessionState.SetString(SaveDirectoryKey, directory);
        }

        public static void End() => SessionState.EraseString(SaveDirectoryKey);
    }
}
#endif
