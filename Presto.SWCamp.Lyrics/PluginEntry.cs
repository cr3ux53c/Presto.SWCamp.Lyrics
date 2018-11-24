using Presto.Common;
using Presto.SDK;
using System;
using System.IO;
using System.Windows.Threading;

[assembly: PrestoTitle("Lyrics")]
[assembly: PrestoAuthor("SW Camp")]
[assembly: PrestoDescription("가사 플러그인 입니다.")]

namespace Presto.SWCamp.Lyrics {
    public class PluginEntry : PrestoPlugin {
        private LyricsWindow _lyrics;
        private string[] lines;

        public override void OnLoad() {
            _lyrics = new LyricsWindow();
            _lyrics.Show();            
        }

        public override void OnUnload() {
            _lyrics.Close();
        }
    }
}
