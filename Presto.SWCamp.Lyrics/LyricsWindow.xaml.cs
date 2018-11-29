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
using Blue.Windows;
using StickyWindowLibrary;
using System.Drawing;

namespace Presto.SWCamp.Lyrics {
    public partial class LyricsWindow : Window {
        private StickyWindow _stickyWindow;
        String[] lyricsRaw;
        DispatcherTimer timer;

        private class LyricsPair {
            public TimeSpan timeline;
            public string lyrics;
            public LyricsPair(TimeSpan timeline, string lyrics) {
                this.timeline = timeline;
                this.lyrics = lyrics;
            }
        }
        List<LyricsPair> timeline;

        List<TextBlock> lyricsTextBlock = new List<TextBlock>();
        int currentLyricIndex;
        double OriginTop;

        public LyricsWindow() {
            InitializeComponent();
            this.Loaded += onLoaded;


            //텍스트블럭 색깔지정-> 3번이 현재가사, 1,2번 이전가사, 4,5번 다음가사
            //가사별 투명도 별도 지정(현재 가사에 집중됨)
            lyricsTextBlock.Add(text_lyrics);
            lyricsTextBlock.Add(text_lyrics2);
            lyricsTextBlock.Add(text_lyrics3);
            lyricsTextBlock.Add(text_lyrics4);
            lyricsTextBlock.Add(text_lyrics5);

            //텍스트블럭 색깔지정-> 3번이 현재가사, 1,2번 이전가사, 4,5번 다음가사
            foreach (TextBlock lyrics in lyricsTextBlock)
                lyrics.Foreground = new SolidColorBrush(Colors.GhostWhite);
            lyricsTextBlock[0].Foreground.Opacity = 0.3;
            lyricsTextBlock[1].Foreground.Opacity = 0.6;
            lyricsTextBlock[2].Foreground = new SolidColorBrush(Colors.Chocolate);
            lyricsTextBlock[3].Foreground.Opacity = 0.6;
            lyricsTextBlock[4].Foreground.Opacity = 0.3;

            PrestoSDK.PrestoService.Player.StreamChanged += Player_StreamChanged;
        }

        void onLoaded(object sender, RoutedEventArgs e) {
            _stickyWindow = new StickyWindow(this);
            _stickyWindow.StickToScreen = true;
            _stickyWindow.StickToOther = true;
            _stickyWindow.StickOnResize = true;
            _stickyWindow.StickOnMove = true;
            OriginTop = lyricsWindow.Top;
        }

        private void Player_StreamChanged(object sender, Common.StreamChangedEventArgs e) {
            
            String filePath = PrestoSDK.PrestoService.Player.CurrentMusic.Path;

            
            //앞의 노래 가사 지우기
            foreach (TextBlock lyrics in lyricsTextBlock)
                lyrics.Text = "";
            
            //윈도우폼 배경을 현재 앨범 이미지로 변경
            String albumPicture = PrestoSDK.PrestoService.Player.CurrentMusic.Album.Picture;
            ImageBrush BackPicture = new ImageBrush(new BitmapImage(new Uri(albumPicture)));

            BackPicture.Opacity = 0.2;
            BackPicture.Stretch = Stretch.UniformToFill;
            this.Background = BackPicture;

            // 가사 파일 읽기
            currentLyricIndex = 0;
            try {
                lyricsRaw = File.ReadAllLines(filePath.Substring(0, filePath.Length-4) + ".lrc");
            }catch (System.IO.FileNotFoundException) {
                this.Title = "Presto Floating Lyrics";
                foreach (TextBlock lyrics in lyricsTextBlock)
                    lyrics.Text = "";
                lyricsTextBlock[1].Text = "가사 파일 없음";
                if (timer != null)
                    timer.Stop();
                return;
            }

            timeline = new List<LyricsPair>();

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
                if (timeline.Count > 0 && timeline[timeline.Count-1].timeline.TotalMilliseconds == timeSpan.TotalMilliseconds) {
                    timeline[timeline.Count -1].lyrics += "\n" + line.Substring(threshold + 1);
                } else {
                    timeline.Add(new LyricsPair(timeSpan, line.Substring(threshold + 1)));
                }
            }

