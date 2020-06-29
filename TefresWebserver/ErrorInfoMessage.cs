using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tfres
{
  public sealed class ErrorInfoMessage
  {
    public int HttpStatusCode { get; set; }
    public string ErrorMessage { get; set; }
    public int ErrorCode { get; set; }
    public string ErrorHelpUrl { get; set; }
  }
}
