using NinthBall.Core;
using System.Collections;
using System.Data;
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
        #region Matrix & Span Transcription (Grid format)
        //......................................................................

        public static DataTable ToWideTable(this ReadOnlySpan<double> span, string[]? labels = null)
        {
            var dt = new DataTable();
            for (int i = 0; i < span.Length; i++)
            {
                var colName = (labels != null && i < labels.Length) ? labels[i] : $"[{i}]";
                dt.WithColumn(colName, typeof(double));
            }

            var row = dt.NewRow();
            for (int i = 0; i < span.Length; i++) row[i] = span[i];
            dt.Rows.Add(row);

            return dt;
        }

        public static DataTable ToWideTable(this TwoDMatrix matrix, string[]? rowLabels = null, string[]? colLabels = null)
        {
            var dt = new DataTable();
            var numRows = matrix.NumRows;
            var numCols = matrix.NumColumns;

            if (rowLabels != null) dt.WithColumn("Row");

            for (int j = 0; j < numCols; j++)
            {
                var colName = (colLabels != null && j < colLabels.Length) ? colLabels[j] : $"Col {j}";
                dt.WithColumn(colName, typeof(double));
            }

            for (int i = 0; i < numRows; i++)
            {
                var row = dt.NewRow();
                int offset = 0;
                if (rowLabels != null) row[offset++] = (i < rowLabels.Length) ? rowLabels[i] : $"Row {i}";

                var rowData = matrix.Row(i).Span;
                for (int j = 0; j < numCols; j++) row[j + offset] = rowData[j];

                dt.Rows.Add(row);
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
                var row = dt.NewRow();
                row[0] = r.RegimeLabel;
                var tx = r.NextRegimeProbabilities.Span;
                for (int i = 0; i < tx.Length; i++) row[i + 1] = tx[i];
                dt.Rows.Add(row);
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
