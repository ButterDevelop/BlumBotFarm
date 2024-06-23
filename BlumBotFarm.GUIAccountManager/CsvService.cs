using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace BlumBotFarm.GUIAccountManager
{
    public class CsvService
    {
        public CsvService()
        {

        }

        public CsvRecord? GetAccountByNumber(string csvFilePath, int number)
        {
            using (var reader = new StreamReader(csvFilePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                var records = csv.GetRecords<CsvRecord>().ToList();
                var record = records.FirstOrDefault(r => r.Number == number);

                if (record != null)
                {
                    return record;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
