//------------------------------------------------------------------------------ 
// <copyright file="ConsumerConnectionPoint.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.WebParts { 
 
    using System;
    using System.Reflection; 
    using System.Security.Permissions;
    using System.Web;
    using System.Web.Util;
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class ConsumerConnectionPoint : ConnectionPoint { 
        // Used by WebPartManager to verify the custom ConnectionPoint type has
        // the correct constructor signature. 
        internal static readonly Type[] ConstructorTypes;

        static ConsumerConnectionPoint() {
            ConstructorInfo constructor = typeof(ConsumerConnectionPoint).GetConstructors()[0]; 
            ConstructorTypes = WebPartUtil.GetTypesForConstructor(constructor);
        } 
 
        public ConsumerConnectionPoint(MethodInfo callbackMethod, Type interfaceType, Type controlType,
                                       string displayName, string id, bool allowsMultipleConnections) : base( 
                                           callbackMethod, interfaceType, controlType, displayName, id, allowsMultipleConnections) {
        }

        public virtual void SetObject(Control control, object data) { 
            if (control == null) {
                throw new ArgumentNullException("control"); 
            } 

            CallbackMethod.Invoke(control, new object[] {data}); 
        }

        /// <devdoc>
        /// Base implementation returns true, can be overridden by subclasses to return 
        /// true or false conditionally based on the available secondary interfaces and the state
        /// of the consumer WebPart passed in. 
        /// </devdoc> 
        public virtual bool SupportsConnection(Control control, ConnectionInterfaceCollection secondaryInterfaces) {
            return true; 
        }
    }
}
 
//------------------------------------------------------------------------------ 
// <copyright file="ConsumerConnectionPoint.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.WebControls.WebParts { 
 
    using System;
    using System.Reflection; 
    using System.Security.Permissions;
    using System.Web;
    using System.Web.Util;
 
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)] 
    public class ConsumerConnectionPoint : ConnectionPoint { 
        // Used by WebPartManager to verify the custom ConnectionPoint type has
        // the correct constructor signature. 
        internal static readonly Type[] ConstructorTypes;

        static ConsumerConnectionPoint() {
            ConstructorInfo constructor = typeof(ConsumerConnectionPoint).GetConstructors()[0]; 
            ConstructorTypes = WebPartUtil.GetTypesForConstructor(constructor);
        } 
 
        public ConsumerConnectionPoint(MethodInfo callbackMethod, Type interfaceType, Type controlType,
                                       string displayName, string id, bool allowsMultipleConnections) : base( 
                                           callbackMethod, interfaceType, controlType, displayName, id, allowsMultipleConnections) {
        }

        public virtual void SetObject(Control control, object data) { 
            if (control == null) {
                throw new ArgumentNullException("control"); 
            } 

            CallbackMethod.Invoke(control, new object[] {data}); 
        }

        /// <devdoc>
        /// Base implementation returns true, can be overridden by subclasses to return 
        /// true or false conditionally based on the available secondary interfaces and the state
        /// of the consumer WebPart passed in. 
        /// </devdoc> 
        public virtual bool SupportsConnection(Control control, ConnectionInterfaceCollection secondaryInterfaces) {
            return true; 
        }
    }
}
 
