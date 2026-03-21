using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infa.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "EmailTemplates",
                columns: new[] { "TemplateId", "BodyHtml", "CreatedAt", "Description", "Subject", "TemplateCode", "TemplateName" },
                values: new object[,]
                {
                    { 1L, "<!DOCTYPE html>\r\n<html>\r\n<head>\r\n    <meta charset=\"utf-8\">\r\n    <style>\r\n        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }\r\n        .container { max-width: 600px; margin: 0 auto; padding: 20px; }\r\n        .button { \r\n            display: inline-block; \r\n            padding: 12px 24px; \r\n            background-color: #007bff; \r\n            color: #ffffff; \r\n            text-decoration: none; \r\n            border-radius: 4px; \r\n            margin: 20px 0;\r\n        }\r\n        .warning { color: #856404; background-color: #fff3cd; padding: 10px; border-radius: 4px; }\r\n    </style>\r\n</head>\r\n<body>\r\n    <div class=\"container\">\r\n        <h2>Password Reset Request</h2>\r\n        <p>Hello {{username}},</p>\r\n        <p>We received a request to reset your password. Click the button below to create a new password:</p>\r\n        <a href=\"{{resetLink}}\" class=\"button\">Reset Password</a>\r\n        <p>Or copy and paste this link into your browser:</p>\r\n        <p><a href=\"{{resetLink}}\">{{resetLink}}</a></p>\r\n        <div class=\"warning\">\r\n            <strong>Security Notice:</strong>\r\n            <ul>\r\n                <li>This link will expire in {{expiryMinutes}} minutes</li>\r\n                <li>If you didn't request a password reset, you can safely ignore this email</li>\r\n                <li>Never share this link with anyone</li>\r\n            </ul>\r\n        </div>\r\n        <p>Best regards,<br>Your Application Team</p>\r\n    </div>\r\n</body>\r\n</html>", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Email sent when a user forgets their password.", "Password Reset Request", "FORGOT_PASSWORD", "Forgot Password" },
                    { 2L, "<!DOCTYPE html>\r\n<html>\r\n<head>\r\n    <meta charset=\"utf-8\">\r\n    <style>\r\n        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }\r\n        .container { max-width: 600px; margin: 0 auto; padding: 20px; }\r\n        .credentials { background-color: #f8f9fa; padding: 15px; border-left: 4px solid #007bff; margin: 20px 0; }\r\n        .warning { color: #856404; background-color: #fff3cd; padding: 10px; border-radius: 4px; margin: 20px 0; }\r\n        .code { font-family: 'Courier New', monospace; font-size: 16px; font-weight: bold; color: #007bff; }\r\n    </style>\r\n</head>\r\n<body>\r\n    <div class=\"container\">\r\n        <h2>Welcome! Your Account Has Been Created</h2>\r\n        <p>Hello {{fullName}},</p>\r\n        <p>Your account has been successfully created. Here are your login credentials:</p>\r\n        <div class=\"credentials\">\r\n            <p><strong>Username:</strong> <span class=\"code\">{{username}}</span></p>\r\n            <p><strong>Temporary Password:</strong> <span class=\"code\">{{temporaryPassword}}</span></p>\r\n        </div>\r\n        <div class=\"warning\">\r\n            <strong>Important Security Notice:</strong>\r\n            <ul>\r\n                <li>This is a temporary password that must be changed on your first login</li>\r\n                <li>Your account is currently locked and will be activated after you change your password</li>\r\n                <li>Never share your password with anyone</li>\r\n            </ul>\r\n        </div>\r\n        <p>Best regards,<br>Restaurant Management Team</p>\r\n    </div>\r\n</body>\r\n</html>", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Email sent to new staff members with their temporary credentials.", "Welcome! Your Account Has Been Created", "ACCOUNT_CREATED", "Account Created" },
                    { 3L, "<!DOCTYPE html>\r\n<html>\r\n<head>\r\n    <meta charset=\"utf-8\">\r\n    <style>\r\n        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }\r\n        .container { max-width: 600px; margin: 0 auto; padding: 20px; }\r\n        .details { background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0; border: 1px solid #dee2e6; }\r\n        .highlight { color: #d97706; font-weight: bold; }\r\n        .footer { margin-top: 30px; font-size: 12px; color: #666; }\r\n    </style>\r\n</head>\r\n<body>\r\n    <div class=\"container\">\r\n        <h2 style=\"color: #1A3A51;\">Reservation Confirmation</h2>\r\n        <p>Hello <span class=\"highlight\">{{CustomerName}}</span>,</p>\r\n        <p>Thank you for choosing An Lac Restaurant. We are pleased to confirm your reservation:</p>\r\n        <div class=\"details\">\r\n            <p><strong>Reservation ID:</strong> #{{ReservationId}}</p>\r\n            <p><strong>Date & Time:</strong> {{ReservedTime}}</p>\r\n            <p><strong>Party Size:</strong> {{PartySize}} people</p>\r\n            <p><strong>Table(s):</strong> {{TableCodes}}</p>\r\n            <p><strong>Zone:</strong> {{Zone}}</p>\r\n        </div>\r\n        <p>If you need to change or cancel your reservation, please contact us at least 2 hours in advance.</p>\r\n        <p>We look forward to serving you!</p>\r\n        <div class=\"footer\">\r\n            <p>An Lac Restaurant<br>123 Restaurant Street, City<br>Phone: (+84) 123-456-789</p>\r\n        </div>\r\n    </div>\r\n</body>\r\n</html>", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Email sent to customers after a successful online reservation.", "Reservation Confirmed - An Lac Restaurant", "RESERVATION_CONFIRM", "Reservation Confirmation" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EmailTemplates",
                keyColumn: "TemplateId",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "EmailTemplates",
                keyColumn: "TemplateId",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                table: "EmailTemplates",
                keyColumn: "TemplateId",
                keyValue: 3L);
        }
    }
}
