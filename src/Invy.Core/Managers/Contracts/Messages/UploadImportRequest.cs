namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A CSV file to import, uploaded before its columns are checked.</summary>
/// <param name="Name">The file's name, such as homebox-export.csv.</param>
/// <param name="Data">The file's bytes, base64 in JSON. UTF-8 CSV with a heading row, up to 20 MB.</param>
public sealed record UploadImportRequest(string Name, byte[] Data);
