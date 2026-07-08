using ManufacturingToolsSuite.Services.Nesting;

var path = args.Length > 0 ? args[0] : @"C:\Users\KLAUS\Desktop\Apron Profiller_24.01.2023.xlsx";
try
{
    var rows = SapBomExcelReader.Read(path);
    Console.WriteLine($"OK: {rows.Count} rows, format={SapBomExcelReader.LastMatchedFormat}");
    foreach (var row in rows.Take(5))
        Console.WriteLine($"  {row.ComponentNumber} | {row.Dimensions} | {row.LengthMm:F0}mm | qty={row.Quantity} | {row.ComponentName}");
    Console.WriteLine($"  Dimensions groups: {rows.Select(r => r.Dimensions).Distinct().Count()}");
}
catch (Exception ex)
{
    Console.WriteLine($"FAIL: {ex.Message}");
    Environment.Exit(1);
}
