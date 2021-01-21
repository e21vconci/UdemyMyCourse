using System;
using System.Data;
using MyCourse.Models.Entities;

namespace MyCourse.Models.ViewModels.Lessons
{
    public class LessonDetailViewModel : LessonViewModel
    {
        public int CourseId { get; set; }
        public string Description { get; set; }

        public static new LessonDetailViewModel FromDataRow(DataRow lessonRow)
        {
            var lessonDetailViewModel = new LessonDetailViewModel
            {
                Id = Convert.ToInt32(lessonRow["Id"]),
                CourseId = Convert.ToInt32(lessonRow["CourseId"]),
                Title = Convert.ToString(lessonRow["Title"]),
                Duration = TimeSpan.Parse(Convert.ToString(lessonRow["Duration"])),
                Description = Convert.ToString(lessonRow["Description"])
            };
            return lessonDetailViewModel;
        }

        public static new LessonDetailViewModel FromEntity(Lesson lesson)
        {
            return new LessonDetailViewModel
            {
                Id = lesson.Id,
                CourseId = lesson.CourseId,
                Title = lesson.Title,
                Duration = lesson.Duration,
                Description = lesson.Description
            };
        }
    }
}