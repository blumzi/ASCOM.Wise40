namespace ASCOM.Wise40.Boltwood
{
    partial class SetupDialogForm
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
            this.cmdOK = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.picASCOM = new System.Windows.Forms.PictureBox();
            this.openFileDialog0 = new System.Windows.Forms.OpenFileDialog();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonChoose0 = new System.Windows.Forms.Button();
            this.buttonChoose1 = new System.Windows.Forms.Button();
            this.buttonChoose2 = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.checkBox4 = new System.Windows.Forms.CheckBox();
            this.checkBox5 = new System.Windows.Forms.CheckBox();
            this.checkBox0 = new System.Windows.Forms.CheckBox();
            this.comboBoxMethod0 = new System.Windows.Forms.ComboBox();
            this.comboBoxMethod1 = new System.Windows.Forms.ComboBox();
            this.comboBoxMethod2 = new System.Windows.Forms.ComboBox();
            this.comboBoxMethod3 = new System.Windows.Forms.ComboBox();
            this.comboBoxMethod4 = new System.Windows.Forms.ComboBox();
            this.comboBoxMethod5 = new System.Windows.Forms.ComboBox();
            this.buttonChoose3 = new System.Windows.Forms.Button();
            this.buttonChoose4 = new System.Windows.Forms.Button();
            this.buttonChoose5 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.labelPath0 = new System.Windows.Forms.Label();
            this.labelPath1 = new System.Windows.Forms.Label();
            this.labelPath2 = new System.Windows.Forms.Label();
            this.labelPath3 = new System.Windows.Forms.Label();
            this.labelPath4 = new System.Windows.Forms.Label();
            this.labelPath5 = new System.Windows.Forms.Label();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.openFileDialog2 = new System.Windows.Forms.OpenFileDialog();
            this.openFileDialog3 = new System.Windows.Forms.OpenFileDialog();
            this.openFileDialog4 = new System.Windows.Forms.OpenFileDialog();
            this.openFileDialog5 = new System.Windows.Forms.OpenFileDialog();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmdOK
            // 
            this.cmdOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.cmdOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmdOK.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.cmdOK.Location = new System.Drawing.Point(663, 224);
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Size = new System.Drawing.Size(59, 24);
            this.cmdOK.TabIndex = 0;
            this.cmdOK.Text = "OK";
            this.cmdOK.UseVisualStyleBackColor = false;
            this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
            // 
            // cmdCancel
            // 
            this.cmdCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cmdCancel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.cmdCancel.Location = new System.Drawing.Point(663, 254);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(59, 25);
            this.cmdCancel.TabIndex = 1;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = false;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.label1.Location = new System.Drawing.Point(12, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(266, 49);
            this.label1.TabIndex = 2;
            this.label1.Text = "Multi-station ASCOM driver for \r\nthe Boltwood CloudSensor\r\nweather station";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // picASCOM
            // 
            this.picASCOM.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.picASCOM.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picASCOM.Image = global::ASCOM.Wise40.Boltwood.Properties.Resources.ASCOM;
            this.picASCOM.Location = new System.Drawing.Point(674, 9);
            this.picASCOM.Name = "picASCOM";
            this.picASCOM.Size = new System.Drawing.Size(48, 56);
            this.picASCOM.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.picASCOM.TabIndex = 3;
            this.picASCOM.TabStop = false;
            this.picASCOM.Click += new System.EventHandler(this.BrowseToAscom);
            this.picASCOM.DoubleClick += new System.EventHandler(this.BrowseToAscom);
            // 
            // openFileDialog0
            // 
            this.openFileDialog0.Title = " ClarityII data file ";
            this.openFileDialog0.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog0_FileOk);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 88F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.label4, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonChoose0, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.buttonChoose1, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.buttonChoose2, 2, 3);
            this.tableLayoutPanel1.Controls.Add(this.checkBox1, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.checkBox2, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.checkBox3, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.checkBox4, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.checkBox5, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.checkBox0, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.buttonChoose3, 2, 4);
            this.tableLayoutPanel1.Controls.Add(this.buttonChoose4, 2, 5);
            this.tableLayoutPanel1.Controls.Add(this.buttonChoose5, 2, 6);
            this.tableLayoutPanel1.Controls.Add(this.label2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxMethod0, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxMethod1, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxMethod2, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxMethod3, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxMethod4, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxMethod5, 1, 6);
            this.tableLayoutPanel1.Controls.Add(this.label3, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelPath0, 3, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelPath1, 3, 2);
            this.tableLayoutPanel1.Controls.Add(this.labelPath2, 3, 3);
            this.tableLayoutPanel1.Controls.Add(this.labelPath3, 3, 4);
            this.tableLayoutPanel1.Controls.Add(this.labelPath4, 3, 5);
            this.tableLayoutPanel1.Controls.Add(this.labelPath5, 3, 6);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(15, 83);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 8;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(628, 194);
            this.tableLayoutPanel1.TabIndex = 9;
            // 
            // buttonChoose0
            // 
            this.buttonChoose0.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonChoose0.Location = new System.Drawing.Point(181, 23);
            this.buttonChoose0.Name = "buttonChoose0";
            this.buttonChoose0.Size = new System.Drawing.Size(75, 23);
            this.buttonChoose0.TabIndex = 10;
            this.buttonChoose0.Text = "Choose";
            this.toolTip1.SetToolTip(this.buttonChoose0, "Select a data file");
            this.buttonChoose0.UseVisualStyleBackColor = false;
            this.buttonChoose0.Click += new System.EventHandler(this.buttonChooseDataFile0_Click);
            // 
            // buttonChoose1
            // 
            this.buttonChoose1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonChoose1.Location = new System.Drawing.Point(181, 52);
            this.buttonChoose1.Name = "buttonChoose1";
            this.buttonChoose1.Size = new System.Drawing.Size(75, 21);
            this.buttonChoose1.TabIndex = 12;
            this.buttonChoose1.Text = "Choose";
            this.toolTip1.SetToolTip(this.buttonChoose1, "Select a data file");
            this.buttonChoose1.UseVisualStyleBackColor = false;
            this.buttonChoose1.Click += new System.EventHandler(this.buttonChooseDataFile1_Click);
            // 
            // buttonChoose2
            // 
            this.buttonChoose2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonChoose2.Enabled = false;
            this.buttonChoose2.Location = new System.Drawing.Point(181, 81);
            this.buttonChoose2.Name = "buttonChoose2";
            this.buttonChoose2.Size = new System.Drawing.Size(75, 21);
            this.buttonChoose2.TabIndex = 11;
            this.buttonChoose2.Text = "Choose";
            this.toolTip1.SetToolTip(this.buttonChoose2, "Select a data file");
            this.buttonChoose2.UseVisualStyleBackColor = false;
            this.buttonChoose2.Click += new System.EventHandler(this.buttonChooseDataFile2_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Checked = true;
            this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox1.Location = new System.Drawing.Point(3, 52);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(45, 17);
            this.checkBox1.TabIndex = 15;
            this.checkBox1.Text = "C28";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Enabled = false;
            this.checkBox2.Location = new System.Drawing.Point(3, 81);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(81, 17);
            this.checkBox2.TabIndex = 16;
            this.checkBox2.Text = "Weizzmann";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // checkBox3
            // 
            this.checkBox3.AutoSize = true;
            this.checkBox3.Enabled = false;
            this.checkBox3.Location = new System.Drawing.Point(3, 110);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(66, 17);
            this.checkBox3.TabIndex = 17;
            this.checkBox3.Text = "Korean1";
            this.checkBox3.UseVisualStyleBackColor = true;
            // 
            // checkBox4
            // 
            this.checkBox4.AutoSize = true;
            this.checkBox4.Enabled = false;
            this.checkBox4.Location = new System.Drawing.Point(3, 139);
            this.checkBox4.Name = "checkBox4";
            this.checkBox4.Size = new System.Drawing.Size(66, 17);
            this.checkBox4.TabIndex = 18;
            this.checkBox4.Text = "Korean2";
            this.checkBox4.UseVisualStyleBackColor = true;
            // 
            // checkBox5
            // 
            this.checkBox5.AutoSize = true;
            this.checkBox5.Enabled = false;
            this.checkBox5.Location = new System.Drawing.Point(3, 168);
            this.checkBox5.Name = "checkBox5";
            this.checkBox5.Size = new System.Drawing.Size(66, 17);
            this.checkBox5.TabIndex = 19;
            this.checkBox5.Text = "Korean3";
            this.checkBox5.UseVisualStyleBackColor = true;
            // 
            // checkBox0
            // 
            this.checkBox0.AutoSize = true;
            this.checkBox0.Checked = true;
            this.checkBox0.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox0.Location = new System.Drawing.Point(3, 23);
            this.checkBox0.Name = "checkBox0";
            this.checkBox0.Size = new System.Drawing.Size(45, 17);
            this.checkBox0.TabIndex = 14;
            this.checkBox0.Text = "C18";
            this.checkBox0.UseVisualStyleBackColor = true;
            // 
            // comboBoxMethod0
            // 
            this.comboBoxMethod0.Enabled = false;
            this.comboBoxMethod0.FormattingEnabled = true;
            this.comboBoxMethod0.Items.AddRange(new object[] {
            "ClarityII",
            "Weizmann",
            "Korean"});
            this.comboBoxMethod0.Location = new System.Drawing.Point(93, 23);
            this.comboBoxMethod0.Name = "comboBoxMethod0";
            this.comboBoxMethod0.Size = new System.Drawing.Size(77, 21);
            this.comboBoxMethod0.TabIndex = 21;
            this.comboBoxMethod0.Text = "ClarityII";
            // 
            // comboBoxMethod1
            // 
            this.comboBoxMethod1.Enabled = false;
            this.comboBoxMethod1.FormattingEnabled = true;
            this.comboBoxMethod1.Items.AddRange(new object[] {
            "ClarityII",
            "Weizmann",
            "Korean"});
            this.comboBoxMethod1.Location = new System.Drawing.Point(93, 52);
            this.comboBoxMethod1.Name = "comboBoxMethod1";
            this.comboBoxMethod1.Size = new System.Drawing.Size(77, 21);
            this.comboBoxMethod1.TabIndex = 22;
            this.comboBoxMethod1.Text = "ClarityII";
            // 
            // comboBoxMethod2
            // 
            this.comboBoxMethod2.Enabled = false;
            this.comboBoxMethod2.FormattingEnabled = true;
            this.comboBoxMethod2.Items.AddRange(new object[] {
            "ClarityII",
            "Weizmann",
            "Korean"});
            this.comboBoxMethod2.Location = new System.Drawing.Point(93, 81);
            this.comboBoxMethod2.Name = "comboBoxMethod2";
            this.comboBoxMethod2.Size = new System.Drawing.Size(77, 21);
            this.comboBoxMethod2.TabIndex = 23;
            this.comboBoxMethod2.Text = "Weizmann";
            // 
            // comboBoxMethod3
            // 
            this.comboBoxMethod3.Enabled = false;
            this.comboBoxMethod3.FormattingEnabled = true;
            this.comboBoxMethod3.Items.AddRange(new object[] {
            "ClarityII",
            "Weizmann",
            "Korean"});
            this.comboBoxMethod3.Location = new System.Drawing.Point(93, 110);
            this.comboBoxMethod3.Name = "comboBoxMethod3";
            this.comboBoxMethod3.Size = new System.Drawing.Size(77, 21);
            this.comboBoxMethod3.TabIndex = 24;
            this.comboBoxMethod3.Text = "Korean";
            // 
            // comboBoxMethod4
            // 
            this.comboBoxMethod4.Enabled = false;
            this.comboBoxMethod4.FormattingEnabled = true;
            this.comboBoxMethod4.Items.AddRange(new object[] {
            "ClarityII",
            "Weizmann",
            "Korean"});
            this.comboBoxMethod4.Location = new System.Drawing.Point(93, 139);
            this.comboBoxMethod4.Name = "comboBoxMethod4";
            this.comboBoxMethod4.Size = new System.Drawing.Size(77, 21);
            this.comboBoxMethod4.TabIndex = 26;
            this.comboBoxMethod4.Text = "Korean";
            // 
            // comboBoxMethod5
            // 
            this.comboBoxMethod5.Enabled = false;
            this.comboBoxMethod5.FormattingEnabled = true;
            this.comboBoxMethod5.Items.AddRange(new object[] {
            "ClarityII",
            "Weizmann",
            "Korean"});
            this.comboBoxMethod5.Location = new System.Drawing.Point(93, 168);
            this.comboBoxMethod5.Name = "comboBoxMethod5";
            this.comboBoxMethod5.Size = new System.Drawing.Size(77, 21);
            this.comboBoxMethod5.TabIndex = 25;
            this.comboBoxMethod5.Text = "Korean";
            // 
            // buttonChoose3
            // 
            this.buttonChoose3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonChoose3.Enabled = false;
            this.buttonChoose3.Location = new System.Drawing.Point(181, 110);
            this.buttonChoose3.Name = "buttonChoose3";
            this.buttonChoose3.Size = new System.Drawing.Size(75, 21);
            this.buttonChoose3.TabIndex = 27;
            this.buttonChoose3.Text = "Choose";
            this.toolTip1.SetToolTip(this.buttonChoose3, "Select a data file");
            this.buttonChoose3.UseVisualStyleBackColor = false;
            this.buttonChoose3.Click += new System.EventHandler(this.buttonChooseDataFile3_Click);
            // 
            // buttonChoose4
            // 
            this.buttonChoose4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonChoose4.Enabled = false;
            this.buttonChoose4.Location = new System.Drawing.Point(181, 139);
            this.buttonChoose4.Name = "buttonChoose4";
            this.buttonChoose4.Size = new System.Drawing.Size(75, 21);
            this.buttonChoose4.TabIndex = 28;
            this.buttonChoose4.Text = "Choose";
            this.toolTip1.SetToolTip(this.buttonChoose4, "Select a data file");
            this.buttonChoose4.UseVisualStyleBackColor = false;
            this.buttonChoose4.Click += new System.EventHandler(this.buttonChooseDataFile4_Click);
            // 
            // buttonChoose5
            // 
            this.buttonChoose5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonChoose5.Enabled = false;
            this.buttonChoose5.Location = new System.Drawing.Point(181, 168);
            this.buttonChoose5.Name = "buttonChoose5";
            this.buttonChoose5.Size = new System.Drawing.Size(75, 21);
            this.buttonChoose5.TabIndex = 29;
            this.buttonChoose5.Text = "Choose";
            this.toolTip1.SetToolTip(this.buttonChoose5, "Select a data file");
            this.buttonChoose5.UseVisualStyleBackColor = false;
            this.buttonChoose5.Click += new System.EventHandler(this.buttonChooseDataFile5_Click);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.label2.Location = new System.Drawing.Point(93, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(82, 20);
            this.label2.TabIndex = 30;
            this.label2.Text = "Input method";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelPath0
            // 
            this.labelPath0.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelPath0.Location = new System.Drawing.Point(262, 20);
            this.labelPath0.Name = "labelPath0";
            this.labelPath0.Size = new System.Drawing.Size(363, 29);
            this.labelPath0.TabIndex = 31;
            this.labelPath0.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelPath1
            // 
            this.labelPath1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelPath1.Location = new System.Drawing.Point(262, 49);
            this.labelPath1.Name = "labelPath1";
            this.labelPath1.Size = new System.Drawing.Size(363, 29);
            this.labelPath1.TabIndex = 32;
            this.labelPath1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelPath2
            // 
            this.labelPath2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelPath2.Location = new System.Drawing.Point(262, 78);
            this.labelPath2.Name = "labelPath2";
            this.labelPath2.Size = new System.Drawing.Size(363, 29);
            this.labelPath2.TabIndex = 33;
            this.labelPath2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelPath3
            // 
            this.labelPath3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelPath3.Location = new System.Drawing.Point(262, 107);
            this.labelPath3.Name = "labelPath3";
            this.labelPath3.Size = new System.Drawing.Size(363, 29);
            this.labelPath3.TabIndex = 34;
            this.labelPath3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelPath4
            // 
            this.labelPath4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelPath4.Location = new System.Drawing.Point(262, 136);
            this.labelPath4.Name = "labelPath4";
            this.labelPath4.Size = new System.Drawing.Size(363, 29);
            this.labelPath4.TabIndex = 35;
            this.labelPath4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelPath5
            // 
            this.labelPath5.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelPath5.Location = new System.Drawing.Point(262, 165);
            this.labelPath5.Name = "labelPath5";
            this.labelPath5.Size = new System.Drawing.Size(363, 29);
            this.labelPath5.TabIndex = 36;
            this.labelPath5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.Title = " ClarityII data file ";
            this.openFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog1_FileOk);
            // 
            // openFileDialog2
            // 
            this.openFileDialog2.Title = " ClarityII data file ";
            this.openFileDialog2.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog2_FileOk);
            // 
            // openFileDialog3
            // 
            this.openFileDialog3.Title = " ClarityII data file ";
            this.openFileDialog3.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog3_FileOk);
            // 
            // openFileDialog4
            // 
            this.openFileDialog4.Title = " ClarityII data file ";
            this.openFileDialog4.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog4_FileOk);
            // 
            // openFileDialog5
            // 
            this.openFileDialog5.Title = " ClarityII data file ";
            this.openFileDialog5.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog5_FileOk);
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.label3, 2);
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.label3.Location = new System.Drawing.Point(181, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(444, 20);
            this.label3.TabIndex = 37;
            this.label3.Text = "Data file path";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.label4.Location = new System.Drawing.Point(3, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(84, 20);
            this.label4.TabIndex = 38;
            this.label4.Text = "Station";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // SetupDialogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.ClientSize = new System.Drawing.Size(732, 291);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.picASCOM);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdOK);
            this.ForeColor = System.Drawing.Color.DarkOrange;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetupDialogForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Wise40 CloudSensor Setup";
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox picASCOM;
        private System.Windows.Forms.OpenFileDialog openFileDialog0;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button buttonChoose0;
        private System.Windows.Forms.Button buttonChoose1;
        private System.Windows.Forms.Button buttonChoose2;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.CheckBox checkBox4;
        private System.Windows.Forms.CheckBox checkBox5;
        private System.Windows.Forms.CheckBox checkBox0;
        private System.Windows.Forms.ComboBox comboBoxMethod0;
        private System.Windows.Forms.ComboBox comboBoxMethod1;
        private System.Windows.Forms.ComboBox comboBoxMethod2;
        private System.Windows.Forms.ComboBox comboBoxMethod3;
        private System.Windows.Forms.ComboBox comboBoxMethod4;
        private System.Windows.Forms.ComboBox comboBoxMethod5;
        private System.Windows.Forms.Button buttonChoose3;
        private System.Windows.Forms.Button buttonChoose4;
        private System.Windows.Forms.Button buttonChoose5;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelPath0;
        private System.Windows.Forms.Label labelPath1;
        private System.Windows.Forms.Label labelPath2;
        private System.Windows.Forms.Label labelPath3;
        private System.Windows.Forms.Label labelPath4;
        private System.Windows.Forms.Label labelPath5;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.OpenFileDialog openFileDialog2;
        private System.Windows.Forms.OpenFileDialog openFileDialog3;
        private System.Windows.Forms.OpenFileDialog openFileDialog4;
        private System.Windows.Forms.OpenFileDialog openFileDialog5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label3;
    }
}