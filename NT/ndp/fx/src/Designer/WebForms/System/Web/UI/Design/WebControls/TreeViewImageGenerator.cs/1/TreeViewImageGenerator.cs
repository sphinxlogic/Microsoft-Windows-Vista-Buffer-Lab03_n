//------------------------------------------------------------------------------ 
// <copyright file="TreeViewImageGenerator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Drawing;
    using System.Drawing.Design; 
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging; 
    using System.IO; 
    using System.Web.UI.Design.Util;
    using System.Windows.Forms; 

    using WebTreeView = System.Web.UI.WebControls.TreeView;

    /// <include file='doc\TreeViewImageGenerator.uex' path='docs/doc[@for="TreeViewImageGenerator"]/*' /> 
    internal class TreeViewImageGenerator : DesignerForm {
        private static Image defaultMinusImage; 
        private static Image defaultPlusImage; 

        private WebTreeView _treeView; 
        private PictureBox _previewPictureBox;
        private TextBox _previewFrameTextBox;
        private Label _previewLabel;
        private Panel _previewPanel; 
        private Button _okButton;
        private Button _cancelButton; 
        private Label _folderNameLabel; 
        private Label _propertiesLabel;
        private PropertyGrid _propertyGrid; 
        private TextBox _folderNameTextBox;
        private ProgressBar _progressBar;
        private Label _progressBarLabel;
 
        private LineImageInfo _imageInfo;
 
        /// <include file='doc\TreeViewImageGenerator.uex' path='docs/doc[@for="TreeViewImageGenerator.TreeViewImageGenerator"]/*' /> 
        public TreeViewImageGenerator(WebTreeView treeView) : base(treeView.Site) {
 
            _previewPictureBox = new PictureBox();
            _previewLabel = new Label();
            _previewPanel = new Panel();
            _previewFrameTextBox = new TextBox(); 
            _okButton = new Button();
            _cancelButton = new Button(); 
            _folderNameLabel = new Label(); 
            _folderNameTextBox = new TextBox();
            _propertiesLabel = new Label(); 
            _propertyGrid = new System.Windows.Forms.Design.VsPropertyGrid(ServiceProvider);
            _progressBar = new ProgressBar();
            _progressBarLabel = new Label();
            _previewPanel.SuspendLayout(); 
            SuspendLayout();
            // 
            // _previewPictureBox 
            //
            _previewPictureBox.Name = "_previewPictureBox"; 
            _previewPictureBox.SizeMode = PictureBoxSizeMode.Normal;
            _previewPictureBox.TabIndex = 10;
            _previewPictureBox.TabStop = false;
            _previewPictureBox.BackColor = Color.White; 
            //
            // _previewLabel 
            // 
            _previewLabel.Location = new Point(12, 12);
            _previewLabel.Name = "_previewLabel"; 
            _previewLabel.Size = new Size(180, 14);
            _previewLabel.TabIndex = 9;
            _previewLabel.Text = SR.GetString(SR.TreeViewImageGenerator_Preview);
            // 
            // _previewPanel
            // 
            _previewPanel.Anchor = (((AnchorStyles.Top | AnchorStyles.Bottom) 
                | AnchorStyles.Left)
                | AnchorStyles.Right); 
            _previewPanel.AutoScroll = true;
            _previewPanel.BorderStyle = BorderStyle.None;
            _previewPanel.Controls.AddRange(new Control[] { _previewPictureBox });
            _previewPanel.Location = new Point(13, 29); 
            _previewPanel.Name = "_previewPanel";
            _previewPanel.Size = new Size(178, 242); 
            _previewPanel.TabIndex = 11; 
            //
            // _previewFrameTextBox, used to make the border themeable (from UI Design guidelines for Whidbey) 
            //
            _previewFrameTextBox.Multiline = true;
            _previewFrameTextBox.Enabled = false;
            _previewFrameTextBox.TabStop = false; 
            _previewFrameTextBox.Location = new Point(12, 28);
            _previewFrameTextBox.Size = new Size(180, 244); 
            // 
            // _okButton
            // 
            _okButton.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            _okButton.FlatStyle = FlatStyle.System;
            _okButton.Location = new Point(376, 324);
            _okButton.Name = "_okButton"; 
            _okButton.Size = new Size(75, 23);
            _okButton.TabIndex = 20; 
            _okButton.Text = SR.GetString(SR.OKCaption); 
            _okButton.Click += new System.EventHandler(OnOKButtonClick);
            // 
            // _cancelButton
            //
            _cancelButton.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            _cancelButton.FlatStyle = FlatStyle.System; 
            _cancelButton.Location = new Point(456, 324);
            _cancelButton.Name = "_cancelButton"; 
            _cancelButton.Size = new Size(75, 23); 
            _cancelButton.TabIndex = 21;
            _cancelButton.Text = SR.GetString(SR.CancelCaption); 
            _cancelButton.Click += new System.EventHandler(OnCancelButtonClick);
            //
            // _folderNameLabel
            // 
            _folderNameLabel.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left);
            _folderNameLabel.Location = new Point(213, 279); 
            _folderNameLabel.Name = "_folderNameLabel"; 
            _folderNameLabel.Size = new Size(315, 14);
            _folderNameLabel.TabIndex = 17; 
            _folderNameLabel.Text = SR.GetString(SR.TreeViewImageGenerator_FolderName);
            //
            // _folderNameTextBox
            // 
            _folderNameTextBox.Anchor = ((AnchorStyles.Bottom | AnchorStyles.Left)
                | AnchorStyles.Right); 
            _folderNameTextBox.Location = new Point(213, 295); 
            _folderNameTextBox.Name = "_folderNameTextBox";
            _folderNameTextBox.Size = new Size(315, 20); 
            _folderNameTextBox.TabIndex = 18;
            _folderNameTextBox.Text = SR.GetString(SR.TreeViewImageGenerator_DefaultFolderName);
            _folderNameTextBox.WordWrap = false;
            _folderNameTextBox.TextChanged += new EventHandler(OnFolderNameTextBoxTextChanged); 

            // 
            // _progressBarLabel 
            //
            _progressBarLabel.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left); 
            _progressBarLabel.Location = new Point(12, 279);
            _progressBarLabel.Name = "_progressBarLabel";
            _progressBarLabel.Size = new Size(180, 14);
            _progressBarLabel.Text = SR.GetString(SR.TreeViewImageGenerator_ProgressBarName); 
            _progressBarLabel.Visible = false;
            // 
            // _progressBar 
            //
            _progressBar.Location = new Point(12, 295); 
            _progressBar.Size = new Size(180, 16);
            _progressBar.Maximum = 16;
            _progressBar.Minimum = 0;
            _progressBar.Visible = false; 

            // 
            // _propertiesLabel 
            //
            _propertiesLabel.Location = new Point(213, 12); 
            _propertiesLabel.Name = "_propertiesLabel";
            _propertiesLabel.Size = new Size(315, 14);
            _propertiesLabel.TabIndex = 12;
            _propertiesLabel.Text = SR.GetString(SR.TreeViewImageGenerator_Properties); 
            //
            // _propertyGrid 
            // 
            _propertyGrid.Anchor = ((AnchorStyles.Top | AnchorStyles.Bottom)
                | AnchorStyles.Right); 
            _propertyGrid.CommandsVisibleIfAvailable = true;
            _propertyGrid.LargeButtons = false;
            _propertyGrid.LineColor = SystemColors.ScrollBar;
            _propertyGrid.Location = new Point(213, 28); 
            _propertyGrid.Name = "_propertyGrid";
            _propertyGrid.PropertySort = PropertySort.Alphabetical; 
            _propertyGrid.Size = new Size(315, 244); 
            _propertyGrid.TabIndex = 13;
            _propertyGrid.ToolbarVisible = true; 
            _propertyGrid.ViewBackColor = SystemColors.Window;
            _propertyGrid.ViewForeColor = SystemColors.WindowText;
            _propertyGrid.PropertyValueChanged += new PropertyValueChangedEventHandler(OnPropertyGridPropertyValueChanged);
            // 
            // TreeLineImageGenerator
            // 
            AcceptButton = _okButton; 
            CancelButton = _cancelButton;
            ClientSize = new Size(540, 359); 
            Controls.AddRange(new Control[] {
                                                                          _propertyGrid,
                                                                          _propertiesLabel,
                                                                          _progressBar, 
                                                                          _progressBarLabel,
                                                                          _folderNameTextBox, 
                                                                          _folderNameLabel, 
                                                                          _cancelButton,
                                                                          _okButton, 
                                                                          _previewPanel,
                                                                          _previewLabel,
                                                                          _previewFrameTextBox});
            MinimumSize = new Size(540, 359); 
            Name = "TreeLineImageGenerator";
            Text = SR.GetString(SR.TreeViewImageGenerator_Title); 
            Resize += new System.EventHandler(OnFormResize); 
            _previewPanel.ResumeLayout(false);
 
            InitializeForm();

            ResumeLayout(false);
 
            _imageInfo = new LineImageInfo();
            _propertyGrid.SelectedObject = _imageInfo; 
 
            _treeView = treeView;
 
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = null;

            UpdatePreview(); 
        }
 
        private Image DefaultMinusImage { 
            get {
                if (defaultMinusImage == null) { 
                    defaultMinusImage = new Bitmap(typeof(TreeViewImageGenerator), "Minus.gif");
                }
                return defaultMinusImage;
            } 
        }
 
        private Image DefaultPlusImage { 
            get {
                if (defaultPlusImage == null) { 
                    defaultPlusImage = new Bitmap(typeof(TreeViewImageGenerator), "Plus.gif");
                }
                return defaultPlusImage;
            } 
        }
 
        protected override string HelpTopic { 
            get {
                return "net.Asp.TreeView.ImageGenerator"; 
            }
        }

        private void OnCancelButtonClick(object sender, System.EventArgs e) { 
            Close();
        } 
 
        private void OnFormResize(object sender, System.EventArgs e) {
            UpdatePreview(); 
        }

        private void OnOKButtonClick(object sender, System.EventArgs e) {
            string folderName = _folderNameTextBox.Text.Trim(); 
            if (folderName.Length == 0) {
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_MissingFolderName)); 
                return; 
            }
 
            if (folderName.IndexOfAny(Path.GetInvalidPathChars()) != -1) {
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_InvalidFolderName, folderName));
                return;
            } 

            IWebApplication webApplication = (IWebApplication)_treeView.Site.GetService(typeof(IWebApplication)); 
            if (webApplication == null) { 
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_ErrorWriting));
                return; 
            }

            IFolderProjectItem root = (IFolderProjectItem)webApplication.RootProjectItem;
            IProjectItem testItem = webApplication.GetProjectItemFromUrl(Path.Combine("~/", folderName)); 
            if ((testItem != null) && !(testItem is IFolderProjectItem)) {
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_DocumentExists, folderName)); 
                return; 
            }
 
            IFolderProjectItem folder = (IFolderProjectItem)testItem;
            if (folder == null) {
                if (UIServiceHelper.ShowMessage(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_NonExistentFolderName, folderName), SR.GetString(SR.TreeViewImageGenerator_Title), MessageBoxButtons.YesNo) == DialogResult.Yes) {
                    try { 
                        folder = root.AddFolder(folderName);
                    } 
                    catch { 
                        UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_ErrorCreatingFolder, folderName));
                        return; 
                    }
                }
                else {
                    return; 
                }
            } 
 
            Image expandImage = _imageInfo.ExpandImage;
            if (expandImage == null) { 
                expandImage = DefaultPlusImage;
            }

            Image collapseImage = _imageInfo.CollapseImage; 
            if (collapseImage == null) {
                collapseImage = DefaultMinusImage; 
            } 

            Image noExpandImage = _imageInfo.NoExpandImage; 

            int width = _imageInfo.Width;
            if (width < 1) {
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_InvalidValue, "Width")); 
                return;
            } 
 
            int height = _imageInfo.Height;
            if (height < 1) { 
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_InvalidValue, "Height"));
                return;
            }
 
            int lineWidth = _imageInfo.LineWidth;
            if (lineWidth < 1) { 
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_InvalidValue, "LineWidth")); 
                return;
            } 

            int lineStyle = (int)_imageInfo.LineStyle;
            Color lineColor = _imageInfo.LineColor;
 
            _progressBar.Value = 0;
            _progressBar.Visible = true; 
            _progressBarLabel.Visible = true; 

            try { 
                bool overwrite = false;
                bool wroteFile = false;

                // Write out the vertical line image 
                Bitmap bitmap = new Bitmap(width, height);
                Graphics g = Graphics.FromImage(bitmap); 
                g.FillRectangle(new SolidBrush(_imageInfo.TransparentColor), 0, 0, width, height); 
                RenderImage(g, 0, 0, width, height, 'i', lineStyle, lineWidth, lineColor, null);
                string filename = "i.gif"; 
                wroteFile |= SaveTransparentGif(bitmap, folder, "i.gif", ref overwrite);
                _progressBar.Value++;

                // Write out the collapse images 
                string lineTypes = "-rtl ";
                for (int i = 0; i < lineTypes.Length; i++) { 
                    bitmap = new Bitmap(width, height); 
                    g = Graphics.FromImage(bitmap);
                    g.FillRectangle(new SolidBrush(_imageInfo.TransparentColor), 0, 0, width, height); 
                    RenderImage(g, 0, 0, width, height, lineTypes[i], lineStyle, lineWidth, lineColor, collapseImage);
                    g.Dispose();
                    filename = "minus.gif";
                    if (lineTypes[i] == '-') { 
                        filename = "dash" + filename;
                    } 
                    else if (lineTypes[i] == ' ') { 
                    }
                    else { 
                        filename = lineTypes[i] + filename;
                    }
                    wroteFile |= SaveTransparentGif(bitmap, folder, filename, ref overwrite);
                    _progressBar.Value++; 
                }
 
                // Write out the expand images 
                for (int i = 0; i < lineTypes.Length; i++) {
                    bitmap = new Bitmap(width, height); 
                    g = Graphics.FromImage(bitmap);
                    g.FillRectangle(new SolidBrush(_imageInfo.TransparentColor), 0, 0, width, height);
                    RenderImage(g, 0, 0, width, height, lineTypes[i], lineStyle, lineWidth, lineColor, expandImage);
                    g.Dispose(); 
                    filename = "plus.gif";
                    if (lineTypes[i] == '-') { 
                        filename = "dash" + filename; 
                    }
                    else if (lineTypes[i] == ' ') { 
                    }
                    else {
                        filename = lineTypes[i] + filename;
                    } 
                    wroteFile |= SaveTransparentGif(bitmap, folder, filename, ref overwrite);
                    _progressBar.Value++; 
                } 

                // Write out the noExpand images 
                for (int i = 0; i < lineTypes.Length; i++) {
                    bitmap = new Bitmap(width, height);
                    g = Graphics.FromImage(bitmap);
                    g.FillRectangle(new SolidBrush(_imageInfo.TransparentColor), 0, 0, width, height); 
                    RenderImage(g, 0, 0, width, height, lineTypes[i], lineStyle, lineWidth, lineColor, noExpandImage);
                    g.Dispose(); 
                    filename = ".gif"; 
                    if (lineTypes[i] == '-') {
                        filename = "dash" + filename; 
                    }
                    else if (lineTypes[i] == ' ') {
                        filename = "noexpand" + filename;
                    } 
                    else {
                        filename = lineTypes[i] + filename; 
                    } 
                    wroteFile |= SaveTransparentGif(bitmap, folder, filename, ref overwrite);
                    _progressBar.Value++; 
                }

                _progressBar.Visible = false;
                _progressBarLabel.Visible = false; 

                if (wroteFile) { 
                    UIServiceHelper.ShowMessage(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_LineImagesGenerated, folderName)); 
                }
            } 
            catch {
                _progressBar.Visible = false;
                _progressBarLabel.Visible = false;
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_ErrorWriting, folderName)); 
                return;
            } 
 
            _treeView.LineImagesFolder = "~/" + folderName;
 
            DialogResult = DialogResult.OK;
            Close();
        }
 
        private void OnFolderNameTextBoxTextChanged(object sender, EventArgs e) {
            if (_folderNameTextBox.Text.Trim().Length > 0) { 
                _okButton.Enabled = true; 
            }
            else { 
                _okButton.Enabled = false;
            }
        }
 
        private void OnPropertyGridPropertyValueChanged(object s, PropertyValueChangedEventArgs e) {
            UpdatePreview(); 
        } 

        private void RenderImage(Graphics g, int x, int y, int width, int height, char lineType, int lineStyle, int lineWidth, Color lineColor, Image image) { 
            Pen p = new Pen(lineColor, lineWidth);
            switch (lineStyle) {
                case 0:
                    p.DashStyle = DashStyle.Dot; 
                    break;
                case 1: 
                    p.DashStyle = DashStyle.Dash; 
                    break;
                default: 
                    p.DashStyle = DashStyle.Solid;
                    break;
            }
 
            if (lineType == 'i') {
                g.DrawLine(p, x + width / 2, y, x + width / 2, y + height); 
            } 
            else if (lineType == 'r') {
                g.DrawLine(p, x + width / 2, y + height / 2, x + width, y + height / 2); 
                g.DrawLine(p, x + width / 2, y + height / 2, x + width / 2, y + height);
            }
            else if (lineType == 't') {
                g.DrawLine(p, x + width / 2, y, x + width / 2, y + height); 
                g.DrawLine(p, x + width / 2, y + height / 2, x + width, y + height / 2);
            } 
            else if (lineType == 'l') { 
                g.DrawLine(p, x + width / 2, y, x + width / 2, y + height / 2);
                g.DrawLine(p, x + width / 2, y + height / 2, x + width, y + height / 2); 
            }
            else if (lineType == '-') {
                g.DrawLine(p, x + width / 2, y + height / 2, x + width, y + height / 2);
            } 

            if (image != null) { 
                int imgWidth = Math.Min(image.Width, width); 
                int imgHeight = Math.Min(image.Height, height);
                g.DrawImage(image, 
                    x + (width - imgWidth + 1) / 2,
                    y + (height - imgHeight + 1) / 2,
                    imgWidth,
                    imgHeight); 
            }
 
            p.Dispose(); 
        }
 
        private void UpdatePreview() {
            Image expandImage = _imageInfo.ExpandImage;
            if (expandImage == null) {
                expandImage = DefaultPlusImage; 
            }
 
            Image collapseImage = _imageInfo.CollapseImage; 
            if (collapseImage == null) {
                collapseImage = DefaultMinusImage; 
            }

            Image noExpandImage = _imageInfo.NoExpandImage;
 
            int width = _imageInfo.Width;
            if (width < 1) { 
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_InvalidValue, "Width")); 
                return;
            } 

            int height = _imageInfo.Height;
            if (height < 1) {
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_InvalidValue, "Height")); 
                return;
            } 
 
            int lineWidth = _imageInfo.LineWidth;
            if (lineWidth < 1) { 
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_InvalidValue, "LineWidth"));
                return;
            }
 
            int lineStyle = (int)_imageInfo.LineStyle;
            Color lineColor = _imageInfo.LineColor; 
 
            Font font = new Font("Tahoma", 10);
            Graphics tempGraphics = Graphics.FromHwnd(Handle); 
            int totalWidth = width * 2 +
                (int)tempGraphics.MeasureString(SR.GetString(SR.TreeViewImageGenerator_SampleParent, 1), font).Width;
            int textHeight = Math.Max(
                (int)tempGraphics.MeasureString(SR.GetString(SR.TreeViewImageGenerator_SampleParent, 1), font).Height, 
                height);
            tempGraphics.Dispose(); 
            int totalHeight = textHeight * 6; 
            int indent = Math.Max(width, _treeView.NodeIndent);
 
            Bitmap bitmap = new Bitmap(Math.Max(totalWidth, _previewPanel.Width), Math.Max(totalHeight, _previewPanel.Height));
            Graphics g = Graphics.FromImage(bitmap);

            int x = 5; 
            int y = 5;
 
            g.FillRectangle(Brushes.White, x, y, totalWidth, totalHeight); 
            RenderImage(g, x, y, width, height, '-', lineStyle, lineWidth, lineColor, expandImage);
            x += width; 
            g.DrawString(SR.GetString(SR.TreeViewImageGenerator_SampleRoot, 1),
                font,
                Brushes.Black,
                x, 
                y + (height - (g.MeasureString(SR.GetString(SR.TreeViewImageGenerator_SampleRoot, 1), font).Height) + 1) / 2);
 
            y += textHeight; 
            x -= width;
            RenderImage(g, x, y, width, height, 'r', lineStyle, lineWidth, lineColor, collapseImage); 
            x += width;
            g.DrawString(SR.GetString(SR.TreeViewImageGenerator_SampleRoot, 2),
                font,
                Brushes.Black, 
                x,
                y + (height - (g.MeasureString(SR.GetString(SR.TreeViewImageGenerator_SampleRoot, 2), font).Height) + 1) / 2); 
 
            y += textHeight;
            x -= width; 
            RenderImage(g, x, y, width, height, 'i', lineStyle, lineWidth, lineColor, null);
            x += indent;
            RenderImage(g, x, y, width, height, 't', lineStyle, lineWidth, lineColor, expandImage);
            x += width; 
            g.DrawString(SR.GetString(SR.TreeViewImageGenerator_SampleParent, 1),
                font, 
                Brushes.Black, 
                x,
                y + (height - (g.MeasureString(SR.GetString(SR.TreeViewImageGenerator_SampleParent, 1), font).Height) + 1) / 2); 

            y += textHeight;
            x -= width + indent;
            RenderImage(g, x, y, width, height, 'i', lineStyle, lineWidth, lineColor, null); 
            x += indent;
            RenderImage(g, x, y, width, height, 't', lineStyle, lineWidth, lineColor, noExpandImage); 
            x += width; 
            g.DrawString(SR.GetString(SR.TreeViewImageGenerator_SampleLeaf, 1),
                font, 
                Brushes.Black,
                x,
                y + (height - (g.MeasureString(SR.GetString(SR.TreeViewImageGenerator_SampleLeaf, 1), font).Height) + 1) / 2);
 
            y += textHeight;
            x -= width + indent; 
            RenderImage(g, x, y, width, height, 'i', lineStyle, lineWidth, lineColor, null); 
            x += indent;
            RenderImage(g, x, y, width, height, 'l', lineStyle, lineWidth, lineColor, noExpandImage); 
            x += width;
            g.DrawString(SR.GetString(SR.TreeViewImageGenerator_SampleLeaf, 2),
                font,
                Brushes.Black, 
                x,
                y + (height - (g.MeasureString(SR.GetString(SR.TreeViewImageGenerator_SampleLeaf, 2), font).Height) + 1) / 2); 
 
            y += textHeight;
            x -= width + indent; 
            RenderImage(g, x, y, width, height, 'l', lineStyle, lineWidth, lineColor, expandImage);
            x += width;
            g.DrawString(SR.GetString(SR.TreeViewImageGenerator_SampleRoot, 3),
                font, 
                Brushes.Black,
                x, 
                y + (height - (g.MeasureString(SR.GetString(SR.TreeViewImageGenerator_SampleRoot, 3), font).Height) + 1) / 2); 

            g.Dispose(); 

            bitmap.MakeTransparent(_imageInfo.TransparentColor);
            _previewPictureBox.Image = bitmap;
            _previewPictureBox.Width = Math.Max(totalWidth, _previewPanel.Width); 
            _previewPictureBox.Height = Math.Max(totalHeight, _previewPanel.Height);
        } 
 
        private enum LineStyle {
            Dotted = 0, 
            Dashed = 1,
            Solid = 2
        }
 
        private class LineImageInfo {
            private int _height; 
            private int _width; 
            private int _lineWidth;
            private LineStyle _lineStyle; 
            private Color _lineColor;
            private Color _transparentColor;
            private Image _collapseImage;
            private Image _expandImage; 
            private Image _noExpandImage;
 
            private const int MaxSize = 300; 

            public LineImageInfo() { 
                _height = 20;
                _width = 19;
                _lineWidth = 1;
                _lineStyle = LineStyle.Dotted; 
                _lineColor = Color.Black;
                _transparentColor = Color.Magenta; 
            } 

            [DefaultValue(null)] 
            [SRDescription(SR.TreeViewImageGenerator_CollapseImage)]
            public Image CollapseImage {
                get {
                    return _collapseImage; 
                }
                set { 
                    _collapseImage = value; 
                }
            } 

            [DefaultValue(null)]
            [SRDescription(SR.TreeViewImageGenerator_ExpandImage)]
            public Image ExpandImage { 
                get {
                    return _expandImage; 
                } 
                set {
                    _expandImage = value; 
                }
            }

            [SRDescription(SR.TreeViewImageGenerator_LineColor)] 
            public Color LineColor {
                get { 
                    return _lineColor; 
                }
                set { 
                    _lineColor = value;
                }
            }
 
            [SRDescription(SR.TreeViewImageGenerator_LineStyle)]
            public LineStyle LineStyle { 
                get { 
                    return _lineStyle;
                } 
                set {
                    _lineStyle = value;
                }
            } 

            [SRDescription(SR.TreeViewImageGenerator_LineWidth)] 
            public int LineWidth { 
                get {
                    return _lineWidth; 
                }
                set {
                    if (value > MaxSize) {
                        throw new ArgumentOutOfRangeException("value"); 
                    }
                    _lineWidth = value; 
                } 
            }
 
            [SRDescription(SR.TreeViewImageGenerator_LineImageHeight)]
            public int Height {
                get {
                    return _height; 
                }
                set { 
                    if (value > MaxSize) { 
                        throw new ArgumentOutOfRangeException("value");
                    } 
                    _height = value;
                }
            }
 
            [DefaultValue(null)]
            [SRDescription(SR.TreeViewImageGenerator_NoExpandImage)] 
            public Image NoExpandImage { 
                get {
                    return _noExpandImage; 
                }
                set {
                    _noExpandImage = value;
                } 
            }
 
            [DefaultValue(typeof(Color), "Magenta")] 
            [SRDescription(SR.TreeViewImageGenerator_TransparentColor)]
            public Color TransparentColor { 
                get {
                    return _transparentColor;
                }
                set { 
                    _transparentColor = value;
                } 
            } 

            [SRDescription(SR.TreeViewImageGenerator_LineImageWidth)] 
            public int Width {
                get {
                    return _width;
                } 
                set {
                    if (value > MaxSize) { 
                        throw new ArgumentOutOfRangeException("value"); 
                    }
                    _width = value; 
                }
            }
        }
 
        #region Copied from ImageUtil.cs
        private static Image ReduceColors(Bitmap bitmap, int maxColors, int numBits, Color transparentColor) { 
            if ((numBits < 3) || (numBits > 8)) { 
                throw new ArgumentOutOfRangeException("numBits");
            } 

            if (maxColors < 16) {
                throw new ArgumentOutOfRangeException("maxColors");
            } 

            // Use Octree Color Quantization to reduce the color palette 
            int width = bitmap.Width; 
            int height = bitmap.Height;
            Octree octree = new Octree(maxColors, numBits, transparentColor); 
            // Scan through each pixel, adding the color to the octree
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    octree.AddColor(bitmap.GetPixel(x, y)); 
                }
            } 
 
            // Grab a color table from the octree
            ColorIndexTable colorTable = octree.GetColorIndexTable(); 

            // Create a new indexed color bitmap
            Bitmap newBmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            ColorPalette pal = newBmp.Palette; 

            // Scan through each pixel again, looking up the color index in the color table 
            Rectangle rect = new Rectangle(0, 0, width, height); 
            BitmapData bmpData = newBmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            IntPtr pixels = bmpData.Scan0; 

            unsafe {
                byte* pBits;
                if (bmpData.Stride > 0) { 
                    pBits = (byte*)pixels.ToPointer();
                } 
                else { 
                    pBits = (byte*)pixels.ToPointer() + (bmpData.Stride * (height - 1));
                } 
                int stride = Math.Abs(bmpData.Stride);

                for (int row = 0; row < height; row++) {
                    for (int col = 0; col < width; col++) { 
                        byte* pixel = pBits + (row * stride) + col;
                        Color c = bitmap.GetPixel(col, row); 
                        byte index = (byte)colorTable[c]; 
                        *pixel = index;
                    } 
                }
            }

            // Set the new palette for the indexed bitmap 
            colorTable.CopyToColorPalette(pal);
            newBmp.Palette = pal; 
            newBmp.UnlockBits(bmpData); 

            return newBmp; 
        }

        private bool SaveTransparentGif(Bitmap bitmap, IFolderProjectItem folder, string name, ref bool overwrite) {
            Image newImage = ReduceColors(bitmap, 256, 5, _imageInfo.TransparentColor); 
            bool writeFile = false;
            try { 
                MemoryStream memStream = new MemoryStream(); 
                newImage.Save(memStream, ImageFormat.Gif);
                memStream.Flush(); 
                memStream.Capacity = (int)memStream.Length;
                folder.AddDocument(name, memStream.GetBuffer());
            }
            finally { 
                newImage.Dispose();
            } 
 
            return writeFile;
        } 

        private class Octree {
            private OctreeNode _root;
            private ArrayList _leafNodes; 

            private int _maxColors; 
            private int _numBits; 
            private Color _transparentColor;
            private bool _hasTransparency; 

            //
            private ArrayList[] _levels;
 
            public Octree(int maxColors, int numBits, Color transparentColor) {
                _root = new OctreeNode(); 
                _maxColors = maxColors; 
                _leafNodes = new ArrayList();
                _numBits = numBits; 

                // Allow an optional transparent color
                _transparentColor = transparentColor;
                if (!_transparentColor.IsEmpty) { 
                    _hasTransparency = true;
                    _maxColors--; 
                } 

                // Create an array of ArrayList of all reducible node at each depth 
                _levels = new ArrayList[_numBits - 1];

                for (int i = 0; i < _levels.Length; i++) {
                    _levels[i] = new ArrayList(); 
                }
            } 
 
            public void AddColor(Color c) {
                // Ignore the transparent color, since we already have a color assigned to it 
                if (_hasTransparency &&
                    (_transparentColor.R == c.R) &&
                    (_transparentColor.G == c.G) &&
                    (_transparentColor.B == c.B)) { 
                    return;
                } 
 
                int depth = -1;
                // If we have the maximum number of leaf nodes, we need to merge some nodes, 
                // so find the one at the deepest level with the largest pixel count
                if (_leafNodes.Count >= _maxColors) {
                    OctreeNode node = null;
                    for (int i = (_numBits - 2); i > 0; i--) { 
                        ArrayList level = _levels[i];
                        if (level.Count > 0) { 
                            depth = i; 
                            int max = -1;
                            for (int j = 0; j < level.Count; j++) { 
                                OctreeNode workNode = (OctreeNode)level[j];
                                if (workNode.PixelCount > max) {
                                    node = workNode;
                                    max = workNode.PixelCount; 
                                }
                            } 
                            break; 
                        }
                    } 

                    // Reduce the node recursively
                    ReduceNode(node, depth);
 
                    // Add the reduced node to the leaf list (since it now has no children)
                    _leafNodes.Add(node); 
                } 

                // Now we want to add the new color to the tree 
                OctreeNode work = _root;
                depth = 0;
                bool newNode = false;
                // Search down the tree remembering the color at each ndoe (so we don't need to do any 
                // work when we reduce)
                while (depth < (_numBits - 1)) { 
                    int index = GetIndex(c, depth); 
                    OctreeNode child = work[index];
                    if (child == null) { 
                        child = new OctreeNode();
                        work[index] = child;
                        newNode = true;
 
                        // If the node has just gotten 2 children, it's reducible
                        if (work.NodeCount == 2) { 
                            _levels[depth].Add(work); 
                        }
                    } 
                    work = child;

                    work.AddColor(c);
 
                    // If we've hit a node that's already been reduced, break out
                    // since we don't want to add any children to it 
                    if (work.Reduced) { 
                        break;
                    } 

                    depth++;
                }
 
                // If we've just added a new node, it must be a leaf
                if (newNode) { 
                    _leafNodes.Add(work); 
                }
            } 

            public ColorIndexTable GetColorIndexTable() {
                Hashtable colorTable = new Hashtable();
                int colorCount = _maxColors; 
                Color[] colors = new Color[colorCount];
                int index = 0; 
 
                // Add the transparent color first, if it isn't empty
                if (!_transparentColor.IsEmpty) { 
                    colorTable[ColorIndexTable.GetColorKey(_transparentColor)] = 0;
                    colors[0] = Color.FromArgb(0, _transparentColor);
                    index = 1;
                } 

                // Go through all leaf nodes, adding an entry for each color into the color table 
                foreach (OctreeNode node in _leafNodes) { 
                    int r = 0;
                    int g = 0; 
                    int b = 0;
                    foreach (Color c in node.Colors) {
                        int key = ColorIndexTable.GetColorKey(c);
                        colorTable[key] = index; 
                        r += c.R;
                        g += c.G; 
                        b += c.B; 
                    }
                    int count = node.Colors.Count; 
                    // Also keep an array of all colors used
                    colors[index] = Color.FromArgb(255, r / count, g / count, b / count);

                    index++; 
                }
 
                return new ColorIndexTable(colorTable, colors); 
            }
 
            private void ReduceNode(OctreeNode node, int depth) {
                // Get the level of the children of the reduced node so we
                // can remove them from the level
                ArrayList childLevel = null; 
                if (depth < (_numBits - 2)) {
                    childLevel = _levels[depth + 1]; 
                } 

                // Look at each child node 
                for (int i = 0; i < 8; i++) {
                    OctreeNode childNode = node[i];
                    if (childNode != null) {
                        // If the child node is not at the deepest level we 
                        // need to reduce that too
                        if (depth < (_numBits - 2)) { 
                            ReduceNode(childNode, depth + 1); 
                        }
 
                        // If there was a level for the children, remove it from the level
                        if (childLevel != null) {
                            childLevel.Remove(childNode);
                        } 

                        // If the child node was a leaf node, remove it from the leaf node collection 
                        if (childNode.NodeCount == 0) { 
                            _leafNodes.Remove(childNode);
                        } 

                        // Remove the child
                        node[i] = null;
 
                    }
                    // Remove the current node from the level, since it is no longer reducible 
                    _levels[depth].Remove(node); 
                    // Mark the node as reducible
                    node.Reduced = true; 
                }
            }

            private int GetIndex(Color c, int depth) { 
                int offset = 7 - depth;
                return (((c.R >> offset) & 0x1) << 2) | (((c.G >> offset) & 0x1) << 1) | ((c.B >> offset) & 0x1); 
            } 
        }
 
        private class OctreeNode {
            private OctreeNode[] _nodes;
            private ArrayList _colors;
            private int _nodeCount; 
            private bool _reduced;
 
            public OctreeNode() { 
                _nodes = new OctreeNode[8];
                _colors = new ArrayList(); 
                _nodeCount = 0;
                _reduced = false;
            }
 
            public ICollection Colors {
                get { 
                    return _colors; 
                }
            } 

            public int NodeCount {
                get {
                    return _nodeCount; 
                }
            } 
 
            public int PixelCount {
                get { 
                    return _colors.Count;
                }
            }
 
            public bool Reduced {
                get { 
                    return _reduced; 
                }
                set { 
                    _reduced = value;
                }
            }
 
            public OctreeNode this[int index] {
                get { 
                    return _nodes[index]; 
                }
                set { 
                   _nodes[index] = value;

                   if (_nodes[index] == null) {
                       _nodeCount--; 
                   }
                   else { 
                       _nodeCount++; 
                   }
                } 
            }

            public void AddColor(Color c) {
                _colors.Add(c); 
            }
        } 
 
        private class ColorIndexTable {
            private IDictionary _table; 
            private Color[] _colors;

            internal ColorIndexTable(IDictionary table, Color[] colors) {
                _table = table; 
                _colors = colors;
            } 
 
            public int this[Color c] {
                get { 
                    object o = _table[GetColorKey(c)];
                    if (o == null) {
                        return 0;
                    } 

                    return (int)o; 
                } 
            }
 
            public void CopyToColorPalette(ColorPalette palette) {
                for (int i = 0; i < _colors.Length; i++) {
                    palette.Entries[i] = _colors[i];
                } 
            }
 
            internal static int GetColorKey(Color c) { 
                return ((c.R & 0xFF) << 16 | (c.G & 0xFF) << 8 | (c.B & 0xFF));
            } 
        }
        #endregion
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TreeViewImageGenerator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design.WebControls { 
    using System; 
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design;
    using System.Drawing;
    using System.Drawing.Design; 
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging; 
    using System.IO; 
    using System.Web.UI.Design.Util;
    using System.Windows.Forms; 

    using WebTreeView = System.Web.UI.WebControls.TreeView;

    /// <include file='doc\TreeViewImageGenerator.uex' path='docs/doc[@for="TreeViewImageGenerator"]/*' /> 
    internal class TreeViewImageGenerator : DesignerForm {
        private static Image defaultMinusImage; 
        private static Image defaultPlusImage; 

        private WebTreeView _treeView; 
        private PictureBox _previewPictureBox;
        private TextBox _previewFrameTextBox;
        private Label _previewLabel;
        private Panel _previewPanel; 
        private Button _okButton;
        private Button _cancelButton; 
        private Label _folderNameLabel; 
        private Label _propertiesLabel;
        private PropertyGrid _propertyGrid; 
        private TextBox _folderNameTextBox;
        private ProgressBar _progressBar;
        private Label _progressBarLabel;
 
        private LineImageInfo _imageInfo;
 
        /// <include file='doc\TreeViewImageGenerator.uex' path='docs/doc[@for="TreeViewImageGenerator.TreeViewImageGenerator"]/*' /> 
        public TreeViewImageGenerator(WebTreeView treeView) : base(treeView.Site) {
 
            _previewPictureBox = new PictureBox();
            _previewLabel = new Label();
            _previewPanel = new Panel();
            _previewFrameTextBox = new TextBox(); 
            _okButton = new Button();
            _cancelButton = new Button(); 
            _folderNameLabel = new Label(); 
            _folderNameTextBox = new TextBox();
            _propertiesLabel = new Label(); 
            _propertyGrid = new System.Windows.Forms.Design.VsPropertyGrid(ServiceProvider);
            _progressBar = new ProgressBar();
            _progressBarLabel = new Label();
            _previewPanel.SuspendLayout(); 
            SuspendLayout();
            // 
            // _previewPictureBox 
            //
            _previewPictureBox.Name = "_previewPictureBox"; 
            _previewPictureBox.SizeMode = PictureBoxSizeMode.Normal;
            _previewPictureBox.TabIndex = 10;
            _previewPictureBox.TabStop = false;
            _previewPictureBox.BackColor = Color.White; 
            //
            // _previewLabel 
            // 
            _previewLabel.Location = new Point(12, 12);
            _previewLabel.Name = "_previewLabel"; 
            _previewLabel.Size = new Size(180, 14);
            _previewLabel.TabIndex = 9;
            _previewLabel.Text = SR.GetString(SR.TreeViewImageGenerator_Preview);
            // 
            // _previewPanel
            // 
            _previewPanel.Anchor = (((AnchorStyles.Top | AnchorStyles.Bottom) 
                | AnchorStyles.Left)
                | AnchorStyles.Right); 
            _previewPanel.AutoScroll = true;
            _previewPanel.BorderStyle = BorderStyle.None;
            _previewPanel.Controls.AddRange(new Control[] { _previewPictureBox });
            _previewPanel.Location = new Point(13, 29); 
            _previewPanel.Name = "_previewPanel";
            _previewPanel.Size = new Size(178, 242); 
            _previewPanel.TabIndex = 11; 
            //
            // _previewFrameTextBox, used to make the border themeable (from UI Design guidelines for Whidbey) 
            //
            _previewFrameTextBox.Multiline = true;
            _previewFrameTextBox.Enabled = false;
            _previewFrameTextBox.TabStop = false; 
            _previewFrameTextBox.Location = new Point(12, 28);
            _previewFrameTextBox.Size = new Size(180, 244); 
            // 
            // _okButton
            // 
            _okButton.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            _okButton.FlatStyle = FlatStyle.System;
            _okButton.Location = new Point(376, 324);
            _okButton.Name = "_okButton"; 
            _okButton.Size = new Size(75, 23);
            _okButton.TabIndex = 20; 
            _okButton.Text = SR.GetString(SR.OKCaption); 
            _okButton.Click += new System.EventHandler(OnOKButtonClick);
            // 
            // _cancelButton
            //
            _cancelButton.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            _cancelButton.FlatStyle = FlatStyle.System; 
            _cancelButton.Location = new Point(456, 324);
            _cancelButton.Name = "_cancelButton"; 
            _cancelButton.Size = new Size(75, 23); 
            _cancelButton.TabIndex = 21;
            _cancelButton.Text = SR.GetString(SR.CancelCaption); 
            _cancelButton.Click += new System.EventHandler(OnCancelButtonClick);
            //
            // _folderNameLabel
            // 
            _folderNameLabel.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left);
            _folderNameLabel.Location = new Point(213, 279); 
            _folderNameLabel.Name = "_folderNameLabel"; 
            _folderNameLabel.Size = new Size(315, 14);
            _folderNameLabel.TabIndex = 17; 
            _folderNameLabel.Text = SR.GetString(SR.TreeViewImageGenerator_FolderName);
            //
            // _folderNameTextBox
            // 
            _folderNameTextBox.Anchor = ((AnchorStyles.Bottom | AnchorStyles.Left)
                | AnchorStyles.Right); 
            _folderNameTextBox.Location = new Point(213, 295); 
            _folderNameTextBox.Name = "_folderNameTextBox";
            _folderNameTextBox.Size = new Size(315, 20); 
            _folderNameTextBox.TabIndex = 18;
            _folderNameTextBox.Text = SR.GetString(SR.TreeViewImageGenerator_DefaultFolderName);
            _folderNameTextBox.WordWrap = false;
            _folderNameTextBox.TextChanged += new EventHandler(OnFolderNameTextBoxTextChanged); 

            // 
            // _progressBarLabel 
            //
            _progressBarLabel.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left); 
            _progressBarLabel.Location = new Point(12, 279);
            _progressBarLabel.Name = "_progressBarLabel";
            _progressBarLabel.Size = new Size(180, 14);
            _progressBarLabel.Text = SR.GetString(SR.TreeViewImageGenerator_ProgressBarName); 
            _progressBarLabel.Visible = false;
            // 
            // _progressBar 
            //
            _progressBar.Location = new Point(12, 295); 
            _progressBar.Size = new Size(180, 16);
            _progressBar.Maximum = 16;
            _progressBar.Minimum = 0;
            _progressBar.Visible = false; 

            // 
            // _propertiesLabel 
            //
            _propertiesLabel.Location = new Point(213, 12); 
            _propertiesLabel.Name = "_propertiesLabel";
            _propertiesLabel.Size = new Size(315, 14);
            _propertiesLabel.TabIndex = 12;
            _propertiesLabel.Text = SR.GetString(SR.TreeViewImageGenerator_Properties); 
            //
            // _propertyGrid 
            // 
            _propertyGrid.Anchor = ((AnchorStyles.Top | AnchorStyles.Bottom)
                | AnchorStyles.Right); 
            _propertyGrid.CommandsVisibleIfAvailable = true;
            _propertyGrid.LargeButtons = false;
            _propertyGrid.LineColor = SystemColors.ScrollBar;
            _propertyGrid.Location = new Point(213, 28); 
            _propertyGrid.Name = "_propertyGrid";
            _propertyGrid.PropertySort = PropertySort.Alphabetical; 
            _propertyGrid.Size = new Size(315, 244); 
            _propertyGrid.TabIndex = 13;
            _propertyGrid.ToolbarVisible = true; 
            _propertyGrid.ViewBackColor = SystemColors.Window;
            _propertyGrid.ViewForeColor = SystemColors.WindowText;
            _propertyGrid.PropertyValueChanged += new PropertyValueChangedEventHandler(OnPropertyGridPropertyValueChanged);
            // 
            // TreeLineImageGenerator
            // 
            AcceptButton = _okButton; 
            CancelButton = _cancelButton;
            ClientSize = new Size(540, 359); 
            Controls.AddRange(new Control[] {
                                                                          _propertyGrid,
                                                                          _propertiesLabel,
                                                                          _progressBar, 
                                                                          _progressBarLabel,
                                                                          _folderNameTextBox, 
                                                                          _folderNameLabel, 
                                                                          _cancelButton,
                                                                          _okButton, 
                                                                          _previewPanel,
                                                                          _previewLabel,
                                                                          _previewFrameTextBox});
            MinimumSize = new Size(540, 359); 
            Name = "TreeLineImageGenerator";
            Text = SR.GetString(SR.TreeViewImageGenerator_Title); 
            Resize += new System.EventHandler(OnFormResize); 
            _previewPanel.ResumeLayout(false);
 
            InitializeForm();

            ResumeLayout(false);
 
            _imageInfo = new LineImageInfo();
            _propertyGrid.SelectedObject = _imageInfo; 
 
            _treeView = treeView;
 
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = null;

            UpdatePreview(); 
        }
 
        private Image DefaultMinusImage { 
            get {
                if (defaultMinusImage == null) { 
                    defaultMinusImage = new Bitmap(typeof(TreeViewImageGenerator), "Minus.gif");
                }
                return defaultMinusImage;
            } 
        }
 
        private Image DefaultPlusImage { 
            get {
                if (defaultPlusImage == null) { 
                    defaultPlusImage = new Bitmap(typeof(TreeViewImageGenerator), "Plus.gif");
                }
                return defaultPlusImage;
            } 
        }
 
        protected override string HelpTopic { 
            get {
                return "net.Asp.TreeView.ImageGenerator"; 
            }
        }

        private void OnCancelButtonClick(object sender, System.EventArgs e) { 
            Close();
        } 
 
        private void OnFormResize(object sender, System.EventArgs e) {
            UpdatePreview(); 
        }

        private void OnOKButtonClick(object sender, System.EventArgs e) {
            string folderName = _folderNameTextBox.Text.Trim(); 
            if (folderName.Length == 0) {
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_MissingFolderName)); 
                return; 
            }
 
            if (folderName.IndexOfAny(Path.GetInvalidPathChars()) != -1) {
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_InvalidFolderName, folderName));
                return;
            } 

            IWebApplication webApplication = (IWebApplication)_treeView.Site.GetService(typeof(IWebApplication)); 
            if (webApplication == null) { 
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_ErrorWriting));
                return; 
            }

            IFolderProjectItem root = (IFolderProjectItem)webApplication.RootProjectItem;
            IProjectItem testItem = webApplication.GetProjectItemFromUrl(Path.Combine("~/", folderName)); 
            if ((testItem != null) && !(testItem is IFolderProjectItem)) {
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_DocumentExists, folderName)); 
                return; 
            }
 
            IFolderProjectItem folder = (IFolderProjectItem)testItem;
            if (folder == null) {
                if (UIServiceHelper.ShowMessage(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_NonExistentFolderName, folderName), SR.GetString(SR.TreeViewImageGenerator_Title), MessageBoxButtons.YesNo) == DialogResult.Yes) {
                    try { 
                        folder = root.AddFolder(folderName);
                    } 
                    catch { 
                        UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_ErrorCreatingFolder, folderName));
                        return; 
                    }
                }
                else {
                    return; 
                }
            } 
 
            Image expandImage = _imageInfo.ExpandImage;
            if (expandImage == null) { 
                expandImage = DefaultPlusImage;
            }

            Image collapseImage = _imageInfo.CollapseImage; 
            if (collapseImage == null) {
                collapseImage = DefaultMinusImage; 
            } 

            Image noExpandImage = _imageInfo.NoExpandImage; 

            int width = _imageInfo.Width;
            if (width < 1) {
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_InvalidValue, "Width")); 
                return;
            } 
 
            int height = _imageInfo.Height;
            if (height < 1) { 
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_InvalidValue, "Height"));
                return;
            }
 
            int lineWidth = _imageInfo.LineWidth;
            if (lineWidth < 1) { 
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_InvalidValue, "LineWidth")); 
                return;
            } 

            int lineStyle = (int)_imageInfo.LineStyle;
            Color lineColor = _imageInfo.LineColor;
 
            _progressBar.Value = 0;
            _progressBar.Visible = true; 
            _progressBarLabel.Visible = true; 

            try { 
                bool overwrite = false;
                bool wroteFile = false;

                // Write out the vertical line image 
                Bitmap bitmap = new Bitmap(width, height);
                Graphics g = Graphics.FromImage(bitmap); 
                g.FillRectangle(new SolidBrush(_imageInfo.TransparentColor), 0, 0, width, height); 
                RenderImage(g, 0, 0, width, height, 'i', lineStyle, lineWidth, lineColor, null);
                string filename = "i.gif"; 
                wroteFile |= SaveTransparentGif(bitmap, folder, "i.gif", ref overwrite);
                _progressBar.Value++;

                // Write out the collapse images 
                string lineTypes = "-rtl ";
                for (int i = 0; i < lineTypes.Length; i++) { 
                    bitmap = new Bitmap(width, height); 
                    g = Graphics.FromImage(bitmap);
                    g.FillRectangle(new SolidBrush(_imageInfo.TransparentColor), 0, 0, width, height); 
                    RenderImage(g, 0, 0, width, height, lineTypes[i], lineStyle, lineWidth, lineColor, collapseImage);
                    g.Dispose();
                    filename = "minus.gif";
                    if (lineTypes[i] == '-') { 
                        filename = "dash" + filename;
                    } 
                    else if (lineTypes[i] == ' ') { 
                    }
                    else { 
                        filename = lineTypes[i] + filename;
                    }
                    wroteFile |= SaveTransparentGif(bitmap, folder, filename, ref overwrite);
                    _progressBar.Value++; 
                }
 
                // Write out the expand images 
                for (int i = 0; i < lineTypes.Length; i++) {
                    bitmap = new Bitmap(width, height); 
                    g = Graphics.FromImage(bitmap);
                    g.FillRectangle(new SolidBrush(_imageInfo.TransparentColor), 0, 0, width, height);
                    RenderImage(g, 0, 0, width, height, lineTypes[i], lineStyle, lineWidth, lineColor, expandImage);
                    g.Dispose(); 
                    filename = "plus.gif";
                    if (lineTypes[i] == '-') { 
                        filename = "dash" + filename; 
                    }
                    else if (lineTypes[i] == ' ') { 
                    }
                    else {
                        filename = lineTypes[i] + filename;
                    } 
                    wroteFile |= SaveTransparentGif(bitmap, folder, filename, ref overwrite);
                    _progressBar.Value++; 
                } 

                // Write out the noExpand images 
                for (int i = 0; i < lineTypes.Length; i++) {
                    bitmap = new Bitmap(width, height);
                    g = Graphics.FromImage(bitmap);
                    g.FillRectangle(new SolidBrush(_imageInfo.TransparentColor), 0, 0, width, height); 
                    RenderImage(g, 0, 0, width, height, lineTypes[i], lineStyle, lineWidth, lineColor, noExpandImage);
                    g.Dispose(); 
                    filename = ".gif"; 
                    if (lineTypes[i] == '-') {
                        filename = "dash" + filename; 
                    }
                    else if (lineTypes[i] == ' ') {
                        filename = "noexpand" + filename;
                    } 
                    else {
                        filename = lineTypes[i] + filename; 
                    } 
                    wroteFile |= SaveTransparentGif(bitmap, folder, filename, ref overwrite);
                    _progressBar.Value++; 
                }

                _progressBar.Visible = false;
                _progressBarLabel.Visible = false; 

                if (wroteFile) { 
                    UIServiceHelper.ShowMessage(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_LineImagesGenerated, folderName)); 
                }
            } 
            catch {
                _progressBar.Visible = false;
                _progressBarLabel.Visible = false;
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_ErrorWriting, folderName)); 
                return;
            } 
 
            _treeView.LineImagesFolder = "~/" + folderName;
 
            DialogResult = DialogResult.OK;
            Close();
        }
 
        private void OnFolderNameTextBoxTextChanged(object sender, EventArgs e) {
            if (_folderNameTextBox.Text.Trim().Length > 0) { 
                _okButton.Enabled = true; 
            }
            else { 
                _okButton.Enabled = false;
            }
        }
 
        private void OnPropertyGridPropertyValueChanged(object s, PropertyValueChangedEventArgs e) {
            UpdatePreview(); 
        } 

        private void RenderImage(Graphics g, int x, int y, int width, int height, char lineType, int lineStyle, int lineWidth, Color lineColor, Image image) { 
            Pen p = new Pen(lineColor, lineWidth);
            switch (lineStyle) {
                case 0:
                    p.DashStyle = DashStyle.Dot; 
                    break;
                case 1: 
                    p.DashStyle = DashStyle.Dash; 
                    break;
                default: 
                    p.DashStyle = DashStyle.Solid;
                    break;
            }
 
            if (lineType == 'i') {
                g.DrawLine(p, x + width / 2, y, x + width / 2, y + height); 
            } 
            else if (lineType == 'r') {
                g.DrawLine(p, x + width / 2, y + height / 2, x + width, y + height / 2); 
                g.DrawLine(p, x + width / 2, y + height / 2, x + width / 2, y + height);
            }
            else if (lineType == 't') {
                g.DrawLine(p, x + width / 2, y, x + width / 2, y + height); 
                g.DrawLine(p, x + width / 2, y + height / 2, x + width, y + height / 2);
            } 
            else if (lineType == 'l') { 
                g.DrawLine(p, x + width / 2, y, x + width / 2, y + height / 2);
                g.DrawLine(p, x + width / 2, y + height / 2, x + width, y + height / 2); 
            }
            else if (lineType == '-') {
                g.DrawLine(p, x + width / 2, y + height / 2, x + width, y + height / 2);
            } 

            if (image != null) { 
                int imgWidth = Math.Min(image.Width, width); 
                int imgHeight = Math.Min(image.Height, height);
                g.DrawImage(image, 
                    x + (width - imgWidth + 1) / 2,
                    y + (height - imgHeight + 1) / 2,
                    imgWidth,
                    imgHeight); 
            }
 
            p.Dispose(); 
        }
 
        private void UpdatePreview() {
            Image expandImage = _imageInfo.ExpandImage;
            if (expandImage == null) {
                expandImage = DefaultPlusImage; 
            }
 
            Image collapseImage = _imageInfo.CollapseImage; 
            if (collapseImage == null) {
                collapseImage = DefaultMinusImage; 
            }

            Image noExpandImage = _imageInfo.NoExpandImage;
 
            int width = _imageInfo.Width;
            if (width < 1) { 
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_InvalidValue, "Width")); 
                return;
            } 

            int height = _imageInfo.Height;
            if (height < 1) {
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_InvalidValue, "Height")); 
                return;
            } 
 
            int lineWidth = _imageInfo.LineWidth;
            if (lineWidth < 1) { 
                UIServiceHelper.ShowError(ServiceProvider, SR.GetString(SR.TreeViewImageGenerator_InvalidValue, "LineWidth"));
                return;
            }
 
            int lineStyle = (int)_imageInfo.LineStyle;
            Color lineColor = _imageInfo.LineColor; 
 
            Font font = new Font("Tahoma", 10);
            Graphics tempGraphics = Graphics.FromHwnd(Handle); 
            int totalWidth = width * 2 +
                (int)tempGraphics.MeasureString(SR.GetString(SR.TreeViewImageGenerator_SampleParent, 1), font).Width;
            int textHeight = Math.Max(
                (int)tempGraphics.MeasureString(SR.GetString(SR.TreeViewImageGenerator_SampleParent, 1), font).Height, 
                height);
            tempGraphics.Dispose(); 
            int totalHeight = textHeight * 6; 
            int indent = Math.Max(width, _treeView.NodeIndent);
 
            Bitmap bitmap = new Bitmap(Math.Max(totalWidth, _previewPanel.Width), Math.Max(totalHeight, _previewPanel.Height));
            Graphics g = Graphics.FromImage(bitmap);

            int x = 5; 
            int y = 5;
 
            g.FillRectangle(Brushes.White, x, y, totalWidth, totalHeight); 
            RenderImage(g, x, y, width, height, '-', lineStyle, lineWidth, lineColor, expandImage);
            x += width; 
            g.DrawString(SR.GetString(SR.TreeViewImageGenerator_SampleRoot, 1),
                font,
                Brushes.Black,
                x, 
                y + (height - (g.MeasureString(SR.GetString(SR.TreeViewImageGenerator_SampleRoot, 1), font).Height) + 1) / 2);
 
            y += textHeight; 
            x -= width;
            RenderImage(g, x, y, width, height, 'r', lineStyle, lineWidth, lineColor, collapseImage); 
            x += width;
            g.DrawString(SR.GetString(SR.TreeViewImageGenerator_SampleRoot, 2),
                font,
                Brushes.Black, 
                x,
                y + (height - (g.MeasureString(SR.GetString(SR.TreeViewImageGenerator_SampleRoot, 2), font).Height) + 1) / 2); 
 
            y += textHeight;
            x -= width; 
            RenderImage(g, x, y, width, height, 'i', lineStyle, lineWidth, lineColor, null);
            x += indent;
            RenderImage(g, x, y, width, height, 't', lineStyle, lineWidth, lineColor, expandImage);
            x += width; 
            g.DrawString(SR.GetString(SR.TreeViewImageGenerator_SampleParent, 1),
                font, 
                Brushes.Black, 
                x,
                y + (height - (g.MeasureString(SR.GetString(SR.TreeViewImageGenerator_SampleParent, 1), font).Height) + 1) / 2); 

            y += textHeight;
            x -= width + indent;
            RenderImage(g, x, y, width, height, 'i', lineStyle, lineWidth, lineColor, null); 
            x += indent;
            RenderImage(g, x, y, width, height, 't', lineStyle, lineWidth, lineColor, noExpandImage); 
            x += width; 
            g.DrawString(SR.GetString(SR.TreeViewImageGenerator_SampleLeaf, 1),
                font, 
                Brushes.Black,
                x,
                y + (height - (g.MeasureString(SR.GetString(SR.TreeViewImageGenerator_SampleLeaf, 1), font).Height) + 1) / 2);
 
            y += textHeight;
            x -= width + indent; 
            RenderImage(g, x, y, width, height, 'i', lineStyle, lineWidth, lineColor, null); 
            x += indent;
            RenderImage(g, x, y, width, height, 'l', lineStyle, lineWidth, lineColor, noExpandImage); 
            x += width;
            g.DrawString(SR.GetString(SR.TreeViewImageGenerator_SampleLeaf, 2),
                font,
                Brushes.Black, 
                x,
                y + (height - (g.MeasureString(SR.GetString(SR.TreeViewImageGenerator_SampleLeaf, 2), font).Height) + 1) / 2); 
 
            y += textHeight;
            x -= width + indent; 
            RenderImage(g, x, y, width, height, 'l', lineStyle, lineWidth, lineColor, expandImage);
            x += width;
            g.DrawString(SR.GetString(SR.TreeViewImageGenerator_SampleRoot, 3),
                font, 
                Brushes.Black,
                x, 
                y + (height - (g.MeasureString(SR.GetString(SR.TreeViewImageGenerator_SampleRoot, 3), font).Height) + 1) / 2); 

            g.Dispose(); 

            bitmap.MakeTransparent(_imageInfo.TransparentColor);
            _previewPictureBox.Image = bitmap;
            _previewPictureBox.Width = Math.Max(totalWidth, _previewPanel.Width); 
            _previewPictureBox.Height = Math.Max(totalHeight, _previewPanel.Height);
        } 
 
        private enum LineStyle {
            Dotted = 0, 
            Dashed = 1,
            Solid = 2
        }
 
        private class LineImageInfo {
            private int _height; 
            private int _width; 
            private int _lineWidth;
            private LineStyle _lineStyle; 
            private Color _lineColor;
            private Color _transparentColor;
            private Image _collapseImage;
            private Image _expandImage; 
            private Image _noExpandImage;
 
            private const int MaxSize = 300; 

            public LineImageInfo() { 
                _height = 20;
                _width = 19;
                _lineWidth = 1;
                _lineStyle = LineStyle.Dotted; 
                _lineColor = Color.Black;
                _transparentColor = Color.Magenta; 
            } 

            [DefaultValue(null)] 
            [SRDescription(SR.TreeViewImageGenerator_CollapseImage)]
            public Image CollapseImage {
                get {
                    return _collapseImage; 
                }
                set { 
                    _collapseImage = value; 
                }
            } 

            [DefaultValue(null)]
            [SRDescription(SR.TreeViewImageGenerator_ExpandImage)]
            public Image ExpandImage { 
                get {
                    return _expandImage; 
                } 
                set {
                    _expandImage = value; 
                }
            }

            [SRDescription(SR.TreeViewImageGenerator_LineColor)] 
            public Color LineColor {
                get { 
                    return _lineColor; 
                }
                set { 
                    _lineColor = value;
                }
            }
 
            [SRDescription(SR.TreeViewImageGenerator_LineStyle)]
            public LineStyle LineStyle { 
                get { 
                    return _lineStyle;
                } 
                set {
                    _lineStyle = value;
                }
            } 

            [SRDescription(SR.TreeViewImageGenerator_LineWidth)] 
            public int LineWidth { 
                get {
                    return _lineWidth; 
                }
                set {
                    if (value > MaxSize) {
                        throw new ArgumentOutOfRangeException("value"); 
                    }
                    _lineWidth = value; 
                } 
            }
 
            [SRDescription(SR.TreeViewImageGenerator_LineImageHeight)]
            public int Height {
                get {
                    return _height; 
                }
                set { 
                    if (value > MaxSize) { 
                        throw new ArgumentOutOfRangeException("value");
                    } 
                    _height = value;
                }
            }
 
            [DefaultValue(null)]
            [SRDescription(SR.TreeViewImageGenerator_NoExpandImage)] 
            public Image NoExpandImage { 
                get {
                    return _noExpandImage; 
                }
                set {
                    _noExpandImage = value;
                } 
            }
 
            [DefaultValue(typeof(Color), "Magenta")] 
            [SRDescription(SR.TreeViewImageGenerator_TransparentColor)]
            public Color TransparentColor { 
                get {
                    return _transparentColor;
                }
                set { 
                    _transparentColor = value;
                } 
            } 

            [SRDescription(SR.TreeViewImageGenerator_LineImageWidth)] 
            public int Width {
                get {
                    return _width;
                } 
                set {
                    if (value > MaxSize) { 
                        throw new ArgumentOutOfRangeException("value"); 
                    }
                    _width = value; 
                }
            }
        }
 
        #region Copied from ImageUtil.cs
        private static Image ReduceColors(Bitmap bitmap, int maxColors, int numBits, Color transparentColor) { 
            if ((numBits < 3) || (numBits > 8)) { 
                throw new ArgumentOutOfRangeException("numBits");
            } 

            if (maxColors < 16) {
                throw new ArgumentOutOfRangeException("maxColors");
            } 

            // Use Octree Color Quantization to reduce the color palette 
            int width = bitmap.Width; 
            int height = bitmap.Height;
            Octree octree = new Octree(maxColors, numBits, transparentColor); 
            // Scan through each pixel, adding the color to the octree
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    octree.AddColor(bitmap.GetPixel(x, y)); 
                }
            } 
 
            // Grab a color table from the octree
            ColorIndexTable colorTable = octree.GetColorIndexTable(); 

            // Create a new indexed color bitmap
            Bitmap newBmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            ColorPalette pal = newBmp.Palette; 

            // Scan through each pixel again, looking up the color index in the color table 
            Rectangle rect = new Rectangle(0, 0, width, height); 
            BitmapData bmpData = newBmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            IntPtr pixels = bmpData.Scan0; 

            unsafe {
                byte* pBits;
                if (bmpData.Stride > 0) { 
                    pBits = (byte*)pixels.ToPointer();
                } 
                else { 
                    pBits = (byte*)pixels.ToPointer() + (bmpData.Stride * (height - 1));
                } 
                int stride = Math.Abs(bmpData.Stride);

                for (int row = 0; row < height; row++) {
                    for (int col = 0; col < width; col++) { 
                        byte* pixel = pBits + (row * stride) + col;
                        Color c = bitmap.GetPixel(col, row); 
                        byte index = (byte)colorTable[c]; 
                        *pixel = index;
                    } 
                }
            }

            // Set the new palette for the indexed bitmap 
            colorTable.CopyToColorPalette(pal);
            newBmp.Palette = pal; 
            newBmp.UnlockBits(bmpData); 

            return newBmp; 
        }

        private bool SaveTransparentGif(Bitmap bitmap, IFolderProjectItem folder, string name, ref bool overwrite) {
            Image newImage = ReduceColors(bitmap, 256, 5, _imageInfo.TransparentColor); 
            bool writeFile = false;
            try { 
                MemoryStream memStream = new MemoryStream(); 
                newImage.Save(memStream, ImageFormat.Gif);
                memStream.Flush(); 
                memStream.Capacity = (int)memStream.Length;
                folder.AddDocument(name, memStream.GetBuffer());
            }
            finally { 
                newImage.Dispose();
            } 
 
            return writeFile;
        } 

        private class Octree {
            private OctreeNode _root;
            private ArrayList _leafNodes; 

            private int _maxColors; 
            private int _numBits; 
            private Color _transparentColor;
            private bool _hasTransparency; 

            //
            private ArrayList[] _levels;
 
            public Octree(int maxColors, int numBits, Color transparentColor) {
                _root = new OctreeNode(); 
                _maxColors = maxColors; 
                _leafNodes = new ArrayList();
                _numBits = numBits; 

                // Allow an optional transparent color
                _transparentColor = transparentColor;
                if (!_transparentColor.IsEmpty) { 
                    _hasTransparency = true;
                    _maxColors--; 
                } 

                // Create an array of ArrayList of all reducible node at each depth 
                _levels = new ArrayList[_numBits - 1];

                for (int i = 0; i < _levels.Length; i++) {
                    _levels[i] = new ArrayList(); 
                }
            } 
 
            public void AddColor(Color c) {
                // Ignore the transparent color, since we already have a color assigned to it 
                if (_hasTransparency &&
                    (_transparentColor.R == c.R) &&
                    (_transparentColor.G == c.G) &&
                    (_transparentColor.B == c.B)) { 
                    return;
                } 
 
                int depth = -1;
                // If we have the maximum number of leaf nodes, we need to merge some nodes, 
                // so find the one at the deepest level with the largest pixel count
                if (_leafNodes.Count >= _maxColors) {
                    OctreeNode node = null;
                    for (int i = (_numBits - 2); i > 0; i--) { 
                        ArrayList level = _levels[i];
                        if (level.Count > 0) { 
                            depth = i; 
                            int max = -1;
                            for (int j = 0; j < level.Count; j++) { 
                                OctreeNode workNode = (OctreeNode)level[j];
                                if (workNode.PixelCount > max) {
                                    node = workNode;
                                    max = workNode.PixelCount; 
                                }
                            } 
                            break; 
                        }
                    } 

                    // Reduce the node recursively
                    ReduceNode(node, depth);
 
                    // Add the reduced node to the leaf list (since it now has no children)
                    _leafNodes.Add(node); 
                } 

                // Now we want to add the new color to the tree 
                OctreeNode work = _root;
                depth = 0;
                bool newNode = false;
                // Search down the tree remembering the color at each ndoe (so we don't need to do any 
                // work when we reduce)
                while (depth < (_numBits - 1)) { 
                    int index = GetIndex(c, depth); 
                    OctreeNode child = work[index];
                    if (child == null) { 
                        child = new OctreeNode();
                        work[index] = child;
                        newNode = true;
 
                        // If the node has just gotten 2 children, it's reducible
                        if (work.NodeCount == 2) { 
                            _levels[depth].Add(work); 
                        }
                    } 
                    work = child;

                    work.AddColor(c);
 
                    // If we've hit a node that's already been reduced, break out
                    // since we don't want to add any children to it 
                    if (work.Reduced) { 
                        break;
                    } 

                    depth++;
                }
 
                // If we've just added a new node, it must be a leaf
                if (newNode) { 
                    _leafNodes.Add(work); 
                }
            } 

            public ColorIndexTable GetColorIndexTable() {
                Hashtable colorTable = new Hashtable();
                int colorCount = _maxColors; 
                Color[] colors = new Color[colorCount];
                int index = 0; 
 
                // Add the transparent color first, if it isn't empty
                if (!_transparentColor.IsEmpty) { 
                    colorTable[ColorIndexTable.GetColorKey(_transparentColor)] = 0;
                    colors[0] = Color.FromArgb(0, _transparentColor);
                    index = 1;
                } 

                // Go through all leaf nodes, adding an entry for each color into the color table 
                foreach (OctreeNode node in _leafNodes) { 
                    int r = 0;
                    int g = 0; 
                    int b = 0;
                    foreach (Color c in node.Colors) {
                        int key = ColorIndexTable.GetColorKey(c);
                        colorTable[key] = index; 
                        r += c.R;
                        g += c.G; 
                        b += c.B; 
                    }
                    int count = node.Colors.Count; 
                    // Also keep an array of all colors used
                    colors[index] = Color.FromArgb(255, r / count, g / count, b / count);

                    index++; 
                }
 
                return new ColorIndexTable(colorTable, colors); 
            }
 
            private void ReduceNode(OctreeNode node, int depth) {
                // Get the level of the children of the reduced node so we
                // can remove them from the level
                ArrayList childLevel = null; 
                if (depth < (_numBits - 2)) {
                    childLevel = _levels[depth + 1]; 
                } 

                // Look at each child node 
                for (int i = 0; i < 8; i++) {
                    OctreeNode childNode = node[i];
                    if (childNode != null) {
                        // If the child node is not at the deepest level we 
                        // need to reduce that too
                        if (depth < (_numBits - 2)) { 
                            ReduceNode(childNode, depth + 1); 
                        }
 
                        // If there was a level for the children, remove it from the level
                        if (childLevel != null) {
                            childLevel.Remove(childNode);
                        } 

                        // If the child node was a leaf node, remove it from the leaf node collection 
                        if (childNode.NodeCount == 0) { 
                            _leafNodes.Remove(childNode);
                        } 

                        // Remove the child
                        node[i] = null;
 
                    }
                    // Remove the current node from the level, since it is no longer reducible 
                    _levels[depth].Remove(node); 
                    // Mark the node as reducible
                    node.Reduced = true; 
                }
            }

            private int GetIndex(Color c, int depth) { 
                int offset = 7 - depth;
                return (((c.R >> offset) & 0x1) << 2) | (((c.G >> offset) & 0x1) << 1) | ((c.B >> offset) & 0x1); 
            } 
        }
 
        private class OctreeNode {
            private OctreeNode[] _nodes;
            private ArrayList _colors;
            private int _nodeCount; 
            private bool _reduced;
 
            public OctreeNode() { 
                _nodes = new OctreeNode[8];
                _colors = new ArrayList(); 
                _nodeCount = 0;
                _reduced = false;
            }
 
            public ICollection Colors {
                get { 
                    return _colors; 
                }
            } 

            public int NodeCount {
                get {
                    return _nodeCount; 
                }
            } 
 
            public int PixelCount {
                get { 
                    return _colors.Count;
                }
            }
 
            public bool Reduced {
                get { 
                    return _reduced; 
                }
                set { 
                    _reduced = value;
                }
            }
 
            public OctreeNode this[int index] {
                get { 
                    return _nodes[index]; 
                }
                set { 
                   _nodes[index] = value;

                   if (_nodes[index] == null) {
                       _nodeCount--; 
                   }
                   else { 
                       _nodeCount++; 
                   }
                } 
            }

            public void AddColor(Color c) {
                _colors.Add(c); 
            }
        } 
 
        private class ColorIndexTable {
            private IDictionary _table; 
            private Color[] _colors;

            internal ColorIndexTable(IDictionary table, Color[] colors) {
                _table = table; 
                _colors = colors;
            } 
 
            public int this[Color c] {
                get { 
                    object o = _table[GetColorKey(c)];
                    if (o == null) {
                        return 0;
                    } 

                    return (int)o; 
                } 
            }
 
            public void CopyToColorPalette(ColorPalette palette) {
                for (int i = 0; i < _colors.Length; i++) {
                    palette.Entries[i] = _colors[i];
                } 
            }
 
            internal static int GetColorKey(Color c) { 
                return ((c.R & 0xFF) << 16 | (c.G & 0xFF) << 8 | (c.B & 0xFF));
            } 
        }
        #endregion
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
