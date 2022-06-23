﻿using Portfolio.Models;

namespace Portfolio.ViewModels;

public class CommentViewModel
{
    public IEnumerable<Comment> Comments { get; set; } = default!;
    public bool IsModerated { get; set; }
    public bool IsDeleted { get; set; }
}