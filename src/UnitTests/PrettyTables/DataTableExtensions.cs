using NinthBall.Core;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace UnitTests.PrettyTables
{
    /// <summary>
    /// Extensions on DataTable supporting PrettyPrint layout bridges.
    /// Responsibility: Structuring data into DataTables (Mapping).
    /// </summary>
    internal static class DataTableExtensions
    {
        //......................................................................
        #region General purpose extensions
        //......................................................................
        public static DataTable WithColumn(this DataTable dt, string colName, Type? type = null)
        {
            dt.Columns.Add(colName, type ?? typeof(string));
            return dt;
        }

        public static DataTable WithFormat(this DataTable dt, string colName, string format)
        {
            dt.Columns[colName]!.ExtendedProperties["Format"] = format;
            return dt;
        }

        public static DataTable WithAlignment(this DataTable dt, string colName, string alignment)
        {
            dt.Columns[colName]!.ExtendedProperties["Align"] = alignment;
            return dt;
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

        public static DataTable ToTable<T>(this IEnumerable<T> collection)
        {
            var dt = new DataTable();
            if (collection == null) return dt;

            // Handle collections of dictionaries via delegation
            if (typeof(IDictionary).IsAssignableFrom(typeof(T)))
            {
                return ToTable(collection.Cast<IDictionary>());
            }

            var properties = GetReadableProperties(typeof(T));
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

        public static DataTable ToTable(this IEnumerable<IDictionary> collection)
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
        #region Matrix & Span Transcription (Grid format)
        //......................................................................

        public static DataTable ToTable(this ReadOnlySpan<double> collection, string[]? labels = null)
        {
            var dt = new DataTable();
            for (int i = 0; i < collection.Length; i++)
            {
                var colName = (labels != null && i < labels.Length) ? labels[i] : $"[{i}]";
                dt.WithColumn(colName, typeof(double));
            }

            var values = new object[collection.Length];
            for (int i = 0; i < collection.Length; i++) values[i] = collection[i];
            dt.Rows.Add(values);

            return dt;
        }

        public static DataTable ToTable(this TwoDMatrix collection, string[]? rowLabels = null, string[]? colLabels = null)
        {
            var dt = new DataTable();
            var numRows = collection.NumRows;
            var numCols = collection.NumColumns;

            if (rowLabels != null) dt.WithColumn("Row");

            for (int j = 0; j < numCols; j++)
            {
                var colName = (colLabels != null && j < colLabels.Length) ? colLabels[j] : $"Col {j}";
                dt.WithColumn(colName, typeof(double));
            }

            for (int i = 0; i < numRows; i++)
            {
                var values = new object[dt.Columns.Count];
                int offset = 0;
                if (rowLabels != null) values[offset++] = (i < rowLabels.Length) ? rowLabels[i] : $"Row {i}";

                var rowData = collection.Row(i).Span;
                for (int j = 0; j < numCols; j++) values[j + offset] = rowData[j];

                dt.Rows.Add(values);
            }

            return dt;
        }

        #endregion

        //......................................................................
        #region Domain Specific Transcription
        //......................................................................

        public static DataTable RegimeTransitionsAsDataTable(this HRegimes regimes)
        {
            var dt = new DataTable();
            dt.WithColumn("Regime");
            foreach (var r in regimes.Regimes)
                dt.WithColumn(r.RegimeLabel, typeof(double)).WithFormat(r.RegimeLabel, "P0");

            foreach (var r in regimes.Regimes)
            {
                var values = new object[dt.Columns.Count];
                values[0] = r.RegimeLabel;
                var tx = r.NextRegimeProbabilities.Span;
                for (int i = 0; i < tx.Length; i++) values[i + 1] = tx[i];
                dt.Rows.Add(values);
            }

            return dt;
        }

        #endregion

        #region Helpers

        private static List<PropertyInfo> GetReadableProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && (p.PropertyType.IsPrimitive || p.PropertyType == typeof(string) || p.PropertyType == typeof(decimal) || p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(TimeSpan)))
                .ToList();
        }

        #endregion
    }
}
