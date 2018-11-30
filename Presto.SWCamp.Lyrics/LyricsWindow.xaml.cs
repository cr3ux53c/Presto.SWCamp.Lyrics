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
using System.Resources;
using System.Reflection;

namespace Presto.SWCamp.Lyrics {
    public partial class LyricsWindow : Window {
        private const int WINDOW_HEIGHT_NORMAL = 230;
        private const int WINDOW_HEIGHT_FULL_LYRICS = 500;
        private const int WINDOW_HEIGHT_MULTILINE = 470;

        private StickyWindow _stickyWindow;
        private List<LyricsPair> timeline;
        private List<TextBlock> lyricsTextBlock = new List<TextBlock>();
        private DispatcherTimer timer;
        private int currentLyricIndex;
        private bool isFullLyricsViewer = false;
        private bool isMultilineLyrics;
        private bool MultilineLyrics_Check=false;
        private bool isLyricsFileExist=false;
        private double Origin_WindowTop;

        private class LyricsPair {
            public TimeSpan timeline;
            public string lyrics;
            public LyricsPair(TimeSpan timeline, string lyrics) {
                this.timeline = timeline;
                this.lyrics = lyrics;
            }
        }

        public LyricsWindow() {
            InitializeComponent();

            this.Loaded += OnLoaded;

            //텍스트블럭 색깔지정-> 3번이 현재가사, 1,2번 이전가사, 4,5번 다음가사
            //가사별 투명도 별도 지정(현재 가사에 집중됨)
            lyricsTextBlock.Add(text_lyrics);
            lyricsTextBlock.Add(text_lyrics2);
            lyricsTextBlock.Add(text_lyrics3);
            lyricsTextBlock.Add(text_lyrics4);
            lyricsTextBlock.Add(text_lyrics5);

            //텍스트블럭 색깔지정-> 3번이 현재가사, 1,2번 이전가사, 4,5번 다음가사
            foreach (var lyrics in lyricsTextBlock)
                lyrics.Foreground = new SolidColorBrush(Colors.GhostWhite);
            lyricsTextBlock[2].Foreground = new SolidColorBrush(Colors.Chocolate);

            PrestoSDK.PrestoService.Player.StreamChanged += Player_StreamChanged;
            button_full_lyrics.Click += Button_full_lyrics_Click;
        }

        void OnLoaded(object sender, RoutedEventArgs e) {
            _stickyWindow = new StickyWindow(this) {
                StickToScreen = true, StickToOther = true, StickOnResize = true, StickOnMove = true
            };
        }

        private void Player_StreamChanged(object sender, Common.StreamChangedEventArgs e) {
            isMultilineLyrics = false;
            String[] lyricsRaw;
            String filePath = PrestoSDK.PrestoService.Player.CurrentMusic.Path;
            
            //앞의 노래 가사 지우기
            foreach (var lyrics in lyricsTextBlock)
                lyrics.Text = "";
            if (timer != null)
                timer.Stop();
            timeline = new List<LyricsPair>();
            text_full_lyrics.Text = "";
            //전체 가사 모드 해제
            if (isFullLyricsViewer)
                Button_full_lyrics_Click(null, null);

            //윈도우폼 배경을 현재 앨범 이미지로 변경
            String albumPicture = PrestoSDK.PrestoService.Player.CurrentMusic.Album.Picture;
            ImageBrush BackPicture = new ImageBrush(new BitmapImage(new Uri(albumPicture))) {
                Opacity = 0.2, Stretch = Stretch.UniformToFill
            };
            this.Background = BackPicture;

            // 가사 파일 읽기
            currentLyricIndex = 0;
            try {
                lyricsRaw = File.ReadAllLines(filePath.Substring(0, filePath.Length-4) + ".lrc");
                isLyricsFileExist = true;
            } catch (System.IO.FileNotFoundException) {
                isMultilineLyrics = false;
                isLyricsFileExist = false;
                lyricsRaw = new string[] { };
            } finally {
                if (isLyricsFileExist) {
                    foreach (var lyrics in lyricsTextBlock)
                        lyrics.Visibility = Visibility.Visible;
                } else { // 가사 파일 존재하지 않을 시 컨트롤 숨기기
                    this.Title = "Presto Floating Lyrics";
                    foreach (var lyrics in lyricsTextBlock)
                        lyrics.Visibility = Visibility.Collapsed;
                    lyricsTextBlock[1].Text = "가사 파일 없음";
                    lyricsTextBlock[1].Visibility = Visibility.Visible;
                    if (!isFullLyricsViewer)
                        button_full_lyrics.Visibility = Visibility.Hidden;
                }
            }

            // 가사 파싱
            foreach (var line in lyricsRaw) {
                if (line[1] > 64) { // ASCII.64 == @
                    continue;
                }
                int threshold = line.IndexOf(']');
                
                // 플레이 타임 추출
                String timeRaw = line.Substring(1, threshold-1);
                var timeSplited = timeRaw.Split(new char[] { ':', '.' });
                TimeSpan timeSpan = new TimeSpan(0, 0, int.Parse(timeSplited[0])
                                            , int.Parse(timeSplited[1])
                                            , int.Parse(timeSplited[2]) * 10);
                // 가사 추출
                if (timeline.Count > 0 && timeline[timeline.Count-1].timeline.TotalMilliseconds == timeSpan.TotalMilliseconds) {
                    timeline[timeline.Count -1].lyrics += "\n" + line.Substring(threshold + 1);
                    isMultilineLyrics = true;
                } else {
                    timeline.Add(new LyricsPair(timeSpan, line.Substring(threshold + 1)));
                }
            }

            //전체 가사 파싱
            foreach (var line in lyricsRaw) {
                if (line[1] > 64) { // ASCII.64 == @
                    text_full_lyrics.Text = "";
                    continue;
                }
                text_full_lyrics.Text += line.Substring(line.IndexOf(']') + 1) + "\n";
            }


            //다국어 가사이면 창크기를 늘리고 위치를 위로 조금 올림
            if (!isFullLyricsViewer) {
                if (isMultilineLyrics) {
                    lyricsWindow.Height = WINDOW_HEIGHT_MULTILINE;
                    if (lyricsWindow.Top + lyricsWindow.Height > System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height)
                    {
                        Origin_WindowTop = lyricsWindow.Top;
                        lyricsWindow.Top -= WINDOW_HEIGHT_NORMAL;
                        MultilineLyrics_Check = true;
                    }
                } else {
                    lyricsWindow.Height = WINDOW_HEIGHT_NORMAL;
                    if (MultilineLyrics_Check)
                    {
                        lyricsWindow.Top = Origin_WindowTop;
                        MultilineLyrics_Check = false;
                    }
                }
            }

            // 타이밍
            if (isLyricsFileExist) {
                timer = new DispatcherTimer {
                    Interval = TimeSpan.FromMilliseconds(10)
                };
                timer.Tick += Timer_Tick;
                timer.Start();
            }
        }

