using System;
using System.Diagnostics;
namespace LiveSplit.ApeOut {
	public enum PointerVersion {
		Steam
	}
	public enum AutoDeref {
		None,
		Single,
		Double
	}
	public enum DerefType {
		Int32,
		Int64
	}
	public class ProgramSignature {
		public PointerVersion Version { get; set; }
		public int[] Offsets { get; set; }
		public ProgramSignature(PointerVersion version, params int[] offsets) {
			Version = version;
			Offsets = offsets;
		}
		public override string ToString() {
			return Version.ToString() + " - " + Offsets[0];
		}
	}
	public class ProgramPointer {
		private int lastID;
		private DateTime lastTry;
		private ProgramSignature[] signatures;
		public IntPtr Pointer { get; private set; }
		public PointerVersion Version { get; private set; }
		public AutoDeref AutoDeref { get; private set; }
		public DerefType DerefType { get; private set; }
		public string AsmName { get; private set; }

		public ProgramPointer(AutoDeref autoDeref, DerefType derefType, string asmName, params ProgramSignature[] signatures) {
			AutoDeref = autoDeref;
			DerefType = derefType;
			this.signatures = signatures;
			AsmName = asmName;
			lastID = -1;
			lastTry = DateTime.MinValue;
		}

		public T Read<T>(Process program, params int[] offsets) where T : struct {
			GetPointer(program);
			return program.Read<T>(Pointer, offsets);
		}
		public string Read(Process program, IntPtr address) {
			GetPointer(program);
			return program.ReadString(address);
		}
		public string Read(Process program, params int[] offsets) {
			GetPointer(program);
			return program.ReadString(Pointer, offsets);
		}
		public string ReadAscii(Process program, IntPtr address) {
			GetPointer(program);
			return program.ReadAscii(address);
		}
		public byte[] ReadBytes(Process program, int length, params int[] offsets) {
			GetPointer(program);
			return program.ReadBytes(Pointer, length, offsets);
		}
		public void Write<T>(Process program, T value, params int[] offsets) where T : struct {
			GetPointer(program);
			program.Write<T>(Pointer, value, offsets);
		}
		public void Write(Process program, byte[] value, params int[] offsets) {
			GetPointer(program);
			program.Write(Pointer, value, offsets);
		}
		public void ClearPointer() {
			Pointer = IntPtr.Zero;
		}
		public IntPtr GetPointer(Process program) {
			if (program == null) {
				Pointer = IntPtr.Zero;
				lastID = -1;
				return Pointer;
			} else if (program.Id != lastID) {
				Pointer = IntPtr.Zero;
				lastID = program.Id;
			}

			if (Pointer == IntPtr.Zero && DateTime.Now > lastTry.AddSeconds(1)) {
				lastTry = DateTime.Now;

				Pointer = GetVersionedFunctionPointer(program);
				if (Pointer != IntPtr.Zero) {
					if (AutoDeref != AutoDeref.None) {
						if (DerefType == DerefType.Int32) {
							Pointer = (IntPtr)program.Read<uint>(Pointer);
						} else {
							Pointer = (IntPtr)program.Read<ulong>(Pointer);
						}
						if (AutoDeref == AutoDeref.Double) {
							if (DerefType == DerefType.Int32) {
								Pointer = (IntPtr)program.Read<uint>(Pointer);
							} else {
								Pointer = (IntPtr)program.Read<ulong>(Pointer);
							}
						}
					}
				}
			}
			return Pointer;
		}
		private IntPtr GetVersionedFunctionPointer(Process program) {
			IntPtr baseAddress = program.MainModule.BaseAddress;
			if (!string.IsNullOrEmpty(AsmName)) {
				Module64[] modules = program.Modules64();
				for (int i = 0; i < modules.Length; i++) {
					Module64 module = modules[i];
					if (module.Name.Equals(AsmName, StringComparison.OrdinalIgnoreCase)) {
						baseAddress = module.BaseAddress;
						break;
					}
				}
			}

			for (int i = 0; i < signatures.Length; i++) {
				ProgramSignature signature = signatures[i];
				IntPtr pointer = IntPtr.Zero;
				if (DerefType == DerefType.Int32) {
					pointer = (IntPtr)program.Read<uint>(baseAddress, signature.Offsets);
				} else {
					pointer = (IntPtr)program.Read<ulong>(baseAddress, signature.Offsets);
				}
				if (pointer != IntPtr.Zero) {
					return pointer;
				}
			}
			return IntPtr.Zero;
		}
	}
}