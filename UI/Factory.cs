using System;
using System.Reflection;
using LiveSplit.Model;
using LiveSplit.UI.Components;
namespace LiveSplit.ApeOut {
    public class Factory : IComponentFactory {
        public static string AutosplitterName = "Ape Out Autosplitter";
        public string ComponentName { get { return $"{AutosplitterName} v{Version.ToString(3)}"; } }
        public string Description { get { return AutosplitterName; } }
        public ComponentCategory Category { get { return ComponentCategory.Control; } }
        public IComponent Create(LiveSplitState state) { return new Component(state); }
        public string UpdateName { get { return this.ComponentName; } }
        public string UpdateURL { get { return "https://raw.githubusercontent.com/ShootMe/LiveSplit.ApeOut/master/"; } }
        public string XMLURL { get { return this.UpdateURL + "Components/Updates.xml"; } }
        public Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }
    }
}