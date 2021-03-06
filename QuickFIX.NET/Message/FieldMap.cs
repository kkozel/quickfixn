﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuickFix.Fields;
using QuickFix.Fields.Converters;

namespace QuickFix
{
    /// <summary>
    /// Field container used by messages, groups, and composites
    /// </summary>
    public class FieldMap : IEnumerable<KeyValuePair<int, Fields.IField>>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public FieldMap()
        {
            _fields = new SortedDictionary<int, Fields.IField>(); /// FIXME sorted dict is a hack to get quasi-correct field order
            _groups = new Dictionary<int, List<Group>>();
            this.RepeatedTags = new List<Fields.IField>();
        }

        /// <summary>
        /// Constructor with field order
        /// </summary>
        /// <param name="fieldOrd"></param>
        public FieldMap(int[] fieldOrd)
            : this()
        {
            _fieldOrder = fieldOrd;
        }


        /// <summary>
        /// FIXME this should probably make a deeper copy
        /// </summary>
        /// <param name="src">The QuickFix.FieldMap to copy</param>
        /// <returns>A copy of the given QuickFix.FieldMap</returns>
        public FieldMap(FieldMap src)
        {
            this._fieldOrder = src._fieldOrder;
            
            this._fields = new SortedDictionary<int, Fields.IField>(src._fields);
            
            this._groups = new Dictionary<int, List<Group>>();
            foreach (KeyValuePair<int, List<Group>> g in src._groups)
                this._groups.Add(g.Key, new List<Group>(g.Value));

            this.RepeatedTags = new List<Fields.IField>(src.RepeatedTags);
        }

        /// <summary>
        /// FieldOrder Property
        /// order of field tags as an integer array
        /// </summary>
        public int[] FieldOrder
        {
            get { return _fieldOrder; }
            private set { _fieldOrder = value; }
        }

        /// <summary>
        /// QuickFIX-CPP compat, see FieldOrder property
        /// </summary>
        /// <returns>field order integer array</returns>
        public int[] getFieldOrder()
        {
            return _fieldOrder;
        }

        /// <summary>
        /// Remove a field from the fieldmap
        /// </summary>
        /// <param name="field"></param>
        /// <returns>true if field was removed, false otherwise</returns>
        public bool RemoveField(int field)
        {
            return _fields.Remove(field);
        }

        /// <summary>
        /// set field in the fieldmap
        /// will overwrite field if it exists
        /// </summary>
        public void SetField(Fields.IField field)
        {
            _fields[field.Tag] = field;
        }

        /// <summary>
        /// Set field, with optional override check
        /// </summary>
        /// <param name="field"></param>
        /// <param name="overwrite">will overwrite existing field if set to true</param>
        /// <returns>false if overwrite would be violated, else true</returns>
        public bool SetField(Fields.IField field, bool overwrite)
        {
            if (_fields.ContainsKey(field.Tag) && !overwrite)
                return false;
            
            SetField(field);
            return true;
        }

        /// <summary>
        /// Gets a boolean field
        /// </summary>
        /// <param name="field"></param>
        /// <exception cref="FieldNotFoundException">thrown if field isn't found</exception>
        public void GetField(Fields.BooleanField field)
        {
            field.Obj = GetBoolean(field.Tag);
        }

        /// <summary>
        /// Gets a string field
        /// </summary>
        /// <param name="field"></param>
        /// <exception cref="FieldNotFoundException">thrown if field isn't found</exception>
        public void GetField(Fields.StringField field)
        {
            field.Obj = GetString(field.Tag);
        }

        /// <summary>
        /// Gets a char field
        /// </summary>
        /// <param name="field"></param>
        /// <exception cref="FieldNotFoundException">thrown if field isn't found</exception>
        public void GetField(Fields.CharField field)
        {
            field.Obj = GetChar(field.Tag);
        }

        /// <summary>
        /// Gets an int field
        /// </summary>
        /// <param name="field"></param>
        /// <exception cref="FieldNotFoundException">thrown if field isn't found</exception>
        public void GetField(Fields.IntField field)
        {
            field.Obj = GetInt(field.Tag);
        }

