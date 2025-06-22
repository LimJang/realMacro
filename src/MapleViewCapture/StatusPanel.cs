using System;
using System.Drawing;
using System.Windows.Forms;

namespace MapleViewCapture
{
    public partial class StatusPanel : Form
    {
        private Label? hpLabel;
        private Label? mpLabel;
        private Label? templateCountLabel;
        private Label? fpsLabel;
        private Label? performanceLabel;
        private Label? minimapPosLabel;      // 미니맵 좌표 표시
        private Label? nearestMonsterLabel;  // 가장 가까운 몬스터 거리
        private Label? detectedCountLabel;   // 감지된 객체 수
        private Panel? hpBar;
        private Panel? mpBar;
        private Panel? hpBackground;
        private Panel? mpBackground;
        
        private HPMPDetector.StatusResult? lastHPResult = null;
        private HPMPDetector.StatusResult? lastMPResult = null;
        private int templateMatchCount = 0;
        private double lastFPS = 0.0;
        private double lastProcessingTime = 0.0;

        public StatusPanel()
        {
            InitializeComponent();
            SetupPanel();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form 설정
            this.Text = "Status Panel";
            this.Size = new Size(300, 280);
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(50, 50);
            this.BackColor = Color.FromArgb(45, 45, 48);
            
            this.ResumeLayout(false);
        }

        private void SetupPanel()
        {
            // HP 라벨
            hpLabel = new Label
            {
                Text = "HP: --",
                Location = new Point(15, 15),
                Size = new Size(80, 20),
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            // HP 바 배경
            hpBackground = new Panel
            {
                Location = new Point(100, 18),
                Size = new Size(150, 14),
                BackColor = Color.DarkGray,
                BorderStyle = BorderStyle.FixedSingle
            };

            // HP 바
            hpBar = new Panel
            {
                Location = new Point(1, 1),
                Size = new Size(0, 12),
                BackColor = Color.Green
            };

            // MP 라벨
            mpLabel = new Label
            {
                Text = "MP: --",
                Location = new Point(15, 45),
                Size = new Size(80, 20),
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            // MP 바 배경
            mpBackground = new Panel
            {
                Location = new Point(100, 48),
                Size = new Size(150, 14),
                BackColor = Color.DarkGray,
                BorderStyle = BorderStyle.FixedSingle
            };

            // MP 바
            mpBar = new Panel
            {
                Location = new Point(1, 1),
                Size = new Size(0, 12),
                BackColor = Color.Blue
            };

            // 템플릿 매칭 정보
            templateCountLabel = new Label
            {
                Text = "Templates: 0 detected",
                Location = new Point(15, 75),
                Size = new Size(250, 20),
                ForeColor = Color.LightGray,
                Font = new Font("Arial", 9)
            };

            // FPS 정보
            fpsLabel = new Label
            {
                Text = "FPS: --",
                Location = new Point(15, 100),
                Size = new Size(120, 20),
                ForeColor = Color.LightGreen,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            // 성능 정보 (처리 시간)
            performanceLabel = new Label
            {
                Text = "Process: -- ms",
                Location = new Point(140, 100),
                Size = new Size(140, 20),
                ForeColor = Color.LightBlue,
                Font = new Font("Arial", 9)
            };

            // 미니맵 좌표 정보
            minimapPosLabel = new Label
            {
                Text = "미니맵: (---, ---)",
                Location = new Point(15, 130),
                Size = new Size(250, 20),
                ForeColor = Color.Yellow,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            // 가장 가까운 몬스터 거리
            nearestMonsterLabel = new Label
            {
                Text = "가장 가까운 몬스터: --px",
                Location = new Point(15, 155),
                Size = new Size(250, 20),
                ForeColor = Color.Orange,
                Font = new Font("Arial", 9)
            };

            // 감지된 객체 수
            detectedCountLabel = new Label
            {
                Text = "감지: 캐릭터0, 몬스터0",
                Location = new Point(15, 180),
                Size = new Size(250, 20),
                ForeColor = Color.LightGray,
                Font = new Font("Arial", 9)
            };

            // 컨트롤 추가
            hpBackground.Controls.Add(hpBar);
            mpBackground.Controls.Add(mpBar);
            
            this.Controls.Add(hpLabel);
            this.Controls.Add(hpBackground);
            this.Controls.Add(mpLabel);
            this.Controls.Add(mpBackground);
            this.Controls.Add(templateCountLabel);
            this.Controls.Add(fpsLabel);
            this.Controls.Add(performanceLabel);
            this.Controls.Add(minimapPosLabel);
            this.Controls.Add(nearestMonsterLabel);
            this.Controls.Add(detectedCountLabel);
        }

        public void UpdateHP(HPMPDetector.StatusResult hpResult)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateHP(hpResult)));
                return;
            }

            lastHPResult = hpResult;
            
            if (hpResult != null && hpLabel != null && hpBar != null)
            {
                hpLabel.Text = $"HP: {hpResult.Ratio:P0}";
                hpLabel.ForeColor = hpResult.StatusColor;
                
                // HP 바 길이 업데이트
                int barWidth = (int)(148 * hpResult.Ratio); // 148 = 150 - 2 (border)
                hpBar.Size = new Size(Math.Max(0, barWidth), 12);
                hpBar.BackColor = hpResult.StatusColor;

                // 위험 상태 시 깜빡임
                if (hpResult.IsLow)
                {
                    this.BackColor = Color.FromArgb(60, 20, 20); // 어두운 빨강
                }
                else
                {
                    this.BackColor = Color.FromArgb(45, 45, 48); // 원래 색
                }
            }
        }