            //다국어 가사이면 창크기를 늘리고 위치를 위로 조금 올림
            if (timeline[3].lyrics.Contains("\n")) {
                lyricsWindow.Height = 450;
                lyricsWindow.Top = OriginTop;
                lyricsWindow.Top -= 100;
            } else {
                lyricsWindow.Height = 200;
                lyricsWindow.Top = OriginTop;
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

            this.Title = PrestoSDK.PrestoService.Player.CurrentMusic.Title + " - " + PrestoSDK.PrestoService.Player.CurrentMusic.Artist.Name; ;
            // 도입부 '노래 - 가수명' 출력
            if (currentPlayTime < timeline[0].timeline.TotalMilliseconds-1000*10) {
                text_lyrics3.Text = PrestoSDK.PrestoService.Player.CurrentMusic.Title + " - " + PrestoSDK.PrestoService.Player.CurrentMusic.Artist.Name;
                currentLyricIndex = 0;
            }

            // FF 가사 이동
            for ( ; currentLyricIndex < timeline.Count && currentPlayTime > timeline[currentLyricIndex].timeline.TotalMilliseconds;) {

                // 마지막 가사 처리
                if (currentLyricIndex == timeline.Count-1) {
                    text_lyrics4.Text = "";
                    text_lyrics.Text = timeline[currentLyricIndex - 2].lyrics;
                    text_lyrics2.Text = timeline[currentLyricIndex - 1].lyrics;
                    text_lyrics3.Text = timeline[currentLyricIndex].lyrics;
                    break;
                }

                //이전 가사가 없는 초반 가사들 처리
                if (currentLyricIndex - 2 >= 0)
                    text_lyrics.Text = timeline[currentLyricIndex - 2].lyrics;
                    
                if(currentLyricIndex -1 >= 0)
                    text_lyrics2.Text = timeline[currentLyricIndex - 1].lyrics;

                // 건너뛰는 가사 있는지 확인
                if (currentPlayTime < timeline[currentLyricIndex+1].timeline.TotalMilliseconds) {
                    text_lyrics3.Text = timeline[currentLyricIndex++].lyrics;
                    //현재 가사 +1,2 출력
                    text_lyrics4.Text = timeline[currentLyricIndex].lyrics;
                    //5번째라인에 마지막줄 가사 처리
                    if(currentLyricIndex+1 < timeline.Count)
                        text_lyrics5.Text = timeline[currentLyricIndex+1].lyrics;
                    else
                        text_lyrics5.Text = "";
                    return;
                }

                currentLyricIndex++;
            }

            // FR 가사 이동
            for (; currentLyricIndex > 0 && currentPlayTime < timeline[currentLyricIndex].timeline.TotalMilliseconds;) {
                currentLyricIndex--;
            }
        }

        /// <summary>
        /// 커서 기반 동적 윈도우 스타일링
        /// 
        /// StickyWindowLibrary에서 WindowStyle == NONE 일 때 작동하지 않음.
        /// </summary>

        //protected override void OnMouseMove(MouseEventArgs e) {
        //    if (MouseButtonState.Pressed == Mouse.LeftButton) {
        //        this.DragMove();
        //    }
        //}

        private DispatcherTimer dispatcher;

        private bool leaveThreshold = false;

        protected override void OnMouseEnter(MouseEventArgs e) {
            base.OnMouseEnter(e);
            this.WindowStyle = WindowStyle.ToolWindow;
            TopCheck.Visibility = Visibility.Visible;
            leaveThreshold = false;
            
        }

        protected override void OnMouseLeave(MouseEventArgs e) {
            base.OnMouseLeave(e);
            if (dispatcher == null) {
                dispatcher = new DispatcherTimer();
                dispatcher.Interval = TimeSpan.FromMilliseconds(2000);
                dispatcher.Tick += Timer_Tick_Sticky;
                dispatcher.Start();
            }
            leaveThreshold = true;
        }

        private void Timer_Tick_Sticky(object sender, EventArgs e) {
            dispatcher.Stop();
            dispatcher = null;
            if (leaveThreshold) {
                this.WindowStyle = WindowStyle.None;
                TopCheck.Visibility = Visibility.Hidden;
            }
        }

        // TopMost 구현

        private void TopCheck_Checked(object sender, RoutedEventArgs e){
            lyricsWindow.Topmost = true;
        }

        private void TopCheck_Unchecked(object sender, RoutedEventArgs e){
            lyricsWindow.Topmost = false;
        }

    }
}
