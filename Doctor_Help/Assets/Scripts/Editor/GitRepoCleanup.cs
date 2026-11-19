using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DoctorHelp.EditorTools
{
    // One-off tool: squashes the unpushed commits and drops build output, logs, recordings,
    // zip archives and raw Design art from git so the branch fits GitHub's push limits.
    internal static class GitRepoCleanup
    {
        private const string RemoteBase = "bedc5efdd333cd8c22dbe75601d15e968a7a748b";
        private const string FirebaseBundle = "Doctor_Help/Assets/Firebase/Plugins/x86_64/FirebaseCppApp-13_11_0.bundle";

        [MenuItem("Tools/Doctor Help/Git/Clean Up Unpushed Commits")]
        private static void Run()
        {
            string repo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string logPath = Path.Combine(Application.dataPath, "..", "Temp", "git_cleanup.log");
            var log = new StringBuilder();
            string git = FindGit(log);
            if (git == null)
            {
                Write(logPath, log.AppendLine("ERROR: git.exe not found."));
                return;
            }

            try
            {
                string head = Git(git, repo, log, "rev-parse HEAD").Trim();
                string remote = Git(git, repo, log, "rev-parse origin/Working_1").Trim();
                if (remote != RemoteBase) throw new Exception("origin/Working_1 moved; stopping without changes.");
                if (head == RemoteBase) throw new Exception("Already squashed; use Finish Clean Up Commit.");

                // Keep the old tip reachable so this can be undone with: git reset --soft backup/before-cleanup
                Git(git, repo, log, $"branch -f backup/before-cleanup {head}");
                Git(git, repo, log, "log --oneline origin/Working_1..HEAD");
                Git(git, repo, log, "reset --soft origin/Working_1");
                Git(git, repo, log, "rm -r --cached --quiet --ignore-unmatch -- Doctor_Help/Builds Doctor_Help/Logs Doctor_Help/Recordings Doctor_Help/Design");
                Git(git, repo, log, "rm --cached --quiet --ignore-unmatch -- \"*.zip\"");
                Git(git, repo, log, "add -- Doctor_Help/Design");
                Git(git, repo, log, "lfs install --local");
                Git(git, repo, log, "lfs track \"Doctor_Help/Assets/Firebase/Plugins/x86_64/*.bundle\"");
                Git(git, repo, log, $"rm --cached --quiet --ignore-unmatch -- \"{FirebaseBundle}\"");
                Git(git, repo, log, $"add -- .gitattributes .gitignore \"{FirebaseBundle}\"");
                Git(git, repo, log, "commit -m \"Update game and clean repo: ignore builds, logs, recordings, zips and raw Design art\"");
                Git(git, repo, log, "count-objects -vH");
                Git(git, repo, log, "lfs ls-files");
                log.AppendLine("DONE");
                Debug.Log("Git cleanup finished. See Temp/git_cleanup.log");
            }
            catch (Exception exception)
            {
                log.AppendLine("ERROR: " + exception.Message);
                Debug.LogError("Git cleanup stopped: " + exception.Message);
            }
            Write(logPath, log);
        }

        [MenuItem("Tools/Doctor Help/Git/Finish Clean Up Commit")]
        private static void Finish()
        {
            string repo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string logPath = Path.Combine(Application.dataPath, "..", "Temp", "git_cleanup_finish.log");
            var log = new StringBuilder();
            string git = FindGit(log);
            try
            {
                // Only continue a cleanup that already squashed back onto the pushed tip.
                if (Git(git, repo, log, "rev-parse HEAD").Trim() != RemoteBase)
                    throw new Exception("HEAD is not at origin/Working_1; nothing to finish.");
                Git(git, repo, log, $"rm -f --cached --quiet --ignore-unmatch -- \"{FirebaseBundle}\"");
                Git(git, repo, log, $"add -- .gitattributes .gitignore \"{FirebaseBundle}\"");
                Git(git, repo, log, "commit -m \"Update game and clean repo: ignore builds, logs, recordings, zips and raw Design art\"");
                Git(git, repo, log, "lfs ls-files");
                Git(git, repo, log, "diff --stat --shortstat origin/Working_1 HEAD");
                log.AppendLine("DONE");
                Debug.Log("Git cleanup commit created. See Temp/git_cleanup_finish.log");
            }
            catch (Exception exception)
            {
                log.AppendLine("ERROR: " + exception.Message);
                Debug.LogError("Git cleanup finish stopped: " + exception.Message);
            }
            Write(logPath, log);
        }

        private static string Git(string git, string repo, StringBuilder log, string arguments)
        {
            log.AppendLine("> git " + arguments);
            var info = new ProcessStartInfo(git, arguments)
            {
                WorkingDirectory = repo,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            using var process = Process.Start(info);
            var error = process.StandardError.ReadToEndAsync();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            log.Append(output).Append(error.Result);
            if (process.ExitCode != 0) throw new Exception($"git {arguments} exited with {process.ExitCode}");
            return output;
        }

        private static string FindGit(StringBuilder log)
        {
            string fork = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fork", "gitInstance");
            var candidates = Directory.Exists(fork)
                ? Directory.GetDirectories(fork).OrderByDescending(d => d).Select(d => Path.Combine(d, "bin", "git.exe"))
                : Enumerable.Empty<string>();
            candidates = candidates.Concat(new[] { @"C:\Program Files\Git\cmd\git.exe", "git" });
            foreach (var candidate in candidates)
            {
                if (candidate != "git" && !File.Exists(candidate)) continue;
                try
                {
                    using var process = Process.Start(new ProcessStartInfo(candidate, "--version")
                        { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true });
                    log.AppendLine($"Using {candidate}: {process.StandardOutput.ReadToEnd().Trim()}");
                    process.WaitForExit();
                    if (process.ExitCode == 0) return candidate;
                }
                catch (Exception) { }
            }
            return null;
        }

        private static void Write(string path, StringBuilder log) => File.WriteAllText(path, log.ToString());
    }
}
