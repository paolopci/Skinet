-- Reset selettivo tabelle Identity (SQL Server)
-- ATTENZIONE: cancella utenti e ruoli

BEGIN TRAN;

DELETE FROM dbo.AspNetUserTokens;
DELETE FROM dbo.AspNetUserLogins;
DELETE FROM dbo.AspNetUserClaims;
DELETE FROM dbo.AspNetUserRoles;
DELETE FROM dbo.AspNetRoleClaims;
DELETE FROM dbo.AspNetUsers;
DELETE FROM dbo.AspNetRoles;

COMMIT;
