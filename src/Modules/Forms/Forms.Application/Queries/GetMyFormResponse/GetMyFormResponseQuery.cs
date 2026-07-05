using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Contracts;

namespace LankaConnect.Modules.Forms.Application.Queries.GetMyFormResponse;

/// <summary>
/// Gets a respondent's own response by access token (for anonymous edit page).
/// </summary>
public record GetMyFormResponseQuery(Guid FormId, string AccessToken) : IQuery<FormResponseDto>;
