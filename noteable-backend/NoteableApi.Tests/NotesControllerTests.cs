using Microsoft.AspNetCore.Mvc;
using NoteableApi.Controllers;
using NoteableApi.Data;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using Moq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NoteableApi.Tests
{
    public class BasicControllerTests
    {
        [Fact]
        public void TestConnection_ShouldReturnOkResult()
        {
            // create whats needed for the controller
            // Use an in-memory DB 
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb" + Guid.NewGuid().ToString())
                .Options;
            
            var dbContext = new ApplicationDbContext(options);
            
            // controller constructor requirement
            var logger = new LoggerFactory().CreateLogger<NotesController>();
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            
            // Make the controller with our test database
            var controller = new NotesController(dbContext, mockEnvironment.Object, logger);
            
            // Run the tested method 
            var result = controller.TestConnection();
            
            // Make sure i get back an OK result (HTTP 200)
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetNotes_ReturnsEmptyList_WhenDatabaseEmpty()
        {
            // Set up test database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("EmptyNotesDb" + Guid.NewGuid().ToString())
                .Options;
            
            var dbContext = new ApplicationDbContext(options);
            var logger = new LoggerFactory().CreateLogger<NotesController>();
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            
            var controller = new NotesController(dbContext, mockEnvironment.Object, logger);
            
            // Call the GetNotes method
            var result = await controller.GetNotes();
            
            // Check for an OK result
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            // Check that the returned list is empty
            var notes = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Empty(notes);
        }
    }
}