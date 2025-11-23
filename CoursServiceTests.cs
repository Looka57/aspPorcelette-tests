using ASPPorcelette.API.DTOs.Cours;
using ASPPorcelette.API.Models;
using ASPPorcelette.API.Repository.Interfaces;
using ASPPorcelette.API.Services.Implementation;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CoursServiceTests
{
    private readonly CoursService _coursService;
    private readonly Mock<ICoursRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper;

    public CoursServiceTests()
    {
        _mockRepository = new Mock<ICoursRepository>();
        _mockMapper = new Mock<IMapper>();

        // Instanciation du service réel avec les mocks
        _coursService = new CoursService(_mockRepository.Object, _mockMapper.Object);

        // Configuration générale du mapping pour Update/Patch si nécessaire
        // (La configuration détaillée sera faite dans les tests spécifiques si besoin)
    }

    // --- Fonctions utilitaires ---

    private Cours GetSampleCours(int id)
    {
        // Utilisation de Libelle au lieu de Nom
        return new Cours { CoursId = id, Libelle = $"Cours {id}" };
        // J'ai retiré DureeMinutes
    }

    private CoursUpdateDto GetSampleCoursUpdateDto(int id)
    {
        // Utilisation de Libelle au lieu de Nom
        return new CoursUpdateDto { Libelle = $"Cours Update {id}" };
        // J'ai retiré DureeMinutes
    }

    // ---
    // ===================================
    // TEST 1 : Lire un Cours (GetByIdAsync)
    // ===================================
    [Fact]
    public async Task GetByIdAsync_CoursExists_ReturnsCoursWithDetails()
    {
        // Arrange
        var coursId = 1;
        var expectedCours = GetSampleCours(coursId);

        // Le service appelle _coursRepository.GetCoursWithDetailsAsync(id)
        _mockRepository.Setup(repo => repo.GetCoursWithDetailsAsync(coursId))
                       .ReturnsAsync(expectedCours);

        // Act
        var result = await _coursService.GetByIdAsync(coursId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(coursId, result.CoursId);
        Assert.Equal(expectedCours.Libelle, result.Libelle);

        // Vérifie que la bonne méthode du Repository a été appelée
        _mockRepository.Verify(repo => repo.GetCoursWithDetailsAsync(coursId), Times.Once);
    }


    // ===================================
    // TEST 2 : Lire un Cours Inexistant (GetByIdAsync)
    // ===================================
    [Fact]
    public async Task GetByIdAsync_CoursDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentId = 99;

        // Simuler que le Repository retourne null pour cet ID
        _mockRepository.Setup(repo => repo.GetCoursWithDetailsAsync(nonExistentId))
                       .ReturnsAsync((Cours)null);

        // Act
        var result = await _coursService.GetByIdAsync(nonExistentId);

        // Assert
        // Le service doit retourner null, confirmant l'absence de l'entité.
        Assert.Null(result);
        _mockRepository.Verify(repo => repo.GetCoursWithDetailsAsync(nonExistentId), Times.Once);
    }
    // ===================================
    // TEST 3 :Echec Cours si cours vide 
    // ===================================

    [Fact]
public async Task GetAllAsync_NoCoursExist_ReturnsEmptyList()
{
    // Arrange
    var emptyList = new List<Cours>();
    
    // Simuler que le Repository retourne une collection vide
    _mockRepository.Setup(repo => repo.GetAllCoursWithDetailsAsync())
                   .ReturnsAsync(emptyList);

    // Act
    var result = await _coursService.GetAllAsync();

    // Assert
    Assert.NotNull(result); // La collection elle-même ne doit pas être null
    Assert.Empty(result);   // La collection doit être vide
    _mockRepository.Verify(repo => repo.GetAllCoursWithDetailsAsync(), Times.Once);
}
}