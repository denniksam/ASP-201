using ASP_201.Data;
using ASP_201.Models.Forum;
using ASP_201.Services.Transliterate;
using ASP_201.Services.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;
using System.Security.Claims;

namespace ASP_201.Controllers
{
    public class ForumController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly ILogger<ForumController> _logger;
        private readonly IValidationService _validationService;
        private readonly ITransliterationService _transliterationService;

        public ForumController(DataContext dataContext, ILogger<ForumController> logger, IValidationService validationService, ITransliterationService transliterationService)
        {
            _dataContext = dataContext;
            _logger = logger;
            _validationService = validationService;
            _transliterationService = transliterationService;
        }

        private int _counter = 0;
        private int Counter { get => _counter++; set => _counter = value; }
        public IActionResult Index()
        {
            Counter = 0;
            ForumIndexModel model = new()
            {
                UserCanCreate = HttpContext.User.Identity?.IsAuthenticated == true,
                Sections = _dataContext
                    .Sections
                    .Include(s => s.Author)   // включити навігаційну властивість Author
                    .Include(s => s.RateList)
                    .Where(s => s.DeletedDt == null)
                    .OrderBy(s => s.CreatedDt)
                    .AsEnumerable()  // IQueriable -> IEnumerable
                    .Select(s => new ForumSectionViewModel()
                    {
                        Title = s.Title,
                        Description = s.Description,
                        LogoUrl = $"/img/logos/section{Counter}.png",
                        CreatedDtString = DateTime.Today == s.CreatedDt.Date
                            ? "Сьогодні " + s.CreatedDt.ToString("HH:mm")
                            : s.CreatedDt.ToString("dd.MM.yyyy HH:mm"),
                        UrlIdString = s.UrlId ?? s.Id.ToString(),
                        // AuthorName - RealName або Login в залежності від налагоджень 
                        AuthorName = s.Author.IsRealNamePublic 
                            ? s.Author.RealName 
                            : s.Author.Login,
                        AuthorAvatarUrl = s.Author.Avatar == null
                            ? "/avatars/no-avatar.png"
                            : $"/avatars/{s.Author.Avatar}",
                        // Rating data
                        LikesCount    = s.RateList.Count(r => r.Rating > 0),
                        DislikesCount = s.RateList.Count(r => r.Rating < 0),
                    })
                    .ToList()
            };

            if(HttpContext.Session.GetString("CreateSectionMessage") is String message)
            {
                model.CreateMessage = message;
                model.IsMessagePositive = HttpContext.Session.GetInt32("IsMessagePositive") == 1;
                if(model.IsMessagePositive == false)
                {
                    model.FormModel = new()
                    {
                        Title = HttpContext.Session.GetString("SavedTitle")!,
                        Description = HttpContext.Session.GetString("SavedDescription")!
                    };
                    HttpContext.Session.Remove("SavedTitle");
                    HttpContext.Session.Remove("SavedDescription");
                }
                HttpContext.Session.Remove("CreateSectionMessage");
                HttpContext.Session.Remove("IsMessagePositive");
            }

            return View(model);
        }
        public ViewResult Sections([FromRoute] String id)
        {
            Guid sectionId;
            try
            {
                sectionId = Guid.Parse(id);
            }
            catch
            {
                sectionId = _dataContext.Sections.First(s => s.UrlId == id).Id;
            }
            ForumSectionsModel model = new()
            {
                UserCanCreate = HttpContext.User.Identity?.IsAuthenticated == true,
                SectionId = sectionId.ToString(),
                Themes = _dataContext
                    .Themes
                    .Include(t => t.Author)
                    .Where(t => t.DeletedDt == null && t.SectionId == sectionId)
                    .Select(t => new ForumThemeViewModel()
                    {
                        Title = t.Title,
                        Description = t.Description,
                        CreatedDtString = DateTime.Today == t.CreatedDt.Date
                            ? "Сьогодні " + t.CreatedDt.ToString("HH:mm")
                            : t.CreatedDt.ToString("dd.MM.yyyy HH:mm"),
                        UrlIdString = t.Id.ToString(),
                        SectionId = t.SectionId.ToString(),
                        AuthorName = t.Author.IsRealNamePublic
                                        ? t.Author.RealName
                                        : t.Author.Login,
                        AuthorAvatarUrl = $"/avatars/{t.Author.Avatar ?? "no-avatar.png"}"
                    })
                    .ToList()
            };

            if (HttpContext.Session.GetString("CreateSectionMessage") is String message)
            {
                model.CreateMessage = message;
                model.IsMessagePositive = HttpContext.Session.GetInt32("IsMessagePositive") == 1;
                if (model.IsMessagePositive == false)
                {
                    model.FormModel = new()
                    {
                        Title = HttpContext.Session.GetString("SavedTitle")!,
                        Description = HttpContext.Session.GetString("SavedDescription")!
                    };
                    HttpContext.Session.Remove("SavedTitle");
                    HttpContext.Session.Remove("SavedDescription");
                }
                HttpContext.Session.Remove("CreateSectionMessage");
                HttpContext.Session.Remove("IsMessagePositive");
            }

            return View(model);
        }
        public IActionResult Themes([FromRoute] String id)
        {
            Guid themeId;
            try
            {
                themeId = Guid.Parse(id);
            }
            catch
            {
                themeId = Guid.Empty; // _dataContext.Themes.First(s => s.UrlId == id).Id;
            }
            var theme = _dataContext.Themes.Find(themeId);
            if (theme == null)
            {
                return NotFound();
            }
            ForumThemesModel model = new()
            {
                UserCanCreate = HttpContext.User.Identity?.IsAuthenticated == true,
                Title = theme.Title,
                ThemeId = id,
                Topics = _dataContext
                    .Topics
                    .Where(t => t.DeletedDt == null && t.ThemeId == themeId)
                    .AsEnumerable()
                    .Select(t => new ForumTopicViewModel()
                    {
                        Title = t.Title,
                        Description = t.Description.Length > 100 
                            ? t.Description[..100] + "..." 
                            : t.Description,
                        UrlIdString = t.Id.ToString(),
                        CreatedDtString = DateTime.Today == t.CreatedDt.Date
                            ? "Сьогодні " + t.CreatedDt.ToString("HH:mm")
                            : t.CreatedDt.ToString("dd.MM.yyyy HH:mm"),
                    })
                    .ToList()
            };
            /* Д.З. Стилізувати картку для Топіку: вивести у заголовку відомості
             про автора даного питання (за зразком картки Теми) */

            if (HttpContext.Session.GetString("CreateSectionMessage") is String message)
            {
                model.CreateMessage = message;
                model.IsMessagePositive = HttpContext.Session.GetInt32("IsMessagePositive") == 1;
                if (model.IsMessagePositive == false)
                {
                    model.FormModel = new()
                    {
                        Title = HttpContext.Session.GetString("SavedTitle")!,
                        Description = HttpContext.Session.GetString("SavedDescription")!
                    };
                    HttpContext.Session.Remove("SavedTitle");
                    HttpContext.Session.Remove("SavedDescription");
                }
                HttpContext.Session.Remove("CreateSectionMessage");
                HttpContext.Session.Remove("IsMessagePositive");
            }

            return View(model);
        }
        public IActionResult Topics([FromRoute] String id)
        {
            // ForumTopicsModel ForumPostFormModel ForumPostViewModel
            Guid topicId;
            try
            {
                topicId = Guid.Parse(id);
            }
            catch
            {
                topicId = Guid.Empty; // _dataContext.Themes.First(s => s.UrlId == id).Id;
            }
            var topic = _dataContext.Topics.Find(topicId);
            if (topic == null)
            {
                return NotFound();
            }
            ForumTopicsModel model = new()
            {
                UserCanCreate = HttpContext.User.Identity?.IsAuthenticated == true,
                Title = topic.Title,
                Description = topic.Description,
                TopicId = id,
                Posts = _dataContext
                    .Posts
                    .Include(p => p.Author)
                    .Include(p => p.Reply)
                    .Where(p => p.DeletedDt == null && p.TopicId == topicId)
                    .Select(p => new ForumPostViewModel
                    {
                        Content = p.Content,
                        AuthorName = p.Author.IsRealNamePublic ? p.Author.RealName : p.Author.Login,
                        AuthorAvatarUrl = $"/avatars/{p.Author.Avatar ?? "no-avatar.png"}"
                    })
                    .ToList()
            };

            if (HttpContext.Session.GetString("CreateSectionMessage") is String message)
            {
                model.CreateMessage = message;
                model.IsMessagePositive = HttpContext.Session.GetInt32("IsMessagePositive") == 1;
                if (model.IsMessagePositive == false)
                {
                    model.FormModel = new()
                    {
                        Content = HttpContext.Session.GetString("SavedContent")!,
                        ReplyId = HttpContext.Session.GetString("SavedReplyId")!
                    };
                    HttpContext.Session.Remove("SavedContent");
                    HttpContext.Session.Remove("SavedReplyId");
                }
                HttpContext.Session.Remove("CreateSectionMessage");
                HttpContext.Session.Remove("IsMessagePositive");
            }

            return View(model);
        }

