//------------------------------------------------------------------------------ 
// <copyright file="DataBoundControlHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Data;
    using System.Security.Permissions;
    using System.Web.Util; 

    /// <devdoc> 
    /// Helper class for DataBoundControls and v1 data controls. 
    /// This is also used by ControlParameter to find its associated
    /// control. 
    /// </devdoc>
    internal static class DataBoundControlHelper {

        /// <devdoc> 
        /// Walks up the stack of NamingContainers starting at 'control' to find a control with the ID 'controlID'.
        /// </devdoc> 
        public static Control FindControl(Control control, string controlID) { 
            Debug.Assert(control != null, "control should not be null");
            Debug.Assert(!String.IsNullOrEmpty(controlID), "controlID should not be empty"); 
            Control currentContainer = control;
            Control foundControl = null;

            if (control == control.Page) { 
                // If we get to the Page itself while we're walking up the
                // hierarchy, just return whatever item we find (if anything) 
                // since we can't walk any higher. 
                return control.FindControl(controlID);
            } 

            while (foundControl == null && currentContainer != control.Page) {
                currentContainer = currentContainer.NamingContainer;
                if (currentContainer == null) { 
                    throw new HttpException(SR.GetString(SR.DataBoundControlHelper_NoNamingContainer, control.GetType().Name, control.ID));
                } 
                foundControl = currentContainer.FindControl(controlID); 
            }
 
            return foundControl;
        }

        /// <devdoc> 
        // return true if the two string arrays have the same members
        /// </devdoc> 
        public static bool CompareStringArrays(string[] stringA, string[] stringB) { 
            if (stringA == null && stringB == null) {
                return true; 
            }

            if (stringA == null || stringB == null) {
                return false; 
            }
 
            if (stringA.Length != stringB.Length) { 
                return false;
            } 

            for (int i = 0; i < stringA.Length; i++) {
                if (!String.Equals(stringA[i], stringB[i], StringComparison.Ordinal)) {
                    return false; 
                }
            } 
            return true; 
        }
 
        // Returns true for types that can be automatically databound in controls such as
        // GridView and DetailsView. Bindable types are simple types, such as primitives, strings,
        // and nullable primitives.
        public static bool IsBindableType(Type type) { 
            if (type == null) {
                return false; 
            } 
            Type underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null) { 
                // If the type is Nullable then it has an underlying type, in which case
                // we want to check the underlying type for bindability.
                type = underlyingType;
            } 
            return (type.IsPrimitive ||
                   (type == typeof(string)) || 
                   (type == typeof(DateTime)) || 
                   (type == typeof(Decimal)) ||
                   (type == typeof(Guid))); 
        }
    }
}
 
//------------------------------------------------------------------------------ 
// <copyright file="DataBoundControlHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Data;
    using System.Security.Permissions;
    using System.Web.Util; 

    /// <devdoc> 
    /// Helper class for DataBoundControls and v1 data controls. 
    /// This is also used by ControlParameter to find its associated
    /// control. 
    /// </devdoc>
    internal static class DataBoundControlHelper {

        /// <devdoc> 
        /// Walks up the stack of NamingContainers starting at 'control' to find a control with the ID 'controlID'.
        /// </devdoc> 
        public static Control FindControl(Control control, string controlID) { 
            Debug.Assert(control != null, "control should not be null");
            Debug.Assert(!String.IsNullOrEmpty(controlID), "controlID should not be empty"); 
            Control currentContainer = control;
            Control foundControl = null;

            if (control == control.Page) { 
                // If we get to the Page itself while we're walking up the
                // hierarchy, just return whatever item we find (if anything) 
                // since we can't walk any higher. 
                return control.FindControl(controlID);
            } 

            while (foundControl == null && currentContainer != control.Page) {
                currentContainer = currentContainer.NamingContainer;
                if (currentContainer == null) { 
                    throw new HttpException(SR.GetString(SR.DataBoundControlHelper_NoNamingContainer, control.GetType().Name, control.ID));
                } 
                foundControl = currentContainer.FindControl(controlID); 
            }
 
            return foundControl;
        }

        /// <devdoc> 
        // return true if the two string arrays have the same members
        /// </devdoc> 
        public static bool CompareStringArrays(string[] stringA, string[] stringB) { 
            if (stringA == null && stringB == null) {
                return true; 
            }

            if (stringA == null || stringB == null) {
                return false; 
            }
 
            if (stringA.Length != stringB.Length) { 
                return false;
            } 

            for (int i = 0; i < stringA.Length; i++) {
                if (!String.Equals(stringA[i], stringB[i], StringComparison.Ordinal)) {
                    return false; 
                }
            } 
            return true; 
        }
 
        // Returns true for types that can be automatically databound in controls such as
        // GridView and DetailsView. Bindable types are simple types, such as primitives, strings,
        // and nullable primitives.
        public static bool IsBindableType(Type type) { 
            if (type == null) {
                return false; 
            } 
            Type underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null) { 
                // If the type is Nullable then it has an underlying type, in which case
                // we want to check the underlying type for bindability.
                type = underlyingType;
            } 
            return (type.IsPrimitive ||
                   (type == typeof(string)) || 
                   (type == typeof(DateTime)) || 
                   (type == typeof(Decimal)) ||
                   (type == typeof(Guid))); 
        }
    }
}
 
