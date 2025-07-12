using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp2.Models
{
    public class ValidationResult
    {
        public string TestName { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;

    }
}
