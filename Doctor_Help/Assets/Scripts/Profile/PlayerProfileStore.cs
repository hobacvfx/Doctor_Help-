using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace DoctorHelp
{
    // Checkpoints (Save) write on the calling thread and rotate the backup.
    // Autosaves serialize on the main thread, then write on a worker thread without touching the backup,
    // so frequent profile changes never stall a frame. Flush waits for the worker before pause/quit.
    public sealed class PlayerProfileStore
    {
        public string SavePath { get; }
        private string BackupPath => SavePath + ".bak";
        private string ResetPath => SavePath + ".reset";
        private string TemporaryPath => SavePath + ".tmp";
        private bool primaryIsValid; // Guarded by ioLock once writes begin.
        private readonly Func<PlayerProfileData> createProfile;

        private readonly object ioLock = new object();
        private readonly object queueLock = new object();
        private string queuedJson;
        private bool writerRunning;
        private volatile bool writeFailed;

        public PlayerProfileStore(string directory, Func<PlayerProfileData> createProfile = null)
        {
            SavePath = Path.Combine(directory, "player-profile.json");
            this.createProfile = createProfile ?? (() => new PlayerProfileData());
        }

        public PlayerProfileData Load()
        {
            Flush();
            lock (ioLock)
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

        private static string Serialize(PlayerProfileData profile)
        {
            if (profile == null || !profile.IsValid())
                throw new ArgumentException("Cannot save an invalid profile.", nameof(profile));
            return JsonUtility.ToJson(profile); // Compact JSON: smaller file, faster write.
        }

        // End-of-day checkpoint: synchronous so a failure keeps the day available for retry.
        public void Save(PlayerProfileData profile)
        {
            string json = Serialize(profile);
            DropQueuedAutosave();
            Flush();
            lock (ioLock) Write(json, rotateBackup: true);
        }

        // Frequent saves: the snapshot is taken now, the disk write happens on a worker thread.
        // Only the newest queued snapshot is written when several arrive while the worker is busy.
        public void Autosave(PlayerProfileData profile)
        {
            string json = Serialize(profile);
#if UNITY_WEBGL && !UNITY_EDITOR
            WriteAutosave(json);
#else
            lock (queueLock)
            {
                queuedJson = json;
                if (writerRunning) return;
                writerRunning = true;
            }
            if (!ThreadPool.QueueUserWorkItem(_ => DrainQueue()))
            {
                lock (queueLock) { writerRunning = false; queuedJson = null; }
                WriteAutosave(json);
            }
#endif
        }

        // Used when the app pauses or quits: the OS may suspend a worker thread mid-write.
        public void AutosaveNow(PlayerProfileData profile)
        {
            string json = Serialize(profile);
            DropQueuedAutosave();
            Flush();
            WriteAutosave(json);
        }

        // Blocks until queued autosaves are on disk.
        public void Flush()
        {
            lock (queueLock)
                while (writerRunning) Monitor.Wait(queueLock);
        }

        // True once after a background write failed, so the caller can schedule a retry.
        public bool ConsumeWriteFailure()
        {
            if (!writeFailed) return false;
            writeFailed = false;
            return true;
        }

        private void DropQueuedAutosave()
        {
            lock (queueLock) queuedJson = null;
        }

        private void DrainQueue()
        {
            while (true)
            {
                string json;
                lock (queueLock)
                {
                    json = queuedJson;
                    queuedJson = null;
                    if (json == null)
                    {
                        writerRunning = false;
                        Monitor.PulseAll(queueLock);
                        return;
                    }
                }
                WriteAutosave(json);
            }
        }

        private void WriteAutosave(string json)
        {
            try { lock (ioLock) Write(json, rotateBackup: false); }
            catch (Exception ex)
            {
                writeFailed = true;
                Debug.LogError($"Player profile autosave failed: {ex}");
            }
        }

        private void Write(string json, bool rotateBackup)
        {
            FinishReset();
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            File.WriteAllText(TemporaryPath, json);
            // Never replace a recovered backup with a corrupt primary file.
            if (rotateBackup && primaryIsValid && File.Exists(SavePath))
                File.Copy(SavePath, BackupPath, true);
            File.Copy(TemporaryPath, SavePath, true);
            primaryIsValid = true;
            if (!File.Exists(BackupPath)) File.Copy(SavePath, BackupPath);
            File.Delete(TemporaryPath);
        }

        public PlayerProfileData Reset()
        {
            var fresh = createProfile();
            if (fresh == null || !fresh.IsValid())
                throw new InvalidDataException("The starting profile is invalid.");

            DropQueuedAutosave();
            Flush();
            lock (ioLock)
            {
                // Commit the reset before deleting either save. A restart must never recover old progress.
                Directory.CreateDirectory(ResetPath);
                primaryIsValid = false;
                TryFinishReset();
            }
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
            File.Delete(TemporaryPath);
            Directory.Delete(ResetPath);
        }
    }
}
