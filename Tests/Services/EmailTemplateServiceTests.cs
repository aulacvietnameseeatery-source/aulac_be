using Core.DTO.EmailTemplate;
using Core.Entity;
using Core.Interface.Repo;
using Core.Service;
using FluentAssertions;
using Moq;

namespace Tests.Services;

/// <summary>
/// Unit Test - EmailTemplateService
/// Code Module : Core/Service/EmailTemplateService.cs
/// Method      : GetByCodeAsync, GetAllAsync, CreateAsync, UpdateAsync, DeleteAsync
/// Created By  : Automation
/// Executed By : Test Runner
/// Test Req.   : Test mapping, create/update behavior, not-found handling, and delete passthrough.
/// </summary>
public class EmailTemplateServiceTests
{
    private readonly Mock<IEmailTemplateRepository> _repositoryMock = new();

    private EmailTemplateService CreateService() => new(_repositoryMock.Object);

    private static EmailTemplate MakeTemplate(
        long id = 1,
        string code = "RESERVATION_CONFIRM",
        string name = "Reservation Confirm",
        string subject = "Your reservation is confirmed",
        string bodyHtml = "<p>hello</p>",
        string? description = "desc") => new()
    {
        TemplateId = id,
        TemplateCode = code,
        TemplateName = name,
        Subject = subject,
        BodyHtml = bodyHtml,
        Description = description,
        CreatedAt = DateTime.UtcNow.AddDays(-1),
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetByCodeAsync")]
    public async Task GetByCodeAsync_WhenTemplateExists_ReturnsMappedDto()
    {
        var template = MakeTemplate();

        _repositoryMock
            .Setup(r => r.GetByCodeAsync("RESERVATION_CONFIRM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var service = CreateService();

        var result = await service.GetByCodeAsync("RESERVATION_CONFIRM", CancellationToken.None);

        result.Should().NotBeNull();
        result!.TemplateId.Should().Be(template.TemplateId);
        result.TemplateCode.Should().Be(template.TemplateCode);
        result.Subject.Should().Be(template.Subject);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetByCodeAsync")]
    public async Task GetByCodeAsync_WhenTemplateNotFound_ReturnsNull()
    {
        _repositoryMock
            .Setup(r => r.GetByCodeAsync("MISSING", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailTemplate?)null);

        var service = CreateService();

        var result = await service.GetByCodeAsync("MISSING", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAllAsync")]
    public async Task GetAllAsync_WhenTemplatesExist_ReturnsMappedList()
    {
        var templates = new List<EmailTemplate>
        {
            MakeTemplate(id: 1, code: "CODE1"),
            MakeTemplate(id: 2, code: "CODE2")
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        var service = CreateService();

        var result = await service.GetAllAsync(CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.TemplateCode).Should().BeEquivalentTo(["CODE1", "CODE2"]);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateAsync")]
    public async Task CreateAsync_WhenValidRequest_CreatesTemplateAndReturnsDto()
    {
        var request = new CreateEmailTemplateRequest
        {
            TemplateCode = "NEW_CODE",
            TemplateName = "New Template",
            Subject = "Subject",
            BodyHtml = "<p>body</p>",
            Description = "description"
        };

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<EmailTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailTemplate template, CancellationToken _) =>
            {
                template.TemplateId = 10;
                template.CreatedAt = new DateTime(2026, 1, 1);
                return template;
            });

        var service = CreateService();

        var result = await service.CreateAsync(request, CancellationToken.None);

        result.TemplateId.Should().Be(10);
        result.TemplateCode.Should().Be("NEW_CODE");
        result.TemplateName.Should().Be("New Template");
        result.Subject.Should().Be("Subject");

        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<EmailTemplate>(t =>
                t.TemplateCode == "NEW_CODE" &&
                t.TemplateName == "New Template" &&
                t.Subject == "Subject" &&
                t.BodyHtml == "<p>body</p>" &&
                t.Description == "description"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateAsync")]
    public async Task UpdateAsync_WhenTemplateExists_UpdatesAndReturnsDto()
    {
        var template = MakeTemplate(id: 7, code: "OLD_CODE", name: "Old Name");
        var request = new UpdateEmailTemplateRequest
        {
            TemplateName = "Updated Name",
            Subject = "Updated Subject",
            BodyHtml = "<p>updated</p>",
            Description = "updated desc"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<EmailTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        var result = await service.UpdateAsync(7, request, CancellationToken.None);

        result.TemplateName.Should().Be("Updated Name");
        result.Subject.Should().Be("Updated Subject");
        result.BodyHtml.Should().Be("<p>updated</p>");

        _repositoryMock.Verify(r => r.UpdateAsync(
            It.Is<EmailTemplate>(t =>
                t.TemplateId == 7 &&
                t.TemplateName == "Updated Name" &&
                t.Subject == "Updated Subject" &&
                t.BodyHtml == "<p>updated</p>" &&
                t.Description == "updated desc"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateAsync")]
    public async Task UpdateAsync_WhenTemplateNotFound_ThrowsKeyNotFoundException()
    {
        _repositoryMock
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailTemplate?)null);

        var service = CreateService();

        await service.Invoking(s => s.UpdateAsync(99, new UpdateEmailTemplateRequest
        {
            TemplateName = "Name",
            Subject = "Sub",
            BodyHtml = "<p>body</p>"
        }, CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Template ID 99 not found.*");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "DeleteAsync")]
    public async Task DeleteAsync_WhenRepositoryReturnsTrue_ReturnsTrue()
    {
        _repositoryMock
            .Setup(r => r.DeleteAsync(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        var result = await service.DeleteAsync(15, CancellationToken.None);

        result.Should().BeTrue();
    }
}
