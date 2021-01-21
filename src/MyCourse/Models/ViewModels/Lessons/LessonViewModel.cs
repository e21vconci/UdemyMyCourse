using System;
using System.Data;
using MyCourse.Models.Entities;

namespace MyCourse.Models.ViewModels.Lessons
{
    public class LessonViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public TimeSpan Duration { get; set; }

        public static LessonViewModel FromDataRow(DataRow lessonRow)
        {
            var lessonViewModel = new LessonViewModel
            {
                Id = Convert.ToInt32(lessonRow["Id"]),
                Title = Convert.ToString(lessonRow["Title"]),
                Duration = TimeSpan.Parse(Convert.ToString(lessonRow["Duration"]))
            };
            return lessonViewModel;
        }

        public static LessonViewModel FromEntity(Lesson lesson)
        {
            return new LessonViewModel
            {
                Id = lesson.Id,
                Title = lesson.Title,
                Duration = lesson.Duration
            };
        }
    }
}