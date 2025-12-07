 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Design {
 
    using System;
    using System.Diagnostics;

 
    internal enum ProblemSeverity {
        Unknown         = 0, 
        Warning         = 1, 
        NonFatalError   = 2,
        FatalError      = 3 
    }

    internal sealed class DSGeneratorProblem {
        private string message = null; 
        private ProblemSeverity severity = ProblemSeverity.Unknown;
        private DataSourceComponent problemSource; 
 
        internal string Message {
            get { 
                return message;
            }
        }
 
        internal ProblemSeverity Severity {
            get { 
                return severity; 
            }
        } 

        internal DataSourceComponent ProblemSource {
            get {
                return problemSource; 
            }
        } 
 
        internal DSGeneratorProblem(string message, ProblemSeverity severity, DataSourceComponent problemSource) {
            Debug.Assert(!StringUtil.Empty(message), "DSGeneratorProblem Constructor: message shouldn't be null or empty."); 
            Debug.Assert(severity != ProblemSeverity.Unknown, "DSGeneratorProblem Constructor: severity shouldn't be Unknown.");

            this.message        = message;
            this.severity       = severity; 
            this.problemSource  = problemSource;
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2002' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
 
namespace System.Data.Design {
 
    using System;
    using System.Diagnostics;

 
    internal enum ProblemSeverity {
        Unknown         = 0, 
        Warning         = 1, 
        NonFatalError   = 2,
        FatalError      = 3 
    }

    internal sealed class DSGeneratorProblem {
        private string message = null; 
        private ProblemSeverity severity = ProblemSeverity.Unknown;
        private DataSourceComponent problemSource; 
 
        internal string Message {
            get { 
                return message;
            }
        }
 
        internal ProblemSeverity Severity {
            get { 
                return severity; 
            }
        } 

        internal DataSourceComponent ProblemSource {
            get {
                return problemSource; 
            }
        } 
 
        internal DSGeneratorProblem(string message, ProblemSeverity severity, DataSourceComponent problemSource) {
            Debug.Assert(!StringUtil.Empty(message), "DSGeneratorProblem Constructor: message shouldn't be null or empty."); 
            Debug.Assert(severity != ProblemSeverity.Unknown, "DSGeneratorProblem Constructor: severity shouldn't be Unknown.");

            this.message        = message;
            this.severity       = severity; 
            this.problemSource  = problemSource;
        } 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
