
using System.Collections;
using System.Data;
using System.Reflection;

namespace NinthBall.Core.PrettyPrint
{
    /// <summary>
    /// Extensions on DataTable supporting PrettyPrint layout bridges.
    /// Responsibility: Structuring data into DataTables (Mapping).
    /// </summary>
    public static class PrettyPrintDataTableExtensions
    {
        //......................................................................
        #region General purpose extensions
        //......................................................................
        public static DataTable WithColumn(this DataTable dt, string colName, Type? type = null, string? format = null, bool? alignRight = null)
        {
            var col = dt.Columns.Add(colName, type ?? typeof(string));
            if (null != format) col.TextFormat = format;
            if (alignRight.HasValue) col.IsRightAligned = alignRight.Value;

            return dt;
        }

        public static DataTable WithColumn<T>(this DataTable dt, string colName, string? format = null, bool? alignRight = null) => dt.WithColumn(colName, typeof(T), format: format, alignRight: alignRight);

        extension(DataColumn dtColumn)
        {
            public bool? IsRightAligned
            {
                set => dtColumn.ExtendedProperties["AlignRight"] = value;
                get => dtColumn.ExtendedProperties["AlignRight"] is bool boolValue ? boolValue : null;
            }

            public string? TextFormat
            {
                set => dtColumn.ExtendedProperties["TextFormat"] = value;
                get => dtColumn.ExtendedProperties["TextFormat"] is string strValue ? strValue : null;
            }

        }

        #endregion

        //......................................................................
        #region Record Transcription (Wide vs Tall)
        //......................................................................

        public static DataTable ToWideTable(this object oneRecord)
        {
            var dt = new DataTable();
            if (oneRecord == null) return dt;

            var properties = GetReadableProperties(oneRecord.GetType());
            foreach (var p in properties) dt.WithColumn(p.Name, p.PropertyType);

            var row = dt.NewRow();
            var idx = 0;
            foreach (var p in properties) row[idx++] = p.GetValue(oneRecord) ?? DBNull.Value;
            dt.Rows.Add(row);

            return dt;
        }

        public static DataTable ToTallTable(this object oneRecord)
        {
            var dt = new DataTable().WithColumn("Property").WithColumn("Value", typeof(object));
            if (oneRecord == null) return dt;

            var properties = GetReadableProperties(oneRecord.GetType());
            foreach (var p in properties)
            {
                var row = dt.NewRow();
                row[0] = p.Name;
                row[1] = p.GetValue(oneRecord) ?? DBNull.Value;
                dt.Rows.Add(row);
            }
            return dt;
        }

        public static DataTable ToWideTable(this IDictionary oneRecord)
        {
            var dt = new DataTable();
            if (oneRecord == null || oneRecord.Count == 0) return dt;

            foreach (DictionaryEntry entry in oneRecord) dt.WithColumn(entry.Key?.ToString() ?? string.Empty, entry.Value?.GetType());

            var row = dt.NewRow();
            var idx = 0;
            foreach (DictionaryEntry entry in oneRecord) row[idx++] = entry.Value ?? DBNull.Value;
            dt.Rows.Add(row);

            return dt;
        }

        public static DataTable ToTallTable(this IDictionary oneRecord)
        {
            var dt = new DataTable().WithColumn("Property").WithColumn("Value", typeof(object));
            if (oneRecord == null || oneRecord.Count == 0) return dt;

            foreach (DictionaryEntry entry in oneRecord)
            {
                var row = dt.NewRow();
                row[0] = entry.Key?.ToString() ?? string.Empty;
                row[1] = entry.Value ?? DBNull.Value;
                dt.Rows.Add(row);
            }
            return dt;
        }

        #endregion

        //......................................................................
        #region Collection Transcription (True Tables)
        //......................................................................

        public static DataTable ToDataTable<TItem>(this IEnumerable<TItem> collection)
        {
            var dt = new DataTable();
            if (collection == null) return dt;

            // Does each TItem represent a IDictionary?
            // Delegate to IEnumerable<IDictionary> to inder columns.
            if (typeof(IDictionary).IsAssignableFrom(typeof(TItem)))
            {
                return ToDataTable(collection.Cast<IDictionary>());
            }

            var properties = GetReadableProperties(typeof(TItem));
            foreach (var p in properties) dt.WithColumn(p.Name, p.PropertyType);

            foreach (var item in collection)
            {
                if (item == null) continue;
                var values = new object?[properties.Count];
                for (int i = 0; i < properties.Count; i++)
                    values[i] = properties[i].GetValue(item) ?? DBNull.Value;

                dt.Rows.Add(values);
            }

            return dt;
        }

        public static DataTable ToDataTable(this IEnumerable<IDictionary> collection)
        {
            var dt = new DataTable();
            if (collection == null) return dt;

            List<object>? keys = null;
            foreach (var dict in collection)
            {
                keys = new List<object>();
                foreach (DictionaryEntry entry in dict)
                {
                    var key = entry.Key?.ToString() ?? string.Empty;
                    dt.WithColumn(key, entry.Value?.GetType());
                    keys.Add(entry.Key!);
                }
                break; // Use first record for schema
            }

            if (keys == null) return dt;

            foreach (var dict in collection)
            {
                var values = new object?[dt.Columns.Count];
                for (int i = 0; i < keys.Count; i++)
                {
                    var key = keys[i];
                    values[i] = dict.Contains(key) ? dict[key] : DBNull.Value;
                }
                dt.Rows.Add(values);
            }

            return dt;
        }

        #endregion

        //......................................................................
        #region Helpers
        //......................................................................
        private static List<PropertyInfo> GetReadableProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && (p.PropertyType.IsPrimitive || p.PropertyType == typeof(string) || p.PropertyType == typeof(decimal) || p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(TimeSpan)))
                .ToList();
        }

        #endregion
    }

}
