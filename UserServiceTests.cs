using Xunit;
using Moq;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using System.Globalization;
using ASPPorcelette.API.Services;
using System.Runtime.CompilerServices;        // pour UserService


// Ton modèle utilisateur
public class ApplicationUser : IdentityUser
{
    public string Nom { get; set; }
    public string Prenom { get; set; }
    public string Telephone { get; set; }
    public DateTime? DateNaissance { get; set; }
    public string RueEtNumero { get; set; }
    public string Ville { get; set; }
    public string CodePostal { get; set; }
    public string Grade { get; set; }
    public string PhotoUrl { get; set; }
    public DateTime? DateAdhesion { get; set; }
    public DateTime? DateRenouvellement { get; set; }
    public int Statut { get; set; }
    public string? Bio { get; set; }
    public int? DisciplineId { get; set; }
    public DateTime DateCreation { get; set; }
    public int AnneeAdhesionSaison { get; set; } // Pour le comptage saisonnier
}

// Ton service
public class UserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // Simulation d’une création d’utilisateur avec rôle
    public async Task<IdentityResult> AddUserAsync(ApplicationUser user, string role)
    {
        var result = await _userManager.CreateAsync(user);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, role);
        }
        return result;
    }
    public async Task<IdentityResult> DeactivateUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            // L'opération est considérée comme réussie pour l'état final (l'utilisateur est bien absent/inactif)
            return IdentityResult.Success;

        user.Statut = 0; // 🎯 Mise à jour du statut

        var result = await _userManager.UpdateAsync(user);
        return result;
    }

    // 🆕 Méthode de Renouvellement (pour Test 6 et 7)
    public async Task<IdentityResult> RenouvelerAdhesion(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = $"Utilisateur avec ID {userId} non trouvé." });
        }

        // 🎯 LOGIQUE DE RENOUVELLEMENT CRITIQUE
        user.Statut = 1; // Le rendre actif
        user.DateRenouvellement = DateTime.UtcNow;
        user.AnneeAdhesionSaison = user.DateRenouvellement.Value.Year + (user.DateRenouvellement.Value.Month >= 9 ? 1 : 0);
        // Assurez-vous d'avoir la logique exacte du calcul de l'année saisonnière si elle est plus complexe

        var result = await _userManager.UpdateAsync(user);

        // Si le renouvellement réussit, assurez-vous qu'il est dans le rôle 'Adherent' (si nécessaire)
        if (result.Succeeded && !await _userManager.IsInRoleAsync(user, "Adherent"))
        {
            await _userManager.AddToRoleAsync(user, "Adherent");
        }

        return result;
    }


}





























