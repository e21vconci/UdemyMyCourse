﻿using FluentValidation;
using MyCourse.Models.InputModels.Courses;

namespace MyCourse.Models.Validators
{
    public class CourseCreateValidator : AbstractValidator<CourseCreateInputModel>
    {
        private const string ValidTitleExpression = @"^[\w\s\.']+$";

        public CourseCreateValidator()
        {
            RuleFor(model => model.Title)
                .NotEmpty().WithMessage("Il titolo è obbligatorio")
                .MinimumLength(10).WithMessage("Il titolo deve essere di almeno {MinLength} caratteri")
                .MaximumLength(100).WithMessage("Il titolo deve essere di al massimo {MaxLength} caratteri")
                .Matches(ValidTitleExpression).WithMessage("Titolo non valido")
                .Must(NotContainMyCourse).WithMessage("Il titolo non può contenere la parola 'MyCourse'")
                .Remote(url: "/Courses/IsTitleAvailable", additionalFields: "Id", errorText: "Il titolo già esiste");
        }

        private bool NotContainMyCourse(string title)
        {
            return !title.Contains("MyCourse");
        }
    }
}
