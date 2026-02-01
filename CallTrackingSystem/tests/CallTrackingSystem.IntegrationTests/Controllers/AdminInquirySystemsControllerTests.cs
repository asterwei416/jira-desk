using CallTrackingSystem.Core.Services;
using CallTrackingSystem.Infrastructure.Data;
using CallTrackingSystem.Infrastructure.Repositories;
using CallTrackingSystem.Web.Controllers;
using CallTrackingSystem.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CallTrackingSystem.IntegrationTests.Controllers;

/// <summary>
/// 詢問系統管理 API 整合測試
/// </summary>
public class AdminInquirySystemsControllerTests
{
    [Fact]
    public async Task CreateAndGetAll_ShouldReturnCreatedItem()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var repository = new InquirySystemRepository(context);
        var service = new InquirySystemService(repository);
        var controller = new AdminInquirySystemsController(service);

        var createResult = await controller.Create(new CreateInquirySystemRequest { Name = "新系統" });
        Assert.IsType<CreatedAtActionResult>(createResult);

        var listResult = await controller.GetAll(null);
        var okResult = Assert.IsType<OkObjectResult>(listResult);
        var items = Assert.IsAssignableFrom<IEnumerable<InquirySystemResponse>>(okResult.Value);

        Assert.Contains(items, x => x.Name == "新系統");
    }
}