        [HttpPost]
        public RedirectToActionResult CreateSection(ForumSectionFormModel formModel)
        {
            _logger.LogInformation("Title: {t}, Description: {d}",
                formModel.Title, formModel.Description);

            if( ! _validationService.Validate(formModel.Title, ValidationTerms.NotEmpty))
            {
                HttpContext.Session.SetString("CreateSectionMessage",
                    "Назва не може бути порожною");
                HttpContext.Session.SetInt32("IsMessagePositive", 0);
                HttpContext.Session.SetString("SavedTitle", formModel.Title ?? String.Empty);
                HttpContext.Session.SetString("SavedDescription", formModel.Description ?? String.Empty);
            }
            else if (!_validationService.Validate(formModel.Description, ValidationTerms.NotEmpty))
            {
                HttpContext.Session.SetString("CreateSectionMessage",
                    "Опис не може бути порожним");
                HttpContext.Session.SetInt32("IsMessagePositive", 0);
                HttpContext.Session.SetString("SavedTitle", formModel.Title ?? String.Empty);
                HttpContext.Session.SetString("SavedDescription", formModel.Description ?? String.Empty);
            }
            else
            {
                Guid userId;
                try
                {
                    userId = Guid.Parse(
                        HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid)?.Value
                    );
                    String trans = _transliterationService.Transliterate(formModel.Title);
                    String urlId = trans;
                    int n = 2;
                    while(_dataContext.Sections.Any(s => s.UrlId == urlId))
                    {
                        urlId = $"{trans}{n++}";
                    }
                    _dataContext.Sections.Add(new()
                    {
                        Id = Guid.NewGuid(),
                        Title = formModel.Title,
                        Description = formModel.Description,
                        CreatedDt = DateTime.Now,
                        AuthorId = userId,
                        UrlId = urlId
                    });
                    _dataContext.SaveChanges();
                    HttpContext.Session.SetString("CreateSectionMessage",
                        "Додано успішно");
                    HttpContext.Session.SetInt32("IsMessagePositive", 1);
                }
                catch
                {
                    HttpContext.Session.SetString("CreateSectionMessage",
                        "Відмовлено в авторизації");
                    HttpContext.Session.SetInt32("IsMessagePositive", 0);
                    HttpContext.Session.SetString("SavedTitle", formModel.Title ?? String.Empty);
                    HttpContext.Session.SetString("SavedDescription", formModel.Description ?? String.Empty);
                }                
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public RedirectToActionResult CreateTheme(ForumThemeFormModel formModel)
        {
            if (!_validationService.Validate(formModel.Title, ValidationTerms.NotEmpty))
            {
                HttpContext.Session.SetString("CreateSectionMessage",
                    "Назва не може бути порожною");
                HttpContext.Session.SetInt32("IsMessagePositive", 0);
                HttpContext.Session.SetString("SavedTitle", formModel.Title ?? String.Empty);
                HttpContext.Session.SetString("SavedDescription", formModel.Description ?? String.Empty);
            }
            else if (!_validationService.Validate(formModel.Description, ValidationTerms.NotEmpty))
            {
                HttpContext.Session.SetString("CreateSectionMessage",
                    "Опис не може бути порожним");
                HttpContext.Session.SetInt32("IsMessagePositive", 0);
                HttpContext.Session.SetString("SavedTitle", formModel.Title ?? String.Empty);
                HttpContext.Session.SetString("SavedDescription", formModel.Description ?? String.Empty);
            }
            else
            {
                Guid userId;
                try
                {
                    userId = Guid.Parse(
                        HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid)?.Value
                    );
                    _dataContext.Themes.Add(new()
                    {
                        Id = Guid.NewGuid(),
                        Title = formModel.Title,
                        Description = formModel.Description,
                        CreatedDt = DateTime.Now,
                        AuthorId = userId,
                        SectionId = Guid.Parse(formModel.SectionId)                        
                    });
                    _dataContext.SaveChanges();
                    
                    HttpContext.Session.SetString("CreateSectionMessage",
                        "Додано успішно");
                    HttpContext.Session.SetInt32("IsMessagePositive", 1);
                }
                catch
                {
                    HttpContext.Session.SetString("CreateSectionMessage",
                        "Відмовлено в авторизації");
                    HttpContext.Session.SetInt32("IsMessagePositive", 0);
                    HttpContext.Session.SetString("SavedTitle", formModel.Title ?? String.Empty);
                    HttpContext.Session.SetString("SavedDescription", formModel.Description ?? String.Empty);
                }
            }

            return RedirectToAction(
                nameof(Sections), 
                new { id = formModel.SectionId });  // TODO: id = UrlId ?? SectionId
        }

