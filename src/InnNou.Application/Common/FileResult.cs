namespace InnNou.Application.Common
{
    // Deliberately NOT wrapped in ApiResponse<T> — binary content can't go in that JSON envelope.
    // Handlers returning this bypass ExceptionHandlingBehavior (which only intercepts
    // ApiResponse<T>-typed handlers); a thrown ApiException still surfaces correctly via
    // Program.cs's global UseExceptionHandler middleware, which builds the same ApiResponse<T>
    // failure JSON regardless of the handler's declared response type.
    public class FileResult
    {
        public required byte[] FileBytes { get; set; }
        public required string FileName { get; set; }
        public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    }
}
