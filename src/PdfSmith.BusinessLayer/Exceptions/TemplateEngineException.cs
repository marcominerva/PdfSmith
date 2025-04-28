namespace PdfSmith.BusinessLayer.Exceptions;

public class TemplateEngineException(string? message, Exception? innerException) : Exception(message, innerException)
{
    public TemplateEngineException(string? message) : this(message, null)
    {
    }
}
