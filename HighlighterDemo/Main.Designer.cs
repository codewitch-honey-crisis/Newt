namespace HighlighterDemo
{
	partial class Main
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
			this.EditBox = new System.Windows.Forms.RichTextBox();
			this.SuspendLayout();
			// 
			// EditBox
			// 
			this.EditBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.EditBox.DetectUrls = false;
			this.EditBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.EditBox.Location = new System.Drawing.Point(0, 0);
			this.EditBox.Name = "EditBox";
			this.EditBox.ShowSelectionMargin = true;
			this.EditBox.Size = new System.Drawing.Size(800, 450);
			this.EditBox.TabIndex = 0;
			this.EditBox.Text = "";
			this.EditBox.WordWrap = false;
			this.EditBox.TextChanged += new System.EventHandler(this.Main_TextChanged);
			// 
			// Main
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.EditBox);
			this.Name = "Main";
			this.Text = "EBNF Syntax Highlighter (Ctrl-V Paste)";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.RichTextBox EditBox;
	}
}

