using ASP_NET_2.Abstract;
using ASP_NET_2.Models;
using Microsoft.AspNetCore.Mvc;

namespace SEP241_ASPNET.Controllers
{
    public class CountryController : Controller
    {
        Icountry service;
        public CountryController(Icountry service)
        {
            this.service = service;
        }
        public ActionResult Index()
        {
            var result = service.GetCountry();
            return View(result);
        }

        //GET: Dowloand
        // Для Excel (ClosedXML)
        public ActionResult Dowloand_ex()
        {
            var (fileBytes, fileName) = service.GetExcelFileContent();
            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(fileBytes, contentType, fileName);
        }

        // Для CSV (CsvHelper)
        public ActionResult Dowloand_csv()
        {
            var (fileBytes, fileName) = service.GetCsvFileContent();
            string contentType = "text/csv";

            return File(fileBytes, contentType, fileName);
        }


        // GET: CountryController/Details/5
        public ActionResult Details(int id)
        {
            return View(service.GetCountryById(id));
        }

        // GET: CountryController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: CountryController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Country model)
        {
            //service.InsOrUpdGetCountry(model);

            try
            {
                model.id = 0;
                service.InsOrUpdCountry(model);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
        [HttpPost]
        public ActionResult ImportExcel(IFormFile uploadedFile)
        {
            // если файл не пустой
            if (uploadedFile != null && uploadedFile.Length > 0)
            { 
                // Передаем поток файла в сервис
                string status = service.ImportFromExcel(uploadedFile.OpenReadStream());

                TempData["StatusMessage"] = status;
                return RedirectToAction(nameof(Index));
            }
            TempData["StatusMessage"] = "Ошибка: Файл не был загружен.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // защита от CSRF атак
        public ActionResult ImportCSV(IFormFile uploadedFile)
        {
            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                try
                {
                    string status = service.ImportFromCsv(uploadedFile.OpenReadStream());

                    // Сохраняем сообщение о статусе для отображения на странице Index
                    TempData["StatusMessage"] = status;

                    // Перенаправляем пользователя на главную страницу списка
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Обработка любых ошибок, возникших во время импорта (например, неверный формат)
                    TempData["StatusMessage"] = $"Ошибка импорта CSV: {ex.Message}";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Если файл не был загружен
            TempData["StatusMessage"] = "Ошибка: Файл не был загружен или пуст.";
            return RedirectToAction(nameof(Index));
        }


        // GET: CountryController/Edit/5
        public ActionResult Edit(int id)
        {
            var country = service.GetCountryById(id);
            // поверка на существование
            if (country == null)
            {
                return NotFound();
            }
            return View(country);
        }

        // POST: CountryController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Country model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Здесь model.id > 0 
                    service.InsOrUpdCountry(model);

                    // Если успех, перенаправляем на список
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Добавьте логирование ex!
                    ModelState.AddModelError("", "Ошибка при обновлении записи: " + ex.Message);
                    return View(model);
                }
            }

            return View(model);
        }

        // GET: CountryController/Delete/5
        public ActionResult Delete(int id)
        {
            var country = service.GetCountryById(id);

            if (country == null)
            {
                return NotFound();
            }

            return View(country);
        }
        // POST: CountryController/Delete/5

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(Country model)
        {
            // 1. Получаем ID из модели, который пришел из скрытого поля формы
            int id = model.id;

            try
            {

                service.DeleteCountryById(model);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ошибка при удалении записи. Возможно, существуют связанные данные. " + ex.Message);


                var failedCountry = service.GetCountryById(id);
                return View(failedCountry ?? model);
            }
        }
    }
}