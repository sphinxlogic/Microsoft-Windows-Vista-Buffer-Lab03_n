namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Drawing.Drawing2D;
    using System.Windows.Forms.Design; 
    using System.Windows.Forms;

    /// <include file='doc\ToolStripPanelSelectionGlyph.uex' path='docs/doc[@for="ToolStripPanelSelectionGlyph"]/*' />
    /// <devdoc> 
    /// </devdoc>
    internal sealed class ToolStripPanelSelectionGlyph : ControlBodyGlyph { 
 
        private ToolStripPanel relatedPanel;
        private Rectangle glyphBounds; 
        private IServiceProvider provider;
        private ToolStripPanelSelectionBehavior relatedBehavior;
        private Image image = null;
 
        private Control baseParent = null;
        private BehaviorService         behaviorService; 
 

        private bool isExpanded = false; 

        private const int imageWidth = 50;
        private const int imageHeight = 6;
 
        /// <include file='doc\ToolStripPanelSelectionGlyph.uex' path='docs/doc[@for="ToolStripPanelSelectionGlyph.ToolStripPanelSelectionGlyph"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        internal ToolStripPanelSelectionGlyph(Rectangle bounds, Cursor cursor, IComponent relatedComponent, IServiceProvider provider, ToolStripPanelSelectionBehavior behavior) : base(bounds, cursor, relatedComponent, behavior) {
 
            relatedBehavior = behavior;
            this.provider = provider;
            this.relatedPanel = relatedComponent as ToolStripPanel;
 

            this.behaviorService = (BehaviorService)provider.GetService(typeof(BehaviorService)); 
            if (behaviorService == null) { 
                Debug.Fail("Could not get the BehaviorService");
                return; 
            }

            IDesignerHost host = (IDesignerHost)provider.GetService(typeof(IDesignerHost));
            if (host == null) { 
                Debug.Fail("Could not get the DesignerHost");
                return; 
            } 

 
            UpdateGlyph();
        }

        public bool IsExpanded 
        {
            get 
            { 
                return isExpanded;
            } 
            set
            {
                if (value != isExpanded)
                { 
                    isExpanded = value;
                    UpdateGlyph(); 
                } 
            }
        } 


        public void UpdateGlyph()
        { 
            if (behaviorService != null)
            { 
                Rectangle translatedBounds = behaviorService.ControlRectInAdornerWindow(relatedPanel); 
                //Reset the glyph.
                this.glyphBounds = Rectangle.Empty; 

                // Refresh the parent
                ToolStripContainer parent = relatedPanel.Parent as ToolStripContainer;
                if (parent != null) 
                {
                    this.baseParent = parent.Parent; // get the control to which ToolStripContainer is added... 
                } 

                if (!isExpanded) 
                {
                    CollapseGlyph(translatedBounds);
                }
                else 
                {
                    ExpandGlyph(translatedBounds); 
                } 
            }
        } 


        private void CollapseGlyph(Rectangle bounds)
        { 
            DockStyle dock = relatedPanel.Dock;
            int x = 0; 
            int y = 0; 

            switch(dock) 
            {
                case DockStyle.Top :
                    this.image = new Bitmap(typeof(ToolStripPanelSelectionGlyph), "topopen.bmp");
                    x = (bounds.Width - imageWidth) /2; 
                    if (x > 0)
                    { 
                        this.glyphBounds = new Rectangle(bounds.X + x, bounds.Y + bounds.Height, imageWidth, imageHeight); 
                    }
                    break; 
                case DockStyle.Bottom :
                    this.image = new Bitmap(typeof(ToolStripPanelSelectionGlyph), "bottomopen.bmp");
                    x = (bounds.Width - imageWidth) /2;
                    if (x > 0) 
                    {
                        this.glyphBounds = new Rectangle(bounds.X + x, bounds.Y - imageHeight, imageWidth, imageHeight); 
                    } 
 					break;
                case DockStyle.Left : 
                    this.image = new Bitmap(typeof(ToolStripPanelSelectionGlyph), "leftopen.bmp");
                    y = (bounds.Height - imageWidth) /2;
                    if (y > 0)
                    { 
                        this.glyphBounds = new Rectangle(bounds.X + bounds.Width, bounds.Y + y, imageHeight, imageWidth);
                    } 
                    break; 
                case DockStyle.Right :
                    this.image = new Bitmap(typeof(ToolStripPanelSelectionGlyph), "rightopen.bmp"); 
                    y = (bounds.Height - imageWidth) /2;
                    if (y > 0)
                    {
                        this.glyphBounds = new Rectangle(bounds.X - imageHeight , bounds.Y + y, imageHeight, imageWidth); 
                    }
                    break; 
                default: 
                    throw new Exception(SR.GetString(SR.ToolStripPanelGlyphUnsupportedDock));
            } 
        }


 
        private void ExpandGlyph(Rectangle bounds)
        { 
            DockStyle dock = relatedPanel.Dock; 
            int x = 0;
            int y = 0; 

            switch(dock)
            {
                case DockStyle.Top : 
                    this.image = new Bitmap(typeof(ToolStripPanelSelectionGlyph), "topclose.bmp");
                    x = (bounds.Width - imageWidth) /2; 
                    if (x > 0) 
                    {
                        this.glyphBounds = new Rectangle(bounds.X + x, bounds.Y + bounds.Height, imageWidth, imageHeight); 
                    }
                    break;
                case DockStyle.Bottom :
                    this.image = new Bitmap(typeof(ToolStripPanelSelectionGlyph), "bottomclose.bmp"); 
                    x = (bounds.Width - imageWidth) /2;
                    if (x > 0) 
                    { 
                        this.glyphBounds = new Rectangle(bounds.X + x, bounds.Y -imageHeight, imageWidth, imageHeight);
                    } 
					break;
                case DockStyle.Left :
                    this.image = new Bitmap(typeof(ToolStripPanelSelectionGlyph), "leftclose.bmp");
                    y = (bounds.Height - imageWidth) /2; 
                    if (y > 0)
                    { 
                        this.glyphBounds = new Rectangle(bounds.X + bounds.Width, bounds.Y + y, imageHeight, imageWidth); 
                    }
                    break; 
                case DockStyle.Right :
                    this.image = new Bitmap(typeof(ToolStripPanelSelectionGlyph), "rightclose.bmp");
                    y = (bounds.Height - imageWidth) /2;
                    if (y > 0) 
                    {
                        this.glyphBounds = new Rectangle(bounds.X - imageHeight, bounds.Y + y, imageHeight, imageWidth); 
                    } 
					break;
                default: 
                    throw new Exception(SR.GetString(SR.ToolStripPanelGlyphUnsupportedDock));
            }
        }
 

        /// <include file='doc\ToolStripPanelSelectionGlyph.uex' path='docs/doc[@for="ToolStripPanelSelectionGlyph.Bounds"]/*' /> 
        /// <devdoc> 
        ///     The bounds of this Glyph.
        /// </devdoc> 
        public override Rectangle Bounds {
            get  {
                return glyphBounds;
            } 
        }
 
 
        /// <include file='doc\ToolStripPanelSelectionGlyph.uex' path='docs/doc[@for="ToolStripPanelSelectionGlyph.GetHitTest"]/*' />
        /// <devdoc> 
        ///     Simple hit test rule: if the point is contained within the bounds
        ///     - then it is a positive hit test.
        /// </devdoc>
        public override Cursor GetHitTest(Point p) { 
            if (behaviorService != null && baseParent != null)
            { 
                Rectangle baseParentBounds = behaviorService.ControlRectInAdornerWindow(baseParent); 
                if (glyphBounds != Rectangle.Empty && baseParentBounds.Contains(glyphBounds) && glyphBounds.Contains(p)) {
                    return Cursors.Hand; 
                }
            }
            return null;
        } 

 
        /// <include file='doc\ToolStripPanelSelectionGlyph.uex' path='docs/doc[@for="ToolStripPanelSelectionGlyph.Paint"]/*' /> 
        /// <devdoc>
        ///     Very simple paint logic. 
        /// </devdoc>
        public override void Paint(PaintEventArgs pe) {
            if (behaviorService != null && baseParent != null)
            { 
                Rectangle baseParentBounds = behaviorService.ControlRectInAdornerWindow(baseParent);
                if (relatedPanel.Visible && image != null && glyphBounds != Rectangle.Empty && baseParentBounds.Contains(glyphBounds)) { 
                    pe.Graphics.DrawImage(image, glyphBounds.Left, glyphBounds.Top); 
                }
            } 
        }

    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Drawing.Drawing2D;
    using System.Windows.Forms.Design; 
    using System.Windows.Forms;

    /// <include file='doc\ToolStripPanelSelectionGlyph.uex' path='docs/doc[@for="ToolStripPanelSelectionGlyph"]/*' />
    /// <devdoc> 
    /// </devdoc>
    internal sealed class ToolStripPanelSelectionGlyph : ControlBodyGlyph { 
 
        private ToolStripPanel relatedPanel;
        private Rectangle glyphBounds; 
        private IServiceProvider provider;
        private ToolStripPanelSelectionBehavior relatedBehavior;
        private Image image = null;
 
        private Control baseParent = null;
        private BehaviorService         behaviorService; 
 

        private bool isExpanded = false; 

        private const int imageWidth = 50;
        private const int imageHeight = 6;
 
        /// <include file='doc\ToolStripPanelSelectionGlyph.uex' path='docs/doc[@for="ToolStripPanelSelectionGlyph.ToolStripPanelSelectionGlyph"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        internal ToolStripPanelSelectionGlyph(Rectangle bounds, Cursor cursor, IComponent relatedComponent, IServiceProvider provider, ToolStripPanelSelectionBehavior behavior) : base(bounds, cursor, relatedComponent, behavior) {
 
            relatedBehavior = behavior;
            this.provider = provider;
            this.relatedPanel = relatedComponent as ToolStripPanel;
 

            this.behaviorService = (BehaviorService)provider.GetService(typeof(BehaviorService)); 
            if (behaviorService == null) { 
                Debug.Fail("Could not get the BehaviorService");
                return; 
            }

            IDesignerHost host = (IDesignerHost)provider.GetService(typeof(IDesignerHost));
            if (host == null) { 
                Debug.Fail("Could not get the DesignerHost");
                return; 
            } 

 
            UpdateGlyph();
        }

        public bool IsExpanded 
        {
            get 
            { 
                return isExpanded;
            } 
            set
            {
                if (value != isExpanded)
                { 
                    isExpanded = value;
                    UpdateGlyph(); 
                } 
            }
        } 


        public void UpdateGlyph()
        { 
            if (behaviorService != null)
            { 
                Rectangle translatedBounds = behaviorService.ControlRectInAdornerWindow(relatedPanel); 
                //Reset the glyph.
                this.glyphBounds = Rectangle.Empty; 

                // Refresh the parent
                ToolStripContainer parent = relatedPanel.Parent as ToolStripContainer;
                if (parent != null) 
                {
                    this.baseParent = parent.Parent; // get the control to which ToolStripContainer is added... 
                } 

                if (!isExpanded) 
                {
                    CollapseGlyph(translatedBounds);
                }
                else 
                {
                    ExpandGlyph(translatedBounds); 
                } 
            }
        } 


        private void CollapseGlyph(Rectangle bounds)
        { 
            DockStyle dock = relatedPanel.Dock;
            int x = 0; 
            int y = 0; 

            switch(dock) 
            {
                case DockStyle.Top :
                    this.image = new Bitmap(typeof(ToolStripPanelSelectionGlyph), "topopen.bmp");
                    x = (bounds.Width - imageWidth) /2; 
                    if (x > 0)
                    { 
                        this.glyphBounds = new Rectangle(bounds.X + x, bounds.Y + bounds.Height, imageWidth, imageHeight); 
                    }
                    break; 
                case DockStyle.Bottom :
                    this.image = new Bitmap(typeof(ToolStripPanelSelectionGlyph), "bottomopen.bmp");
                    x = (bounds.Width - imageWidth) /2;
                    if (x > 0) 
                    {
                        this.glyphBounds = new Rectangle(bounds.X + x, bounds.Y - imageHeight, imageWidth, imageHeight); 
                    } 
 					break;
                case DockStyle.Left : 
                    this.image = new Bitmap(typeof(ToolStripPanelSelectionGlyph), "leftopen.bmp");
                    y = (bounds.Height - imageWidth) /2;
                    if (y > 0)
                    { 
                        this.glyphBounds = new Rectangle(bounds.X + bounds.Width, bounds.Y + y, imageHeight, imageWidth);
                    } 
                    break; 
                case DockStyle.Right :
                    this.image = new Bitmap(typeof(ToolStripPanelSelectionGlyph), "rightopen.bmp"); 
                    y = (bounds.Height - imageWidth) /2;
                    if (y > 0)
                    {
                        this.glyphBounds = new Rectangle(bounds.X - imageHeight , bounds.Y + y, imageHeight, imageWidth); 
                    }
                    break; 
                default: 
                    throw new Exception(SR.GetString(SR.ToolStripPanelGlyphUnsupportedDock));
            } 
        }


 
        private void ExpandGlyph(Rectangle bounds)
        { 
            DockStyle dock = relatedPanel.Dock; 
            int x = 0;
            int y = 0; 

            switch(dock)
            {
                case DockStyle.Top : 
                    this.image = new Bitmap(typeof(ToolStripPanelSelectionGlyph), "topclose.bmp");
                    x = (bounds.Width - imageWidth) /2; 
                    if (x > 0) 
                    {
                        this.glyphBounds = new Rectangle(bounds.X + x, bounds.Y + bounds.Height, imageWidth, imageHeight); 
                    }
                    break;
                case DockStyle.Bottom :
                    this.image = new Bitmap(typeof(ToolStripPanelSelectionGlyph), "bottomclose.bmp"); 
                    x = (bounds.Width - imageWidth) /2;
                    if (x > 0) 
                    { 
                        this.glyphBounds = new Rectangle(bounds.X + x, bounds.Y -imageHeight, imageWidth, imageHeight);
                    } 
					break;
                case DockStyle.Left :
                    this.image = new Bitmap(typeof(ToolStripPanelSelectionGlyph), "leftclose.bmp");
                    y = (bounds.Height - imageWidth) /2; 
                    if (y > 0)
                    { 
                        this.glyphBounds = new Rectangle(bounds.X + bounds.Width, bounds.Y + y, imageHeight, imageWidth); 
                    }
                    break; 
                case DockStyle.Right :
                    this.image = new Bitmap(typeof(ToolStripPanelSelectionGlyph), "rightclose.bmp");
                    y = (bounds.Height - imageWidth) /2;
                    if (y > 0) 
                    {
                        this.glyphBounds = new Rectangle(bounds.X - imageHeight, bounds.Y + y, imageHeight, imageWidth); 
                    } 
					break;
                default: 
                    throw new Exception(SR.GetString(SR.ToolStripPanelGlyphUnsupportedDock));
            }
        }
 

        /// <include file='doc\ToolStripPanelSelectionGlyph.uex' path='docs/doc[@for="ToolStripPanelSelectionGlyph.Bounds"]/*' /> 
        /// <devdoc> 
        ///     The bounds of this Glyph.
        /// </devdoc> 
        public override Rectangle Bounds {
            get  {
                return glyphBounds;
            } 
        }
 
 
        /// <include file='doc\ToolStripPanelSelectionGlyph.uex' path='docs/doc[@for="ToolStripPanelSelectionGlyph.GetHitTest"]/*' />
        /// <devdoc> 
        ///     Simple hit test rule: if the point is contained within the bounds
        ///     - then it is a positive hit test.
        /// </devdoc>
        public override Cursor GetHitTest(Point p) { 
            if (behaviorService != null && baseParent != null)
            { 
                Rectangle baseParentBounds = behaviorService.ControlRectInAdornerWindow(baseParent); 
                if (glyphBounds != Rectangle.Empty && baseParentBounds.Contains(glyphBounds) && glyphBounds.Contains(p)) {
                    return Cursors.Hand; 
                }
            }
            return null;
        } 

 
        /// <include file='doc\ToolStripPanelSelectionGlyph.uex' path='docs/doc[@for="ToolStripPanelSelectionGlyph.Paint"]/*' /> 
        /// <devdoc>
        ///     Very simple paint logic. 
        /// </devdoc>
        public override void Paint(PaintEventArgs pe) {
            if (behaviorService != null && baseParent != null)
            { 
                Rectangle baseParentBounds = behaviorService.ControlRectInAdornerWindow(baseParent);
                if (relatedPanel.Visible && image != null && glyphBounds != Rectangle.Empty && baseParentBounds.Contains(glyphBounds)) { 
                    pe.Graphics.DrawImage(image, glyphBounds.Left, glyphBounds.Top); 
                }
            } 
        }

    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
