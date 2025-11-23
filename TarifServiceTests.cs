using ASPPorcelette.API.DTOs.Tarif;
using ASPPorcelette.API.Models;
using ASPPorcelette.API.Repository.Interfaces;
using ASPPorcelette.API.Services.Implementation;
using ASPPorcelette.API.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

public class TarifServiceTests
{
    private readonly TarifService _tarifService;
    private readonly Mock<ITarifRepository> _mockTarifRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IDisciplineService> _mockDisciplineService;

    public TarifServiceTests()
    {
        _mockTarifRepository = new Mock<ITarifRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockDisciplineService = new Mock<IDisciplineService>();

        _tarifService = new TarifService(
            _mockTarifRepository.Object,
            _mockMapper.Object,
            _mockDisciplineService.Object // Injection du service mocké
        );
    }

    // --- Fonctions utilitaires ---
    private Tarif GetSampleTarif(int id, int disciplineId)
    {
        return new Tarif
        {
            TarifId = id,
            Nom = $"Tarif Mensuel {id}",
            Montant = 50.00m + id,
            Periodicite = "Mensuel",
            EstActif = true,
            DisciplineId = disciplineId
        };
    }

    private TarifCreateDto GetSampleCreateDto(int disciplineId)
    {
        return new TarifCreateDto
        {
            Nom = "Nouveau Tarif",
            Montant = 75.00m,
            Periodicite = "Annuel",
            DisciplineId = disciplineId
        };
    }

    private TarifUpdateDto GetSampleUpdateDto(int? disciplineId = null)
    {
        return new TarifUpdateDto
        {
            Nom = "Tarif Modifié",
            Montant = 80.00m,
            Periodicite = "Trimestriel",
            DisciplineId = disciplineId // Peut être null
        };
    }
    // --------------------------------------------------------------------------------
    // TESTS DE LECTURE (READ)
    // --------------------------------------------------------------------------------

    // Tester GetAllTarifsAsync (Cas nominal)
    [Fact]
    public async Task GetAllTarifsAsync_TarifsExist_ReturnsAllTarifs()
    {
        // Arrange
        var tarifs = new List<Tarif> { GetSampleTarif(1, 10), GetSampleTarif(2, 10) };
        _mockTarifRepository.Setup(repo => repo.GetAllTarifsWithDisciplineAsync())
                            .ReturnsAsync(tarifs);

        // Act
        var result = await _tarifService.GetAllTarifsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockTarifRepository.Verify(repo => repo.GetAllTarifsWithDisciplineAsync(), Times.Once);
    }

    // Tester GetTarifByIdAsync (Cas nominal)
    [Fact]
    public async Task GetTarifByIdAsync_TarifExists_ReturnsTarif()
    {
        // Arrange
        var tarifId = 5;
        var expectedTarif = GetSampleTarif(tarifId, 10);
        _mockTarifRepository.Setup(repo => repo.GetTarifByIdWithDisciplineAsync(tarifId))
                            .ReturnsAsync(expectedTarif);

        // Act
        var result = await _tarifService.GetTarifByIdAsync(tarifId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tarifId, result.TarifId);
        _mockTarifRepository.Verify(repo => repo.GetTarifByIdWithDisciplineAsync(tarifId), Times.Once);
    }

    // Tester GetTarifByIdAsync (Cas inexistant)
    [Fact]
    public async Task GetTarifByIdAsync_TarifDoesNotExist_ReturnsNull()
    {
        // Arrange
        var tarifId = 99;
        _mockTarifRepository.Setup(repo => repo.GetTarifByIdWithDisciplineAsync(tarifId))
                            .ReturnsAsync((Tarif)null);

        // Act
        var result = await _tarifService.GetTarifByIdAsync(tarifId);

        // Assert
        Assert.Null(result);
        _mockTarifRepository.Verify(repo => repo.GetTarifByIdWithDisciplineAsync(tarifId), Times.Once);
    }

    // --------------------------------------------------------------------------------
    // TESTS DE CRÉATION (CREATE)
    // --------------------------------------------------------------------------------

