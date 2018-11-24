using Presto.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Presto.SWCamp.Lyrics {
    public partial class LyricsWindow : Window {
        String[] lyricsRaw;
        DispatcherTimer timer;
        List<String> list;
        List<TimeSpan> time;
        int currentLyricIndex = 0;

        public LyricsWindow() {
            InitializeComponent();

            PrestoSDK.PrestoService.Player.StreamChanged += Player_StreamChanged;
        }

        private void Player_StreamChanged(object sender, Common.StreamChangedEventArgs e) {
            String filePath = PrestoSDK.PrestoService.Player.CurrentMusic.Path;
            // TODO:: 확장자까지 동적으로 짜르기
            lyricsRaw = File.ReadAllLines(filePath.Substring(0, filePath.Length-3) + "lrc");
            list = new List<string>();
            time = new List<TimeSpan>();

            //
            foreach (var line in lyricsRaw) {
                if (line[1] > 64) {
                    continue;
                }
                int threshold = line.IndexOf(']');
                list.Add(line.Substring(threshold+1));
                String str = line.Substring(1, threshold-1);
                var timeSplited = str.Split(new char[] { ':', '.' });
                time.Add(new TimeSpan(0, 0, int.Parse(timeSplited[0])
                                            , int.Parse(timeSplited[1])
                                            , int.Parse(timeSplited[2])*10));
                // 밀리세컨드는 100 곱해야 함.

                Regex regex = new Regex("[^[]]");

                regex.IsMatch("fdsafdsa");
            }

            
            // 타이밍
            timer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e) {
            int currentPlayTime = (int)PrestoSDK.PrestoService.Player.Position;

            if (currentPlayTime < time[0].TotalMilliseconds-1000*10) {
                text_lyrics.Text = PrestoSDK.PrestoService.Player.CurrentMusic.Title + " - " + PrestoSDK.PrestoService.Player.CurrentMusic.Artist.Name;
            }

            if (currentPlayTime > time[currentLyricIndex].TotalMilliseconds) {
                text_lyrics.Text = list[currentLyricIndex++];
            } else {
                if (currentLyricIndex > 0)
                    text_lyrics.Text = list[--currentLyricIndex];
            }
        }
    }
}
