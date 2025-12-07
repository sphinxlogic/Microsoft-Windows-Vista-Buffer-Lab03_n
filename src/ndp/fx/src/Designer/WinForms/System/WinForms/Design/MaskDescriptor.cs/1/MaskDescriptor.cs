//------------------------------------------------------------------------------ 
// <copyright file="TextBoxBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design 
{ 
    using System;
    using System.ComponentModel; 
    using System.Design;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization; 
    using System.Threading;
 
    /// <devdoc> 
    ///     MaskDescriptor abstract class defines the set of methods mask descriptors need to implement for the
    ///     MaskedTextBox.Mask UITypeEditor to include as options in the property editor.  MaskDescriptor 
    ///     types are discovered at designed time by querying the ITypeDiscoveryService service provider from
    ///     the UITypeEditor object.
    /// </devdoc>
    public abstract class MaskDescriptor 
    {
        /// <devdoc> 
        ///     The mask being described. 
        /// </devdoc>
        public abstract string Mask   { get; } 

        /// <devdoc>
        ///     The friendly name of the mask descriptor.
        ///     Used also as the description for the mask. 
        /// </devdoc>
        public abstract string Name   { get; } 
 
        /// <devdoc>
        ///     A sample text following the mask specification. 
        /// </devdoc>
        public abstract string Sample { get; }

        /// <devdoc> 
        ///     A Type representing the type providing validation for this mask.
        /// </devdoc> 
        public abstract Type ValidatingType { get; } 

        /// <devdoc> 
        ///     The CultureInfo representing the locale the mask is designed for.
        /// </devdoc>
        public virtual CultureInfo Culture
        { 
            get{ return Thread.CurrentThread.CurrentCulture; }
        } 
 
        /// <devdoc>
        ///     Determines whether the specified mask descriptor is valid and hence can be added to the canned masks list. 
        ///     A valid MaskDescriptor must meet the following conditions:
        ///     1. Not null.
        ///     2. Not null or empty mask.
        ///     3. Not null or empty name. 
        ///     4. Not null or empty sample.
        ///     5. The sample is correct based on the mask and all required edit characters have been provided (mask completed - not necessarily full). 
        ///     6. The sample is valid based on the ValidatingType object (if any). 
        /// </devdoc>
        public static bool IsValidMaskDescriptor( MaskDescriptor maskDescriptor ) 
        {
            string dummy;
            return IsValidMaskDescriptor(maskDescriptor, out dummy);
        } 
        public static bool IsValidMaskDescriptor( MaskDescriptor maskDescriptor, out string validationErrorDescription)
        { 
            validationErrorDescription = string.Empty; 

            if ( maskDescriptor == null ) 
            {
                validationErrorDescription = SR.GetString(SR.MaskDescriptorNull);
                return false;
            } 

            if ( string.IsNullOrEmpty(maskDescriptor.Mask) || string.IsNullOrEmpty(maskDescriptor.Name) || string.IsNullOrEmpty(maskDescriptor.Sample) ) 
            { 
                validationErrorDescription = SR.GetString(SR.MaskDescriptorNullOrEmptyRequiredProperty);
                return false; 
            }

            MaskedTextProvider mtp = new MaskedTextProvider( maskDescriptor.Mask, maskDescriptor.Culture );
            MaskedTextBox mtb = new MaskedTextBox(mtp); 

            mtb.SkipLiterals  = true; 
            mtb.ResetOnPrompt = true; 
            mtb.ResetOnSpace  = true;
            mtb.ValidatingType = maskDescriptor.ValidatingType; 
            mtb.FormatProvider = maskDescriptor.Culture;
            mtb.Culture = maskDescriptor.Culture;
            mtb.TypeValidationCompleted += new System.Windows.Forms.TypeValidationEventHandler(maskedTextBox1_TypeValidationCompleted);
            mtb.MaskInputRejected += new System.Windows.Forms.MaskInputRejectedEventHandler(maskedTextBox1_MaskInputRejected); 

            // Add sample. If it fails we are done. 
            mtb.Text = maskDescriptor.Sample; 

            if (mtb.Tag == null) // Sample was added successfully (MaskInputRejected event handler did not change the mtb tag). 
            {
                if( maskDescriptor.ValidatingType != null )
                {
                    mtb.ValidateText(); 
                }
// 
 

 



 
            }
 
            if (mtb.Tag != null) // Validation failed. 
            {
                validationErrorDescription = mtb.Tag.ToString(); 
            }

            return validationErrorDescription.Length == 0;
        } 

        private static void maskedTextBox1_MaskInputRejected(object sender, MaskInputRejectedEventArgs e) 
        { 
            MaskedTextBox mtb = sender as MaskedTextBox;
            mtb.Tag = MaskedTextBoxDesigner.GetMaskInputRejectedErrorMessage(e); 
        }

        private static void maskedTextBox1_TypeValidationCompleted(object sender, TypeValidationEventArgs e)
        { 
            if (!e.IsValidInput)
            { 
                MaskedTextBox mtb = sender as MaskedTextBox; 
                mtb.Tag = e.Message;
            } 
        }

        /// <devdoc>
        ///     Determines whether this mask descriptor and the passed object describe the same mask. 
        ///     True if the following conditions are met in both, this and the passed object:
        ///     1. Mask property is the same. 
        ///     2. Validating type is the same 
        ///     Observe that the Name property is not considered since MaskedTextProvider/Box are not
        ///     aware of it. 
        /// </devdoc>
        public override bool Equals( object maskDescriptor )
        {
            MaskDescriptor descriptor = maskDescriptor as MaskDescriptor; 

            if( !IsValidMaskDescriptor(descriptor) || !IsValidMaskDescriptor(this) ) 
            { 
                return this == maskDescriptor; // shallow comparison.
            } 

            return ((this.Mask == descriptor.Mask) && (this.ValidatingType == descriptor.ValidatingType));
        }
 
        /// <devdoc>
        ///     override. 
        /// </devdoc> 
        public override int GetHashCode()
        { 
            string hash = this.Mask;

            if (this.ValidatingType != null )
            { 
                hash += this.ValidatingType.ToString();
            } 
            return hash.GetHashCode(); 
        }
 
        /// <devdoc>
        ///     ToString override.
        /// </devdoc>
        public override string ToString() 
        {
            return string.Format(CultureInfo.CurrentCulture, "{0}<Name={1}, Mask={2}, ValidatingType={3}", 
                this.GetType(), 
                this.Name != null ? this.Name : "null",
                this.Mask != null ? this.Mask : "null", 
                this.ValidatingType != null ? this.ValidatingType.ToString() : "null"
                );
        }
    } 

    /// <devdoc> 
    ///     Implements the manual sorting of items by columns in the mask descriptor table. 
    ///     Used by the MaskDesignerDialog to sort the items in the mask descriptors list.
    /// </devdoc> 
    internal class MaskDescriptorComparer : System.Collections.Generic.IComparer<MaskDescriptor>
    {
        private SortOrder sortOrder;
        private SortType  sortType; 

        public enum SortType 
        { 
            ByName,
            BySample, 
            ByValidatingTypeName
        }

        public MaskDescriptorComparer(SortType sortType, SortOrder sortOrder) 
        {
            this.sortType  = sortType; 
            this.sortOrder = sortOrder; 
        }
 
        public int Compare(MaskDescriptor maskDescriptorA, MaskDescriptor maskDescriptorB)
        {
            if( maskDescriptorA == null || maskDescriptorB == null ) {
                // Since this is an internal class we cannot throw here, the user cannot do anything about this. 
                Debug.Fail( "One or more parameters invalid" );
                return 0; 
            } 

            string textA, textB; 

            switch( sortType )
            {
                default: 
                    Debug.Fail( "Invalid SortType, defaulting to SortType.ByName" );
                    goto case SortType.ByName; 
 
                case SortType.ByName:
                    textA = maskDescriptorA.Name; 
                    textB = maskDescriptorB.Name;
                    break;

                case SortType.BySample: 
                    textA = maskDescriptorA.Sample;
                    textB = maskDescriptorB.Sample; 
                    break; 

                case SortType.ByValidatingTypeName: 
                    textA = maskDescriptorA.ValidatingType == null ? SR.GetString( SR.MaskDescriptorValidatingTypeNone ) : maskDescriptorA.ValidatingType.Name;
                    textB = maskDescriptorB.ValidatingType == null ? SR.GetString( SR.MaskDescriptorValidatingTypeNone ) : maskDescriptorB.ValidatingType.Name;
                    break;
            } 

            int retVal = String.Compare(textA, textB); 
 
            return sortOrder == SortOrder.Descending ? -retVal : retVal;
        } 

        [SuppressMessage("Microsoft.Performance", "CA1801:AvoidUnusedParameters")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int GetHashCode(MaskDescriptor maskDescriptor) 
        {
            if( maskDescriptor != null ) 
            { 
                return maskDescriptor.GetHashCode();
            } 

            Debug.Fail("Null maskDescriptor passed.");
            return 0;
        } 

        [SuppressMessage("Microsoft.Performance", "CA1801:AvoidUnusedParameters")] 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        public bool Equals( MaskDescriptor maskDescriptorA, MaskDescriptor maskDescriptorB )
        { 
            if( !MaskDescriptor.IsValidMaskDescriptor(maskDescriptorA) || !MaskDescriptor.IsValidMaskDescriptor(maskDescriptorB) )
            {
                return maskDescriptorA == maskDescriptorB; // shallow comparison.
            } 

            return maskDescriptorA.Equals(maskDescriptorB); 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TextBoxBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design 
{ 
    using System;
    using System.ComponentModel; 
    using System.Design;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization; 
    using System.Threading;
 
    /// <devdoc> 
    ///     MaskDescriptor abstract class defines the set of methods mask descriptors need to implement for the
    ///     MaskedTextBox.Mask UITypeEditor to include as options in the property editor.  MaskDescriptor 
    ///     types are discovered at designed time by querying the ITypeDiscoveryService service provider from
    ///     the UITypeEditor object.
    /// </devdoc>
    public abstract class MaskDescriptor 
    {
        /// <devdoc> 
        ///     The mask being described. 
        /// </devdoc>
        public abstract string Mask   { get; } 

        /// <devdoc>
        ///     The friendly name of the mask descriptor.
        ///     Used also as the description for the mask. 
        /// </devdoc>
        public abstract string Name   { get; } 
 
        /// <devdoc>
        ///     A sample text following the mask specification. 
        /// </devdoc>
        public abstract string Sample { get; }

        /// <devdoc> 
        ///     A Type representing the type providing validation for this mask.
        /// </devdoc> 
        public abstract Type ValidatingType { get; } 

        /// <devdoc> 
        ///     The CultureInfo representing the locale the mask is designed for.
        /// </devdoc>
        public virtual CultureInfo Culture
        { 
            get{ return Thread.CurrentThread.CurrentCulture; }
        } 
 
        /// <devdoc>
        ///     Determines whether the specified mask descriptor is valid and hence can be added to the canned masks list. 
        ///     A valid MaskDescriptor must meet the following conditions:
        ///     1. Not null.
        ///     2. Not null or empty mask.
        ///     3. Not null or empty name. 
        ///     4. Not null or empty sample.
        ///     5. The sample is correct based on the mask and all required edit characters have been provided (mask completed - not necessarily full). 
        ///     6. The sample is valid based on the ValidatingType object (if any). 
        /// </devdoc>
        public static bool IsValidMaskDescriptor( MaskDescriptor maskDescriptor ) 
        {
            string dummy;
            return IsValidMaskDescriptor(maskDescriptor, out dummy);
        } 
        public static bool IsValidMaskDescriptor( MaskDescriptor maskDescriptor, out string validationErrorDescription)
        { 
            validationErrorDescription = string.Empty; 

            if ( maskDescriptor == null ) 
            {
                validationErrorDescription = SR.GetString(SR.MaskDescriptorNull);
                return false;
            } 

            if ( string.IsNullOrEmpty(maskDescriptor.Mask) || string.IsNullOrEmpty(maskDescriptor.Name) || string.IsNullOrEmpty(maskDescriptor.Sample) ) 
            { 
                validationErrorDescription = SR.GetString(SR.MaskDescriptorNullOrEmptyRequiredProperty);
                return false; 
            }

            MaskedTextProvider mtp = new MaskedTextProvider( maskDescriptor.Mask, maskDescriptor.Culture );
            MaskedTextBox mtb = new MaskedTextBox(mtp); 

            mtb.SkipLiterals  = true; 
            mtb.ResetOnPrompt = true; 
            mtb.ResetOnSpace  = true;
            mtb.ValidatingType = maskDescriptor.ValidatingType; 
            mtb.FormatProvider = maskDescriptor.Culture;
            mtb.Culture = maskDescriptor.Culture;
            mtb.TypeValidationCompleted += new System.Windows.Forms.TypeValidationEventHandler(maskedTextBox1_TypeValidationCompleted);
            mtb.MaskInputRejected += new System.Windows.Forms.MaskInputRejectedEventHandler(maskedTextBox1_MaskInputRejected); 

            // Add sample. If it fails we are done. 
            mtb.Text = maskDescriptor.Sample; 

            if (mtb.Tag == null) // Sample was added successfully (MaskInputRejected event handler did not change the mtb tag). 
            {
                if( maskDescriptor.ValidatingType != null )
                {
                    mtb.ValidateText(); 
                }
// 
 

 



 
            }
 
            if (mtb.Tag != null) // Validation failed. 
            {
                validationErrorDescription = mtb.Tag.ToString(); 
            }

            return validationErrorDescription.Length == 0;
        } 

        private static void maskedTextBox1_MaskInputRejected(object sender, MaskInputRejectedEventArgs e) 
        { 
            MaskedTextBox mtb = sender as MaskedTextBox;
            mtb.Tag = MaskedTextBoxDesigner.GetMaskInputRejectedErrorMessage(e); 
        }

        private static void maskedTextBox1_TypeValidationCompleted(object sender, TypeValidationEventArgs e)
        { 
            if (!e.IsValidInput)
            { 
                MaskedTextBox mtb = sender as MaskedTextBox; 
                mtb.Tag = e.Message;
            } 
        }

        /// <devdoc>
        ///     Determines whether this mask descriptor and the passed object describe the same mask. 
        ///     True if the following conditions are met in both, this and the passed object:
        ///     1. Mask property is the same. 
        ///     2. Validating type is the same 
        ///     Observe that the Name property is not considered since MaskedTextProvider/Box are not
        ///     aware of it. 
        /// </devdoc>
        public override bool Equals( object maskDescriptor )
        {
            MaskDescriptor descriptor = maskDescriptor as MaskDescriptor; 

            if( !IsValidMaskDescriptor(descriptor) || !IsValidMaskDescriptor(this) ) 
            { 
                return this == maskDescriptor; // shallow comparison.
            } 

            return ((this.Mask == descriptor.Mask) && (this.ValidatingType == descriptor.ValidatingType));
        }
 
        /// <devdoc>
        ///     override. 
        /// </devdoc> 
        public override int GetHashCode()
        { 
            string hash = this.Mask;

            if (this.ValidatingType != null )
            { 
                hash += this.ValidatingType.ToString();
            } 
            return hash.GetHashCode(); 
        }
 
        /// <devdoc>
        ///     ToString override.
        /// </devdoc>
        public override string ToString() 
        {
            return string.Format(CultureInfo.CurrentCulture, "{0}<Name={1}, Mask={2}, ValidatingType={3}", 
                this.GetType(), 
                this.Name != null ? this.Name : "null",
                this.Mask != null ? this.Mask : "null", 
                this.ValidatingType != null ? this.ValidatingType.ToString() : "null"
                );
        }
    } 

    /// <devdoc> 
    ///     Implements the manual sorting of items by columns in the mask descriptor table. 
    ///     Used by the MaskDesignerDialog to sort the items in the mask descriptors list.
    /// </devdoc> 
    internal class MaskDescriptorComparer : System.Collections.Generic.IComparer<MaskDescriptor>
    {
        private SortOrder sortOrder;
        private SortType  sortType; 

        public enum SortType 
        { 
            ByName,
            BySample, 
            ByValidatingTypeName
        }

        public MaskDescriptorComparer(SortType sortType, SortOrder sortOrder) 
        {
            this.sortType  = sortType; 
            this.sortOrder = sortOrder; 
        }
 
        public int Compare(MaskDescriptor maskDescriptorA, MaskDescriptor maskDescriptorB)
        {
            if( maskDescriptorA == null || maskDescriptorB == null ) {
                // Since this is an internal class we cannot throw here, the user cannot do anything about this. 
                Debug.Fail( "One or more parameters invalid" );
                return 0; 
            } 

            string textA, textB; 

            switch( sortType )
            {
                default: 
                    Debug.Fail( "Invalid SortType, defaulting to SortType.ByName" );
                    goto case SortType.ByName; 
 
                case SortType.ByName:
                    textA = maskDescriptorA.Name; 
                    textB = maskDescriptorB.Name;
                    break;

                case SortType.BySample: 
                    textA = maskDescriptorA.Sample;
                    textB = maskDescriptorB.Sample; 
                    break; 

                case SortType.ByValidatingTypeName: 
                    textA = maskDescriptorA.ValidatingType == null ? SR.GetString( SR.MaskDescriptorValidatingTypeNone ) : maskDescriptorA.ValidatingType.Name;
                    textB = maskDescriptorB.ValidatingType == null ? SR.GetString( SR.MaskDescriptorValidatingTypeNone ) : maskDescriptorB.ValidatingType.Name;
                    break;
            } 

            int retVal = String.Compare(textA, textB); 
 
            return sortOrder == SortOrder.Descending ? -retVal : retVal;
        } 

        [SuppressMessage("Microsoft.Performance", "CA1801:AvoidUnusedParameters")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int GetHashCode(MaskDescriptor maskDescriptor) 
        {
            if( maskDescriptor != null ) 
            { 
                return maskDescriptor.GetHashCode();
            } 

            Debug.Fail("Null maskDescriptor passed.");
            return 0;
        } 

        [SuppressMessage("Microsoft.Performance", "CA1801:AvoidUnusedParameters")] 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        public bool Equals( MaskDescriptor maskDescriptorA, MaskDescriptor maskDescriptorB )
        { 
            if( !MaskDescriptor.IsValidMaskDescriptor(maskDescriptorA) || !MaskDescriptor.IsValidMaskDescriptor(maskDescriptorB) )
            {
                return maskDescriptorA == maskDescriptorB; // shallow comparison.
            } 

            return maskDescriptorA.Equals(maskDescriptorB); 
        } 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
