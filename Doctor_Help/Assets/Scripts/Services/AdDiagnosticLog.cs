using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DoctorHelp
{
    // Session-only diagnostic buffer. No player data, file writes or network uploads.
    public static class AdDiagnosticLog
    {
        private const int MaxCharacters = 120000;
        private static readonly object Gate = new object();
        private static readonly Queue<string> Lines = new Queue<string>();
        private static int characters, dropped, revision;
        private static string header;
        public static int Revision { get { lock (Gate) return revision; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            Application.logMessageReceivedThreaded -= Capture;
            lock (Gate)
            {
                Lines.Clear(); characters = dropped = revision = 0;
                header = $"Doctor Help / TEST ADS diagnostics v1\nSession UTC: {DateTime.UtcNow:O}\n" +
                    $"App: {Application.identifier} {Application.version}\nBuild: {Application.buildGUID}\n" +
                    $"Unity: {Application.unityVersion}; platform: {Application.platform}; development: {Debug.isDebugBuild}\n" +
                    $"OS: {SystemInfo.operatingSystem}; model: {SystemInfo.deviceModel}\n" +
                    $"Sample app: {DayEndInterstitialService.AndroidSampleAppId}\nSample unit: {DayEndInterstitialService.AndroidSampleInterstitialId}\n";
            }
            Application.logMessageReceivedThreaded += Capture;
            Debug.Log("[Test Ads] Diagnostic capture started before scene/bootstrap initialization.");
        }

        private static void Capture(string message, string stack, LogType type)
        {
            bool failure = type == LogType.Exception || type == LogType.Error || type == LogType.Assert;
            if (!failure && !message.Contains("[Test Ads]") &&
                message.IndexOf("GoogleMobileAds", StringComparison.OrdinalIgnoreCase) < 0) return;
            string line = $"{DateTime.UtcNow:HH:mm:ss.fff}Z [{type}] {message}\n";
            if (failure && !string.IsNullOrEmpty(stack)) line += stack + "\n";
            if (line.Length > 12000) line = line.Substring(0, 12000) + "\n[entry truncated]\n";
            lock (Gate)
            {
                while (characters + line.Length > MaxCharacters && Lines.Count > 0)
                { characters -= Lines.Dequeue().Length; dropped++; }
                Lines.Enqueue(line); characters += line.Length; revision++;
            }
        }

        public static string Snapshot()
        {
            lock (Gate)
            {
                var text = new StringBuilder(header);
                text.AppendLine($"Export UTC: {DateTime.UtcNow:O}; older entries dropped: {dropped}");
                foreach (string line in Lines) text.Append(line);
                return text.ToString();
            }
        }
    }
}