    // Cas nominal de création
    [Fact]
    public async Task CreateTarifAsync_ValidDiscipline_ReturnsCreatedTarif()
    {
        // Arrange
        var disciplineId = 1;
        var createDto = GetSampleCreateDto(disciplineId);
        var tarifEntity = GetSampleTarif(0, disciplineId);
        var createdTarif = GetSampleTarif(10, disciplineId);

        // Mock 1: Validation de la Discipline (GetByIdAsync lève une exception si null dans le service)
        // NOTE: Comme votre service GetByIdAsync lève une KeyNotFoundException, 
        // nous devons simuler GetByIdAsync(id) != null pour que IsDisciplineValid retourne true.
        _mockDisciplineService.Setup(s => s.GetByIdAsync(disciplineId)).ReturnsAsync(new Discipline()); // Une discipline non null

        // Mock 2: Mapping DTO -> Entité
        _mockMapper.Setup(m => m.Map<Tarif>(createDto)).Returns(tarifEntity);

        // Mock 3: Ajout au Repository
        _mockTarifRepository.Setup(repo => repo.AddAsync(tarifEntity)).ReturnsAsync(createdTarif);

        // Mock 4: Récupération pour le retour (appelle GetTarifByIdAsync)
        _mockTarifRepository.Setup(repo => repo.GetTarifByIdWithDisciplineAsync(createdTarif.TarifId))
                            .ReturnsAsync(createdTarif);

        // Act
        var result = await _tarifService.CreateTarifAsync(createDto);

        // Assert
        Assert.Equal(10, result.TarifId);
        _mockTarifRepository.Verify(repo => repo.AddAsync(tarifEntity), Times.Once);
        _mockDisciplineService.Verify(s => s.GetByIdAsync(disciplineId), Times.Once);
    }

    // Cas d'échec de validation de la clé étrangère
    [Fact]
    public async Task CreateTarifAsync_InvalidDiscipline_ThrowsKeyNotFoundException()
    {
        // Arrange
        var invalidDisciplineId = 99;
        var createDto = GetSampleCreateDto(invalidDisciplineId);

        // Mock 1: Validation de la Discipline (GetByIdAsync lève une exception)
        _mockDisciplineService.Setup(s => s.GetByIdAsync(invalidDisciplineId))
                              .ThrowsAsync(new KeyNotFoundException()); // Simuler l'échec de la validation

        // Act & Assert
        // On vérifie que la méthode lève l'exception spécifique
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _tarifService.CreateTarifAsync(createDto)
        );

