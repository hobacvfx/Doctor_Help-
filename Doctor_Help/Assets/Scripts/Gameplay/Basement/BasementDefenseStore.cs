using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace DoctorHelp
{
    // basement-defense.json beside the player profile, with a .bak recovery copy.
    // Autosaves serialize on the main thread and write on a worker thread (newest snapshot wins);
    // SaveNow writes synchronously for pause/quit and for transactions that must not be lost.
    internal sealed class BasementDefenseStore
    {
        internal const string FileName = "basement-defense.json";

        private readonly string path;
        private string BackupPath => path + ".bak";
        private string TemporaryPath => path + ".tmp";
        private readonly object ioLock = new object();
        private readonly object queueLock = new object();
        private string queuedJson;
        private bool writerRunning;
        private bool primaryIsValid, backupRotated;
        private volatile bool writeFailed;

        // False when the files on disk could not be read: progress then stays in memory and nothing overwrites them.
        internal bool CanWrite { get; private set; } = true;

        internal BasementDefenseStore(string directory) => path = Path.Combine(directory, FileName);

        internal BasementDefenseData Load()
        {
            Flush();
            lock (ioLock)
            {
                primaryIsValid = false;
                if (!File.Exists(path) && !File.Exists(BackupPath)) return new BasementDefenseData();
                try
                {
                    var data = Read(path);
                    primaryIsValid = true;
                    return data;
                }
                catch (NotSupportedException) { CanWrite = false; throw; }
                catch (Exception error) when (error is IOException || error is InvalidDataException || error is ArgumentException)
                {
                    Debug.LogWarning("Cannot load basement progress; trying backup. " + error.Message);
                }
                try { return Read(BackupPath); }
                catch
                {
                    CanWrite = false; // Keep both files for recovery rather than replacing them with a fresh basement.
                    throw;
                }
            }
        }

        private static BasementDefenseData Read(string file)
        {
            string json = File.ReadAllText(file);
            if (string.IsNullOrWhiteSpace(json) || json.TrimStart()[0] != '{')
                throw new InvalidDataException("Invalid basement progress: " + file);
            var data = new BasementDefenseData { schemaVersion = 0 };
            JsonUtility.FromJsonOverwrite(json, data);
            if (data.schemaVersion > BasementDefenseData.CurrentSchemaVersion)
                throw new NotSupportedException("Basement progress was saved by a newer game version.");
            if (data.schemaVersion < 1) throw new InvalidDataException("Invalid basement progress: " + file);
            data.Migrate(); // Schema 1 shelf plants wait for the profile to own them (BasementDefenseService).
            data.Sanitize();
            return data;
        }

        internal void Autosave(BasementDefenseData data)
        {
            if (!CanWrite) return;
            string json = JsonUtility.ToJson(data);
#if UNITY_WEBGL && !UNITY_EDITOR
            WriteSafely(json);
#else
            lock (queueLock)
            {
                queuedJson = json;
                if (writerRunning) return;
                writerRunning = true;
            }
            if (!ThreadPool.QueueUserWorkItem(_ => Drain()))
            {
                lock (queueLock) { writerRunning = false; queuedJson = null; }
                WriteSafely(json);
            }
#endif
        }

        internal void SaveNow(BasementDefenseData data)
        {
            if (!CanWrite) return;
            string json = JsonUtility.ToJson(data);
            lock (queueLock) queuedJson = null;
            Flush();
            WriteSafely(json);
        }

        internal void Flush()
        {
            lock (queueLock)
                while (writerRunning) Monitor.Wait(queueLock);
        }

        internal bool ConsumeWriteFailure()
        {
            if (!writeFailed) return false;
            writeFailed = false;
            return true;
        }

        // Clears the basement for a new profile. Both files go, so nothing older can be recovered into it.
        internal void Delete()
        {
            lock (queueLock) queuedJson = null;
            Flush();
            lock (ioLock)
            {
                File.Delete(path);
                File.Delete(BackupPath);
                File.Delete(TemporaryPath);
                primaryIsValid = false;
                backupRotated = true;
                CanWrite = true;
            }
        }

        private void Drain()
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
                WriteSafely(json);
            }
        }

        private void WriteSafely(string json)
        {
            try
            {
                lock (ioLock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(TemporaryPath, json);
                    // Once per session, keep the last good file as the backup before replacing it.
                    if (!backupRotated && primaryIsValid && File.Exists(path)) File.Copy(path, BackupPath, true);
                    backupRotated = true;
                    File.Copy(TemporaryPath, path, true);
                    primaryIsValid = true;
                    if (!File.Exists(BackupPath)) File.Copy(path, BackupPath);
                    File.Delete(TemporaryPath);
                }
            }
            catch (Exception error)
            {
                writeFailed = true;
                Debug.LogError("Basement progress save failed: " + error);
            }
        }
    }
}
