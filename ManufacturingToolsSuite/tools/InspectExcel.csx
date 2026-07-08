#r "nuget: ClosedXML, 0.105.0"
using ClosedXML.Excel;

var path = @"C:\Users\KLAUS\Desktop\Apron Profiller_24.01.2023.xlsx";
using var wb = new XLWorkbook(path);
var sheet = wb.Worksheet(1);
var used = sheet.RangeUsed();
if (used == null) { Console.WriteLine("empty"); return; }
int maxRow = Math.Min(used.LastRow().RowNumber(), 15);
int maxCol = Math.Min(used.LastColumn().ColumnNumber(), 12);
for (int r = 1; r <= maxRow; r++)
{
    var cells = new List<string>();
    for (int c = 1; c <= maxCol; c++)
        cells.Add($"{(char)('A'+c-1)}={sheet.Cell(r,c).GetFormattedString()}");
    Console.WriteLine($"R{r}: {string.Join(" | ", cells)}");
}
