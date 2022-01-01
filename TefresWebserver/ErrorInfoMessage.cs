namespace Tfres
{
  public sealed class ErrorInfoMessage
  {
    public int ErrorCode { get; set; }
    public string ErrorHelpUrl { get; set; }
    public string ErrorMessage { get; set; }
    public int HttpStatusCode { get; set; }
  }
}