//------------------------------------------------------------------------------ 
// <copyright file="TimeSpanStorage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
// <owner current="false" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

namespace System.Data.Common { 
    using System;
    using System.Xml;
    using System.Data.SqlTypes;
    using System.Collections; 

    internal sealed class TimeSpanStorage : DataStorage { 
 
        private static readonly TimeSpan defaultValue = TimeSpan.Zero;
 
        private TimeSpan[] values;

        public TimeSpanStorage(DataColumn column)
        : base(column, typeof(TimeSpan), defaultValue) { 
        }
 
        override public Object Aggregate(int[] records, AggregateType kind) { 
            bool hasData = false;
            try { 
                switch (kind) {
                    case AggregateType.Min:
                        TimeSpan min = TimeSpan.MaxValue;
                        for (int i = 0; i < records.Length; i++) { 
                            int record = records[i];
                            if (IsNull(record)) 
                                continue; 
                            min=(TimeSpan.Compare(values[record],min) < 0) ? values[record] : min;
                            hasData = true; 
                        }
                        if (hasData) {
                            return min;
                        } 
                        return NullValue;
 
                    case AggregateType.Max: 
                        TimeSpan max = TimeSpan.MinValue;
                        for (int i = 0; i < records.Length; i++) { 
                            int record = records[i];
                            if (IsNull(record))
                                continue;
                            max=(TimeSpan.Compare(values[record],max) >= 0) ? values[record] : max; 
                            hasData = true;
                        } 
                        if (hasData) { 
                            return max;
                        } 
                        return NullValue;

                    case AggregateType.First:
                        if (records.Length > 0) { 
                            return values[records[0]];
                        } 
                        return null; 

                    case AggregateType.Count: 
                        return base.Aggregate(records, kind);

                }
            } 
            catch (OverflowException) {
                throw ExprException.Overflow(typeof(TimeSpan)); 
            } 
            throw ExceptionBuilder.AggregateException(kind, DataType);
        } 

        override public int Compare(int recordNo1, int recordNo2) {
            TimeSpan valueNo1 = values[recordNo1];
            TimeSpan valueNo2 = values[recordNo2]; 

            if (valueNo1 == defaultValue || valueNo2 == defaultValue) { 
                int bitCheck = CompareBits(recordNo1, recordNo2); 
                if (0 != bitCheck)
                    return bitCheck; 
            }
            return TimeSpan.Compare(valueNo1, valueNo2);
        }
 
        public override int CompareValueTo(int recordNo, object value) {
            System.Diagnostics.Debug.Assert(0 <= recordNo, "Invalid record"); 
            System.Diagnostics.Debug.Assert(null != value, "null value"); 

            if (NullValue == value) { 
                if (IsNull(recordNo)) {
                    return 0;
                }
                return 1; 
            }
 
            TimeSpan valueNo1 = values[recordNo]; 
            if ((defaultValue == valueNo1) && IsNull(recordNo)) {
                return -1; 
            }
            return valueNo1.CompareTo((TimeSpan)value);
        }
 
        private static TimeSpan ConvertToTimeSpan(object value) {
            // Webdata 94686: Do not change this checks 
            Type typeofValue= value.GetType(); 

            if (typeofValue == typeof(string)) { 
                return TimeSpan.Parse((string)value);
            }
            else if (typeofValue == typeof(Int32)) {
                return new TimeSpan((Int64)((Int32)value)); 
            }
            else if (typeofValue == typeof(Int64))  { 
                return new TimeSpan((Int64)value); 
            }
            else { 
                return (TimeSpan) value;
            }
        }
 
        public override object ConvertValue(object value) {
            if (NullValue != value) { 
                if (null != value) { 
                    value = ConvertToTimeSpan(value);
                } 
                else {
                    value = NullValue;
                }
            } 
            return value;
        } 
 

        override public void Copy(int recordNo1, int recordNo2) { 
            CopyBits(recordNo1, recordNo2);
            values[recordNo2] = values[recordNo1];
        }
 
        override public Object Get(int record) {
            TimeSpan value = values[record]; 
            if (value != defaultValue) { 
                return value;
            } 
            return GetBits(record);
        }

        override public void Set(int record, Object value) { 
            System.Diagnostics.Debug.Assert(null != value, "null value");
            if (NullValue == value) { 
                values[record] = defaultValue; 
                SetNullBit(record, true);
            } 
            else {
                values[record] = ConvertToTimeSpan(value);
                SetNullBit(record, false);
            } 
        }
 
        override public void SetCapacity(int capacity) { 
            TimeSpan[] newValues = new TimeSpan[capacity];
            if (null != values) { 
                Array.Copy(values, 0, newValues, 0, Math.Min(capacity, values.Length));
            }
            values = newValues;
            base.SetCapacity(capacity); 
        }
 
        override public object ConvertXmlToObject(string s) { 
            return XmlConvert.ToTimeSpan(s);
        } 

        override public string ConvertObjectToXml(object value) {
            return XmlConvert.ToString((TimeSpan)value);
        } 

        override protected object GetEmptyStorage(int recordCount) { 
            return new TimeSpan[recordCount]; 
        }
 
        override protected void CopyValue(int record, object store, BitArray nullbits, int storeIndex) {
            TimeSpan[] typedStore = (TimeSpan[]) store;
            typedStore[storeIndex] = values[record];
            nullbits.Set(storeIndex, IsNull(record)); 
        }
 
