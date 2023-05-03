namespace ASP_201.Models.Forum
{
    public class ForumIndexModel
    {
        public List<Data.Entity.Section> Sections { get; set; } = null!;
        public Boolean UserCanCreate { get; set; }

        // Дані від створення нової секції
        public String? CreateMessage { get; set; }
        public bool? IsMessagePositive { get; set; }
        public ForumSectionFormModel? FormModel { get; set; }
    }
}
