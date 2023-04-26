namespace ASP_201.Services.Email
{
    public interface IEmailService
    {
        bool Send(String mailTemplate, object model);
    }
}
