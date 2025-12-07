namespace System.Web.UI.Design { 

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SupportsPreviewControlAttribute : Attribute {
        private bool _supportsPreviewControl; 

        public static readonly SupportsPreviewControlAttribute Default = new SupportsPreviewControlAttribute(false); 
 
        public SupportsPreviewControlAttribute(bool supportsPreviewControl) {
            _supportsPreviewControl = supportsPreviewControl; 
        }

        public bool SupportsPreviewControl {
            get { 
                return _supportsPreviewControl;
            } 
        } 

        public override int GetHashCode() { 
            return _supportsPreviewControl.GetHashCode();
        }

        public override bool IsDefaultAttribute() { 
            return this.Equals(Default);
        } 
 
        public override bool Equals(object obj) {
            if (obj == this) { 
                return true;
            }

            SupportsPreviewControlAttribute other = obj as SupportsPreviewControlAttribute; 
            return (other != null) && (other.SupportsPreviewControl == _supportsPreviewControl);
        } 
    } 

} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.Web.UI.Design { 

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SupportsPreviewControlAttribute : Attribute {
        private bool _supportsPreviewControl; 

        public static readonly SupportsPreviewControlAttribute Default = new SupportsPreviewControlAttribute(false); 
 
        public SupportsPreviewControlAttribute(bool supportsPreviewControl) {
            _supportsPreviewControl = supportsPreviewControl; 
        }

        public bool SupportsPreviewControl {
            get { 
                return _supportsPreviewControl;
            } 
        } 

        public override int GetHashCode() { 
            return _supportsPreviewControl.GetHashCode();
        }

        public override bool IsDefaultAttribute() { 
            return this.Equals(Default);
        } 
 
        public override bool Equals(object obj) {
            if (obj == this) { 
                return true;
            }

            SupportsPreviewControlAttribute other = obj as SupportsPreviewControlAttribute; 
            return (other != null) && (other.SupportsPreviewControl == _supportsPreviewControl);
        } 
    } 

} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
