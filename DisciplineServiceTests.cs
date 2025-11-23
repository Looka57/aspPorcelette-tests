using ASPPorcelette.API.Models;
using ASPPorcelette.API.Repository.Interfaces;
using ASPPorcelette.API.Services.Implementation;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

public class DisciplineServiceTests
{
    private readonly DisciplineService _disciplineService;
    private readonly Mock<IDisciplineRepository> _mockRepository;

    public DisciplineServiceTests()
    {
        _mockRepository = new Mock<IDisciplineRepository>();
        _disciplineService = new DisciplineService(_mockRepository.Object);
    }

    // --- Fonctions utilitaires ---
    private Discipline GetSampleDiscipline(int id)
    {
        return new Discipline
        {
            DisciplineId = id,
            Nom = $"Discipline {id}",
            Description = $"Description pour discipline {id}"
        };
    }

    // -------------------------------------------------------------
    // TEST 1 : GetAllDisciplinesAsync (Lecture de tout)
    // -------------------------------------------------------------

    [Fact]
    public async Task GetAllDisciplinesAsync_DisciplinesExist_ReturnsAllDisciplines()
    {
        // Arrange
        var disciplineList = new List<Discipline>
        {
            GetSampleDiscipline(1),
            GetSampleDiscipline(2)
        };

        // Le service appelle _repository.GetAllAsync()
        _mockRepository.Setup(repo => repo.GetAllAsync())
                       .ReturnsAsync(disciplineList);

        // Act
        var result = await _disciplineService.GetAllDisciplinesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Equal("Discipline 1", result.First().Nom);
        _mockRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllDisciplinesAsync_NoDisciplinesExist_ReturnsEmptyList()
    {
        // Arrange
        var emptyList = new List<Discipline>();

        _mockRepository.Setup(repo => repo.GetAllAsync())
                       .ReturnsAsync(emptyList);

        // Act
        var result = await _disciplineService.GetAllDisciplinesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    // --------------------------------------------------------------------------------------
    // TEST 2 : GetDisciplineByIdAsync (Lecture par ID - Retourne Null si non trouvé)
    // --------------------------------------------------------------------------------------

    [Fact]
    public async Task GetDisciplineByIdAsync_DisciplineExists_ReturnsDiscipline()
    {
        // Arrange
        var disciplineId = 1;
        var expectedDiscipline = GetSampleDiscipline(disciplineId);

        // Le service appelle _repository.GetByIdAsync(id)
        _mockRepository.Setup(repo => repo.GetByIdAsync(disciplineId))
                       .ReturnsAsync(expectedDiscipline);

        // Act
        var result = await _disciplineService.GetDisciplineByIdAsync(disciplineId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(disciplineId, result.DisciplineId);
        _mockRepository.Verify(repo => repo.GetByIdAsync(disciplineId), Times.Once);
    }

    [Fact]
    public async Task GetDisciplineByIdAsync_DisciplineDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentId = 99;

        // Simuler que le Repository retourne null
        _mockRepository.Setup(repo => repo.GetByIdAsync(nonExistentId))
                       .ReturnsAsync((Discipline)null);

        // Act
        var result = await _disciplineService.GetDisciplineByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(repo => repo.GetByIdAsync(nonExistentId), Times.Once);
    }

    // --------------------------------------------------------------------------------------------------------
    // TEST 3 : GetByIdAsync (Lecture par ID - Lève une exception si non trouvé, comportement critique)
    // --------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_DisciplineExists_ReturnsDiscipline()
    {
        // Arrange
        var disciplineId = 1;
        var expectedDiscipline = GetSampleDiscipline(disciplineId);

        // Le service appelle _repository.GetByIdAsync(id)
        _mockRepository.Setup(repo => repo.GetByIdAsync(disciplineId))
                       .ReturnsAsync(expectedDiscipline);

        // Act
        var result = await _disciplineService.GetByIdAsync(disciplineId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(disciplineId, result.DisciplineId);
        _mockRepository.Verify(repo => repo.GetByIdAsync(disciplineId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_DisciplineDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        var nonExistentId = 99;

        // Simuler que le Repository retourne null
        _mockRepository.Setup(repo => repo.GetByIdAsync(nonExistentId))
                       .ReturnsAsync((Discipline)null);

        // Act & Assert
        // On vérifie que l'exécution de la méthode lève l'exception spécifique
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _disciplineService.GetByIdAsync(nonExistentId)
        );
        _mockRepository.Verify(repo => repo.GetByIdAsync(nonExistentId), Times.Once);
    }
}