using ASPPorcelette.API.DTOs.Horaire;
using ASPPorcelette.API.Models;
using ASPPorcelette.API.Repository.Interfaces;
using ASPPorcelette.API.Services.Implementation;
using AutoMapper;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

public class HoraireServiceTests
{
    private readonly HoraireService _horaireService;
    private readonly Mock<IHoraireRepository> _mockRepository = new Mock<IHoraireRepository>();
    private readonly Mock<IMapper> _mockMapper = new Mock<IMapper>();

    public HoraireServiceTests()
    {
        // Instanciation du service réel avec les mocks
        _horaireService = new HoraireService(_mockRepository.Object, _mockMapper.Object);
    }

    // --- Fonctions utilitaires pour créer des données de test ---

    // Simule l'entité de la base de données
    private Horaire GetSampleHoraire(int id, int coursId)
    {
        return new Horaire
        {
            HoraireId = id,
            Jour = (id % 2 == 0) ? "Mardi" : "Lundi",
            HeureDebut = new TimeSpan(18, 0, 0), // Utilisation de TimeSpan correcte
            HeureFin = new TimeSpan(19, 30, 0),
            CoursId = coursId
        };
    }

    // Simule le DTO de sortie du service (après mapping)
    private HoraireDto GetSampleHoraireDto(int id, int coursId)
    {
        return new HoraireDto
        {
            HoraireId = id,
            Jour = (id % 2 == 0) ? "Mardi" : "Lundi",
            HeureDebut = TimeSpan.Parse("18:00"), // Fixed: Parse string to TimeSpan
            HeureFin = TimeSpan.Parse("19:30"),   // Fixed: Parse string to TimeSpan
            CoursId = coursId
        };
    }

    // -------------------------------------------------------------
    // TEST 1 : GetAllHorairesAsync
    // -------------------------------------------------------------

    [Fact]
    public async Task GetAllHorairesAsync_HorairesExist_ReturnsMappedDtos()
    {
        // Arrange
        var horairesEntities = new List<Horaire>
    {
        GetSampleHoraire(1, 10),
        GetSampleHoraire(2, 11)
    };
        var horairesDtos = new List<HoraireDto>
    {
        GetSampleHoraireDto(1, 10),
        GetSampleHoraireDto(2, 11)
    };

        // 1. Simuler la récupération des entités par le Repository
        _mockRepository.Setup(repo => repo.GetAllWithCoursAsync())
                       .ReturnsAsync(horairesEntities);

        // 2. Simuler le mapping Entité[] -> DTO[]
        _mockMapper.Setup(m => m.Map<IEnumerable<HoraireDto>>(horairesEntities))
                   .Returns(horairesDtos);

        // Act
        var result = await _horaireService.GetAllHorairesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Equal(TimeSpan.Parse("18:00"), result.First().HeureDebut);  // Correction : Compare TimeSpan à TimeSpan

        // Vérifie les appels
        _mockRepository.Verify(repo => repo.GetAllWithCoursAsync(), Times.Once);
        _mockMapper.Verify(m => m.Map<IEnumerable<HoraireDto>>(horairesEntities), Times.Once);
    }


    [Fact]
    public async Task GetAllHorairesAsync_NoHorairesExist_ReturnsEmptyList()
    {
        // Arrange
        var emptyEntities = new List<Horaire>();
        var emptyDtos = new List<HoraireDto>();

        _mockRepository.Setup(repo => repo.GetAllWithCoursAsync())
                       .ReturnsAsync(emptyEntities);

        _mockMapper.Setup(m => m.Map<IEnumerable<HoraireDto>>(emptyEntities))
                   .Returns(emptyDtos);

        // Act
        var result = await _horaireService.GetAllHorairesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // -------------------------------------------------------------
    // TEST 2 : GetHoraireByIdAsync
    // -------------------------------------------------------------

    [Fact]
    public async Task GetHoraireByIdAsync_HoraireExists_ReturnsMappedDto()
    {
        // Arrange
        var horaireId = 5;
        var expectedEntity = GetSampleHoraire(horaireId, 10);
        var expectedDto = GetSampleHoraireDto(horaireId, 10);

        // 1. Simuler la récupération de l'entité
        _mockRepository.Setup(repo => repo.GetByIdWithCoursAsync(horaireId))
                       .ReturnsAsync(expectedEntity);

        // 2. Simuler le mapping Entité -> DTO
        _mockMapper.Setup(m => m.Map<HoraireDto>(expectedEntity))
                   .Returns(expectedDto);

        // Act
        var result = await _horaireService.GetHoraireByIdAsync(horaireId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(horaireId, result.HoraireId);

        _mockRepository.Verify(repo => repo.GetByIdWithCoursAsync(horaireId), Times.Once);
        _mockMapper.Verify(m => m.Map<HoraireDto>(expectedEntity), Times.Once);
    }

    [Fact]
    public async Task GetHoraireByIdAsync_HoraireDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentId = 99;

        // Simuler que le Repository retourne null
        // Remarque : Le type de retour du mock doit correspondre au type attendu (Horaire)
        _mockRepository.Setup(repo => repo.GetByIdWithCoursAsync(nonExistentId))
                       .ReturnsAsync((Horaire?)null);

        // Act
        var result = await _horaireService.GetHoraireByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);

        // Assurer que le mapper n'est jamais appelé si l'entité est null
        _mockMapper.Verify(m => m.Map<HoraireDto>(It.IsAny<Horaire>()), Times.Never);
        _mockRepository.Verify(repo => repo.GetByIdWithCoursAsync(nonExistentId), Times.Once);
    }
}
