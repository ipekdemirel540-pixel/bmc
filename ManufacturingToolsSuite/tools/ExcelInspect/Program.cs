using ManufacturingToolsSuite.Services.Nesting;
using System;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var path = args.Length > 0 ? args[0] : @"C:\Users\diagnostic\Documents\repos\Üretim Teknolojileri Araçları\12M PROCITY.XLSX";
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
    }
}