        override protected void SetStorage(object store, BitArray nullbits) { 
            values = (TimeSpan[]) store;
            SetNullStorage(nullbits); 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TimeSpanStorage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
// <owner current="false" primary="false">[....]</owner> 
//----------------------------------------------------------------------------- 

namespace System.Data.Common { 
    using System;
    using System.Xml;
    using System.Data.SqlTypes;
    using System.Collections; 

    internal sealed class TimeSpanStorage : DataStorage { 
 
        private static readonly TimeSpan defaultValue = TimeSpan.Zero;
 
        private TimeSpan[] values;

        public TimeSpanStorage(DataColumn column)
        : base(column, typeof(TimeSpan), defaultValue) { 
        }
 
        override public Object Aggregate(int[] records, AggregateType kind) { 
            bool hasData = false;
            try { 
                switch (kind) {
                    case AggregateType.Min:
                        TimeSpan min = TimeSpan.MaxValue;
                        for (int i = 0; i < records.Length; i++) { 
                            int record = records[i];
                            if (IsNull(record)) 
                                continue; 
                            min=(TimeSpan.Compare(values[record],min) < 0) ? values[record] : min;
                            hasData = true; 
                        }
                        if (hasData) {
                            return min;
                        } 
                        return NullValue;
 
                    case AggregateType.Max: 
                        TimeSpan max = TimeSpan.MinValue;
                        for (int i = 0; i < records.Length; i++) { 
                            int record = records[i];
                            if (IsNull(record))
                                continue;
                            max=(TimeSpan.Compare(values[record],max) >= 0) ? values[record] : max; 
                            hasData = true;
                        } 
                        if (hasData) { 
                            return max;
                        } 
                        return NullValue;

                    case AggregateType.First:
                        if (records.Length > 0) { 
                            return values[records[0]];
                        } 
                        return null; 

                    case AggregateType.Count: 
                        return base.Aggregate(records, kind);

                }
            } 
            catch (OverflowException) {
                throw ExprException.Overflow(typeof(TimeSpan)); 
            } 
            throw ExceptionBuilder.AggregateException(kind, DataType);
        } 

        override public int Compare(int recordNo1, int recordNo2) {
            TimeSpan valueNo1 = values[recordNo1];
            TimeSpan valueNo2 = values[recordNo2]; 

            if (valueNo1 == defaultValue || valueNo2 == defaultValue) { 
                int bitCheck = CompareBits(recordNo1, recordNo2); 
                if (0 != bitCheck)
                    return bitCheck; 
            }
            return TimeSpan.Compare(valueNo1, valueNo2);
        }
 
        public override int CompareValueTo(int recordNo, object value) {
            System.Diagnostics.Debug.Assert(0 <= recordNo, "Invalid record"); 
            System.Diagnostics.Debug.Assert(null != value, "null value"); 

            if (NullValue == value) { 
                if (IsNull(recordNo)) {
                    return 0;
                }
                return 1; 
            }
 
            TimeSpan valueNo1 = values[recordNo]; 
            if ((defaultValue == valueNo1) && IsNull(recordNo)) {
                return -1; 
            }
            return valueNo1.CompareTo((TimeSpan)value);
        }
 
        private static TimeSpan ConvertToTimeSpan(object value) {
            // Webdata 94686: Do not change this checks 
            Type typeofValue= value.GetType(); 

            if (typeofValue == typeof(string)) { 
                return TimeSpan.Parse((string)value);
            }
            else if (typeofValue == typeof(Int32)) {
                return new TimeSpan((Int64)((Int32)value)); 
            }
            else if (typeofValue == typeof(Int64))  { 
                return new TimeSpan((Int64)value); 
            }
            else { 
                return (TimeSpan) value;
            }
        }
 
        public override object ConvertValue(object value) {
            if (NullValue != value) { 
                if (null != value) { 
                    value = ConvertToTimeSpan(value);
                } 
                else {
                    value = NullValue;
                }
            } 
            return value;
        } 
 

        override public void Copy(int recordNo1, int recordNo2) { 
            CopyBits(recordNo1, recordNo2);
            values[recordNo2] = values[recordNo1];
        }
 
        override public Object Get(int record) {
            TimeSpan value = values[record]; 
            if (value != defaultValue) { 
                return value;
            } 
            return GetBits(record);
        }

        override public void Set(int record, Object value) { 
            System.Diagnostics.Debug.Assert(null != value, "null value");
            if (NullValue == value) { 
                values[record] = defaultValue; 
                SetNullBit(record, true);
            } 
            else {
                values[record] = ConvertToTimeSpan(value);
                SetNullBit(record, false);
            } 
        }
 
        override public void SetCapacity(int capacity) { 
            TimeSpan[] newValues = new TimeSpan[capacity];
            if (null != values) { 
                Array.Copy(values, 0, newValues, 0, Math.Min(capacity, values.Length));
            }
            values = newValues;
            base.SetCapacity(capacity); 
        }
 
        override public object ConvertXmlToObject(string s) { 
            return XmlConvert.ToTimeSpan(s);
        } 

        override public string ConvertObjectToXml(object value) {
            return XmlConvert.ToString((TimeSpan)value);
        } 

        override protected object GetEmptyStorage(int recordCount) { 
            return new TimeSpan[recordCount]; 
        }
 
        override protected void CopyValue(int record, object store, BitArray nullbits, int storeIndex) {
            TimeSpan[] typedStore = (TimeSpan[]) store;
            typedStore[storeIndex] = values[record];
            nullbits.Set(storeIndex, IsNull(record)); 
        }
 
        override protected void SetStorage(object store, BitArray nullbits) { 
            values = (TimeSpan[]) store;
            SetNullStorage(nullbits); 
        }
    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
