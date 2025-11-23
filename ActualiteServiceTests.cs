using ASPPorcelette.API.Data;
using ASPPorcelette.API.DTOs.Evenement;
using ASPPorcelette.API.Models;
using ASPPorcelette.API.Models.Identity;
using ASPPorcelette.API.Repository.Interfaces;
using ASPPorcelette.API.Services.Implementation;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System.Threading.Tasks;
using System;
using System.Reflection;



public class ActualiteServiceTests
{
    private readonly ActualiteService _actualiteService;
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<UserManager<User>> _mockUserManager;

    public ActualiteServiceTests()
    {
        // --- 1️⃣ Création DbContext en mémoire ---
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        // --- 2️⃣ Mapper mock ---
        _mockMapper = new Mock<IMapper>();

        // --- 3️⃣ UserManager mock ---
        var store = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            store.Object, null, null, null, null, null, null, null, null
        );

        // --- 4️⃣ Service ---
        var mockRepo = new Mock<IActualiteRepository>(); // tu peux mocker si tu veux des tests sur repository
        _actualiteService = new ActualiteService(
            mockRepo.Object,
            _mockMapper.Object,
            _mockUserManager.Object,
            _dbContext
        );
        _mockMapper.Setup(m => m.Map(It.IsAny<ActualiteUpdateDto>(), It.IsAny<Actualite>()))
    .Callback<ActualiteUpdateDto, Actualite>((src, dest) =>
    {
        dest.Titre = src.Titre;
        dest.Contenu = src.Contenu;
        dest.DateDePublication = src.DatePublication;
    });

    }

    // =============================
    // TEST 1 : Création d'une actualité
    // =============================
    [Fact]
    public async Task CreateActualiteAsync_ShouldReturnCreatedActualite_WhenSuccessful()
    {
        // --- Arrange ---
        var user = new User
        {
            Id = "user1",
            UserName = "testuser",
            Nom = "Dupont",
            Prenom = "Jean",
            Grade = "Sensei",
            CodePostal = "75000",
            RueEtNumero = "1 rue du Test",
            Ville = "Paris",
            Telephone = "0102030405",
            PhotoUrl = "/images/users/default.png"
        };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var dto = new ActualiteCreateDto
        {
            Titre = "Nouvelle Actualité",
            Contenu = "Contenu de l'actualité",
            UserId = "user1",
            EvenementId = null,
            ImageUrl = null
        };

        _mockUserManager.Setup(u => u.FindByIdAsync("user1")).ReturnsAsync(user);

        // --- Act ---
        var result = await _actualiteService.CreateAsync(dto);

        // --- Assert ---
        Assert.NotNull(result);
        Assert.Equal("Nouvelle Actualité", result.Titre);
        Assert.Equal("user1", result.UserId);
    }

    // =============================
    // TEST 2 : Récupération d'une actualité
    // =============================
    [Fact]

    public async Task GetByIdAsync_ShouldReturnActualite_WhenExists()
    {
        // --- Arrange : créer l'utilisateur obligatoire ---
        var user = new User
        {
            Id = "user1",
            UserName = "testuser",
            Nom = "Dupont",
            Prenom = "Jean",
            Grade = "Sensei",
            CodePostal = "75000",
            RueEtNumero = "1 rue du Test",
            Ville = "Paris",
            Telephone = "0102030405",
            PhotoUrl = "/images/users/default.png"
        };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // --- Ajouter l'actualité ---
        var actualite = new Actualite
        {
            Titre = "Test",
            Contenu = "Contenu",
            UserId = "user1",
            DateDePublication = DateTime.UtcNow
        };
        await _dbContext.Actualites.AddAsync(actualite);
        await _dbContext.SaveChangesAsync();

        // --- Act ---
        var result = await _actualiteService.GetByIdAsync(actualite.ActualiteId);

        // --- Assert ---
        Assert.NotNull(result);
        Assert.Equal("Test", result.Titre);
    }

    // =============================
    // TEST 3 : Modification d'une actualité
    // =============================
    [Fact]

    public async Task UpdateAsync_ShouldReturnTrue_WhenSuccessful()
    {
        // --- Arrange : Modifier l'actualité ---
        var actualite = new Actualite
        {
            Titre = "Test",
            Contenu = "Contenu",
            UserId = "user1",
            DateDePublication = DateTime.UtcNow

        };
        await _dbContext.Actualites.AddAsync(actualite);
        await _dbContext.SaveChangesAsync();

        var updatedActualite = new ActualiteUpdateDto
        {
            Titre = "Nouveau titre",
            Contenu = "Nouveau contenu",
            DatePublication = DateTime.UtcNow.AddDays(1),
            ImageFile = null,
            DeleteExistingImage = false,
            EvenementId = null
        };

        // Simuler le chemin racine du site (wwwroot)
        string fakeWebRootPath = Path.Combine(Path.GetTempPath(), "wwwroot");
        Directory.CreateDirectory(fakeWebRootPath);

        // --- Act ---
        var result = await _actualiteService.UpdateAsync(actualite.ActualiteId, updatedActualite, fakeWebRootPath);

        // --- Assert ---
        Assert.True(result);
        var updated = await _dbContext.Actualites.FindAsync(actualite.ActualiteId);
        Assert.NotNull(updated);
        Assert.Equal("Nouveau titre", updated.Titre);
        Assert.Equal("Nouveau contenu", updated.Contenu);
    }



    // =============================
    // TEST 4 : Suppression d'une actualité
    // =============================
    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var actualite = new Actualite
        {
            Titre = "À supprimer",
            Contenu = "Contenu",
            UserId = "user1",
            DateDePublication = DateTime.UtcNow
        };
        await _dbContext.Actualites.AddAsync(actualite);
        await _dbContext.SaveChangesAsync();

        // Mock repository pour DeleteAsync
        var mockRepo = new Mock<IActualiteRepository>();
        mockRepo.Setup(r => r.GetByIdWithDetailsAsync(actualite.ActualiteId))
                .ReturnsAsync(actualite);
        mockRepo.Setup(r => r.DeleteAsync(actualite.ActualiteId))
                .ReturnsAsync(true);

        var service = new ActualiteService(
            mockRepo.Object,
            _mockMapper.Object,
            _mockUserManager.Object,
            _dbContext
        );

        // Act
        var result = await service.DeleteAsync(actualite.ActualiteId, "wwwroot");

        // Assert
        Assert.True(result);
    }
}