        /// <summary>
        /// Gets a decimal field
        /// </summary>
        /// <param name="field"></param>
        /// <exception cref="FieldNotFoundException">thrown if field isn't found</exception>
        public void GetField(Fields.DecimalField field)
        {
            field.Obj = GetDecimal(field.Tag);
        }

        /// <summary>
        /// Gets a datetime field
        /// </summary>
        /// <param name="field"></param>
        /// <exception cref="FieldNotFoundException">thrown if field isn't found</exception>
        public void GetField(Fields.DateTimeField field)
        {
            field.Obj = GetDateTime(field.Tag);
        }

        /// <summary>
        /// Check to see if field is set
        /// </summary>
        /// <param name="field">Field Object</param>
        /// <returns>true if set</returns>
        public bool IsSetField(Fields.IField field)
        {
            return IsSetField(field.Tag);
        }

        /// <summary>
        /// Check to see if field is set
        /// </summary>
        /// <param name="tag">Tag Number</param>
        /// <returns>true if set</returns>
        public bool IsSetField(int tag)
        {
            return _fields.ContainsKey(tag);
        }

        /// <summary>
        /// Add a group to message; the group counter is automatically incremented.
        /// </summary>
        /// <param name="group">group to add</param>
        public void AddGroup(Group grp)
        {
            AddGroup(grp, true);
        }

        /// <summary>
        /// Add a group to message; optionally auto-increment the counter.
        /// When parsing from a string (e.g. Message::FromString()), we want to leave the counter alone
        /// so we can detect when the counterparty has set it wrong.
        /// </summary>
        /// <param name="group">group to add</param>
        /// <param name="autoIncCounter">if true, auto-increment the counter, else leave it as-is</param>
        internal void AddGroup(Group grp, bool autoIncCounter)
        {
            Group group = new Group(grp); // copy, in case user code reuses input object

            if (!_groups.ContainsKey(group.Field))
                _groups.Add(group.Field, new List<Group>());
            _groups[group.Field].Add(group);

            if (autoIncCounter)
            {
                // increment group size
                int groupsize = _groups[group.Field].Count;
                int counttag = group.Field;
                IntField count = null;

                count = new IntField(counttag, groupsize);
                this.SetField(count, true);
            }
        }

        /// <summary>
        /// Gets specific group instance
        /// </summary>
        /// <param name="num">num of group (starting at 1)</param>
        /// <param name="field">tag of group</param>
        /// <returns>Group object</returns>
        /// <exception cref="FieldNotFoundException" />
        public Group GetGroup(int num, int field)
        {
            if (!_groups.ContainsKey(field))
                throw new FieldNotFoundException(field);
            if (num <= 0)
                throw new FieldNotFoundException(field);
            if (_groups[field].Count < num)
                throw new FieldNotFoundException(field);

            return _groups[field][num - 1];
        }

        /// <summary>
        /// Gets the integer value of a field
        /// </summary>
        /// <param name="tag">the FIX tag</param>
        /// <returns>the integer field value</returns>
        /// <exception cref="FieldNotFoundException" />
        public int GetInt(int tag)
        {
            try
            {
                Fields.IField fld = _fields[tag];
                if (fld.GetType() == typeof(IntField))
                    return ((IntField)fld).Obj;
                else
                    return IntConverter.Convert(fld.ToString());
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                throw new FieldNotFoundException(tag);
            }
        }

        /// <summary>
        /// Gets the DateTime value of a field
        /// </summary>
        /// <param name="tag">the FIX tag</param>
        /// <returns>the DateTime value</returns>
        /// <exception cref="FieldNotFoundException" />
        public System.DateTime GetDateTime(int tag)
        {
            try
            {
                Fields.IField fld = _fields[tag];
                if (fld.GetType() == typeof(DateTimeField))
                    return ((DateTimeField)(fld)).Obj;
                else
                    return DateTimeConverter.ConvertToDateTime(fld.ToString());
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                throw new FieldNotFoundException(tag);
            }
        }

