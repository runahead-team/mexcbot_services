using System;
using System.Collections.Generic;
using sp.Core.Exceptions;

namespace sp.Core.Models
{
    public class TableRequest : PagingRequest
    {
        public Dictionary<string, string> Filters { get; set; } = new Dictionary<string, string>();

        public string Search { get; set; }

        //Return metadata
        public bool ResponseMeta { get; set; }

        #region Sort

        public string OrderBy { get; set; }

        public bool IsDesc { get; set; }

        public string GetOrderByText(Type type, string defaultOrder = "Id", string prefix = "")
        {
            if (string.IsNullOrEmpty(defaultOrder))
            {
                if (string.IsNullOrEmpty(OrderBy))
                {
                    OrderBy = defaultOrder;
                    IsDesc = true;
                }
            }

            if (string.IsNullOrEmpty(OrderBy))
                return string.Empty;

            if (type.GetProperty(OrderBy) == null)
                throw new AppException("Invalid OrderBy");

            var orderType = IsDesc ? "DESC" : "";

            if (string.IsNullOrEmpty(OrderBy))
                return $"ORDER BY {OrderBy} {orderType}";

            return $"ORDER BY {prefix}.{OrderBy} {orderType}";
        }

        #endregion
    }
}