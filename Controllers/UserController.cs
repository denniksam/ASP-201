using ASP_201.Data;
using ASP_201.Data.Entity;
using ASP_201.Models.User;
using ASP_201.Services.Hash;
using ASP_201.Services.Kdf;
using ASP_201.Services.Random;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Text.RegularExpressions;

namespace ASP_201.Controllers
{
    public class UserController : Controller
    {
        private readonly IHashService _hashService;
        private readonly ILogger<UserController> _logger;
        private readonly DataContext _dataContext;
        private readonly IRandomService _randomService;
        private readonly IKdfService _kdfService;

        public UserController(IHashService hashService, ILogger<UserController> logger, DataContext dataContext, IRandomService randomService, IKdfService kdfService)
        {
            _hashService = hashService;
            _logger = logger;
            _dataContext = dataContext;
            _randomService = randomService;
            _kdfService = kdfService;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Registration()
        {
            return View();
        }
        public IActionResult Register(RegistrationModel registrationModel)
        {
            bool isModelValid = true;
            byte minPasswordLength = 3;
            RegisterValidationModel registerValidation = new();

            #region Login Validation
            if (String.IsNullOrEmpty(registrationModel.Login))
            {
                registerValidation.LoginMessage = "Логін не може бути порожним";
                isModelValid = false;
            }
            #endregion

            #region Password / Repeat Validation
            if (String.IsNullOrEmpty(registrationModel.Password))
            {
                registerValidation.PasswordMessage = "Пароль не може бути порожним";
                registerValidation.RepeatPasswordMessage = "";
                isModelValid = false;
            }
            else if(registrationModel.Password.Length < minPasswordLength)
            {
                registerValidation.PasswordMessage = 
                    $"Пароль закороткий. Щонайменше символів - {minPasswordLength}";
                registerValidation.RepeatPasswordMessage = "";
                isModelValid = false;
            } 
            else if( ! registrationModel.Password.Equals(registrationModel.RepeatPassword))
            {
                registerValidation.PasswordMessage = 
                    registerValidation.RepeatPasswordMessage = "Паролі не збігаються";
                isModelValid = false;
            }
            #endregion

            #region Email Validation
            if (String.IsNullOrEmpty(registrationModel.Email))
            {
                registerValidation.EmailMessage = "Email не може бути порожним";
                isModelValid = false;
            }
            else
            {
                String emailRegex = @"^[\w.%+-]+@[\w.-]+\.[a-zA-Z]{2,}$";
                if( ! Regex.IsMatch(registrationModel.Email, emailRegex))
                {
                    registerValidation.EmailMessage = "Email не відповідає шаблону";
                    isModelValid = false;
                }
            }
            #endregion

            #region Real Name Validation
            if (String.IsNullOrEmpty(registrationModel.RealName))
            {
                registerValidation.RealNameMessage = "Ім'я не може бути порожним";
                isModelValid = false;
            }
            else
            {
                String nameRegex = @"^.+$";
                if (!Regex.IsMatch(registrationModel.RealName, nameRegex))
                {
                    registerValidation.RealNameMessage = "Ім'я не відповідає шаблону";
                    isModelValid = false;
                }
            }
            #endregion

            #region IsAgree Validation
            if(registrationModel.IsAgree == false)
            {
                registerValidation.IsAgreeMessage = "Для реєстрації слід прийняти правила сайту";
                isModelValid = false;
            }
            #endregion

            #region Avatar
            // будемо вважати аватар необов'язковим, обробляємо лише якщо він переданий
            String savedName = null!;
            if (registrationModel.Avatar is not null)  // є файл
            {
                // Генеруємо для файла нове ім'я, але зберігаємо розширення
                String ext = Path.GetExtension(registrationModel.Avatar.FileName);
                // TODO: перевірити розширення на перелік дозволених
                savedName = _hashService.Hash(
                    registrationModel.Avatar.FileName + DateTime.Now)[..16]
                    + ext;
                /* Д.З. Перед збереженням файлу пересвідчитись у тому, що
                 * згенероване ім'я не зайняте. Перевірку зробити циклічною
                 * на випадок повторних збігів перегенерованого імені.
                 */
                String path = "wwwroot/avatars/" + savedName;
                using FileStream fs = new(path,FileMode.Create);
                registrationModel.Avatar.CopyTo(fs);
                ViewData["savedName"] = savedName;
            }

            #endregion
            // якщо всі перевірки пройдені, то переходимо на нову сторінку з вітаннями
            if (isModelValid)
            {
                String salt = _randomService.RandomString(16);
                User user = new()
                {
                    Id = Guid.NewGuid(),
                    Login = registrationModel.Login,
                    RealName = registrationModel.RealName,
                    Email = registrationModel.Email,
                    EmailCode = _randomService.ConfirmCode(6),
                    PasswordSalt = salt,
                    PasswordHash = _kdfService.GetDerivedKey(registrationModel.Password, salt),
                    Avatar = savedName,
                    RegisterDt = DateTime.Now,
                    LastEnterDt = null
                };
                _dataContext.Users.Add(user);
                _dataContext.SaveChangesAsync();

                return View(registrationModel);
            }
            else  // не всі дані валідні - повертаємо на форму реєстрації
            {
                // передаємо дані щодо перевірок
                ViewData["registerValidationModel"] = registerValidation;
                // спосіб перейти на View з іншою назвою, ніж метод
                return View("Registration");
            }            
        }

        [HttpPost]   // метод доступний тільки для POST запитів
        public String AuthUser()
        {
            // альтернативний (до моделей) спосіб отримання параметрів запиту
            StringValues loginValues = Request.Form["user-login"];
            // колекція loginValues формується при будь-якому ключі, але для
            // неправильних (відсутніх) ключів вона порожня
            if( loginValues.Count == 0 )
            {
                // немає логіну у складі полів
                return "Missed required parameter: user-login";
            }
            String login = loginValues[0] ?? "";

            StringValues passValues = Request.Form["user-password"];
            if (passValues.Count == 0)
            {
                return "Missed required parameter: user-password";
            }
            String password = passValues[0] ?? "";

            // шукаємо користувача за логіном
            User? user = _dataContext.Users.Where(u => u.Login == login).FirstOrDefault();
            if(user is not null)
            {
                // якщо знайшли - перевіряємо пароль (derived key)
                if(user.PasswordHash == 
                    _kdfService.GetDerivedKey(password, user.PasswordSalt))
                {
                    // дані перевірені - користувач автентифікований - зберігаємо у сесії
                    HttpContext.Session.SetString("authUserId", user.Id.ToString());
                    return "OK";
                }
            }

            return "Авторизацію відхилено";
        }
    }
}
/* Д.З. Перенести алгоритм формування імені файла-аватара у 
 * RandomService. У іменах залишати тільки файл-безпечні символи.
 * Змінити коди формування імені файлу
 */