        /// <summary>
        /// Gets the boolean value of a field
        /// </summary>
        /// <param name="tag">the FIX tag</param>
        /// <returns>the bool value</returns>
        /// <exception cref="FieldNotFoundException" />
        public bool GetBoolean(int tag)
        {
            try
            {
                Fields.IField fld = _fields[tag];
                if (fld.GetType() == typeof(BooleanField))
                    return ((BooleanField)fld).Obj;
                else
                    return BoolConverter.Convert(fld.ToString());
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                throw new FieldNotFoundException(tag);
            }
        }

        /// <summary>
        /// Gets the string value of a field
        /// </summary>
        /// <param name="tag">the FIX tag</param>
        /// <returns>the string value</returns>
        /// <exception cref="FieldNotFoundException" />
        public String GetString(int tag)
        {
            try
            {
                return _fields[tag].ToString();
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                throw new FieldNotFoundException(tag);
            }
        }

        /// <summary>
        /// Gets the char value of a field
        /// </summary>
        /// <param name="tag">the FIX tag</param>
        /// <returns>the char value</returns>
        /// <exception cref="FieldNotFoundException" />
        public char GetChar(int tag)
        {
            try
            {
                Fields.IField fld = _fields[tag];
                if (fld.GetType() == typeof(CharField))
                    return ((CharField)fld).Obj;
                else
                    return CharConverter.Convert(fld.ToString());
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                throw new FieldNotFoundException(tag);
            }
        }

        /// <summary>
        /// Gets the decimal value of a field
        /// </summary>
        /// <param name="tag">the FIX tag</param>
        /// <returns>the decimal value</returns>
        /// <exception cref="FieldNotFoundException" />
        public Decimal GetDecimal(int tag)
        {
            try
            {
                Fields.IField fld = _fields[tag];
                if (fld.GetType() == typeof(DecimalField))
                    return ((DecimalField)fld).Obj;
                else
                    return DecimalConverter.Convert(fld.ToString());
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                throw new FieldNotFoundException(tag);
            }
        }

        /// <summary>
        /// Removes specific group instance
        /// </summary>
        /// <param name="num">num of group (starting at 1)</param>
        /// <param name="field">tag of group</param>
        /// <exception cref="FieldNotFoundException" />
        public void RemoveGroup(int num, int field)
        {
            if (!_groups.ContainsKey(field))
                throw new FieldNotFoundException(field);
            if (num <= 0)
                throw new FieldNotFoundException(field);
            if (_groups[field].Count < num)
                throw new FieldNotFoundException(field);

            if (_groups[field].Count.Equals(1))
                _groups.Remove(field);
            else
                _groups[field].RemoveAt(num - 1);
        }

        /// <summary>
        /// Replaces specific group instance
        /// </summary>
        /// <param name="num">num of group (starting at 1)</param>
        /// <param name="field">tag of group</param>
        /// <returns>Group object</returns>
        /// <exception cref="FieldNotFoundException" />
        public Group ReplaceGroup(int num, int field, Group group)
        {
            if (!_groups.ContainsKey(field))
                throw new FieldNotFoundException(field);
            if (num <= 0)
                throw new FieldNotFoundException(field);
            if (_groups[field].Count < num)
                throw new FieldNotFoundException(field);

            return _groups[field][num - 1] = group;
        }


        /// <summary>
        /// getField without a type defaults to returning a string
        /// </summary>
        /// <param name="tag">fix tag</param>
        public string GetField(int tag)
        {
            if (_fields.ContainsKey(tag))
                return _fields[tag].ToString();
            else
                throw new FieldNotFoundException(tag);
        }

        /// <summary>
        /// Removes fields and groups in message
        /// </summary>
        public virtual void Clear()
        {
            _fields.Clear();
            _groups.Clear();
        }

        /// <summary>
        /// Checks emptiness of message
        /// </summary>
        /// <returns>true if no fields or groups have been set</returns>
        public bool IsEmpty()
        {
            return ((_fields.Count == 0) && (_groups.Count == 0));
        }

