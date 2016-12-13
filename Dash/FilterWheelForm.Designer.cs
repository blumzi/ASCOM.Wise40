namespace Dash
{
    partial class FilterWheelForm
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
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.labelCurrentWheelValue = new System.Windows.Forms.Label();
            this.labelCurrentPositionValue = new System.Windows.Forms.Label();
            this.buttonIdentify = new System.Windows.Forms.Button();
            this.tableLayoutPanelWheel8 = new System.Windows.Forms.TableLayoutPanel();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.labelWheel8Filter0 = new System.Windows.Forms.Label();
            this.labelWheel8Filter1 = new System.Windows.Forms.Label();
            this.labelWheel8Filter2 = new System.Windows.Forms.Label();
            this.labelWheel8Filter3 = new System.Windows.Forms.Label();
            this.labelWheel8Filter4 = new System.Windows.Forms.Label();
            this.labelWheel8Filter5 = new System.Windows.Forms.Label();
            this.labelWheel8Filter6 = new System.Windows.Forms.Label();
            this.labelWheel8Filter7 = new System.Windows.Forms.Label();
            this.buttonGoTo = new System.Windows.Forms.Button();
            this.textBoxPositionValue = new System.Windows.Forms.TextBox();
            this.buttonPrev = new System.Windows.Forms.Button();
            this.buttonNext = new System.Windows.Forms.Button();
            this.tableLayoutPanelWheel4 = new System.Windows.Forms.TableLayoutPanel();
            this.labelWheel4Filter3 = new System.Windows.Forms.Label();
            this.labelWheel4Filter2 = new System.Windows.Forms.Label();
            this.labelWheel4Filter1 = new System.Windows.Forms.Label();
            this.labelWheel4Filter0 = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.label22 = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.label25 = new System.Windows.Forms.Label();
            this.label30 = new System.Windows.Forms.Label();
            this.labelFilterWheelStatus = new System.Windows.Forms.Label();
            this.timerRefresh = new System.Windows.Forms.Timer(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanelWheel8.SuspendLayout();
            this.tableLayoutPanelWheel4.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(16, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(93, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Current wheel: ";
            // 
            // groupBox1
            // 
            this.groupBox1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.groupBox1.Controls.Add(this.labelFilterWheelStatus);
            this.groupBox1.Controls.Add(this.buttonNext);
            this.groupBox1.Controls.Add(this.buttonPrev);
            this.groupBox1.Controls.Add(this.textBoxPositionValue);
            this.groupBox1.Controls.Add(this.buttonGoTo);
            this.groupBox1.Controls.Add(this.buttonIdentify);
            this.groupBox1.Controls.Add(this.labelCurrentPositionValue);
            this.groupBox1.Controls.Add(this.labelCurrentWheelValue);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.panel1);
            this.groupBox1.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBox1.Location = new System.Drawing.Point(8, 8);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(320, 304);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = " FilterWheel ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(16, 44);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Current position: ";
            // 
            // labelCurrentWheelValue
            // 
            this.labelCurrentWheelValue.AutoSize = true;
            this.labelCurrentWheelValue.Location = new System.Drawing.Point(120, 24);
            this.labelCurrentWheelValue.Name = "labelCurrentWheelValue";
            this.labelCurrentWheelValue.Size = new System.Drawing.Size(51, 13);
            this.labelCurrentWheelValue.TabIndex = 2;
            this.labelCurrentWheelValue.Text = "unknown";
            // 
            // labelCurrentPositionValue
            // 
            this.labelCurrentPositionValue.AutoSize = true;
            this.labelCurrentPositionValue.Location = new System.Drawing.Point(120, 44);
            this.labelCurrentPositionValue.Name = "labelCurrentPositionValue";
            this.labelCurrentPositionValue.Size = new System.Drawing.Size(51, 13);
            this.labelCurrentPositionValue.TabIndex = 3;
            this.labelCurrentPositionValue.Text = "unknown";
            // 
            // buttonIdentify
            // 
            this.buttonIdentify.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonIdentify.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonIdentify.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonIdentify.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonIdentify.Location = new System.Drawing.Point(202, 32);
            this.buttonIdentify.Name = "buttonIdentify";
            this.buttonIdentify.Size = new System.Drawing.Size(75, 23);
            this.buttonIdentify.TabIndex = 21;
            this.buttonIdentify.Text = "Identify";
            this.buttonIdentify.UseVisualStyleBackColor = false;
            this.buttonIdentify.Click += new System.EventHandler(this.buttonIdentify_Click);
            // 
            // tableLayoutPanelWheel8
            // 
            this.tableLayoutPanelWheel8.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanelWheel8.AutoSize = true;
            this.tableLayoutPanelWheel8.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanelWheel8.ColumnCount = 2;
            this.tableLayoutPanelWheel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanelWheel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelWheel8.Controls.Add(this.labelWheel8Filter7, 1, 8);
            this.tableLayoutPanelWheel8.Controls.Add(this.labelWheel8Filter6, 1, 7);
            this.tableLayoutPanelWheel8.Controls.Add(this.labelWheel8Filter5, 1, 6);
            this.tableLayoutPanelWheel8.Controls.Add(this.labelWheel8Filter4, 1, 5);
            this.tableLayoutPanelWheel8.Controls.Add(this.labelWheel8Filter3, 1, 4);
            this.tableLayoutPanelWheel8.Controls.Add(this.labelWheel8Filter2, 1, 3);
            this.tableLayoutPanelWheel8.Controls.Add(this.labelWheel8Filter1, 1, 2);
            this.tableLayoutPanelWheel8.Controls.Add(this.labelWheel8Filter0, 1, 1);
            this.tableLayoutPanelWheel8.Controls.Add(this.label3, 0, 0);
            this.tableLayoutPanelWheel8.Controls.Add(this.label4, 0, 1);
            this.tableLayoutPanelWheel8.Controls.Add(this.label5, 0, 2);
            this.tableLayoutPanelWheel8.Controls.Add(this.label6, 0, 3);
            this.tableLayoutPanelWheel8.Controls.Add(this.label7, 0, 4);
            this.tableLayoutPanelWheel8.Controls.Add(this.label8, 0, 5);
            this.tableLayoutPanelWheel8.Controls.Add(this.label10, 0, 6);
            this.tableLayoutPanelWheel8.Controls.Add(this.label13, 0, 7);
            this.tableLayoutPanelWheel8.Controls.Add(this.label9, 0, 8);
            this.tableLayoutPanelWheel8.Controls.Add(this.label14, 1, 0);
            this.tableLayoutPanelWheel8.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelWheel8.Name = "tableLayoutPanelWheel8";
            this.tableLayoutPanelWheel8.RowCount = 9;
            this.tableLayoutPanelWheel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelWheel8.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelWheel8.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelWheel8.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelWheel8.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelWheel8.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelWheel8.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelWheel8.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelWheel8.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelWheel8.Size = new System.Drawing.Size(81, 124);
            this.tableLayoutPanelWheel8.TabIndex = 22;
            this.tableLayoutPanelWheel8.Visible = false;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(3, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(34, 20);
            this.label3.TabIndex = 0;
            this.label3.Text = "Pos";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(3, 20);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(34, 13);
            this.label4.TabIndex = 1;
            this.label4.Text = "1";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(3, 33);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(34, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "2";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(3, 46);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(34, 13);
            this.label6.TabIndex = 3;
            this.label6.Text = "3";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(3, 59);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(34, 13);
            this.label7.TabIndex = 4;
            this.label7.Text = "4";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(3, 72);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(34, 13);
            this.label8.TabIndex = 5;
            this.label8.Text = "5";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(3, 85);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(34, 13);
            this.label10.TabIndex = 8;
            this.label10.Text = "6";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label13
            // 
            this.label13.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.Location = new System.Drawing.Point(3, 98);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(34, 13);
            this.label13.TabIndex = 11;
            this.label13.Text = "7";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(3, 111);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(34, 13);
            this.label9.TabIndex = 6;
            this.label9.Text = "8";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label14
            // 
            this.label14.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.Location = new System.Drawing.Point(43, 0);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(35, 20);
            this.label14.TabIndex = 7;
            this.label14.Text = "Filter";
            this.label14.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelWheel8Filter0
            // 
            this.labelWheel8Filter0.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWheel8Filter0.AutoSize = true;
            this.labelWheel8Filter0.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelWheel8Filter0.Location = new System.Drawing.Point(43, 20);
            this.labelWheel8Filter0.Name = "labelWheel8Filter0";
            this.labelWheel8Filter0.Size = new System.Drawing.Size(35, 13);
            this.labelWheel8Filter0.TabIndex = 23;
            this.labelWheel8Filter0.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelWheel8Filter1
            // 
            this.labelWheel8Filter1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWheel8Filter1.AutoSize = true;
            this.labelWheel8Filter1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelWheel8Filter1.Location = new System.Drawing.Point(43, 33);
            this.labelWheel8Filter1.Name = "labelWheel8Filter1";
            this.labelWheel8Filter1.Size = new System.Drawing.Size(35, 13);
            this.labelWheel8Filter1.TabIndex = 24;
            this.labelWheel8Filter1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelWheel8Filter2
            // 
            this.labelWheel8Filter2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWheel8Filter2.AutoSize = true;
            this.labelWheel8Filter2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelWheel8Filter2.Location = new System.Drawing.Point(43, 46);
            this.labelWheel8Filter2.Name = "labelWheel8Filter2";
            this.labelWheel8Filter2.Size = new System.Drawing.Size(35, 13);
            this.labelWheel8Filter2.TabIndex = 25;
            this.labelWheel8Filter2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelWheel8Filter3
            // 
            this.labelWheel8Filter3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWheel8Filter3.AutoSize = true;
            this.labelWheel8Filter3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelWheel8Filter3.Location = new System.Drawing.Point(43, 59);
            this.labelWheel8Filter3.Name = "labelWheel8Filter3";
            this.labelWheel8Filter3.Size = new System.Drawing.Size(35, 13);
            this.labelWheel8Filter3.TabIndex = 26;
            this.labelWheel8Filter3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelWheel8Filter4
            // 
            this.labelWheel8Filter4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWheel8Filter4.AutoSize = true;
            this.labelWheel8Filter4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelWheel8Filter4.Location = new System.Drawing.Point(43, 72);
            this.labelWheel8Filter4.Name = "labelWheel8Filter4";
            this.labelWheel8Filter4.Size = new System.Drawing.Size(35, 13);
            this.labelWheel8Filter4.TabIndex = 27;
            this.labelWheel8Filter4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelWheel8Filter5
            // 
            this.labelWheel8Filter5.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWheel8Filter5.AutoSize = true;
            this.labelWheel8Filter5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelWheel8Filter5.Location = new System.Drawing.Point(43, 85);
            this.labelWheel8Filter5.Name = "labelWheel8Filter5";
            this.labelWheel8Filter5.Size = new System.Drawing.Size(35, 13);
            this.labelWheel8Filter5.TabIndex = 28;
            this.labelWheel8Filter5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelWheel8Filter6
            // 
            this.labelWheel8Filter6.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWheel8Filter6.AutoSize = true;
            this.labelWheel8Filter6.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelWheel8Filter6.Location = new System.Drawing.Point(43, 98);
            this.labelWheel8Filter6.Name = "labelWheel8Filter6";
            this.labelWheel8Filter6.Size = new System.Drawing.Size(35, 13);
            this.labelWheel8Filter6.TabIndex = 29;
            this.labelWheel8Filter6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelWheel8Filter7
            // 
            this.labelWheel8Filter7.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWheel8Filter7.AutoSize = true;
            this.labelWheel8Filter7.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelWheel8Filter7.Location = new System.Drawing.Point(43, 111);
            this.labelWheel8Filter7.Name = "labelWheel8Filter7";
            this.labelWheel8Filter7.Size = new System.Drawing.Size(35, 13);
            this.labelWheel8Filter7.TabIndex = 30;
            this.labelWheel8Filter7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonGoTo
            // 
            this.buttonGoTo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonGoTo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonGoTo.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonGoTo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonGoTo.Location = new System.Drawing.Point(123, 70);
            this.buttonGoTo.Name = "buttonGoTo";
            this.buttonGoTo.Size = new System.Drawing.Size(75, 23);
            this.buttonGoTo.TabIndex = 23;
            this.buttonGoTo.Text = "GoTo";
            this.buttonGoTo.UseVisualStyleBackColor = false;
            this.buttonGoTo.Click += new System.EventHandler(this.buttonGoTo_Click);
            // 
            // textBoxPositionValue
            // 
            this.textBoxPositionValue.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.textBoxPositionValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxPositionValue.Location = new System.Drawing.Point(142, 96);
            this.textBoxPositionValue.Name = "textBoxPositionValue";
            this.textBoxPositionValue.Size = new System.Drawing.Size(36, 20);
            this.textBoxPositionValue.TabIndex = 26;
            this.textBoxPositionValue.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // buttonPrev
            // 
            this.buttonPrev.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonPrev.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonPrev.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonPrev.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonPrev.Location = new System.Drawing.Point(44, 83);
            this.buttonPrev.Name = "buttonPrev";
            this.buttonPrev.Size = new System.Drawing.Size(75, 23);
            this.buttonPrev.TabIndex = 27;
            this.buttonPrev.Text = "Prev";
            this.buttonPrev.UseVisualStyleBackColor = false;
            this.buttonPrev.Click += new System.EventHandler(this.buttonPrev_Click);
            // 
            // buttonNext
            // 
            this.buttonNext.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonNext.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonNext.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonNext.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonNext.Location = new System.Drawing.Point(202, 83);
            this.buttonNext.Name = "buttonNext";
            this.buttonNext.Size = new System.Drawing.Size(75, 23);
            this.buttonNext.TabIndex = 28;
            this.buttonNext.Text = "Next";
            this.buttonNext.UseVisualStyleBackColor = false;
            this.buttonNext.Click += new System.EventHandler(this.buttonNext_Click);
            // 
            // tableLayoutPanelWheel4
            // 
            this.tableLayoutPanelWheel4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanelWheel4.AutoSize = true;
            this.tableLayoutPanelWheel4.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanelWheel4.ColumnCount = 2;
            this.tableLayoutPanelWheel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanelWheel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelWheel4.Controls.Add(this.labelWheel4Filter3, 1, 4);
            this.tableLayoutPanelWheel4.Controls.Add(this.labelWheel4Filter2, 1, 3);
            this.tableLayoutPanelWheel4.Controls.Add(this.labelWheel4Filter1, 1, 2);
            this.tableLayoutPanelWheel4.Controls.Add(this.labelWheel4Filter0, 1, 1);
            this.tableLayoutPanelWheel4.Controls.Add(this.label21, 0, 0);
            this.tableLayoutPanelWheel4.Controls.Add(this.label22, 0, 1);
            this.tableLayoutPanelWheel4.Controls.Add(this.label23, 0, 2);
            this.tableLayoutPanelWheel4.Controls.Add(this.label24, 0, 3);
            this.tableLayoutPanelWheel4.Controls.Add(this.label25, 0, 4);
            this.tableLayoutPanelWheel4.Controls.Add(this.label30, 1, 0);
            this.tableLayoutPanelWheel4.Location = new System.Drawing.Point(128, 40);
            this.tableLayoutPanelWheel4.Name = "tableLayoutPanelWheel4";
            this.tableLayoutPanelWheel4.RowCount = 6;
            this.tableLayoutPanelWheel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelWheel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelWheel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelWheel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelWheel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelWheel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelWheel4.Size = new System.Drawing.Size(351, 85);
            this.tableLayoutPanelWheel4.TabIndex = 23;
            this.tableLayoutPanelWheel4.Visible = false;
            // 
            // labelWheel4Filter3
            // 
            this.labelWheel4Filter3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWheel4Filter3.AutoSize = true;
            this.labelWheel4Filter3.Location = new System.Drawing.Point(43, 52);
            this.labelWheel4Filter3.Name = "labelWheel4Filter3";
            this.labelWheel4Filter3.Size = new System.Drawing.Size(305, 13);
            this.labelWheel4Filter3.TabIndex = 26;
            this.labelWheel4Filter3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelWheel4Filter2
            // 
            this.labelWheel4Filter2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWheel4Filter2.AutoSize = true;
            this.labelWheel4Filter2.Location = new System.Drawing.Point(43, 39);
            this.labelWheel4Filter2.Name = "labelWheel4Filter2";
            this.labelWheel4Filter2.Size = new System.Drawing.Size(305, 13);
            this.labelWheel4Filter2.TabIndex = 25;
            this.labelWheel4Filter2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelWheel4Filter1
            // 
            this.labelWheel4Filter1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWheel4Filter1.AutoSize = true;
            this.labelWheel4Filter1.Location = new System.Drawing.Point(43, 26);
            this.labelWheel4Filter1.Name = "labelWheel4Filter1";
            this.labelWheel4Filter1.Size = new System.Drawing.Size(305, 13);
            this.labelWheel4Filter1.TabIndex = 24;
            this.labelWheel4Filter1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelWheel4Filter0
            // 
            this.labelWheel4Filter0.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWheel4Filter0.AutoSize = true;
            this.labelWheel4Filter0.Location = new System.Drawing.Point(43, 13);
            this.labelWheel4Filter0.Name = "labelWheel4Filter0";
            this.labelWheel4Filter0.Size = new System.Drawing.Size(305, 13);
            this.labelWheel4Filter0.TabIndex = 23;
            this.labelWheel4Filter0.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label21
            // 
            this.label21.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label21.AutoSize = true;
            this.label21.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label21.Location = new System.Drawing.Point(3, 0);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(34, 13);
            this.label21.TabIndex = 0;
            this.label21.Text = "Pos";
            this.label21.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label22
            // 
            this.label22.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label22.AutoSize = true;
            this.label22.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label22.Location = new System.Drawing.Point(3, 13);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(34, 13);
            this.label22.TabIndex = 1;
            this.label22.Text = "1";
            this.label22.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label23
            // 
            this.label23.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label23.AutoSize = true;
            this.label23.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label23.Location = new System.Drawing.Point(3, 26);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(34, 13);
            this.label23.TabIndex = 2;
            this.label23.Text = "2";
            this.label23.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label24
            // 
            this.label24.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label24.AutoSize = true;
            this.label24.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label24.Location = new System.Drawing.Point(3, 39);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(34, 13);
            this.label24.TabIndex = 3;
            this.label24.Text = "3";
            this.label24.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label25
            // 
            this.label25.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label25.AutoSize = true;
            this.label25.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label25.Location = new System.Drawing.Point(3, 52);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(34, 13);
            this.label25.TabIndex = 4;
            this.label25.Text = "4";
            this.label25.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label30
            // 
            this.label30.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.label30.AutoSize = true;
            this.label30.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label30.Location = new System.Drawing.Point(43, 0);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(35, 13);
            this.label30.TabIndex = 7;
            this.label30.Text = "Filter";
            this.label30.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelFilterWheelStatus
            // 
            this.labelFilterWheelStatus.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, System.Drawing.FontStyle.Italic);
            this.labelFilterWheelStatus.Location = new System.Drawing.Point(16, 128);
            this.labelFilterWheelStatus.Name = "labelFilterWheelStatus";
            this.labelFilterWheelStatus.Size = new System.Drawing.Size(296, 23);
            this.labelFilterWheelStatus.TabIndex = 29;
            this.labelFilterWheelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // timerRefresh
            // 
            this.timerRefresh.Tick += new System.EventHandler(this.timerRefresh_Tick);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.tableLayoutPanelWheel4);
            this.panel1.Controls.Add(this.tableLayoutPanelWheel8);
            this.panel1.Location = new System.Drawing.Point(8, 160);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(304, 136);
            this.panel1.TabIndex = 30;
            // 
            // FilterWheelForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.ClientSize = new System.Drawing.Size(336, 320);
            this.Controls.Add(this.groupBox1);
            this.ForeColor = System.Drawing.Color.DarkOrange;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FilterWheelForm";
            this.ShowIcon = false;
            this.Text = "FilterWheel";
            this.VisibleChanged += new System.EventHandler(this.FilterWheelForm_VisibleChanged);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tableLayoutPanelWheel8.ResumeLayout(false);
            this.tableLayoutPanelWheel8.PerformLayout();
            this.tableLayoutPanelWheel4.ResumeLayout(false);
            this.tableLayoutPanelWheel4.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label labelCurrentPositionValue;
        private System.Windows.Forms.Label labelCurrentWheelValue;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonIdentify;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelWheel8;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label labelWheel8Filter7;
        private System.Windows.Forms.Label labelWheel8Filter6;
        private System.Windows.Forms.Label labelWheel8Filter5;
        private System.Windows.Forms.Label labelWheel8Filter4;
        private System.Windows.Forms.Label labelWheel8Filter3;
        private System.Windows.Forms.Label labelWheel8Filter2;
        private System.Windows.Forms.Label labelWheel8Filter1;
        private System.Windows.Forms.Label labelWheel8Filter0;
        private System.Windows.Forms.Button buttonGoTo;
        private System.Windows.Forms.Button buttonNext;
        private System.Windows.Forms.Button buttonPrev;
        private System.Windows.Forms.TextBox textBoxPositionValue;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelWheel4;
        private System.Windows.Forms.Label labelWheel4Filter3;
        private System.Windows.Forms.Label labelWheel4Filter2;
        private System.Windows.Forms.Label labelWheel4Filter1;
        private System.Windows.Forms.Label labelWheel4Filter0;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.Label label30;
        private System.Windows.Forms.Label labelFilterWheelStatus;
        private System.Windows.Forms.Timer timerRefresh;
        private System.Windows.Forms.Panel panel1;
    }
}