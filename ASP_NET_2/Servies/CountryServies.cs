using ASP_NET_2.Abstract;
using ASP_NET_2.Models;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEP241_ASPNET.Service
{
    // Класс для маппинга CsvHelper
    public sealed class CountryMap : ClassMap<Country>
    {
        public CountryMap()
        {

            Map(m => m.name).Index(1);
            Map(m => m.capital).Index(2);
        }
    }

    public class CountryService : Icountry
    {
        private readonly IConfiguration _config;
        private string ConnectionString => _config["db"];

        public CountryService(IConfiguration config)
        {
            _config = config;
            // Регистрация кодировки для CSVHelper
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        // достать все
        public IEnumerable<Country> GetCountry()
        {
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                return db.Query<Country>("pcountry", commandType: System.Data.CommandType.StoredProcedure);
            }
        }

        // достать по id 
        public Country GetCountryById(int id)
        {
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                // pcountry;2 - вероятно, это выполнение хранимой процедуры с SELECT по ID
                return db.Query<Country>("pcountry;2", new { @id = id }, commandType: System.Data.CommandType.StoredProcedure).FirstOrDefault();
            }
        }

        // изменить и добавить 
        // Используется как универсальный метод для сохранения в БД
        public string InsOrUpdCountry(Country model)
        {
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                DynamicParameters p = new DynamicParameters(model);
                // pcountry;3 - вероятно, это выполнение хранимой процедуры INSERT/UPDATE
                db.ExecuteScalar<Country>("pcountry;3", p, commandType: System.Data.CommandType.StoredProcedure);
                return "ok";
            }
        }

        // удалить
        public string DeleteCountryById(Country model)
        {
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                // pCountry;4 - вероятно, это выполнение хранимой процедуры DELETE
                int rowsAffected = db.Execute("pCountry;4", new { @id = model.id },
                                             commandType: System.Data.CommandType.StoredProcedure);

                return rowsAffected > 0 ? "Successfully deleted" : "Deletion failed: record not found";
            }
        }

        // --- Логика скачивания (Export) ---

        // excel
        public (byte[], string) GetExcelFileContent()
        {
            var countries = GetCountry().ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Countries Data");
                worksheet.Cell(1, 1).InsertTable(countries, "CountriesTable", true);
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    byte[] fileBytes = stream.ToArray();
                    string fileName = $"Countries_ClosedXML_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                    return (fileBytes, fileName);
                }
            }
        }

        // csv
        public (byte[], string) GetCsvFileContent()
        {
            var countries = GetCountry();

            // Используем CultureInfo.InvariantCulture, чтобы избежать проблем с региональными разделителями
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";", // Ваш выбранный разделитель
                // Указываем кодировку UTF-8 с BOM, чтобы Excel корректно открывал русские символы
                Encoding = new UTF8Encoding(true)
            };

            using (var memoryStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(memoryStream, config.Encoding))
                using (var csvWriter = new CsvWriter(streamWriter, config))
                {
                    csvWriter.WriteRecords(countries);
                    streamWriter.Flush();

                    byte[] fileBytes = memoryStream.ToArray();
                    string fileName = $"Countries_CsvHelper_{DateTime.Now:yyyyMMdd_HHmm}.csv";

                    return (fileBytes, fileName);
                }
            }
        }


        // --- Логика импорта (Import) ---

        // excel (ClosedXML)
        public string ImportFromExcel(Stream fileStream)
        {
            var countries = new List<Country>();
            int importedCount = 0;

            using (var workbook = new XLWorkbook(fileStream))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                    return "Ошибка: Рабочий лист не найден в файле Excel.";

                var range = worksheet.RangeUsed();

                // Перебираем строки, начиная со 2-й (пропускаем заголовок)
                for (int row = 2; row <= range.RowCount(); row++)
                {
                    string name = worksheet.Cell(row, 1).GetString().Trim();
                    string capital = worksheet.Cell(row, 2).GetString().Trim();

                    if (string.IsNullOrEmpty(name))
                        continue;

                    countries.Add(new Country
                    {
                        // id будет 0 или NULL, что позволит InsOrUpdCountry добавить новую запись
                        name = name,
                        capital = capital
                    });
                    importedCount++;
                }
            }

            // Сохранение в БД: используем Dapper-метод InsOrUpdCountry для каждой записи
            foreach (var country in countries)
            {
                InsOrUpdCountry(country);
            }

            return $"Успешно импортировано и сохранено {importedCount} стран из Excel.";
        }

        // csv (CsvHelper)
        public string ImportFromCsv(Stream fileStream)
        {
            int importedCount = 0;

            // разделитель {;}  и кодировка win-1251
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true, // Пропускаем строку заголовков
                Encoding = Encoding.GetEncoding("windows-1251") // Часто используется для русского CSV
            };

            using (var reader = new StreamReader(fileStream, config.Encoding))
            using (var csv = new CsvReader(reader, config))
            {
                // Регистрируем маппинг
                csv.Context.RegisterClassMap<CountryMap>();

                // Получаем все записи
                var countries = csv.GetRecords<Country>().ToList();
                importedCount = countries.Count;

                // Сохранение в БД
                foreach (var country in countries)
                {
                    InsOrUpdCountry(country);
                }
            }

            return $"Успешно импортировано и сохранено {importedCount} стран из CSV (CsvHelper).";
        }
    }
}