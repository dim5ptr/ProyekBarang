﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarangAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserIdFromBarang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Barang");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Barang",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