        public int CalculateTotal()
        {
            int total = 0;
            foreach (Fields.IField field in _fields.Values)
            {
                if (field.Tag != Fields.Tags.CheckSum)
                    total += field.getTotal();
            }

            // TODO not sure if repeated CheckSum should be included in the total
            foreach (Fields.IField field in this.RepeatedTags)
            {
                if (field.Tag != Fields.Tags.CheckSum)
                    total += field.getTotal();
            }

            foreach (List<Group> groupList in _groups.Values)
            {
                foreach (Group group in groupList)
                    total += group.CalculateTotal();
            }
            return total;
        }

        public int CalculateLength()
        {
            int total = 0;
            foreach (Fields.IField field in _fields.Values)
            {
                if (field != null
                    && field.Tag != Tags.BeginString
                    && field.Tag != Tags.BodyLength
                    && field.Tag != Tags.CheckSum)
                {
                    total += field.getLength();
                }
            }

            // TODO not sure if repeated BeginString/BodyLength/CheckSum should be counted
            foreach (Fields.IField field in this.RepeatedTags)
            {
                if (field != null
                    && field.Tag != Tags.BeginString
                    && field.Tag != Tags.BodyLength
                    && field.Tag != Tags.CheckSum)
                {
                    total += field.getLength();
                }
            }

            foreach (List<Group> groupList in _groups.Values)
            {
                foreach (Group group in groupList)
                    total += group.CalculateLength();
            }
    
            return total;
        }

        public virtual string CalculateString()
        {
            return CalculateString(new StringBuilder(), new int[0]);
        }

        public virtual string CalculateString(StringBuilder sb, int[] preFields)
        {
            foreach (int preField in preFields)
            {
                if (IsSetField(preField))
                    sb.Append(preField + "=" + GetField(preField)).Append(Message.SOH);
            }

            HashSet<int> groupCounterTags = new HashSet<int>(_groups.Keys);

            foreach (Fields.IField field in _fields.Values)
            {
                if (groupCounterTags.Contains(field.Tag))
                    continue;
                if (IsOrderedField(field.Tag, preFields))
                    continue;
                sb.Append(field.Tag.ToString() + "=" + field.ToString());
                sb.Append(Message.SOH);
            }

            foreach (List<Group> groupList in _groups.Values)
            {
                if (groupList.Count > 0) // for extra caution
                {
                    int counterTag = groupList[0].Field;
                    sb.Append(_fields[counterTag].toStringField());
                    sb.Append(Message.SOH);
                }

                foreach (Group group in groupList)
                {
                    sb.Append(group.CalculateString());
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get count of items in the repeating group
        /// </summary>
        /// <param name="fieldNo">the counter tag of the group</param>
        /// <returns></returns>
        public int GroupCount(int fieldNo)
        {
            if(_groups.ContainsKey(fieldNo))
            {
                return _groups[fieldNo].Count;
            }
            else
            {
                return 0;
            }
        }

        private bool IsOrderedField(int field, int[] fieldOrder)
        {
            foreach (int f in fieldOrder)
            {
                if (field == f)
                    return true;
            }
        
            return false;
        }

        /// <summary>
        /// Return a List containing the counter tag for each group in this message.
        /// (The returned List is a static copy.)
        /// </summary>
        /// <returns></returns>
        public List<int> GetGroupTags()
        {
            return new List<int>(_groups.Keys);
        }

        #region Private Members
        private SortedDictionary<int, Fields.IField> _fields; /// FIXME sorted dict is a hack to get quasi-correct field order
        private Dictionary<int, List<Group>> _groups;
        private int[] _fieldOrder;
        #endregion

        #region Properties
        /// <summary>
        /// Used for validation.  Only set during Message parsing.
        /// </summary>
        public List<Fields.IField> RepeatedTags { get; private set; }
        #endregion

        #region IEnumerable<KeyValuePair<int,IField>> Members

        public IEnumerator<KeyValuePair<int, IField>> GetEnumerator()
        {
            return _fields.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _fields.GetEnumerator();
        }

        #endregion
    }
}
