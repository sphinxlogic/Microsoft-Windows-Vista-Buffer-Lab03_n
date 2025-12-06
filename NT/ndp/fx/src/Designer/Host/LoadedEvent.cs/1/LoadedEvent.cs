//------------------------------------------------------------------------------ 
// <copyright file="LoadedEvent.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.ComponentModel.Design {
 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Design; 

    /// <include file='doc\LoadedEvent.uex' path='docs/doc[@for="LoadedEventHandler"]/*' /> 
    /// <devdoc> 
    ///     Represents the method that will handle a Loaded event.
    /// </devdoc> 
    public delegate void LoadedEventHandler(object sender, LoadedEventArgs e);

    /// <include file='doc\LoadedEvent.uex' path='docs/doc[@for="LoadedEventArgs"]/*' />
    /// <devdoc> 
    ///     Provides additional information for the Loaded event.
    /// </devdoc> 
    public sealed class LoadedEventArgs : EventArgs { 

        private bool          _succeeded; 
        private ICollection   _errors;

        /// <include file='doc\LoadedEvent.uex' path='docs/doc[@for="LoadedEventArgs.LoadedEventArgs"]/*' />
        /// <devdoc> 
        ///     Creates a new LoadedEventArgs object.
        /// </devdoc> 
        public LoadedEventArgs(bool succeeded, ICollection errors) { 

            _succeeded = succeeded; 
            _errors = errors;

            if (_errors == null) {
                _errors = new object[0]; 
            }
        } 
 
        /// <include file='doc\LoadedEvent.uex' path='docs/doc[@for="LoadedEventArgs.Errors"]/*' />
        /// <devdoc> 
        ///     A collection of errors that occurred while
        ///     the designer was loading.
        /// </devdoc>
        public ICollection Errors { 
            get {
                return _errors; 
            } 
        }
 
        /// <include file='doc\LoadedEvent.uex' path='docs/doc[@for="LoadedEventArgs.HasSucceeded"]/*' />
        /// <devdoc>
        ///     True to indicate the designer load was successful.
        ///     Even successful loads can have errors, if the errors 
        ///     were not too servere to prevent the designer from
        ///     loading. 
        /// </devdoc> 
        public bool HasSucceeded {
            get { 
                return _succeeded;
            }
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="LoadedEvent.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.ComponentModel.Design {
 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Design; 

    /// <include file='doc\LoadedEvent.uex' path='docs/doc[@for="LoadedEventHandler"]/*' /> 
    /// <devdoc> 
    ///     Represents the method that will handle a Loaded event.
    /// </devdoc> 
    public delegate void LoadedEventHandler(object sender, LoadedEventArgs e);

    /// <include file='doc\LoadedEvent.uex' path='docs/doc[@for="LoadedEventArgs"]/*' />
    /// <devdoc> 
    ///     Provides additional information for the Loaded event.
    /// </devdoc> 
    public sealed class LoadedEventArgs : EventArgs { 

        private bool          _succeeded; 
        private ICollection   _errors;

        /// <include file='doc\LoadedEvent.uex' path='docs/doc[@for="LoadedEventArgs.LoadedEventArgs"]/*' />
        /// <devdoc> 
        ///     Creates a new LoadedEventArgs object.
        /// </devdoc> 
        public LoadedEventArgs(bool succeeded, ICollection errors) { 

            _succeeded = succeeded; 
            _errors = errors;

            if (_errors == null) {
                _errors = new object[0]; 
            }
        } 
 
        /// <include file='doc\LoadedEvent.uex' path='docs/doc[@for="LoadedEventArgs.Errors"]/*' />
        /// <devdoc> 
        ///     A collection of errors that occurred while
        ///     the designer was loading.
        /// </devdoc>
        public ICollection Errors { 
            get {
                return _errors; 
            } 
        }
 
        /// <include file='doc\LoadedEvent.uex' path='docs/doc[@for="LoadedEventArgs.HasSucceeded"]/*' />
        /// <devdoc>
        ///     True to indicate the designer load was successful.
        ///     Even successful loads can have errors, if the errors 
        ///     were not too servere to prevent the designer from
        ///     loading. 
        /// </devdoc> 
        public bool HasSucceeded {
            get { 
                return _succeeded;
            }
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
