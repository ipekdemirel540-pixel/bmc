using System.IO;
using ManufacturingToolsSuite.Models.Nesting;

namespace ManufacturingToolsSuite.Services.Nesting;

public static class NestingValidator
{
    public static NestingValidationResult Validate(
        IReadOnlyList<NestingRow> rows,
        NestingParameters parameters)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(parameters.ExcelPath) || !File.Exists(parameters.ExcelPath))
            errors.Add("Geçerli bir SAP BOM Excel dosyası seçin.");

        if (parameters.TotalVehicles <= 0)
            errors.Add("Toplam üretim en az 1 araç olmalıdır.");

        if (parameters.VehiclesPerDay <= 0)
            errors.Add("Günlük üretim en az 1 araç olmalıdır.");

        if (parameters.KerfMm < 0)
            errors.Add("Kesim aralığı negatif olamaz.");

        if (parameters.StandardLengthMm <= 0)
            errors.Add("Standart profil boyu pozitif olmalıdır.");

        if (parameters.StandardLengthMm > NestingParameters.MaxStandardLengthMm)
            errors.Add($"Standart profil boyu en fazla {NestingParameters.MaxStandardLengthMm} mm olabilir.");

        if (parameters.ClampPerEndMm < 0)
            errors.Add("Kelepçe payı negatif olamaz.");

        var usableLength = parameters.StandardLengthMm - (2 * parameters.ClampPerEndMm);
        if (usableLength <= 0)
            errors.Add("Kullanılabilir profil boyu pozitif olmalıdır (standart boy − 2×kelepçe).");

        if (rows.Count == 0 && errors.Count == 0)
            errors.Add("Excel dosyasında işlenebilir parça bulunamadı.");

        if (parameters.CustomSequence.Count > 0)
        {
            if (parameters.CustomSequence.Any(v => v <= 0))
                errors.Add("Özel sekans değerleri pozitif tam sayı olmalıdır.");

            var sum = parameters.CustomSequence.Sum();
            if (sum != parameters.TotalVehicles)
                errors.Add($"Sekans toplamı ({sum}) toplam üretim ({parameters.TotalVehicles}) ile eşleşmiyor.");
        }

        return errors.Count > 0
            ? NestingValidationResult.Fail(errors.ToArray())
            : NestingValidationResult.Ok(warnings);
    }
}
