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

            //제목 표시줄 색깔 지정
            lyricsTitle.Foreground = new SolidColorBrush(Colors.DarkBlue);
            //텍스트블럭 색깔지정-> 3번이 현재가사, 1,2번 이전가사, 3,4번 다음가사
            text_lyrics.Foreground = new SolidColorBrush(Colors.GhostWhite);
            text_lyrics2.Foreground = new SolidColorBrush(Colors.GhostWhite);
            text_lyrics3.Foreground = new SolidColorBrush(Colors.Chocolate);
            text_lyrics4.Foreground = new SolidColorBrush(Colors.GhostWhite);
            text_lyrics5.Foreground = new SolidColorBrush(Colors.GhostWhite);

            PrestoSDK.PrestoService.Player.StreamChanged += Player_StreamChanged;
        }

        private void Player_StreamChanged(object sender, Common.StreamChangedEventArgs e) {
            this.Activate();
            
            String filePath = PrestoSDK.PrestoService.Player.CurrentMusic.Path;

            //앞의 노래 가사 지우기
            text_lyrics.Text = "";
            text_lyrics2.Text = "";
            text_lyrics4.Text = "";
            text_lyrics5.Text = "";

            // TODO:: 확장자까지 동적으로 짜르기
            currentLyricIndex = 0;
            lyricsRaw = File.ReadAllLines(filePath.Substring(0, filePath.Length-3) + "lrc");
            list = new List<string>();
            time = new List<TimeSpan>();

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

            //다국어 가사이면 창크기를 늘림
            if (list[3].Contains("\n"))
                lyricsWindow.Height = 450;
            else
                lyricsWindow.Height = 200;

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
                text_lyrics3.Text = PrestoSDK.PrestoService.Player.CurrentMusic.Title + " - " + PrestoSDK.PrestoService.Player.CurrentMusic.Artist.Name;
                currentLyricIndex = 0;
            }

            // FF 가사 이동
            for ( ; currentLyricIndex < time.Count && currentPlayTime > time[currentLyricIndex].TotalMilliseconds;) {

                // 마지막 가사 처리
                if (currentLyricIndex == time.Count-1) {
                    text_lyrics4.Text = "";
                    text_lyrics.Text = list[currentLyricIndex - 2];
                    text_lyrics2.Text = list[currentLyricIndex - 1];
                    text_lyrics3.Text = list[currentLyricIndex];
                    break;
                }

                //이전 가사가 없는 초반 가사들 처리
                if (currentLyricIndex - 2 >= 0)
                    text_lyrics.Text = list[currentLyricIndex - 2];
                    
                if(currentLyricIndex -1 >= 0)
                    text_lyrics2.Text = list[currentLyricIndex - 1];

                // 건너뛰는 가사 있는지 확인
                if (currentPlayTime < time[currentLyricIndex+1].TotalMilliseconds) {
                    text_lyrics3.Text = list[currentLyricIndex++];
                    //현재 가사 +1,2 출력
                    text_lyrics4.Text = list[currentLyricIndex];
                    //5번째라인에 마지막줄 가사 처리
                    if(currentLyricIndex+1 < time.Count)
                        text_lyrics5.Text = list[currentLyricIndex+1];
                    else
                        text_lyrics5.Text = "";
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

        private void TopCheck_Checked(object sender, RoutedEventArgs e)
        {
                lyricsWindow.Topmost = true;
        }

        private void TopCheck_Unchecked(object sender, RoutedEventArgs e)
        {
                lyricsWindow.Topmost = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
