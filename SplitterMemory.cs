using System;
using System.Diagnostics;
namespace LiveSplit.ApeOut {
	public partial class SplitterMemory {
		private static ProgramPointer GameplayDirector = new ProgramPointer(AutoDeref.None, DerefType.Int64, "GameAssembly.dll",
			new ProgramSignature(PointerVersion.Steam, 0x1be1cd0)
		);
		//1st: new ProgramSignature(PointerVersion.Steam, 0x1ba0198)
		//2nd: new ProgramSignature(PointerVersion.Steam, 0x1baf0f0)
		//3rd: new ProgramSignature(PointerVersion.Steam, 0x1be1cd0)
		public Process Program { get; set; }
		public bool IsHooked { get; set; } = false;
		public DateTime LastHooked;

		public SplitterMemory() {
			LastHooked = DateTime.MinValue;
		}

		public string Pointer() {
			long pointer = (long)GameplayDirector.Pointer;
			return pointer.ToString("X") + " - " + (pointer != 0 ? GameplayDirector.Read<long>(Program, 0xb8, 0x0).ToString("X") : string.Empty);
		}
		public bool IsValid() {
			return IsHooked && LastHooked.AddSeconds(5) < DateTime.Now && GameplayDirector.Pointer != IntPtr.Zero && GameplayDirector.Read<long>(Program, 0xb8, 0x0) != 0;
		}
		public int LevelNumber() {
			return GameplayDirector.Read<int>(Program, 0xb8, 0x0, 0x8c);
		}
		public int Kills() {
			return GameplayDirector.Read<int>(Program, 0xb8, 0x0, 0xc4);
		}
		public int Deaths() {
			return GameplayDirector.Read<int>(Program, 0xb8, 0x0, 0x1b0, 0x1c);
		}
		public int HP() {
			return GameplayDirector.Read<int>(Program, 0xb8, 0x0, 0x28, 0x1c);
		}
		public int FloorNumber() {
			return GameplayDirector.Read<int>(Program, 0xb8, 0x0, 0x1a8);
		}
		public bool Paused() {
			return !GameplayDirector.Read<bool>(Program, 0xb8, 0x0, 0x300);
		}
		public bool HasBeenHit() {
			return !GameplayDirector.Read<bool>(Program, 0xb8, 0x0, 0x26a);
		}
		public bool HasKilled() {
			return !GameplayDirector.Read<bool>(Program, 0xb8, 0x0, 0x269);
		}
		public bool Dead() {
			return GameplayDirector.Read<bool>(Program, 0xb8, 0x0, 0xd1);
		}
		public bool HasNoControl() {
			return GameplayDirector.Read<bool>(Program, 0xb8, 0x0, 0x140);
		}
		public float DiscTimer() {
			return GameplayDirector.Read<float>(Program, 0xb8, 0x0, 0x138);
		}
		public float XPos() {
			return GameplayDirector.Read<float>(Program, 0xb8, 0x0, 0x174);
		}
		public float YPos() {
			return GameplayDirector.Read<float>(Program, 0xb8, 0x0, 0x178);
		}
		public bool Visible() {
			return GameplayDirector.Read<bool>(Program, 0xb8, 0x0, 0x90);
		}
		public bool DiscComplete() {
			return GameplayDirector.Read<bool>(Program, 0xb8, 0x0, 0x318);
		}
		public bool HookProcess() {
			IsHooked = Program != null && !Program.HasExited;
			if (!IsHooked && DateTime.Now > LastHooked.AddSeconds(1)) {
				LastHooked = DateTime.Now;
				Process[] processes = Process.GetProcessesByName("ApeOut");
				Program = processes != null && processes.Length > 0 ? processes[0] : null;

				if (Program != null && !Program.HasExited) {
					MemoryReader.Update64Bit(Program);
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