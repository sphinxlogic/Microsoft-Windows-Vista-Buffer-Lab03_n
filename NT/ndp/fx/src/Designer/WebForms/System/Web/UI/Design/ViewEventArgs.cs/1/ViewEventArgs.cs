//------------------------------------------------------------------------------ 
// <copyright file="ViewEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 

    /// <include file='doc\ViewEventArgs.uex' path='docs/doc[@for="ViewEventArgs"]/*' /> 
    public class ViewEventArgs : EventArgs {
        private DesignerRegion _region;
        private EventArgs _eventArgs;
        private ViewEvent _eventType; 

        /// <include file='doc\ViewEventArgs.uex' path='docs/doc[@for="ViewEventArgs.ViewEventArgs"]/*' /> 
        public ViewEventArgs(ViewEvent eventType, DesignerRegion region, EventArgs eventArgs) { 
            _eventType = eventType;
            _region = region; 
            _eventArgs = eventArgs;
        }

        /// <include file='doc\ViewEventArgs.uex' path='docs/doc[@for="ViewEventArgs.EventArgs"]/*' /> 
        public EventArgs EventArgs {
            get { 
                return _eventArgs; 
            }
        } 

        /// <include file='doc\ViewEventArgs.uex' path='docs/doc[@for="ViewEventArgs.EventType"]/*' />
        public ViewEvent EventType {
            get { 
                return _eventType;
            } 
        } 

        /// <include file='doc\ViewEventArgs.uex' path='docs/doc[@for="ViewEventArgs.Region"]/*' /> 
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
// <copyright file="ViewEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 

    /// <include file='doc\ViewEventArgs.uex' path='docs/doc[@for="ViewEventArgs"]/*' /> 
    public class ViewEventArgs : EventArgs {
        private DesignerRegion _region;
        private EventArgs _eventArgs;
        private ViewEvent _eventType; 

        /// <include file='doc\ViewEventArgs.uex' path='docs/doc[@for="ViewEventArgs.ViewEventArgs"]/*' /> 
        public ViewEventArgs(ViewEvent eventType, DesignerRegion region, EventArgs eventArgs) { 
            _eventType = eventType;
            _region = region; 
            _eventArgs = eventArgs;
        }

        /// <include file='doc\ViewEventArgs.uex' path='docs/doc[@for="ViewEventArgs.EventArgs"]/*' /> 
        public EventArgs EventArgs {
            get { 
                return _eventArgs; 
            }
        } 

        /// <include file='doc\ViewEventArgs.uex' path='docs/doc[@for="ViewEventArgs.EventType"]/*' />
        public ViewEvent EventType {
            get { 
                return _eventType;
            } 
        } 

        /// <include file='doc\ViewEventArgs.uex' path='docs/doc[@for="ViewEventArgs.Region"]/*' /> 
        public DesignerRegion Region {
            get {
                return _region;
            } 
        }
    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