        // Assert: S'assurer que l'ajout au Repository n'a jamais été appelé
        _mockTarifRepository.Verify(repo => repo.AddAsync(It.IsAny<Tarif>()), Times.Never);
        _mockDisciplineService.Verify(s => s.GetByIdAsync(invalidDisciplineId), Times.Once);
    }

    // --------------------------------------------------------------------------------
    // TESTS DE MODIFICATION (UPDATE/PATCH)
    // --------------------------------------------------------------------------------

    // Cas nominal de mise à jour (PUT)
    [Fact]
    public async Task UpdateTarifAsync_TarifExistsAndValidDiscipline_ReturnsUpdatedTarif()
    {
        // Arrange
        var tarifId = 5;
        var newDisciplineId = 2;
        var existingTarif = GetSampleTarif(tarifId, 1);
        var updateDto = GetSampleUpdateDto(newDisciplineId); // Changement de DisciplineId

        // Mock 1: Récupération de l'entité existante
        _mockTarifRepository.Setup(repo => repo.GetTarifByIdWithDisciplineAsync(tarifId))
                            .ReturnsAsync(existingTarif);

        // Mock 2: Validation de la nouvelle Discipline
        _mockDisciplineService.Setup(s => s.GetByIdAsync(newDisciplineId)).ReturnsAsync(new Discipline());

        // Mock 3: Mapping DTO -> Entité (simule le transfert des valeurs)
        _mockMapper.Setup(m => m.Map(updateDto, existingTarif))
           .Callback<TarifUpdateDto, Tarif>((src, dest) =>
           {
               dest.Nom = src.Nom;
               dest.Montant = src.Montant.Value; // <-- CORRECTION 1: Ajout de .Value
               dest.DisciplineId = src.DisciplineId.Value;
           });

        // Mock 4: Mise à jour réussie
        // Simule le succès de la mise à jour en retournant un Task<bool> avec la valeur true
        _mockTarifRepository.Setup(repo => repo.UpdateAsync(existingTarif)).ReturnsAsync(true);
        // Act
        var result = await _tarifService.UpdateTarifAsync(tarifId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Tarif Modifié", result.Nom);
        Assert.Equal(newDisciplineId, result.DisciplineId); // Vérifie le changement

        _mockTarifRepository.Verify(repo => repo.UpdateAsync(existingTarif), Times.Once);
        _mockDisciplineService.Verify(s => s.GetByIdAsync(newDisciplineId), Times.Once);
    }

    // Cas d'échec de mise à jour (Discipline Invalide)
    [Fact]
    public async Task UpdateTarifAsync_InvalidNewDiscipline_ThrowsKeyNotFoundException()
    {
        // Arrange
        var tarifId = 5;
        var invalidDisciplineId = 99;
        var existingTarif = GetSampleTarif(tarifId, 1);
        var updateDto = GetSampleUpdateDto(invalidDisciplineId);

        // Mock 1: Récupération de l'entité existante (réussie)
        _mockTarifRepository.Setup(repo => repo.GetTarifByIdWithDisciplineAsync(tarifId))
                            .ReturnsAsync(existingTarif);

        // Mock 2: Validation de la nouvelle Discipline (échec)
        _mockDisciplineService.Setup(s => s.GetByIdAsync(invalidDisciplineId))
                              .ThrowsAsync(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _tarifService.UpdateTarifAsync(tarifId, updateDto)
        );

        _mockTarifRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Tarif>()), Times.Never);
    }

    // Cas nominal de Mise à Jour Partielle (PATCH)
    [Fact]
    public async Task PartialUpdateTarifAsync_TarifExistsAndPatchValid_ReturnsSuccess()
    {
        // Arrange
        var tarifId = 5;
        var existingTarif = GetSampleTarif(tarifId, 1);
        var tarifDtoToPatch = GetSampleUpdateDto(1);
        tarifDtoToPatch.Montant = 50.00m; // Montant initial

        var patchDocument = new JsonPatchDocument<TarifUpdateDto>();
        patchDocument.Replace(t => t.Montant, 150.00m); // Patch: Augmenter le montant

        // Mocks pour la lecture
        _mockTarifRepository.Setup(repo => repo.GetTarifByIdWithDisciplineAsync(tarifId))
                            .ReturnsAsync(existingTarif);

        // Mocks pour le Mapping DTO
        _mockMapper.Setup(m => m.Map<TarifUpdateDto>(existingTarif)).Returns(tarifDtoToPatch);

        // Mocks pour le Mapping Entité (après patch)
        _mockMapper.Setup(m => m.Map(tarifDtoToPatch, existingTarif))
                   .Callback<TarifUpdateDto, Tarif>((src, dest) => dest.Montant = src.Montant.Value);

        // Mock pour la Validation (Discipline non changée, on simule la réussite)
        _mockDisciplineService.Setup(s => s.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Discipline());

        // Mock pour la Mise à jour
        // APRÈS (Simule le succès de la mise à jour en retournant Task<bool> true)
        _mockTarifRepository.Setup(repo => repo.UpdateAsync(existingTarif)).ReturnsAsync(true);
        // Act
        var (resultTarif, resultSuccess) = await _tarifService.PartialUpdateTarifAsync(tarifId, patchDocument);

        // Assert
        Assert.True(resultSuccess);
        Assert.NotNull(resultTarif);
        Assert.Equal(150.00m, resultTarif.Montant); // Montant mis à jour
        _mockTarifRepository.Verify(repo => repo.UpdateAsync(existingTarif), Times.Once);
    }

    // --------------------------------------------------------------------------------
    // TESTS DE SUPPRESSION (DELETE)
    // --------------------------------------------------------------------------------

    // Cas nominal de suppression
    [Fact]
    public async Task DeleteTarifAsync_TarifExists_ReturnsTrue()
    {
        // Arrange
        var tarifId = 5;

        // Le service appelle _tarifRepository.DeleteAsync(id)
        _mockTarifRepository.Setup(repo => repo.DeleteAsync(tarifId)).ReturnsAsync(true);

        // Act
        var result = await _tarifService.DeleteTarifAsync(tarifId);

        // Assert
        Assert.True(result);
        _mockTarifRepository.Verify(repo => repo.DeleteAsync(tarifId), Times.Once);
    }

    // Cas d'échec de suppression
    [Fact]
    public async Task DeleteTarifAsync_TarifDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var tarifId = 99;

        // Le service appelle _tarifRepository.DeleteAsync(id)
        _mockTarifRepository.Setup(repo => repo.DeleteAsync(tarifId)).ReturnsAsync(false);

        // Act
        var result = await _tarifService.DeleteTarifAsync(tarifId);

        // Assert
        Assert.False(result);
        _mockTarifRepository.Verify(repo => repo.DeleteAsync(tarifId), Times.Once);
    }
}