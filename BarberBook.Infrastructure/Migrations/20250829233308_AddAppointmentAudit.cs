using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberBook.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "appointments",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "appointments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "appointments");
        }
    }
}
