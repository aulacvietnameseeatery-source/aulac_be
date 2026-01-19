using Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Helpers
{

    /**
     * Helper class để map string operators sang FilterOperator enum và xử lý SQL.
     * Created By DatND (17/1/2026)
     */

    public static class FilterOperatorHelper
    {

        /**
         * Map từ string operator (từ JSON) sang FilterOperator enum.
         * Case-insensitive mapping.
         * Created By DatND (17/1/2026)
        */

        private static readonly Dictionary<string, FilterOperator> OperatorMap = new()
   {

            // NULL check operators
            { "isnull", FilterOperator.IsNull },
            { "notnull", FilterOperator.IsNotNull },

            // Different from operator
            { "<>", FilterOperator.DifferentFrom },
            { "differentfrom", FilterOperator.DifferentFrom },

            // LIKE operators
            { "contains", FilterOperator.Contains },
            { "notcontains", FilterOperator.NotContains },
            { "startswith", FilterOperator.StartsWith },
            { "endswith", FilterOperator.EndsWith },

            // Comparison operators
            { "=", FilterOperator.Equal },
            { "==", FilterOperator.Equal },
            { "eq", FilterOperator.Equal },
            { "equals", FilterOperator.Equal },

            { "!=", FilterOperator.NotEqual },
            { "ne", FilterOperator.NotEqual },
            { "notequal", FilterOperator.NotEqual },

            { ">", FilterOperator.GreaterThan },
            { "gt", FilterOperator.GreaterThan },
            { "greaterthan", FilterOperator.GreaterThan },

            { "<", FilterOperator.LessThan },
            { "lt", FilterOperator.LessThan },
            { "lessthan", FilterOperator.LessThan },

            { ">=", FilterOperator.GreaterThanOrEqual },
            { "gte", FilterOperator.GreaterThanOrEqual },
            { "greaterthanorequal", FilterOperator.GreaterThanOrEqual },

            { "<=", FilterOperator.LessThanOrEqual },
            { "lte", FilterOperator.LessThanOrEqual },
            { "lessthanorequal", FilterOperator.LessThanOrEqual },

        };


        /** Parse string operator thành FilterOperator enum.
         * Trả về null nếu không tìm thấy operator hợp lệ.
         * Created By DatND (17/1/2026)
         */

        public static FilterOperator? ParseOperator(string operatorStr)
        {
            if (string.IsNullOrWhiteSpace(operatorStr))
                return null;

            var normalized = operatorStr.ToLower().Trim();
            return OperatorMap.TryGetValue(normalized, out var op) ? op : null;
        }


        /** Lấy SQL operator string từ FilterOperator enum.
         * Created By DatND (17/1/2026)
         */
        public static string GetSqlOperator(FilterOperator operatorType)
        {
            return operatorType switch
            {
                // Standard comparison operators
                FilterOperator.Equal => "=",
                FilterOperator.NotEqual => "!=",
                FilterOperator.DifferentFrom => "<>",
                FilterOperator.GreaterThan => ">",
                FilterOperator.LessThan => "<",
                FilterOperator.GreaterThanOrEqual => ">=",
                FilterOperator.LessThanOrEqual => "<=",
                _ => throw new ArgumentException($"Unsupported operator: {operatorType}")
            };
        }


        /**
         * Build điều kiện cho LIKE operators (contains, startswith, endswith).
         * Created By DatND (17/1/2026)
         */

        public static string BuildLikePattern(FilterOperator operatorType, string value)
        {
            return operatorType switch
            {
                FilterOperator.Contains => $"%{value}%",
                FilterOperator.NotContains => $"%{value}%",
                FilterOperator.StartsWith => $"{value}%",
                FilterOperator.EndsWith => $"%{value}",
                _ => throw new ArgumentException($"Operator {operatorType} is not a LIKE operator")
            };
        }


        /**
         * Kiểm tra operator có phải là LIKE operator không.
         * Created By DatND (17/1/2026)
         */
        public static bool IsLikeOperator(FilterOperator operatorType)
        {
            return operatorType == FilterOperator.Contains ||
                   operatorType == FilterOperator.NotContains ||
                   operatorType == FilterOperator.StartsWith ||
                   operatorType == FilterOperator.EndsWith;
        }
    }
}
