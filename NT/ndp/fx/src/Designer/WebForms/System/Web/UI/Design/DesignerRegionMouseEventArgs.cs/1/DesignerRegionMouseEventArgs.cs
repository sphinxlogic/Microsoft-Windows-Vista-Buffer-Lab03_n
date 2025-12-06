//------------------------------------------------------------------------------ 
// <copyright file="DesignerRegionMouseEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Web.UI.Design {
    using System.Drawing; 
 
    public sealed class DesignerRegionMouseEventArgs : EventArgs {
        private Point _location; 
        private DesignerRegion _region;

        /// <devdoc>
        /// </devdoc> 
        public DesignerRegionMouseEventArgs(DesignerRegion region, Point location) {
            _location = location; 
            _region = region; 
        }
 
        /// <devdoc>
        /// </devdoc>
        public Point Location {
            get { 
                return _location;
            } 
        } 

        /// <devdoc> 
        /// </devdoc>
        public DesignerRegion Region {
            get {
                return _region; 
            }
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerRegionMouseEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Web.UI.Design {
    using System.Drawing; 
 
    public sealed class DesignerRegionMouseEventArgs : EventArgs {
        private Point _location; 
        private DesignerRegion _region;

        /// <devdoc>
        /// </devdoc> 
        public DesignerRegionMouseEventArgs(DesignerRegion region, Point location) {
            _location = location; 
            _region = region; 
        }
 
        /// <devdoc>
        /// </devdoc>
        public Point Location {
            get { 
                return _location;
            } 
        } 

        /// <devdoc> 
        /// </devdoc>
        public DesignerRegion Region {
            get {
                return _region; 
            }
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
