﻿namespace ASP_201.Models.Forum
{
    public class ForumSectionViewModel
    {
        public String Title { get; set; } = null!;
        public String Description { get; set; } = null!;
        public String LogoUrl { get; set; } = null!;
        public String CreatedDtString { get; set; } = null!;


        // Author data
        public String AuthorName { get; set; } = null!;
        public String AuthorAvatarUrl { get; set; } = null!;

    }
}