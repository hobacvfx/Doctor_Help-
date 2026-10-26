using System;
using System.IO;
using UnityEngine;

namespace DoctorHelp
{
    public sealed class PlayerProfileStore
    {
        public string SavePath { get; }
        private string BackupPath => SavePath + ".bak";
        private bool primaryIsValid;

        public PlayerProfileStore(string directory)
        {
            SavePath = Path.Combine(directory, "player-profile.json");
        }

        public PlayerProfileData Load()
        {
            primaryIsValid = false;
            if (!File.Exists(SavePath) && !File.Exists(BackupPath))
                return new PlayerProfileData();

            try
            {
                var profile = Read(SavePath);
                primaryIsValid = true;
                return profile;
            }
            catch (NotSupportedException) { throw; }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException)
            {
                Debug.LogWarning($"Cannot load primary profile; trying backup. {ex.Message}");
            }

            // If both files are invalid, fail without silently replacing player progress.
            return Read(BackupPath);
        }

        private static PlayerProfileData Read(string path)
        {
            var json = File.ReadAllText(path);
            var profile = JsonUtility.FromJson<PlayerProfileData>(json);
            if (profile != null && profile.schemaVersion > 1)
                throw new NotSupportedException("Profile was saved by a newer game version.");
            if (profile == null || !profile.IsValid())
                throw new InvalidDataException("Invalid player profile: " + path);
            return profile;
        }

        public void Save(PlayerProfileData profile)
        {
            if (profile == null || !profile.IsValid())
                throw new ArgumentException("Cannot save an invalid profile.", nameof(profile));
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            string temporaryPath = SavePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(profile, true));
            // Never replace a recovered backup with a corrupt primary file.
            if (primaryIsValid && File.Exists(SavePath))
                File.Copy(SavePath, BackupPath, true);
            File.Copy(temporaryPath, SavePath, true);
            primaryIsValid = true;
            if (!File.Exists(BackupPath)) File.Copy(SavePath, BackupPath);
            File.Delete(temporaryPath);
        }
    }
}
