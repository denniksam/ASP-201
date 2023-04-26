using System.Net;
using System.Net.Mail;
using System.Net.WebSockets;
using System.Runtime.Intrinsics.X86;

namespace ASP_201.Services.Email
{
    public class GmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GmailService> _logger;

        public GmailService(IConfiguration configuration, ILogger<GmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public bool Send(string mailTemplate, object model)
        {
            // Шукаємо шаблон листа
            String? template = null;
            String[] filenames = new String[]
            {
                mailTemplate,
                mailTemplate + ".html",
                "Services/Email/" + mailTemplate,
                "Services/Email/" + mailTemplate + ".html"
            };
            foreach (String filename in filenames)
            {
                if (System.IO.File.Exists(filename))
                {
                    template = System.IO.File.ReadAllText(filename);
                    break;
                }
            }
            if(template is null)
            {
                throw new ArgumentException($"Template '{mailTemplate}' not found");
            }
            // Перевіряємо поштову конфігурацію
            String? host = _configuration["Smtp:Gmail:Host"];
            if (host is null) throw new MissingFieldException("Missing configuration 'Smtp:Gmail:Host'");
            String? mailbox = _configuration["Smtp:Gmail:Email"];
            if (mailbox is null) throw new MissingFieldException("Missing configuration 'Smtp:Gmail:Email'");
            String? password = _configuration["Smtp:Gmail:Password"];
            if (password is null) throw new MissingFieldException("Missing configuration 'Smtp:Gmail:Password'");
            int port;
            try { port = Convert.ToInt32(_configuration["Smtp:Gmail:Port"]); }
            catch { throw new MissingFieldException("Missing or ivalid configuration 'Smtp:Gmail:Port'"); }
            bool ssl;
            try { ssl = Convert.ToBoolean(_configuration["Smtp:Gmail:Ssl"]); }
            catch { throw new MissingFieldException("Missing or ivalid configuration 'Smtp:Gmail:Ssl'"); }

            // заповнюємо шаблон - проходимо по властивостях моделі та замінюємо
            // їх значення у шаблоні за збігом імен
            String? userEmail = null;
            foreach(var prop in model.GetType().GetProperties())
            {
                if (prop.Name == "Email") userEmail = prop.GetValue(model)?.ToString();
                String placeholder = $"{{{{{prop.Name}}}}}";
                if(template.Contains(placeholder))
                {
                    template = template.Replace(placeholder, prop.GetValue(model)?.ToString() ?? "");
                }
            }
            if(userEmail is null)
            {
                throw new ArgumentException("No 'Email' property in model");
            }
            // TODO: перевірити залишок {{\w+}} плейсхолдерів у шаблоні

            using SmtpClient smtpClient = new(host, port)
            {
                EnableSsl = ssl,
                Credentials = new NetworkCredential(mailbox, password)
            };
            MailMessage mailMessage = new()
            {
                From = new MailAddress(mailbox),
                Subject = "ASP-201 Project",
                IsBodyHtml = true,
                Body = template
            };
            mailMessage.To.Add(userEmail);
            try
            {
                smtpClient.Send(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Send Email exception '{ex}'", ex.Message);
                return false;
            }            
        }
    }
}
