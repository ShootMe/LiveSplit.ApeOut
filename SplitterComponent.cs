using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
namespace LiveSplit.ApeOut {
	public class SplitterComponent : IComponent {
		public TimerModel Model { get; set; }
		public string ComponentName { get { return "Ape Out Autosplitter " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3); } }
		public IDictionary<string, Action> ContextMenuControls { get { return null; } }
		private static string LOGFILE = "_ApeOut.txt";
		private Dictionary<LogObject, string> currentValues = new Dictionary<LogObject, string>();
		private SplitterMemory mem;
		private TextComponent infoComponent;
		private int currentSplit = -1, lastLogCheck, lastLevel;
		private int totalKills, lastKills, currentKills, totalDeaths, totalHits, lastHits;
		private bool hasLog = false, lastComplete = false, changed = true;
		private List<string> infoList = new List<string>();
		private string currentInfo;
		private float apeOutXPos = 0;
		private Thread updateLoop;
		public SplitterComponent(LiveSplitState state) {
			mem = new SplitterMemory();
			foreach (LogObject key in Enum.GetValues(typeof(LogObject))) {
				currentValues[key] = "";
			}

			if (state != null) {
				Model = new TimerModel() { CurrentState = state };
				Model.InitializeGameTime();
				Model.CurrentState.IsGameTimePaused = true;
				state.OnReset += OnReset;
				state.OnPause += OnPause;
				state.OnResume += OnResume;
				state.OnStart += OnStart;
				state.OnSplit += OnSplit;
				state.OnUndoSplit += OnUndoSplit;
				state.OnSkipSplit += OnSkipSplit;

				updateLoop = new Thread(UpdateLoop);
				updateLoop.IsBackground = true;
				updateLoop.Start();
			}
		}
		private void UpdateLoop() {
			while (updateLoop != null) {
				try {
					GetValues();
				} catch (Exception ex) {
					WriteLog(ex.ToString());
				}
				Thread.Sleep(8);
			}
		}
		public void GetValues() {
			if (!mem.HookProcess()) { return; }

			if (Model != null) {
				HandleSplits();
			}

			LogValues();
		}
		private void HandleSplits() {
			bool shouldSplit = false;

			if (Model.CurrentState.CurrentPhase == TimerPhase.NotRunning) {
				ResetVariables();
				shouldSplit = mem.Kills() == 0 && mem.HP() == 0 && mem.FloorNumber() == 0 && mem.HasBeenHit() && mem.IsValid();
			} else {
				int level = mem.LevelNumber();
				bool complete = mem.DiscComplete();
				if (complete && level == 6) {
					float xpos = mem.XPos();
					if (!lastComplete) {
						apeOutXPos = xpos + 133;
					}
					shouldSplit = xpos >= apeOutXPos;
				} else {
					shouldSplit = (Model.CurrentState.Run.Count != 4 && level == lastLevel + 1) || (complete && !lastComplete && (level > 5 || mem.DiscTimer() > 100));
				}
				lastLevel = level;
				lastComplete = complete;
			}

			UpdateKills();
			UpdateHits();

			Model.CurrentState.IsGameTimePaused = Model.CurrentState.CurrentPhase != TimerPhase.Running;

			HandleSplit(shouldSplit, false);
		}
		private void ResetVariables(bool onReset = false, bool onStart = false) {
			currentSplit = onStart ? 1 : 0;
			totalKills = 0;
			lastKills = 0;
			if (onReset || onStart) {
				totalDeaths = 0;
				totalHits = 0;
				lastHits = 0;
			}
		}
		private void UpdateKills() {
			int kills = mem.Kills();
			if (kills == 0 && lastKills > 0) {
				totalKills += lastKills;
			}
			lastKills = kills;
			if (currentKills != totalKills + kills) {
				changed = true;
				currentKills = totalKills + kills;
			}
		}
		private void UpdateHits() {
			int hits = mem.HP();
			if (hits < lastHits && !mem.Paused()) {
				totalHits++;
				if (hits == 0) {
					totalDeaths++;
				}
				changed = true;
			}
			lastHits = hits;
		}
		private void HandleSplit(bool shouldSplit, bool shouldReset = false) {
			if (shouldReset) {
				if (currentSplit > 0) {
					Model.Reset();
				}
			} else if (shouldSplit) {
				if (currentSplit <= 0) {
					Model.Start();
				} else {
					Model.Split();
				}
			}
		}
		private void LogValues() {
			if (lastLogCheck == 0) {
				hasLog = File.Exists(LOGFILE);
				lastLogCheck = 300;
			}
			lastLogCheck--;

			if (hasLog || !Console.IsOutputRedirected) {
				string prev = string.Empty, curr = string.Empty;
				foreach (LogObject key in Enum.GetValues(typeof(LogObject))) {
					prev = currentValues[key];

					switch (key) {
						case LogObject.CurrentSplit: curr = currentSplit.ToString(); break;
						case LogObject.Pointer: curr = mem.Pointer().ToString(); break;
						case LogObject.IsValid: curr = mem.IsValid().ToString(); break;
						case LogObject.LevelNumber: curr = mem.LevelNumber().ToString(); break;
						case LogObject.Dead: curr = mem.Dead().ToString(); break;
						case LogObject.Kills: curr = mem.Kills().ToString(); break;
						case LogObject.Deaths: curr = mem.Deaths().ToString(); break;
						case LogObject.Hit: curr = mem.HasBeenHit().ToString(); break;
						case LogObject.NoControl: curr = mem.HasNoControl().ToString(); break;
						case LogObject.HP: curr = mem.HP().ToString(); break;
						case LogObject.Visible: curr = mem.Visible().ToString(); break;
						case LogObject.Paused: curr = mem.Paused().ToString(); break;
						case LogObject.DiscComplete: curr = mem.DiscComplete().ToString(); break;
						case LogObject.FloorNumber: curr = mem.FloorNumber().ToString(); break;
						default: curr = string.Empty; break;
					}

					if (prev == null) { prev = string.Empty; }
					if (curr == null) { curr = string.Empty; }
					if (!prev.Equals(curr)) {
						WriteLogWithTime(key.ToString() + ": ".PadRight(16 - key.ToString().Length, ' ') + prev.PadLeft(25, ' ') + " -> " + curr);

						currentValues[key] = curr;
					}
				}
			}
		}
		private void WriteLog(string data) {
			lock (LOGFILE) {
				if (hasLog || !Console.IsOutputRedirected) {
					if (!Console.IsOutputRedirected) {
						Console.WriteLine(data);
					}
					if (hasLog) {
						using (StreamWriter wr = new StreamWriter(LOGFILE, true)) {
							wr.WriteLine(data);
						}
					}
				}
			}
		}
		private void WriteLogWithTime(string data) {
			WriteLog(DateTime.Now.ToString(@"HH\:mm\:ss.fff") + (Model != null && Model.CurrentState.CurrentTime.RealTime.HasValue ? " | " + Model.CurrentState.CurrentTime.RealTime.Value.ToString("G").Substring(3, 11) : "") + ": " + data);
		}
		public void Update(IInvalidator invalidator, LiveSplitState lvstate, float width, float height, LayoutMode mode) {
			if ((lastLogCheck & 63) == 0) {
				IList<ILayoutComponent> components = lvstate.Layout.LayoutComponents;
				infoComponent = null;
				for (int i = components.Count - 1; i >= 0; i--) {
					ILayoutComponent component = components[i];
					if (component.Component is TextComponent) {
						TextComponent text = (TextComponent)component.Component;
						if (text.Settings.Text1.IndexOf("Deaths", StringComparison.OrdinalIgnoreCase) >= 0
							|| text.Settings.Text1.IndexOf("Kills", StringComparison.OrdinalIgnoreCase) >= 0
							|| text.Settings.Text1.IndexOf("Hits", StringComparison.OrdinalIgnoreCase) >= 0) {
							infoComponent = text;
							changed = true;
							break;
						}
					}
				}
			}

			if (infoComponent != null) {
				if (changed) {
					changed = false;
					infoList.Clear();
					int killIndex = infoComponent.Settings.Text1.IndexOf("Kills", StringComparison.OrdinalIgnoreCase);
					int deathIndex = infoComponent.Settings.Text1.IndexOf("Deaths", StringComparison.OrdinalIgnoreCase);
					int hitIndex = infoComponent.Settings.Text1.IndexOf("Hits", StringComparison.OrdinalIgnoreCase);

					if (killIndex >= 0) {
						infoList.Add(killIndex.ToString().PadLeft(3, '0') + currentKills.ToString());
					}
					if (deathIndex >= 0) {
						infoList.Add(deathIndex.ToString().PadLeft(3, '0') + totalDeaths.ToString());
					}
					if (hitIndex >= 0) {
						infoList.Add(hitIndex.ToString().PadLeft(3, '0') + totalHits.ToString());
					}
					infoList.Sort();

					StringBuilder sb = new StringBuilder();
					for (int i = 0; i < infoList.Count; i++) {
						sb.Append(infoList[i].Substring(3)).Append(" / ");
					}
					sb.Length -= 3;
					currentInfo = sb.ToString();
				}
				infoComponent.Settings.Text2 = currentInfo;
			}
		}
		public void OnReset(object sender, TimerPhase e) {
			ResetVariables(true);
			WriteLog("---------Reset----------------------------------");
		}
		public void OnResume(object sender, EventArgs e) {
			WriteLog("---------Resumed--------------------------------");
		}
		public void OnPause(object sender, EventArgs e) {
			WriteLog("---------Paused---------------------------------");
		}
		public void OnStart(object sender, EventArgs e) {
			ResetVariables(false, true);
			Model.CurrentState.SetGameTime(TimeSpan.Zero);
			Model.CurrentState.IsGameTimePaused = true;
			WriteLog("---------New Game " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3) + "-------------------------");
		}
		public void OnUndoSplit(object sender, EventArgs e) {
			currentSplit--;
			WriteLog("---------Undo-----------------------------------");
		}
		public void OnSkipSplit(object sender, EventArgs e) {
			currentSplit++;
			WriteLog("---------Skip-----------------------------------");
		}
		public void OnSplit(object sender, EventArgs e) {
			currentSplit++;
			WriteLog("---------Split----------------------------------");
		}
		public Control GetSettingsControl(LayoutMode mode) { return null; }
		public void SetSettings(XmlNode document) { }
		public XmlNode GetSettings(XmlDocument document) { return document.CreateElement("Settings"); }
		public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion) { }
		public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion) { }
		public float HorizontalWidth { get { return 0; } }
		public float MinimumHeight { get { return 0; } }
		public float MinimumWidth { get { return 0; } }
		public float PaddingBottom { get { return 0; } }
		public float PaddingLeft { get { return 0; } }
		public float PaddingRight { get { return 0; } }
		public float PaddingTop { get { return 0; } }
		public float VerticalHeight { get { return 0; } }
		public void Dispose() {
			if (updateLoop != null) {
				updateLoop = null;
			}
			if (Model != null) {
				Model.CurrentState.OnReset -= OnReset;
				Model.CurrentState.OnPause -= OnPause;
				Model.CurrentState.OnResume -= OnResume;
				Model.CurrentState.OnStart -= OnStart;
				Model.CurrentState.OnSplit -= OnSplit;
				Model.CurrentState.OnUndoSplit -= OnUndoSplit;
				Model.CurrentState.OnSkipSplit -= OnSkipSplit;
				Model = null;
			}
		}
	}
}