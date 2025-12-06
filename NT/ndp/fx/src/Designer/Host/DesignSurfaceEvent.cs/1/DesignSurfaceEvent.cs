//------------------------------------------------------------------------------ 
// <copyright file="DesignSurfaceEvent.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    /// <include file='doc\DesignSurfaceEventHandler.uex' path='docs/doc[@for="DesignSurfaceEventHandler"]/*' />
    /// <devdoc> 
    ///     Event handler for the DesignSurface event.
    /// </devdoc>
    public delegate void DesignSurfaceEventHandler(object sender, DesignSurfaceEventArgs e);
 
    /// <include file='doc\DesignSurfaceEventArgs.uex' path='docs/doc[@for="DesignSurfaceEventArgs"]/*' />
    /// <devdoc> 
    ///     Event args for the DesignSurface event. 
    /// </devdoc>
    public class DesignSurfaceEventArgs : EventArgs { 

        private DesignSurface _surface;

        /// <include file='doc\DesignSurfaceEventArgs.uex' path='docs/doc[@for="DesignSurfaceEventArgs.DesignSurfaceEventArgs"]/*' /> 
        /// <devdoc>
        ///     Creates a new DesignSurfaceEventArgs for the given design surface. 
        /// </devdoc> 
        public DesignSurfaceEventArgs(DesignSurface surface) {
            if (surface == null) { 
                throw new ArgumentNullException("surface");
            }
            _surface = surface;
        } 

        /// <include file='doc\DesignSurfaceEventArgs.uex' path='docs/doc[@for="DesignSurfaceEventArgs.Surface"]/*' /> 
        /// <devdoc> 
        ///     The design surface passed into the constructor.
        /// </devdoc> 
        public DesignSurface Surface {
            get {
                return _surface;
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignSurfaceEvent.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    /// <include file='doc\DesignSurfaceEventHandler.uex' path='docs/doc[@for="DesignSurfaceEventHandler"]/*' />
    /// <devdoc> 
    ///     Event handler for the DesignSurface event.
    /// </devdoc>
    public delegate void DesignSurfaceEventHandler(object sender, DesignSurfaceEventArgs e);
 
    /// <include file='doc\DesignSurfaceEventArgs.uex' path='docs/doc[@for="DesignSurfaceEventArgs"]/*' />
    /// <devdoc> 
    ///     Event args for the DesignSurface event. 
    /// </devdoc>
    public class DesignSurfaceEventArgs : EventArgs { 

        private DesignSurface _surface;

        /// <include file='doc\DesignSurfaceEventArgs.uex' path='docs/doc[@for="DesignSurfaceEventArgs.DesignSurfaceEventArgs"]/*' /> 
        /// <devdoc>
        ///     Creates a new DesignSurfaceEventArgs for the given design surface. 
        /// </devdoc> 
        public DesignSurfaceEventArgs(DesignSurface surface) {
            if (surface == null) { 
                throw new ArgumentNullException("surface");
            }
            _surface = surface;
        } 

        /// <include file='doc\DesignSurfaceEventArgs.uex' path='docs/doc[@for="DesignSurfaceEventArgs.Surface"]/*' /> 
        /// <devdoc> 
        ///     The design surface passed into the constructor.
        /// </devdoc> 
        public DesignSurface Surface {
            get {
                return _surface;
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
