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
using System.Runtime.CompilerServices;


public class EvenementServiceTests
{
    private readonly EvenementService _evenementService;
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<IEvenementRepository> _mockEvenementRepository;


    public EvenementServiceTests()
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
        var mockRepo = new Mock<IEvenementRepository>();
        _evenementService = new EvenementService(
            mockRepo.Object,
            _mockMapper.Object
        );
        _mockMapper.Setup(m => m.Map(It.IsAny<EvenementUpdateDto>(), It.IsAny<Evenement>())).Callback<EvenementUpdateDto, Evenement>((src, dest) =>
        {
            dest.Titre = src.Titre;
            dest.Description = src.Description;
            dest.DateDebut = src.DateDebut;
            dest.DateFin = src.DateFin;
            dest.Lieu = src.Lieu;
            dest.DisciplineId = src.DisciplineId;
            dest.ImageUrl = src.ImageUrl;
            dest.TypeEvenementId = src.TypeEvenementId;
        });
    }

    // =============================
    // TEST 1 : Lire un evenement
    // =============================
    [Fact]
    public async Task GetEvenementByIdAsync_EventExists_ReturnsEvent()
    {
        // Arrange
        var evenementId = 1;
        var expectedEvenement = new Evenement
        {
            EvenementId = evenementId,
            Titre = "Test Event",
            Description = "This is a test event.",
            DateDebut = DateTime.UtcNow,
            DateFin = DateTime.UtcNow.AddHours(2),
            Lieu = "Test Location",
            DisciplineId = 1,
            TypeEvenementId = 1,
            ImageUrl = "http://example.com/image.jpg",
        };

        var mockRepo = new Mock<IEvenementRepository>();
        mockRepo.Setup(repo => repo.GetEvenementWithDetailsAsync(evenementId)).ReturnsAsync(expectedEvenement);

        var evenementService = new EvenementService(mockRepo.Object, _mockMapper.Object);

        // Act
        var result = await evenementService.GetEvenementByIdAsync(evenementId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedEvenement.EvenementId, result.EvenementId);
        Assert.Equal(expectedEvenement.Titre, result.Titre);
        Assert.Equal(expectedEvenement.Description, result.Description);
        Assert.Equal(expectedEvenement.DateDebut, result.DateDebut);
        Assert.Equal(expectedEvenement.DateFin, result.DateFin);
        Assert.Equal(expectedEvenement.Lieu, result.Lieu);
        Assert.Equal(expectedEvenement.DisciplineId, result.DisciplineId);
        Assert.Equal(expectedEvenement.TypeEvenementId, result.TypeEvenementId);
    }

    // =============================
    // TEST 2 : Creer un evenement
    // =============================

    [Fact]
    public async Task CreateEvenementAsync_ValidDto_ReturnsCreatedEvenement()
    {
        // Arrange
        var createDto = new EvenementCreateDto
        {
            Titre = "New Event",
            Description = "This is a new event.",
            DateDebut = DateTime.UtcNow,
            DateFin = DateTime.UtcNow.AddHours(3),
            Lieu = "New Location",
            DisciplineId = 2,
            TypeEvenementId = 2,
            ImageUrl = "http://example.com/newimage.jpg",
        };

        var createdEvenement = new Evenement
        {
            EvenementId = 5,
            Titre = createDto.Titre,
            Description = createDto.Description,
            DateDebut = createDto.DateDebut,
            DateFin = createDto.DateFin,
            Lieu = createDto.Lieu,
            DisciplineId = createDto.DisciplineId,
            TypeEvenementId = createDto.TypeEvenementId,
            ImageUrl = createDto.ImageUrl
        };

        _mockMapper.Setup(m => m.Map<Evenement>(It.IsAny<EvenementCreateDto>()))
            .Returns<EvenementCreateDto>(dto => new Evenement
            {
                Titre = dto.Titre,
                Description = dto.Description,
                DateDebut = dto.DateDebut,
                DateFin = dto.DateFin,
                Lieu = dto.Lieu,
                DisciplineId = dto.DisciplineId,
                TypeEvenementId = dto.TypeEvenementId,
                ImageUrl = dto.ImageUrl
            });

        var mockRepo = new Mock<IEvenementRepository>();
        mockRepo.Setup(repo => repo.AddAsync(It.IsAny<Evenement>())).ReturnsAsync(createdEvenement);

        var evenementService = new EvenementService(mockRepo.Object, _mockMapper.Object);

        // Act
        var result = await evenementService.CreateEvenementAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdEvenement.EvenementId, result.EvenementId);
        Assert.Equal(createDto.Titre, result.Titre);
        Assert.Equal(createDto.Description, result.Description);
        Assert.Equal(createDto.DateDebut, result.DateDebut);
        Assert.Equal(createDto.DateFin, result.DateFin);
        Assert.Equal(createDto.Lieu, result.Lieu);
        Assert.Equal(createDto.DisciplineId, result.DisciplineId);
        Assert.Equal(createDto.TypeEvenementId, result.TypeEvenementId);
    }

    // =============================
    // TEST 3 : Modifier un evenement
    // =============================
    [Fact]
    public async Task UpdateEvenementAsync_EventExists_ReturnsTrue()
    {
        // Arrange
        var evenementId = 1;

        var existingEvenement = new Evenement
        {
            EvenementId = evenementId,
            Titre = "Old Event",
            Description = "Old description",
            Lieu = "Old location",
            DateDebut = DateTime.UtcNow,
            DateFin = DateTime.UtcNow.AddHours(2),
            DisciplineId = 1,
            TypeEvenementId = 1
        };

        var updateDto = new EvenementUpdateDto
        {
            Titre = "Updated Event",
            Description = "This is an updated event.",
            DateDebut = DateTime.UtcNow,
            DateFin = DateTime.UtcNow.AddHours(4),
            Lieu = "Updated Location",
            DisciplineId = 3,
            TypeEvenementId = 3,
            ImageUrl = "http://example.com/updatedimage.jpg",
        };
        var mockRepo = new Mock<IEvenementRepository>();
        mockRepo.Setup(repo => repo.GetByIdAsync(evenementId)).ReturnsAsync(existingEvenement);

        mockRepo.Setup(repo => repo.UpdateAsync(It.IsAny<Evenement>())).ReturnsAsync(true);

        _mockMapper.Setup(m => m.Map<Evenement>(It.IsAny<EvenementUpdateDto>()))
    .Returns((EvenementUpdateDto dto) => new Evenement
    {
        Titre = dto.Titre,
        Description = dto.Description,
        DateDebut = dto.DateDebut,
        DateFin = dto.DateFin,
        Lieu = dto.Lieu,
        DisciplineId = dto.DisciplineId,
        TypeEvenementId = dto.TypeEvenementId,
        ImageUrl = dto.ImageUrl
    });

        var evenementService = new EvenementService(mockRepo.Object, _mockMapper.Object);

        // Act
        var result = await evenementService.UpdateEvenementAsync(evenementId, updateDto);

        // Assert
        Assert.True(result);
        mockRepo.Verify(r => r.UpdateAsync(It.Is<Evenement>(e =>
            e.EvenementId == evenementId &&
            e.Titre == updateDto.Titre &&
            e.Description == updateDto.Description &&
            e.DateDebut == updateDto.DateDebut &&
            e.DateFin == updateDto.DateFin &&
            e.Lieu == updateDto.Lieu &&
            e.DisciplineId == updateDto.DisciplineId &&
            e.TypeEvenementId == updateDto.TypeEvenementId &&
            e.ImageUrl == updateDto.ImageUrl
        )), Times.Once);
        mockRepo.Verify(r => r.GetByIdAsync(evenementId), Times.Never);
    }

    // =============================
    // TEST 4 : Supprimer un evenement
    // =============================
    [Fact]
    public async Task DeleteEvenementAsync_EventExists_ReturnsTrue()
    {
        // Arrange
        var evenementId = 1;

        var mockRepo = new Mock<IEvenementRepository>();
        mockRepo.Setup(repo => repo.DeleteAsync(evenementId)).ReturnsAsync(true);

        var evenementService = new EvenementService(mockRepo.Object, _mockMapper.Object);
        // Act
        var result = await evenementService.DeleteEvenementAsync(evenementId);
        // Assert
        Assert.True(result);
        mockRepo.Verify(r => r.DeleteAsync(evenementId), Times.Once);
    }


}

