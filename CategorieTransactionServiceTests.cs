using ASPPorcelette.API.Models;
using ASPPorcelette.API.Models.Enums;
using ASPPorcelette.API.Repository.Interfaces;
using ASPPorcelette.API.Services.Implementation;
using AutoMapper;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class CategorieTransactionServiceTests
{
    private readonly CategorieTransactionService _categorieService;
    private readonly Mock<ICategorieTransactionRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper; 

    public CategorieTransactionServiceTests()
    {
        _mockRepository = new Mock<ICategorieTransactionRepository>();
        _mockMapper = new Mock<IMapper>();
        
        // Instanciation du service réel avec les mocks
        _categorieService = new CategorieTransactionService(
            _mockRepository.Object, 
            _mockMapper.Object
        );
    }

    // --- Fonctions utilitaires ---
    private CategorieTransaction GetSampleCategorie(int id, TypeFlux type)
    {
        return new CategorieTransaction 
        { 
            CategorieTransactionId = id, 
            Nom = $"Catégorie {id} ({type})", 
            TypeFlux = type 
        };
    }
    
    // -------------------------------------------------------------
    // TEST 1 : Lecture de toutes les catégories (GetAllAsync)
    // -------------------------------------------------------------
    
    [Fact]
    public async Task GetAllAsync_CategoriesExist_ReturnsAllCategories()
    {
        // Arrange
        var categorieList = new List<CategorieTransaction>
        {
            GetSampleCategorie(1, TypeFlux.Revenu),
            GetSampleCategorie(2, TypeFlux.Depense)
        };
        
        // Simuler que le Repository retourne la liste
        _mockRepository.Setup(repo => repo.GetAllAsync())
                       .ReturnsAsync(categorieList);

        // Act
        var result = await _categorieService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count()); 
        Assert.Contains(result, c => c.TypeFlux == TypeFlux.Revenu);
        _mockRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }
    
    [Fact]
    public async Task GetAllAsync_NoCategoriesExist_ReturnsEmptyList()
    {
        // Arrange
        var emptyList = new List<CategorieTransaction>();
        
        // Simuler que le Repository retourne une collection vide
        _mockRepository.Setup(repo => repo.GetAllAsync())
                       .ReturnsAsync(emptyList);

        // Act
        var result = await _categorieService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result); 
        _mockRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    // -------------------------------------------------------------
    // TEST 2 : Lecture par ID (GetByIdAsync)
    // -------------------------------------------------------------
    
    [Fact]
    public async Task GetByIdAsync_CategorieExists_ReturnsCategorie()
    {
        // Arrange
        var categorieId = 1;
        var expectedCategorie = GetSampleCategorie(categorieId, TypeFlux.Neutre);
        
        // Simuler que le Repository retourne la catégorie
        _mockRepository.Setup(repo => repo.GetByIdAsync(categorieId))
                       .ReturnsAsync(expectedCategorie);

        // Act
        var result = await _categorieService.GetByIdAsync(categorieId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(categorieId, result.CategorieTransactionId);
        Assert.Equal(TypeFlux.Neutre, result.TypeFlux);
        _mockRepository.Verify(repo => repo.GetByIdAsync(categorieId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_CategorieDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentId = 99;
        
        // Simuler que le Repository retourne null
        _mockRepository.Setup(repo => repo.GetByIdAsync(nonExistentId))
                       .ReturnsAsync((CategorieTransaction)null);

        // Act
        var result = await _categorieService.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result); 
        _mockRepository.Verify(repo => repo.GetByIdAsync(nonExistentId), Times.Once);
    }
}