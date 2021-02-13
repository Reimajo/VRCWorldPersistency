using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Security.Permissions;

namespace VRCWorldPersistency
{
    /// <summary>
    /// Taken from MerlinVR's https://github.com/MerlinVR/UdonSharp and modified for standalone usage
    /// </summary>
    internal class RuntimeLogfileWatcher
    {
        class LogFileState
        {
            public string playerName;
            public long lineOffset = -1;
            public string nameColor = "0000ff";
        }

        // Log watcher vars
        static FileSystemWatcher logDirectoryWatcher;
        static object logModifiedLock = new object();
        static Dictionary<string, LogFileState> logFileStates = new Dictionary<string, LogFileState>();
        static HashSet<string> modifiedLogPaths = new HashSet<string>();
        
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private static bool InitializeScriptLookup()
        {
            if (logDirectoryWatcher == null)
            {
                string VRCDataPath = $"C:\\Users\\{Environment.UserName}\\AppData\\LocalLow\\VRChat\\vrchat";
                if (Directory.Exists(VRCDataPath))
                {
                    logDirectoryWatcher = new FileSystemWatcher(VRCDataPath, "output_log_*.txt");
                    logDirectoryWatcher.IncludeSubdirectories = false;
                    logDirectoryWatcher.NotifyFilter = NotifyFilters.LastWrite;
                    logDirectoryWatcher.Changed += OnLogFileChanged;
                    logDirectoryWatcher.InternalBufferSize = 1024;
                    logDirectoryWatcher.EnableRaisingEvents = true;
                    Console.WriteLine("Logwatcher is setup.");
                    return true;
                }
                else
                {
                    MessageBox.Show("Could not locate VRChat data directory for watcher");
                }
            }
            return false;
        }

        private static void CleanupLogWatcher()
        {
            if (logDirectoryWatcher != null)
            {
                logDirectoryWatcher.EnableRaisingEvents = false;
                logDirectoryWatcher.Changed -= OnLogFileChanged;
                logDirectoryWatcher.Dispose();
                logDirectoryWatcher = null;
            }
        }

        private static void OnLogFileChanged(object source, FileSystemEventArgs args)
        {
            Console.WriteLine("File has changed, called OnLogFileChanged()");
            lock (logModifiedLock)
            {
                modifiedLogPaths.Add(args.FullPath);
            }
        }

        const string MATCH_STR = "\\n\\n\\r\\n\\d{4}.\\d{2}.\\d{2} \\d{2}:\\d{2}:\\d{2} ";
        static Regex lineMatch;

