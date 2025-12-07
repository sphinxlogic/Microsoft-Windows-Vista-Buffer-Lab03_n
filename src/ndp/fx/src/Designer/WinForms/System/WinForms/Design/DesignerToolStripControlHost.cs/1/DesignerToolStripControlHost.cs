//------------------------------------------------------------------------------ 
// <copyright file="DesignerToolStripControlHost.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Design; 
    using Accessibility;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using System.Security;
    using System.Security.Permissions; 
    using System.ComponentModel.Design; 
    using System.Windows.Forms;
    using System.Drawing; 
    using System.Drawing.Design;
    using System.Windows.Forms.Design.Behavior;

 
    /// <include file='doc\DesignerToolStripControlHost.uex' path='docs/doc[@for="DesignerToolStripControlHost"]/*' />
    /// <devdoc> 
    ///      This internal class is used by the new ToolStripDesigner to add a dummy 
    ///      node to the end. This class inherits from WinBarControlHost and overrides the
    ///      CanSelect property so that the dummy Node when shown in the designer doesnt show 
    ///      selection on Mouse movements.
    ///      The image is set to theDummyNodeImage embedded into the resources.
    /// </devdoc>
    /// <internalonly/> 

    internal class DesignerToolStripControlHost : ToolStripControlHost, IComponent 
    { 
        private BehaviorService b;
        internal ToolStrip parent=null; 

        //
        // Constructor
        // 
        /// <include file='doc\DesignerToolStripControlHost.uex' path='docs/doc[@for="DesignerToolStripControlHost.DesignerMenuItem"]/*' />
        public DesignerToolStripControlHost(Control c) : base(c) 
        { 
            // this ToolStripItem should not have defaultPadding.
            this.Margin = Padding.Empty; 
        }

        /// <include file='doc\DesignerToolStripControlHost.uex' path='docs/doc[@for="DesignerToolStripControlHost.DefaultSize"]/*' />
        /// <devdoc> 
        /// We need to return Default size for Editor ToolStrip (92, 22).
        /// </devdoc> 
        protected override Size DefaultSize { 
            get {
                return new Size(92, 22); 
            }
        }

        internal GlyphCollection GetGlyphs(ToolStrip parent, GlyphCollection glyphs, System.Windows.Forms.Design.Behavior.Behavior standardBehavior) { 

            if (b == null) 
            { 
                b = (BehaviorService)parent.Site.GetService(typeof(BehaviorService));
            } 

            Point loc = b.ControlToAdornerWindow(this.Parent);

            Rectangle r = this.Bounds; 
            r.Offset(loc);
            r.Inflate (-2 , -2); 
 
            glyphs.Add(new MiniLockedBorderGlyph(r, SelectionBorderGlyphType.Top, standardBehavior, true));
            glyphs.Add(new MiniLockedBorderGlyph(r, SelectionBorderGlyphType.Bottom, standardBehavior, true)); 
            glyphs.Add(new MiniLockedBorderGlyph(r, SelectionBorderGlyphType.Left, standardBehavior, true));
            glyphs.Add(new MiniLockedBorderGlyph(r, SelectionBorderGlyphType.Right, standardBehavior, true));

            return glyphs; 
        }
 
        internal void RefreshSelectionGlyph() 
        {
            ToolStrip miniToolStrip = this.Control as ToolStrip; 
            if (miniToolStrip != null)
            {
                ToolStripTemplateNode.MiniToolStripRenderer renderer = miniToolStrip.Renderer as ToolStripTemplateNode.MiniToolStripRenderer;
                if (renderer != null) 
                {
                    renderer.State = (int)TemplateNodeSelectionState.None; 
                    miniToolStrip.Invalidate(); 
                }
            } 

        }

        internal void SelectControl() 
        {
            ToolStrip miniToolStrip = this.Control as ToolStrip; 
            if (miniToolStrip != null) 
            {
                ToolStripTemplateNode.MiniToolStripRenderer renderer = miniToolStrip.Renderer as ToolStripTemplateNode.MiniToolStripRenderer; 
                if (renderer != null)
                {
                    renderer.State = (int)TemplateNodeSelectionState.TemplateNodeSelected;
                    miniToolStrip.Invalidate(); 
                }
            } 
        } 

 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerToolStripControlHost.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Design; 
    using Accessibility;
    using System.ComponentModel;
    using System.Diagnostics;
    using System; 
    using System.Security;
    using System.Security.Permissions; 
    using System.ComponentModel.Design; 
    using System.Windows.Forms;
    using System.Drawing; 
    using System.Drawing.Design;
    using System.Windows.Forms.Design.Behavior;

 
    /// <include file='doc\DesignerToolStripControlHost.uex' path='docs/doc[@for="DesignerToolStripControlHost"]/*' />
    /// <devdoc> 
    ///      This internal class is used by the new ToolStripDesigner to add a dummy 
    ///      node to the end. This class inherits from WinBarControlHost and overrides the
    ///      CanSelect property so that the dummy Node when shown in the designer doesnt show 
    ///      selection on Mouse movements.
    ///      The image is set to theDummyNodeImage embedded into the resources.
    /// </devdoc>
    /// <internalonly/> 

    internal class DesignerToolStripControlHost : ToolStripControlHost, IComponent 
    { 
        private BehaviorService b;
        internal ToolStrip parent=null; 

        //
        // Constructor
        // 
        /// <include file='doc\DesignerToolStripControlHost.uex' path='docs/doc[@for="DesignerToolStripControlHost.DesignerMenuItem"]/*' />
        public DesignerToolStripControlHost(Control c) : base(c) 
        { 
            // this ToolStripItem should not have defaultPadding.
            this.Margin = Padding.Empty; 
        }

        /// <include file='doc\DesignerToolStripControlHost.uex' path='docs/doc[@for="DesignerToolStripControlHost.DefaultSize"]/*' />
        /// <devdoc> 
        /// We need to return Default size for Editor ToolStrip (92, 22).
        /// </devdoc> 
        protected override Size DefaultSize { 
            get {
                return new Size(92, 22); 
            }
        }

        internal GlyphCollection GetGlyphs(ToolStrip parent, GlyphCollection glyphs, System.Windows.Forms.Design.Behavior.Behavior standardBehavior) { 

            if (b == null) 
            { 
                b = (BehaviorService)parent.Site.GetService(typeof(BehaviorService));
            } 

            Point loc = b.ControlToAdornerWindow(this.Parent);

            Rectangle r = this.Bounds; 
            r.Offset(loc);
            r.Inflate (-2 , -2); 
 
            glyphs.Add(new MiniLockedBorderGlyph(r, SelectionBorderGlyphType.Top, standardBehavior, true));
            glyphs.Add(new MiniLockedBorderGlyph(r, SelectionBorderGlyphType.Bottom, standardBehavior, true)); 
            glyphs.Add(new MiniLockedBorderGlyph(r, SelectionBorderGlyphType.Left, standardBehavior, true));
            glyphs.Add(new MiniLockedBorderGlyph(r, SelectionBorderGlyphType.Right, standardBehavior, true));

            return glyphs; 
        }
 
        internal void RefreshSelectionGlyph() 
        {
            ToolStrip miniToolStrip = this.Control as ToolStrip; 
            if (miniToolStrip != null)
            {
                ToolStripTemplateNode.MiniToolStripRenderer renderer = miniToolStrip.Renderer as ToolStripTemplateNode.MiniToolStripRenderer;
                if (renderer != null) 
                {
                    renderer.State = (int)TemplateNodeSelectionState.None; 
                    miniToolStrip.Invalidate(); 
                }
            } 

        }

        internal void SelectControl() 
        {
            ToolStrip miniToolStrip = this.Control as ToolStrip; 
            if (miniToolStrip != null) 
            {
                ToolStripTemplateNode.MiniToolStripRenderer renderer = miniToolStrip.Renderer as ToolStripTemplateNode.MiniToolStripRenderer; 
                if (renderer != null)
                {
                    renderer.State = (int)TemplateNodeSelectionState.TemplateNodeSelected;
                    miniToolStrip.Invalidate(); 
                }
            } 
        } 

 
    }
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
