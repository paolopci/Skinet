using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAddressToInternationalModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Address_Street",
                table: "AspNetUsers",
                newName: "Address_AddressLine1");

            migrationBuilder.RenameColumn(
                name: "Address_State",
                table: "AspNetUsers",
                newName: "Address_Region");

            migrationBuilder.AddColumn<string>(
                name: "Address_AddressLine2",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_CountryCode",
                table: "AspNetUsers",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_FirstName",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_LastName",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address_AddressLine2",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Address_CountryCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Address_FirstName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Address_LastName",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "Address_Region",
                table: "AspNetUsers",
                newName: "Address_State");

            migrationBuilder.RenameColumn(
                name: "Address_AddressLine1",
                table: "AspNetUsers",
                newName: "Address_Street");
        }
    }
}
