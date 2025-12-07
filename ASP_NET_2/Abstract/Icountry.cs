using ASP_NET_2.Models;

namespace ASP_NET_2.Abstract
{
    public interface Icountry
    {
        IEnumerable<Country> GetCountry();
        Country GetCountryById(int id);
        // скачивание в файл
        (byte[], string) GetExcelFileContent();
        (byte[], string) GetCsvFileContent();

        string InsOrUpdCountry(Country model);
        // загрузка из файла
        string ImportFromExcel(Stream fileStream);
        string ImportFromCsv(Stream fileStream);

        string DeleteCountryById(Country model);

    }
}
