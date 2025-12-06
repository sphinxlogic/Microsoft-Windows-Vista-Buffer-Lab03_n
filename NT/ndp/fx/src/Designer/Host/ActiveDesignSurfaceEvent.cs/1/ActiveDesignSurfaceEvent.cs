//------------------------------------------------------------------------------ 
// <copyright file="ActiveDesignSurfaceEvent.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    /// <devdoc>
    ///     An event handler for the ActiveDesignSurfaceChanged event. 
    /// </devdoc>
    public delegate void ActiveDesignSurfaceChangedEventHandler(object sender, ActiveDesignSurfaceChangedEventArgs e);

    /// <devdoc> 
    ///     The event args for the ActiveDesignSurface event.
    /// </devdoc> 
    public class ActiveDesignSurfaceChangedEventArgs : EventArgs { 

        private DesignSurface _oldSurface; 
        private DesignSurface _newSurface;

        /// <devdoc>
        ///     Creates a new ActiveDesignSurfaceChangedEventArgs instance. 
        /// </devdoc>
        public ActiveDesignSurfaceChangedEventArgs(DesignSurface oldSurface, DesignSurface newSurface) { 
            _oldSurface = oldSurface; 
            _newSurface = newSurface;
        } 

        /// <devdoc>
        ///     Gets the design surface that is losing activation.
        /// </devdoc> 
        public DesignSurface OldSurface {
            get { 
                return _oldSurface; 
            }
        } 

        /// <devdoc>
        ///     Gets the design surface that is gaining activation.
        /// </devdoc> 
        public DesignSurface NewSurface {
            get { 
                return _newSurface; 
            }
        } 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ActiveDesignSurfaceEvent.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
 
    /// <devdoc>
    ///     An event handler for the ActiveDesignSurfaceChanged event. 
    /// </devdoc>
    public delegate void ActiveDesignSurfaceChangedEventHandler(object sender, ActiveDesignSurfaceChangedEventArgs e);

    /// <devdoc> 
    ///     The event args for the ActiveDesignSurface event.
    /// </devdoc> 
    public class ActiveDesignSurfaceChangedEventArgs : EventArgs { 

        private DesignSurface _oldSurface; 
        private DesignSurface _newSurface;

        /// <devdoc>
        ///     Creates a new ActiveDesignSurfaceChangedEventArgs instance. 
        /// </devdoc>
        public ActiveDesignSurfaceChangedEventArgs(DesignSurface oldSurface, DesignSurface newSurface) { 
            _oldSurface = oldSurface; 
            _newSurface = newSurface;
        } 

        /// <devdoc>
        ///     Gets the design surface that is losing activation.
        /// </devdoc> 
        public DesignSurface OldSurface {
            get { 
                return _oldSurface; 
            }
        } 

        /// <devdoc>
        ///     Gets the design surface that is gaining activation.
        /// </devdoc> 
        public DesignSurface NewSurface {
            get { 
                return _newSurface; 
            }
        } 
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
