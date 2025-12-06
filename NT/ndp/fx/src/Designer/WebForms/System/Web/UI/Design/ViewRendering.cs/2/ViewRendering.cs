//------------------------------------------------------------------------------ 
// <copyright file="ControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System.ComponentModel.Design; 

    /// <include file='doc\ViewRendering.uex' path='docs/doc[@for="ViewRendering"]/*' /> 
    public class ViewRendering {
        private string _content;
        private DesignerRegionCollection _regions;
        private bool _visible; 

        /// <include file='doc\ViewRendering.uex' path='docs/doc[@for="ViewRendering.ViewRendering"]/*' /> 
        public ViewRendering(string content, DesignerRegionCollection regions) : this(content, regions, true) { 
        }
 
        /// <include file='doc\ViewRendering.uex' path='docs/doc[@for="ViewRendering.ViewRendering"]/*' />
        public ViewRendering(string content, DesignerRegionCollection regions, bool visible) {
            _content = content;
            _regions = regions; 
            _visible = visible;
        } 
 
        /// <include file='doc\ViewRendering.uex' path='docs/doc[@for="ViewRendering.Content"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public string Content {
            get {
                if (_content == null) { 
                    return String.Empty;
                } 
                return _content; 
            }
        } 

        /// <include file='doc\ViewRendering.uex' path='docs/doc[@for="ViewRendering.Regions"]/*' />
        /// <devdoc>
        /// </devdoc> 
        public DesignerRegionCollection Regions {
            get { 
                if (_regions == null) { 
                    _regions = new DesignerRegionCollection();
                } 
                return _regions;
            }
        }
 
        /// <include file='doc\ViewRendering.uex' path='docs/doc[@for="ViewRendering.Visible"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        public bool Visible {
            get { 
                return _visible;
            }
        }
    } 
}
 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ControlDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System.ComponentModel.Design; 

    /// <include file='doc\ViewRendering.uex' path='docs/doc[@for="ViewRendering"]/*' /> 
    public class ViewRendering {
        private string _content;
        private DesignerRegionCollection _regions;
        private bool _visible; 

        /// <include file='doc\ViewRendering.uex' path='docs/doc[@for="ViewRendering.ViewRendering"]/*' /> 
        public ViewRendering(string content, DesignerRegionCollection regions) : this(content, regions, true) { 
        }
 
        /// <include file='doc\ViewRendering.uex' path='docs/doc[@for="ViewRendering.ViewRendering"]/*' />
        public ViewRendering(string content, DesignerRegionCollection regions, bool visible) {
            _content = content;
            _regions = regions; 
            _visible = visible;
        } 
 
        /// <include file='doc\ViewRendering.uex' path='docs/doc[@for="ViewRendering.Content"]/*' />
        /// <devdoc> 
        /// </devdoc>
        public string Content {
            get {
                if (_content == null) { 
                    return String.Empty;
                } 
                return _content; 
            }
        } 

        /// <include file='doc\ViewRendering.uex' path='docs/doc[@for="ViewRendering.Regions"]/*' />
        /// <devdoc>
        /// </devdoc> 
        public DesignerRegionCollection Regions {
            get { 
                if (_regions == null) { 
                    _regions = new DesignerRegionCollection();
                } 
                return _regions;
            }
        }
 
        /// <include file='doc\ViewRendering.uex' path='docs/doc[@for="ViewRendering.Visible"]/*' />
        /// <devdoc> 
        /// </devdoc> 
        public bool Visible {
            get { 
                return _visible;
            }
        }
    } 
}
 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
