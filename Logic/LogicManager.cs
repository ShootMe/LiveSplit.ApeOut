using System;
namespace LiveSplit.ApeOut {
    public class LogicManager {
        public bool ShouldSplit { get; private set; }
        public bool ShouldReset { get; private set; }
        public int CurrentSplit { get; private set; }
        public bool Running { get; private set; }
        public bool Paused { get; private set; }
        public float GameTime { get; private set; }
        public MemoryManager Memory { get; private set; }
        public SplitterSettings Settings { get; private set; }
        public int Kills { get; private set; }
        public int Deaths { get; private set; }
        public int Hits { get; private set; }
        private bool lastBoolValue;
        private int lastIntValue;
        private Vector2 lastVector;
        private DateTime splitLate;
        private int lastKills, totalKills;
        private int lastHits;

        public LogicManager(SplitterSettings settings) {
            Memory = new MemoryManager();
            Settings = settings;
            splitLate = DateTime.MaxValue;
        }

        public void Reset() {
            splitLate = DateTime.MaxValue;
            Paused = false;
            Running = false;
            CurrentSplit = 0;
            InitializeSplit();
            ShouldSplit = false;
            ShouldReset = false;
            ResetStats();
        }
        public void Decrement() {
            CurrentSplit--;
            splitLate = DateTime.MaxValue;
            InitializeSplit();
        }
        public void Increment() {
            Running = true;
            splitLate = DateTime.MaxValue;
            if (CurrentSplit == 0) {
                ResetStats();
            }
            CurrentSplit++;
            InitializeSplit();
        }
        private void InitializeSplit() {
            if (CurrentSplit < Settings.Autosplits.Count) {
                bool temp = ShouldSplit;
                CheckSplit(Settings.Autosplits[CurrentSplit], true);
                ShouldSplit = temp;
            }
        }
        public bool IsHooked() {
            bool hooked = Memory.HookProcess();
            Paused = !hooked;
            ShouldSplit = false;
            ShouldReset = false;
            GameTime = -1;
            return hooked;
        }
        public void Update(int currentSplit) {
            if (currentSplit != CurrentSplit) {
                CurrentSplit = currentSplit;
                Running = CurrentSplit > 0;
                InitializeSplit();
            }

            if (CurrentSplit < Settings.Autosplits.Count) {
                CheckSplit(Settings.Autosplits[CurrentSplit], !Running);
                if (!Running) {
                    Paused = true;
                    if (ShouldSplit) {
                        Running = true;
                    }
                }

                if (ShouldSplit) {
                    Increment();
                }
            }

            UpdateHits();
            UpdateKills();
        }
        private void ResetStats() {
            Kills = 0;
            Deaths = 0;
            Hits = 0;
            totalKills = 0;
            lastKills = 0;
            lastHits = 0;
        }
        private void UpdateKills() {
            int kills = Memory.Kills();
            if (kills == 0 && lastKills > 0) {
                totalKills += lastKills;
            }
            lastKills = kills;
            if (Kills != totalKills + kills) {
                Kills = totalKills + kills;
            }
        }
        private void UpdateHits() {
            int hits = Memory.Health();
            if (hits < lastHits && !Memory.Paused()) {
                Hits++;
                if (hits <= 0) {
                    Deaths++;
                }
            }
            lastHits = hits;
        }
        private void CheckSplit(Split split, bool updateValues) {
            ShouldSplit = false;
            Paused = Memory.IsLoading();

            if (!updateValues && Paused) {
                return;
            }

            switch (split.Type) {
                case SplitType.ManualSplit:
                    break;
                case SplitType.GameStart:
                    CheckGameStart();
                    break;
                case SplitType.Album:
                    CheckAlbum(split);
                    break;
                case SplitType.Track:
                    CheckTrack(split);
                    break;
            }

            if (Running && Paused) {
                ShouldSplit = false;
            } else if (DateTime.Now > splitLate) {
                ShouldSplit = true;
                splitLate = DateTime.MaxValue;
            }
        }
        private void CheckGameStart() {
            Vector2 position = Memory.PlayerPosition();
            bool isValid = Memory.IsValid();
            ShouldSplit = isValid && position == Vector2.ZERO && lastVector != Vector2.ZERO;
            lastVector = isValid ? position : Vector2.INVALID;
        }
        private void CheckAlbumTrack(Album album, int level) {
            Album currentAlbum = (Album)(((int)Memory.Disc() / 2) * 2);
            if (album == Album.Any) { album = currentAlbum; }

            int currentLevel = Memory.Level();
            if (level < 0) {
                if (level == -2) {
                    if (album == Album.Adrift) {
                        level = 6;
                    } else if (album == Album.BreakIn) {
                        level = 0;
                    } else {
                        level = 7;
                    }
                } else {
                    level = lastIntValue;
                }
            }

            bool checkComplete = album == Album.BreakIn || (album == Album.Adrift && level == 6) || level == 7;
            if (checkComplete) {
                bool discComplete = Memory.DiscComplete();
                if (album == Album.Adrift) {
                    CheckEndGame(discComplete);
                } else {
                    ShouldSplit = discComplete && !lastBoolValue && album == currentAlbum;
                }
                lastBoolValue = discComplete;
            } else {
                ShouldSplit = level == lastIntValue && currentLevel > lastIntValue && album == currentAlbum;
            }
            lastIntValue = currentLevel;
        }
        private void CheckEndGame(bool discComplete) {
            if (discComplete && Memory.Level() == 6) {
                Vector2 shadow = Memory.ShadowOrigin();
                if (!lastBoolValue) {
                    lastVector = shadow + 133;
                }
                ShouldSplit = shadow.X > lastVector.X;
            }
        }
        private void CheckAlbum(Split split) {
            SplitAlbumn album = Utility.GetEnumValue<SplitAlbumn>(split.Value);
            switch (album) {
                case SplitAlbumn.Any: CheckAlbumTrack(Album.Any, -2); break;
                case SplitAlbumn.Album1: CheckAlbumTrack(Album.Subject4, 7); break;
                case SplitAlbumn.Album2: CheckAlbumTrack(Album.HighRise, 7); break;
                case SplitAlbumn.Album3: CheckAlbumTrack(Album.Fugue, 7); break;
                case SplitAlbumn.Album4: CheckAlbumTrack(Album.Adrift, 6); break;
                case SplitAlbumn.Single: CheckAlbumTrack(Album.BreakIn, 0); break;
            }
        }
        private void CheckTrack(Split split) {
            SplitTrack track = Utility.GetEnumValue<SplitTrack>(split.Value);
            switch (track) {
                case SplitTrack.Any: CheckAlbumTrack(Album.Any, -1); break;
                case SplitTrack.Album1_1Intro: CheckAlbumTrack(Album.Subject4, 0); break;
                case SplitTrack.Album1_2HeatingUp: CheckAlbumTrack(Album.Subject4, 1); break;
                case SplitTrack.Album1_3KnockKnock: CheckAlbumTrack(Album.Subject4, 2); break;
                case SplitTrack.Album1_4FalseAlarm: CheckAlbumTrack(Album.Subject4, 3); break;
                case SplitTrack.Album1_5PowerDown: CheckAlbumTrack(Album.Subject4, 4); break;
                case SplitTrack.Album1_6LongShadows: CheckAlbumTrack(Album.Subject4, 5); break;
                case SplitTrack.Album1_7Ding: CheckAlbumTrack(Album.Subject4, 6); break;
                case SplitTrack.Album1_8BlownOut: CheckAlbumTrack(Album.Subject4, 7); break;
                case SplitTrack.Album2_1ToTheTop: CheckAlbumTrack(Album.HighRise, 0); break;
                case SplitTrack.Album2_2FullSwing: CheckAlbumTrack(Album.HighRise, 1); break;
                case SplitTrack.Album2_3AimHigh: CheckAlbumTrack(Album.HighRise, 2); break;
                case SplitTrack.Album2_4OverIt: CheckAlbumTrack(Album.HighRise, 3); break;
                case SplitTrack.Album2_5ConcreteJungle: CheckAlbumTrack(Album.HighRise, 4); break;
                case SplitTrack.Album2_6CircleBack: CheckAlbumTrack(Album.HighRise, 5); break;
                case SplitTrack.Album2_7LowPressure: CheckAlbumTrack(Album.HighRise, 6); break;
                case SplitTrack.Album2_8DownAndOut: CheckAlbumTrack(Album.HighRise, 7); break;
                case SplitTrack.Album3_1Contact: CheckAlbumTrack(Album.Fugue, 0); break;
                case SplitTrack.Album3_2Crossfire: CheckAlbumTrack(Album.Fugue, 1); break;
                case SplitTrack.Album3_3RedAlert: CheckAlbumTrack(Album.Fugue, 2); break;
                case SplitTrack.Album3_4Incoming: CheckAlbumTrack(Album.Fugue, 3); break;
                case SplitTrack.Album3_5FireInTheHole: CheckAlbumTrack(Album.Fugue, 4); break;
                case SplitTrack.Album3_6NoMansLand: CheckAlbumTrack(Album.Fugue, 5); break;
                case SplitTrack.Album3_7Fury: CheckAlbumTrack(Album.Fugue, 6); break;
                case SplitTrack.Album3_8BurnOut: CheckAlbumTrack(Album.Fugue, 7); break;
                case SplitTrack.Album4_1HoldFast: CheckAlbumTrack(Album.Adrift, 0); break;
                case SplitTrack.Album4_2RoughSeas: CheckAlbumTrack(Album.Adrift, 1); break;
                case SplitTrack.Album4_3NoQuarter: CheckAlbumTrack(Album.Adrift, 2); break;
                case SplitTrack.Album4_4HitTheDeck: CheckAlbumTrack(Album.Adrift, 3); break;
                case SplitTrack.Album4_5AbandonShip: CheckAlbumTrack(Album.Adrift, 4); break;
                case SplitTrack.Album4_6Wreck: CheckAlbumTrack(Album.Adrift, 5); break;
                case SplitTrack.Album4_7Outro: CheckAlbumTrack(Album.Adrift, 6); break;
            }
        }
    }
}