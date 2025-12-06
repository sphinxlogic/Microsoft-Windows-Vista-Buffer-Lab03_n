  //------------------------------------------------------------------------------ 
  // <copyright file="ToolStripItemGlyph.cs" company="Microsoft">
  //     Copyright (c) Microsoft Corporation.  All rights reserved.
  // </copyright>
  //----------------------------------------------------------------------------- 

  /* 
   */ 
  namespace System.Windows.Forms.Design {
      using System.Design; 
      using Accessibility;
      using System.Runtime.Serialization.Formatters;
      using System.Threading;
      using System.Runtime.InteropServices; 
      using System.ComponentModel;
      using System.Diagnostics; 
      using System; 
      using System.Security;
      using System.Security.Permissions; 
      using System.Collections;
      using System.ComponentModel.Design;
      using System.ComponentModel.Design.Serialization;
      using System.Windows.Forms; 
      using System.Drawing;
      using System.Drawing.Design; 
      using Microsoft.Win32; 
      using System.Windows.Forms.Design.Behavior;
      using System.Reflection; 


      /// <summary>
      ///  The glyph we put over the items.  Basically this sets the hit-testable area of the item itself. 
      /// </summary>
      internal class ToolStripItemGlyph : ControlBodyGlyph{ 
 
            private ToolStripItem _item;
            private Rectangle  _bounds; 
            private ToolStripItemDesigner _itemDesigner;

            public ToolStripItemGlyph(ToolStripItem item, ToolStripItemDesigner itemDesigner, Rectangle bounds, System.Windows.Forms.Design.Behavior.Behavior b) : base(bounds, Cursors.Default, item, b) {
                _item = item; 
                _bounds = bounds;
                _itemDesigner = itemDesigner; 
            } 

            public ToolStripItem Item { 
                get {
                    return _item;
                }
            } 

            public override Rectangle Bounds { 
                get { 
                    return _bounds;
                } 
            }

            public ToolStripItemDesigner ItemDesigner {
                get { 
                    return _itemDesigner;
                } 
            } 

            /// <include file='doc\Glyph.uex' path='docs/doc[@for="Glyph.GetHitTest"]/*' /> 
            /// <devdoc>
            ///     Abstract method that forces Glyph implementations to provide
            ///     hit test logic.  Given any point - if the Glyph has decided to
            ///     be involved with that location, the Glyph will need to return 
            ///     a valid Cursor.  Otherwise, returning null will cause the
            ///     the BehaviorService to simply ignore it. 
            /// </devdoc> 
            public override Cursor GetHitTest(Point p) {
                    if (_item.Visible && _bounds.Contains(p)) { 
                        return Cursors.Default;
                    }
                    return null;
            } 

            /// <include file='doc\ComponentGlyph.uex' path='docs/doc[@for="ComponentGlyph.Paint"]/*' /> 
            /// <devdoc> 
            ///     Control host dont draw on Invalidation...
            /// </devdoc> 
            public override void Paint(PaintEventArgs pe) {
                if (_item is ToolStripControlHost && _item.IsOnDropDown )
                {
                    if( _item is System.Windows.Forms.ToolStripComboBox && VisualStyles.VisualStyleRenderer.IsSupported) { 
                        // When processing WM_PAINT and the OS has a theme enabled, the native ComboBox sends a WM_PAINT
                        // message to its parent when a theme is enabled in the OS forcing a repaint in the AdornerWindow 
                        // generating an infinite WM_PAINT message processing loop. We guard against this here.  See DDB#99105. 
                        return;
                    } 

                    _item.Invalidate();
                }
            } 

      } 
 } 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
  //------------------------------------------------------------------------------ 
  // <copyright file="ToolStripItemGlyph.cs" company="Microsoft">
  //     Copyright (c) Microsoft Corporation.  All rights reserved.
  // </copyright>
  //----------------------------------------------------------------------------- 

  /* 
   */ 
  namespace System.Windows.Forms.Design {
      using System.Design; 
      using Accessibility;
      using System.Runtime.Serialization.Formatters;
      using System.Threading;
      using System.Runtime.InteropServices; 
      using System.ComponentModel;
      using System.Diagnostics; 
      using System; 
      using System.Security;
      using System.Security.Permissions; 
      using System.Collections;
      using System.ComponentModel.Design;
      using System.ComponentModel.Design.Serialization;
      using System.Windows.Forms; 
      using System.Drawing;
      using System.Drawing.Design; 
      using Microsoft.Win32; 
      using System.Windows.Forms.Design.Behavior;
      using System.Reflection; 


      /// <summary>
      ///  The glyph we put over the items.  Basically this sets the hit-testable area of the item itself. 
      /// </summary>
      internal class ToolStripItemGlyph : ControlBodyGlyph{ 
 
            private ToolStripItem _item;
            private Rectangle  _bounds; 
            private ToolStripItemDesigner _itemDesigner;

            public ToolStripItemGlyph(ToolStripItem item, ToolStripItemDesigner itemDesigner, Rectangle bounds, System.Windows.Forms.Design.Behavior.Behavior b) : base(bounds, Cursors.Default, item, b) {
                _item = item; 
                _bounds = bounds;
                _itemDesigner = itemDesigner; 
            } 

            public ToolStripItem Item { 
                get {
                    return _item;
                }
            } 

            public override Rectangle Bounds { 
                get { 
                    return _bounds;
                } 
            }

            public ToolStripItemDesigner ItemDesigner {
                get { 
                    return _itemDesigner;
                } 
            } 

            /// <include file='doc\Glyph.uex' path='docs/doc[@for="Glyph.GetHitTest"]/*' /> 
            /// <devdoc>
            ///     Abstract method that forces Glyph implementations to provide
            ///     hit test logic.  Given any point - if the Glyph has decided to
            ///     be involved with that location, the Glyph will need to return 
            ///     a valid Cursor.  Otherwise, returning null will cause the
            ///     the BehaviorService to simply ignore it. 
            /// </devdoc> 
            public override Cursor GetHitTest(Point p) {
                    if (_item.Visible && _bounds.Contains(p)) { 
                        return Cursors.Default;
                    }
                    return null;
            } 

            /// <include file='doc\ComponentGlyph.uex' path='docs/doc[@for="ComponentGlyph.Paint"]/*' /> 
            /// <devdoc> 
            ///     Control host dont draw on Invalidation...
            /// </devdoc> 
            public override void Paint(PaintEventArgs pe) {
                if (_item is ToolStripControlHost && _item.IsOnDropDown )
                {
                    if( _item is System.Windows.Forms.ToolStripComboBox && VisualStyles.VisualStyleRenderer.IsSupported) { 
                        // When processing WM_PAINT and the OS has a theme enabled, the native ComboBox sends a WM_PAINT
                        // message to its parent when a theme is enabled in the OS forcing a repaint in the AdornerWindow 
                        // generating an infinite WM_PAINT message processing loop. We guard against this here.  See DDB#99105. 
                        return;
                    } 

                    _item.Invalidate();
                }
            } 

      } 
 } 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