        [HttpPost]
        public RedirectToActionResult CreateTopic(ForumTopicFormModel formModel)
        {
            if (!_validationService.Validate(formModel.Title, ValidationTerms.NotEmpty))
            {
                HttpContext.Session.SetString("CreateSectionMessage",
                    "Назва не може бути порожною");
                HttpContext.Session.SetInt32("IsMessagePositive", 0);
                HttpContext.Session.SetString("SavedTitle", formModel.Title ?? String.Empty);
                HttpContext.Session.SetString("SavedDescription", formModel.Description ?? String.Empty);
            }
            else if (!_validationService.Validate(formModel.Description, ValidationTerms.NotEmpty))
            {
                HttpContext.Session.SetString("CreateSectionMessage",
                    "Опис не може бути порожним");
                HttpContext.Session.SetInt32("IsMessagePositive", 0);
                HttpContext.Session.SetString("SavedTitle", formModel.Title ?? String.Empty);
                HttpContext.Session.SetString("SavedDescription", formModel.Description ?? String.Empty);
            }
            else
            {
                Guid userId;
                try
                {
                    userId = Guid.Parse(
                        HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid)?.Value
                    );
                    _dataContext.Topics.Add(new()
                    {
                        Id = Guid.NewGuid(),
                        Title = formModel.Title,
                        Description = formModel.Description,
                        CreatedDt = DateTime.Now,
                        AuthorId = userId,
                        ThemeId = Guid.Parse(formModel.ThemeId)
                    });
                    _dataContext.SaveChanges();

                    HttpContext.Session.SetString("CreateSectionMessage",
                        "Додано успішно");
                    HttpContext.Session.SetInt32("IsMessagePositive", 1);
                }
                catch
                {
                    HttpContext.Session.SetString("CreateSectionMessage",
                        "Відмовлено в авторизації");
                    HttpContext.Session.SetInt32("IsMessagePositive", 0);
                    HttpContext.Session.SetString("SavedTitle", formModel.Title ?? String.Empty);
                    HttpContext.Session.SetString("SavedDescription", formModel.Description ?? String.Empty);
                }
            }
            return RedirectToAction(
                nameof(Themes),
                new { id = formModel.ThemeId });
        }

        [HttpPost]
        public RedirectToActionResult CreatePost(ForumPostFormModel formModel)
        {
            if (!_validationService.Validate(formModel.Content, ValidationTerms.NotEmpty))
            {
                HttpContext.Session.SetString("CreateSectionMessage",
                    "Відповідь не може бути порожною");
                HttpContext.Session.SetInt32("IsMessagePositive", 0);
                HttpContext.Session.SetString("SavedContent", formModel.Content ?? String.Empty);
                HttpContext.Session.SetString("SavedReplyId", formModel.ReplyId ?? String.Empty);
            }
            else
            {
                Guid userId;
                try
                {
                    userId = Guid.Parse(
                        HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid)?.Value
                    );
                    _dataContext.Posts.Add(new()
                    {
                        Id = Guid.NewGuid(),
                        Content = formModel.Content,
                        ReplyId = String.IsNullOrEmpty(formModel.ReplyId) 
                            ? null 
                            : Guid.Parse(formModel.ReplyId),
                        CreatedDt = DateTime.Now,
                        AuthorId = userId,
                        TopicId = Guid.Parse(formModel.TopicId)
                    });
                    _dataContext.SaveChanges();

                    HttpContext.Session.SetString("CreateSectionMessage",
                        "Додано успішно");
                    HttpContext.Session.SetInt32("IsMessagePositive", 1);
                }
                catch
                {
                    HttpContext.Session.SetString("CreateSectionMessage",
                        "Відмовлено в авторизації");
                    HttpContext.Session.SetInt32("IsMessagePositive", 0);
                    HttpContext.Session.SetString("SavedContent", formModel.Content ?? String.Empty);
                    HttpContext.Session.SetString("SavedReplyId", formModel.ReplyId ?? String.Empty);
                }
            }
            return RedirectToAction(
                nameof(Topics),
                new { id = formModel.TopicId });
        }
    }
}
/* Д.З. Реорганізувати код перевірок у CreateSection, уникнути дублювання коду
 * Додати всі відомості про розділи на домашній сторінці форуму
 * (дата створення, назва, опис, автор-ід), можна без
 * оформлення
 */
