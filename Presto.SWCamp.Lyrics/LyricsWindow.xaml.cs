using Presto.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        int currentLyricIndex;

        public LyricsWindow() {
            InitializeComponent();

            PrestoSDK.PrestoService.Player.StreamChanged += Player_StreamChanged;
        }

        private void Player_StreamChanged(object sender, Common.StreamChangedEventArgs e) {
            this.Activate();

            String filePath = PrestoSDK.PrestoService.Player.CurrentMusic.Path;

            // TODO:: 확장자까지 동적으로 짜르기
            currentLyricIndex = 0;
            lyricsRaw = File.ReadAllLines(filePath.Substring(0, filePath.Length-3) + "lrc");
            list = new List<string>();
            time = new List<TimeSpan>();
            //lyricsTextBoxManager = new LyricsTextBoxManager();

            // 가사 파싱
            foreach (var line in lyricsRaw) {
                if (line[1] > 64) {
                    continue;
                }
                int threshold = line.IndexOf(']');
                String str = line.Substring(1, threshold-1);
                var timeSplited = str.Split(new char[] { ':', '.' });
                TimeSpan timeSpan = new TimeSpan(0, 0, int.Parse(timeSplited[0])
                                            , int.Parse(timeSplited[1])
                                            , int.Parse(timeSplited[2]) * 10);
                if (time.Count > 0 && time[time.Count-1].TotalMilliseconds == timeSpan.TotalMilliseconds) {
                    list[list.Count - 1] += "\n" + line.Substring(threshold + 1);
                } else {
                    time.Add(timeSpan);
                    list.Add(line.Substring(threshold+1));
                }
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

            // 도입부 '노래 - 가수명' 출력
            if (currentPlayTime < time[0].TotalMilliseconds-1000*10) {
                text_lyrics.Text = PrestoSDK.PrestoService.Player.CurrentMusic.Title + " - " + PrestoSDK.PrestoService.Player.CurrentMusic.Artist.Name;
                currentLyricIndex = 0;
            }

            // FF 가사 이동
            for ( ; currentLyricIndex < time.Count && currentPlayTime > time[currentLyricIndex].TotalMilliseconds;) {

                // 마지막 가사 처리
                if (currentLyricIndex == time.Count-1) {
                    text_lyrics.Text = list[currentLyricIndex];
                    break;
                }

                // 건너뛰는 가사 있는지 확인
                if (currentPlayTime < time[currentLyricIndex+1].TotalMilliseconds) {
                    text_lyrics.Text = list[currentLyricIndex++];
                    return;
                }
                currentLyricIndex++;
            }

            // FR 가사 이동
            for (; currentLyricIndex > 0 && currentPlayTime < time[currentLyricIndex].TotalMilliseconds;) {
                currentLyricIndex--;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            if (MouseButtonState.Pressed == Mouse.LeftButton) {
                this.DragMove();
            }
        }
    }
}
