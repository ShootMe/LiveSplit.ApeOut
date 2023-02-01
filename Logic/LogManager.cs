using System;
using System.Collections.Generic;
using System.IO;
namespace LiveSplit.ApeOut {
    public enum LogObject {
        CurrentSplit,
        Pointers,
        Version,
        Loading,
        IsValid,
        Disc,
        Level,
        FloorNumber,
        Kills,
        Health,
        Paused,
        Dead,
        Uncaged,
        DiscComplete,
        NewGame,
        Titling,
        Guards
    }
    public class LogManager {
        public const string LOG_FILE = "ApeOut.txt";
        private Dictionary<LogObject, string> currentValues = new Dictionary<LogObject, string>();
        private bool enableLogging;
        public bool EnableLogging {
            get { return enableLogging; }
            set {
                if (value != enableLogging) {
                    enableLogging = value;
                    if (value) {
                        AddEntryUnlocked(new EventLogEntry("Initialized"));
                    }
                }
            }
        }

        public LogManager() {
            EnableLogging = false;
            Clear();
        }
        public void Clear(bool deleteFile = false) {
            lock (currentValues) {
                if (deleteFile) {
                    try {
                        File.Delete(LOG_FILE);
                    } catch { }
                }
                foreach (LogObject key in Enum.GetValues(typeof(LogObject))) {
                    currentValues[key] = null;
                }
            }
        }
        public void AddEntry(ILogEntry entry) {
            lock (currentValues) {
                AddEntryUnlocked(entry);
            }
        }
        private void AddEntryUnlocked(ILogEntry entry) {
            string logEntry = entry.ToString();
            if (EnableLogging) {
                try {
                    using (StreamWriter sw = new StreamWriter(LOG_FILE, true)) {
                        sw.WriteLine(logEntry);
                    }
                } catch { }
                Console.WriteLine(logEntry);
            }
        }
        public void Update(LogicManager logic, SplitterSettings settings) {
            if (!EnableLogging) { return; }

            lock (currentValues) {
                DateTime date = DateTime.Now;
                bool updateLog = true;
                bool isLoading = logic.Memory.IsLoading();

                foreach (LogObject key in Enum.GetValues(typeof(LogObject))) {
                    string previous = currentValues[key];
                    string current = null;

                    switch (key) {
                        case LogObject.CurrentSplit: current = $"{logic.CurrentSplit} ({GetCurrentSplit(logic, settings)})"; break;
                        case LogObject.Pointers: current = logic.Memory.GamePointers(); break;
                        case LogObject.Version: current = MemoryManager.Version.ToString(); break;
                        case LogObject.Loading: current = isLoading.ToString(); break;
                        case LogObject.Titling: current = logic.Memory.Titling().ToString(); break;
                        case LogObject.Guards: current = logic.Memory.GuardsOnScreen().ToString(); break;
                        case LogObject.IsValid: current = updateLog ? logic.Memory.IsValid().ToString() : previous; break;
                        case LogObject.Disc: current = updateLog ? logic.Memory.Disc().ToString() : previous; break;
                        case LogObject.Level: current = updateLog ? logic.Memory.Level().ToString() : previous; break;
                        case LogObject.FloorNumber: current = updateLog ? logic.Memory.FloorNumber().ToString() : previous; break;
                        case LogObject.Kills: current = updateLog ? logic.Memory.Kills().ToString() : previous; break;
                        case LogObject.Health: current = updateLog ? logic.Memory.Health().ToString() : previous; break;
                        case LogObject.Paused: current = updateLog ? logic.Memory.Paused().ToString() : previous; break;
                        case LogObject.Dead: current = updateLog ? logic.Memory.Dead().ToString() : previous; break;
                        case LogObject.Uncaged: current = updateLog ? logic.Memory.Uncaged().ToString() : previous; break;
                        case LogObject.DiscComplete: current = updateLog ? logic.Memory.DiscComplete().ToString() : previous; break;
                        case LogObject.NewGame: current = updateLog ? (logic.Memory.PlayerPosition() == Vector2.ZERO).ToString() : previous; break;
                    }

                    if (previous != current) {
                        AddEntryUnlocked(new ValueLogEntry(date, key, previous, current));
                        currentValues[key] = current;
                    }
                }
            }
        }
        private string GetCurrentSplit(LogicManager logic, SplitterSettings settings) {
            if (logic.CurrentSplit >= settings.Autosplits.Count) { return "N/A"; }
            return settings.Autosplits[logic.CurrentSplit].ToString();
        }
    }
    public interface ILogEntry { }
    public class ValueLogEntry : ILogEntry {
        public DateTime Date;
        public LogObject Type;
        public object PreviousValue;
        public object CurrentValue;

        public ValueLogEntry(DateTime date, LogObject type, object previous, object current) {
            Date = date;
            Type = type;
            PreviousValue = previous;
            CurrentValue = current;
        }

        public override string ToString() {
            return string.Concat(
                Date.ToString(@"HH\:mm\:ss.fff"),
                ": (",
                Type.ToString(),
                ") ",
                PreviousValue,
                " -> ",
                CurrentValue
            );
        }
    }
    public class EventLogEntry : ILogEntry {
        public DateTime Date;
        public string Event;

        public EventLogEntry(string description) {
            Date = DateTime.Now;
            Event = description;
        }
        public EventLogEntry(DateTime date, string description) {
            Date = date;
            Event = description;
        }

        public override string ToString() {
            return string.Concat(
                Date.ToString(@"HH\:mm\:ss.fff"),
                ": ",
                Event
            );
        }
    }
}
