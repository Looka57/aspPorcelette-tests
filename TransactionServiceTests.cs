using ASPPorcelette.API.DTOs.Transaction;
using ASPPorcelette.API.Models;
using ASPPorcelette.API.Repository.Interfaces;
using ASPPorcelette.API.Services.Implementation;
using AutoMapper;
using Moq;
using Xunit;
using Microsoft.AspNetCore.JsonPatch;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ASPPorcelette.API.Models.Enums; // <-- nécessaire pour TypeFlux
using static ASPPorcelette.API.Models.Enums.TypeFlux; // <-- permet Depense directement


public class TransactionServiceTests
{
    private readonly TransactionService _service;
    private readonly Mock<ITransactionRepository> _transactionRepo = new();
    private readonly Mock<ICompteRepository> _compteRepo = new();
    private readonly Mock<ICategorieTransactionRepository> _categorieRepo = new();
    private readonly Mock<IDisciplineRepository> _disciplineRepo = new();
    private readonly Mock<IMapper> _mapper = new();

    public TransactionServiceTests()
    {
        _service = new TransactionService(
            _transactionRepo.Object,
            _compteRepo.Object,
            _categorieRepo.Object,
            _disciplineRepo.Object,
            _mapper.Object
        );
    }

    // -----------------------------
    // 🔹 Helpers pour les tests
    // -----------------------------
    private Compte GetCompte(int id, decimal solde = 0m) => new Compte { CompteId = id, Solde = solde, Nom = $"Compte{id}" };
    private CategorieTransaction GetCategorie(int id, TypeFlux typeFlux) =>
    new CategorieTransaction { CategorieTransactionId = id, TypeFlux = typeFlux, Nom = $"Cat{id}" };


    private Transaction GetTransaction(int id, int compteId, int categorieId, decimal montant) =>
        new Transaction { TransactionId = id, CompteId = compteId, CategorieTransactionId = categorieId, Montant = montant, DisciplineId = 1 };

    private TransactionCreateDto GetCreateDto(decimal montant = 100m, int compteId = 1, int categorieId = 1) =>
        new TransactionCreateDto { Montant = montant, CompteId = compteId, CategorieTransactionId = categorieId, DisciplineId = 1, Description = "Test" };

    // -----------------------------
    // 🔹 CreateTransactionAsync
    // -----------------------------
    [Fact]
    public async Task CreateTransactionAsync_Valid_UpdatesCompte()
    {
        var userId = Guid.NewGuid();
        var dto = GetCreateDto();
        var compte = GetCompte(1, 200m);
        var categorie = GetCategorie(1, Depense);  // Passe Depense (TypeFlux)
        var transaction = GetTransaction(10, 1, 1, -100m);

        _compteRepo.Setup(r => r.GetByIdAsync(dto.CompteId)).ReturnsAsync(compte);
        _categorieRepo.Setup(r => r.GetByIdAsync(dto.CategorieTransactionId)).ReturnsAsync(categorie);
        _transactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).ReturnsAsync(transaction);
        _transactionRepo.Setup(r => r.GetByIdWithDetailsAsync(transaction.TransactionId)).ReturnsAsync(transaction);
        _mapper.Setup(m => m.Map<Transaction>(dto)).Returns(new Transaction { Montant = dto.Montant, CompteId = dto.CompteId, CategorieTransactionId = dto.CategorieTransactionId, DisciplineId = dto.DisciplineId });

        var result = await _service.CreateTransactionAsync(dto, userId);

