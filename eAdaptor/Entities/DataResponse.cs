using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eAdaptor.Entities
{
    public class DataResponse
    {
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? Data { get; set; }
    }
    public class XMLDataResponse
    {
        public string? Status { get; set; }
        public string? Message { get; set; }
        public UniversalResponseData? Data { get; set; }
    }
}