        internal static void Update()
        {
            if (!InitializeScriptLookup())
                return;

            if (lineMatch == null)
                lineMatch = new Regex(MATCH_STR, RegexOptions.Compiled);

            List<(string, string)> modifiedFilesAndContents = null;

            lock (logModifiedLock)
            {
                if (modifiedLogPaths.Count > 0)
                {
                    Console.WriteLine("Files have changed!");
                    modifiedFilesAndContents = new List<(string, string)>();
                    HashSet<string> newLogPaths = new HashSet<string>();

                    foreach (string logPath in modifiedLogPaths)
                    {
                        if (!logFileStates.TryGetValue(logPath, out LogFileState logState))
                            logFileStates.Add(logPath, new LogFileState());

                        logState = logFileStates[logPath];

                        string newLogContent = "";

                        newLogPaths.Add(logPath);

                        try
                        {
                            FileInfo fileInfo = new FileInfo(logPath);

                            using (var stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                using (StreamReader reader = new StreamReader(stream))
                                {
                                    if (logState.playerName == null) // Search for the player name that this log belongs to
                                    {
                                        string fullFileContents = reader.ReadToEnd();

                                        const string SEARCH_STR = "[VRCFlowManagerVRC] User Authenticated: ";
                                        int userIdx = fullFileContents.IndexOf(SEARCH_STR);
                                        if (userIdx != -1)
                                        {
                                            userIdx += SEARCH_STR.Length;

                                            int endIdx = userIdx;

                                            while (fullFileContents[endIdx] != '\r' && fullFileContents[endIdx] != '\n') endIdx++; // Seek to end of name

                                            string username = fullFileContents.Substring(userIdx, endIdx - userIdx);

                                            logState.playerName = username;

                                            // Use the log path as well since Build & Test can have multiple of the same display named users
                                            System.Random random = new System.Random((username + logPath).GetHashCode());
                                        }
                                    }

                                    if (logState.lineOffset == -1)
                                    {
                                        reader.BaseStream.Seek(0, SeekOrigin.End);
                                    }
                                    else
                                    {
                                        reader.BaseStream.Seek(logState.lineOffset - 4 < 0 ? 0 : logState.lineOffset - 4, SeekOrigin.Begin); // Subtract 4 characters to pick up the newlines from the prior line for the log forwarding
                                    }

                                    newLogContent = reader.ReadToEnd();

                                    logFileStates[logPath].lineOffset = reader.BaseStream.Position;
                                    reader.Close();
                                }

                                stream.Close();
                            }

                            newLogPaths.Remove(logPath);

                            if (newLogContent != "")
                                modifiedFilesAndContents.Add((logPath, newLogContent));
                        }
                        catch (System.IO.IOException)
                        { }
                    }

                    modifiedLogPaths = newLogPaths;
                }
            }

            if (modifiedFilesAndContents != null)
            {
                foreach (var modifiedFile in modifiedFilesAndContents)
                {
                    LogFileState state = logFileStates[modifiedFile.Item1];

                    // Log forwarding
                    int currentIdx = 0;
                    Match match = null;

                    do
                    {
                        currentIdx = (match?.Index ?? -1);

                        match = lineMatch.Match(modifiedFile.Item2, currentIdx + 1);

                        string logStr = null;

                        if (currentIdx == -1)
                        {
                            if (match.Success)
                            {
                                Match nextMatch = lineMatch.Match(modifiedFile.Item2, match.Index + 1);

                                if (nextMatch.Success)
                                    logStr = modifiedFile.Item2.Substring(0, nextMatch.Index);
                                else
                                    logStr = modifiedFile.Item2;

                                match = nextMatch;
                            }
                        }
                        else if (match.Success)
                        {
                            logStr = modifiedFile.Item2.Substring(currentIdx < 0 ? 0 : currentIdx, match.Index - currentIdx);
                        }
                        else if (currentIdx != -1)
                        {
                            logStr = modifiedFile.Item2.Substring(currentIdx < 0 ? 0 : currentIdx, modifiedFile.Item2.Length - currentIdx);
                        }

                        if (logStr != null)
                        {
                            logStr = logStr.Trim('\n', '\r');

                            HandleForwardedLog(logStr, state);
                        }
                    } while (match.Success);
                }
            }
        }

        // Common messages that can spam the log and have no use for debugging
        static readonly string[] filteredPrefixes = new string[]
        {
            "Received Notification: <Notification from username:",
            "Received Message of type: notification content: {{\"id\":\"",
            "[VRCFlowNetworkManager] Sending token from provider vrchat",
            "[USpeaker] uSpeak [",
            "Internal: JobTempAlloc has allocations",
            "To Debug, enable the define: TLA_DEBUG_STACK_LEAK in ThreadsafeLinearAllocator.cpp.",
            "PLAYLIST GET id=",
            "Checking server time received at ",
            "[RoomManager] Room metadata is unchanged, skipping update",
        };

        static void HandleForwardedLog(string logMessage, LogFileState state)
        {
            const string FMT_STR = "0000.00.00 00:00:00 ";

            string trimmedStr = logMessage.Substring(FMT_STR.Length);

            string message = trimmedStr.Substring(trimmedStr.IndexOf('-') + 2);
            string trimmedMessage = message.TrimStart(' ', '\t');

            string prefixStr = trimmedMessage;

            if (!prefixStr.StartsWith("[VRCWorldPersistency]"))
            {
                Console.WriteLine("Other message: " + message);
                return;
            }
            else
            {
                Console.WriteLine(message);
            }
        }
    }
}
