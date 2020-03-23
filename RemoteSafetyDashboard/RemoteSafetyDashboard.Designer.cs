namespace RemoteSafetyDashboard
{
    partial class RemoteSafetyDashboard
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RemoteSafetyDashboard));
            this.groupBoxWeather = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanelWeather = new System.Windows.Forms.TableLayoutPanel();
            this.label12 = new System.Windows.Forms.Label();
            this.labelCloudCoverValue = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.labelRainRateValue = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.labelDewPointValue = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.labelPressureValue = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.labelHumidityValue = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.labelSkyTempValue = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.labelTempValue = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.labelWindDirValue = new System.Windows.Forms.Label();
            this.labelSunElevationValue = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label22 = new System.Windows.Forms.Label();
            this.labelWindSpeedValue = new System.Windows.Forms.Label();
            this.labelWeatherStatus = new System.Windows.Forms.Label();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.safetyOnTheWIse40WikiToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.labelNextCheck = new System.Windows.Forms.Label();
            this.timerRefresh = new System.Windows.Forms.Timer(this.components);
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.label5 = new System.Windows.Forms.Label();
            this.labelTitle = new System.Windows.Forms.Label();
            this.labelDate = new System.Windows.Forms.Label();
            this.groupBoxWeather.SuspendLayout();
            this.tableLayoutPanelWeather.SuspendLayout();
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxWeather
            // 
            this.groupBoxWeather.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.groupBoxWeather.Controls.Add(this.tableLayoutPanelWeather);
            this.groupBoxWeather.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBoxWeather.Location = new System.Drawing.Point(13, 168);
            this.groupBoxWeather.Name = "groupBoxWeather";
            this.groupBoxWeather.Size = new System.Drawing.Size(383, 182);
            this.groupBoxWeather.TabIndex = 34;
            this.groupBoxWeather.TabStop = false;
            this.groupBoxWeather.Text = "Safe To Operate (latest readings) ";
            // 
            // tableLayoutPanelWeather
            // 
            this.tableLayoutPanelWeather.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanelWeather.ColumnCount = 4;
            this.tableLayoutPanelWeather.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 26.41509F));
            this.tableLayoutPanelWeather.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 24.21384F));
            this.tableLayoutPanelWeather.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 27.35849F));
            this.tableLayoutPanelWeather.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 22.01258F));
            this.tableLayoutPanelWeather.Controls.Add(this.label12, 0, 0);
            this.tableLayoutPanelWeather.Controls.Add(this.labelCloudCoverValue, 1, 0);
            this.tableLayoutPanelWeather.Controls.Add(this.label18, 2, 0);
            this.tableLayoutPanelWeather.Controls.Add(this.labelRainRateValue, 3, 0);
            this.tableLayoutPanelWeather.Controls.Add(this.label13, 0, 1);
            this.tableLayoutPanelWeather.Controls.Add(this.labelDewPointValue, 1, 1);
            this.tableLayoutPanelWeather.Controls.Add(this.label14, 0, 2);
            this.tableLayoutPanelWeather.Controls.Add(this.labelPressureValue, 1, 4);
            this.tableLayoutPanelWeather.Controls.Add(this.label16, 0, 4);
            this.tableLayoutPanelWeather.Controls.Add(this.labelHumidityValue, 1, 2);
            this.tableLayoutPanelWeather.Controls.Add(this.label19, 2, 1);
            this.tableLayoutPanelWeather.Controls.Add(this.labelSkyTempValue, 3, 1);
            this.tableLayoutPanelWeather.Controls.Add(this.label20, 2, 2);
            this.tableLayoutPanelWeather.Controls.Add(this.labelTempValue, 3, 2);
            this.tableLayoutPanelWeather.Controls.Add(this.label21, 2, 3);
            this.tableLayoutPanelWeather.Controls.Add(this.labelWindDirValue, 3, 3);
            this.tableLayoutPanelWeather.Controls.Add(this.labelSunElevationValue, 3, 4);
            this.tableLayoutPanelWeather.Controls.Add(this.label2, 2, 4);
            this.tableLayoutPanelWeather.Controls.Add(this.label22, 0, 3);
            this.tableLayoutPanelWeather.Controls.Add(this.labelWindSpeedValue, 1, 3);
            this.tableLayoutPanelWeather.Location = new System.Drawing.Point(8, 19);
            this.tableLayoutPanelWeather.Name = "tableLayoutPanelWeather";
            this.tableLayoutPanelWeather.RowCount = 5;
            this.tableLayoutPanelWeather.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanelWeather.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanelWeather.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanelWeather.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanelWeather.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanelWeather.Size = new System.Drawing.Size(369, 150);
            this.tableLayoutPanelWeather.TabIndex = 0;
            // 
            // label12
            // 
            this.label12.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label12.Location = new System.Drawing.Point(14, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(80, 30);
            this.label12.TabIndex = 0;
            this.label12.Text = "Cloud Cover:";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCloudCoverValue
            // 
            this.labelCloudCoverValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCloudCoverValue.AutoSize = true;
            this.labelCloudCoverValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelCloudCoverValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelCloudCoverValue.Location = new System.Drawing.Point(100, 0);
            this.labelCloudCoverValue.Name = "labelCloudCoverValue";
            this.labelCloudCoverValue.Size = new System.Drawing.Size(83, 30);
            this.labelCloudCoverValue.TabIndex = 11;
            this.labelCloudCoverValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label18
            // 
            this.label18.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label18.AutoSize = true;
            this.label18.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label18.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label18.Location = new System.Drawing.Point(215, 0);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(68, 30);
            this.label18.TabIndex = 4;
            this.label18.Text = "Rain Rate:";
            this.label18.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelRainRateValue
            // 
            this.labelRainRateValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelRainRateValue.AutoSize = true;
            this.labelRainRateValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelRainRateValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelRainRateValue.Location = new System.Drawing.Point(289, 0);
            this.labelRainRateValue.Name = "labelRainRateValue";
            this.labelRainRateValue.Size = new System.Drawing.Size(77, 30);
            this.labelRainRateValue.TabIndex = 15;
            this.labelRainRateValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label13
            // 
            this.label13.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label13.Location = new System.Drawing.Point(25, 30);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(69, 30);
            this.label13.TabIndex = 1;
            this.label13.Text = "Dew Point:";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelDewPointValue
            // 
            this.labelDewPointValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDewPointValue.AutoSize = true;
            this.labelDewPointValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelDewPointValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelDewPointValue.Location = new System.Drawing.Point(100, 30);
            this.labelDewPointValue.Name = "labelDewPointValue";
            this.labelDewPointValue.Size = new System.Drawing.Size(83, 30);
            this.labelDewPointValue.TabIndex = 12;
            this.labelDewPointValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label14
            // 
            this.label14.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label14.Location = new System.Drawing.Point(35, 60);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(59, 30);
            this.label14.TabIndex = 2;
            this.label14.Text = "Humidity:";
            this.label14.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelPressureValue
            // 
            this.labelPressureValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelPressureValue.AutoSize = true;
            this.labelPressureValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelPressureValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelPressureValue.Location = new System.Drawing.Point(100, 120);
            this.labelPressureValue.Name = "labelPressureValue";
            this.labelPressureValue.Size = new System.Drawing.Size(83, 30);
            this.labelPressureValue.TabIndex = 14;
            this.labelPressureValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label16
            // 
            this.label16.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label16.Location = new System.Drawing.Point(34, 120);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(60, 30);
            this.label16.TabIndex = 3;
            this.label16.Text = "Pressure:";
            this.label16.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelHumidityValue
            // 
            this.labelHumidityValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelHumidityValue.AutoSize = true;
            this.labelHumidityValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelHumidityValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelHumidityValue.Location = new System.Drawing.Point(100, 60);
            this.labelHumidityValue.Name = "labelHumidityValue";
            this.labelHumidityValue.Size = new System.Drawing.Size(83, 30);
            this.labelHumidityValue.TabIndex = 13;
            this.labelHumidityValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label19
            // 
            this.label19.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label19.AutoSize = true;
            this.label19.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label19.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label19.Location = new System.Drawing.Point(216, 30);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(67, 30);
            this.label19.TabIndex = 5;
            this.label19.Text = "Sky Temp:";
            this.label19.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelSkyTempValue
            // 
            this.labelSkyTempValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelSkyTempValue.AutoSize = true;
            this.labelSkyTempValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelSkyTempValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelSkyTempValue.Location = new System.Drawing.Point(289, 30);
            this.labelSkyTempValue.Name = "labelSkyTempValue";
            this.labelSkyTempValue.Size = new System.Drawing.Size(77, 30);
            this.labelSkyTempValue.TabIndex = 16;
            this.labelSkyTempValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label20
            // 
            this.label20.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label20.AutoSize = true;
            this.label20.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label20.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label20.Location = new System.Drawing.Point(241, 60);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(42, 30);
            this.label20.TabIndex = 6;
            this.label20.Text = "Temp:";
            this.label20.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelTempValue
            // 
            this.labelTempValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelTempValue.AutoSize = true;
            this.labelTempValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelTempValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelTempValue.Location = new System.Drawing.Point(289, 60);
            this.labelTempValue.Name = "labelTempValue";
            this.labelTempValue.Size = new System.Drawing.Size(77, 30);
            this.labelTempValue.TabIndex = 17;
            this.labelTempValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label21
            // 
            this.label21.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label21.AutoSize = true;
            this.label21.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label21.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label21.Location = new System.Drawing.Point(223, 90);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(60, 30);
            this.label21.TabIndex = 7;
            this.label21.Text = "Wind Dir:";
            this.label21.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelWindDirValue
            // 
            this.labelWindDirValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWindDirValue.AutoSize = true;
            this.labelWindDirValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelWindDirValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelWindDirValue.Location = new System.Drawing.Point(289, 90);
            this.labelWindDirValue.Name = "labelWindDirValue";
            this.labelWindDirValue.Size = new System.Drawing.Size(77, 30);
            this.labelWindDirValue.TabIndex = 18;
            this.labelWindDirValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelSunElevationValue
            // 
            this.labelSunElevationValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelSunElevationValue.AutoSize = true;
            this.labelSunElevationValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelSunElevationValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelSunElevationValue.Location = new System.Drawing.Point(289, 120);
            this.labelSunElevationValue.Name = "labelSunElevationValue";
            this.labelSunElevationValue.Size = new System.Drawing.Size(77, 30);
            this.labelSunElevationValue.TabIndex = 21;
            this.labelSunElevationValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label2.Location = new System.Drawing.Point(221, 120);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 30);
            this.label2.TabIndex = 20;
            this.label2.Text = "Sun Elev:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label22
            // 
            this.label22.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label22.AutoSize = true;
            this.label22.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label22.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label22.Location = new System.Drawing.Point(14, 90);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(80, 30);
            this.label22.TabIndex = 8;
            this.label22.Text = "Wind Speed:";
            this.label22.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelWindSpeedValue
            // 
            this.labelWindSpeedValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWindSpeedValue.AutoSize = true;
            this.labelWindSpeedValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelWindSpeedValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelWindSpeedValue.Location = new System.Drawing.Point(100, 90);
            this.labelWindSpeedValue.Name = "labelWindSpeedValue";
            this.labelWindSpeedValue.Size = new System.Drawing.Size(83, 30);
            this.labelWindSpeedValue.TabIndex = 19;
            this.labelWindSpeedValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelWeatherStatus
            // 
            this.labelWeatherStatus.AutoEllipsis = true;
            this.labelWeatherStatus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelWeatherStatus.Font = new System.Drawing.Font("Lucida Sans Unicode", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelWeatherStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelWeatherStatus.Location = new System.Drawing.Point(19, 360);
            this.labelWeatherStatus.Name = "labelWeatherStatus";
            this.labelWeatherStatus.Size = new System.Drawing.Size(377, 20);
            this.labelWeatherStatus.TabIndex = 32;
            this.labelWeatherStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(408, 24);
            this.menuStrip.TabIndex = 35;
            this.menuStrip.Text = "menuStrip";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(92, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem,
            this.safetyOnTheWIse40WikiToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // safetyOnTheWIse40WikiToolStripMenuItem
            // 
            this.safetyOnTheWIse40WikiToolStripMenuItem.Name = "safetyOnTheWIse40WikiToolStripMenuItem";
            this.safetyOnTheWIse40WikiToolStripMenuItem.Size = new System.Drawing.Size(207, 22);
            this.safetyOnTheWIse40WikiToolStripMenuItem.Text = "Safety on the Wise40 wiki";
            this.safetyOnTheWIse40WikiToolStripMenuItem.Click += new System.EventHandler(this.safetyOnTheWIse40WikiToolStripMenuItem_Click);
            // 
            // labelNextCheck
            // 
            this.labelNextCheck.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelNextCheck.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelNextCheck.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelNextCheck.Location = new System.Drawing.Point(208, 138);
            this.labelNextCheck.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.labelNextCheck.Name = "labelNextCheck";
            this.labelNextCheck.Size = new System.Drawing.Size(70, 13);
            this.labelNextCheck.TabIndex = 40;
            this.labelNextCheck.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // timerRefresh
            // 
            this.timerRefresh.Enabled = true;
            this.timerRefresh.Interval = 1000;
            this.timerRefresh.Tick += new System.EventHandler(this.timerRefresh_Tick);
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.DarkOrange;
            this.label5.Location = new System.Drawing.Point(112, 138);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(90, 13);
            this.label5.TabIndex = 41;
            this.label5.Text = "Next check in:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelTitle
            // 
            this.labelTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.labelTitle.ForeColor = System.Drawing.Color.DarkOrange;
            this.labelTitle.Location = new System.Drawing.Point(63, 40);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(280, 42);
            this.labelTitle.TabIndex = 42;
            this.labelTitle.Text = "title";
            this.labelTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelDate
            // 
            this.labelDate.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.labelDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelDate.Location = new System.Drawing.Point(142, 74);
            this.labelDate.Name = "labelDate";
            this.labelDate.Size = new System.Drawing.Size(124, 55);
            this.labelDate.TabIndex = 43;
            this.labelDate.Text = "date";
            this.labelDate.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // RemoteSafetyDashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.ClientSize = new System.Drawing.Size(408, 395);
            this.Controls.Add(this.labelDate);
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.labelNextCheck);
            this.Controls.Add(this.labelWeatherStatus);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.groupBoxWeather);
            this.Controls.Add(this.menuStrip);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip;
            this.MaximizeBox = false;
            this.Name = "RemoteSafetyDashboard";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Wise Remote Safety Dashboard";
            this.groupBoxWeather.ResumeLayout(false);
            this.tableLayoutPanelWeather.ResumeLayout(false);
            this.tableLayoutPanelWeather.PerformLayout();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxWeather;
        private System.Windows.Forms.Label labelWeatherStatus;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelWeather;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label labelCloudCoverValue;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label labelRainRateValue;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label labelDewPointValue;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label labelPressureValue;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label labelHumidityValue;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label labelSkyTempValue;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Label labelTempValue;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label labelWindDirValue;
        private System.Windows.Forms.Label labelSunElevationValue;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Label labelWindSpeedValue;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.Label labelNextCheck;
        private System.Windows.Forms.Timer timerRefresh;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ToolStripMenuItem safetyOnTheWIse40WikiToolStripMenuItem;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Label labelDate;
    }
}

