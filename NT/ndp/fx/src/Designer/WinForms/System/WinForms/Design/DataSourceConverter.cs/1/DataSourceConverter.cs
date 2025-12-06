//------------------------------------------------------------------------------ 
// <copyright file="DataSourceConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Data;
    using System.Globalization;
    using System.Diagnostics.CodeAnalysis; 

    internal class DataSourceConverter : ReferenceConverter { 
 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public DataSourceConverter() : base(typeof(IListSource)) { 
        }

        ReferenceConverter listConverter = new ReferenceConverter(typeof(IList));
 
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
            ArrayList listSources = new ArrayList(base.GetStandardValues(context)); 
            StandardValuesCollection lists = listConverter.GetStandardValues(context); 

            ArrayList listsList = new ArrayList(); 

            BindingSource bs = context.Instance as BindingSource;

            foreach (object listSource in listSources) { 
                if (listSource != null) {
 
                    // bug 46563: work around the TableMappings property on the OleDbDataAdapter 
                    ListBindableAttribute listBindable = (ListBindableAttribute) TypeDescriptor.GetAttributes(listSource)[typeof(ListBindableAttribute)];
                    if (listBindable != null && !listBindable.ListBindable) { 
                            continue;
                    }

                    // Prevent user from being able to connect a BindingSource to itself 
                    if (bs != null && bs == listSource) {
                        continue; 
                    } 

                    // Per Whidbey spec : DataSourcePicker.doc, 3.4.1 
                    //
                    // if this is a DataTable and the DataSet that owns the table is in the list,
                    // don't add it.  this way we only show the top-level data sources and don't clutter the
                    // list with duplicates like: 
                    //
                    //   NorthWind1.Customers 
                    //   NorthWind1.Employees 
                    //   NorthWind1
                    // 
                    // but instead just show "NorthWind1".  This does force the user to pick a data member but helps
                    // with simplicity.
                    //
                    // we are doing an n^2 lookup here but this list will never be more than 10 or 15 entries long so it should 
                    // not be a problem.
                    // 
                    DataTable listSourceDataTable = listSource as DataTable; 
                    if (listSourceDataTable == null || !listSources.Contains(listSourceDataTable.DataSet)) {
                        listsList.Add(listSource); 
                    }
                }
            }
 
            foreach (object list in lists) {
                if (list!= null) { 
                    // bug 46563: work around the TableMappings property on the OleDbDataAdapter 
                    ListBindableAttribute listBindable = (ListBindableAttribute) TypeDescriptor.GetAttributes(list)[typeof(ListBindableAttribute)];
                    if (listBindable != null && !listBindable.ListBindable) 
                        continue;

                    // Prevent user from being able to connect a BindingSource to itself
                    if (bs != null && bs == list) { 
                        continue;
                    } 
 
                    listsList.Add(list);
                } 
            }
            // bug 71417: add a null list to reset the dataSource
            listsList.Add(null);
 
            return new StandardValuesCollection(listsList);
        } 
 
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
            return true; 
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
            return true; 
        }
 
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) { 
            // Types are now valid data sources, so we need to be able to
            // represent them as strings (since ReferenceConverter can't) 
            if (destinationType == typeof(string) && value is Type) {
                return value.ToString();
            }
 
            return base.ConvertTo(context, culture, value, destinationType);
        } 
 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataSourceConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
 
    using System;
    using System.Collections; 
    using System.ComponentModel;
    using System.Data;
    using System.Globalization;
    using System.Diagnostics.CodeAnalysis; 

    internal class DataSourceConverter : ReferenceConverter { 
 
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public DataSourceConverter() : base(typeof(IListSource)) { 
        }

        ReferenceConverter listConverter = new ReferenceConverter(typeof(IList));
 
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
            ArrayList listSources = new ArrayList(base.GetStandardValues(context)); 
            StandardValuesCollection lists = listConverter.GetStandardValues(context); 

            ArrayList listsList = new ArrayList(); 

            BindingSource bs = context.Instance as BindingSource;

            foreach (object listSource in listSources) { 
                if (listSource != null) {
 
                    // bug 46563: work around the TableMappings property on the OleDbDataAdapter 
                    ListBindableAttribute listBindable = (ListBindableAttribute) TypeDescriptor.GetAttributes(listSource)[typeof(ListBindableAttribute)];
                    if (listBindable != null && !listBindable.ListBindable) { 
                            continue;
                    }

                    // Prevent user from being able to connect a BindingSource to itself 
                    if (bs != null && bs == listSource) {
                        continue; 
                    } 

                    // Per Whidbey spec : DataSourcePicker.doc, 3.4.1 
                    //
                    // if this is a DataTable and the DataSet that owns the table is in the list,
                    // don't add it.  this way we only show the top-level data sources and don't clutter the
                    // list with duplicates like: 
                    //
                    //   NorthWind1.Customers 
                    //   NorthWind1.Employees 
                    //   NorthWind1
                    // 
                    // but instead just show "NorthWind1".  This does force the user to pick a data member but helps
                    // with simplicity.
                    //
                    // we are doing an n^2 lookup here but this list will never be more than 10 or 15 entries long so it should 
                    // not be a problem.
                    // 
                    DataTable listSourceDataTable = listSource as DataTable; 
                    if (listSourceDataTable == null || !listSources.Contains(listSourceDataTable.DataSet)) {
                        listsList.Add(listSource); 
                    }
                }
            }
 
            foreach (object list in lists) {
                if (list!= null) { 
                    // bug 46563: work around the TableMappings property on the OleDbDataAdapter 
                    ListBindableAttribute listBindable = (ListBindableAttribute) TypeDescriptor.GetAttributes(list)[typeof(ListBindableAttribute)];
                    if (listBindable != null && !listBindable.ListBindable) 
                        continue;

                    // Prevent user from being able to connect a BindingSource to itself
                    if (bs != null && bs == list) { 
                        continue;
                    } 
 
                    listsList.Add(list);
                } 
            }
            // bug 71417: add a null list to reset the dataSource
            listsList.Add(null);
 
            return new StandardValuesCollection(listsList);
        } 
 
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
            return true; 
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
            return true; 
        }
 
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) { 
            // Types are now valid data sources, so we need to be able to
            // represent them as strings (since ReferenceConverter can't) 
            if (destinationType == typeof(string) && value is Type) {
                return value.ToString();
            }
 
            return base.ConvertTo(context, culture, value, destinationType);
        } 
 
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
