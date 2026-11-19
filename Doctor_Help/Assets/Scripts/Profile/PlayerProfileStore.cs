using System;
using System.IO;
using UnityEngine;

namespace DoctorHelp
{
    public sealed class PlayerProfileStore
    {
        public string SavePath { get; }
        private string BackupPath => SavePath + ".bak";
        private string ResetPath => SavePath + ".reset";
        private bool primaryIsValid;
        private readonly Func<PlayerProfileData> createProfile;

        public PlayerProfileStore(string directory, Func<PlayerProfileData> createProfile = null)
        {
            SavePath = Path.Combine(directory, "player-profile.json");
            this.createProfile = createProfile ?? (() => new PlayerProfileData());
        }

        public PlayerProfileData Load()
        {
            primaryIsValid = false;
            if (Directory.Exists(ResetPath))
            {
                TryFinishReset();
                return createProfile();
            }
            if (!File.Exists(SavePath) && !File.Exists(BackupPath))
                return createProfile();

            try
            {
                var profile = Read(SavePath);
                primaryIsValid = true;
                return profile;
            }
            catch (NotSupportedException) { throw; }
            catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is ArgumentException)
            {
                Debug.LogWarning($"Cannot load primary profile; trying backup. {ex.Message}");
            }

            // If both files are invalid, fail without silently replacing player progress.
            return Read(BackupPath);
        }

        private static PlayerProfileData Read(string path)
        {
            var json = File.ReadAllText(path);
            // Fields missing from older checkpoints keep their defaults, including zero ghosts.
            var profile = new PlayerProfileData { schemaVersion = 0, currentDay = 0, medicines = null };
            if (string.IsNullOrWhiteSpace(json) || json.TrimStart()[0] != '{')
                throw new InvalidDataException("Invalid player profile: " + path);
            JsonUtility.FromJsonOverwrite(json, profile);
            if (profile.schemaVersion > PlayerProfileData.CurrentSchemaVersion)
                throw new NotSupportedException("Profile was saved by a newer game version.");
            if (profile.schemaVersion == 1)
                profile.schemaVersion = PlayerProfileData.CurrentSchemaVersion;
            if (!profile.IsValid())
                throw new InvalidDataException("Invalid player profile: " + path);
            return profile;
        }

        public void Save(PlayerProfileData profile)
        {
            if (profile == null || !profile.IsValid())
                throw new ArgumentException("Cannot save an invalid profile.", nameof(profile));
            FinishReset();
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

        public PlayerProfileData Reset()
        {
            var fresh = createProfile();
            if (fresh == null || !fresh.IsValid())
                throw new InvalidDataException("The starting profile is invalid.");

            // Commit the reset before deleting either save. A restart must never recover old progress.
            Directory.CreateDirectory(ResetPath);
            primaryIsValid = false;
            TryFinishReset();
            return fresh;
        }

        private void TryFinishReset()
        {
            try { FinishReset(); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Player profile reset cleanup will be retried. {error.Message}");
            }
        }

        private void FinishReset()
        {
            if (!Directory.Exists(ResetPath)) return;
            File.Delete(SavePath);
            File.Delete(BackupPath);
            File.Delete(SavePath + ".tmp");
            Directory.Delete(ResetPath);
        }
    }
}
