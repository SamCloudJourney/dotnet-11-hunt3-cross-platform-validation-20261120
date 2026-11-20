using System.Text.Json;

var result = new List<object>();
foreach (string component in new[] { "ordinary", "..", ".. ", "name. " })
{
    string root = Path.Combine(Path.GetTempPath(), "path-control-" + Guid.NewGuid().ToString("N"));
    string child = Path.Combine(root, "child");
    Directory.CreateDirectory(child);
    string candidate = Path.Combine(child, component);
    string calculated = Path.GetFullPath(candidate);
    string status;
    try
    {
        var created = Directory.CreateDirectory(candidate);
        status = "created:" + created.FullName;
    }
    catch (Exception ex)
    {
        status = ex.GetType().Name;
    }
    string[] physicalChildren = Directory.EnumerateDirectories(child).Select(Path.GetFileName).OrderBy(x => x).ToArray()!;
    result.Add(new { component, calculated, status, physicalChildren });
    Directory.Delete(root, recursive: true);
}
Console.WriteLine(JsonSerializer.Serialize(new { framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, os = System.Runtime.InteropServices.RuntimeInformation.OSDescription, result }, new JsonSerializerOptions { WriteIndented = true }));
