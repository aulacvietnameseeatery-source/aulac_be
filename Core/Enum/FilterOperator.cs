using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Enum
{

    /// Các toán tử lọc được hỗ trợ trong QueryDTO Filter.
    /// Dùng để parse JSON filter array và build SQL WHERE conditions.
    /// Created By DatND (17/1/2026)

    public enum FilterOperator
    {
        ///Toans tử IS NULL - kiểm tra giá trị NULL
        
        IsNull,
        /// Toán tử IS NOT NULL - kiểm tra giá trị không NULL
        IsNotNull,

        /// Toán tử <> - so sánh khác
        DifferentFrom,

        /// Toán tử LIKE '%value%' - tìm kiếm chuỗi chứa giá trị

        Contains,

        /// Toán tử NOT LIKE '%value%' - tìm kiếm chuỗi không chứa giá trị

        NotContains,

        /// Toán tử LIKE 'value%' - tìm kiếm chuỗi bắt đầu bằng giá trị

        StartsWith,


        /// Toán tử LIKE '%value' - tìm kiếm chuỗi kết thúc bằng giá trị

        EndsWith,


        /// Toán tử = - so sánh bằng

        Equal,


        /// Toán tử != hoặc &lt;&gt; - so sánh khác

        NotEqual,


        /// Toán tử &gt; - lớn hơn

        GreaterThan,


        /// Toán tử &lt; - nhỏ hơn

        LessThan,


        /// Toán tử &gt;= - lớn hơn hoặc bằng

        GreaterThanOrEqual,


        /// Toán tử &lt;= - nhỏ hơn hoặc bằng

        LessThanOrEqual,
    }
}