        public void UpdateMP(HPMPDetector.StatusResult mpResult)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateMP(mpResult)));
                return;
            }

            lastMPResult = mpResult;
            
            if (mpResult != null && mpLabel != null && mpBar != null)
            {
                mpLabel.Text = $"MP: {mpResult.Ratio:P0}";
                mpLabel.ForeColor = mpResult.StatusColor;
                
                // MP 바 길이 업데이트
                int barWidth = (int)(148 * mpResult.Ratio);
                mpBar.Size = new Size(Math.Max(0, barWidth), 12);
                mpBar.BackColor = mpResult.StatusColor;

                // 위험 상태 시 배경색 조정
                if (mpResult.IsLow && (lastHPResult?.IsLow != true))
                {
                    this.BackColor = Color.FromArgb(20, 20, 60); // 어두운 파랑
                }
                else if (!mpResult.IsLow && (lastHPResult?.IsLow != true))
                {
                    this.BackColor = Color.FromArgb(45, 45, 48); // 원래 색
                }
            }
        }

        public void UpdateTemplateCount(int count)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateTemplateCount(count)));
                return;
            }

            templateMatchCount = count;
            if (templateCountLabel != null)
            {
                templateCountLabel.Text = $"Templates: {count} detected";
                
                if (count > 0)
                {
                    templateCountLabel.ForeColor = Color.LightGreen;
                }
                else
                {
                    templateCountLabel.ForeColor = Color.LightGray;
                }
            }
        }

        public void UpdatePerformance(double fps, double processingTimeMs)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdatePerformance(fps, processingTimeMs)));
                return;
            }

            lastFPS = fps;
            lastProcessingTime = processingTimeMs;

            if (fpsLabel != null)
            {
                fpsLabel.Text = $"FPS: {fps:F1}";
                
                // FPS에 따른 색상 변경
                if (fps >= 8.0)
                    fpsLabel.ForeColor = Color.LightGreen;    // 좋음
                else if (fps >= 5.0)
                    fpsLabel.ForeColor = Color.Yellow;        // 보통
                else
                    fpsLabel.ForeColor = Color.Orange;        // 나쁨
            }

            if (performanceLabel != null)
            {
                performanceLabel.Text = $"Process: {processingTimeMs:F1}ms";
                
                // 처리 시간에 따른 색상 변경
                if (processingTimeMs <= 50.0)
                    performanceLabel.ForeColor = Color.LightBlue;     // 빠름
                else if (processingTimeMs <= 100.0)
                    performanceLabel.ForeColor = Color.Yellow;        // 보통
                else
                    performanceLabel.ForeColor = Color.Orange;        // 느림
            }
        }

        public void UpdateMinimapPosition(Point gameCoords)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateMinimapPosition(gameCoords)));
                return;
            }

            if (minimapPosLabel != null)
            {
                minimapPosLabel.Text = $"미니맵: ({gameCoords.X}, {gameCoords.Y})";
            }
        }

        public void UpdateNearestMonster(double distance, string monsterName = "몬스터")
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateNearestMonster(distance, monsterName)));
                return;
            }

            if (nearestMonsterLabel != null)
            {
                if (distance >= 0)
                {
                    nearestMonsterLabel.Text = $"가장 가까운 {monsterName}: {distance:F0}px";
                    
                    // 거리에 따른 색상 변경
                    if (distance <= 50)
                        nearestMonsterLabel.ForeColor = Color.Red;        // 매우 가까움
                    else if (distance <= 100)
                        nearestMonsterLabel.ForeColor = Color.Orange;     // 가까움
                    else if (distance <= 200)
                        nearestMonsterLabel.ForeColor = Color.Yellow;     // 보통
                    else
                        nearestMonsterLabel.ForeColor = Color.LightGreen; // 멀음
                }
                else
                {
                    nearestMonsterLabel.Text = "가장 가까운 몬스터: 없음";
                    nearestMonsterLabel.ForeColor = Color.Gray;
                }
            }
        }

        public void UpdateDetectedCount(int playerCount, int monsterCount)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateDetectedCount(playerCount, monsterCount)));
                return;
            }

            if (detectedCountLabel != null)
            {
                detectedCountLabel.Text = $"감지: 캐릭터{playerCount}, 몬스터{monsterCount}";
                
                // 감지된 객체 수에 따른 색상
                if (playerCount > 0 || monsterCount > 0)
                    detectedCountLabel.ForeColor = Color.LightGreen;
                else
                    detectedCountLabel.ForeColor = Color.Gray;
            }
        }

        public void AddStatusMessage(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => AddStatusMessage(message)));
                return;
            }

            // 임시 메시지 표시 (3초 후 사라짐)
            Label tempLabel = new Label
            {
                Text = message,
                Location = new Point(15, 105),
                Size = new Size(250, 40),
                ForeColor = Color.Yellow,
                Font = new Font("Arial", 8),
                AutoSize = true
            };

            this.Controls.Add(tempLabel);

            // 3초 후 제거
            System.Windows.Forms.Timer removeTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            removeTimer.Tick += (s, e) =>
            {
                this.Controls.Remove(tempLabel);
                tempLabel.Dispose();
                removeTimer.Stop();
                removeTimer.Dispose();
            };
            removeTimer.Start();
        }
    }
}
