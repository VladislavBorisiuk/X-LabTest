using Microsoft.AspNetCore.Identity;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using X_LabDataBase.Entityes;
using X_LabTest.Models;
using Xunit;
using System;

namespace LabXUnitTest
{
    public class ResourceControllerTests
    {
        private readonly Mock<UserManager<Person>> _userManagerMock;

        public ResourceControllerTests()
        {
            var store = new Mock<IUserStore<Person>>();
            _userManagerMock = new Mock<UserManager<Person>>(
                store.Object, null, null, null, null, null, null, null, null);
        }

        [Fact]
        public async Task Register_ValidModel_ReturnsOk()
        {

            var model = new PersonDTO { Login = "testuser", Password = "Test@123" };
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<Person>(), It.IsAny<string>()))
                            .ReturnsAsync(IdentityResult.Success);

            var controller = new ResourceController(_userManagerMock.Object);


            var result = await controller.Register(model);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Register_InvalidModel_ReturnsBadRequest()
        {
            var model = new PersonDTO { Login = "testuser", Password = "Test@123" };
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<Person>(), It.IsAny<string>()))
                            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error" }));

            var controller = new ResourceController(_userManagerMock.Object);


            var result = await controller.Register(model);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void Get_ReturnsListOfUsers()
        {
            var users = new List<Person> { new Person { UserName = "testuser" } };
            _userManagerMock.Setup(x => x.Users)
                            .Returns(users.AsQueryable());

            var controller = new ResourceController(_userManagerMock.Object);

            var result = controller.Get();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("testuser", okResult.Value.ToString());
        }
    }

}