        private void Timer_Tick(object sender, EventArgs e) {
            int currentPlayTime = (int)PrestoSDK.PrestoService.Player.Position;

            this.Title = PrestoSDK.PrestoService.Player.CurrentMusic.Title + " - " + PrestoSDK.PrestoService.Player.CurrentMusic.Artist.Name; ;
            // 도입부 '노래 - 가수명' 출력
            if (currentPlayTime < timeline[0].timeline.TotalMilliseconds-1000*5) {
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
            if (isLyricsFileExist)
                button_full_lyrics.Visibility = Visibility.Visible;
            TopCheck.Visibility = Visibility.Visible;
            leaveThreshold = false;
            
        }

        protected override void OnMouseLeave(MouseEventArgs e) {
            base.OnMouseLeave(e);
            if (dispatcher == null) {
                dispatcher = new DispatcherTimer {
                    Interval = TimeSpan.FromMilliseconds(2000)
                };
                dispatcher.Tick += Timer_Tick_Sticky;
                dispatcher.Start();
            }
            button_full_lyrics.Visibility = Visibility.Hidden;
            TopCheck.Visibility = Visibility.Hidden;
            leaveThreshold = true;
        }

        private void Timer_Tick_Sticky(object sender, EventArgs e) {
            dispatcher.Stop();
            dispatcher = null;
            if (leaveThreshold) {
                this.WindowStyle = WindowStyle.None;
            }
        }

        // TopMost 구현

        private void TopCheck_Checked(object sender, RoutedEventArgs e){
            lyricsWindow.Topmost = true;
        }

        private void TopCheck_Unchecked(object sender, RoutedEventArgs e){
            lyricsWindow.Topmost = false;
        }


        // 전체 가사 출력
        DispatcherTimer timerSlidingWindow;
        int SlidingWindowSize = 40;
        void Button_full_lyrics_Click(object sender, RoutedEventArgs e) {
            if (timerSlidingWindow == null || (timerSlidingWindow != null && timerSlidingWindow.IsEnabled == false)) {
                timerSlidingWindow = new DispatcherTimer {
                    Interval = TimeSpan.FromMilliseconds(2)
                };
                timerSlidingWindow.Tick += Timer_Tick_Sliding_Window;
                timerSlidingWindow.Start();
            }
        }

        private void Timer_Tick_Sliding_Window(object sender, EventArgs e) {
            if (SlidingWindowSize >= 5) SlidingWindowSize -= 2;

            if (button_full_lyrics.Content.Equals("∨")) { // TO INCREASE
                // runOnce
                if (!isFullLyricsViewer) { 
                    isFullLyricsViewer = true;
                    if (SlidingWindowSize != 100) {
                        foreach (var lyrics in lyricsTextBlock) {
                            lyrics.Visibility = Visibility.Collapsed;
                        }
                        scroll_full_lyrics.Visibility = Visibility.Visible;
                        scroll_full_lyrics.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                    }
                }

                this.Height += SlidingWindowSize;

                if (this.Height >= WINDOW_HEIGHT_FULL_LYRICS) {
                    this.Height = WINDOW_HEIGHT_FULL_LYRICS;
                    timerSlidingWindow.Stop();
                    button_full_lyrics.Content = "∧";
                    SlidingWindowSize = 32;
                    isFullLyricsViewer = true;
                }
            } else {                                       // TO DECRESE
                // runOnce
                if (isFullLyricsViewer) {
                    isFullLyricsViewer = false;
                    if (SlidingWindowSize != 100) {
                        foreach (var lyrics in lyricsTextBlock) {
                            lyrics.Visibility = Visibility.Visible;
                        }
                        scroll_full_lyrics.Visibility = Visibility.Collapsed;
                        scroll_full_lyrics.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                    }
                }

                this.Height -= SlidingWindowSize;
                
                if (this.Height <= (isMultilineLyrics ? WINDOW_HEIGHT_MULTILINE : WINDOW_HEIGHT_NORMAL)) {
                    this.Height = (isMultilineLyrics ? WINDOW_HEIGHT_MULTILINE : WINDOW_HEIGHT_NORMAL);
                    timerSlidingWindow.Stop();
                    button_full_lyrics.Content = "∨";
                    SlidingWindowSize = 32;
                    isFullLyricsViewer = false;
                }
            }
            
            timerSlidingWindow.Interval = TimeSpan.FromMilliseconds(timerSlidingWindow.Interval.TotalMilliseconds + 1);
        }
    }
}
