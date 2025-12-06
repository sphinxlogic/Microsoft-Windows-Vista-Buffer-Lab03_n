//------------------------------------------------------------------------------ 
// <copyright file="MultilineStringEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Design;
    using System.Diagnostics;
    using System.Drawing; 
    using System.Drawing.Design;
    using System.Reflection; 
    using System.Runtime.InteropServices; 
    using System.Windows.Forms;
    using System.Windows.Forms.ComponentModel; 
    using System.Windows.Forms.Design;
    using System.Security;
    using System.Security.Permissions;
    using Microsoft.Win32; 
    using System.Globalization;
    using System.IO; 
    using System.Runtime.Remoting; 
    using System.Runtime.Serialization.Formatters;
    using System.Text; 

    using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

    /// <include file='doc\MultilineStringEditor.uex' path='docs/doc[@for="MultilineStringEditor"]/*' /> 
    /// <devdoc>
    ///     An editor for editing strings that supports multiple lines of text and is resizable. 
    /// </devdoc> 
    public sealed class MultilineStringEditor : UITypeEditor {
 
        private MultilineStringEditorUI _editorUI = null;

        /// <include file='doc\MultilineStringEditor.uex' path='docs/doc[@for="MultilineStringEditor.EditValue"]/*' />
        /// <devdoc> 
        ///     Edits the given value, returning the editing results.
        /// </devdoc> 
        public override object EditValue (ITypeDescriptorContext context, 
            IServiceProvider provider,
            object value) { 

            if (provider != null) {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                if (edSvc != null) { 
                    if (_editorUI == null) {
                        _editorUI = new MultilineStringEditorUI(); 
                    } 
                    _editorUI.BeginEdit(edSvc, value);
 
                    edSvc.DropDownControl(_editorUI);

                    object newValue = _editorUI.Value;
                    if(_editorUI.EndEdit()) { 
                        value = newValue;
                    } 
                } 
            }
            return value; 
        }

        /// <include file='doc\MultilineStringEditor.uex' path='docs/doc[@for="MultilineStringEditor.GetEditStyle"]/*' />
        /// <devdoc> 
        ///     The MultilineStringEditor is a drop down editor, so this returns UITypeEditorEditStyle.DropDown.
        /// </devdoc> 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            return UITypeEditorEditStyle.DropDown;
        } 

        /// <include file='doc\MultilineStringEditor.uex' path='docs/doc[@for="MultilineStringEditor.GetPaintValueSupported"]/*' />
        /// <devdoc>
        ///     Returns false; no extra painting is performed. 
        /// </devdoc>
        public override bool GetPaintValueSupported (ITypeDescriptorContext context) { 
            return false; 
        }
 
        private class MultilineStringEditorUI : RichTextBox {

            private IWindowsFormsEditorService _editorService;
            private bool _editing = false; 
            private bool _escapePressed;    // Initialized in BeginEdit
            private bool _ctrlEnterPressed; 
            SolidBrush _watermarkBrush; 
            private Hashtable _fallbackFonts;
 
            private readonly StringFormat _watermarkFormat;

            // TextBox needs a little space greater than that actualy text content
            // to display the carent. 
            private const int _caretPadding = 3;
 
            // Keep textbox from expanding too close to the edge of the working 
            // area.
            private const int _workAreaPadding = 16; 

            internal MultilineStringEditorUI() {
                InitializeComponent();
                _watermarkFormat = new StringFormat(); 
                _watermarkFormat.Alignment = StringAlignment.Center;
                _watermarkFormat.LineAlignment = StringAlignment.Center; 
                _fallbackFonts = new Hashtable(2); 
            }
 

            private void InitializeComponent() {
                this.RichTextShortcutsEnabled = false;
                this.WordWrap = false; 
                this.BorderStyle = BorderStyle.None;
                this.Multiline = true; 
                this.ScrollBars = RichTextBoxScrollBars.Both; 
                this.DetectUrls = false;
            } 

            protected override void Dispose(bool disposing) {
                if(disposing) {
                    if(_watermarkBrush != null) { 
                        _watermarkBrush.Dispose();
                        _watermarkBrush = null; 
                    } 
                }
                base.Dispose(disposing); 
            }
 	
            [SecurityPermission(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
            protected override object CreateRichEditOleCallback() { 
                return new OleCallback(this);
            } 
 
            protected override bool IsInputKey(Keys keyData) {
                if ((keyData & Keys.KeyCode) == Keys.Return) { 
                    if (Multiline && (keyData & Keys.Alt) == 0) {
                        return true;
                    }
                } 
                return base.IsInputKey(keyData);
            } 
 
            protected override bool ProcessDialogKey(Keys keyData) {
 
                if ((keyData & (Keys.Shift | Keys.Alt)) == 0) {
                    switch (keyData & Keys.KeyCode) {
                        case Keys.Escape:
                            if((keyData & Keys.Control) == 0) { 
                                // Returned by EndEdit to signal that we should disregard changes.
                                _escapePressed = true; 
                            } 
                            break;
                    } 
                }
                return base.ProcessDialogKey(keyData);
            }
 
            protected override void OnKeyDown(KeyEventArgs e) {
                // NDPWhidbey 32414 - The RichTextBox does not always invalidate the entire client area 
                // unless there is a resize, so if you do a newline before you type enough text to resize the 
                // editor the watermark will be ScrollWindowEx'ed down the screen.  To prevent this, we do
                // a full Invalidate if the watermark is showing when a key is pressed. 
                if(ShouldShowWatermark) {
                    Invalidate();
                }
 
                // Ask the editor service to ask my parent to close when the user types "Ctrl+Enter".
                if (e.Control && e.KeyCode == Keys.Return && e.Modifiers == Keys.Control) { 
                    _editorService.CloseDropDown(); 
                    _ctrlEnterPressed = true;
                } 
            }

            internal object Value {
                get { 

                    Debug.Assert(_editing, "Value is only valid between Begin and EndEdit. (Do not want to keep a reference to a large text buffer.)"); 
                    return this.Text; 
                }
            } 

            internal void BeginEdit(IWindowsFormsEditorService editorService, object value) {
                _editing = true;
                _editorService = editorService; 
                _minimumSize = Size.Empty;
                _watermarkSize = Size.Empty; 
                _escapePressed = false; 
                _ctrlEnterPressed = false;
                this.Text = (string) value; 
            }

            internal bool EndEdit() {
                _editing = false; 
                _editorService = null;
                _ctrlEnterPressed = false; 
                this.Text = null; 
                return !_escapePressed;     // If user pressed Esc, return false so we disregard changes.
            } 

            private void ResizeToContent() {
                if (!Visible) {
                    return; 
                }
 
                Size requestedSize = ContentSize; 

                // AdjustWindowRectEx() does not take the WS_VSCROLL or WS_HSCROLL styles into account. 
                // (See NDPWhidbey #11498).  We can not tell when ScrollBars should or shouldn't be visible,
                // so we always add space for them.
                requestedSize.Width += SystemInformation.VerticalScrollBarWidth;
                // NOT USED: requestedSize.Height += SystemInformation.HorizontalScrollBarHeight; 

                // Ensure we do not shrink smaller than our minimum size 
                requestedSize.Width = Math.Max(requestedSize.Width, MinimumSize.Width); 

                Rectangle workingArea = Screen.GetWorkingArea(this); 
                Point location = PointToScreen(this.Location);
                // DANGER:  This assumes we will grow to the left.  This is true for propertygrid
                //          (DropDownHolder::OnCurrentControlResize)
                int maxDelta = location.X - workingArea.Left; 
                // NOTE:  If we are shrinking, requestedWidth will be negative, so the Min will not
                //        bound shrinking by maxDelta.  This is intentional. 
                int requestedDelta = Math.Min((requestedSize.Width - ClientSize.Width), maxDelta); 
                ClientSize = new Size(ClientSize.Width + requestedDelta, MinimumSize.Height);
                Debug.Assert(workingArea.Contains(RectangleToScreen(ClientRectangle)), 
                    "Failed to keep MultilineStringEditor on screen.");
            }

            private Size ContentSize { 
                get {
                    NativeMethods.RECT rect = new NativeMethods.RECT(); 
                    HandleRef hdc = new HandleRef(null, UnsafeNativeMethods.GetDC(NativeMethods.NullHandleRef)); 
                    HandleRef hRtbFont = new HandleRef(null, this.Font.ToHfont());
                    HandleRef hOldFont = new HandleRef(null, SafeNativeMethods.SelectObject(hdc, hRtbFont)); 

                    try {
                        SafeNativeMethods.DrawText(hdc, this.Text, this.Text.Length, ref rect, NativeMethods.DT_CALCRECT);
                    } 
                    finally {
                        NativeMethods.ExternalDeleteObject(hRtbFont); 
                        SafeNativeMethods.SelectObject(hdc, hOldFont); 
                        UnsafeNativeMethods.ReleaseDC(NativeMethods.NullHandleRef, hdc);
                    } 
                    return new Size(rect.right - rect.left + _caretPadding, rect.bottom - rect.top);
                }
            }
 
            private bool _contentsResizedRaised = false;
 
            protected override void OnContentsResized(ContentsResizedEventArgs e) { 
                _contentsResizedRaised = true;
                ResizeToContent(); 
                base.OnContentsResized(e);
            }

            protected override void OnTextChanged(EventArgs e) { 
                // OnContentsResized does not get raised for trailing whitespace.  To work
                // around this, we listen for an OnTextChanged that was not preceeded by 
                // an OnContentsResized.  Changing the box size here is more expensive, 
                // however, so we only want to do it when we have to.
                if(!_contentsResizedRaised) { 
                    ResizeToContent();
                }
                _contentsResizedRaised = false;
                base.OnTextChanged(e); 
            }
 
            protected override void OnVisibleChanged(EventArgs e) { 
                if (this.Visible) {
                    ProcessSurrogateFonts(0, this.Text.Length); 
                    Select(this.Text.Length, 0); // move caret to the end
                }
                ResizeToContent();
                base.OnVisibleChanged(e); 
            }
 
            private Size _minimumSize = Size.Empty; 

            public override Size MinimumSize { 
                get {
                    if(_minimumSize == Size.Empty) {
                        Rectangle workingArea = Screen.GetWorkingArea(this);
                        _minimumSize = new Size( 
                            (int) Math.Min(
                                Math.Ceiling(WatermarkSize.Width * 1.75), 
                                workingArea.Width / 4 
                            ),
                            (int) Math.Min( 
                                Font.Height * 10,
                                workingArea.Height / 4
                            )
                        ); 
                    }
                    return _minimumSize; 
                } 
            }
 
            public override Font Font {
                get {
                    return base.Font;
                } 
                set {
                    return; 
                } 
            }
 
            public void ProcessSurrogateFonts(int start, int length){
                string value = this.Text;
                if(value == null)
                    return; 

                int[] surrogates = StringInfo.ParseCombiningCharacters(value); 
 
                if(surrogates.Length != value.Length) {
                    for(int i=0;i<surrogates.Length;i++) { 
                        if(surrogates[i] >= start && surrogates[i] < start+length) { // only process text in the specified area
                            string fallBackFontName = null;
                            char high = value[surrogates[i]];
                            char low = (char)0x0000; 
                            if(surrogates[i]+1<value.Length) {
                                low = value[surrogates[i]+1]; 
                            } 
                            if(high >= 0xD800 && high <= 0xDBFF) {
                                if(low >=0xDC00 && low <= 0xDFFF) { 
                                    int planeNumber = (high / 0x40) - (0xD800 / 0x40) +1; //plane 0 is the default plane
                                    Font replaceFont = _fallbackFonts[planeNumber] as Font;

                                    if(replaceFont == null ) { 
                                        using(RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\LanguagePack\SurrogateFallback")) {
                                            if(regkey != null) { 
                                                fallBackFontName =  (string)regkey.GetValue("Plane"+planeNumber); 
                                                if(!string.IsNullOrEmpty(fallBackFontName)) {
                                                    replaceFont = new Font(fallBackFontName, base.Font.Size, base.Font.Style); 
                                                }
                                                _fallbackFonts[planeNumber] = replaceFont;
                                            }
                                        } 
                                    }
                                    if(replaceFont != null) { 
                                       int selectionLength = (i==surrogates.Length-1) ? value.Length-surrogates[i] : surrogates[i+1]-surrogates[i]; 
                                       base.Select(surrogates[i], selectionLength);
                                       this.SelectionFont = replaceFont; 
                                    }
                                }
                            }
                        } 
                    }
                } 
            } 

            // VSWhidbey 187367: Override the Text property from RichTextBox so that we can get 
            // the window text from this control without doing a StreamOut operation on the control
            // since StreamOut will cause an IME Composition Window to close unexpectedly.
            public override string Text {
                get { 
                    if (IsHandleCreated) {
                        int textLen = SafeNativeMethods.GetWindowTextLength(new HandleRef(this, Handle)); 
                        StringBuilder sb = new StringBuilder(textLen+1); 
                        UnsafeNativeMethods.GetWindowText(new HandleRef(this, Handle), sb, sb.Capacity);
                        if (!_ctrlEnterPressed) { 
                            return sb.ToString();
                        }
                        else {
                            String str = sb.ToString(); 
                            int index = str.LastIndexOf("\r\n");
                            Debug.Assert(index != -1, "We should have found a Ctrl+Enter in the string"); 
                            return str.Remove(index, 2); 
                        }
                    } 
                    else
                        return "";
                }
                set { 
                    base.Text = value;
                } 
            } 

            #region Watermark 

            private Size _watermarkSize = Size.Empty;

            private Size WatermarkSize { 
                get {
                    if(_watermarkSize == Size.Empty) { 
                        SizeF size; 

                        // See how much space we should reserve for watermark 
                        using(Graphics g = CreateGraphics()) {
                            size = g.MeasureString(
                                SR.GetString(SR.MultilineStringEditorWatermark),
                                this.Font 
                            );
                        } 
                        _watermarkSize = new Size((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height)); 
                    }
                    return _watermarkSize; 
                }
            }

 
            private bool ShouldShowWatermark {
                get { 
                    // Do not show watermark if we already have text 
                    if (this.Text.Length != 0) {
                        return false; 
                    }
                    return WatermarkSize.Width < this.ClientSize.Width;
                }
            } 

            private Brush WatermarkBrush { 
                get { 
                    if(_watermarkBrush == null) {
                        Color cw = SystemColors.Window; 
                        Color ct = SystemColors.WindowText;
                        Color c = Color.FromArgb((Int16)(ct.R * 0.3 + cw.R * 0.7),
                            (Int16)(ct.G * 0.3 + cw.G * 0.7),
                            (Int16)(ct.B * 0.3 + cw.B * 0.7)); 
                        _watermarkBrush = new SolidBrush(c);
                    } 
                    return _watermarkBrush; 
                }
            } 

            protected override void WndProc(ref Message m) {
                base.WndProc(ref m);
                switch (m.Msg) { 
                    case NativeMethods.WM_PAINT: {
                        if(ShouldShowWatermark) { 
                            using(Graphics g = CreateGraphics()) { 
                                g.DrawString(
                                    SR.GetString(SR.MultilineStringEditorWatermark), 
                                    this.Font,
                                    WatermarkBrush,
                                    new RectangleF(0.0f, 0.0f, this.ClientSize.Width, this.ClientSize.Height),
                                    _watermarkFormat 
                                );
                            } 
                        } 
                        break;
                    } 
                }
            }
            #endregion
        } 

        // I used the visual basic 6 RichText (REOleCB.CPP) as a guide for this 
        private class OleCallback : UnsafeNativeMethods.IRichTextBoxOleCallback { 

            private RichTextBox owner; 
            bool unrestricted = false;
            static TraceSwitch richTextDbg;

            static TraceSwitch RichTextDbg { 
                get {
                    if (richTextDbg == null) { 
                        richTextDbg = new TraceSwitch("RichTextDbg", "Debug info about RichTextBox"); 
                    }
                    return richTextDbg; 
                }
            }

            internal OleCallback(RichTextBox owner) { 
                this.owner = owner;
            } 
 

            public int GetNewStorage(out UnsafeNativeMethods.IStorage storage) { 
                Debug.WriteLineIf(RichTextDbg.TraceVerbose, "IRichTextBoxOleCallback::GetNewStorage");
                // Debug.WriteLine("get new storage");
                UnsafeNativeMethods.ILockBytes pLockBytes = UnsafeNativeMethods.CreateILockBytesOnHGlobal(NativeMethods.NullHandleRef, true);
 
                Debug.Assert(pLockBytes != null, "pLockBytes is NULL!");
 
                storage = UnsafeNativeMethods.StgCreateDocfileOnILockBytes(pLockBytes, 
                                                                           NativeMethods.STGM_SHARE_EXCLUSIVE | NativeMethods.STGM_CREATE | NativeMethods.STGM_READWRITE,
                                                                           0); 
                Debug.Assert(storage != null, "storage is NULL!");

                return NativeMethods.S_OK;
            } 

            public int GetInPlaceContext(IntPtr lplpFrame, 
                                         IntPtr lplpDoc, 
                                         IntPtr lpFrameInfo) {
                Debug.WriteLineIf(RichTextDbg.TraceVerbose, "IRichTextBoxOleCallback::GetInPlaceContext"); 
                return NativeMethods.E_NOTIMPL;
            }

            public int ShowContainerUI(int fShow) { 
                Debug.WriteLineIf(RichTextDbg.TraceVerbose, "IRichTextBoxOleCallback::ShowContainerUI");
                // Do nothing 
                return NativeMethods.S_OK; 
            }
 
            public int QueryInsertObject(ref Guid lpclsid, IntPtr lpstg,
                                         int cp) {
                Debug.WriteLineIf(RichTextDbg.TraceVerbose, "IRichTextBoxOleCallback::QueryInsertObject(" + lpclsid.ToString() + ")");
                if (unrestricted) { 
                    return NativeMethods.S_OK;
                } 
                else { 
                    Guid realClsid = new Guid();
 

                    int hr = UnsafeNativeMethods.ReadClassStg(new HandleRef(null, lpstg), ref realClsid);
                    Debug.WriteLineIf(RichTextDbg.TraceVerbose, "real clsid:" + realClsid.ToString() + " (hr=" + hr.ToString("X", CultureInfo.InvariantCulture) + ")");
 
                    if (!NativeMethods.Succeeded(hr)) {
                        return NativeMethods.S_FALSE; 
                    } 

                    if (realClsid == Guid.Empty) { 
                        realClsid = lpclsid;
                    }

                    switch (realClsid.ToString().ToUpper(CultureInfo.InvariantCulture)) { 
                        case "00000315-0000-0000-C000-000000000046": // Metafile
                        case "00000316-0000-0000-C000-000000000046": // DIB 
                        case "00000319-0000-0000-C000-000000000046": // EMF 
                        case "0003000A-0000-0000-C000-000000000046": //BMP
                            return NativeMethods.S_OK; 
                        default:
                            Debug.WriteLineIf(RichTextDbg.TraceVerbose, "   denying '" + lpclsid.ToString() + "' from being inserted due to security restrictions");
                            return NativeMethods.S_FALSE;
                    } 
                }
            } 
 
            public int DeleteObject(IntPtr lpoleobj) {
                Debug.WriteLineIf(RichTextDbg.TraceVerbose, "IRichTextBoxOleCallback::DeleteObject"); 
                // Do nothing
                return NativeMethods.S_OK;
            }
 
            public int QueryAcceptData(IComDataObject lpdataobj,
                                       /* CLIPFORMAT* */ IntPtr lpcfFormat, int reco, 
                                       int fReally, IntPtr hMetaPict) { 
                Debug.WriteLineIf(RichTextDbg.TraceVerbose, "IRichTextBoxOleCallback::QueryAcceptData(reco=" + reco + ")");
 
                if (reco == NativeMethods.RECO_PASTE){
                    DataObject dataObj = new DataObject(lpdataobj);
                    if (dataObj != null &&
                        (dataObj.GetDataPresent(DataFormats.Text) || dataObj.GetDataPresent(DataFormats.UnicodeText))) { 
                        return NativeMethods.S_OK;
                    } 
 
                    return NativeMethods.E_FAIL;
                } 
                else {
                    return NativeMethods.E_NOTIMPL;
                }
            } 

            public int ContextSensitiveHelp(int fEnterMode) { 
                Debug.WriteLineIf(RichTextDbg.TraceVerbose, "IRichTextBoxOleCallback::ContextSensitiveHelp"); 
                return NativeMethods.E_NOTIMPL;
            } 

            public int GetClipboardData(NativeMethods.CHARRANGE lpchrg, int reco,
                                        IntPtr lplpdataobj) {
                Debug.WriteLineIf(RichTextDbg.TraceVerbose, "IRichTextBoxOleCallback::GetClipboardData"); 
                return NativeMethods.E_NOTIMPL;
            } 
 
            public int GetDragDropEffect(bool fDrag, int grfKeyState, ref int pdwEffect) {
                pdwEffect = (int)DragDropEffects.None; 
                return NativeMethods.S_OK;
            }

            public int GetContextMenu(short seltype, IntPtr lpoleobj, NativeMethods.CHARRANGE lpchrg, out IntPtr hmenu) { 
                TextBox tb = new TextBox();
                tb.Visible = true; 
                ContextMenu cm = tb.ContextMenu; 
                if (cm == null || owner.ShortcutsEnabled == false)
                    hmenu = IntPtr.Zero; 
                else {
                    /*cm.sourceControl = owner;
                    cm.OnPopup(EventArgs.Empty);
                    // RichEd calls DestroyMenu after displaying the context menu 
                    IntPtr handle = cm.Handle;
                    // if another control shares the same context menu 
                    // then we have to mark the context menu's handles empty because 
                    // RichTextBox will delete the menu handles once the popup menu is dismissed.
                    Menu menu = cm; 
                    while (true) {
                        int i = 0;
                        int count = menu.ItemCount;
                        for (; i< count; i++) { 
                            if (menu.items[i].handle != IntPtr.Zero) {
                                menu = menu.items[i]; 
                                break; 
                            }
                        } 
                        if (i == count) {
                            menu.handle = IntPtr.Zero;
                            menu.created = false;
                            if (menu == cm) 
                                break;
                            else 
                                menu = ((MenuItem) menu).Menu; 
                        }
                    }*/ 

                    hmenu = cm.Handle;
                }
 
                return NativeMethods.S_OK;
            } 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="MultilineStringEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Design;
    using System.Diagnostics;
    using System.Drawing; 
    using System.Drawing.Design;
    using System.Reflection; 
    using System.Runtime.InteropServices; 
    using System.Windows.Forms;
    using System.Windows.Forms.ComponentModel; 
    using System.Windows.Forms.Design;
    using System.Security;
    using System.Security.Permissions;
    using Microsoft.Win32; 
    using System.Globalization;
    using System.IO; 
    using System.Runtime.Remoting; 
    using System.Runtime.Serialization.Formatters;
    using System.Text; 

    using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

    /// <include file='doc\MultilineStringEditor.uex' path='docs/doc[@for="MultilineStringEditor"]/*' /> 
    /// <devdoc>
    ///     An editor for editing strings that supports multiple lines of text and is resizable. 
    /// </devdoc> 
    public sealed class MultilineStringEditor : UITypeEditor {
 
        private MultilineStringEditorUI _editorUI = null;

        /// <include file='doc\MultilineStringEditor.uex' path='docs/doc[@for="MultilineStringEditor.EditValue"]/*' />
        /// <devdoc> 
        ///     Edits the given value, returning the editing results.
        /// </devdoc> 
        public override object EditValue (ITypeDescriptorContext context, 
            IServiceProvider provider,
            object value) { 

            if (provider != null) {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                if (edSvc != null) { 
                    if (_editorUI == null) {
                        _editorUI = new MultilineStringEditorUI(); 
                    } 
                    _editorUI.BeginEdit(edSvc, value);
 
                    edSvc.DropDownControl(_editorUI);

                    object newValue = _editorUI.Value;
                    if(_editorUI.EndEdit()) { 
                        value = newValue;
                    } 
                } 
            }
            return value; 
        }

        /// <include file='doc\MultilineStringEditor.uex' path='docs/doc[@for="MultilineStringEditor.GetEditStyle"]/*' />
        /// <devdoc> 
        ///     The MultilineStringEditor is a drop down editor, so this returns UITypeEditorEditStyle.DropDown.
        /// </devdoc> 
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { 
            return UITypeEditorEditStyle.DropDown;
        } 

        /// <include file='doc\MultilineStringEditor.uex' path='docs/doc[@for="MultilineStringEditor.GetPaintValueSupported"]/*' />
        /// <devdoc>
        ///     Returns false; no extra painting is performed. 
        /// </devdoc>
        public override bool GetPaintValueSupported (ITypeDescriptorContext context) { 
            return false; 
        }
 
        private class MultilineStringEditorUI : RichTextBox {

            private IWindowsFormsEditorService _editorService;
            private bool _editing = false; 
            private bool _escapePressed;    // Initialized in BeginEdit
            private bool _ctrlEnterPressed; 
            SolidBrush _watermarkBrush; 
            private Hashtable _fallbackFonts;
 
            private readonly StringFormat _watermarkFormat;

            // TextBox needs a little space greater than that actualy text content
            // to display the carent. 
            private const int _caretPadding = 3;
 
            // Keep textbox from expanding too close to the edge of the working 
            // area.
            private const int _workAreaPadding = 16; 

            internal MultilineStringEditorUI() {
                InitializeComponent();
                _watermarkFormat = new StringFormat(); 
                _watermarkFormat.Alignment = StringAlignment.Center;
                _watermarkFormat.LineAlignment = StringAlignment.Center; 
                _fallbackFonts = new Hashtable(2); 
            }
 

            private void InitializeComponent() {
                this.RichTextShortcutsEnabled = false;
                this.WordWrap = false; 
                this.BorderStyle = BorderStyle.None;
                this.Multiline = true; 
                this.ScrollBars = RichTextBoxScrollBars.Both; 
                this.DetectUrls = false;
            } 

            protected override void Dispose(bool disposing) {
                if(disposing) {
                    if(_watermarkBrush != null) { 
                        _watermarkBrush.Dispose();
                        _watermarkBrush = null; 
                    } 
                }
                base.Dispose(disposing); 
            }
 	
            [SecurityPermission(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
            protected override object CreateRichEditOleCallback() { 
                return new OleCallback(this);
            } 
 
            protected override bool IsInputKey(Keys keyData) {
                if ((keyData & Keys.KeyCode) == Keys.Return) { 
                    if (Multiline && (keyData & Keys.Alt) == 0) {
                        return true;
                    }
                } 
                return base.IsInputKey(keyData);
            } 
 
            protected override bool ProcessDialogKey(Keys keyData) {
 
                if ((keyData & (Keys.Shift | Keys.Alt)) == 0) {
                    switch (keyData & Keys.KeyCode) {
                        case Keys.Escape:
                            if((keyData & Keys.Control) == 0) { 
                                // Returned by EndEdit to signal that we should disregard changes.
                                _escapePressed = true; 
                            } 
                            break;
                    } 
                }
                return base.ProcessDialogKey(keyData);
            }
 
            protected override void OnKeyDown(KeyEventArgs e) {
                // NDPWhidbey 32414 - The RichTextBox does not always invalidate the entire client area 
                // unless there is a resize, so if you do a newline before you type enough text to resize the 
                // editor the watermark will be ScrollWindowEx'ed down the screen.  To prevent this, we do
                // a full Invalidate if the watermark is showing when a key is pressed. 
                if(ShouldShowWatermark) {
                    Invalidate();
                }
 
                // Ask the editor service to ask my parent to close when the user types "Ctrl+Enter".
                if (e.Control && e.KeyCode == Keys.Return && e.Modifiers == Keys.Control) { 
                    _editorService.CloseDropDown(); 
                    _ctrlEnterPressed = true;
                } 
            }

            internal object Value {
                get { 

                    Debug.Assert(_editing, "Value is only valid between Begin and EndEdit. (Do not want to keep a reference to a large text buffer.)"); 
                    return this.Text; 
                }
            } 

            internal void BeginEdit(IWindowsFormsEditorService editorService, object value) {
                _editing = true;
                _editorService = editorService; 
                _minimumSize = Size.Empty;
                _watermarkSize = Size.Empty; 
                _escapePressed = false; 
                _ctrlEnterPressed = false;
                this.Text = (string) value; 
            }

            internal bool EndEdit() {
                _editing = false; 
                _editorService = null;
                _ctrlEnterPressed = false; 
                this.Text = null; 
                return !_escapePressed;     // If user pressed Esc, return false so we disregard changes.
            } 

            private void ResizeToContent() {
                if (!Visible) {
                    return; 
                }
 
                Size requestedSize = ContentSize; 

                // AdjustWindowRectEx() does not take the WS_VSCROLL or WS_HSCROLL styles into account. 
                // (See NDPWhidbey #11498).  We can not tell when ScrollBars should or shouldn't be visible,
                // so we always add space for them.
                requestedSize.Width += SystemInformation.VerticalScrollBarWidth;
                // NOT USED: requestedSize.Height += SystemInformation.HorizontalScrollBarHeight; 

                // Ensure we do not shrink smaller than our minimum size 
                requestedSize.Width = Math.Max(requestedSize.Width, MinimumSize.Width); 

                Rectangle workingArea = Screen.GetWorkingArea(this); 
                Point location = PointToScreen(this.Location);
                // DANGER:  This assumes we will grow to the left.  This is true for propertygrid
                //          (DropDownHolder::OnCurrentControlResize)
                int maxDelta = location.X - workingArea.Left; 
                // NOTE:  If we are shrinking, requestedWidth will be negative, so the Min will not
                //        bound shrinking by maxDelta.  This is intentional. 
                int requestedDelta = Math.Min((requestedSize.Width - ClientSize.Width), maxDelta); 
                ClientSize = new Size(ClientSize.Width + requestedDelta, MinimumSize.Height);
                Debug.Assert(workingArea.Contains(RectangleToScreen(ClientRectangle)), 
                    "Failed to keep MultilineStringEditor on screen.");
            }

            private Size ContentSize { 
                get {
                    NativeMethods.RECT rect = new NativeMethods.RECT(); 
                    HandleRef hdc = new HandleRef(null, UnsafeNativeMethods.GetDC(NativeMethods.NullHandleRef)); 
                    HandleRef hRtbFont = new HandleRef(null, this.Font.ToHfont());
                    HandleRef hOldFont = new HandleRef(null, SafeNativeMethods.SelectObject(hdc, hRtbFont)); 

                    try {
                        SafeNativeMethods.DrawText(hdc, this.Text, this.Text.Length, ref rect, NativeMethods.DT_CALCRECT);
                    } 
                    finally {
                        NativeMethods.ExternalDeleteObject(hRtbFont); 
                        SafeNativeMethods.SelectObject(hdc, hOldFont); 
                        UnsafeNativeMethods.ReleaseDC(NativeMethods.NullHandleRef, hdc);
                    } 
                    return new Size(rect.right - rect.left + _caretPadding, rect.bottom - rect.top);
                }
            }
 
            private bool _contentsResizedRaised = false;
 
            protected override void OnContentsResized(ContentsResizedEventArgs e) { 
                _contentsResizedRaised = true;
                ResizeToContent(); 
                base.OnContentsResized(e);
            }

            protected override void OnTextChanged(EventArgs e) { 
                // OnContentsResized does not get raised for trailing whitespace.  To work
                // around this, we listen for an OnTextChanged that was not preceeded by 
                // an OnContentsResized.  Changing the box size here is more expensive, 
                // however, so we only want to do it when we have to.
                if(!_contentsResizedRaised) { 
                    ResizeToContent();
                }
                _contentsResizedRaised = false;
                base.OnTextChanged(e); 
            }
 
            protected override void OnVisibleChanged(EventArgs e) { 
                if (this.Visible) {
                    ProcessSurrogateFonts(0, this.Text.Length); 
                    Select(this.Text.Length, 0); // move caret to the end
                }
                ResizeToContent();
                base.OnVisibleChanged(e); 
            }
 
            private Size _minimumSize = Size.Empty; 

            public override Size MinimumSize { 
                get {
                    if(_minimumSize == Size.Empty) {
                        Rectangle workingArea = Screen.GetWorkingArea(this);
                        _minimumSize = new Size( 
                            (int) Math.Min(
                                Math.Ceiling(WatermarkSize.Width * 1.75), 
                                workingArea.Width / 4 
                            ),
                            (int) Math.Min( 
                                Font.Height * 10,
                                workingArea.Height / 4
                            )
                        ); 
                    }
                    return _minimumSize; 
                } 
            }
 
            public override Font Font {
                get {
                    return base.Font;
                } 
                set {
                    return; 
                } 
            }
 
            public void ProcessSurrogateFonts(int start, int length){
                string value = this.Text;
                if(value == null)
                    return; 

                int[] surrogates = StringInfo.ParseCombiningCharacters(value); 
 
                if(surrogates.Length != value.Length) {
                    for(int i=0;i<surrogates.Length;i++) { 
                        if(surrogates[i] >= start && surrogates[i] < start+length) { // only process text in the specified area
                            string fallBackFontName = null;
                            char high = value[surrogates[i]];
                            char low = (char)0x0000; 
                            if(surrogates[i]+1<value.Length) {
                                low = value[surrogates[i]+1]; 
                            } 
                            if(high >= 0xD800 && high <= 0xDBFF) {
                                if(low >=0xDC00 && low <= 0xDFFF) { 
                                    int planeNumber = (high / 0x40) - (0xD800 / 0x40) +1; //plane 0 is the default plane
                                    Font replaceFont = _fallbackFonts[planeNumber] as Font;

                                    if(replaceFont == null ) { 
                                        using(RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\LanguagePack\SurrogateFallback")) {
                                            if(regkey != null) { 
                                                fallBackFontName =  (string)regkey.GetValue("Plane"+planeNumber); 
                                                if(!string.IsNullOrEmpty(fallBackFontName)) {
                                                    replaceFont = new Font(fallBackFontName, base.Font.Size, base.Font.Style); 
                                                }
                                                _fallbackFonts[planeNumber] = replaceFont;
                                            }
                                        } 
                                    }
                                    if(replaceFont != null) { 
                                       int selectionLength = (i==surrogates.Length-1) ? value.Length-surrogates[i] : surrogates[i+1]-surrogates[i]; 
                                       base.Select(surrogates[i], selectionLength);
                                       this.SelectionFont = replaceFont; 
                                    }
                                }
                            }
                        } 
                    }
                } 
            } 

            // VSWhidbey 187367: Override the Text property from RichTextBox so that we can get 
            // the window text from this control without doing a StreamOut operation on the control
            // since StreamOut will cause an IME Composition Window to close unexpectedly.
            public override string Text {
                get { 
                    if (IsHandleCreated) {
                        int textLen = SafeNativeMethods.GetWindowTextLength(new HandleRef(this, Handle)); 
                        StringBuilder sb = new StringBuilder(textLen+1); 
                        UnsafeNativeMethods.GetWindowText(new HandleRef(this, Handle), sb, sb.Capacity);
                        if (!_ctrlEnterPressed) { 
                            return sb.ToString();
                        }
                        else {
                            String str = sb.ToString(); 
                            int index = str.LastIndexOf("\r\n");
                            Debug.Assert(index != -1, "We should have found a Ctrl+Enter in the string"); 
                            return str.Remove(index, 2); 
                        }
                    } 
                    else
                        return "";
                }
                set { 
                    base.Text = value;
                } 
            } 

            #region Watermark 

            private Size _watermarkSize = Size.Empty;

            private Size WatermarkSize { 
                get {
                    if(_watermarkSize == Size.Empty) { 
                        SizeF size; 

                        // See how much space we should reserve for watermark 
                        using(Graphics g = CreateGraphics()) {
                            size = g.MeasureString(
                                SR.GetString(SR.MultilineStringEditorWatermark),
                                this.Font 
                            );
                        } 
                        _watermarkSize = new Size((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height)); 
                    }
                    return _watermarkSize; 
                }
            }

 
            private bool ShouldShowWatermark {
                get { 
                    // Do not show watermark if we already have text 
                    if (this.Text.Length != 0) {
                        return false; 
                    }
                    return WatermarkSize.Width < this.ClientSize.Width;
                }
            } 

            private Brush WatermarkBrush { 
                get { 
                    if(_watermarkBrush == null) {
                        Color cw = SystemColors.Window; 
                        Color ct = SystemColors.WindowText;
                        Color c = Color.FromArgb((Int16)(ct.R * 0.3 + cw.R * 0.7),
                            (Int16)(ct.G * 0.3 + cw.G * 0.7),
                            (Int16)(ct.B * 0.3 + cw.B * 0.7)); 
                        _watermarkBrush = new SolidBrush(c);
                    } 
                    return _watermarkBrush; 
                }
            } 

            protected override void WndProc(ref Message m) {
                base.WndProc(ref m);
                switch (m.Msg) { 
                    case NativeMethods.WM_PAINT: {
                        if(ShouldShowWatermark) { 
                            using(Graphics g = CreateGraphics()) { 
                                g.DrawString(
                                    SR.GetString(SR.MultilineStringEditorWatermark), 
                                    this.Font,
                                    WatermarkBrush,
                                    new RectangleF(0.0f, 0.0f, this.ClientSize.Width, this.ClientSize.Height),
                                    _watermarkFormat 
                                );
                            } 
                        } 
                        break;
                    } 
                }
            }
            #endregion
        } 

        // I used the visual basic 6 RichText (REOleCB.CPP) as a guide for this 
        private class OleCallback : UnsafeNativeMethods.IRichTextBoxOleCallback { 

            private RichTextBox owner; 
            bool unrestricted = false;
            static TraceSwitch richTextDbg;

            static TraceSwitch RichTextDbg { 
                get {
                    if (richTextDbg == null) { 
                        richTextDbg = new TraceSwitch("RichTextDbg", "Debug info about RichTextBox"); 
                    }
                    return richTextDbg; 
                }
            }

            internal OleCallback(RichTextBox owner) { 
                this.owner = owner;
            } 
 

            public int GetNewStorage(out UnsafeNativeMethods.IStorage storage) { 
                Debug.WriteLineIf(RichTextDbg.TraceVerbose, "IRichTextBoxOleCallback::GetNewStorage");
                // Debug.WriteLine("get new storage");
                UnsafeNativeMethods.ILockBytes pLockBytes = UnsafeNativeMethods.CreateILockBytesOnHGlobal(NativeMethods.NullHandleRef, true);
 
                Debug.Assert(pLockBytes != null, "pLockBytes is NULL!");
 
                storage = UnsafeNativeMethods.StgCreateDocfileOnILockBytes(pLockBytes, 
                                                                           NativeMethods.STGM_SHARE_EXCLUSIVE | NativeMethods.STGM_CREATE | NativeMethods.STGM_READWRITE,
                                                                           0); 
                Debug.Assert(storage != null, "storage is NULL!");

                return NativeMethods.S_OK;
            } 

            public int GetInPlaceContext(IntPtr lplpFrame, 
                                         IntPtr lplpDoc, 
                                         IntPtr lpFrameInfo) {
                Debug.WriteLineIf(RichTextDbg.TraceVerbose, "IRichTextBoxOleCallback::GetInPlaceContext"); 
                return NativeMethods.E_NOTIMPL;
            }

            public int ShowContainerUI(int fShow) { 
                Debug.WriteLineIf(RichTextDbg.TraceVerbose, "IRichTextBoxOleCallback::ShowContainerUI");
                // Do nothing 
                return NativeMethods.S_OK; 
            }
 
            public int QueryInsertObject(ref Guid lpclsid, IntPtr lpstg,
                                         int cp) {
                Debug.WriteLineIf(RichTextDbg.TraceVerbose, "IRichTextBoxOleCallback::QueryInsertObject(" + lpclsid.ToString() + ")");
                if (unrestricted) { 
                    return NativeMethods.S_OK;
                } 
                else { 
                    Guid realClsid = new Guid();
 

                    int hr = UnsafeNativeMethods.ReadClassStg(new HandleRef(null, lpstg), ref realClsid);
                    Debug.WriteLineIf(RichTextDbg.TraceVerbose, "real clsid:" + realClsid.ToString() + " (hr=" + hr.ToString("X", CultureInfo.InvariantCulture) + ")");
 
                    if (!NativeMethods.Succeeded(hr)) {
                        return NativeMethods.S_FALSE; 
                    } 

                    if (realClsid == Guid.Empty) { 
                        realClsid = lpclsid;
                    }

                    switch (realClsid.ToString().ToUpper(CultureInfo.InvariantCulture)) { 
                        case "00000315-0000-0000-C000-000000000046": // Metafile
                        case "00000316-0000-0000-C000-000000000046": // DIB 
                        case "00000319-0000-0000-C000-000000000046": // EMF 
                        case "0003000A-0000-0000-C000-000000000046": //BMP
                            return NativeMethods.S_OK; 
                        default:
                            Debug.WriteLineIf(RichTextDbg.TraceVerbose, "   denying '" + lpclsid.ToString() + "' from being inserted due to security restrictions");
                            return NativeMethods.S_FALSE;
                    } 
                }
            } 
 
            public int DeleteObject(IntPtr lpoleobj) {
                Debug.WriteLineIf(RichTextDbg.TraceVerbose, "IRichTextBoxOleCallback::DeleteObject"); 
                // Do nothing
                return NativeMethods.S_OK;
            }
 
            public int QueryAcceptData(IComDataObject lpdataobj,
                                       /* CLIPFORMAT* */ IntPtr lpcfFormat, int reco, 
                                       int fReally, IntPtr hMetaPict) { 
                Debug.WriteLineIf(RichTextDbg.TraceVerbose, "IRichTextBoxOleCallback::QueryAcceptData(reco=" + reco + ")");
 
                if (reco == NativeMethods.RECO_PASTE){
                    DataObject dataObj = new DataObject(lpdataobj);
                    if (dataObj != null &&
                        (dataObj.GetDataPresent(DataFormats.Text) || dataObj.GetDataPresent(DataFormats.UnicodeText))) { 
                        return NativeMethods.S_OK;
                    } 
 
                    return NativeMethods.E_FAIL;
                } 
                else {
                    return NativeMethods.E_NOTIMPL;
                }
            } 

            public int ContextSensitiveHelp(int fEnterMode) { 
                Debug.WriteLineIf(RichTextDbg.TraceVerbose, "IRichTextBoxOleCallback::ContextSensitiveHelp"); 
                return NativeMethods.E_NOTIMPL;
            } 

            public int GetClipboardData(NativeMethods.CHARRANGE lpchrg, int reco,
                                        IntPtr lplpdataobj) {
                Debug.WriteLineIf(RichTextDbg.TraceVerbose, "IRichTextBoxOleCallback::GetClipboardData"); 
                return NativeMethods.E_NOTIMPL;
            } 
 
            public int GetDragDropEffect(bool fDrag, int grfKeyState, ref int pdwEffect) {
                pdwEffect = (int)DragDropEffects.None; 
                return NativeMethods.S_OK;
            }

            public int GetContextMenu(short seltype, IntPtr lpoleobj, NativeMethods.CHARRANGE lpchrg, out IntPtr hmenu) { 
                TextBox tb = new TextBox();
                tb.Visible = true; 
                ContextMenu cm = tb.ContextMenu; 
                if (cm == null || owner.ShortcutsEnabled == false)
                    hmenu = IntPtr.Zero; 
                else {
                    /*cm.sourceControl = owner;
                    cm.OnPopup(EventArgs.Empty);
                    // RichEd calls DestroyMenu after displaying the context menu 
                    IntPtr handle = cm.Handle;
                    // if another control shares the same context menu 
                    // then we have to mark the context menu's handles empty because 
                    // RichTextBox will delete the menu handles once the popup menu is dismissed.
                    Menu menu = cm; 
                    while (true) {
                        int i = 0;
                        int count = menu.ItemCount;
                        for (; i< count; i++) { 
                            if (menu.items[i].handle != IntPtr.Zero) {
                                menu = menu.items[i]; 
                                break; 
                            }
                        } 
                        if (i == count) {
                            menu.handle = IntPtr.Zero;
                            menu.created = false;
                            if (menu == cm) 
                                break;
                            else 
                                menu = ((MenuItem) menu).Menu; 
                        }
                    }*/ 

                    hmenu = cm.Handle;
                }
 
                return NativeMethods.S_OK;
            } 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
