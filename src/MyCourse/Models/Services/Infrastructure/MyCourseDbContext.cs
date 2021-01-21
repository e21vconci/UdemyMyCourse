using Microsoft.EntityFrameworkCore;
using MyCourse.Models.Entities;
using MyCourse.Models.Enums;

namespace MyCourse.Models.Services.Infrastructure
{
    public partial class MyCourseDbContext : DbContext
    {
        public MyCourseDbContext(DbContextOptions<MyCourseDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Course> Courses { get; set; }
        public virtual DbSet<Lesson> Lessons { get; set; }

        /* Viene eliminato quando si utilizza il service AddDbContextPool
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlite("Data Source=Data/MyCourse.db");
            }
        }*/

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.4-servicing-10062");

            modelBuilder.Entity<Course>(entity =>
            {
                entity.ToTable("Courses"); //Superfluo se la tabella si chiama come la proprietà che espone il DbSet 
                entity.HasKey(course => course.Id); //Superfluo se la proprietà si chiama Id oppure se si chiama CoursesId
                //entity.HasKey(course => new { course.Id, course.Author }); //Tutte le proprietà coinvolte nel vincolo di chiave primaria

                // mapping per univocità titolo corso
                entity.HasIndex(course => course.Title).IsUnique();
                // proprietà per il campo RowVersion utilizzato per verificare se è consentito modificare un corso
                entity.Property(course => course.RowVersion).IsRowVersion();
                // persistere il valore string dello status di un corso
                entity.Property(course => course.Status).HasConversion<string>();

                //Mapping per gli owned types
                entity.OwnsOne(course => course.CurrentPrice, builder => {
                    builder.Property(money => money.Currency)
                    .HasConversion<string>() // Perchè Currency è di tipo enum
                    .HasColumnName("CurrentPrice_Currency"); //Questo è superfluo perchè le nostre colonne seguono già la convenzione di nomi
                    
                    builder.Property(money => money.Amount)
                    .HasConversion<float>() //Mapping da decimal a float per il prezzo per EFCore 3.0. Questo indica al meccanismo delle migration che la colonna della tabella dovrà essere creata di tipo numerico
                    .HasColumnName("CurrentPrice_Amount"); //Questo è superfluo perchè le nostre colonne seguono già la convenzione di nomi
                });

                entity.OwnsOne(course => course.FullPrice, builder => {
                    builder.Property(money => money.Currency)
                        .HasConversion<string>();
                    builder.Property(money => money.Amount)
                        .HasConversion<float>(); //Questo indica al meccanismo delle migration che la colonna della tabella dovrà essere creata di tipo numerico
                });

                //Mapping per le relazioni
                entity.HasMany(course => course.Lessons) //entità principale Course
                      .WithOne(lesson => lesson.Course)
                      .HasForeignKey(lesson => lesson.CourseId); //Superflua se la proprietà si chiama CourseID

                //Global Query Filter
                entity.HasQueryFilter(course => course.Status != CourseStatus.Deleted);
                
                #region Mapping generato automaticamente dal tool di reverse engineering
                /*
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Author)
                    .IsRequired()
                    .HasColumnType("TEXT (100)");

                entity.Property(e => e.CurrentPriceAmount)
                    .IsRequired()
                    .HasColumnName("CurrentPrice_Amount")
                    .HasColumnType("NUMERIC")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CurrentPriceCurrency)
                    .IsRequired()
                    .HasColumnName("CurrentPrice_Currency")
                    .HasColumnType("TEXT (3)")
                    .HasDefaultValueSql("'EUR'");

                entity.Property(e => e.Description).HasColumnType("TEXT (10000)");

                entity.Property(e => e.Email).HasColumnType("TEXT (100)");

                entity.Property(e => e.FullPriceAmount)
                    .IsRequired()
                    .HasColumnName("FullPrice_Amount")
                    .HasColumnType("NUMERIC")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.FullPriceCurrency)
                    .IsRequired()
                    .HasColumnName("FullPrice_Currency")
                    .HasColumnType("TEXT (3)")
                    .HasDefaultValueSql("'EUR'");

                entity.Property(e => e.ImagePath).HasColumnType("TEXT (100)");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasColumnType("TEXT (100)");
                    */
                #endregion
            });

            modelBuilder.Entity<Lesson>(entity =>
            {
                entity.Property(lesson => lesson.RowVersion).IsRowVersion();
                entity.Property(lesson => lesson.Order).HasDefaultValue(1000).ValueGeneratedNever();

                entity.HasOne(lesson => lesson.Course)
                      .WithMany(course => course.Lessons); //basta definirlo solo una volta: o dal punto di vista dell'entità principale 
                      //o dal punto di vista dell'entità dipendente (questa è l'entità dipendente Lesson)
                
                #region Mapping generato automaticamente dal tool di reverse engineering
                /*
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Description).HasColumnType("TEXT (10000)");

                entity.Property(e => e.Duration)
                    .IsRequired()
                    .HasColumnType("TEXT (8)")
                    .HasDefaultValueSql("'00:00:00'");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasColumnType("TEXT (100)");

                entity.HasOne(d => d.Course)
                    .WithMany(p => p.Lessons)
                    .HasForeignKey(d => d.CourseId);
                */
                #endregion
            });
        }
    }
}
