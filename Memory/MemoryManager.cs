using System;
using System.Diagnostics;
namespace LiveSplit.ApeOut {
    public partial class MemoryManager {
        private static ProgramPointer Global = new ProgramPointer("GameAssembly.dll",
            new FindPointerSignature(PointerVersion.All, AutoDeref.Single, "4533C94C8BC3488BD7488BC84C8BF0E8????????4885F60F84????????4C8B05????????498BD6488BCEE8????????488B05????????488B88B8000000488939488B0D????????F681????000001740E4439B9", 0x32, 0x0));
        public static PointerVersion Version { get; set; } = PointerVersion.All;
        public Process Program { get; set; }
        public bool IsHooked { get; set; }
        public DateTime LastHooked { get; set; }

        public MemoryManager() {
            LastHooked = DateTime.MinValue;
        }
        public string GamePointers() {
            return string.Concat(
                $"GLB: {(ulong)Global.GetPointer(Program):X} "
            );
        }
        public bool AllPointersFound() {
            return Global.GetPointer(Program) != IntPtr.Zero;
        }
        public bool IsLoading() {
            return false;
        }
        public bool IsValid() {
            return Global.Read<IntPtr>(Program, 0xb8, 0x0) != IntPtr.Zero;
        }
        public Album Disc() {
            //Global.me.healthMaster.albumIndex
            return Global.Read<Album>(Program, 0xb8, 0x0, 0xa8, 0x15c);
        }
        public int Level() {
            //Global.me.level
            return Global.Read<int>(Program, 0xb8, 0x0, 0x8c);
        }
        public int FloorNumber() {
            //Global.me.curFloor
            return Global.Read<int>(Program, 0xb8, 0x0, 0x1a8);
        }
        public float TimeSinceLastKill() {
            //Global.me.timeSinceLastKill
            return Global.Read<float>(Program, 0xb8, 0x0, 0x138);
        }
        public int Kills() {
            //Global.me.guardsKilled
            return Global.Read<int>(Program, 0xb8, 0x0, 0xc4);
        }
        public int Health() {
            //Global.me.playerState.health
            return Global.Read<int>(Program, 0xb8, 0x0, 0x28, 0x1c);
        }
        public Vector2 PlayerPosition() {
            //Global.me.playerMovement.myPos
            return Global.Read<Vector2>(Program, 0xb8, 0x0, 0x160, 0x120);
        }
        public bool Paused() {
            //Global.me.fixedNum
            return Global.Read<int>(Program, 0xb8, 0x0, 0x300) == 0;
        }
        public bool Dead() {
            //Global.me.killerAssigned
            return Global.Read<bool>(Program, 0xb8, 0x0, 0xd1);
        }
        public Vector2 ShadowOrigin() {
            //Global.me.shadowOrigin
            return Global.Read<Vector2>(Program, 0xb8, 0x0, 0x174);
        }
        public bool Uncaged() {
            //Global.me.uncaged
            return Global.Read<bool>(Program, 0xb8, 0x0, 0x90);
        }
        public bool DiscComplete() {
            //Global.me.dontPause
            return Global.Read<bool>(Program, 0xb8, 0x0, 0x318);
        }
        public bool HookProcess() {
            IsHooked = Program != null && !Program.HasExited;
            if (!IsHooked && DateTime.Now > LastHooked.AddSeconds(1)) {
                LastHooked = DateTime.Now;

                Process[] processes = Process.GetProcessesByName("ApeOut");
                Program = processes != null && processes.Length > 0 ? processes[0] : null;

                if (Program != null && !Program.HasExited) {
                    MemoryReader.Update64Bit(Program);
                    MemoryManager.Version = PointerVersion.All;
                    //Module64 module = Program.MainModule64();
                    //if (module != null) {
                    //    switch (module.MemorySize) {
                    //        case 77430784: MemoryManager.Version = PointerVersion.V2; break;
                    //    }
                    //}
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