using System.ComponentModel;
namespace LiveSplit.ApeOut {
    public enum SplitType {
        [Description("Manual Split")]
        ManualSplit,
        [Description("Album")]
        Album,
        [Description("Game Start")]
        GameStart,
        [Description("Track")]
        Track
    }
    public class Split {
        public string Name { get; set; }
        public SplitType Type { get; set; }
        public string Value { get; set; }

        public override string ToString() {
            return $"{Type}|{Value}";
        }
    }
}