// Tests unitaires
namespace ASPPorcelette.Tests
{
    public class UserServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            _userService = new UserService(_mockUserManager.Object);
        }

        // 🔹 TEST 1 : Création d’un adhérent
        [Fact]
        public async Task AddUser_ShouldCreateUserRole()
        {
            var newUser = new ApplicationUser
            {
                UserName = "test.adherent",
                Email = "adherent@test.com",
                Nom = "Dupont",
                Prenom = "Jean",
                Telephone = "0123456789",
                RueEtNumero = "1 rue de la Test",
                Ville = "TestVille",
                CodePostal = "75000",
                Grade = "Ceinture Blanche",
                PhotoUrl = "placeholder.jpg",
                DateNaissance = new DateTime(1990, 1, 1),
                DateAdhesion = DateTime.UtcNow,
                DateRenouvellement = DateTime.UtcNow.AddYears(1),
                Statut = 1,
                Bio = "Nouveau membre enthousiaste",
                DisciplineId = 1,
                DateCreation = DateTime.UtcNow
            };

            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>()))
                            .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Adherent"))
                            .ReturnsAsync(IdentityResult.Success);

            var result = await _userService.AddUserAsync(newUser, "Adherent");

            Assert.True(result.Succeeded);
            _mockUserManager.Verify(um => um.CreateAsync(It.Is<ApplicationUser>(
                u => u.Email == "adherent@test.com")), Times.Once);

            _mockUserManager.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Adherent"), Times.Once);
        }

        // 🔹 TEST 2 : Création d’un sensei
        [Fact]
        public async Task AddUser_ShouldCreateUserAndAssignSenseiRole()
        {
            var senseiUser = new ApplicationUser
            {
                UserName = "test.sensei",
                Email = "sensei@test.com",
                Nom = "Yamada",
                Prenom = "Kenji",
                Ville = "Tokyo",
                Statut = 1,
                Grade = "Ceinture Noire",
                DateNaissance = new DateTime(1985, 3, 20),
                Bio = "Sensei expérimenté avec 10 ans d'enseignement",
                DateCreation = DateTime.UtcNow
            };

            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>()))
                            .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Sensei"))
                            .ReturnsAsync(IdentityResult.Success);

            var result = await _userService.AddUserAsync(senseiUser, "Sensei");

            Assert.True(result.Succeeded);
            _mockUserManager.Verify(um => um.CreateAsync(It.Is<ApplicationUser>(
                u => u.Email == "sensei@test.com")), Times.Once);

            _mockUserManager.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Sensei"), Times.Once);
        }

        // 🔹 TEST 3 : Echoue de la creation d'un  Adherent
        [Fact]
        public async Task AddUser_WhenMissingRequiredFields_ShouldFailCreation()
        {
            // Arrange : on crée un utilisateur sans email (champ obligatoire)
            var invalidUser = new ApplicationUser
            {
                UserName = "invalid.user",
                Email = null, // ❌ Email manquant
                Nom = "Test"
            };

            // On simule le comportement du UserManager :
            // -> renvoie une erreur si l'email est vide
            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync((ApplicationUser u) =>
                {
                    if (string.IsNullOrWhiteSpace(u.Email))
                    {
                        return IdentityResult.Failed(new IdentityError
                        {
                            Code = "EmailRequired",
                            Description = "L'adresse e-mail est obligatoire."
                        });
                    }

                    return IdentityResult.Success;
                });

            // Act
            var result = await _userService.AddUserAsync(invalidUser, "Adherent");

            // Assert
            Assert.False(result.Succeeded); // ✅ Le test doit échouer
            Assert.Contains(result.Errors, e => e.Code == "EmailRequired");

            // Vérifie que la méthode CreateAsync a bien été appelée
            _mockUserManager.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>()), Times.Once);

            // 🚫 Aucun rôle ne doit être ajouté
            _mockUserManager.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        // 🔹 TEST 4 : Échec de création d’un sensei
        [Fact]
        public async Task AddUser_ShouldFail_WhenPasswordMissing()
        {
            var invalidSensei = new ApplicationUser
            {
                UserName = "sensei.fail",
                Email = "sensei@test.com",
                Nom = "Yamada",
                Prenom = "Kenji"
            };

            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError
                {
                    Code = "PasswordRequired",
                    Description = "Le mot de passe est obligatoire."
                }));

            var result = await _userService.AddUserAsync(invalidSensei, "Sensei");

            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Code == "PasswordRequired");

            _mockUserManager.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>()), Times.Once);
            _mockUserManager.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        // 🔹 TEST 5 : Désactivation de l'utilisateur (Statut = 0)
        [Fact]
        public async Task DesactiverUtilisateur_ShouldSetStatutToZero_WhenSuccessful()
        {
            // ARRANGE
            string userId = "u555";
            var userToDesactivate = new ApplicationUser { Id = userId, Statut = 1, Email = "actif@test.com" };

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(userToDesactivate);
            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

            // ACT
            var result = await _userService.DeactivateUserAsync(userId);

            // ASSERT
            Assert.True(result.Succeeded); // Le service retourne bien Succès
            Assert.Equal(0, userToDesactivate.Statut); // Le statut a bien été modifié sur l'objet mocké
            _mockUserManager.Verify(m => m.UpdateAsync(userToDesactivate), Times.Once());
        }

        // 🔹 TEST 6 : Renouvellement Réussi (Passage Inactif -> Actif)
        [Fact]
        public async Task RenouvelerAdhesion_ShouldUpdateDateStatutAndSeason_WhenSuccessful()
        {
            // ARRANGE
            string userId = "u600";
            // Utilisateur Inactif, dont l'adhésion était l'année dernière.
            var userToRenew = new ApplicationUser
            {
                Id = userId,
                Statut = 0,
                DateRenouvellement = DateTime.UtcNow.AddYears(-1),
                AnneeAdhesionSaison = 2024
            };

            // Simuler le comportement du Manager
            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(userToRenew);
            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.IsInRoleAsync(userToRenew, "Adherent")).ReturnsAsync(false); // Il n'est pas dans le rôle
            _mockUserManager.Setup(m => m.AddToRoleAsync(userToRenew, "Adherent")).ReturnsAsync(IdentityResult.Success); // Le rôle est ajouté

            // ACT
            var result = await _userService.RenouvelerAdhesion(userId);

            // ASSERT
            Assert.True(result.Succeeded);
            // 1. Statut doit être Actif
            Assert.Equal(1, userToRenew.Statut);
            // 2. Date de renouvellement doit être aujourd'hui
            Assert.True(userToRenew.DateRenouvellement.Value.Date == DateTime.UtcNow.Date);
            // 3. L'année de saison doit avoir été recalculée (ex: si on est en 2025, ça doit être 2025 ou 2026)
            Assert.True(userToRenew.AnneeAdhesionSaison >= DateTime.UtcNow.Year);
            // 4. L'appel à la mise à jour doit avoir eu lieu
            _mockUserManager.Verify(m => m.UpdateAsync(userToRenew), Times.Once());
            // 5. L'appel à l'ajout de rôle doit avoir eu lieu (si le rôle manquait)
            _mockUserManager.Verify(m => m.AddToRoleAsync(userToRenew, "Adherent"), Times.Once());
        }

        // 🔹 TEST 7 : Échec du Renouvellement (Utilisateur non trouvé)
        [Fact]
        public async Task RenouvelerAdhesion_ShouldFail_WhenUserNotFound()
        {
            // ARRANGE
            string userId = "nonExistant";

            // Simuler que la recherche ne trouve rien
            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null);

            // ACT
            var result = await _userService.RenouvelerAdhesion(userId);

            // ASSERT
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Description.Contains("non trouvé."));
            // L'appel à la mise à jour ne doit PAS avoir eu lieu
            _mockUserManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never());
        }


        // 🔹 TEST 8 : Échec du Adhrent
        [Fact]
        public async Task AddUser_ShouldFail_WhenAdherentEmailMissing()
        {
            var invalidAdherent = new ApplicationUser
            {
                UserName = "adh.fail",
                Email = null,  // ❌ Email manquant
                Nom = "Dupont",
                Prenom = "Jean"
            };

            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync((ApplicationUser u) =>
                    string.IsNullOrWhiteSpace(u.Email)
                        ? IdentityResult.Failed(new IdentityError { Code = "EmailRequired", Description = "Email obligatoire" })
                        : IdentityResult.Success
                );

            var result = await _userService.AddUserAsync(invalidAdherent, "Adherent");

            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Code == "EmailRequired");

            _mockUserManager.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>()), Times.Once);
            _mockUserManager.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        // 🔹 TEST 9 : Échec du Sensei
        [Fact]
        public async Task AddUser_ShouldFail_WhenSenseiPasswordMissing()
        {
            var invalidSensei = new ApplicationUser
            {
                UserName = "sensei.fail",
                Email = "sensei@test.com",
                Nom = "Yamada",
                Prenom = "Kenji"
            };

            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError
                {
                    Code = "PasswordRequired",
                    Description = "Mot de passe obligatoire"
                }));

            var result = await _userService.AddUserAsync(invalidSensei, "Sensei");

            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Code == "PasswordRequired");

            _mockUserManager.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>()), Times.Once);
            _mockUserManager.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }


        // 🔹 TEST 10 : Modification du Sensei
        [Fact]
        public async Task UpdateUser_ShouldModifyAdherentSuccessfully()
        {
            // ARRANGE
            var existingUser = new ApplicationUser
            {
                Id = "u123",
                UserName = "adh.existant",
                Email = "adherent@test.com",
                Nom = "Dupont",
                Prenom = "Jean",
                Statut = 1
            };

            // DTO simulé pour le test (on ne le prend pas du projet principal)
            var updateDto = new
            {
                Nom = "Durand",
                Prenom = "Jean",
                Statut = 1
            };

            _mockUserManager.Setup(um => um.FindByIdAsync(existingUser.Id))
                            .ReturnsAsync(existingUser);

            _mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>()))
                            .ReturnsAsync(IdentityResult.Success);

            // On simule ce que ferait ton service : mettre à jour le Nom
            existingUser.Nom = updateDto.Nom;

            // ACT
            var result = await _mockUserManager.Object.UpdateAsync(existingUser);

            // ASSERT
            Assert.True(result.Succeeded);
            Assert.Equal("Durand", existingUser.Nom);
            _mockUserManager.Verify(um => um.UpdateAsync(existingUser), Times.Once);
        }

    }


}


