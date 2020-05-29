using System;
using System.Data;

namespace MyCourse.Models.ViewModels
{
    public class LessonViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TimeSpan Duration { get; set; }

        public static LessonViewModel FromDataRow(DataRow lessonRow)
        {
            var lessonViewModel = new LessonViewModel 
            {
                Title = Convert.ToString(lessonRow["Title"]),
                Duration = TimeSpan.Parse(Convert.ToString(lessonRow["Duration"]))
            }; 
            return lessonViewModel;
        }
    }
}