        Assert.Equal(transaction.TransactionId, result.TransactionId);
        Assert.Equal(100m, compte.Solde); // 200 - 100
        _compteRepo.Verify(r => r.UpdateCompteAsync(It.Is<Compte>(c => c.Solde == 100m)), Times.Once);
    }

    // -----------------------------
    // 🔹 UpdateTransactionAsync
    // -----------------------------
    [Fact]
    public async Task UpdateTransactionAsync_Valid_UpdatesMontantEtSolde()
    {
        int id = 5;
        var transaction = GetTransaction(id, 1, 1, -50m);  // Dépense initiale
        var updateDto = new TransactionUpdateDto { Montant = 100m };
        var compte = GetCompte(1, 200m);
        var categorie = GetCategorie(1, Depense);

        _transactionRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(transaction);
        _compteRepo.Setup(r => r.GetByIdAsync(transaction.CompteId)).ReturnsAsync(compte);
        _categorieRepo.Setup(r => r.GetByIdAsync(transaction.CategorieTransactionId)).ReturnsAsync(categorie);
        _transactionRepo.Setup(r => r.UpdateAsync(It.IsAny<Transaction>())).ReturnsAsync(transaction);
        _transactionRepo.Setup(r => r.GetByIdWithDetailsAsync(id)).ReturnsAsync(transaction);
        _mapper.Setup(m => m.Map(updateDto, transaction)).Callback(() => transaction.Montant = updateDto.Montant ?? transaction.Montant);

        var result = await _service.UpdateTransactionAsync(id, updateDto);

        Assert.Equal(-100m, transaction.Montant);
        Assert.Equal(150m, compte.Solde);
    }

    [Fact]
    public async Task UpdateTransactionAsync_NotExist_ReturnsNull()
    {
        _transactionRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Transaction?)null);
        var result = await _service.UpdateTransactionAsync(99, new TransactionUpdateDto());
        Assert.Null(result);
        _transactionRepo.Verify(r => r.UpdateAsync(It.IsAny<Transaction>()), Times.Never);
    }

    // -----------------------------
    // 🔹 PartialUpdateTransactionAsync
    // -----------------------------
    [Fact]
    public async Task PartialUpdateTransactionAsync_ValidPatch_UpdatesTransaction()
    {
        int id = 1;
        var transaction = GetTransaction(id, 1, 1, -50m);  // Ajusté pour dépense
        var compte = GetCompte(1, 100m);
        var categorie = GetCategorie(1, Depense);

        _transactionRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(transaction);
        _compteRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(compte);
        _categorieRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(categorie);
        _transactionRepo.Setup(r => r.UpdateAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => t); // Retourne la transaction passée
        _transactionRepo.Setup(r => r.GetByIdWithDetailsAsync(id)).ReturnsAsync(transaction);
        _mapper.Setup(m => m.Map<TransactionUpdateDto>(transaction)).Returns(new TransactionUpdateDto { Montant = transaction.Montant });
        _mapper.Setup(m => m.Map(It.IsAny<TransactionUpdateDto>(), transaction)).Callback((TransactionUpdateDto dto, Transaction existing) =>
        {
            existing.Montant = dto.Montant ?? existing.Montant;
        });

        var patch = new JsonPatchDocument<TransactionUpdateDto>();
        patch.Replace(t => t.Montant, 100m);

        var (updated, success) = await _service.PartialUpdateTransactionAsync(id, patch);

        Assert.True(success);
        // Avec transaction initiale -50m : 100 - (-50) + (-100) = 50m
        Assert.Equal(-100m, updated!.Montant);
        Assert.Equal(50m, compte.Solde);
    }

    // -----------------------------
    // 🔹 DeleteTransactionAsync
    // -----------------------------
    [Fact]
    public async Task DeleteTransactionAsync_Existing_AdjustsSolde()
    {
        var transaction = GetTransaction(1, 1, 1, 50m);
        var compte = GetCompte(1, 200m);

        _transactionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(transaction);
        _compteRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(compte);
        _transactionRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _service.DeleteTransactionAsync(1);

        Assert.True(result);
        Assert.Equal(150m, compte.Solde);
    }

    // -----------------------------
    // 🔹 TransferAsync
    // -----------------------------
    [Fact]
    public async Task TransferAsync_Valid_UpdatesBothAccounts()
    {
        var userId = Guid.NewGuid();
        var source = GetCompte(1, 500m);
        var dest = GetCompte(2, 200m);
        var discipline = new Discipline { DisciplineId = 1 };

        _compteRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(source);
        _compteRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(dest);
        _disciplineRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(discipline);
        _transactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).ReturnsAsync(new Transaction());

        await _service.TransferAsync(1, 2, 100m, "Test", null, 1, userId);

        Assert.Equal(400m, source.Solde);
        Assert.Equal(300m, dest.Solde);
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Exactly(2));
    }

    // -----------------------------
    // 🔹 CreateTransactionAsync (Revenu)
    // -----------------------------
    [Fact]
    public async Task CreateTransactionAsync_Revenu_UpdatesCompteCorrectly()
    {
        var userId = Guid.NewGuid();
        // Crée un DTO avec un montant de 50 (input positif)
        var dto = GetCreateDto(montant: 50m);
        var compte = GetCompte(1, 100m); // Solde initial : 100

        // Obtient une catégorie de type REVENU
        var categorie = GetCategorie(1, TypeFlux.Revenu);

        // La transaction enregistrée doit avoir un montant positif (+50m)
        var transaction = GetTransaction(11, 1, 1, 50m);

        _compteRepo.Setup(r => r.GetByIdAsync(dto.CompteId)).ReturnsAsync(compte);
        _categorieRepo.Setup(r => r.GetByIdAsync(dto.CategorieTransactionId)).ReturnsAsync(categorie);
        _transactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).ReturnsAsync(transaction);
        _transactionRepo.Setup(r => r.GetByIdWithDetailsAsync(transaction.TransactionId)).ReturnsAsync(transaction);

        // Le mapper transfère le montant du DTO (50m)
        _mapper.Setup(m => m.Map<Transaction>(dto)).Returns(new Transaction { Montant = dto.Montant, CompteId = dto.CompteId, CategorieTransactionId = dto.CategorieTransactionId, DisciplineId = dto.DisciplineId });

        var result = await _service.CreateTransactionAsync(dto, userId);

        Assert.Equal(transaction.TransactionId, result.TransactionId);
        Assert.Equal(150m, compte.Solde); // 100 (initial) + 50 (revenu) = 150

        // Vérification que le montant positif (+50m) a été ajouté à la BDD
        _transactionRepo.Verify(r => r.AddAsync(It.Is<Transaction>(t => t.Montant == 50m)), Times.Once);
        _compteRepo.Verify(r => r.UpdateCompteAsync(It.Is<Compte>(c => c.Solde == 150m)), Times.Once);
    }


}
