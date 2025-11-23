using ASPPorcelette.API.Models.Identity;
using ASPPorcelette.API.Models.Identity.Dto;
using ASPPorcelette.API.Services.Identity;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ASPPorcelette.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            var userStoreMock = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _mockTokenService = new Mock<ITokenService>();

            _authService = new AuthService(_mockUserManager.Object, _mockTokenService.Object);
        }

        // ✅ TEST 1 : Connexion réussie
        [Fact]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsValid()
        {
            var user = new User { Email = "test@test.com", UserName = "test@test.com", Statut = 1 };
            var loginDto = new LoginDto { Email = "test@test.com", Password = "Password123!" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Sensei" });
            _mockTokenService.Setup(x => x.CreateTokenAsync(user)).ReturnsAsync("fake-jwt-token");

            var result = await _authService.LoginAsync(loginDto);

            Assert.True(result.IsSuccess);
            Assert.Equal("test@test.com", result.Email);
            Assert.Contains("Sensei", result.Roles);
            Assert.Equal("fake-jwt-token", result.Token);
        }

        // ❌ TEST 2 : Utilisateur inexistant
        [Fact]
        public async Task LoginAsync_ShouldFail_WhenUserNotFound()
        {
            var loginDto = new LoginDto { Email = "unknown@test.com", Password = "whatever" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email)).ReturnsAsync((User)null);

            var result = await _authService.LoginAsync(loginDto);

            Assert.False(result.IsSuccess);
            Assert.Contains("Identifiants invalides.", result.Errors);
        }

        // ❌ TEST 3 : Mauvais mot de passe
        [Fact]
        public async Task LoginAsync_ShouldFail_WhenWrongPassword()
        {
            var user = new User { Email = "wrong@test.com", Statut = 1 };
            var loginDto = new LoginDto { Email = "wrong@test.com", Password = "badpass" };

            _mockUserManager.Setup(u => u.FindByEmailAsync(loginDto.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(false);

            var result = await _authService.LoginAsync(loginDto);

            Assert.False(result.IsSuccess);
            Assert.Contains("Identifiants invalides.", result.Errors);
        }

        // 🚫 TEST 4 : Compte désactivé
        [Fact]
        public async Task LoginAsync_ShouldFail_WhenUserInactive()
        {
            var inactiveUser = new User { Email = "inactive@test.com", Statut = 0 };
            var loginDto = new LoginDto { Email = "inactive@test.com", Password = "Password123!" };

            _mockUserManager.Setup(u => u.FindByEmailAsync(loginDto.Email))
                            .ReturnsAsync(inactiveUser);

            var result = await _authService.LoginAsync(loginDto);

            Assert.False(result.IsSuccess);
            Assert.Contains("Compte désactivé.", result.Errors);
        }
    }
}
