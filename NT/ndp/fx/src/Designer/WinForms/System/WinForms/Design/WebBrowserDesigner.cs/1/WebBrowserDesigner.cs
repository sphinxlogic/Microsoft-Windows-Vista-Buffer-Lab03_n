//------------------------------------------------------------------------------ 
// <copyright file="WebBrowserDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Windows.Forms; 
    using System.Collections;
    using System.ComponentModel;
    using System.Design;
 
    internal class WebBrowserDesigner : AxDesigner {
        public Uri Url { 
            get { 
                return (Uri)ShadowProperties["Url"];
            } 
            set {
                ShadowProperties["Url"] = value;
                //((WebBrowser)Component).Url = value;
            } 
        }
 
        public override void Initialize(IComponent c) { 
            // we have to do this before base.Init because we want to force create the whole
            // handle hierarchy in the AX Control 
            WebBrowser webBrowser = c as WebBrowser;
            this.Url = webBrowser.Url;
            webBrowser.Url = new Uri("about:blank"); // by navigating now to a URL we force the creation of all handles
 

            base.Initialize(c); 
 
             //HookChildHandles(Control.Handle);
            webBrowser.Url = null; // in the inherited case, where the designer is not on the control before its properties are set 
                                // by running InitializeComponent, we don't want to load/show the page either, so we're doing this
        }

        public override void InitializeNewComponent(IDictionary defaultValues) { 
            base.InitializeNewComponent(defaultValues);
 
 
            WebBrowser webBrowser = (WebBrowser)Component;
            if (webBrowser != null) { 
                //Set MinimumSize in the designer, so that the control doesn't go to 0-height
                //in FlowLayoutPanel (VSWhidbey 491172)
                webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            } 

        } 
 
        protected override InheritanceAttribute InheritanceAttribute
        { 
            get
            {
                if (base.InheritanceAttribute == InheritanceAttribute.Inherited)
                { 
                    return InheritanceAttribute.InheritedReadOnly;
                } 
                return base.InheritanceAttribute; 
            }
        } 

        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties);
 
            // Handle shadowed properties
            // 
            string[] shadowProps = new string[] { 
                "Url"
            }; 

            PropertyDescriptor prop;
            Attribute[] empty = new Attribute[0];
 
            for (int i = 0; i < shadowProps.Length; i++) {
                prop = (PropertyDescriptor)properties[shadowProps[i]]; 
                if (prop != null) { 
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(WebBrowserDesigner), prop, empty);
                } 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="WebBrowserDesigner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
    using System.Windows.Forms; 
    using System.Collections;
    using System.ComponentModel;
    using System.Design;
 
    internal class WebBrowserDesigner : AxDesigner {
        public Uri Url { 
            get { 
                return (Uri)ShadowProperties["Url"];
            } 
            set {
                ShadowProperties["Url"] = value;
                //((WebBrowser)Component).Url = value;
            } 
        }
 
        public override void Initialize(IComponent c) { 
            // we have to do this before base.Init because we want to force create the whole
            // handle hierarchy in the AX Control 
            WebBrowser webBrowser = c as WebBrowser;
            this.Url = webBrowser.Url;
            webBrowser.Url = new Uri("about:blank"); // by navigating now to a URL we force the creation of all handles
 

            base.Initialize(c); 
 
             //HookChildHandles(Control.Handle);
            webBrowser.Url = null; // in the inherited case, where the designer is not on the control before its properties are set 
                                // by running InitializeComponent, we don't want to load/show the page either, so we're doing this
        }

        public override void InitializeNewComponent(IDictionary defaultValues) { 
            base.InitializeNewComponent(defaultValues);
 
 
            WebBrowser webBrowser = (WebBrowser)Component;
            if (webBrowser != null) { 
                //Set MinimumSize in the designer, so that the control doesn't go to 0-height
                //in FlowLayoutPanel (VSWhidbey 491172)
                webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            } 

        } 
 
        protected override InheritanceAttribute InheritanceAttribute
        { 
            get
            {
                if (base.InheritanceAttribute == InheritanceAttribute.Inherited)
                { 
                    return InheritanceAttribute.InheritedReadOnly;
                } 
                return base.InheritanceAttribute; 
            }
        } 

        protected override void PreFilterProperties(IDictionary properties) {
            base.PreFilterProperties(properties);
 
            // Handle shadowed properties
            // 
            string[] shadowProps = new string[] { 
                "Url"
            }; 

            PropertyDescriptor prop;
            Attribute[] empty = new Attribute[0];
 
            for (int i = 0; i < shadowProps.Length; i++) {
                prop = (PropertyDescriptor)properties[shadowProps[i]]; 
                if (prop != null) { 
                    properties[shadowProps[i]] = TypeDescriptor.CreateProperty(typeof(WebBrowserDesigner), prop, empty);
                } 
            }
        }
    }
} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
