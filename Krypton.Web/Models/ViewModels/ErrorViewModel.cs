namespace Krypton.Web.Models.ViewModels;

public sealed class ErrorViewModel
{
    public string? RequestId { get; set; }
    public string? ErrorMessage { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
