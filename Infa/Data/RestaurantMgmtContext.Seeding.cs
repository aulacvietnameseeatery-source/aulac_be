using Core.Entity;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infa.Data;

public partial class RestaurantMgmtContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailTemplate>().HasData(
            new EmailTemplate
            {
                TemplateId = 1,
                TemplateCode = "FORGOT_PASSWORD",
                TemplateName = "Forgot Password",
                Subject = "Password Reset Request",
                BodyHtml = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .button { 
            display: inline-block; 
            padding: 12px 24px; 
            background-color: #007bff; 
            color: #ffffff; 
            text-decoration: none; 
            border-radius: 4px; 
            margin: 20px 0;
        }
        .warning { color: #856404; background-color: #fff3cd; padding: 10px; border-radius: 4px; }
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Password Reset Request</h2>
        <p>Hello {{username}},</p>
        <p>We received a request to reset your password. Click the button below to create a new password:</p>
        <a href=""{{resetLink}}"" class=""button"">Reset Password</a>
        <p>Or copy and paste this link into your browser:</p>
        <p><a href=""{{resetLink}}"">{{resetLink}}</a></p>
        <div class=""warning"">
            <strong>Security Notice:</strong>
            <ul>
                <li>This link will expire in {{expiryMinutes}} minutes</li>
                <li>If you didn't request a password reset, you can safely ignore this email</li>
                <li>Never share this link with anyone</li>
            </ul>
        </div>
        <p>Best regards,<br>Your Application Team</p>
    </div>
</body>
</html>",
                Description = "Email sent when a user forgets their password.",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new EmailTemplate
            {
                TemplateId = 2,
                TemplateCode = "ACCOUNT_CREATED",
                TemplateName = "Account Created",
                Subject = "Welcome! Your Account Has Been Created",
                BodyHtml = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .credentials { background-color: #f8f9fa; padding: 15px; border-left: 4px solid #007bff; margin: 20px 0; }
        .warning { color: #856404; background-color: #fff3cd; padding: 10px; border-radius: 4px; margin: 20px 0; }
        .code { font-family: 'Courier New', monospace; font-size: 16px; font-weight: bold; color: #007bff; }
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Welcome! Your Account Has Been Created</h2>
        <p>Hello {{fullName}},</p>
        <p>Your account has been successfully created. Here are your login credentials:</p>
        <div class=""credentials"">
            <p><strong>Username:</strong> <span class=""code"">{{username}}</span></p>
            <p><strong>Temporary Password:</strong> <span class=""code"">{{temporaryPassword}}</span></p>
        </div>
        <div class=""warning"">
            <strong>Important Security Notice:</strong>
            <ul>
                <li>This is a temporary password that must be changed on your first login</li>
                <li>Your account is currently locked and will be activated after you change your password</li>
                <li>Never share your password with anyone</li>
            </ul>
        </div>
        <p>Best regards,<br>Restaurant Management Team</p>
    </div>
</body>
</html>",
                Description = "Email sent to new staff members with their temporary credentials.",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new EmailTemplate
            {
                TemplateId = 3,
                TemplateCode = "RESERVATION_CONFIRM",
                TemplateName = "Reservation Confirmation",
                Subject = "Reservation Confirmed - An Lac Restaurant",
                BodyHtml = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .details { background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0; border: 1px solid #dee2e6; }
        .highlight { color: #d97706; font-weight: bold; }
        .footer { margin-top: 30px; font-size: 12px; color: #666; }
    </style>
</head>
<body>
    <div class=""container"">
        <h2 style=""color: #1A3A51;"">Reservation Confirmation</h2>
        <p>Hello <span class=""highlight"">{{CustomerName}}</span>,</p>
        <p>Thank you for choosing An Lac Restaurant. We are pleased to confirm your reservation:</p>
        <div class=""details"">
            <p><strong>Reservation ID:</strong> #{{ReservationId}}</p>
            <p><strong>Date & Time:</strong> {{ReservedTime}}</p>
            <p><strong>Party Size:</strong> {{PartySize}} people</p>
            <p><strong>Table(s):</strong> {{TableCodes}}</p>
            <p><strong>Zone:</strong> {{Zone}}</p>
        </div>
        <p>If you need to change or cancel your reservation, please contact us at least 2 hours in advance.</p>
        <p>We look forward to serving you!</p>
        <div class=""footer"">
            <p>An Lac Restaurant<br>123 Restaurant Street, City<br>Phone: (+84) 123-456-789</p>
        </div>
    </div>
</body>
</html>",
                Description = "Email sent to customers after a successful online reservation.",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
