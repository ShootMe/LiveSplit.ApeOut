﻿using System;
using System.Diagnostics;
using System.Text;
namespace LiveSplit.ApeOut {
    public partial class MemoryManager {
        private static ProgramPointer Global = new ProgramPointer("GameAssembly.dll",
            new FindPointerSignature(PointerVersion.All, AutoDeref.Single, "4533C94C8BC3488BD7488BC84C8BF0E8????????4885F60F84????????4C8B05????????498BD6488BCEE8????????488B05????????488B88B8000000488939488B0D????????F681????000001740E4439B9", 0x32, 0x0),
            new FindPointerSignature(PointerVersion.All, AutoDeref.Single, "33D2488BCBE8????????4C8B05????????4C8BC84885C0750433D2EB24488B00", 13, 0));
        private static ProgramPointer HealthMaster = new ProgramPointer("GameAssembly.dll",
            new FindPointerSignature(PointerVersion.All, AutoDeref.Single, "4533C94533C0488BD333C9E8????????84C00F85????????4889742460488B05????????4533C0", 32, 0),
            new FindPointerSignature(PointerVersion.All, AutoDeref.Single, "4533C033D2488BCBE8????????84C00F85????????4889742460488B05????????488BD7488B88B8000000488939", 29, 0));
        public static PointerVersion Version { get; set; } = PointerVersion.All;
        public Process Program { get; set; }
        public bool IsHooked { get; set; }
        public DateTime LastHooked { get; set; }
        private bool lastTitling = false;
        private int lastGuards;
        private IntPtr globalPtr;
        private Vector2 lastPos;
        private bool setPos;

        public MemoryManager() {
            LastHooked = DateTime.MinValue;
        }
        public string GamePointers() {
            return string.Concat(
                $"GLB: {(ulong)Global.GetPointer(Program):X} "
            );
        }
        public bool IsLoading() {
            //HealthMaster.me
            IntPtr healthPtr = HealthMaster.Read<IntPtr>(Program, 0xb8, 0x0);
            if (healthPtr == IntPtr.Zero) { return true; }

            //Global.me
            globalPtr = Global.Read<IntPtr>(Program, 0xb8, 0x0);

            bool titling = Titling();
            int guards = titling ? GuardsOnScreen() : 0;
            Vector2 playerPos = titling ? PlayerPosition() : Vector2.ZERO;

            if (titling != lastTitling) {
                if (titling & Paused()) {
                    lastPos = playerPos;
                    setPos = true;
                } else {
                    lastGuards = 0;
                }
            }

            if (titling && playerPos != lastPos && setPos) {
                lastGuards = guards;
                lastPos = playerPos;
                setPos = false;
            }

            lastTitling = titling;

            return titling && guards == lastGuards && !setPos;
        }
        public string MapLines() {
            StringBuilder sb = new StringBuilder();
            //Global.me.mapGenerator.mapLines
            IntPtr ptr = Program.Read<IntPtr>(globalPtr, 0x188, 0x70);
            int count = Program.Read<int>(ptr, 0x18);
            for (int i = 0; i < count; i++) {
                string line = Program.ReadString(ptr, 0x20 + (i * 8), 0x0);

                if (line.IndexOf('*') >= 0 || line.IndexOf('H') >= 0 || (line.IndexOf('S') >= 0 && line.Length > 4)) {
                    byte[] data = Encoding.Unicode.GetBytes(line.Replace("*", "2").Replace("H", "2").Replace("S", "M"));
                    Program.Write(ptr, data, 0x20 + (i * 8), 0x14);
                }

                sb.AppendLine(line);
            }
            return sb.ToString();
        }
        public int GuardsOnScreen() {
            //Global.me.guardsOnScreen
            return Program.Read<int>(globalPtr, 0x13c);
        }
        public bool Titling() {
            //Global.me.titling
            return Program.Read<bool>(globalPtr, 0x140);
        }
        public bool IsValid() {
            return globalPtr != IntPtr.Zero;
        }
        public Album Disc() {
            //Global.me.healthMaster.albumIndex
            return Program.Read<Album>(globalPtr, 0xa8, 0x15c);
        }
        public int Level() {
            //Global.me.level
            return Program.Read<int>(globalPtr, 0x8c);
        }
        public int FloorNumber() {
            //Global.me.curFloor
            return Program.Read<int>(globalPtr, 0x1a8);
        }
        public float TimeSinceLastKill() {
            //Global.me.timeSinceLastKill
            return Program.Read<float>(globalPtr, 0x138);
        }
        public int Kills() {
            //Global.me.guardsKilled
            return Program.Read<int>(globalPtr, 0xc4);
        }
        public int Health() {
            //Global.me.playerState.health
            return Program.Read<int>(globalPtr, 0x28, 0x1c);
        }
        public Vector2 PlayerPosition() {
            //Global.me.playerMovement.myPos
            return Program.Read<Vector2>(globalPtr, 0x160, 0x120);
        }
        public bool Paused() {
            //Global.me.fixedNum
            return Program.Read<int>(globalPtr, 0x300) == 0;
        }
        public bool Dead() {
            //Global.me.killerAssigned
            return Program.Read<bool>(globalPtr, 0xd1);
        }
        public bool Uncaged() {
            //Global.me.uncaged
            return Program.Read<bool>(globalPtr, 0x90);
        }
        public bool DiscComplete() {
            //Global.me.dontPause
            return Program.Read<bool>(globalPtr, 0x318);
        }
        public bool HookProcess() {
            IsHooked = Program != null && !Program.HasExited;
            if (!IsHooked && DateTime.Now > LastHooked.AddSeconds(1)) {
                LastHooked = DateTime.Now;

                globalPtr = IntPtr.Zero;
                Process[] processes = Process.GetProcessesByName("ApeOut");
                Program = processes != null && processes.Length > 0 ? processes[0] : null;

                if (Program != null && !Program.HasExited) {
                    MemoryReader.Update64Bit(Program);
                    MemoryManager.Version = PointerVersion.All;
                    IsHooked = true;
                }
            }

            return IsHooked;
        }
        public void Dispose() {
            if (Program != null) {
                Program.Dispose();
            }
        }
    }
}