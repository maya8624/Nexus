using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos.Requests
{
    public class PaginationRequest
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public int Skip => (Page - 1) * PageSize;
    }
}
