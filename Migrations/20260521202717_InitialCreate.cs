using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace meow.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Tytul = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Autor = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Gatunek = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RokWydania = table.Column<int>(type: "int", nullable: false),
                    IloscEgzemplarzy = table.Column<int>(type: "int", nullable: false),
                    Cena = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    IloscDoSprzedazy = table.Column<int>(type: "int", nullable: false),
                    Opis = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImageUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CenaOkladkowa = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    Wydawnictwo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LiczbaStron = table.Column<int>(type: "int", nullable: true),
                    OkladkaTyp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tlumaczenie = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EAN = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TytulOryginalny = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Seria = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JezykWydania = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JezykOryginalu = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NumerWydania = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataPremiery = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DataWydania = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    WysokoscMm = table.Column<int>(type: "int", nullable: true),
                    GlebokoscMm = table.Column<int>(type: "int", nullable: true),
                    SzerokoscMm = table.Column<int>(type: "int", nullable: true),
                    Indeks = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GpsrCertyfikaty = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Klienci",
                columns: table => new
                {
                    IdKlienta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Imie = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nazwisko = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefon = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Klienci", x => x.IdKlienta);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Egzemplarze",
                columns: table => new
                {
                    IdEgzemplarza = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdKsiazka = table.Column<int>(type: "int", nullable: false),
                    NumerInwentarzowy = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Stan = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Egzemplarze", x => x.IdEgzemplarza);
                    table.ForeignKey(
                        name: "FK_Egzemplarze_Books_IdKsiazka",
                        column: x => x.IdKsiazka,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Login = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Haslo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Rola = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KlientId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Klienci_KlientId",
                        column: x => x.KlientId,
                        principalTable: "Klienci",
                        principalColumn: "IdKlienta",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Zamowienia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdKlienta = table.Column<int>(type: "int", nullable: false),
                    DataZamowienia = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NumerSledzenia = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdKsiazki = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zamowienia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Zamowienia_Books_IdKsiazki",
                        column: x => x.IdKsiazki,
                        principalTable: "Books",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Zamowienia_Klienci_IdKlienta",
                        column: x => x.IdKlienta,
                        principalTable: "Klienci",
                        principalColumn: "IdKlienta",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Wypozyczenia",
                columns: table => new
                {
                    IdWypozyczenie = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdKlient = table.Column<int>(type: "int", nullable: false),
                    IdEgzemplarz = table.Column<int>(type: "int", nullable: true),
                    IdKsiazki = table.Column<int>(type: "int", nullable: true),
                    DataWypozyczenia = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataPlanowanegoZwrotu = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataZwrotu = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wypozyczenia", x => x.IdWypozyczenie);
                    table.ForeignKey(
                        name: "FK_Wypozyczenia_Books_IdKsiazki",
                        column: x => x.IdKsiazki,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Wypozyczenia_Egzemplarze_IdEgzemplarz",
                        column: x => x.IdEgzemplarz,
                        principalTable: "Egzemplarze",
                        principalColumn: "IdEgzemplarza");
                    table.ForeignKey(
                        name: "FK_Wypozyczenia_Klienci_IdKlient",
                        column: x => x.IdKlient,
                        principalTable: "Klienci",
                        principalColumn: "IdKlienta",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Platnosci",
                columns: table => new
                {
                    IdPlatnosc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdWypozyczenie = table.Column<int>(type: "int", nullable: false),
                    Kwota = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Platnosci", x => x.IdPlatnosc);
                    table.ForeignKey(
                        name: "FK_Platnosci_Wypozyczenia_IdWypozyczenie",
                        column: x => x.IdWypozyczenie,
                        principalTable: "Wypozyczenia",
                        principalColumn: "IdWypozyczenie",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Egzemplarze_IdKsiazka",
                table: "Egzemplarze",
                column: "IdKsiazka");

            migrationBuilder.CreateIndex(
                name: "IX_Platnosci_IdWypozyczenie",
                table: "Platnosci",
                column: "IdWypozyczenie",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_KlientId",
                table: "Users",
                column: "KlientId");

            migrationBuilder.CreateIndex(
                name: "IX_Wypozyczenia_IdEgzemplarz",
                table: "Wypozyczenia",
                column: "IdEgzemplarz");

            migrationBuilder.CreateIndex(
                name: "IX_Wypozyczenia_IdKlient",
                table: "Wypozyczenia",
                column: "IdKlient");

            migrationBuilder.CreateIndex(
                name: "IX_Wypozyczenia_IdKsiazki",
                table: "Wypozyczenia",
                column: "IdKsiazki");

            migrationBuilder.CreateIndex(
                name: "IX_Zamowienia_IdKlienta",
                table: "Zamowienia",
                column: "IdKlienta");

            migrationBuilder.CreateIndex(
                name: "IX_Zamowienia_IdKsiazki",
                table: "Zamowienia",
                column: "IdKsiazki");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Platnosci");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Zamowienia");

            migrationBuilder.DropTable(
                name: "Wypozyczenia");

            migrationBuilder.DropTable(
                name: "Egzemplarze");

            migrationBuilder.DropTable(
                name: "Klienci");

            migrationBuilder.DropTable(
                name: "Books");
        }
    }
}
