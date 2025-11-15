using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CodeFamily.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:dependency_type", "import,function_call,inheritance")
                .Annotation("Npgsql:Enum:repo_status", "queued,cloning,analyzing_dependencies,calculating_ownership,ready,error")
                .Annotation("Npgsql:Enum:review_state", "approved,changes_requested,commented,pending")
                .Annotation("Npgsql:Enum:webhook_status", "pending,processing,completed,failed");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GithubId = table.Column<long>(type: "bigint", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookQueue",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Payload = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookQueue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Repositories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GithubRepoId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    OwnerUsername = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConnectedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repositories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Repositories_Users_ConnectedByUserId",
                        column: x => x.ConnectedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Commits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Sha = table.Column<string>(type: "text", nullable: false),
                    AuthorName = table.Column<string>(type: "text", nullable: false),
                    AuthorEmail = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: true),
                    CommittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Commits_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PullRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GithubPrId = table.Column<long>(type: "bigint", nullable: false),
                    PrNumber = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MergedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RepositoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PullRequests_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PullRequests_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RepositoryCollaborators",
                columns: table => new
                {
                    RepositoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepositoryCollaborators", x => new { x.RepositoryId, x.UserId });
                    table.ForeignKey(
                        name: "FK_RepositoryCollaborators_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RepositoryCollaborators_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepositoryFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    TotalLines = table.Column<int>(type: "integer", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepositoryFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepositoryFiles_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PrId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_PullRequests_PrId",
                        column: x => x.PrId,
                        principalTable: "PullRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dependencies",
                columns: table => new
                {
                    DependencyType = table.Column<int>(type: "integer", nullable: false),
                    SourceFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Strength = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependencies", x => new { x.SourceFileId, x.TargetFileId, x.DependencyType });
                    table.ForeignKey(
                        name: "FK_Dependencies_RepositoryFiles_SourceFileId",
                        column: x => x.SourceFileId,
                        principalTable: "RepositoryFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dependencies_RepositoryFiles_TargetFileId",
                        column: x => x.TargetFileId,
                        principalTable: "RepositoryFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FileChanges",
                columns: table => new
                {
                    CommitId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Additions = table.Column<int>(type: "integer", nullable: false),
                    Deletions = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileChanges", x => new { x.CommitId, x.FileId });
                    table.ForeignKey(
                        name: "FK_FileChanges_Commits_CommitId",
                        column: x => x.CommitId,
                        principalTable: "Commits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileChanges_RepositoryFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "RepositoryFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileOwnerships",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileOwnerships", x => new { x.FileId, x.UserId });
                    table.ForeignKey(
                        name: "FK_FileOwnerships_RepositoryFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "RepositoryFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileOwnerships_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrFileChanges",
                columns: table => new
                {
                    PrId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrFileChanges", x => new { x.PrId, x.FileId });
                    table.ForeignKey(
                        name: "FK_PrFileChanges_PullRequests_PrId",
                        column: x => x.PrId,
                        principalTable: "PullRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrFileChanges_RepositoryFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "RepositoryFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Commits_RepositoryId",
                table: "Commits",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_TargetFileId",
                table: "Dependencies",
                column: "TargetFileId");

            migrationBuilder.CreateIndex(
                name: "IX_FileChanges_FileId",
                table: "FileChanges",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_FileOwnerships_UserId",
                table: "FileOwnerships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrFileChanges_FileId",
                table: "PrFileChanges",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequests_AuthorId",
                table: "PullRequests",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequests_RepositoryId",
                table: "PullRequests",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_ConnectedByUserId",
                table: "Repositories",
                column: "ConnectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryCollaborators_UserId",
                table: "RepositoryCollaborators",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryFiles_RepositoryId",
                table: "RepositoryFiles",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_PrId",
                table: "Reviews",
                column: "PrId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ReviewerId",
                table: "Reviews",
                column: "ReviewerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dependencies");

            migrationBuilder.DropTable(
                name: "FileChanges");

            migrationBuilder.DropTable(
                name: "FileOwnerships");

            migrationBuilder.DropTable(
                name: "PrFileChanges");

            migrationBuilder.DropTable(
                name: "RepositoryCollaborators");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "WebhookQueue");

            migrationBuilder.DropTable(
                name: "Commits");

            migrationBuilder.DropTable(
                name: "RepositoryFiles");

            migrationBuilder.DropTable(
                name: "PullRequests");

            migrationBuilder.DropTable(
                name: "Repositories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
