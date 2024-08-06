using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Globalization;

public class CurrencyConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrEmpty(text)) return 0m;

        var cleanedText = text.Replace("$", "").Trim();
        return decimal.Parse(cleanedText, NumberStyles.Currency, CultureInfo.InvariantCulture);
    }
}
