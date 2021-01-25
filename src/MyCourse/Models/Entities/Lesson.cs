using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyCourse.Models.Entities
{
    [Table("Lessons")]
    public partial class Lesson
    {
        public Lesson(string title, int courseId)
        {
            ChangeTitle(title);
            CourseId = courseId;
            Order = 1000;
            Duration = TimeSpan.FromSeconds(0);
        }

        [Key]
        public int Id { get; private set; }
        [ForeignKey(nameof(Course))]
        public int CourseId { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public TimeSpan Duration { get; private set; } //00:00:00
        public virtual Course Course { get; set; }
        public string RowVersion { get; private set; }
        public int Order { get; private set; }

        public void ChangeTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentException("A lesson must have a title");
            }
            Title = title;
        }

        public void ChangeDescription(string description)
        {
            Description = description;
        }

        public void ChangeDuration(TimeSpan duration)
        {
            Duration = duration;
        }

        public void ChangeOrder(int order)
        {
            Order = order;
        }
    }
}
