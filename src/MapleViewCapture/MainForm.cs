using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Drawing.Imaging;

namespace MapleViewCapture
{
    public partial class MainForm : Form
    {
        // Win32 API 함수들
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hGdiObj);

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hDestDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, uint dwRop);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // 상수
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SRCCOPY = 0x00CC0020;

        // 멤버 변수
        private IntPtr gameWindowHandle = IntPtr.Zero;
        private System.Windows.Forms.Timer captureTimer = null!;
        private PictureBox previewPictureBox = null!;
        private Button findGameButton = null!;
        private Button startCaptureButton = null!;
        private Label statusLabel = null!;
        private ComboBox windowComboBox = null!;
        private List<WindowInfo> availableWindows = null!;
        private int captureCount = 0;
        private Button directXButton = null!;
        private System.Diagnostics.Stopwatch performanceTimer = new System.Diagnostics.Stopwatch();
        private Label performanceLabel = null!;
        private Button roiModeButton = null!;
        private Button saveRoiButton = null!;
        private bool isRoiMode = false;
        private Rectangle currentRoi;
        private bool isDrawing = false;
        private Point startPoint;
        private Button loadRoiButton = null!;
        private Button roiCaptureButton = null!;
        private Dictionary<string, Rectangle> savedRois = new Dictionary<string, Rectangle>();
        private List<Form> roiWindows = new List<Form>();
        private Bitmap? lastCapturedImage = null;
        private Size actualImageSize;
        private Rectangle displayRect;
        private bool isTemplateMode = false;
        private bool isMatchingMode = false;
        private Dictionary<string, Bitmap> templates = new Dictionary<string, Bitmap>();
        private Dictionary<string, OpenCvSharp.Mat> preconvertedTemplates = new Dictionary<string, OpenCvSharp.Mat>();
        private Dictionary<string, List<string>> roiTemplateMap = new Dictionary<string, List<string>>();
        private Button templateModeButton = null!;
        private Button saveTemplateButton = null!;
        private Button startMatchingButton = null!;
        private ListBox debugListBox = null!;
        private Button clearLogButton = null!;
        private TrackBar thresholdTrackBar = null!;
        private Label thresholdLabel = null!;
        private double matchingThreshold = 0.8;
        private DateTime lastLogTime = DateTime.MinValue;

        // HP/MP 임계값 설정 관련
        private float hpThreshold = 0.3f; // 기본 30%
        private float mpThreshold = 0.2f; // 기본 20%

        // Status Panel
        private StatusPanel? statusPanel = null;

        // 생성자 수정
        public MainForm()
        {
            try
            {
                InitializeComponent();
                availableWindows = new List<WindowInfo>();
                AddDebugLog("프로그램 시작");
                
                // 시작 시 자동으로 템플릿 로드
                LoadAllTemplates();
                if (templates.Count > 0)
                {
                    AddDebugLog($"시작 시 {templates.Count}개 템플릿 자동 로드됨");
                    startMatchingButton.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"초기화 오류: {ex.Message}", "오류");
            }
        }

        private void AddDebugLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logMessage = $"[{timestamp}] {message}";
            
            if (debugListBox.InvokeRequired)
            {
                debugListBox.Invoke(new Action(() => {
                    debugListBox.Items.Add(logMessage);
                    // 자동 스크롤 (최근 메시지가 보이도록)
                    if (debugListBox.Items.Count > 0)
                        debugListBox.TopIndex = debugListBox.Items.Count - 1;
                    
                    // 100개 이상이면 오래된 것 삭제
                    if (debugListBox.Items.Count > 100)
                        debugListBox.Items.RemoveAt(0);
                }));
            }
            else
            {
                debugListBox.Items.Add(logMessage);
                if (debugListBox.Items.Count > 0)
                    debugListBox.TopIndex = debugListBox.Items.Count - 1;
                
                if (debugListBox.Items.Count > 100)
                    debugListBox.Items.RemoveAt(0);
            }
        }

        // 창 정보 클래스
        public class WindowInfo
        {
            public IntPtr Handle { get; set; }
            public string Title { get; set; } = string.Empty;
            
            public override string ToString()
            {
                return Title;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "메이플랜드 라이브뷰 캡처";
            this.Size = new Size(1400, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // UI 컨트롤 생성
            windowComboBox = new ComboBox
            {
                Location = new Point(20, 20),
                Size = new Size(300, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            findGameButton = new Button
            {
                Text = "창 목록 새로고침",
                Location = new Point(340, 18),
                Size = new Size(120, 30)
            };
            findGameButton.Click += FindGameButton_Click;

            startCaptureButton = new Button
            {
                Text = "캡처 시작",
                Location = new Point(480, 18),
                Size = new Size(100, 30),
                Enabled = false
            };
            startCaptureButton.Click += StartCaptureButton_Click;

            directXButton = new Button
            {
                Text = "화면좌표 캡처",
                Location = new Point(590, 18),
                Size = new Size(100, 30)
            };
            directXButton.Click += DirectXButton_Click;

            roiModeButton = new Button
            {
                Text = "ROI 설정",
                Location = new Point(700, 18),
                Size = new Size(80, 30),
                BackColor = Color.LightBlue
            };
            roiModeButton.Click += RoiModeButton_Click;

            saveRoiButton = new Button
            {
                Text = "ROI 저장",
                Location = new Point(790, 18),
                Size = new Size(80, 30),
                Enabled = false
            };
            saveRoiButton.Click += SaveRoiButton_Click;

            // 두 번째 줄 버튼들
            loadRoiButton = new Button
            {
                Text = "ROI 로드",
                Location = new Point(20, 50),
                Size = new Size(80, 25),
                BackColor = Color.LightGreen
            };
            loadRoiButton.Click += LoadRoiButton_Click;
            loadRoiButton.Click += LoadRoiButton_Click;

            roiCaptureButton = new Button
            {
                Text = "ROI 캡처 시작",
                Location = new Point(110, 50),
                Size = new Size(100, 25),
                Enabled = false
            };
            roiCaptureButton.Click += RoiCaptureButton_Click;

            // Status Panel 버튼
            Button statusPanelButton = new Button
            {
                Text = "Status Panel",
                Location = new Point(220, 50),
                Size = new Size(100, 25),
                BackColor = Color.LightCyan
            };
            statusPanelButton.Click += StatusPanelButton_Click;

            // 세 번째 줄 버튼들 (템플릿 관련)
            templateModeButton = new Button
            {
                Text = "템플릿 모드",
                Location = new Point(230, 50),
                Size = new Size(90, 25),
                BackColor = Color.LightYellow
            };
            templateModeButton.Click += TemplateModeButton_Click;

            saveTemplateButton = new Button
            {
                Text = "템플릿 저장",
                Location = new Point(330, 50),
                Size = new Size(90, 25),
                Enabled = false
            };
            saveTemplateButton.Click += SaveTemplateButton_Click;

            startMatchingButton = new Button
            {
                Text = "매칭 시작",
                Location = new Point(430, 50),
                Size = new Size(90, 25),
                Enabled = false,
                BackColor = Color.LightPink
            };
            startMatchingButton.Click += StartMatchingButton_Click;

            Button loadTemplateButton = new Button
            {
                Text = "템플릿 로드",
                Location = new Point(530, 50),
                Size = new Size(90, 25),
                BackColor = Color.LightCyan
            };
            loadTemplateButton.Click += LoadTemplateButton_Click;

            statusLabel = new Label
            {
                Text = "상태: 창을 선택하세요",
                Location = new Point(20, 80),
                Size = new Size(400, 20)
            };

            performanceLabel = new Label
            {
                Text = "성능: 대기중",
                Location = new Point(450, 80),
                Size = new Size(200, 20),
                ForeColor = Color.Blue
            };

            previewPictureBox = new PictureBox
            {
                Location = new Point(20, 110),
                Size = new Size(900, 450),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            
            // ROI 그리기를 위한 이벤트 연결
            previewPictureBox.MouseDown += PreviewPictureBox_MouseDown;
            previewPictureBox.MouseMove += PreviewPictureBox_MouseMove;
            previewPictureBox.MouseUp += PreviewPictureBox_MouseUp;
            previewPictureBox.Paint += PreviewPictureBox_Paint;

            // 디버그 로그 ListBox
            debugListBox = new ListBox
            {
                Location = new Point(940, 110),
                Size = new Size(430, 400),
                Font = new Font("Consolas", 8),
                SelectionMode = SelectionMode.One
            };

            clearLogButton = new Button
            {
                Text = "로그 지우기",
                Location = new Point(940, 520),
                Size = new Size(80, 25)
            };
            clearLogButton.Click += (s, e) => {
                debugListBox.Items.Clear();
                AddDebugLog("로그 지워짐");
            };

            // 임계값 조절 컨트롤
            thresholdLabel = new Label
            {
                Text = $"임계값: {matchingThreshold:F2}",
                Location = new Point(1030, 520),
                Size = new Size(100, 20)
            };

            thresholdTrackBar = new TrackBar
            {
                Location = new Point(1130, 515),
                Size = new Size(150, 30),
                Minimum = 50,
                Maximum = 99,
                Value = (int)(matchingThreshold * 100),
                TickFrequency = 10
            };
            thresholdTrackBar.ValueChanged += (s, e) => {
                matchingThreshold = thresholdTrackBar.Value / 100.0;
                thresholdLabel.Text = $"임계값: {matchingThreshold:F2}";
                AddDebugLog($"임계값 변경: {matchingThreshold:F2}");
            };

            // 타이머 초기화
            captureTimer = new System.Windows.Forms.Timer
            {
                Interval = 100 // 100ms
            };
            captureTimer.Tick += CaptureTimer_Tick;

            // 컨트롤 추가
            this.Controls.Add(windowComboBox);
            this.Controls.Add(findGameButton);
            this.Controls.Add(startCaptureButton);
            this.Controls.Add(directXButton);
            this.Controls.Add(roiModeButton);
            this.Controls.Add(saveRoiButton);
            this.Controls.Add(loadRoiButton);
            this.Controls.Add(roiCaptureButton);
            this.Controls.Add(statusPanelButton);
            this.Controls.Add(templateModeButton);
            this.Controls.Add(saveTemplateButton);
            this.Controls.Add(startMatchingButton);
            this.Controls.Add(loadTemplateButton);
            this.Controls.Add(statusLabel);
            this.Controls.Add(performanceLabel);
            this.Controls.Add(previewPictureBox);
            this.Controls.Add(debugListBox);
            this.Controls.Add(clearLogButton);
            this.Controls.Add(thresholdLabel);
            this.Controls.Add(thresholdTrackBar);

            // 이벤트 연결
            windowComboBox.SelectedIndexChanged += WindowComboBox_SelectedIndexChanged;

            // 시작 시 창 목록 로드
            // RefreshWindowList(); // 일단 주석 처리
        }

        private void FindGameButton_Click(object sender, EventArgs e)
        {
            RefreshWindowList();
        }

        private void RefreshWindowList()
        {
            try
            {
                AddDebugLog("창 목록 새로고침 시작");
                
                availableWindows.Clear();
                
                // DataSource를 null로 설정한 후 Items 조작
                windowComboBox.DataSource = null;
                windowComboBox.Items.Clear();

                // 모든 창 열거
                EnumWindows(new EnumWindowsProc(EnumWindowCallback), IntPtr.Zero);

                if (availableWindows.Count > 0)
                {
                    windowComboBox.DataSource = availableWindows;
                    windowComboBox.DisplayMember = "Title";
                    AddDebugLog($"{availableWindows.Count}개의 창을 찾았습니다");
                    statusLabel.Text = $"상태: {availableWindows.Count}개의 창을 찾았습니다";
                }
                else
                {
                    AddDebugLog("사용 가능한 창이 없습니다");
                    statusLabel.Text = "상태: 사용 가능한 창이 없습니다";
                }
            }
            catch (Exception ex)
            {
                AddDebugLog($"창 목록 새로고침 실패: {ex.Message}");
                statusLabel.Text = $"상태: 새로고침 실패 - {ex.Message}";
            }
        }

        private bool EnumWindowCallback(IntPtr hWnd, IntPtr lParam)
        {
            // 보이는 창만 선택
            if (IsWindowVisible(hWnd))
            {
                System.Text.StringBuilder text = new System.Text.StringBuilder(256);
                GetWindowText(hWnd, text, 256);
                
                string windowTitle = text.ToString();
                
                // 제목이 있고 최소 길이 이상인 창만 추가
                if (!string.IsNullOrEmpty(windowTitle) && windowTitle.Length > 2)
                {
                    availableWindows.Add(new WindowInfo
                    {
                        Handle = hWnd,
                        Title = windowTitle
                    });
                }
            }
            
            return true; // 계속 열거
        }

        private void WindowComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (windowComboBox.SelectedItem is WindowInfo selectedWindow)
            {
                gameWindowHandle = selectedWindow.Handle;
                statusLabel.Text = $"상태: '{selectedWindow.Title}' 창 선택됨";
                startCaptureButton.Enabled = true;
                
                // 선택된 창을 800x600으로 설정
                SetGameWindowSize();
            }
        }

        private void SetGameWindowSize()
        {
            if (gameWindowHandle != IntPtr.Zero)
            {
                // 창을 800x600 크기로 설정 (테두리 포함하여 약간 크게)
                SetWindowPos(gameWindowHandle, IntPtr.Zero, 100, 100, 820, 640, SWP_NOZORDER);
                statusLabel.Text = "상태: 게임 창 크기 조정 완료 (800x600)";
            }
        }

        private void StartCaptureButton_Click(object sender, EventArgs e)
        {
            if (captureTimer.Enabled)
            {
                captureTimer.Stop();
                startCaptureButton.Text = "캡처 시작";
                statusLabel.Text = "상태: 캡처 중지";
            }
            else
            {
                captureTimer.Start();
                startCaptureButton.Text = "캡처 중지";
                statusLabel.Text = "상태: 캡처 중 (100ms 간격)";
            }
        }

        private void CaptureTimer_Tick(object sender, EventArgs e)
        {
            if (gameWindowHandle != IntPtr.Zero)
            {
                performanceTimer.Restart();
                
                captureCount++;
                statusLabel.Text = $"상태: 캡처 중... (#{captureCount})";
                
                // 메인 캡처
                CaptureGameWindow();
                
                // ROI 캡처 (ROI 윈도우가 있을 때)
                CaptureRoiWindows();
                
                performanceTimer.Stop();
                double elapsedMs = performanceTimer.Elapsed.TotalMilliseconds;
                double currentFPS = 1000.0 / elapsedMs;
                performanceLabel.Text = $"성능: {elapsedMs:F1}ms ({currentFPS:F1} FPS)";
                
                // StatusPanel에 성능 정보 업데이트
                if (statusPanel != null && !statusPanel.IsDisposed && statusPanel.Visible)
                {
                    statusPanel.UpdatePerformance(currentFPS, elapsedMs);
                }
            }
        }

        private void CaptureRoiWindows()
        {
            if (roiWindows.Count == 0) return;

            try
            {
                // 전체 창 캡처 (ROI들을 잘라낼 원본)
                var fullCapture = ScreenCapture.CaptureWindow(gameWindowHandle);
                
                if (fullCapture == null) return;

                // 각 ROI 윈도우 업데이트
                foreach (Form roiWindow in roiWindows)
                {
                    if (roiWindow.IsDisposed) continue;

                    dynamic roiData = roiWindow.Tag;
                    string roiName = roiData.Name;
                    Rectangle roiRect = roiData.Rect;
                    PictureBox pictureBox = roiData.PictureBox;

                    // ROI 영역 잘라내기
                    if (roiRect.Right <= fullCapture.Width && roiRect.Bottom <= fullCapture.Height)
                    {
                        Rectangle cropRect = new Rectangle(
                            roiRect.X, roiRect.Y, 
                            Math.Min(roiRect.Width, fullCapture.Width - roiRect.X),
                            Math.Min(roiRect.Height, fullCapture.Height - roiRect.Y)
                        );

                        Bitmap roiBitmap = new Bitmap(cropRect.Width, cropRect.Height);
                        using (Graphics g = Graphics.FromImage(roiBitmap))
                        {
                            g.DrawImage(fullCapture, 0, 0, cropRect, GraphicsUnit.Pixel);
                        }

                        // 템플릿 매칭 수행 (매칭 모드일 때만)
                        if (isMatchingMode)
                        {
                            roiBitmap = PerformTemplateMatching(roiBitmap, roiName);
                        }

                        // UI 업데이트 (UI 스레드에서)
                        if (pictureBox.InvokeRequired)
                        {
                            pictureBox.Invoke(new Action(() => {
                                if (pictureBox.Image != null)
                                    pictureBox.Image.Dispose();
                                pictureBox.Image = roiBitmap;
                            }));
                        }
                        else
                        {
                            if (pictureBox.Image != null)
                                pictureBox.Image.Dispose();
                            pictureBox.Image = roiBitmap;
                        }
                    }
                }

                fullCapture.Dispose();
            }
            catch (Exception ex)
            {
                // ROI 캡처 오류는 조용히 처리
                AddDebugLog($"ROI 캡처 오류: {ex.Message}");
            }
        }

        private Bitmap PerformTemplateMatching(Bitmap sourceImage, string roiName)
        {
            try
            {
                // HP/MP 바 감지 처리 (순수 ROI 방식)
                if (roiName == "hp_bar")
                {
                    return PerformHPMPDetection(sourceImage, roiName, true);
                }
                else if (roiName == "mp_bar")
                {
                    return PerformHPMPDetection(sourceImage, roiName, false);
                }

                // 결과 이미지를 그릴 Graphics 생성
                Bitmap resultImage = new Bitmap(sourceImage);
                using (Graphics g = Graphics.FromImage(resultImage))
                {
                    // 해당 ROI에 맞는 템플릿들만 매칭
                    if (roiTemplateMap.ContainsKey(roiName))
                    {
                        var templatesForRoi = roiTemplateMap[roiName];
                        
                        if (templatesForRoi.Count == 0)
                        {
                            // 3초마다 한 번만 로그
                            if (DateTime.Now.Subtract(lastLogTime).TotalSeconds >= 3)
                            {
                                AddDebugLog($"ROI '{roiName}' 에 매핑된 템플릿이 없음");
                                lastLogTime = DateTime.Now;
                            }
                            return resultImage;
                        }
                        
                        foreach (string templateName in templatesForRoi)
                        {
                            if (templates.ContainsKey(templateName))
                            {
                                try
                                {
                                    var matchResult = TemplateMatching.FindTemplate(
                                        sourceImage, 
                                        templates[templateName], 
                                        matchingThreshold
                                    );

                                    if (matchResult.IsMatch)
                                    {
                                        // 바운딩박스 그리기
                                        Color boxColor = GetTemplateColor(templateName);
                                        using (Pen pen = new Pen(boxColor, 2))
                                        {
                                            g.DrawRectangle(pen, matchResult.BoundingBox);
                                        }

                                        // 중심점 표시
                                        using (Brush brush = new SolidBrush(boxColor))
                                        {
                                            int centerSize = 4;
                                            g.FillEllipse(brush, 
                                                matchResult.CenterPoint.X - centerSize/2,
                                                matchResult.CenterPoint.Y - centerSize/2,
                                                centerSize, centerSize);
                                        }

                                        // 템플릿 이름과 신뢰도 표시
                                        string info = $"{templateName}\n{matchResult.Confidence:F2}";
                                        using (Brush textBrush = new SolidBrush(boxColor))
                                        {
                                            g.DrawString(info, new Font("Arial", 8), textBrush,
                                                matchResult.BoundingBox.X, matchResult.BoundingBox.Y - 25);
                                        }

                                        // 좌표 로그 출력 (3초마다만)
                                        if (DateTime.Now.Subtract(lastLogTime).TotalSeconds >= 3)
                                        {
                                            AddDebugLog($"🎯 [{roiName}] {templateName} 발견! " +
                                                      $"중심점:({matchResult.CenterPoint.X},{matchResult.CenterPoint.Y}) " +
                                                      $"신뢰도:{matchResult.Confidence:F2}");
                                            lastLogTime = DateTime.Now;
                                        }
                                    }
                                    else
                                    {
                                        // 매칭 실패 디버깅 (10초마다만)
                                        if (DateTime.Now.Subtract(lastLogTime).TotalSeconds >= 10)
                                        {
                                            AddDebugLog($"매칭 실패: [{roiName}] {templateName} - 신뢰도:{matchResult.Confidence:F2} < 임계값:{matchingThreshold:F2}");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    AddDebugLog($"❌ 템플릿 매칭 오류 ({templateName}): {ex.Message}");
                                }
                            }
                            else
                            {
                                AddDebugLog($"⚠️ 템플릿 '{templateName}' 이 메모리에 없음");
                            }
                        }
                    }
                    else
                    {
                        // 3초마다 한 번만 로그
                        if (DateTime.Now.Subtract(lastLogTime).TotalSeconds >= 3)
                        {
                            AddDebugLog($"ROI '{roiName}' 이 매핑 테이블에 없음");
                            lastLogTime = DateTime.Now;
                        }
                    }
                }

                return resultImage;
            }
            catch (Exception ex)
            {
                AddDebugLog($"❌ 템플릿 매칭 전체 오류: {ex.Message}");
                return sourceImage;
            }
        }

        private Bitmap PerformHPMPDetection(Bitmap sourceImage, string roiName, bool isHP)
        {
            try
            {
                // HP/MP 감지 수행
                HPMPDetector.StatusResult result = isHP ? 
                    HPMPDetector.DetectHP(sourceImage, hpThreshold) :
                    HPMPDetector.DetectMP(sourceImage, mpThreshold);

                // 결과 이미지 생성
                Bitmap resultImage = new Bitmap(sourceImage);
                using (Graphics g = Graphics.FromImage(resultImage))
                {
                    // 상태 정보 표시
                    using (Brush textBrush = new SolidBrush(result.StatusColor))
                    using (Font font = new Font("Arial", 10, FontStyle.Bold))
                    {
                        string statusText = $"{result.Status}\n{(result.IsLow ? "⚠️ 위험!" : "✅ 안전")}";
                        g.DrawString(statusText, font, textBrush, 5, 5);
                    }

                    // 바 길이 시각화
                    int barHeight = 4;
                    int barY = sourceImage.Height - barHeight - 2;
                    
                    // 배경 바 (회색)
                    using (Brush bgBrush = new SolidBrush(Color.Gray))
                    {
                        g.FillRectangle(bgBrush, 0, barY, sourceImage.Width, barHeight);
                    }
                    
                    // 실제 바 (색상)
                    int actualWidth = (int)(sourceImage.Width * result.Ratio);
                    using (Brush statusBrush = new SolidBrush(result.StatusColor))
                    {
                        g.FillRectangle(statusBrush, 0, barY, actualWidth, barHeight);
                    }
                }

                // 위험 상태일 때 로그 출력 (3초마다)
                if (result.IsLow && DateTime.Now.Subtract(lastLogTime).TotalSeconds >= 3)
                {
                    string type = isHP ? "HP" : "MP";
                    AddDebugLog($"⚠️ {type} 위험! {result.Ratio:P0} (임계값: {(isHP ? hpThreshold : mpThreshold):P0})");
                    lastLogTime = DateTime.Now;
                }

                // Status Panel에 결과 전달
                if (statusPanel != null && !statusPanel.IsDisposed && statusPanel.Visible)
                {
                    if (isHP)
                    {
                        statusPanel.UpdateHP(result);
                    }
                    else
                    {
                        statusPanel.UpdateMP(result);
                    }
                }

                return resultImage;
            }
            catch (Exception ex)
            {
                AddDebugLog($"❌ {roiName} 감지 오류: {ex.Message}");
                return sourceImage;
            }
        }

        private Color GetTemplateColor(string templateName)
        {
            // 템플릿 타입에 따라 색상 구분
            if (templateName.ToLower().Contains("player") || templateName.ToLower().Contains("character"))
                return Color.Green;
            else if (templateName.ToLower().Contains("monster"))
                return Color.Red;
            else if (templateName.ToLower().Contains("minimap"))
                return Color.Blue;
            else
                return Color.Yellow;
        }

        private void CaptureGameWindow()
        {
            try
            {
                // 방법 1: ScreenCapture.CopyFromScreen 시도
                if (TryScreenCapture())
                    return;
                
                // 방법 2: BitBlt 시도
                if (CaptureWithBitBlt())
                    return;
                
                // 방법 3: PrintWindow 시도
                if (CaptureWithPrintWindow())
                    return;
                    
                statusLabel.Text = "상태: 모든 캡처 방법 실패";
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"상태: 캡처 오류 - {ex.Message}";
            }
        }

        private bool TryScreenCapture()
        {
            try
            {
                var startTime = System.Diagnostics.Stopwatch.StartNew();
                
                // 창의 화면상 위치에서 직접 캡처
                var bitmap = ScreenCapture.CaptureWindow(gameWindowHandle);
                
                startTime.Stop();
                double captureTime = startTime.Elapsed.TotalMilliseconds;
                
                if (bitmap != null)
                {
                    if (previewPictureBox.Image != null)
                        previewPictureBox.Image.Dispose();
                    
                    previewPictureBox.Image = bitmap;
                    
                    // 실제 이미지 크기와 표시 영역 계산
                    lastCapturedImage = bitmap;
                    actualImageSize = bitmap.Size;
                    CalculateDisplayRect();
                    
                    // 상세 성능 정보 표시
                    performanceLabel.Text = $"캡처: {captureTime:F1}ms";
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private void CalculateDisplayRect()
        {
            if (lastCapturedImage == null) return;

            // PictureBox에서 실제로 이미지가 표시되는 영역 계산 (Zoom 모드)
            var pbSize = previewPictureBox.Size;
            var imgSize = lastCapturedImage.Size;

            float scaleX = (float)pbSize.Width / imgSize.Width;
            float scaleY = (float)pbSize.Height / imgSize.Height;
            float scale = Math.Min(scaleX, scaleY);

            int displayWidth = (int)(imgSize.Width * scale);
            int displayHeight = (int)(imgSize.Height * scale);

            int offsetX = (pbSize.Width - displayWidth) / 2;
            int offsetY = (pbSize.Height - displayHeight) / 2;

            displayRect = new Rectangle(offsetX, offsetY, displayWidth, displayHeight);
        }

        private Point PictureBoxToImageCoordinates(Point pbPoint)
        {
            if (lastCapturedImage == null) return pbPoint;

            // PictureBox 좌표를 실제 이미지 좌표로 변환
            float scaleX = (float)actualImageSize.Width / displayRect.Width;
            float scaleY = (float)actualImageSize.Height / displayRect.Height;

            int imageX = (int)((pbPoint.X - displayRect.X) * scaleX);
            int imageY = (int)((pbPoint.Y - displayRect.Y) * scaleY);

            // 경계 체크
            imageX = Math.Max(0, Math.Min(imageX, actualImageSize.Width - 1));
            imageY = Math.Max(0, Math.Min(imageY, actualImageSize.Height - 1));

            return new Point(imageX, imageY);
        }

        private bool CaptureWithBitBlt()
        {
            // 기존 BitBlt 방식
            GetWindowRect(gameWindowHandle, out RECT windowRect);
            int clientWidth = 800;
            int clientHeight = 600;

            IntPtr srcDC = GetDC(gameWindowHandle);
            IntPtr destDC = CreateCompatibleDC(srcDC);
            IntPtr bitmap = CreateCompatibleBitmap(srcDC, clientWidth, clientHeight);
            IntPtr oldBitmap = SelectObject(destDC, bitmap);

            bool success = BitBlt(destDC, 0, 0, clientWidth, clientHeight, srcDC, 0, 0, SRCCOPY);

            if (success)
            {
                Bitmap capturedBitmap = Image.FromHbitmap(bitmap);
                
                if (previewPictureBox.Image != null)
                    previewPictureBox.Image.Dispose();
                
                previewPictureBox.Image = capturedBitmap;
            }

            SelectObject(destDC, oldBitmap);
            DeleteObject(bitmap);
            DeleteDC(destDC);
            ReleaseDC(gameWindowHandle, srcDC);
            
            return success;
        }

        private bool CaptureWithPrintWindow()
        {
            // PrintWindow API 시도
            int clientWidth = 800;
            int clientHeight = 600;
            
            IntPtr srcDC = GetDC(IntPtr.Zero);
            IntPtr destDC = CreateCompatibleDC(srcDC);
            IntPtr bitmap = CreateCompatibleBitmap(srcDC, clientWidth, clientHeight);
            IntPtr oldBitmap = SelectObject(destDC, bitmap);

            bool success = PrintWindow(gameWindowHandle, destDC, 0);

            if (success)
            {
                Bitmap capturedBitmap = Image.FromHbitmap(bitmap);
                
                if (previewPictureBox.Image != null)
                    previewPictureBox.Image.Dispose();
                
                previewPictureBox.Image = capturedBitmap;
            }

            SelectObject(destDC, oldBitmap);
            DeleteObject(bitmap);
            DeleteDC(destDC);
            ReleaseDC(IntPtr.Zero, srcDC);
            
            return success;
        }

        private void DirectXButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (gameWindowHandle != IntPtr.Zero)
                {
                    // 선택된 창을 화면 좌표로 캡처
                    var bitmap = ScreenCapture.CaptureWindow(gameWindowHandle);
                    
                    if (bitmap != null)
                    {
                        if (previewPictureBox.Image != null)
                            previewPictureBox.Image.Dispose();
                        
                        previewPictureBox.Image = bitmap;
                        statusLabel.Text = "상태: 화면 좌표 캡처 성공";
                    }
                    else
                    {
                        statusLabel.Text = "상태: 화면 좌표 캡처 실패";
                    }
                }
                else
                {
                    statusLabel.Text = "상태: 먼저 창을 선택하세요";
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"상태: 캡처 오류 - {ex.Message}";
            }
        }
        private void RoiModeButton_Click(object sender, EventArgs e)
        {
            isRoiMode = !isRoiMode;
            
            if (isRoiMode)
            {
                roiModeButton.Text = "ROI 종료";
                roiModeButton.BackColor = Color.LightCoral;
                statusLabel.Text = "상태: ROI 설정 모드 - 영역을 드래그하세요";
                
                // 캡처 중지
                if (captureTimer.Enabled)
                {
                    captureTimer.Stop();
                    startCaptureButton.Text = "캡처 시작";
                }
            }
            else
            {
                roiModeButton.Text = "ROI 설정";
                roiModeButton.BackColor = Color.LightBlue;
                statusLabel.Text = "상태: 일반 모드";
                saveRoiButton.Enabled = false;
                previewPictureBox.Invalidate(); // ROI 사각형 지우기
            }
        }

        private void SaveRoiButton_Click(object sender, EventArgs e)
        {
            if (currentRoi.Width > 0 && currentRoi.Height > 0)
            {
                // ROI 이름 입력 받기
                string roiName = Microsoft.VisualBasic.Interaction.InputBox(
                    "ROI 이름을 입력하세요 (예: minimap, character, hp_mp):",
                    "ROI 저장",
                    "roi_" + DateTime.Now.ToString("HHmmss"));

                if (!string.IsNullOrEmpty(roiName))
                {
                    SaveRoiToConfig(roiName, currentRoi);
                    statusLabel.Text = $"상태: ROI '{roiName}' 저장 완료";
                }
            }
        }

        private void PreviewPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if ((isRoiMode || isTemplateMode) && e.Button == MouseButtons.Left)
            {
                isDrawing = true;
                startPoint = PictureBoxToImageCoordinates(e.Location);
                currentRoi = new Rectangle(startPoint, new Size(0, 0));
            }
        }

        private void PreviewPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if ((isRoiMode || isTemplateMode) && isDrawing)
            {
                // 마우스 좌표를 이미지 좌표로 변환
                Point imagePoint = PictureBoxToImageCoordinates(e.Location);
                
                // 현재 마우스 위치까지의 사각형 계산 (이미지 좌표계에서)
                int x = Math.Min(startPoint.X, imagePoint.X);
                int y = Math.Min(startPoint.Y, imagePoint.Y);
                int width = Math.Abs(imagePoint.X - startPoint.X);
                int height = Math.Abs(imagePoint.Y - startPoint.Y);
                
                currentRoi = new Rectangle(x, y, width, height);
                previewPictureBox.Invalidate(); // 다시 그리기
            }
        }

        private void PreviewPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if ((isRoiMode || isTemplateMode) && isDrawing)
            {
                isDrawing = false;
                
                if (currentRoi.Width > 10 && currentRoi.Height > 10)
                {
                    if (isRoiMode)
                    {
                        saveRoiButton.Enabled = true;
                        statusLabel.Text = $"상태: ROI 선택됨 (이미지좌표: {currentRoi.X},{currentRoi.Y} {currentRoi.Width}x{currentRoi.Height}) - 저장 버튼을 누르세요";
                    }
                    else if (isTemplateMode)
                    {
                        saveTemplateButton.Enabled = true;
                        statusLabel.Text = $"상태: 템플릿 선택됨 ({currentRoi.Width}x{currentRoi.Height}) - 저장 버튼을 누르세요";
                    }
                }
                else
                {
                    statusLabel.Text = "상태: 선택 영역이 너무 작습니다. 다시 그려주세요";
                }
            }
        }

        private void PreviewPictureBox_Paint(object sender, PaintEventArgs e)
        {
            if ((isRoiMode || isTemplateMode) && currentRoi.Width > 0 && currentRoi.Height > 0)
            {
                // 이미지 좌표를 PictureBox 좌표로 변환해서 그리기
                Rectangle displayRoi = ImageToPictureBoxCoordinates(currentRoi);
                
                // 모드에 따라 색상 변경
                Color rectColor = isRoiMode ? Color.Red : Color.Blue;
                
                // 사각형 그리기
                using (Pen pen = new Pen(rectColor, 2))
                {
                    e.Graphics.DrawRectangle(pen, displayRoi);
                }
                
                // 정보 표시
                using (Brush brush = new SolidBrush(rectColor))
                {
                    string info = $"{currentRoi.Width}x{currentRoi.Height}";
                    string modeText = isRoiMode ? "ROI" : "템플릿";
                    e.Graphics.DrawString($"{modeText}: {info}", this.Font, brush, 
                        displayRoi.X, displayRoi.Y - 20);
                }
            }
        }

        private Rectangle ImageToPictureBoxCoordinates(Rectangle imageRect)
        {
            if (lastCapturedImage == null) return imageRect;

            // 이미지 좌표를 PictureBox 표시 좌표로 변환
            float scaleX = (float)displayRect.Width / actualImageSize.Width;
            float scaleY = (float)displayRect.Height / actualImageSize.Height;

            int pbX = (int)(imageRect.X * scaleX) + displayRect.X;
            int pbY = (int)(imageRect.Y * scaleY) + displayRect.Y;
            int pbWidth = (int)(imageRect.Width * scaleX);
            int pbHeight = (int)(imageRect.Height * scaleY);

            return new Rectangle(pbX, pbY, pbWidth, pbHeight);
        }
        private void LoadRoiButton_Click(object sender, EventArgs e)
        {
            try
            {
                AddDebugLog("ROI 로드 시작");
                string configPath = @"D:\macro\rois\roi_config.json";
                
                if (File.Exists(configPath))
                {
                    AddDebugLog($"JSON 파일 발견: {configPath}");
                    string json = File.ReadAllText(configPath);
                    dynamic config = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                    
                    savedRois.Clear();
                    
                    if (config?.rois != null)
                    {
                        foreach (var roi in config.rois)
                        {
                            string roiName = roi.Name;
                            var roiData = roi.Value;
                            
                            Rectangle rect = new Rectangle(
                                (int)roiData.x,
                                (int)roiData.y,
                                (int)roiData.width,
                                (int)roiData.height
                            );
                            
                            savedRois[roiName] = rect;
                            AddDebugLog($"ROI 로드됨: {roiName} ({rect.X},{rect.Y} {rect.Width}x{rect.Height})");
                        }
                    }
                    
                    statusLabel.Text = $"상태: {savedRois.Count}개 ROI 로드 완료";
                    roiCaptureButton.Enabled = savedRois.Count > 0 && gameWindowHandle != IntPtr.Zero;
                    
                    AddDebugLog($"총 {savedRois.Count}개 ROI 로드 완료");
                    if (savedRois.Count > 0)
                    {
                        string roiList = string.Join(", ", savedRois.Keys);
                        AddDebugLog($"로드된 ROI 목록: {roiList}");
                        
                        // 기존 템플릿이 있으면 새로운 ROI에 재매핑
                        if (templates.Count > 0)
                        {
                            AddDebugLog($"기존 템플릿 {templates.Count}개를 새 ROI에 재매핑 시작");
                            RemapExistingTemplates();
                        }
                    }
                }
                else
                {
                    AddDebugLog($"JSON 파일 없음: {configPath}");
                    statusLabel.Text = "상태: ROI 설정 파일이 없습니다";
                }
            }
            catch (Exception ex)
            {
                AddDebugLog($"ROI 로드 실패: {ex.Message}");
                statusLabel.Text = $"상태: ROI 로드 실패 - {ex.Message}";
            }
        }

        private void RoiCaptureButton_Click(object sender, EventArgs e)
        {
            AddDebugLog("ROI 캡처 버튼 클릭됨!");
            
            if (roiCaptureButton.Text == "ROI 캡처 시작")
            {
                AddDebugLog("ROI 캡처 시작 모드 진입");
                
                if (savedRois.Count == 0)
                {
                    AddDebugLog("저장된 ROI가 없음");
                    statusLabel.Text = "상태: 먼저 ROI를 로드하세요";
                    return;
                }

                if (gameWindowHandle == IntPtr.Zero)
                {
                    AddDebugLog("게임 창 핸들이 없음");
                    statusLabel.Text = "상태: 먼저 창을 선택하세요";
                    return;
                }

                AddDebugLog("ROI 캡처 조건 만족 - 윈도우 생성 시작");

                // 기존 ROI 윈도우들 닫기
                CloseRoiWindows();

                // 각 ROI에 대해 미니 윈도우 생성
                CreateRoiWindows();

                // ROI 캡처 타이머 시작
                roiCaptureButton.Text = "ROI 캡처 중지";
                statusLabel.Text = $"상태: {savedRois.Count}개 ROI 실시간 캡처 중";
                AddDebugLog("ROI 캡처 시작 완료");
            }
            else
            {
                AddDebugLog("ROI 캡처 중지 모드 진입");
                // ROI 캡처 중지
                CloseRoiWindows();
                roiCaptureButton.Text = "ROI 캡처 시작";
                statusLabel.Text = "상태: ROI 캡처 중지";
                AddDebugLog("ROI 캡처 중지 완료");
            }
        }

        private void CloseRoiWindows()
        {
            foreach (Form window in roiWindows)
            {
                if (!window.IsDisposed)
                    window.Close();
            }
            roiWindows.Clear();
        }

        private void CreateRoiWindows()
        {
            try
            {
                AddDebugLog($"ROI 윈도우 생성 시작 - {savedRois.Count}개 ROI");
                
                if (savedRois.Count == 0)
                {
                    AddDebugLog("저장된 ROI가 없음 - 생성 중단");
                    statusLabel.Text = "상태: 저장된 ROI가 없습니다";
                    return;
                }

                // 화면 중앙 기준으로 배치
                int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
                int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
                
                // 화면 중앙에서 시작
                int centerX = screenWidth / 2;
                int centerY = screenHeight / 2;
                
                // 시작 위치 (중앙에서 약간 왼쪽 위)
                int windowX = centerX - 200;
                int windowY = centerY - 150;
                
                int createdCount = 0;
                int offsetX = 0; // 가로 오프셋
                int offsetY = 0; // 세로 오프셋

                foreach (var kvp in savedRois)
                {
                    try
                    {
                        string roiName = kvp.Key;
                        Rectangle roiRect = kvp.Value;

                        AddDebugLog($"'{roiName}' 윈도우 생성 중... ({roiRect.Width}x{roiRect.Height})");

                        // 크기 검증
                        if (roiRect.Width <= 0 || roiRect.Height <= 0)
                        {
                            AddDebugLog($"'{roiName}' 크기 오류: {roiRect.Width}x{roiRect.Height}");
                            continue;
                        }

                        int windowWidth = roiRect.Width + 20;
                        int windowHeight = roiRect.Height + 50;

                        // 현재 윈도우 위치 계산 (중앙 기준 + 오프셋)
                        int currentX = windowX + offsetX;
                        int currentY = windowY + offsetY;
                        
                        // 화면 경계 체크 및 안전 조정
                        if (currentX + windowWidth > screenWidth)
                        {
                            currentX = screenWidth - windowWidth - 10;
                        }
                        if (currentY + windowHeight > screenHeight)
                        {
                            currentY = screenHeight - windowHeight - 10;
                        }
                        if (currentX < 0) currentX = 10;
                        if (currentY < 0) currentY = 10;

                        // 미니 윈도우 생성
                        Form roiWindow = new Form();
                        roiWindow.Text = $"ROI: {roiName}";
                        roiWindow.Size = new Size(windowWidth, windowHeight);
                        roiWindow.Location = new Point(currentX, currentY);
                        roiWindow.FormBorderStyle = FormBorderStyle.FixedToolWindow;
                        roiWindow.TopMost = true;
                        roiWindow.StartPosition = FormStartPosition.Manual;

                        PictureBox roiPictureBox = new PictureBox();
                        roiPictureBox.Size = new Size(roiRect.Width, roiRect.Height);
                        roiPictureBox.Location = new Point(10, 10);
                        roiPictureBox.BorderStyle = BorderStyle.FixedSingle;
                        roiPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                        roiPictureBox.BackColor = Color.Black;

                        roiWindow.Controls.Add(roiPictureBox);
                        roiWindow.Tag = new { Name = roiName, Rect = roiRect, PictureBox = roiPictureBox };
                        
                        roiWindows.Add(roiWindow);
                        
                        // 윈도우 표시
                        roiWindow.Show();
                        roiWindow.BringToFront();
                        createdCount++;

                        AddDebugLog($"'{roiName}' 윈도우 생성 성공 - 위치: ({currentX},{currentY})");

                        // 다음 윈도우 오프셋 계산 (대각선으로 배치)
                        offsetX += 30;  // 오른쪽으로 30px
                        offsetY += 30;  // 아래로 30px
                        
                        // 오프셋이 너무 커지면 리셋
                        if (offsetX > 200)
                        {
                            offsetX = 0;
                            offsetY += 100;
                        }

                        Application.DoEvents();
                    }
                    catch (Exception ex)
                    {
                        AddDebugLog($"'{kvp.Key}' 윈도우 생성 실패: {ex.Message}");
                    }
                }

                AddDebugLog($"ROI 윈도우 생성 완료! {createdCount}/{savedRois.Count}개 성공");
                statusLabel.Text = $"상태: {createdCount}개 ROI 윈도우 생성 완료! (화면 중앙 배치)";
            }
            catch (Exception ex)
            {
                AddDebugLog($"ROI 윈도우 생성 전체 실패: {ex.Message}");
                statusLabel.Text = $"상태: ROI 윈도우 생성 전체 실패 - {ex.Message}";
            }
        }

        private void StatusPanelButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (statusPanel == null || statusPanel.IsDisposed)
                {
                    statusPanel = new StatusPanel();
                    statusPanel.Show();
                    AddDebugLog("Status Panel 생성 및 표시");
                }
                else
                {
                    if (statusPanel.Visible)
                    {
                        statusPanel.Hide();
                        AddDebugLog("Status Panel 숨김");
                    }
                    else
                    {
                        statusPanel.Show();
                        statusPanel.BringToFront();
                        AddDebugLog("Status Panel 표시");
                    }
                }
            }
            catch (Exception ex)
            {
                AddDebugLog($"Status Panel 오류: {ex.Message}");
            }
        }

        private void SaveRoiToConfig(string roiName, Rectangle roi)
        {
            try
            {
                string configPath = @"D:\macro\rois\roi_config.json";
                
                // 기존 설정 로드 또는 새로 생성
                dynamic config;
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    config = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                }
                else
                {
                    config = new
                    {
                        gameResolution = new { width = 800, height = 600 },
                        rois = new { }
                    };
                }

                // 새 ROI 추가
                var roiData = new
                {
                    x = roi.X,
                    y = roi.Y,
                    width = roi.Width,
                    height = roi.Height
                };

                // JSON 업데이트 (동적으로)
                string updatedJson = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
                
                // 간단한 문자열 조작으로 ROI 추가
                updatedJson = updatedJson.Replace("\"rois\": {", 
                    $"\"rois\": {{\r\n    \"{roiName}\": {{\r\n      \"x\": {roi.X},\r\n      \"y\": {roi.Y},\r\n      \"width\": {roi.Width},\r\n      \"height\": {roi.Height}\r\n    }},");
                
                File.WriteAllText(configPath, updatedJson);
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"상태: ROI 저장 실패 - {ex.Message}";
            }
        }

        private void TemplateModeButton_Click(object sender, EventArgs e)
        {
            isTemplateMode = !isTemplateMode;
            
            if (isTemplateMode)
            {
                templateModeButton.Text = "템플릿 종료";
                templateModeButton.BackColor = Color.Orange;
                statusLabel.Text = "상태: 템플릿 모드 - 객체를 드래그로 선택하세요";
                
                // 다른 모드들 비활성화
                isRoiMode = false;
                roiModeButton.Text = "ROI 설정";
                roiModeButton.BackColor = Color.LightBlue;
                
                // 캡처 중지
                if (captureTimer.Enabled)
                {
                    captureTimer.Stop();
                    startCaptureButton.Text = "캡처 시작";
                }
            }
            else
            {
                templateModeButton.Text = "템플릿 모드";
                templateModeButton.BackColor = Color.LightYellow;
                statusLabel.Text = "상태: 일반 모드";
                saveTemplateButton.Enabled = false;
                previewPictureBox.Invalidate(); // 템플릿 사각형 지우기
            }
        }

        private void SaveTemplateButton_Click(object sender, EventArgs e)
        {
            if (currentRoi.Width > 0 && currentRoi.Height > 0 && lastCapturedImage != null)
            {
                try
                {
                    // 템플릿 이름 입력 받기
                    string templateName = Microsoft.VisualBasic.Interaction.InputBox(
                        "템플릿 이름을 입력하세요:\n(예: player_character, goblin_monster, player_icon_minimap)",
                        "템플릿 저장",
                        "template_" + DateTime.Now.ToString("HHmmss"));

                    if (!string.IsNullOrEmpty(templateName))
                    {
                        // 카테고리 선택
                        string[] categories = { "character", "monster", "object", "minimap" };
                        string category = Microsoft.VisualBasic.Interaction.InputBox(
                            "카테고리를 선택하세요:\n" + string.Join(", ", categories),
                            "카테고리 선택",
                            "character");

                        if (string.IsNullOrEmpty(category) || !Array.Exists(categories, c => c == category))
                            category = "object";

                        // 템플릿 이미지 추출
                        Bitmap templateImage = ExtractTemplate(lastCapturedImage, currentRoi);
                        
                        // 파일 저장
                        string templateDir = $@"D:\macro\templates\{category}";
                        Directory.CreateDirectory(templateDir);
                        string templatePath = Path.Combine(templateDir, $"{templateName}.png");
                        
                        templateImage.Save(templatePath, ImageFormat.Png);
                        
                        // 메모리에 로드
                        templates[templateName] = templateImage;
                        
                        // 즉시 ROI 매핑 업데이트
                        SetTemplateRoiMappingForSingle(templateName, category);
                        
                        statusLabel.Text = $"상태: 템플릿 '{templateName}' 저장 완료 ({category})";
                        startMatchingButton.Enabled = templates.Count > 0;
                        AddDebugLog($"템플릿 저장 및 로드됨: {templateName} ({category})");
                    }
                }
                catch (Exception ex)
                {
                    statusLabel.Text = $"상태: 템플릿 저장 실패 - {ex.Message}";
                }
            }
        }

        private void RemapExistingTemplates()
        {
            AddDebugLog("=== 기존 템플릿 재매핑 시작 ===");
            roiTemplateMap.Clear();
            
            // 각 템플릿에 대해 카테고리를 추론하고 재매핑
            foreach (var template in templates)
            {
                string templateName = template.Key;
                string category = InferTemplateCategory(templateName);
                
                AddDebugLog($"템플릿 '{templateName}' -> 추론된 카테고리: {category}");
                SetTemplateRoiMapping(templateName, category);
            }
            
            AddDebugLog("=== 기존 템플릿 재매핑 완료 ===");
        }
        
        private string InferTemplateCategory(string templateName)
        {
            string lowerName = templateName.ToLower();
            
            // 미니맵 관련 키워드 우선 체크 (가장 구체적)
            if (lowerName.Contains("minimap") || lowerName.Contains("map_icon") || 
                lowerName.Contains("player_icon") || lowerName.Contains("icon_minimap"))
                return "minimap";
            // 캐릭터 관련 키워드
            else if (lowerName.Contains("player") || lowerName.Contains("character") || lowerName.Contains("hero"))
                return "character";
            // 몬스터 관련 키워드
            else if (lowerName.Contains("monster") || lowerName.Contains("mob") || 
                     lowerName.Contains("enemy") || lowerName.Contains("goblin") || 
                     lowerName.Contains("orc") || lowerName.Contains("skeleton"))
                return "monster";
            // 기타는 오브젝트
            else
                return "object";
        }

        private void LoadTemplateButton_Click(object sender, EventArgs e)
        {
            AddDebugLog("수동 템플릿 로드 시작");
            templates.Clear();
            roiTemplateMap.Clear();
            
            LoadAllTemplates();
            
            if (templates.Count > 0)
            {
                startMatchingButton.Enabled = true;
                AddDebugLog($"수동 로드 완료: {templates.Count}개 템플릿");
                statusLabel.Text = $"상태: {templates.Count}개 템플릿 로드 완료";
            }
            else
            {
                AddDebugLog("로드할 템플릿이 없음");
                statusLabel.Text = "상태: 로드할 템플릿이 없습니다";
            }
        }

        private void StartMatchingButton_Click(object sender, EventArgs e)
        {
            AddDebugLog("매칭 버튼 클릭됨");
            
            if (!isMatchingMode)
            {
                // 템플릿이 없으면 다시 로드 시도
                if (templates.Count == 0)
                {
                    LoadAllTemplates();
                }
                
                if (templates.Count == 0)
                {
                    AddDebugLog("로드된 템플릿이 없음");
                    statusLabel.Text = "상태: 템플릿 파일이 없습니다. 먼저 템플릿을 생성하세요";
                    return;
                }
                
                // ROI-템플릿 매핑 상태 체크
                if (savedRois.Count == 0)
                {
                    AddDebugLog("⚠️ 저장된 ROI가 없음 - ROI를 먼저 로드하세요");
                    statusLabel.Text = "상태: ROI를 먼저 로드하세요";
                    return;
                }
                
                // 매핑이 비어있으면 재매핑 시도
                if (roiTemplateMap.Count == 0)
                {
                    AddDebugLog("ROI-템플릿 매핑이 없어서 재매핑 시도");
                    RemapExistingTemplates();
                }
                
                // 매핑 상태 최종 확인
                bool hasValidMapping = false;
                foreach (var mapping in roiTemplateMap)
                {
                    if (mapping.Value.Count > 0)
                    {
                        hasValidMapping = true;
                        break;
                    }
                }
                
                if (!hasValidMapping)
                {
                    AddDebugLog("⚠️ 유효한 ROI-템플릿 매핑이 없음");
                    statusLabel.Text = "상태: ROI와 템플릿 이름이 매칭되지 않습니다. ROI 이름에 'character', 'minimap' 등을 포함하세요";
                    return;
                }

                isMatchingMode = true;
                startMatchingButton.Text = "매칭 중지";
                startMatchingButton.BackColor = Color.Red;
                statusLabel.Text = $"상태: 실시간 템플릿 매칭 중 ({templates.Count}개 템플릿)";
                AddDebugLog($"템플릿 매칭 시작 - {templates.Count}개 템플릿 사용");
                
                // 매칭 타이머 시작 (기존 캡처 타이머와 함께)
                if (!captureTimer.Enabled)
                {
                    captureTimer.Start();
                    startCaptureButton.Text = "캡처 중지";
                }
            }
            else
            {
                isMatchingMode = false;
                startMatchingButton.Text = "매칭 시작";
                startMatchingButton.BackColor = Color.LightPink;
                statusLabel.Text = "상태: 매칭 중지";
                AddDebugLog("템플릿 매칭 중지");
            }
        }

        private void LoadAllTemplates()
        {
            try
            {
                AddDebugLog("=== 템플릿 로드 시작 ===");
                templates.Clear();
                roiTemplateMap.Clear();

                string templatesBasePath = @"D:\macro\templates";
                AddDebugLog($"템플릿 기본 경로: {templatesBasePath}");
                
                if (!Directory.Exists(templatesBasePath))
                {
                    AddDebugLog($"템플릿 폴더가 존재하지 않음: {templatesBasePath}");
                    return;
                }

                string[] categories = { "character", "monster", "object", "minimap" };

                foreach (string category in categories)
                {
                    string categoryPath = Path.Combine(templatesBasePath, category);
                    AddDebugLog($"카테고리 확인: {category} -> {categoryPath}");
                    
                    if (Directory.Exists(categoryPath))
                    {
                        string[] imageFiles = Directory.GetFiles(categoryPath, "*.png");
                        AddDebugLog($"{category} 폴더에서 {imageFiles.Length}개 PNG 파일 발견");
                        
                        foreach (string filePath in imageFiles)
                        {
                            try
                            {
                                string fileName = Path.GetFileNameWithoutExtension(filePath);
                                AddDebugLog($"템플릿 로드 시도: {fileName} ({filePath})");
                                
                                Bitmap templateImage = new Bitmap(filePath);
                                templates[fileName] = templateImage;
                                
                                // ROI별 템플릿 매핑 설정
                                SetTemplateRoiMapping(fileName, category);
                                
                                AddDebugLog($"✅ 템플릿 로드 성공: {fileName} ({category}) - 크기: {templateImage.Width}x{templateImage.Height}");
                            }
                            catch (Exception ex)
                            {
                                AddDebugLog($"❌ 템플릿 로드 실패: {filePath} - {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        AddDebugLog($"카테고리 폴더 없음: {categoryPath}");
                    }
                }

                AddDebugLog($"=== 템플릿 로드 완료: 총 {templates.Count}개 ===");
                
                // ROI 매핑 상태 출력
                foreach (var roi in roiTemplateMap)
                {
                    AddDebugLog($"ROI '{roi.Key}' -> 템플릿 {roi.Value.Count}개: [{string.Join(", ", roi.Value)}]");
                }
            }
            catch (Exception ex)
            {
                AddDebugLog($"❌ 템플릿 로드 전체 실패: {ex.Message}");
            }
        }

        private void SetTemplateRoiMapping(string templateName, string category)
        {
            AddDebugLog($"템플릿-ROI 매핑 설정: {templateName} ({category})");
            
            // 저장된 ROI가 없으면 매핑할 수 없음
            if (savedRois.Count == 0)
            {
                AddDebugLog("⚠️ 저장된 ROI가 없어서 템플릿 매핑 불가 - 먼저 ROI를 로드하세요");
                return;
            }
            
            // ROI별로 어떤 템플릿을 사용할지 매핑 (엄격한 규칙 적용)
            foreach (var roiName in savedRois.Keys)
            {
                if (!roiTemplateMap.ContainsKey(roiName))
                    roiTemplateMap[roiName] = new List<string>();

                bool mapped = false;
                string lowerRoiName = roiName.ToLower();
                
                // 엄격한 매핑 규칙 적용
                if (category == "minimap")
                {
                    // 미니맵 템플릿은 오직 미니맵 ROI에만
                    if (lowerRoiName.Contains("minimap") || lowerRoiName.Contains("map"))
                    {
                        roiTemplateMap[roiName].Add(templateName);
                        mapped = true;
                        AddDebugLog($"  ✅ ROI '{roiName}' 에 미니맵 템플릿 '{templateName}' 매핑됨");
                    }
                }
                else if (category == "character")
                {
                    // 캐릭터 템플릿은 오직 캐릭터 ROI에만
                    if (lowerRoiName.Contains("character") || lowerRoiName.Contains("player"))
                    {
                        roiTemplateMap[roiName].Add(templateName);
                        mapped = true;
                        AddDebugLog($"  ✅ ROI '{roiName}' 에 캐릭터 템플릿 '{templateName}' 매핑됨");
                    }
                }
                else if (category == "monster")
                {
                    // 몬스터 템플릿은 오직 캐릭터 ROI에만 (캐릭터 주변에서 몬스터 찾기)
                    if (lowerRoiName.Contains("character") || lowerRoiName.Contains("player"))
                    {
                        roiTemplateMap[roiName].Add(templateName);
                        mapped = true;
                        AddDebugLog($"  ✅ ROI '{roiName}' 에 몬스터 템플릿 '{templateName}' 매핑됨");
                    }
                }
                else if (category == "object")
                {
                    // 오브젝트 템플릿은 미니맵과 캐릭터가 아닌 ROI에만
                    if (!lowerRoiName.Contains("minimap") && !lowerRoiName.Contains("map") &&
                        !lowerRoiName.Contains("character") && !lowerRoiName.Contains("player"))
                    {
                        roiTemplateMap[roiName].Add(templateName);
                        mapped = true;
                        AddDebugLog($"  ✅ ROI '{roiName}' 에 오브젝트 템플릿 '{templateName}' 매핑됨");
                    }
                }
                
                if (!mapped)
                {
                    AddDebugLog($"  ❌ ROI '{roiName}' 에 {category} 템플릿 '{templateName}' 매핑 안됨 (엄격한 규칙)");
                }
            }
        }

        private void SetTemplateRoiMappingForSingle(string templateName, string category)
        {
            // 단일 템플릿에 대해 ROI 매핑 설정 (엄격한 규칙)
            foreach (var roiName in savedRois.Keys)
            {
                if (!roiTemplateMap.ContainsKey(roiName))
                    roiTemplateMap[roiName] = new List<string>();

                string lowerRoiName = roiName.ToLower();
                
                // 엄격한 매핑 규칙 적용
                if (category == "minimap")
                {
                    // 미니맵 템플릿은 오직 미니맵 ROI에만
                    if (lowerRoiName.Contains("minimap") || lowerRoiName.Contains("map"))
                    {
                        roiTemplateMap[roiName].Add(templateName);
                    }
                }
                else if (category == "character")
                {
                    // 캐릭터 템플릿은 오직 캐릭터 ROI에만
                    if (lowerRoiName.Contains("character") || lowerRoiName.Contains("player"))
                    {
                        roiTemplateMap[roiName].Add(templateName);
                    }
                }
                else if (category == "monster")
                {
                    // 몬스터 템플릿은 오직 캐릭터 ROI에만
                    if (lowerRoiName.Contains("character") || lowerRoiName.Contains("player"))
                    {
                        roiTemplateMap[roiName].Add(templateName);
                    }
                }
                else if (category == "object")
                {
                    // 오브젝트는 미니맵/캐릭터가 아닌 ROI에만
                    if (!lowerRoiName.Contains("minimap") && !lowerRoiName.Contains("map") &&
                        !lowerRoiName.Contains("character") && !lowerRoiName.Contains("player"))
                    {
                        roiTemplateMap[roiName].Add(templateName);
                    }
                }
            }
        }

        private Bitmap ExtractTemplate(Bitmap sourceImage, Rectangle roi)
        {
            // ROI 영역을 템플릿으로 추출
            Bitmap template = new Bitmap(roi.Width, roi.Height);
            using (Graphics g = Graphics.FromImage(template))
            {
                g.DrawImage(sourceImage, 0, 0, roi, GraphicsUnit.Pixel);
            }
            return template;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            captureTimer?.Stop();
            captureTimer?.Dispose();
            previewPictureBox?.Image?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
