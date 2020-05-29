using System;
using System.Collections.Generic;

namespace MyCourse.Models.Entities
{
    public partial class Lesson
    {
        public int Id { get; private set; }
        public int CourseId { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public TimeSpan Duration { get; private set; } //00:00:00

        public virtual Course Course { get; set; }
    }
}
