﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyCourse.Models.Enums;
using MyCourse.Models.ValueTypes;

namespace MyCourse.Models.Entities
{
    [Table("Courses")]
    public partial class Course
    {
        // authorId per la relazione utente registrato e autore del corso.
        public Course(string title, string author, string authorId)
        {
            ChangeTitle(title);
            ChangeAuthor(author, authorId);
            ChangeStatus(CourseStatus.Draft);
            Lessons = new HashSet<Lesson>();
            CurrentPrice = new Money(Currency.EUR, 0);
            FullPrice = new Money(Currency.EUR, 0);
            ImagePath = "/Courses/default.png";
        }

        [Key]
        public int Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string ImagePath { get; private set; }
        public string Author { get; private set; }
        public string Email { get; private set; }
        public double Rating { get; private set; }
        public Money FullPrice { get; private set; }
        public Money CurrentPrice { get; private set; }
        public string RowVersion { get; private set; }
        public CourseStatus Status { get; private set; }
        public string AuthorId { get; set; }
        public virtual ApplicationUser AuthorUser { get; set; }
        public virtual ICollection<Lesson> Lessons { get; private set; }
        // Relazione molti a molti tra utenti e corsi. Proprietà di navigazione per mappare corsi e utenti.
        public virtual ICollection<ApplicationUser> SubscribedUsers { get; private set; }

        public void ChangeStatus(CourseStatus newStatus)
        {
            //TODO: logica di validazione
            Status = newStatus;
        }

        public void ChangeAuthor(string newAuthor, string newAuthorId) 
        {
            if (string.IsNullOrWhiteSpace(newAuthor))
            {
                throw new ArgumentException("The author must have a name");
            }
            if (string.IsNullOrWhiteSpace(newAuthorId))
            {
                throw new ArgumentException("The author must have a id");
            }
            Author = newAuthor;
            AuthorId = newAuthorId;
        }

        public void ChangeTitle(string newTitle) 
        {
            if (string.IsNullOrWhiteSpace(newTitle))
            {
                throw new ArgumentException("The course must have a title");
            }
            Title = newTitle;
        }

        public void ChangePrices(Money newFullPrice, Money newDiscountPrice)
        {
            if (newFullPrice == null || newDiscountPrice == null)
            {
                throw new ArgumentException("Prices can't be null");
            }
            if (newFullPrice.Currency != newDiscountPrice.Currency) 
            {
                throw new ArgumentException("Currencies don't match");
            }
            if (newFullPrice.Amount < newDiscountPrice.Amount) 
            {
                throw new ArgumentException("Full price can't be less than the current price");
            }
            FullPrice = newFullPrice;
            CurrentPrice = newDiscountPrice;
        }

        public void ChangeEmail(string newEmail)
        {
            if (string.IsNullOrEmpty(newEmail))
            {
                throw new ArgumentException("Email can't be empty");
            }
            Email = newEmail;
        }

        public void ChangeDescription(string newDescription)
        {
            if (newDescription != null)
            {
                if (newDescription.Length < 20)
                {
                    throw new Exception("Description is too short");
                }
                else if (newDescription.Length > 4000)
                {
                    throw new Exception("Description is too long");
                }
            }
            Description = newDescription;
        }

        public void ChangeImagePath(string imagePath)
        {
            ImagePath = imagePath;
        }

    }
}
