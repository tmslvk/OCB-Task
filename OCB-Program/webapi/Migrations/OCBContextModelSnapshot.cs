﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using webapi.Context;

#nullable disable

namespace webapi.Migrations
{
    [DbContext(typeof(OCBContext))]
    partial class OCBContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("webapi.Model.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("webapi.Model.Document", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("BankName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("EndDate")
                        .HasColumnType("datetime2");

                    b.Property<int?>("ExcelFileId")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ExcelFileId")
                        .IsUnique()
                        .HasFilter("[ExcelFileId] IS NOT NULL");

                    b.ToTable("Documents");
                });

            modelBuilder.Entity("webapi.Model.ExcelFile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<byte[]>("FileContent")
                        .IsRequired()
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("Filename")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("UploadDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("ExcelFiles");
                });

            modelBuilder.Entity("webapi.Model.Outlay", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AccountId")
                        .HasColumnType("int");

                    b.Property<int>("CategoryId")
                        .HasColumnType("int");

                    b.Property<int>("DocumentId")
                        .HasColumnType("int");

                    b.Property<decimal>("ISaldoActive")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("ISaldoPassive")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("OSaldoActive")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("OSaldoPassive")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("TurnoverCredit")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("TurnoverDebet")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.HasIndex("DocumentId");

                    b.ToTable("Outlays");
                });

            modelBuilder.Entity("webapi.Model.Document", b =>
                {
                    b.HasOne("webapi.Model.ExcelFile", "ExcelFile")
                        .WithOne("Document")
                        .HasForeignKey("webapi.Model.Document", "ExcelFileId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("ExcelFile");
                });

            modelBuilder.Entity("webapi.Model.Outlay", b =>
                {
                    b.HasOne("webapi.Model.Category", "Category")
                        .WithMany("Outlays")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("webapi.Model.Document", "Document")
                        .WithMany("Outlays")
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Category");

                    b.Navigation("Document");
                });

            modelBuilder.Entity("webapi.Model.Category", b =>
                {
                    b.Navigation("Outlays");
                });

            modelBuilder.Entity("webapi.Model.Document", b =>
                {
                    b.Navigation("Outlays");
                });

            modelBuilder.Entity("webapi.Model.ExcelFile", b =>
                {
                    b.Navigation("Document");
                });
#pragma warning restore 612, 618
        }
    }
}
